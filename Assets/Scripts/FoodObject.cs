using UnityEngine;

public class FoodObject : CellObject
{
    public int AmountGranted = 10;
    public AudioClip[] EatSounds;

    public override void PlayerEntered()
    {
        // === CRITICAL FIX: Added "EatSounds != null" to prevent crashes! ===
        if (SoundManager.Instance != null && EatSounds != null && EatSounds.Length > 0)
        {
            SoundManager.Instance.RandomizeSfx(EatSounds);
        }

        GameEvents.OnFoodEaten?.Invoke(AmountGranted, gameObject.name);

        // === CRITICAL FIX: Erase the food from the board's memory! ===
        GameManager.Instance.BoardManager.GetCellData(m_Cell).ContainedObject = null;
        // =============================================================

        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }
}