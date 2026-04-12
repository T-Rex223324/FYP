using UnityEngine;

public class CellObject : MonoBehaviour
{
    // === NEW: Zero-Memory Object Identification ===
    public enum ObjectType { None, Wall, SmallFood, BigFood, Enemy, EliteEnemy, Exit }

    [HideInInspector] public ObjectType Type = ObjectType.None;
    [HideInInspector] public int PrefabIndex = 0; // Remembers exact wall types!
    // ==============================================

    protected Vector2Int m_Cell;

    public virtual void Init(Vector2Int cell)
    {
        m_Cell = cell;
    }

    public virtual void PlayerEntered()
    {

    }
    public virtual bool PlayerWantsToEnter()
    {
        return true;
    }
}