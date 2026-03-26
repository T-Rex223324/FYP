<<<<<<< HEAD
using UnityEditor;
=======
﻿using UnityEditor;
>>>>>>> origin/matser
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardManager BoardManager;

<<<<<<< HEAD
    // === NEW: Character Prefab List ===
    // We hide this PlayerController from the Inspector so we don't accidentally drag anything into it.
    [HideInInspector] public PlayerController PlayerController;

    // This creates a list in the Unity Inspector where you will drop Bob, Steve, and Caso!
    public PlayerController[] CharacterPrefabs;
    // ==================================
=======
    [HideInInspector] public PlayerController PlayerController;
    public PlayerController[] CharacterPrefabs;

    // === NEW: Floating Text Prefab ===
    public GameObject FloatingTextPrefab;
    // =================================

    // === NEW: Statistic & Account UI ===
    private VisualElement m_StatisticPanel;
    private Button m_StatisticButton;
    private Button m_CloseStatisticButton;
    private Label m_StatsText;

    private Button m_GenerateCodeButton;
    private Label m_CodeDisplayLabel;
    private TextField m_InputCodeField;
    private Button m_LoginCodeButton;
    // ===================================
>>>>>>> origin/matser

    public UIDocument UIDoc;
    public TurnManager TurnManager { get; private set; }

    public int DevStartDay = 1;
    public bool IsPaused { get; private set; }
    public int SelectedCharacter = 1;

    private int m_FoodAmount = 100;
    private Label m_FoodLabel;
    private Label m_DayLabel;
    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;

    private VisualElement m_MainMenuPanel;
    private Button m_PlayButton;
<<<<<<< HEAD
=======
    private Button m_ContinueButton;
>>>>>>> origin/matser

    private VisualElement m_PauseMenuPanel;
    private Button m_ResumeButton;
    private Button m_ExitMenuButton;

    private Slider m_MusicSlider;
    private Slider m_SFXSlider;

    private VisualElement m_CharacterSelectionPanel;
    private Button m_Char1Button;
    private Button m_Char2Button;
    private Button m_Char3Button;
    private Button m_BackButton;

    private int m_CurrentLevel = 1;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;

        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        m_DayLabel = UIDoc.rootVisualElement.Q<Label>("DayLabel");
        m_GameOverPanel = UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");

        m_MainMenuPanel = UIDoc.rootVisualElement.Q<VisualElement>("MainMenuPanel");
        m_PlayButton = m_MainMenuPanel.Q<Button>("PlayButton");

<<<<<<< HEAD
        // Note: I removed the old code that turned the player invisible here, 
        // because we don't even spawn the player until the game starts now!
=======
        m_ContinueButton = m_MainMenuPanel.Q<Button>("ContinueButton");
        if (m_ContinueButton != null)
        {
            m_ContinueButton.clicked += ContinueSavedGame;
            if (PlayerPrefs.GetInt("HasSave", 0) == 1)
                m_ContinueButton.style.display = DisplayStyle.Flex;
            else
                m_ContinueButton.style.display = DisplayStyle.None;
        }
>>>>>>> origin/matser

        if (m_PlayButton != null) m_PlayButton.clicked += StartGameFromMenu;

        m_PauseMenuPanel = UIDoc.rootVisualElement.Q<VisualElement>("PauseMenuPanel");
        m_ResumeButton = m_PauseMenuPanel.Q<Button>("ResumeButton");
        m_ExitMenuButton = m_PauseMenuPanel.Q<Button>("ExitMenuButton");

        if (m_ResumeButton != null) m_ResumeButton.clicked += ResumeGame;
        if (m_ExitMenuButton != null) m_ExitMenuButton.clicked += ReturnToMainMenu;

        m_MusicSlider = m_PauseMenuPanel.Q<Slider>("MusicSlider");
        m_SFXSlider = m_PauseMenuPanel.Q<Slider>("SFXSlider");

        if (m_MusicSlider != null)
        {
            m_MusicSlider.RegisterValueChangedCallback(evt =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(evt.newValue);
            });
        }
<<<<<<< HEAD

