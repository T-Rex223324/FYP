using UnityEngine;
using System.Collections;

public class Enemy : CellObject
{
    public int Health = 3;
    public int Damage = 3;

    private int m_CurrentHealth;
    private Animator m_Animator;
    private SpriteRenderer m_SpriteRenderer;
    private bool m_IsMoving;
    private Vector3 m_MoveTarget;
    public float MoveSpeed = 5.0f;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.TurnManager != null)
            GameManager.Instance.TurnManager.OnTick += TurnHappened;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.TurnManager != null)
            GameManager.Instance.TurnManager.OnTick -= TurnHappened;
    }

    public override void Init(Vector2Int coord)
    {
        base.Init(coord);
        m_CurrentHealth = Health;
        m_IsMoving = false;
        if (m_Animator != null) m_Animator.SetBool("Moving", false);
        if (m_SpriteRenderer != null) m_SpriteRenderer.color = Color.white;
    }

    private void Update()
    {
        if (m_IsMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, m_MoveTarget) < 0.01f)
            {
                transform.position = m_MoveTarget;
                m_IsMoving = false;
                if (m_Animator != null) m_Animator.SetBool("Moving", false);
            }
        }
    }

    public override bool PlayerWantsToEnter()
    {
        m_CurrentHealth -= 1;

        // === JUICE: Flash Red when hit! ===
        if (gameObject.activeInHierarchy) StartCoroutine(FlashRed());

        if (m_CurrentHealth <= 0)
        {
            GameEvents.OnEnemyKilled?.Invoke(gameObject.name);

            // === CRITICAL FIX: Erase the enemy from the board's memory! ===
            GameManager.Instance.BoardManager.GetCellData(m_Cell).ContainedObject = null;
            // ===============================================================

            if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(gameObject);
            else Destroy(gameObject);
        }
        return false;
    }

    // === THE JUICE COROUTINE ===
    private IEnumerator FlashRed()
    {
        m_SpriteRenderer.color = new Color(1f, 0.3f, 0.3f); // Bright Red
        yield return new WaitForSeconds(0.1f);
        m_SpriteRenderer.color = Color.white; // Back to normal
    }

    bool MoveTo(Vector2Int coord)
    {
        var board = GameManager.Instance.BoardManager;
        var targetCell = board.GetCellData(coord);

        if (targetCell == null || !targetCell.Passable || targetCell.ContainedObject != null) return false;

        var currentCell = board.GetCellData(m_Cell);
        currentCell.ContainedObject = null;
        targetCell.ContainedObject = this;
        m_Cell = coord;

        m_MoveTarget = board.CellToWorld(coord);
        m_IsMoving = true;
        if (m_Animator != null) m_Animator.SetBool("Moving", true);

        return true;
    }

    void TurnHappened()
    {
        var playerCell = GameManager.Instance.PlayerController.Cell;
        int xDist = playerCell.x - m_Cell.x;
        int yDist = playerCell.y - m_Cell.y;
        int absXDist = Mathf.Abs(xDist);
        int absYDist = Mathf.Abs(yDist);

        if ((xDist == 0 && absYDist == 1) || (yDist == 0 && absXDist == 1))
        {
            if (m_Animator != null) m_Animator.SetTrigger("Attack");

            // === JUICE: Camera Shake when the Player gets hit! ===
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.2f);

            GameManager.Instance.PlayerController.TakeDamage(Damage);
        }
        else
        {
            if (absXDist > absYDist) { if (!TryMoveInX(xDist)) TryMoveInY(yDist); }
            else { if (!TryMoveInY(yDist)) TryMoveInX(xDist); }
        }
    }

    bool TryMoveInX(int xDist)
    {
        if (xDist > 0) return MoveTo(m_Cell + Vector2Int.right);
        return MoveTo(m_Cell + Vector2Int.left);
    }

    bool TryMoveInY(int yDist)
    {
        if (yDist > 0) return MoveTo(m_Cell + Vector2Int.up);
        return MoveTo(m_Cell + Vector2Int.down);
    }
}