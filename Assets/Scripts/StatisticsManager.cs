using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RunStats
{
    public int RunNumber;
    public string CharacterName;
    public int DaysSurvived;
    public int StepsTaken;
    public int WallsBroken;
    public int MonstersKilled;
}

[System.Serializable]
public class LifetimeStats
{
    public int TotalRuns;
    public int TotalWins;
    public int HighestDaySurvived;
    public int TotalSteps;

    public int HighestFoodHeld;
    public int MostMonstersKilledInOneRun;
    public int MostWallsBrokenInOneRun;

    public int TotalFoodEaten;
    public int SmallFoodEaten;
    public int SodaDrank;
    public int BurgersEaten;

    public int TotalWallsBroken;
    public int WallType1Broken;
    public int WallType2Broken;
    public int CactusBroken;
    public int PinkRockBroken;

    public int TotalMonstersKilled;
    public int NormalEnemiesKilled;
    public int EliteEnemiesKilled;

    public int TotalHitsTaken;
}

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

        // Check for longest survival!
        if (finalDay > GameStats.Lifetime.HighestDaySurvived)
            GameStats.Lifetime.HighestDaySurvived = finalDay;

        // === NEW: Check for Personal Bests! ===
        if (CurrentRun.MonstersKilled > GameStats.Lifetime.MostMonstersKilledInOneRun)
            GameStats.Lifetime.MostMonstersKilledInOneRun = CurrentRun.MonstersKilled;

        if (CurrentRun.WallsBroken > GameStats.Lifetime.MostWallsBrokenInOneRun)
            GameStats.Lifetime.MostWallsBrokenInOneRun = CurrentRun.WallsBroken;
        // ======================================

        GameStats.PastRuns.Add(CurrentRun);
        SaveStats();
    }

    // === NEW DETAILED TRACKING FUNCTIONS ===
    public void AddWin() { GameStats.Lifetime.TotalWins++; }
    public void AddDamageTaken() { GameStats.Lifetime.TotalHitsTaken++; }

    public void AddStep()
    {
        if (CurrentRun != null) CurrentRun.StepsTaken++;
        GameStats.Lifetime.TotalSteps++;
    }

    public void AddFoodEaten(string foodName)
    {
        GameStats.Lifetime.TotalFoodEaten++;
        string lowerName = foodName.ToLower();

        if (lowerName.Contains("soda") || lowerName.Contains("coca")) GameStats.Lifetime.SodaDrank++;
        else if (lowerName.Contains("burger") || lowerName.Contains("big")) GameStats.Lifetime.BurgersEaten++;
        else GameStats.Lifetime.SmallFoodEaten++;
    }

    public void AddWallBroken(string wallName)
    {
        if (CurrentRun != null) CurrentRun.WallsBroken++;
        GameStats.Lifetime.TotalWallsBroken++;
        string lowerName = wallName.ToLower();

        if (lowerName.Contains("cactus")) GameStats.Lifetime.CactusBroken++;
        else if (lowerName.Contains("pink")) GameStats.Lifetime.PinkRockBroken++;
        else if (lowerName.Contains("type02")) GameStats.Lifetime.WallType2Broken++;
        else GameStats.Lifetime.WallType1Broken++;
    }

    public void AddMonsterKilled(bool isElite)
    {
        if (CurrentRun != null) CurrentRun.MonstersKilled++;
        GameStats.Lifetime.TotalMonstersKilled++;

        if (isElite) GameStats.Lifetime.EliteEnemiesKilled++;
        else GameStats.Lifetime.NormalEnemiesKilled++;
    }

    public void UpdateHighestFood(int currentFood)
    {
        if (currentFood > GameStats.Lifetime.HighestFoodHeld)
        {
            GameStats.Lifetime.HighestFoodHeld = currentFood;
        }
    }

    private void SaveStats()
    {
        string json = JsonUtility.ToJson(GameStats);
        PlayerPrefs.SetString("GameStatistics", json);
        PlayerPrefs.Save();
        if (UGSManager.Instance != null) UGSManager.Instance.SyncLocalToCloud();
    }

    private void LoadStats()
    {
        string json = PlayerPrefs.GetString("GameStatistics", "");
        if (string.IsNullOrEmpty(json)) GameStats = new AllGameStats();
        else GameStats = JsonUtility.FromJson<AllGameStats>(json);
    }
}