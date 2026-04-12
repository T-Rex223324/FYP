using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Leaderboards;
using Unity.Services.CloudCode;

public class UGSManager : MonoBehaviour
{

    public bool IsCloudSyncDisabled = false;
    private float m_LastFocusCheckTime = 0f;

    public static UGSManager Instance { get; private set; }

    private string m_LocalDeviceToken;

    // We use a secret hardcoded password so the player only needs to remember their code!
    private readonly string SECRET_PASSWORD = "Save@30Days!";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        try
        {
            Debug.Log("Đang khởi tạo Unity Services..."); // Tells us the script is awake!
            await UnityServices.InitializeAsync();

            m_LocalDeviceToken = SecurePrefs.GetString("DeviceToken", "");
            if (string.IsNullOrEmpty(m_LocalDeviceToken))
            {
                m_LocalDeviceToken = System.Guid.NewGuid().ToString();
                SecurePrefs.SetString("DeviceToken", m_LocalDeviceToken);
                SecurePrefs.Save();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                string savedTransferCode = SecurePrefs.GetString("TransferCode", "");

                if (!string.IsNullOrEmpty(savedTransferCode))
                {
                    await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(savedTransferCode, SECRET_PASSWORD);
                }
                else
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                Debug.Log("Đã kết nối Cloud! ID: " + AuthenticationService.Instance.PlayerId);
                await CheckAndSyncCloudToLocal();
                //StartCoroutine(HeartbeatRoutine());
            }
        }
        catch (System.Exception e)
        {
            // IF WEBGL BLOCKS IT, THIS WILL TELL US EXACTLY WHY!
            Debug.LogError("LỖI KHỞI TẠO UGS: " + e.Message);
        }
    }

    public async Task<string> GenerateTransferCode()
    {
        try
        {
            // 1. Kiểm tra xem tài khoản này đã từng tạo mã chưa
            string secureCode = SecurePrefs.GetString("TransferCode", "");

            if (string.IsNullOrEmpty(secureCode))
            {
                // Lần đầu tiên tạo mã: Gắn mã OTP vĩnh viễn vào tài khoản
                secureCode = GenerateSecureOTP();
                await AuthenticationService.Instance.AddUsernamePasswordAsync(secureCode, SECRET_PASSWORD);

                SecurePrefs.SetString("TransferCode", secureCode);
                SecurePrefs.Save();
            }

            // 2. BẬT MÃ (Khởi động bộ đếm 5 phút)
            // Dù là mã mới hay mã cũ, ta đều gia hạn nó thêm 5 phút trên Cloud!
            DateTime expiryTime = DateTime.UtcNow.AddMinutes(5);
            var data = new Dictionary<string, object> { { "OTP_Expiry", expiryTime.ToString("o") } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            Debug.Log($"Tạo/Gia hạn mã thành công: {secureCode}. Hết hạn lúc: {expiryTime.ToLocalTime()}");
            return secureCode;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Lỗi tạo mã: " + ex.Message);
            return "LỖI";
        }
    }

    public async void LoginWithTransferCode(string codeToUse)
    {
        try
        {
            if (GameManager.Instance != null)
            {
                var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                if (displayLabel != null) displayLabel.text = "Syncing Cloud Data... Please Wait...";
            }

            if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();

            // 1. Thử đăng nhập bằng mã
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(codeToUse, SECRET_PASSWORD);

            // 2. KIỂM TRA ĐỒNG HỒ 5 PHÚT TRÊN CLOUD
            var keys = new HashSet<string> { "OTP_Expiry" };
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (savedData.TryGetValue("OTP_Expiry", out var expiryItem))
            {
                DateTime expiryTime = DateTime.Parse(expiryItem.Value.GetAsString());

                if (DateTime.UtcNow > expiryTime)
                {
                    Debug.LogError("Mã đã hết hạn (quá 5 phút)!");
                    if (GameManager.Instance != null)
                    {
                        var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                        if (displayLabel != null) displayLabel.text = "Error: Code Expired!";
                    }
                    AuthenticationService.Instance.SignOut(); // Đá văng ra ngoài
                    return;
                }
            }
            else
            {
                // Nếu hacker mò ra mã nhưng mã chưa được kích hoạt hẹn giờ -> Chặn luôn!
                Debug.LogError("Mã này đang bị đóng băng!");
                AuthenticationService.Instance.SignOut();
                return;
            }

            // 3. MÃ HỢP LỆ! Lưu lại mã vào máy mới và tải Save
            Debug.Log("Mã hợp lệ! Đang tải dữ liệu...");
            SecurePrefs.SetString("TransferCode", codeToUse);
            SecurePrefs.Save();

            await SyncCloudToLocal(forceOverwriteCloudToken: true);

            // 4. BURN AFTER READING (Đốt mã ngay lập tức)
            await BurnCode();
            Debug.Log("Chuyển máy thành công. Mã đã bị vô hiệu hóa.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Mã không hợp lệ: " + e.Message);
            if (GameManager.Instance != null)
            {
                var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                if (displayLabel != null) displayLabel.text = "Error: Invalid Code!";
            }
        }
    }

    

    private async Task CheckAndSyncCloudToLocal()
    {
        await SyncCloudToLocal(forceOverwriteCloudToken: false);
    }

    private async Task SyncCloudToLocal(bool forceOverwriteCloudToken)
    {
        try
        {
            var keys = new HashSet<string> { "DeviceToken", "GameStatistics", "SavedMap", "SavedDay", "SavedFood", "SavedChar", "PlayerX", "PlayerY", "HasSave" };
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            // 1. Check if the token exists on the cloud
            if (!forceOverwriteCloudToken && savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                // 2. Check if we are a Guest
                bool isGuest = string.IsNullOrEmpty(SecurePrefs.GetString("TransferCode", ""));

                // 3. If we are NOT a guest, compare the tokens!
                if (!isGuest)
                {
                    string cloudToken = cloudTokenItem.Value.GetAsString();
                    if (cloudToken != m_LocalDeviceToken)
                    {
                        KickPlayerOut();
                        return;
                    }
                }
            }
            if (savedData.TryGetValue("GameStatistics", out var statsItem)) SecurePrefs.SetString("GameStatistics", statsItem.Value.GetAsString());
            if (savedData.TryGetValue("SavedMap", out var mapItem)) SecurePrefs.SetString("SavedMap", mapItem.Value.GetAsString());
            if (savedData.TryGetValue("SavedDay", out var dayItem)) SecurePrefs.SetInt("SavedDay", dayItem.Value.GetAs<int>());
            if (savedData.TryGetValue("SavedFood", out var foodItem)) SecurePrefs.SetInt("SavedFood", foodItem.Value.GetAs<int>());
            if (savedData.TryGetValue("SavedChar", out var charItem)) SecurePrefs.SetInt("SavedChar", charItem.Value.GetAs<int>());
            if (savedData.TryGetValue("PlayerX", out var pxItem)) SecurePrefs.SetInt("PlayerX", pxItem.Value.GetAs<int>());
            if (savedData.TryGetValue("PlayerY", out var pyItem)) SecurePrefs.SetInt("PlayerY", pyItem.Value.GetAs<int>());
            if (savedData.TryGetValue("HasSave", out var saveItem)) SecurePrefs.SetInt("HasSave", saveItem.Value.GetAs<int>());

            SecurePrefs.Save();

            if (forceOverwriteCloudToken)
            {
                await OverwriteCloudTokenOnly();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
        catch (System.Exception)
        {
            await OverwriteCloudTokenOnly();
        }
    }

    public async void SyncLocalToCloud()
    {
        if (IsCloudSyncDisabled) return;
        try
        {
            var keys = new HashSet<string> { "DeviceToken" };
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                string cloudToken = cloudTokenItem.Value.GetAsString();

                // === NEW FIX: Don't kick Guests! ===
                bool isGuest = string.IsNullOrEmpty(SecurePrefs.GetString("TransferCode", ""));

                if (!isGuest && cloudToken != m_LocalDeviceToken)
                {
                    KickPlayerOut();
                    return;
                }
            }

            var data = new Dictionary<string, object>
            {
                { "DeviceToken", m_LocalDeviceToken },
                { "GameStatistics", SecurePrefs.GetString("GameStatistics", "") },
                { "SavedMap", SecurePrefs.GetString("SavedMap", "") },
                { "SavedDay", SecurePrefs.GetInt("SavedDay", 1) },
                { "SavedFood", SecurePrefs.GetInt("SavedFood", 100) },
                { "SavedChar", SecurePrefs.GetInt("SavedChar", 1) },
                { "PlayerX", SecurePrefs.GetInt("PlayerX", 1) },
                { "PlayerY", SecurePrefs.GetInt("PlayerY", 1) },
                { "HasSave", SecurePrefs.GetInt("HasSave", 0) }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("Save thành công lên Cloud!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi khi đẩy lên cloud: " + e.Message);
        }
    }

    private async Task OverwriteCloudTokenOnly()
    {
        var data = new Dictionary<string, object> { { "DeviceToken", m_LocalDeviceToken } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    private void KickPlayerOut()
    {
        // Changed to LogWarning so it doesn't freeze the Unity Editor!
        Debug.LogWarning("BỊ KICK: Tài khoản đã được đăng nhập ở máy khác!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowKickPopup();
        }
        else
        {
            ExecuteKickAndReload();
        }
    }

    // === NEW: The actual execution of the kick, called by the GameManager OK button! ===
    public void ExecuteKickAndReload()
    {
        if (UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
            // Clear the old broken session!
            AuthenticationService.Instance.ClearSessionToken();
        }

        // Wipe local memory and restart as a brand new Guest
        SecurePrefs.DeleteAll();
        m_LocalDeviceToken = System.Guid.NewGuid().ToString();
        SecurePrefs.SetString("DeviceToken", m_LocalDeviceToken);
        SecurePrefs.Save();

        // === THE FIX ===
        // Destroy the immortal UGSManager so a fresh one spawns!
        Instance = null;
        Destroy(gameObject);
        // ===============

        // Reload the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
    // ===================================================================================

    public async void DeleteCloudSave()
    {
        try
        {
            // FIXED WARNING: Using the updated Unity API for deleting saves
            await CloudSaveService.Instance.Data.Player.DeleteAsync("HasSave", new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
        }
        catch (System.Exception) { }
    }

    // === NEW: HEARTBEAT SYSTEM ===
    private System.Collections.IEnumerator HeartbeatRoutine()
    {
        // Wait a few seconds before starting the loop so it doesn't clash with the initial startup load
        yield return new WaitForSeconds(5f);

        while (true)
        {
            // Pause the script for 15 seconds, then check the cloud!
            yield return new WaitForSeconds(15f);

            if (AuthenticationService.Instance.IsSignedIn)
            {
                CheckTokenSilently();
            }
        }
    }

    private async void CheckTokenSilently()
    {
        if (IsCloudSyncDisabled) return;
        try
        {
            // We only download the DeviceToken to save bandwidth, not the whole save file!
            var keys = new HashSet<string> { "DeviceToken" };
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                string cloudToken = cloudTokenItem.Value.GetAsString();

                // === NEW FIX: Don't kick Guests! ===
                bool isGuest = string.IsNullOrEmpty(SecurePrefs.GetString("TransferCode", ""));

                // If they are not a guest, and the token changed... KICK!
                if (!isGuest && !string.IsNullOrEmpty(cloudToken) && cloudToken != m_LocalDeviceToken)
                {
                    Debug.LogWarning("HEARTBEAT BÁO ĐỘNG: Phát hiện đăng nhập từ thiết bị khác!");
                    KickPlayerOut();
                }
            }
        }
        catch (System.Exception)
        {
            // We leave this empty on purpose so the game doesn't crash if the internet flickers for a second!
        }
    }


    // =============================

    // === THE HYBRID SOLUTION ===
    // This built-in Unity function fires automatically whenever 
    // the player minimizes the game, switches tabs, or clicks back into the window!
    // === THE HYBRID SOLUTION ===
    private void OnApplicationFocus(bool hasFocus)
    {
        // === NEW FIX: Do not check anything if Unity Services haven't finished loading yet! ===
        if (UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized)
        {
            return;
        }
        // =======================================================================================

        // If the player just clicked back into the game window...
        if (hasFocus && AuthenticationService.Instance.IsSignedIn)
        {
            // === ANTI-SPAM COOLDOWN (FIXES THE 401 CORS ERROR) ===
            // We only allow this security check to run if 2 seconds have passed since the last check.
            // This prevents WebGL from firing 8 API requests at the exact same time!
            if (Time.time - m_LastFocusCheckTime > 2f)
            {
                m_LastFocusCheckTime = Time.time;
                Debug.Log("Player focused the window. Doing ONE quick security check...");
                CheckTokenSilently();
            }
        }
    }

    // === BẢO MẬT: THUẬT TOÁN SINH SỐ NGẪU NHIÊN CHUẨN MÃ HÓA (CSPRNG) ===
    private string GenerateSecureOTP()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        StringBuilder result = new StringBuilder("30D-");

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            byte[] data = new byte[8];
            rng.GetBytes(data);

            for (int i = 0; i < 4; i++) result.Append(chars[data[i] % chars.Length]);
            result.Append("-");
            for (int i = 4; i < 8; i++) result.Append(chars[data[i] % chars.Length]);
        }

        return result.ToString();
    }

    private async Task BurnCode()
    {
        // Đốt mã bằng cách đẩy thời gian hết hạn lùi về 1 ngày trước
        // Bất cứ ai đăng nhập sau giây phút này đều sẽ bị văng ra vì thời gian báo "Đã quá hạn"
        DateTime expiredTime = DateTime.UtcNow.AddDays(-1);
        var data = new Dictionary<string, object> { { "OTP_Expiry", expiredTime.ToString("o") } };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    // === LEADERBOARD SYSTEM ===

    // 1. Create a tiny class to hold the text so Unity can convert it to JSON!
    [System.Serializable]
    private class ScoreMetadata
    {
        public string details;
    }

    public async void SubmitDailyScore(int calculatedScore, string metadata)
    {
        if (IsCloudSyncDisabled || !AuthenticationService.Instance.IsSignedIn) return;
        if (StatisticsManager.Instance == null) return;

        try
        {
            var run = StatisticsManager.Instance.CurrentRun;

            // 1. Package the RAW facts (The server will do the math!)
            var arguments = new Dictionary<string, object>
            {
                { "daysSurvived", run.DaysSurvived },
                { "monstersKilled", run.MonstersKilled },
                { "stepsTaken", run.StepsTaken },
                { "wallsBroken", run.WallsBroken },
                { "metadataText", metadata }
            };

            // 2. Send it to the unhackable Cloud Code script!
            string response = await CloudCodeService.Instance.CallEndpointAsync<string>("SubmitScoreSecurely", arguments);
            Debug.Log("Server Reply: " + response);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Cloud Code Error: " + e.Message);
        }
    }

    // === NEW UI TOOLKIT LEADERBOARD GENERATOR ===
    public async Task PopulateLeaderboardUI(VisualElement container)
    {
        if (container == null) return;
        container.Clear(); // Wipe any old data

        // Show a loading message
        Label loadingLabel = new Label("Loading Leaderboard...");
        loadingLabel.style.color = Color.white;
        loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        loadingLabel.style.marginTop = 20;
        container.Add(loadingLabel);

        try
        {
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync("DailyLeaderboard", new GetScoresOptions { Limit = 10, IncludeMetadata = true });

            container.Clear(); // Remove loading text

            // 1. CREATE HEADER ROW (Blue background)
            VisualElement headerRow = CreateRow("RANK", "PLAYER", "SCORE", "DETAILS", new Color(0.15f, 0.35f, 0.75f), true);
            container.Add(headerRow);

            if (scoresResponse.Results.Count == 0)
            {
                Label emptyLabel = new Label("No one has played today! Be the first!");
                emptyLabel.style.color = Color.white;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.marginTop = 20;
                container.Add(emptyLabel);
                return;
            }

            // 2. CREATE DATA ROWS
            for (int i = 0; i < scoresResponse.Results.Count; i++)
            {
                var entry = scoresResponse.Results[i];
                string playerName = string.IsNullOrEmpty(entry.PlayerName) ? "Anonymous" : entry.PlayerName;
                string metadataText = "No details recorded.";

                if (!string.IsNullOrEmpty(entry.Metadata))
                {
                    try
                    {
                        // Safely unwrap the JSON object back into plain text
                        var parsedData = JsonUtility.FromJson<ScoreMetadata>(entry.Metadata);
                        if (parsedData != null && !string.IsNullOrEmpty(parsedData.details))
                        {
                            metadataText = parsedData.details;
                        }
                    }
                    catch { }
                }

                // Alternate row colors for that clean table look!
                Color rowColor = (i % 2 == 0) ? new Color(0.1f, 0.15f, 0.3f) : new Color(0.08f, 0.12f, 0.25f);

                VisualElement row = CreateRow($"{(entry.Rank + 1).ToString("00")}", playerName, $"{entry.Score}", metadataText, rowColor, false);
                container.Add(row);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading leaderboard: " + e.Message);
            container.Clear();
            Label errorLabel = new Label("Could not connect to Leaderboard.");
            errorLabel.style.color = Color.red;
            container.Add(errorLabel);
        }
    }

    // Helper method to generate a perfectly styled row block
    // Helper method to generate a perfectly styled row block
    private VisualElement CreateRow(string rank, string name, string score, string details, Color bgColor, bool isHeader)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.width = Length.Percent(100);
        row.style.backgroundColor = bgColor;

        row.style.paddingTop = 20;
        row.style.paddingBottom = 20;
        row.style.paddingLeft = 25;
        row.style.paddingRight = 25;
        row.style.marginBottom = 8;
        row.style.minHeight = 80;

        row.style.borderBottomLeftRadius = 8;
        row.style.borderBottomRightRadius = 8;
        row.style.borderTopLeftRadius = 8;
        row.style.borderTopRightRadius = 8;

        // === FONT SIZE ADJUSTED TO FIT ===
        // Size 28 and 24 are still huge, but will fit your long text!
        int fontSize = isHeader ? 28 : 24;

        // Rank Column (10% width)
        row.Add(CreateColumnLabel(rank, 10, isHeader, TextAnchor.MiddleLeft, fontSize, Color.white));

        // Name Column (20% width)
        row.Add(CreateColumnLabel(name, 20, isHeader, TextAnchor.MiddleLeft, fontSize, Color.white));

        // Score Column (15% width)
        Color scoreColor = isHeader ? Color.white : new Color(0.3f, 0.9f, 0.5f);
        row.Add(CreateColumnLabel(score, 15, isHeader, TextAnchor.MiddleCenter, fontSize, scoreColor));

        // Details Column (55% width)
        Color detailsColor = isHeader ? Color.white : new Color(0.8f, 0.8f, 0.8f);

        // === CHANGED TO MIDDLE CENTER ===
        row.Add(CreateColumnLabel(details, 55, false, TextAnchor.MiddleCenter, fontSize, detailsColor));

        return row;
    }

    // Helper method to generate individual text columns
    // Helper method to generate individual text columns
    private Label CreateColumnLabel(string text, float widthPercent, bool isBold, TextAnchor alignment, int fontSize, Color fontColor)
    {
        Label lbl = new Label(text);
        lbl.style.width = Length.Percent(widthPercent);
        lbl.style.unityTextAlign = alignment;
        lbl.style.whiteSpace = WhiteSpace.NoWrap;

        // === ADD THIS LINE TO PREVENT OVERLAPPING ===
        lbl.style.overflow = Overflow.Hidden;
        // ============================================

        lbl.style.color = fontColor;
        lbl.style.fontSize = fontSize;

        if (isBold) lbl.style.unityFontStyleAndWeight = FontStyle.Bold;

        return lbl;
    }
}

