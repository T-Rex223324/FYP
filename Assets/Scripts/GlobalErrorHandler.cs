using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.IO; // Needed for File Management!

public class GlobalErrorHandler : MonoBehaviour
{
    public static GlobalErrorHandler Instance { get; private set; }

    [Header("Crash Reporting")]
    public string DiscordWebhookURL = "https://discord.com/api/webhooks/1492760415741149235/OZzJImS8a57WhP8ESHrTsuNgg4N10Kq7uGn6j-YdjzU9iwl6s-95TqZ-2cRo_xPi23MG";

    private bool m_HasCrashed = false;
    private string m_ErrorMessage = "";
    private string m_StackTrace = "";
    private float m_Countdown = 30f;

    private static Queue<string> s_Breadcrumbs = new Queue<string>();

    // Updated Key
    private const string OFFLINE_REPORTS_KEY = "OfflineCrashDataV3";

    [System.Serializable]
    private class OfflineCrashData
    {
        public string DiscordMessage;
        public string FilePath; // We now store the PATH, not the giant text!
    }

    [System.Serializable]
    private class OfflineReportList { public List<OfflineCrashData> Reports = new List<OfflineCrashData>(); }

    public static void AddBreadcrumb(string actionMessage)
    {
        if (s_Breadcrumbs.Count >= 10) s_Breadcrumbs.Dequeue();
        s_Breadcrumbs.Enqueue($"[{Time.realtimeSinceStartup:F1}s] {actionMessage}");
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() { StartCoroutine(SendOfflineReportsCoroutine()); }
    private void OnEnable() { Application.logMessageReceived += HandleLog; }
    private void OnDisable() { Application.logMessageReceived -= HandleLog; }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception && !m_HasCrashed)
        {
            m_HasCrashed = true;
            m_Countdown = 30f;
            m_ErrorMessage = logString;
            m_StackTrace = string.IsNullOrEmpty(stackTrace) ? System.Environment.StackTrace : stackTrace;

            Time.timeScale = 0f;
            AudioListener.pause = true;

            StartCoroutine(ProcessCrashReport());
        }
    }

    private void Update()
    {
        if (m_HasCrashed)
        {
            m_Countdown -= Time.unscaledDeltaTime;
            if (m_Countdown <= 0f)
            {
                m_Countdown = 9999f;
                RestartGame();
            }
        }
    }

    private IEnumerator ProcessCrashReport()
    {
        if (string.IsNullOrEmpty(DiscordWebhookURL)) yield break;

        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string playerId = SystemInfo.deviceUniqueIdentifier;
        try { if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn) playerId = AuthenticationService.Instance.PlayerId; } catch { }

        string breadcrumbText = s_Breadcrumbs.Count > 0 ? string.Join("\n", s_Breadcrumbs) : "No actions recorded.";

        string fullLogContent = $"CRITICAL CRASH REPORT\n\nPlayer ID: {playerId}\nScene: {activeScene}\nTime: {Time.realtimeSinceStartup:F1}s\n\nERROR:\n{m_ErrorMessage}\n\nLAST 10 ACTIONS:\n{breadcrumbText}\n\nFULL STACK TRACE:\n{m_StackTrace}";

        string shortStack = m_StackTrace;
        if (shortStack.Length > 1000)
        {
            shortStack = shortStack.Substring(0, 1000) + "\n...[TRUNCATED: SEE ATTACHED TEXT FILE FOR FULL TRACE]";
        }

        string detailedMessage =
            $"🚨 **CRASH:** `{m_ErrorMessage}`\n" +
            $"👤 `ID: {playerId}` | 🗺️ `Scene: {activeScene}` | ⏱️ `{Time.realtimeSinceStartup:F1}s`\n" +
            $"🐾 **Last 10 Actions:**\n```text\n{breadcrumbText}\n```\n" +
            $"📍 **Stack Trace:**\n```cs\n{shortStack}\n```";

        yield return StartCoroutine(SendToDiscord(detailedMessage, fullLogContent, true));
    }

    private IEnumerator SendToDiscord(string messagePayload, string fileContent, bool saveIfOffline)
    {
        WWWForm form = new WWWForm();
        form.AddField("content", messagePayload);

        if (!string.IsNullOrEmpty(fileContent))
        {
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
            form.AddBinaryData("file", fileBytes, "Full_CrashLog.txt", "text/plain");
        }

        using (UnityWebRequest www = UnityWebRequest.Post(DiscordWebhookURL, form))
        {
            yield return www.SendWebRequest();

            // === THE FIX: Only touch the Hard Drive if the internet fails! ===
            if (www.result != UnityWebRequest.Result.Success && saveIfOffline)
            {
                try
                {
                    // Create a unique file name using the current time
                    string fileName = "CrashLog_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                    string filePath = Path.Combine(Application.persistentDataPath, fileName);

                    // Save the file physically to the computer
                    File.WriteAllText(filePath, fileContent);

                    // Tell the system to remember where we put this file
                    SaveReportOffline(messagePayload, filePath);
                }
                catch { /* Ignore if hard drive access is blocked */ }
            }
        }
    }

    private void SaveReportOffline(string payload, string filePath)
    {
        OfflineReportList list = new OfflineReportList();
        string existingJson = PlayerPrefs.GetString(OFFLINE_REPORTS_KEY, "");
        if (!string.IsNullOrEmpty(existingJson)) try { list = JsonUtility.FromJson<OfflineReportList>(existingJson); } catch { }

        string offlinePayload = payload.Replace("🚨 **CRASH:**", "📡 **[OFFLINE RECOVERY] CRASH:**");

        list.Reports.Add(new OfflineCrashData { DiscordMessage = offlinePayload, FilePath = filePath });

        PlayerPrefs.SetString(OFFLINE_REPORTS_KEY, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    private IEnumerator SendOfflineReportsCoroutine()
    {
        yield return new WaitForSeconds(5f);
        string existingJson = PlayerPrefs.GetString(OFFLINE_REPORTS_KEY, "");
        if (string.IsNullOrEmpty(existingJson)) yield break;

        OfflineReportList list = new OfflineReportList();
        try { list = JsonUtility.FromJson<OfflineReportList>(existingJson); } catch { yield break; }

        if (list.Reports.Count > 0)
        {
            List<OfflineCrashData> failedReports = new List<OfflineCrashData>();
            foreach (var report in list.Reports)
            {
                WWWForm form = new WWWForm();
                form.AddField("content", report.DiscordMessage);

                // Try to find the physical file on the hard drive
                if (!string.IsNullOrEmpty(report.FilePath) && File.Exists(report.FilePath))
                {
                    string fileContent = File.ReadAllText(report.FilePath);
                    byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
                    form.AddBinaryData("file", fileBytes, "Full_CrashLog.txt", "text/plain");
                }

                using (UnityWebRequest www = UnityWebRequest.Post(DiscordWebhookURL, form))
                {
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        failedReports.Add(report); // Still no internet, keep it!
                    }
                    else
                    {
                        // === THE FIX: Successfully sent! Delete the physical file! ===
                        try
                        {
                            if (File.Exists(report.FilePath)) File.Delete(report.FilePath);
                        }
                        catch { }
                    }
                }
                yield return new WaitForSeconds(1f);
            }

            list.Reports = failedReports;
            if (list.Reports.Count == 0) PlayerPrefs.DeleteKey(OFFLINE_REPORTS_KEY);
            else PlayerPrefs.SetString(OFFLINE_REPORTS_KEY, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }
    }

    private void RestartGame()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        m_HasCrashed = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void OnGUI()
    {
        if (!m_HasCrashed) return;

        GUI.backgroundColor = Color.black;
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 40;
        titleStyle.normal.textColor = Color.red;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(0, Screen.height / 2 - 100, Screen.width, 100), "A CRITICAL ERROR OCCURRED", titleStyle);

        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 24;
        textStyle.normal.textColor = Color.white;
        textStyle.alignment = TextAnchor.MiddleCenter;

        int secondsLeft = Mathf.CeilToInt(m_Countdown);
        GUI.Label(new Rect(0, Screen.height / 2, Screen.width, 100), $"We have saved the error report.\nThe game will automatically restart in {secondsLeft} seconds...", textStyle);
    }
}