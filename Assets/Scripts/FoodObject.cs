using UnityEngine;

public class FoodObject : CellObject
{
    public int AmountGranted = 10;

    // === NEW: AUDIO ARRAY ===
    public AudioClip[] EatSounds;
    // ========================

    public override void PlayerEntered()
    {
        // === NEW: PLAY EATING SOUND ===
        if (SoundManager.Instance != null && EatSounds.Length > 0)
        {
            SoundManager.Instance.RandomizeSfx(EatSounds);
        }
        // ==============================
        if (StatisticsManager.Instance != null) StatisticsManager.Instance.AddFoodEaten(gameObject.name);
        // Destroy the food item from the board
        Destroy(gameObject);

        // increase food
        GameManager.Instance.ChangeFood(AmountGranted);
    }
}