    using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RunStats
{
    public int RunNumber;
    public string CharacterName;
    public int DaysSurvived;
    public int StepsTaken;
    public int TurnsTaken;
    public int WallsBroken;
    public int MonstersKilled;
}

[System.Serializable]
public class LifetimeStats
{
    public int TotalRuns;
    public int TotalWins;
    public int HighestDaySurvived;
    public RunStats BestRun;
    public int TotalSteps;
    public int TotalTurns;

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

        // Check for Personal Bests!
        if (CurrentRun.MonstersKilled > GameStats.Lifetime.MostMonstersKilledInOneRun)
            GameStats.Lifetime.MostMonstersKilledInOneRun = CurrentRun.MonstersKilled;

        if (CurrentRun.WallsBroken > GameStats.Lifetime.MostWallsBrokenInOneRun)
            GameStats.Lifetime.MostWallsBrokenInOneRun = CurrentRun.WallsBroken;

        // === NEW: ONLY RECORD THE BEST RUN IF IT IS A WIN (DAY 31) ===
        if (finalDay >= 31)
        {
            bool isNewBest = false;

            // If they have never won before, this is automatically the best run!
            if (GameStats.Lifetime.BestRun == null || GameStats.Lifetime.BestRun.DaysSurvived == 0)
            {
                isNewBest = true;
            }
            else
            {
                // If they ALREADY have a winning run, compare them! 
                // (We now calculate the score using all 4 tracked run statistics!)
                int currentScore = CurrentRun.MonstersKilled + CurrentRun.WallsBroken + CurrentRun.StepsTaken + CurrentRun.DaysSurvived;

                int oldBestScore = GameStats.Lifetime.BestRun.MonstersKilled + GameStats.Lifetime.BestRun.WallsBroken + GameStats.Lifetime.BestRun.StepsTaken + GameStats.Lifetime.BestRun.DaysSurvived;

                if (currentScore > oldBestScore)
                {
                    isNewBest = true; // The new run scored higher overall!
                }
            }

            // If the new run won the comparison, overwrite the old Best Run!
            // If the new run won the comparison, overwrite the old Best Run!
            if (isNewBest)
            {
                GameStats.Lifetime.BestRun = new RunStats
                {
                    RunNumber = CurrentRun.RunNumber,
                    CharacterName = CurrentRun.CharacterName,
                    DaysSurvived = CurrentRun.DaysSurvived,
                    StepsTaken = CurrentRun.StepsTaken,
                    TurnsTaken = CurrentRun.TurnsTaken, // <--- Add this line!
                    WallsBroken = CurrentRun.WallsBroken,
                    MonstersKilled = CurrentRun.MonstersKilled
                };
            }
        }
        // =============================================================

        // WE DELETED THE "PastRuns.Add(CurrentRun)" LINE SO IT STOPS SAVING 1000 RUNS!

        SaveStats(); // Instantly push this optimized data to the Cloud
    }

    // === NEW DETAILED TRACKING FUNCTIONS ===
    public void AddWin() { GameStats.Lifetime.TotalWins++; }
    public void AddDamageTaken() { GameStats.Lifetime.TotalHitsTaken++; }

    public void AddStep()
    {
        if (CurrentRun != null) CurrentRun.StepsTaken++;
        GameStats.Lifetime.TotalSteps++;
    }
    public void AddTurn()
    {
        if (CurrentRun != null) CurrentRun.TurnsTaken++;
        GameStats.Lifetime.TotalTurns++;
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