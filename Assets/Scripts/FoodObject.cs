using UnityEngine;

public class FoodObject : CellObject
{
    public int AmountGranted = 10;

    public override void PlayerEntered()
    {
        Destroy(gameObject);

        //increase food
        GameManager.Instance.ChangeFood(AmountGranted);
    }
}

