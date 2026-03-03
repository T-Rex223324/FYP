using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private BoardManager m_Board;
    private Vector2Int m_CellPosition;
    private bool m_IsGameOver;

    private Animator m_Animator;
    private bool m_IsMoving;
    private Vector3 m_MoveTarget;
    public float MoveSpeed = 5.0f;

    // === CHANGED: AUDIO ARRAYS INSTEAD OF SINGLE CLIPS ===
    public AudioClip[] MoveSounds;
    public AudioClip[] AttackSounds;
    public AudioClip[] HitSounds;
    // =====================================================

    // Allow enemies to read the player's position
    public Vector2Int Cell
    {
        get { return m_CellPosition; }
    }

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        // Note: We removed the local AudioSource here because the SoundManager handles it now!
    }

    public void Init()
    {
        m_IsGameOver = false;
        m_IsMoving = false;
    }

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell, true);
    }

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

            // === CHANGED: CALL SOUND MANAGER FOR FOOTSTEPS ===
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.RandomizeSfx(MoveSounds);
            }
            // =================================================
        }

        m_Animator.SetBool("Moving", m_IsMoving);
    }

    public void GameOver()
    {
        m_IsGameOver = true;
    }

    // Called by the Enemy when they attack
    public void TakeDamage(int damageAmount)
    {
        // Play the "Hit" or "Damage" animation
        m_Animator.SetTrigger("Hit");

        // Reduce food
        GameManager.Instance.ChangeFood(-damageAmount);

        // === CHANGED: CALL SOUND MANAGER FOR TAKING DAMAGE ===
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.RandomizeSfx(HitSounds);
        }
        // =====================================================
    }

    private void Update()
    {
        if (m_Board == null) return;

        if (m_IsGameOver)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                GameManager.Instance.StartNewGame();
            }
            return;
        }

        if (m_IsMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);

            if (transform.position == m_MoveTarget)
            {
                m_IsMoving = false;
                m_Animator.SetBool("Moving", false);

                var cellData = m_Board.GetCellData(m_CellPosition);
                if (cellData.ContainedObject != null)
                    cellData.ContainedObject.PlayerEntered();
            }
            return;
        }

        // Wait for a turn
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Trigger the turn manager without changing position
            GameManager.Instance.TurnManager.Tick();
            return;
        }

        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

        // Checks for Arrow Keys OR WASD Keys
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
        {
            newCellTarget.y += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
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
                    MoveTo(newCellTarget, false);
                }
                else
                {
                    bool canEnter = cellData.ContainedObject.PlayerWantsToEnter();

                    if (canEnter)
                    {
                        MoveTo(newCellTarget, false);
                    }
                    else
                    {
                        m_Animator.SetTrigger("Attack");

                        // === CHANGED: CALL SOUND MANAGER FOR CHOPPING/ATTACKING ===
                        if (SoundManager.Instance != null)
                        {
                            SoundManager.Instance.RandomizeSfx(AttackSounds);
                        }
                        // ==========================================================
                    }
                }
            }
        }
    }
}