=======
>>>>>>> origin/matser
        if (m_SFXSlider != null)
        {
            m_SFXSlider.RegisterValueChangedCallback(evt =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(evt.newValue);
            });
        }

        m_CharacterSelectionPanel = UIDoc.rootVisualElement.Q<VisualElement>("CharacterSelectionPanel");
        m_Char1Button = m_CharacterSelectionPanel.Q<Button>("Char1Button");
        m_Char2Button = m_CharacterSelectionPanel.Q<Button>("Char2Button");
        m_Char3Button = m_CharacterSelectionPanel.Q<Button>("Char3Button");
        m_BackButton = m_CharacterSelectionPanel.Q<Button>("BackButton");

        if (m_Char1Button != null) m_Char1Button.clicked += () => ChooseCharacterAndPlay(1);
        if (m_Char2Button != null) m_Char2Button.clicked += () => ChooseCharacterAndPlay(2);
        if (m_Char3Button != null) m_Char3Button.clicked += () => ChooseCharacterAndPlay(3);
        if (m_BackButton != null) m_BackButton.clicked += CancelCharacterSelection;

        m_CharacterSelectionPanel.style.visibility = Visibility.Hidden;
        m_PauseMenuPanel.style.visibility = Visibility.Hidden;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Hidden;
        m_DayLabel.style.visibility = Visibility.Hidden;
<<<<<<< HEAD
=======

        // === NEW: Link Statistic UI ===
        m_StatisticPanel = UIDoc.rootVisualElement.Q<VisualElement>("StatisticPanel");
        m_StatisticButton = UIDoc.rootVisualElement.Q<Button>("StatisticButton");
        m_CloseStatisticButton = UIDoc.rootVisualElement.Q<Button>("CloseStatisticButton");
        m_StatsText = m_StatisticPanel?.Q<Label>("StatsText");

        m_GenerateCodeButton = m_StatisticPanel?.Q<Button>("GenerateCodeButton");
        m_CodeDisplayLabel = m_StatisticPanel?.Q<Label>("CodeDisplayLabel");
        m_InputCodeField = m_StatisticPanel?.Q<TextField>("InputCodeField");
        m_LoginCodeButton = m_StatisticPanel?.Q<Button>("LoginCodeButton");

        // Click Events
        if (m_StatisticButton != null) m_StatisticButton.clicked += OpenStatisticPanel;
        if (m_CloseStatisticButton != null) m_CloseStatisticButton.clicked += CloseStatisticPanel;
        if (m_GenerateCodeButton != null) m_GenerateCodeButton.clicked += OnGenerateCodeClicked;
        if (m_LoginCodeButton != null) m_LoginCodeButton.clicked += OnLoginCodeClicked;

        if (m_StatisticPanel != null) m_StatisticPanel.style.display = DisplayStyle.None;
        // ==============================

>>>>>>> origin/matser
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            m_MainMenuPanel.style.visibility == Visibility.Hidden &&
            m_GameOverPanel.style.visibility == Visibility.Hidden &&
            m_CharacterSelectionPanel.style.visibility == Visibility.Hidden)
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }

<<<<<<< HEAD
        if (Keyboard.current.tabKey.wasPressedThisFrame) NewLevel();
=======
        if (Keyboard.current.tabKey.wasPressedThisFrame && PlayerController != null) NewLevel();
