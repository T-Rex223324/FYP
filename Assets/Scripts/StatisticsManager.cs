using UnityEngine;
using System.Collections.Generic;

// 1. Define what a single playthrough looks like
[System.Serializable]
public class RunStats
{
    public int RunNumber;
    public string CharacterName;
    public int DaysSurvived;
    public int StepsTaken;
    public int WallsBroken;
    public int SmallFoodEaten;
    public int MonstersKilled;
}

// 2. Define the Lifetime achievements
[System.Serializable]
public class LifetimeStats
{
    public int TotalRuns;
    public int TotalDaysSurvived;
    public int TotalFoodEaten;
    public int TotalHitsTaken;
}

// 3. The master save file that holds everything
[System.Serializable]
public class AllGameStats
{
    public LifetimeStats Lifetime = new LifetimeStats();
    public List<RunStats> PastRuns = new List<RunStats>();
}

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    public AllGameStats GameStats;
    public RunStats CurrentRun;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadStats();
    }

    // === TRACKING FUNCTIONS ===
    public void StartNewRun(int characterIndex)
    {
        CurrentRun = new RunStats();
        GameStats.Lifetime.TotalRuns++;
        CurrentRun.RunNumber = GameStats.Lifetime.TotalRuns;

        if (characterIndex == 1) CurrentRun.CharacterName = "Bob";
        else if (characterIndex == 2) CurrentRun.CharacterName = "Steve";
        else if (characterIndex == 3) CurrentRun.CharacterName = "Caso";
    }

    public void EndRun(int finalDay)
    {
        CurrentRun.DaysSurvived = finalDay;
        GameStats.Lifetime.TotalDaysSurvived += finalDay;

        // Add this run to the history list!
        GameStats.PastRuns.Add(CurrentRun);
        SaveStats();
    }

    // Call these from anywhere in your game to increase the numbers!
    public void AddStep() { if (CurrentRun != null) CurrentRun.StepsTaken++; }
    public void AddWallBroken() { if (CurrentRun != null) CurrentRun.WallsBroken++; }
    public void AddMonsterKilled() { if (CurrentRun != null) CurrentRun.MonstersKilled++; }
    public void AddDamageTaken() { GameStats.Lifetime.TotalHitsTaken++; }

    public void AddFoodEaten(bool isSmall)
    {
        if (CurrentRun != null)
        {
            if (isSmall) CurrentRun.SmallFoodEaten++;
            GameStats.Lifetime.TotalFoodEaten++;
        }
    }

    // === SAVING AND LOADING ===
    private void SaveStats()
    {
        string json = JsonUtility.ToJson(GameStats);
        PlayerPrefs.SetString("GameStatistics", json);
        PlayerPrefs.Save();
        Debug.Log("Stats Saved! Total Runs: " + GameStats.Lifetime.TotalRuns);
    }

    private void LoadStats()
    {
        string json = PlayerPrefs.GetString("GameStatistics", "");
        if (string.IsNullOrEmpty(json))
        {
            GameStats = new AllGameStats(); // First time playing!
        }
        else
        {
            GameStats = JsonUtility.FromJson<AllGameStats>(json);
        }
    }
}