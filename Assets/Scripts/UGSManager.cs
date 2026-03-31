using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UIElements;

public class UGSManager : MonoBehaviour
{

    public bool IsCloudSyncDisabled = false;

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

            m_LocalDeviceToken = PlayerPrefs.GetString("DeviceToken", "");
            if (string.IsNullOrEmpty(m_LocalDeviceToken))
            {
                m_LocalDeviceToken = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("DeviceToken", m_LocalDeviceToken);
                PlayerPrefs.Save();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                string savedTransferCode = PlayerPrefs.GetString("TransferCode", "");

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
        // === NEW FIX: Check if we ALREADY have a code before asking the server! ===
        string existingCode = PlayerPrefs.GetString("TransferCode", "");
        if (!string.IsNullOrEmpty(existingCode))
        {
            //Debug.Log("You already have a code! Showing existing code: " + existingCode);
            return existingCode;
        }
        // ==========================================================================

        string randomCode = "30DAYS-" + Random.Range(1000, 9999) + "-" + Random.Range(1000, 9999);

        try
        {
            await AuthenticationService.Instance.AddUsernamePasswordAsync(randomCode, SECRET_PASSWORD);

            PlayerPrefs.SetString("TransferCode", randomCode);
            PlayerPrefs.Save();

            Debug.Log("Tạo mã thành công: " + randomCode);
            return randomCode;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogWarning("Tài khoản này đã có mã rồi: " + ex.Message);
            return PlayerPrefs.GetString("TransferCode", "LỖI");
        }
    }

    public async void LoginWithTransferCode(string codeToUse)
    {
        try
        {
            // === NEW: Tell the UI to show a loading message! ===
            if (GameManager.Instance != null)
            {
                // We borrow the Code Display label to show the loading status!
                var displayLabel = GameManager.Instance.UIDoc.rootVisualElement.Q<Label>("CodeDisplayLabel");
                if (displayLabel != null) displayLabel.text = "Syncing Cloud Data... Please Wait...";
            }
            // ===================================================

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(codeToUse, SECRET_PASSWORD);

            PlayerPrefs.SetString("TransferCode", codeToUse);
            PlayerPrefs.Save();

            Debug.Log("Nhập mã thành công! Đang tải dữ liệu...");
            await SyncCloudToLocal(forceOverwriteCloudToken: true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Mã không hợp lệ: " + e.Message);

            // Reset the text if it fails
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
                bool isGuest = string.IsNullOrEmpty(PlayerPrefs.GetString("TransferCode", ""));

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
            if (savedData.TryGetValue("GameStatistics", out var statsItem)) PlayerPrefs.SetString("GameStatistics", statsItem.Value.GetAsString());
            if (savedData.TryGetValue("SavedMap", out var mapItem)) PlayerPrefs.SetString("SavedMap", mapItem.Value.GetAsString());
            if (savedData.TryGetValue("SavedDay", out var dayItem)) PlayerPrefs.SetInt("SavedDay", dayItem.Value.GetAs<int>());
            if (savedData.TryGetValue("SavedFood", out var foodItem)) PlayerPrefs.SetInt("SavedFood", foodItem.Value.GetAs<int>());
            if (savedData.TryGetValue("SavedChar", out var charItem)) PlayerPrefs.SetInt("SavedChar", charItem.Value.GetAs<int>());
            if (savedData.TryGetValue("PlayerX", out var pxItem)) PlayerPrefs.SetInt("PlayerX", pxItem.Value.GetAs<int>());
            if (savedData.TryGetValue("PlayerY", out var pyItem)) PlayerPrefs.SetInt("PlayerY", pyItem.Value.GetAs<int>());
            if (savedData.TryGetValue("HasSave", out var saveItem)) PlayerPrefs.SetInt("HasSave", saveItem.Value.GetAs<int>());

            PlayerPrefs.Save();

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
                bool isGuest = string.IsNullOrEmpty(PlayerPrefs.GetString("TransferCode", ""));

                if (!isGuest && cloudToken != m_LocalDeviceToken)
                {
                    KickPlayerOut();
                    return;
                }
            }

            var data = new Dictionary<string, object>
            {
                { "DeviceToken", m_LocalDeviceToken },
                { "GameStatistics", PlayerPrefs.GetString("GameStatistics", "") },
                { "SavedMap", PlayerPrefs.GetString("SavedMap", "") },
                { "SavedDay", PlayerPrefs.GetInt("SavedDay", 1) },
                { "SavedFood", PlayerPrefs.GetInt("SavedFood", 100) },
                { "SavedChar", PlayerPrefs.GetInt("SavedChar", 1) },
                { "PlayerX", PlayerPrefs.GetInt("PlayerX", 1) },
                { "PlayerY", PlayerPrefs.GetInt("PlayerY", 1) },
                { "HasSave", PlayerPrefs.GetInt("HasSave", 0) }
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
        PlayerPrefs.DeleteAll();
        m_LocalDeviceToken = System.Guid.NewGuid().ToString();
        PlayerPrefs.SetString("DeviceToken", m_LocalDeviceToken);
        PlayerPrefs.Save();

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
                bool isGuest = string.IsNullOrEmpty(PlayerPrefs.GetString("TransferCode", ""));

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
            Debug.Log("Player focused the window. Doing ONE quick security check...");
            CheckTokenSilently();
        }
    }


}