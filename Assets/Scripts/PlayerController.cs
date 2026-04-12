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

    [Header("Audio")]
    public AudioClip[] MoveSounds;
    public AudioClip[] AttackSounds;
    public AudioClip[] HitSounds;

    [Header("Character Stats")]
    public int StartingFood = 100;
    public int FoodDrainPerStep = 1;
    public bool MovesTwicePerFood = false;
    public int DamageTakenMultiplier = 1;
    public float FoodPickupMultiplier = 1.0f;

    private int m_StepCounter = 0;

    public Vector2Int Cell { get { return m_CellPosition; } }

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    public void Init()
    {
        m_IsGameOver = false;
        m_IsMoving = false;
        m_StepCounter = 0;

        int foodDifference = StartingFood - 100;
        if (foodDifference != 0)
        {
            GameManager.Instance.ChangeFood(foodDifference);
        }

        GlobalErrorHandler.AddBreadcrumb("Player Initialized.");
    }

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell, true);
        GlobalErrorHandler.AddBreadcrumb($"Player Spawned at {cell.x},{cell.y}");
    }

    public void MoveTo(Vector2Int cell, bool immediate)
    {
        if (m_Board == null) return;

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

            if (SoundManager.Instance != null) SoundManager.Instance.RandomizeSfx(MoveSounds);

            // === BREADCRUMB: Log Movement ===
            GlobalErrorHandler.AddBreadcrumb($"Moved to Tile {cell.x},{cell.y}");
        }

        m_Animator.SetBool("Moving", m_IsMoving);
    }

    public void GameOver()
    {
        m_IsGameOver = true;
        GlobalErrorHandler.AddBreadcrumb("Player Died (Game Over triggered).");
    }

    public void TakeDamage(int damageAmount)
    {
        int finalDamage = damageAmount * DamageTakenMultiplier;

        m_Animator.SetTrigger("Hit");
        GameManager.Instance.ChangeFood(-finalDamage);

        GameManager.Instance.ShowFloatingText("-" + finalDamage, transform.position, true);

        if (SoundManager.Instance != null) SoundManager.Instance.RandomizeSfx(HitSounds);

        // === BREADCRUMB: Log Damage ===
        GlobalErrorHandler.AddBreadcrumb($"Took {finalDamage} damage! Health/Food reduced.");
    }

    private void Update()
    {
        // === TEMPORARY TEST CRASH ===
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            throw new System.Exception("THIS IS A TEST CRASH! THE DISCORD BOT IS WORKING!");
        }
        // ============================

        if (GameManager.Instance.IsPaused) return;
        if (m_Board == null) return;

        if (m_IsGameOver)
        {
            // We check for both the main Enter key and the Numpad Enter key just to be safe!
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                // 1. Make sure the game isn't accidentally paused or frozen
                Time.timeScale = 1f;

                // 2. Bulletproof way to go back to the Main Menu!
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            return;
        }

        if (m_IsMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, m_MoveTarget) < 0.01f)
            {
                transform.position = m_MoveTarget;
                m_IsMoving = false;
                m_Animator.SetBool("Moving", false);

                var cellData = m_Board.GetCellData(m_CellPosition);
                if (cellData.ContainedObject != null)
                    cellData.ContainedObject.PlayerEntered();
            }
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GlobalErrorHandler.AddBreadcrumb("Pressed Space (Skipped Turn).");
            GameManager.Instance.TurnManager.Tick();
            return;
        }

        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

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
                m_StepCounter++;
                bool skipFoodDrain = (MovesTwicePerFood && m_StepCounter % 2 != 0);

                GameManager.Instance.TurnManager.Tick();

                if (FoodDrainPerStep > 1)
                {
                    GameManager.Instance.ChangeFood(-(FoodDrainPerStep - 1));
                }
                else if (skipFoodDrain)
                {
                    GameManager.Instance.ChangeFood(1);
                }

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

                        if (SoundManager.Instance != null)
                        {
                            SoundManager.Instance.RandomizeSfx(AttackSounds);
                        }

                        // === BREADCRUMB: Log Attack ===
                        GlobalErrorHandler.AddBreadcrumb($"Attacked object at {newCellTarget.x},{newCellTarget.y}");
                    }
                }
            }
        }
    }

    public int CalculateFoodPickup(int baseFoodAmount)
    {
        return Mathf.RoundToInt(baseFoodAmount * FoodPickupMultiplier);
    }
}