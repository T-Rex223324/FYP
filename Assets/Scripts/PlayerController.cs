using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private BoardManager m_Board;
    private Vector2Int m_CellPosition;
    private bool m_IsGameOver;

    // === NEW VARIABLES FOR ANIMATION & SMOOTH MOVEMENT ===
    private Animator m_Animator;
    private bool m_IsMoving;
    private Vector3 m_MoveTarget;
    public float MoveSpeed = 5.0f; // Speed of the walk
    // =====================================================

    // === ADDED AWAKE to get the Animator ===
    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    public void Init()
    {
        m_IsGameOver = false;
        m_IsMoving = false;
    }

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        // Use MoveTo with 'true' for immediate teleportation on spawn
        MoveTo(cell, true);
    }

    // === UPDATED FUNCTION: Now handles Smooth Movement ===
    public void MoveTo(Vector2Int cell, bool immediate)
    {
        if (m_Board == null)
        {
            Debug.LogError("PlayerController: m_Board is null in MoveTo!");
            return;
        }

        m_CellPosition = cell;

        if (immediate)
        {
            m_IsMoving = false;
            transform.position = m_Board.CellToWorld(m_CellPosition);
        }
        else
        {
            m_IsMoving = true;
            m_MoveTarget = m_Board.CellToWorld(m_CellPosition);
        }

        // Tell the Animator if we are moving or not
        m_Animator.SetBool("Moving", m_IsMoving);
    }

    public void GameOver()
    {
        m_IsGameOver = true;
    }

    private void Update()
    {
        if (m_Board == null) return;

        // 1. Game Over Check
        if (m_IsGameOver)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                GameManager.Instance.StartNewGame();
            }
            return;
        }

        // === 2. NEW: Handle Smooth Movement ===
        // If the player is currently walking, move them towards the target
        if (m_IsMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);

            // Check if we reached the target
            if (transform.position == m_MoveTarget)
            {
                m_IsMoving = false;
                m_Animator.SetBool("Moving", false);

                // Check for events (like picking up food) AFTER we arrive
                var cellData = m_Board.GetCellData(m_CellPosition);
                if (cellData.ContainedObject != null)
                    cellData.ContainedObject.PlayerEntered();
            }
            // Stop here! Don't accept new input while walking
            return;
        }
        // ======================================

        // 3. Input Checks (Only runs if NOT moving)
        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x -= 1;
            hasMoved = true;
        }

        if (hasMoved)
        {
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.TurnManager.Tick();

                if (cellData.ContainedObject == null)
                {
                    // Move smoothly (immediate = false)
                    MoveTo(newCellTarget, false);
                }
                else if (cellData.ContainedObject.PlayerWantsToEnter())
                {
                    // Move smoothly (immediate = false)
                    MoveTo(newCellTarget, false);

                    // NOTE: We removed the immediate call to PlayerEntered() here.
                    // It is now called inside the "if (m_IsMoving)" block above
                    // when the player physically arrives at the square!
                }
            }
        }
    }
}