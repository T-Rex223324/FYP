using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardManager BoardManager;
    public PlayerController PlayerController;
    public UIDocument UIDoc;
    public TurnManager TurnManager { get; private set; }

    private int m_FoodAmount = 100;
    private Label m_FoodLabel;

    // Variable to hold our Day Label
    private Label m_DayLabel;

    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;

    private int m_CurrentLevel = 1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;

        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");

        // Find the DayLabel in the UI Document
        m_DayLabel = UIDoc.rootVisualElement.Q<Label>("DayLabel");

        m_GameOverPanel = UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");

        StartNewGame();
    }

    public void StartNewGame()
    {
        m_GameOverPanel.style.visibility = Visibility.Hidden;

        m_CurrentLevel = 1;

        // Starts the game with 100 food as requested!
        m_FoodAmount = 100;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        // Reset the day text when a new game starts
        if (m_DayLabel != null)
        {
            m_DayLabel.text = "Day : " + m_CurrentLevel;
        }

        BoardManager.Clean();
        // Pass the level into the Init function
        BoardManager.Init(m_CurrentLevel);

        PlayerController.Init();
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }

    public void NewLevel()
    {
        m_CurrentLevel++; // Increase the level FIRST so the UI shows the new day

        // Update the day text when moving to the next level
        if (m_DayLabel != null)
        {
            m_DayLabel.text = "Day : " + m_CurrentLevel;
        }

        BoardManager.Clean();
        // Pass the level into the Init function
        BoardManager.Init(m_CurrentLevel);

        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }

    void OnTurnHappen()
    {
        ChangeFood(-1);
    }

    public void ChangeFood(int amount)
    {
        m_FoodAmount += amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        // Check for Game Over condition
        if (m_FoodAmount <= 0)
        {
            // 1. Tell player to stop moving
            PlayerController.GameOver();

            // 2. Show the hidden Game Over panel
            m_GameOverPanel.style.visibility = Visibility.Visible;

            // 3. Update the text to show how many levels we survived
            m_GameOverMessage.text = "Game Over!\n\nSurvived " + m_CurrentLevel + " days";
        }
    }
}