>>>>>>> origin/matser
    }

    private void PauseGame()
    {
        IsPaused = true;
        m_PauseMenuPanel.style.visibility = Visibility.Visible;
    }

    private void ResumeGame()
    {
        IsPaused = false;
        m_PauseMenuPanel.style.visibility = Visibility.Hidden;
    }

    private void ReturnToMainMenu()
    {
<<<<<<< HEAD
=======
        PlayerPrefs.SetInt("SavedDay", m_CurrentLevel);
        PlayerPrefs.SetInt("SavedFood", m_FoodAmount);
        PlayerPrefs.SetInt("SavedChar", SelectedCharacter);
        PlayerPrefs.SetInt("HasSave", 1);

        // === CHANGED: WE NOW SAVE THE MAP AND PLAYER COORDINATES ===
        if (PlayerController != null)
        {
            PlayerPrefs.SetInt("PlayerX", PlayerController.Cell.x);
            PlayerPrefs.SetInt("PlayerY", PlayerController.Cell.y);
        }
        string mapJson = BoardManager.SaveMap();
        PlayerPrefs.SetString("SavedMap", mapJson);
        // ===========================================================

        PlayerPrefs.Save();

        if (m_ContinueButton != null) m_ContinueButton.style.display = DisplayStyle.Flex;
>>>>>>> origin/matser
        if (SoundManager.Instance != null) SoundManager.Instance.StopMusic();

        IsPaused = false;
        m_PauseMenuPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Hidden;
        m_DayLabel.style.visibility = Visibility.Hidden;

        m_MainMenuPanel.style.visibility = Visibility.Visible;
        BoardManager.Clean();

<<<<<<< HEAD
        // === NEW: Destroy the player when going back to the menu! ===
        if (PlayerController != null)
        {
            Destroy(PlayerController.gameObject);
        }
        // ============================================================
=======
        if (PlayerController != null) Destroy(PlayerController.gameObject);
>>>>>>> origin/matser
    }

    private void StartGameFromMenu()
    {
        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_CharacterSelectionPanel.style.visibility = Visibility.Visible;
    }

    private void CancelCharacterSelection()
    {
        m_CharacterSelectionPanel.style.visibility = Visibility.Hidden;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
    }

    private void ChooseCharacterAndPlay(int characterIndex)
    {
        SelectedCharacter = characterIndex;
        m_CharacterSelectionPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Visible;
        m_DayLabel.style.visibility = Visibility.Visible;
        StartNewGame();
    }

<<<<<<< HEAD
=======
    private void ContinueSavedGame()
    {
        m_CurrentLevel = PlayerPrefs.GetInt("SavedDay");
        m_FoodAmount = PlayerPrefs.GetInt("SavedFood");
        SelectedCharacter = PlayerPrefs.GetInt("SavedChar");

        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Visible;
        m_DayLabel.style.visibility = Visibility.Visible;

        m_FoodLabel.text = "Food : " + m_FoodAmount;
        if (m_DayLabel != null) m_DayLabel.text = "Day : " + m_CurrentLevel;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayRandomTrack();

        // === CHANGED: CALL THE NEW LOAD MAP FUNCTION ===
        BoardManager.Clean();
        string savedMap = PlayerPrefs.GetString("SavedMap");
        BoardManager.LoadMap(m_CurrentLevel, savedMap);
        // ===============================================

        if (CharacterPrefabs != null && CharacterPrefabs.Length > 0)
        {
            int index = SelectedCharacter - 1;
            PlayerController = Instantiate(CharacterPrefabs[index]);
            PlayerController.Init();
            SetFoodExact(m_FoodAmount);

            // === CHANGED: SPAWN PLAYER EXACTLY WHERE THEY SAVED ===
            int pX = PlayerPrefs.GetInt("PlayerX", 1);
            int pY = PlayerPrefs.GetInt("PlayerY", 1);
            PlayerController.Spawn(BoardManager, new Vector2Int(pX, pY));
            // ======================================================
        }
    }

>>>>>>> origin/matser
    public void StartNewGame()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayRandomTrack();

        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_CurrentLevel = DevStartDay;
        m_FoodAmount = 100;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (m_DayLabel != null) m_DayLabel.text = "Day : " + m_CurrentLevel;

        BoardManager.Clean();
        BoardManager.Init(m_CurrentLevel);

<<<<<<< HEAD
        // === NEW: SPAWN THE CHOSEN PREFAB ===
        // 1. Safety check to make sure you put the prefabs in the inspector!
        if (CharacterPrefabs == null || CharacterPrefabs.Length == 0) return;

        // 2. Arrays start at 0. So if SelectedCharacter is 1, we want index 0 (Bob).
        int index = SelectedCharacter - 1;

        // 3. Create the actual player prefab in the game!
        PlayerController = Instantiate(CharacterPrefabs[index]);
        // ====================================

        PlayerController.Init();
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
=======
        if (CharacterPrefabs != null && CharacterPrefabs.Length > 0)
        {
            int index = SelectedCharacter - 1;
            PlayerController = Instantiate(CharacterPrefabs[index]);
            PlayerController.Init();
            PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
        }
