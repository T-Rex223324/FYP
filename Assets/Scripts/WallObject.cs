using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class WallObject : CellObject
{
    public Tile ObstacleTile;
    public Tile DamagedTile;
    public int MaxHealth = 3;

    // === JUICE: Add a particle explosion prefab ===
    public GameObject BreakParticlePrefab;

    private int m_HealthPoint;
    private Tile m_OriginalTile;
    private SpriteRenderer m_SpriteRenderer;

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void Init(Vector2Int cell)
    {
        base.Init(cell);
        m_HealthPoint = MaxHealth;
        m_OriginalTile = GameManager.Instance.BoardManager.GetCellTile(cell);
        GameManager.Instance.BoardManager.SetCellTile(cell, ObstacleTile);
        if (m_SpriteRenderer != null) m_SpriteRenderer.color = Color.white;
    }

    public override bool PlayerWantsToEnter()
    {
        m_HealthPoint -= 1;

        if (gameObject.activeInHierarchy) StartCoroutine(FlashWhite());

        if (m_HealthPoint > 0)
        {
            if (m_HealthPoint == 1 && DamagedTile != null) GameManager.Instance.BoardManager.SetCellTile(m_Cell, DamagedTile);
            return false;
        }

        GameEvents.OnWallBroken?.Invoke(gameObject.name);
        GameManager.Instance.BoardManager.SetCellTile(m_Cell, m_OriginalTile);

        // === CRITICAL FIX: Erase the wall from the board's memory! ===
        GameManager.Instance.BoardManager.GetCellData(m_Cell).ContainedObject = null;

        if (BreakParticlePrefab != null && ObjectPooler.Instance != null)
        {
            // === CRITICAL FIX: Spawn particles at Z: -1 so they render on top of the floor! ===
            Vector3 particlePos = transform.position + new Vector3(0, 0, -1f);
            ObjectPooler.Instance.SpawnFromPool(BreakParticlePrefab, particlePos);
        }

        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(gameObject);
        else Destroy(gameObject);

        return true;
    }

    // === THE JUICE COROUTINE ===
    private IEnumerator FlashWhite()
    {
        m_SpriteRenderer.color = new Color(0.8f, 0.8f, 0.8f, 0.5f); // Transparent flash
        yield return new WaitForSeconds(0.1f);
        m_SpriteRenderer.color = Color.white;
    }
}