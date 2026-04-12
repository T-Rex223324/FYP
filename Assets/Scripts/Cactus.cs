using UnityEngine;

public class Cactus : WallObject
{
    public int Damage = 1;

    public override bool PlayerWantsToEnter()
    {
        // 1. Hurt the player
        GameManager.Instance.PlayerController.TakeDamage(Damage);

        // === JUICE: Add a small camera shake when hitting the cactus! ===
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.1f);
        // ================================================================

        // 2. Do normal Wall stuff (the cactus takes a hit, and breaks if health is 0)
        return base.PlayerWantsToEnter();
    }
}