>>>>>>> origin/matser
    }

    public void NewLevel()
    {
        m_CurrentLevel++;
        if (m_DayLabel != null) m_DayLabel.text = "Day : " + m_CurrentLevel;

        if (m_CurrentLevel == 31)
        {
<<<<<<< HEAD
            PlayerController.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "You is the winner. Thank you for play my video game";
=======
            if (PlayerController != null) PlayerController.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "You is the winner. Thank you for play my video game";
            ClearSaveData();
>>>>>>> origin/matser
            return;
        }

        BoardManager.Clean();
        BoardManager.Init(m_CurrentLevel);
<<<<<<< HEAD
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
=======
        if (PlayerController != null) PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
>>>>>>> origin/matser
    }

    void OnTurnHappen() { ChangeFood(-1); }

    public void ChangeFood(int amount)
    {
        m_FoodAmount += amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (m_FoodAmount <= 0)
        {
<<<<<<< HEAD
            PlayerController.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "Game Over!\n\nSurvived " + m_CurrentLevel + " days";
        }
    }
=======
            if (PlayerController != null) PlayerController.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "Game Over!\n\nSurvived " + m_CurrentLevel + " days";
            ClearSaveData();
        }
    }

    public void SetFoodExact(int amount)
    {
        m_FoodAmount = amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
    }

    public void ClearSaveData()
    {
        PlayerPrefs.DeleteKey("HasSave");
        if (m_ContinueButton != null) m_ContinueButton.style.display = DisplayStyle.None;
    }

    // === NEW: Function to spawn floating text anywhere! ===
    public void ShowFloatingText(string message, Vector3 position, bool isDamage = true)
    {
        if (FloatingTextPrefab != null)
        {
            // Spawn the text slightly above the character's head
            Vector3 spawnPos = position + new Vector3(0, 0.5f, -2f);
            GameObject go = Instantiate(FloatingTextPrefab, spawnPos, Quaternion.identity);

            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) ft.Setup(message, isDamage);
        }
    }
    // ======================================================

    // === NEW: Statistic & Transfer Code Logic ===
    private void OpenStatisticPanel()
    {
        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_StatisticPanel.style.display = DisplayStyle.Flex;

        // Fetch the stats from the StatisticsManager and write them into the label!
        if (StatisticsManager.Instance != null && m_StatsText != null)
        {
            var lifetime = StatisticsManager.Instance.GameStats.Lifetime;
            m_StatsText.text =
                "Total Runs: " + lifetime.TotalRuns + "\n" +
                "Total Days Survived: " + lifetime.TotalDaysSurvived + "\n" +
                "Total Food Eaten: " + lifetime.TotalFoodEaten + "\n" +
                "Total Damage Taken: " + lifetime.TotalHitsTaken;
        }
    }

    private void CloseStatisticPanel()
    {
        m_StatisticPanel.style.display = DisplayStyle.None;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
    }

    private async void OnGenerateCodeClicked()
    {
        Debug.Log("Generate Button Clicked!"); // <--- ADD THIS LINE

        if (UGSManager.Instance != null && m_CodeDisplayLabel != null)
        {
            m_CodeDisplayLabel.text = "Generating...";
            string newCode = await UGSManager.Instance.GenerateTransferCode();
            m_CodeDisplayLabel.text = "Your Code: " + newCode;
        }
        else
        {
            Debug.LogError("Lỗi: UGSManager hoặc Label đang bị NULL!"); // <--- ADD THIS LINE
        }
    }

    private void OnLoginCodeClicked()
    {
        if (UGSManager.Instance != null && m_InputCodeField != null)
        {
            string codeToUse = m_InputCodeField.value.Trim(); // Get what the player typed
            if (!string.IsNullOrEmpty(codeToUse))
            {
                UGSManager.Instance.LoginWithTransferCode(codeToUse);
            }
        }
    }

   

>>>>>>> origin/matser
}