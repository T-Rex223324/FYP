using UnityEngine;
using UnityEngine.Tilemaps;

public class WallObject : CellObject
{
    public Tile ObstacleTile;

    // === CHANGE START ===
    public int MaxHealth = 3;   // How many hits it takes to break
    private int m_HealthPoint;  // Current health
    private Tile m_OriginalTile; // The tile that was here before the wall
    // === CHANGE END ===

    public override void Init(Vector2Int cell)
    {
        base.Init(cell);

        // === CHANGE START ===
        m_HealthPoint = MaxHealth;
        // 1. Remember the tile that is currently on the ground (e.g., Grass)
        m_OriginalTile = GameManager.Instance.BoardManager.GetCellTile(cell);
        // 2. Replace it with the Wall tile
        GameManager.Instance.BoardManager.SetCellTile(cell, ObstacleTile);
        // === CHANGE END ===
    }

    public override bool PlayerWantsToEnter()
    {
        // === CHANGE START ===
        // The player bumped into us! Reduce health.
        m_HealthPoint -= 1;

        // If we still have health left, return false (movement blocked)
        if (m_HealthPoint > 0)
        {
            return false;
        }

        // If health is 0 or less, destroy the wall!
        // 1. Put the original ground tile back
        GameManager.Instance.BoardManager.SetCellTile(m_Cell, m_OriginalTile);
        // 2. Destroy this GameObject
        Destroy(gameObject);

        // Return true so the player can now walk into this empty space
        return true;
        // === CHANGE END ===
    }
}