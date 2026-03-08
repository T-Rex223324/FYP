using UnityEngine;

public class Cactus : WallObject
{
    public int Damage = 1;

    public override bool PlayerWantsToEnter()
    {
        // 1. Hurt the player (takes away 1 extra food and plays hit animation)
        GameManager.Instance.PlayerController.TakeDamage(Damage);

        // 2. Do normal Wall stuff (the cactus takes a hit, and breaks if health is 0)
        return base.PlayerWantsToEnter();
    }
}