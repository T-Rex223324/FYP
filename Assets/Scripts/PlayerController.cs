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

    // === NEW: Character Stats ===
    [Header("Character Stats")]
    public int StartingFood = 100;
    public int FoodDrainPerStep = 1;
    public bool MovesTwicePerFood = false;
    public int DamageTakenMultiplier = 1;
    public float FoodPickupMultiplier = 1.0f;

    // This tracks steps to calculate Steve's free movement
    private int m_StepCounter = 0;
    // ============================

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

        // === NEW: Apply Starting Food ===
        // The GameManager automatically gives 100 food when the game starts. 
        // Here, the player calculates the difference and applies it immediately!
        int foodDifference = StartingFood - 100;
        if (foodDifference != 0)
        {
            GameManager.Instance.ChangeFood(foodDifference);
        }
        // ================================
    }

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell, true);
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
        }

        m_Animator.SetBool("Moving", m_IsMoving);
    }

    public void GameOver()
    {
        m_IsGameOver = true;
    }

    public void TakeDamage(int damageAmount)
    {
        int finalDamage = damageAmount * DamageTakenMultiplier;

        m_Animator.SetTrigger("Hit");
        GameManager.Instance.ChangeFood(-finalDamage);

        // === NEW: SPIT OUT FLOATING TEXT! ===
        GameManager.Instance.ShowFloatingText("-" + finalDamage, transform.position, true);
        // ====================================

        if (SoundManager.Instance != null) SoundManager.Instance.RandomizeSfx(HitSounds);
    }

    private void Update()
    {
        if (GameManager.Instance.IsPaused) return;
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

            // === FIXED: Check if the player is "close enough" to the target ===
            if (Vector3.Distance(transform.position, m_MoveTarget) < 0.01f)
            {
                transform.position = m_MoveTarget; // Snap perfectly to the grid center!
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
                // === NEW: Apply Food Drain Passives/Debuffs ===
                m_StepCounter++;
                bool skipFoodDrain = (MovesTwicePerFood && m_StepCounter % 2 != 0);

                GameManager.Instance.TurnManager.Tick(); // This automatically subtracts 1 food

                if (FoodDrainPerStep > 1)
                {
                    // Bob consumes extra food
                    GameManager.Instance.ChangeFood(-(FoodDrainPerStep - 1));
                }
                else if (skipFoodDrain)
                {
                    // Steve gets his 1 food refunded this turn!
                    GameManager.Instance.ChangeFood(1);
                }
                // ==============================================

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
                    }
                }
            }
        }
    }

    // === NEW: Helper function for Caso's Food Multiplier ===
    public int CalculateFoodPickup(int baseFoodAmount)
    {
        return Mathf.RoundToInt(baseFoodAmount * FoodPickupMultiplier);
    }
    // =======================================================
}