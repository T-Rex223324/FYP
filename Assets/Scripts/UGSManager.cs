using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UGSManager : MonoBehaviour
{
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
            Debug.Log("You already have a code! Showing existing code: " + existingCode);
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
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            // === FIXED: Login using Username/Password ===
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(codeToUse, SECRET_PASSWORD);

            PlayerPrefs.SetString("TransferCode", codeToUse);
            PlayerPrefs.Save();

            Debug.Log("Nhập mã thành công! Đang tải dữ liệu...");
            await SyncCloudToLocal(forceOverwriteCloudToken: true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Mã không hợp lệ: " + e.Message);
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

            if (!forceOverwriteCloudToken && savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                string cloudToken = cloudTokenItem.Value.GetAsString();
                if (cloudToken != m_LocalDeviceToken)
                {
                    KickPlayerOut();
                    return;
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
        try
        {
            var keys = new HashSet<string> { "DeviceToken" };
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (savedData.TryGetValue("DeviceToken", out var cloudTokenItem))
            {
                string cloudToken = cloudTokenItem.Value.GetAsString();
                if (cloudToken != m_LocalDeviceToken)
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
        Debug.LogError("BỊ KICK: Tài khoản đã được đăng nhập ở máy khác!");
        PlayerPrefs.DeleteAll();
        m_LocalDeviceToken = System.Guid.NewGuid().ToString();
        PlayerPrefs.SetString("DeviceToken", m_LocalDeviceToken);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public async void DeleteCloudSave()
    {
        try
        {
            // FIXED WARNING: Using the updated Unity API for deleting saves
            await CloudSaveService.Instance.Data.Player.DeleteAsync("HasSave", new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
        }
        catch (System.Exception) { }
    }
}