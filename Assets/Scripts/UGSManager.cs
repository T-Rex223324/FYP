using System;
using System.Collections;
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
    private readonly string SECRET_PASSWORD = "Save@30Days!";

    private Coroutine m_RollingCodeCoroutine;

    // === Bulletproof Kick & Warning Variables ===
    private bool m_IsKicked = false;
    private float m_KickTimer = 5f;

    private bool m_IsShowingGuestWarning = false;
    private string m_PendingLoginCode = "";
    // =======================================

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
            Debug.Log("Đang khởi tạo Unity Services...");
            await UnityServices.InitializeAsync();

            m_LocalDeviceToken = SecurePrefs.GetString("DeviceToken", "");
            if (string.IsNullOrEmpty(m_LocalDeviceToken))
            {
                m_LocalDeviceToken = System.Guid.NewGuid().ToString();
                SecurePrefs.SetString("DeviceToken", m_LocalDeviceToken);
                SecurePrefs.Save();
            }

            // If we just got kicked from another device, DO NOT auto-login. 
            // Stay as a fresh, clean Guest!
            if (PlayerPrefs.GetInt("JustKicked", 0) == 1)
            {
                Debug.Log("Player was just kicked. Staying logged out on Main Menu as a new Guest.");
                PlayerPrefs.DeleteKey("JustKicked");
                PlayerPrefs.Save();
                return;
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
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("LỖI KHỞI TẠO UGS: " + e.Message);
        }
    }

    private void Update()
    {
        if (m_IsKicked)
        {
            m_KickTimer -= Time.unscaledDeltaTime;
            if (m_KickTimer <= 0f)
            {
                m_KickTimer = 999f;
                ExecuteKickAndReload();
            }
        }
    }

    public async Task<string> GenerateTransferCode()
    {
        try
        {
            string secureCode = SecurePrefs.GetString("TransferCode", "");

            if (string.IsNullOrEmpty(secureCode))
            {
                secureCode = GenerateSecureOTP();
                await AuthenticationService.Instance.AddUsernamePasswordAsync(secureCode, SECRET_PASSWORD);

                SecurePrefs.SetString("TransferCode", secureCode);
                SecurePrefs.Save();
            }

            Debug.Log($"Mã khôi phục tài khoản của bạn là: {secureCode}");
            return secureCode;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Lỗi tạo mã: " + ex.Message);
            return "LỖI";
        }
    }

    // === NEW: SAFE LOGIN SYSTEM ===
    public async void LoginWithTransferCode(string codeToUse)
    {
        try
        {
            string currentCode = SecurePrefs.GetString("TransferCode", "");

            if (!string.IsNullOrEmpty(currentCode))
            {
                // SCENARIO 1: They already have an account. Backup their old data first!
                if (GameManager.Instance != null)
                {
                    var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                    if (displayLabel != null) displayLabel.text = "Backing up old account...";
                }

                await ForceSyncCurrentAccountToCloud();
                ProceedWithLogin(codeToUse);
            }
            else
            {
                // SCENARIO 2: They are a Guest. Show the warning screen!
                m_PendingLoginCode = codeToUse;
                m_IsShowingGuestWarning = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error starting login: " + e.Message);
        }
    }

    private async Task ForceSyncCurrentAccountToCloud()
    {
        if (IsCloudSyncDisabled || !AuthenticationService.Instance.IsSignedIn) return;
        try
        {
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
            Debug.Log("Old account backed up safely.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to backup old account: " + e.Message);
        }
    }

    private async void ProceedWithLogin(string codeToUse)
    {
        try
        {
            if (GameManager.Instance != null)
            {
                var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                if (displayLabel != null) displayLabel.text = "Syncing New Cloud Data...";
            }

            if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();

            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(codeToUse, SECRET_PASSWORD);

            // WIPE LOCAL MEMORY SO OLD STATS DON'T MERGE WITH THE NEW ACCOUNT
            SecurePrefs.DeleteAll();

            // Restore device ID and new transfer code
            SecurePrefs.SetString("DeviceToken", m_LocalDeviceToken);
            SecurePrefs.SetString("TransferCode", codeToUse);
            SecurePrefs.Save();

            await SyncCloudToLocal(forceOverwriteCloudToken: true);
            Debug.Log("Chuyển máy thành công.");
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
    // ===============================

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

            if (!forceOverwriteCloudToken && savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                bool isGuest = string.IsNullOrEmpty(SecurePrefs.GetString("TransferCode", ""));

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
        Debug.LogWarning("BỊ KICK: Tài khoản đã được đăng nhập ở máy khác!");

        m_IsKicked = true;
        m_KickTimer = 5f;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public void ExecuteKickAndReload()
    {
        if (UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
            AuthenticationService.Instance.ClearSessionToken();
        }

        SecurePrefs.DeleteAll();
        m_LocalDeviceToken = System.Guid.NewGuid().ToString();
        SecurePrefs.SetString("DeviceToken", m_LocalDeviceToken);
        SecurePrefs.Save();

        // Let the game know it was kicked so it resets perfectly to a guest
        PlayerPrefs.SetInt("JustKicked", 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Instance = null;
        Destroy(gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public async void DeleteCloudSave()
    {
        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync("HasSave", new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
        }
        catch (System.Exception) { }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized) return;

        if (hasFocus && AuthenticationService.Instance.IsSignedIn)
        {
            if (Time.time - m_LastFocusCheckTime > 2f)
            {
                m_LastFocusCheckTime = Time.time;
                CheckTokenSilently();
            }
        }
    }

    private async void CheckTokenSilently()
    {
        if (IsCloudSyncDisabled) return;
        try
        {
            var keys = new HashSet<string> { "DeviceToken" };
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                string cloudToken = cloudTokenItem.Value.GetAsString();
                bool isGuest = string.IsNullOrEmpty(SecurePrefs.GetString("TransferCode", ""));

                if (!isGuest && !string.IsNullOrEmpty(cloudToken) && cloudToken != m_LocalDeviceToken)
                {
                    Debug.LogWarning("HEARTBEAT BÁO ĐỘNG: Phát hiện đăng nhập từ thiết bị khác!");
                    KickPlayerOut();
                }
            }
        }
        catch (System.Exception) { }
    }

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
            var arguments = new Dictionary<string, object>
            {
                { "daysSurvived", run.DaysSurvived },
                { "monstersKilled", run.MonstersKilled },
                { "stepsTaken", run.StepsTaken },
                { "wallsBroken", run.WallsBroken },
                { "metadataText", metadata }
            };

            string response = await CloudCodeService.Instance.CallEndpointAsync<string>("SubmitScoreSecurely", arguments);
            Debug.Log("Server Reply: " + response);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Cloud Code Error: " + e.Message);
        }
    }

    public async Task PopulateLeaderboardUI(VisualElement container)
    {
        if (container == null) return;
        container.Clear();

        Label loadingLabel = new Label("Loading Leaderboard...");
        loadingLabel.style.color = Color.white;
        loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        loadingLabel.style.marginTop = 20;
        container.Add(loadingLabel);

        try
        {
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync("DailyLeaderboard", new GetScoresOptions { Limit = 10, IncludeMetadata = true });

            container.Clear();

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

            for (int i = 0; i < scoresResponse.Results.Count; i++)
            {
                var entry = scoresResponse.Results[i];
                string playerName = string.IsNullOrEmpty(entry.PlayerName) ? "Anonymous" : entry.PlayerName;
                string metadataText = "No details recorded.";

                if (!string.IsNullOrEmpty(entry.Metadata))
                {
                    try
                    {
                        var parsedData = JsonUtility.FromJson<ScoreMetadata>(entry.Metadata);
                        if (parsedData != null && !string.IsNullOrEmpty(parsedData.details))
                        {
                            metadataText = parsedData.details;
                        }
                    }
                    catch { }
                }

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

        int fontSize = isHeader ? 28 : 24;

        row.Add(CreateColumnLabel(rank, 10, isHeader, TextAnchor.MiddleLeft, fontSize, Color.white));
        row.Add(CreateColumnLabel(name, 20, isHeader, TextAnchor.MiddleLeft, fontSize, Color.white));

        Color scoreColor = isHeader ? Color.white : new Color(0.3f, 0.9f, 0.5f);
        row.Add(CreateColumnLabel(score, 15, isHeader, TextAnchor.MiddleCenter, fontSize, scoreColor));

        Color detailsColor = isHeader ? Color.white : new Color(0.8f, 0.8f, 0.8f);
        row.Add(CreateColumnLabel(details, 55, false, TextAnchor.MiddleCenter, fontSize, detailsColor));

        return row;
    }

    private Label CreateColumnLabel(string text, float widthPercent, bool isBold, TextAnchor alignment, int fontSize, Color fontColor)
    {
        Label lbl = new Label(text);
        lbl.style.width = Length.Percent(widthPercent);
        lbl.style.unityTextAlign = alignment;
        lbl.style.whiteSpace = WhiteSpace.NoWrap;
        lbl.style.overflow = Overflow.Hidden;
        lbl.style.color = fontColor;
        lbl.style.fontSize = fontSize;

        if (isBold) lbl.style.unityFontStyleAndWeight = FontStyle.Bold;

        return lbl;
    }

    // === NEW: Handles BOTH the Kick Screen and the Guest Warning! ===
    private void OnGUI()
    {
        if (m_IsKicked)
        {
            GUI.backgroundColor = Color.black;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 40;
            titleStyle.normal.textColor = Color.red;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(0, Screen.height / 2 - 100, Screen.width, 100), "DISCONNECTED", titleStyle);

            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 24;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.MiddleCenter;

            int secondsLeft = Mathf.CeilToInt(m_KickTimer);
            GUI.Label(new Rect(0, Screen.height / 2, Screen.width, 100), $"Your account was logged in from another device.\nYou have been logged out securely.\n\nReturning to Main Menu in {secondsLeft}...", textStyle);
        }
        else if (m_IsShowingGuestWarning)
        {
            // Dark transparent background
            GUI.backgroundColor = new Color(0, 0, 0, 0.95f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 36;
            titleStyle.normal.textColor = Color.yellow;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(0, Screen.height / 2 - 150, Screen.width, 100), "WARNING: GUEST PROGRESS WILL BE LOST", titleStyle);

            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 24;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100),
                "If you log in now, all your current Guest progress will be permanently deleted!\n" +
                "You should click 'Cancel', then click 'Generate Code' to save your progress first.\n\n" +
                "Are you sure you want to delete your local progress and log in?", textStyle);

            if (GUI.Button(new Rect(Screen.width / 2 - 210, Screen.height / 2 + 100, 200, 50), "OK (Delete Progress)"))
            {
                m_IsShowingGuestWarning = false;
                ProceedWithLogin(m_PendingLoginCode);
            }

            if (GUI.Button(new Rect(Screen.width / 2 + 10, Screen.height / 2 + 100, 200, 50), "Cancel"))
            {
                m_IsShowingGuestWarning = false;
                m_PendingLoginCode = "";

                if (GameManager.Instance != null)
                {
                    var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                    if (displayLabel != null) displayLabel.text = "Login Cancelled.";
                }
            }
        }
    }
}