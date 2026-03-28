using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardManager BoardManager;

    [HideInInspector] public PlayerController PlayerController;
    public PlayerController[] CharacterPrefabs;

    public GameObject FloatingTextPrefab;

    // === Statistic & Account UI ===
    private VisualElement m_StatisticPanel;
    private Button m_StatisticButton;
    private Button m_CloseStatisticButton;
    private Label m_StatsText;

    private Button m_GenerateCodeButton;
    private Label m_CodeDisplayLabel;
    private Button m_CopyCodeButton;
    private string m_GeneratedCode = "";
    private TextField m_InputCodeField;
    private Button m_LoginCodeButton;
    // ===================================

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
    private Button m_ContinueButton;

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

        m_ContinueButton = m_MainMenuPanel.Q<Button>("ContinueButton");
        if (m_ContinueButton != null)
        {
            m_ContinueButton.clicked += ContinueSavedGame;
            if (PlayerPrefs.GetInt("HasSave", 0) == 1)
                m_ContinueButton.style.display = DisplayStyle.Flex;
            else
                m_ContinueButton.style.display = DisplayStyle.None;
        }

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

        // Statistic UI Link
        m_StatisticPanel = UIDoc.rootVisualElement.Q<VisualElement>("StatisticPanel");
        m_StatisticButton = UIDoc.rootVisualElement.Q<Button>("StatisticButton");
        m_CloseStatisticButton = UIDoc.rootVisualElement.Q<Button>("CloseStatisticButton");

        // === CHANGED: Search the entire root document just to be safe! ===
        m_StatsText = UIDoc.rootVisualElement.Q<Label>("StatsText");
        // =================================================================

        m_GenerateCodeButton = m_StatisticPanel?.Q<Button>("GenerateCodeButton");
        m_CodeDisplayLabel = m_StatisticPanel?.Q<Label>("CodeDisplayLabel");
        m_CopyCodeButton = m_StatisticPanel?.Q<Button>("CopyCodeButton");
        if (m_CopyCodeButton != null) m_CopyCodeButton.clicked += CopyCodeToClipboard;
        m_InputCodeField = m_StatisticPanel?.Q<TextField>("InputCodeField");
        m_InputCodeField.Q("unity-text-input").style.color = Color.black;
        m_LoginCodeButton = m_StatisticPanel?.Q<Button>("LoginCodeButton");

        if (m_StatisticButton != null) m_StatisticButton.clicked += OpenStatisticPanel;
        if (m_CloseStatisticButton != null) m_CloseStatisticButton.clicked += CloseStatisticPanel;
        if (m_GenerateCodeButton != null) m_GenerateCodeButton.clicked += OnGenerateCodeClicked;
        if (m_LoginCodeButton != null) m_LoginCodeButton.clicked += OnLoginCodeClicked;

        if (m_StatisticPanel != null) m_StatisticPanel.style.display = DisplayStyle.None;
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

        if (Keyboard.current.tabKey.wasPressedThisFrame && PlayerController != null) NewLevel();
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
        PlayerPrefs.SetInt("SavedDay", m_CurrentLevel);
        PlayerPrefs.SetInt("SavedFood", m_FoodAmount);
        PlayerPrefs.SetInt("SavedChar", SelectedCharacter);
        PlayerPrefs.SetInt("HasSave", 1);

        if (PlayerController != null)
        {
            PlayerPrefs.SetInt("PlayerX", PlayerController.Cell.x);
            PlayerPrefs.SetInt("PlayerY", PlayerController.Cell.y);
        }
        string mapJson = BoardManager.SaveMap();
        PlayerPrefs.SetString("SavedMap", mapJson);
        PlayerPrefs.Save();

        if (m_ContinueButton != null) m_ContinueButton.style.display = DisplayStyle.Flex;
        if (SoundManager.Instance != null) SoundManager.Instance.StopMusic();

        IsPaused = false;
        m_PauseMenuPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Hidden;
        m_DayLabel.style.visibility = Visibility.Hidden;

        m_MainMenuPanel.style.visibility = Visibility.Visible;
        BoardManager.Clean();

        if (PlayerController != null) Destroy(PlayerController.gameObject);

        if (UGSManager.Instance != null) UGSManager.Instance.SyncLocalToCloud();
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

        BoardManager.Clean();
        string savedMap = PlayerPrefs.GetString("SavedMap");
        BoardManager.LoadMap(m_CurrentLevel, savedMap);

        if (CharacterPrefabs != null && CharacterPrefabs.Length > 0)
        {
            int index = SelectedCharacter - 1;
            PlayerController = Instantiate(CharacterPrefabs[index]);
            PlayerController.Init();
            SetFoodExact(m_FoodAmount);

            int pX = PlayerPrefs.GetInt("PlayerX", 1);
            int pY = PlayerPrefs.GetInt("PlayerY", 1);
            PlayerController.Spawn(BoardManager, new Vector2Int(pX, pY));
        }
    }

    public void StartNewGame()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayRandomTrack();

        if (StatisticsManager.Instance != null) StatisticsManager.Instance.StartNewRun(SelectedCharacter);

        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_CurrentLevel = DevStartDay;
        m_FoodAmount = 100;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (m_DayLabel != null) m_DayLabel.text = "Day : " + m_CurrentLevel;

        BoardManager.Clean();
        BoardManager.Init(m_CurrentLevel);

        if (CharacterPrefabs != null && CharacterPrefabs.Length > 0)
        {
            int index = SelectedCharacter - 1;
            PlayerController = Instantiate(CharacterPrefabs[index]);
            PlayerController.Init();
            PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
        }
    }

    public void NewLevel()
    {
        m_CurrentLevel++;
        if (m_DayLabel != null) m_DayLabel.text = "Day : " + m_CurrentLevel;

        if (m_CurrentLevel == 31)
        {
            if (StatisticsManager.Instance != null)
            {
                StatisticsManager.Instance.AddWin();
                StatisticsManager.Instance.EndRun(m_CurrentLevel);
            }

            if (PlayerController != null) PlayerController.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "You is the winner. Thank you for play my video game";
            ClearSaveData();
            return;
        }

        BoardManager.Clean();
        BoardManager.Init(m_CurrentLevel);
        if (PlayerController != null) PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }

    void OnTurnHappen() { ChangeFood(-1); }

    public void ChangeFood(int amount)
    {
        m_FoodAmount += amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.UpdateHighestFood(m_FoodAmount);

        if (m_FoodAmount <= 0)
        {
            if (StatisticsManager.Instance != null) StatisticsManager.Instance.EndRun(m_CurrentLevel);

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
        if (UGSManager.Instance != null) UGSManager.Instance.DeleteCloudSave();
    }

    public void ShowFloatingText(string message, Vector3 position, bool isDamage = true)
    {
        if (FloatingTextPrefab != null)
        {
            Vector3 spawnPos = position + new Vector3(0, 0.5f, -2f);
            GameObject go = Instantiate(FloatingTextPrefab, spawnPos, Quaternion.identity);

            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) ft.Setup(message, isDamage);
        }
    }

    private void OpenStatisticPanel()
    {
        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_StatisticPanel.style.display = DisplayStyle.Flex;

        if (StatisticsManager.Instance == null)
        {
            Debug.LogError("LỖI: StatisticsManager is missing from the scene!");
            return;
        }
        if (m_StatsText == null)
        {
            Debug.LogError("LỖI: Cannot find 'StatsText'! Check the exact name in UI Builder.");
            return;
        }

        var lifetime = StatisticsManager.Instance.GameStats.Lifetime;

        // Using Rich Text to add colors and bolding!
        m_StatsText.text =
            $"<color=#FACC15><b>--- PERSONAL BESTS (Single Run) ---</b></color>\n" +
            $"Longest Survival: <color=#FFFFFF>Day {lifetime.HighestDaySurvived}</color>\n" +
            $"Highest Food Held: <color=#FFFFFF>{lifetime.HighestFoodHeld}</color>\n" +
            $"Most Monsters Defeated: <color=#FFFFFF>{lifetime.MostMonstersKilledInOneRun}</color>\n" +
            $"Most Walls Mined: <color=#FFFFFF>{lifetime.MostWallsBrokenInOneRun}</color>\n\n" +

            $"<color=#38BDF8><b>--- LIFETIME TOTALS ---</b></color>\n" +
            $"Total Runs: <color=#FFFFFF>{lifetime.TotalRuns}</color>  |  Wins: <color=#FFFFFF>{lifetime.TotalWins}</color>\n" +
            $"Total Steps Walked: <color=#FFFFFF>{lifetime.TotalSteps}</color>\n\n" +

            $"<color=#4ADE80><b>--- FOOD CONSUMED ---</b></color>\n" +
            $"Total: <color=#FFFFFF>{lifetime.TotalFoodEaten}</color> (Small: {lifetime.SmallFoodEaten}, Soda: {lifetime.SodaDrank}, Burger: {lifetime.BurgersEaten})\n\n" +

            $"<color=#A78BFA><b>--- WALLS DESTROYED ---</b></color>\n" +
            $"Total: <color=#FFFFFF>{lifetime.TotalWallsBroken}</color> (Normal: {lifetime.WallType1Broken}, Type 2: {lifetime.WallType2Broken}, Cactus: {lifetime.CactusBroken}, Pink Rock: {lifetime.PinkRockBroken})\n\n" +

            $"<color=#F87171><b>--- MONSTERS DEFEATED ---</b></color>\n" +
            $"Total: <color=#FFFFFF>{lifetime.TotalMonstersKilled}</color> (Normal: {lifetime.NormalEnemiesKilled}, Elite: {lifetime.EliteEnemiesKilled})\n\n" +

            $"Damage Taken: <color=#FF8A65>{lifetime.TotalHitsTaken}</color>";

        // === NEW: INSTANTLY LOAD THE STICKY CODE ===
        if (m_CodeDisplayLabel != null)
        {
            string savedCode = PlayerPrefs.GetString("TransferCode", "");
            if (!string.IsNullOrEmpty(savedCode))
            {
                // If the player already has a code, show it immediately!
                m_CodeDisplayLabel.text = "Your Code: " + savedCode;
                m_GeneratedCode = savedCode; // Make sure the "Copy" button works immediately too!
            }
            else
            {
                // If they've never generated a code, tell them
                m_CodeDisplayLabel.text = "Your Code: Not Generated";
            }
        }

        // Reset the copy button text just in case it said "Copied!" previously
        if (m_CopyCodeButton != null) m_CopyCodeButton.text = "Copy Code";
        // ============================================
    }

    private void CloseStatisticPanel()
    {
        m_StatisticPanel.style.display = DisplayStyle.None;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
    }

    private async void OnGenerateCodeClicked()
    {
        Debug.Log("Generate Button Clicked!");

        if (UGSManager.Instance != null && m_CodeDisplayLabel != null)
        {
            m_CodeDisplayLabel.text = "Generating...";

            // Save the raw code into our new variable so we can copy it later!
            m_GeneratedCode = await UGSManager.Instance.GenerateTransferCode();
            m_CodeDisplayLabel.text = "Your Code: " + m_GeneratedCode;

            // Reset the copy button text just in case they generate a new one
            if (m_CopyCodeButton != null) m_CopyCodeButton.text = "Copy Code";
        }
        else
        {
            Debug.LogError("Lỗi: UGSManager hoặc Label đang bị NULL!");
        }
    }

    // === NEW: The function that actually copies the text! ===
    private void CopyCodeToClipboard()
    {
        // Make sure there is actually a code to copy!
        if (!string.IsNullOrEmpty(m_GeneratedCode))
        {
            // This single line of code tells Unity to copy to your PC/Browser clipboard!
            GUIUtility.systemCopyBuffer = m_GeneratedCode;

            Debug.Log("Code copied to clipboard: " + m_GeneratedCode);

            // Give the player visual feedback so they know it worked!
            if (m_CopyCodeButton != null) m_CopyCodeButton.text = "Copied!";
        }
    }
    // ========================================================

    private void OnLoginCodeClicked()
    {
        if (UGSManager.Instance != null && m_InputCodeField != null)
        {
            string codeToUse = m_InputCodeField.value.Trim();
            if (!string.IsNullOrEmpty(codeToUse))
            {
                UGSManager.Instance.LoginWithTransferCode(codeToUse);
            }
        }
    }





}