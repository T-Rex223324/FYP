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
    public int FruitEaten;
    public int SodaDrank;
    public int BurgersEaten;
    public int ChickenEaten;
    public int FishEaten;
    public int SaladEaten;

    public int TotalWallsBroken;
    public int NormalWallBroken;
    public int WallType2Broken;
    public int WallType3Broken;
    public int CactusBroken;
    public int CactusType2Broken;
    public int CactusType3Broken;
    public int PinkRockBroken;

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

    // === STEP 2: EVENT DRIVEN ARCHITECTURE ===
    // Listen to the Radio Tower when the object is active!
    private void OnEnable()
    {
        GameEvents.OnFoodEaten += HandleFoodEaten;
        GameEvents.OnEnemyKilled += AddMonsterKilled;
        GameEvents.OnWallBroken += AddWallBroken;
    }

    private void OnDisable()
    {
        GameEvents.OnFoodEaten -= HandleFoodEaten;
        GameEvents.OnEnemyKilled -= AddMonsterKilled;
        GameEvents.OnWallBroken -= AddWallBroken;
    }

    private void HandleFoodEaten(int amount, string foodName)
    {
        AddFoodEaten(foodName);
    }
    // =========================================

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

        if (finalDay > GameStats.Lifetime.HighestDaySurvived) GameStats.Lifetime.HighestDaySurvived = finalDay;
        if (CurrentRun.MonstersKilled > GameStats.Lifetime.MostMonstersKilledInOneRun) GameStats.Lifetime.MostMonstersKilledInOneRun = CurrentRun.MonstersKilled;
        if (CurrentRun.WallsBroken > GameStats.Lifetime.MostWallsBrokenInOneRun) GameStats.Lifetime.MostWallsBrokenInOneRun = CurrentRun.WallsBroken;

        if (finalDay >= 30)
        {
            bool isNewBest = false;
            int currentScore = CurrentRun.MonstersKilled + CurrentRun.WallsBroken + CurrentRun.StepsTaken + CurrentRun.DaysSurvived;
            string runDetails = $"The {CurrentRun.RunNumber} try: {CurrentRun.CharacterName} - Day {CurrentRun.DaysSurvived} | {CurrentRun.StepsTaken} Moves | {CurrentRun.TurnsTaken} Turns | {CurrentRun.MonstersKilled} Kills | {CurrentRun.WallsBroken} Walls";

            if (UGSManager.Instance != null) UGSManager.Instance.SubmitDailyScore(currentScore, runDetails);

            if (GameStats.Lifetime.BestRun == null || GameStats.Lifetime.BestRun.DaysSurvived == 0) isNewBest = true;
            else
            {
                int oldBestScore = GameStats.Lifetime.BestRun.MonstersKilled + GameStats.Lifetime.BestRun.WallsBroken + GameStats.Lifetime.BestRun.StepsTaken + GameStats.Lifetime.BestRun.DaysSurvived;
                if (currentScore > oldBestScore) isNewBest = true;
            }

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
        SaveStats();
    }

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
        if (foodName.IndexOf("soda", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.SodaDrank++;
        else if (foodName.IndexOf("burger", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.BurgersEaten++;
        else if (foodName.IndexOf("fruit", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.FruitEaten++;
        else if (foodName.IndexOf("chicken", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.ChickenEaten++;
        else if (foodName.IndexOf("fish", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.FishEaten++;
        else if (foodName.IndexOf("salad", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.SaladEaten++;
    }

    public void AddWallBroken(string wallName)
    {
        if (CurrentRun != null) CurrentRun.WallsBroken++;
        GameStats.Lifetime.TotalWallsBroken++;
        if (wallName.IndexOf("pink", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.PinkRockBroken++;
        else if (wallName.IndexOf("cactustype3", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.CactusType3Broken++;
        else if (wallName.IndexOf("cactustype2", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.CactusType2Broken++;
        else if (wallName.IndexOf("cactus", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.CactusBroken++;
        else if (wallName.IndexOf("walltype3", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.WallType3Broken++;
        else if (wallName.IndexOf("walltype2", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.WallType2Broken++;
        else GameStats.Lifetime.NormalWallBroken++;
    }

    public void AddMonsterKilled(string monsterName)
    {
        if (CurrentRun != null) CurrentRun.MonstersKilled++;
        GameStats.Lifetime.TotalMonstersKilled++;
        if (monsterName.IndexOf("elite", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.EliteZombieKilled++;
        else if (monsterName.IndexOf("fly", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.FlyGH28Killed++;
        else if (monsterName.IndexOf("mummy", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.MummyKilled++;
        else if (monsterName.IndexOf("mutant", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.MutantSlimeKilled++;
        else if (monsterName.IndexOf("slime", System.StringComparison.OrdinalIgnoreCase) >= 0) GameStats.Lifetime.SlimeKilled++;
        else GameStats.Lifetime.ZombieKilled++;
    }

    public void UpdateHighestFood(int currentFood)
    {
        if (currentFood > GameStats.Lifetime.HighestFoodHeld) GameStats.Lifetime.HighestFoodHeld = currentFood;
    }

    private void SaveStats()
    {
        string json = JsonUtility.ToJson(GameStats);
        SecurePrefs.SetString("GameStatistics", json);
        SecurePrefs.Save();
        if (UGSManager.Instance != null) UGSManager.Instance.SyncLocalToCloud();
    }

    private void LoadStats()
    {
        string json = SecurePrefs.GetString("GameStatistics", "");
        if (string.IsNullOrEmpty(json)) GameStats = new AllGameStats();
        else GameStats = JsonUtility.FromJson<AllGameStats>(json);
    }
}