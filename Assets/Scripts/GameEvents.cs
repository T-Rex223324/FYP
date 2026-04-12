using System;

public static class GameEvents
{
    // Int = amount of food, String = name of the food
    public static Action<int, string> OnFoodEaten;

    // String = name of the enemy or wall destroyed
    public static Action<string> OnEnemyKilled;
    public static Action<string> OnWallBroken;
}