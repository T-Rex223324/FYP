using UnityEngine;
using UnityEngine.Tilemaps;

public class WallObject : CellObject
{
    public Tile ObstacleTile;
    
    // === CHALLENGE 2: Add reference to the Damaged Tile ===
    public Tile DamagedTile; 
    // ======================================================

    public int MaxHealth = 3;   
    private int m_HealthPoint;  
    private Tile m_OriginalTile; 

    public override void Init(Vector2Int cell)
    {
        base.Init(cell);

        m_HealthPoint = MaxHealth;
        m_OriginalTile = GameManager.Instance.BoardManager.GetCellTile(cell);
        
        // Set the wall to look like the full "ObstacleTile" at start
        GameManager.Instance.BoardManager.SetCellTile(cell, ObstacleTile);
    }

    public override bool PlayerWantsToEnter()
    {
        m_HealthPoint -= 1;

        if (m_HealthPoint > 0)
        {
            // === CHALLENGE 2: Check if wall is nearly destroyed ===
            if (m_HealthPoint == 1 && DamagedTile != null)
            {
                GameManager.Instance.BoardManager.SetCellTile(m_Cell, DamagedTile);
            }
            // ======================================================

            return false;
        }

        GameManager.Instance.BoardManager.SetCellTile(m_Cell, m_OriginalTile);
        Destroy(gameObject);
        return true;
    }
}