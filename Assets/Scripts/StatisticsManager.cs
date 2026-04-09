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

    // --- FOOD ---
    public int TotalFoodEaten;
    public int FruitEaten;
    public int SodaDrank;
    public int BurgersEaten;
    public int ChickenEaten;
    public int FishEaten;
    public int SaladEaten;

    // --- WALLS ---
    public int TotalWallsBroken;
    public int NormalWallBroken;
    public int WallType2Broken;
    public int WallType3Broken;
    public int CactusBroken;
    public int CactusType2Broken;
    public int CactusType3Broken;
    public int PinkRockBroken;

    // --- ENEMIES ---
    public int TotalMonstersKilled;
    public int ZombieKilled;
    public int EliteZombieKilled;
    public int FlyGH28Killed;
    public int MummyKilled;
    public int SlimeKilled;
    public int MutantSlimeKilled;

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
        if (finalDay >= 30)
        {
            bool isNewBest = false;

            // 1. Calculate the score
            int currentScore = CurrentRun.MonstersKilled + CurrentRun.WallsBroken + CurrentRun.StepsTaken + CurrentRun.DaysSurvived;

            // 2. Generate the detailed text string for the Leaderboard Metadata!
            string runDetails = $"The {CurrentRun.RunNumber} try: {CurrentRun.CharacterName} - Day {CurrentRun.DaysSurvived} | {CurrentRun.StepsTaken} Moves | {CurrentRun.TurnsTaken} Turns | {CurrentRun.MonstersKilled} Kills | {CurrentRun.WallsBroken} Walls";

            // 3. ALWAYS push the winning score AND the detailed text to the Leaderboard!
            if (UGSManager.Instance != null)
            {
                UGSManager.Instance.SubmitDailyScore(currentScore, runDetails);
            }

            // 4. Check if this is their first win ever
            if (GameStats.Lifetime.BestRun == null || GameStats.Lifetime.BestRun.DaysSurvived == 0)
            {
                isNewBest = true;
            }
            else
            {
                // 5. Compare with their old local best
                int oldBestScore = GameStats.Lifetime.BestRun.MonstersKilled + GameStats.Lifetime.BestRun.WallsBroken + GameStats.Lifetime.BestRun.StepsTaken + GameStats.Lifetime.BestRun.DaysSurvived;

                if (currentScore > oldBestScore)
                {
                    isNewBest = true;
                }
            }

            // 6. If it's a new best, save it locally to their profile
            if (isNewBest)
            {
                GameStats.Lifetime.BestRun = new RunStats
                {
                    RunNumber = CurrentRun.RunNumber,
                    CharacterName = CurrentRun.CharacterName,
                    DaysSurvived = CurrentRun.DaysSurvived,
                    StepsTaken = CurrentRun.StepsTaken,
                    TurnsTaken = CurrentRun.TurnsTaken,
                    WallsBroken = CurrentRun.WallsBroken,
                    MonstersKilled = CurrentRun.MonstersKilled
                };
            }
        }
        // =============================================================
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

        // Unity adds "(Clone)" to prefabs, so we just check if the name CONTAINS the word
        if (lowerName.Contains("soda")) GameStats.Lifetime.SodaDrank++;
        else if (lowerName.Contains("burger")) GameStats.Lifetime.BurgersEaten++;
        else if (lowerName.Contains("fruit")) GameStats.Lifetime.FruitEaten++;
        else if (lowerName.Contains("chicken")) GameStats.Lifetime.ChickenEaten++;
        else if (lowerName.Contains("fish")) GameStats.Lifetime.FishEaten++;
        else if (lowerName.Contains("salad")) GameStats.Lifetime.SaladEaten++;
    }

    public void AddWallBroken(string wallName)
    {
        if (CurrentRun != null) CurrentRun.WallsBroken++;
        GameStats.Lifetime.TotalWallsBroken++;
        string lowerName = wallName.ToLower();

        // Check the specific versions first!
        if (lowerName.Contains("pink")) GameStats.Lifetime.PinkRockBroken++;
        else if (lowerName.Contains("cactustype3")) GameStats.Lifetime.CactusType3Broken++;
        else if (lowerName.Contains("cactustype2")) GameStats.Lifetime.CactusType2Broken++;
        else if (lowerName.Contains("cactus")) GameStats.Lifetime.CactusBroken++;
        else if (lowerName.Contains("walltype3")) GameStats.Lifetime.WallType3Broken++;
        else if (lowerName.Contains("walltype2")) GameStats.Lifetime.WallType2Broken++;
        else GameStats.Lifetime.NormalWallBroken++; // Default dirt wall
    }

    public void AddMonsterKilled(string monsterName)
    {
        if (CurrentRun != null) CurrentRun.MonstersKilled++;
        GameStats.Lifetime.TotalMonstersKilled++;
        string lowerName = monsterName.ToLower();

        if (lowerName.Contains("elite")) GameStats.Lifetime.EliteZombieKilled++;
        else if (lowerName.Contains("fly")) GameStats.Lifetime.FlyGH28Killed++;
        else if (lowerName.Contains("mummy")) GameStats.Lifetime.MummyKilled++;
        else if (lowerName.Contains("mutant")) GameStats.Lifetime.MutantSlimeKilled++;
        else if (lowerName.Contains("slime")) GameStats.Lifetime.SlimeKilled++;
        else GameStats.Lifetime.ZombieKilled++; // Default normal zombie
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