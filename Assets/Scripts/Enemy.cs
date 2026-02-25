using UnityEngine;

public class Enemy : CellObject
{
    public int Health = 3;
    private int m_CurrentHealth;

    // === NEW VARIABLES FOR ANIMATION & MOVEMENT ===
    private Animator m_Animator;
    private bool m_IsMoving;
    private Vector3 m_MoveTarget;
    public float MoveSpeed = 5.0f; // Keep this the same as the Player's speed!
    // ==============================================

    private void Awake()
    {
        m_Animator = GetComponent<Animator>(); // Get the Animator
        GameManager.Instance.TurnManager.OnTick += TurnHappened;
    }

    private void OnDestroy()
    {
        GameManager.Instance.TurnManager.OnTick -= TurnHappened;
    }

    public override void Init(Vector2Int coord)
    {
        base.Init(coord);
        m_CurrentHealth = Health;
    }

    // === NEW: Update loop for smooth movement ===
    private void Update()
    {
        if (m_IsMoving)
        {
            // Slide smoothly towards the target
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);

            if (transform.position == m_MoveTarget)
            {
                m_IsMoving = false;
                if (m_Animator != null) m_Animator.SetBool("Moving", false);
            }
        }
    }
    // ============================================

    public override bool PlayerWantsToEnter()
    {
        m_CurrentHealth -= 1;

        // Optional: You could trigger an enemy "Hit" animation here if you make one!

        if (m_CurrentHealth <= 0)
        {
            Destroy(gameObject);
        }
        return false;
    }

    bool MoveTo(Vector2Int coord)
    {
        var board = GameManager.Instance.BoardManager;
        var targetCell = board.GetCellData(coord);

        if (targetCell == null || !targetCell.Passable || targetCell.ContainedObject != null)
        {
            return false;
        }

        var currentCell = board.GetCellData(m_Cell);
        currentCell.ContainedObject = null;

        targetCell.ContainedObject = this;
        m_Cell = coord;

        // === CHANGED: Smooth Movement Setup ===
        m_MoveTarget = board.CellToWorld(coord);
        m_IsMoving = true;
        if (m_Animator != null) m_Animator.SetBool("Moving", true);
        // ======================================

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
            // === CHANGED: Attack the Player ===
            if (m_Animator != null) m_Animator.SetTrigger("Attack"); // Play Zombie Attack Animation
            GameManager.Instance.PlayerController.TakeDamage(3); // Tell Player to take damage
            // ==================================
        }
        else
        {
            if (absXDist > absYDist)
            {
                if (!TryMoveInX(xDist)) TryMoveInY(yDist);
            }
            else
            {
                if (!TryMoveInY(yDist)) TryMoveInX(xDist);
            }
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