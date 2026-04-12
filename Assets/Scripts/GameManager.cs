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
    private VisualElement m_KickPopupPanel;
    private Label m_KickTitleLabel;
    private Label m_KickMessageLabel;
    private Button m_KickOkButton;

    private VisualElement m_LeaderboardPanel;
    private VisualElement m_LeaderboardContainer;
    private Button m_OpenLeaderboardButton;
    private Button m_CloseLeaderboardButton;
    private Button m_ReloadLeaderboardButton;

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

    private Button m_ExitButton;
    private VisualElement m_ExitConfirmPanel;
    private Button m_ConfirmExitYesButton;
    private Button m_ConfirmExitNoButton;

    private int m_CurrentLevel = 1;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnFoodEaten += HandleFoodEaten;
    }

    private void OnDisable()
    {
        GameEvents.OnFoodEaten -= HandleFoodEaten;
    }

    private void HandleFoodEaten(int amount, string foodName)
    {
        if (PlayerController != null)
        {
            amount = PlayerController.CalculateFoodPickup(amount);
        }

        ChangeFood(amount);
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
        m_ExitButton = m_MainMenuPanel?.Q<Button>("ExitButton");
        m_ExitConfirmPanel = UIDoc.rootVisualElement.Q<VisualElement>("ExitConfirmPanel");
        m_ConfirmExitYesButton = m_ExitConfirmPanel?.Q<Button>("ConfirmExitYesButton");
        m_ConfirmExitNoButton = m_ExitConfirmPanel?.Q<Button>("ConfirmExitNoButton");

        if (m_ExitButton != null) m_ExitButton.clicked += ShowExitConfirmation;
        if (m_ConfirmExitYesButton != null) m_ConfirmExitYesButton.clicked += ExecuteExit;
        if (m_ConfirmExitNoButton != null) m_ConfirmExitNoButton.clicked += CancelExit;

        if (m_ExitConfirmPanel != null) m_ExitConfirmPanel.style.display = DisplayStyle.None;

        m_LeaderboardPanel = UIDoc.rootVisualElement.Q<VisualElement>("LeaderboardPanel");
        m_LeaderboardContainer = m_LeaderboardPanel?.Q<VisualElement>("LeaderboardContainer");

        m_OpenLeaderboardButton = UIDoc.rootVisualElement.Q<Button>("LeaderboardButton");
        m_CloseLeaderboardButton = m_LeaderboardPanel?.Q<Button>("CloseLeaderboardButton");
        m_ReloadLeaderboardButton = m_LeaderboardPanel?.Q<Button>("ReloadLeaderboardButton");

        if (m_OpenLeaderboardButton != null) m_OpenLeaderboardButton.clicked += OpenLeaderboard;
        if (m_CloseLeaderboardButton != null) m_CloseLeaderboardButton.clicked += CloseLeaderboard;
        if (m_ReloadLeaderboardButton != null) m_ReloadLeaderboardButton.clicked += ReloadLeaderboard;

        if (m_LeaderboardPanel != null) m_LeaderboardPanel.style.display = DisplayStyle.None;

        m_KickPopupPanel = UIDoc.rootVisualElement.Q<VisualElement>("KickPopupPanel");
        m_KickOkButton = UIDoc.rootVisualElement.Q<Button>("KickOkButton");
        m_KickTitleLabel = m_KickPopupPanel?.Q<Label>("KickTitleLabel");
        m_KickMessageLabel = m_KickPopupPanel?.Q<Label>("KickMessageLabel");

        if (m_KickOkButton != null) m_KickOkButton.clicked += OnKickOkClicked;
        if (m_KickPopupPanel != null) m_KickPopupPanel.style.display = DisplayStyle.None;

        if (m_ContinueButton != null)
        {
            m_ContinueButton.clicked += ContinueSavedGame;
            if (SecurePrefs.GetInt("HasSave", 0) == 1)
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

        m_StatisticPanel = UIDoc.rootVisualElement.Q<VisualElement>("StatisticPanel");
        m_StatisticButton = UIDoc.rootVisualElement.Q<Button>("StatisticButton");
        m_CloseStatisticButton = UIDoc.rootVisualElement.Q<Button>("CloseStatisticButton");
        m_StatsText = UIDoc.rootVisualElement.Q<Label>("StatsText");

        m_GenerateCodeButton = m_StatisticPanel?.Q<Button>("GenerateCodeButton");
        m_CodeDisplayLabel = m_StatisticPanel?.Q<Label>("CodeDisplayLabel");
        m_CopyCodeButton = m_StatisticPanel?.Q<Button>("CopyCodeButton");
        if (m_CopyCodeButton != null) m_CopyCodeButton.clicked += CopyCodeToClipboard;
        m_InputCodeField = m_StatisticPanel?.Q<TextField>("InputCodeField");
        if (m_InputCodeField != null) m_InputCodeField.Q("unity-text-input").style.color = Color.black;
        m_LoginCodeButton = m_StatisticPanel?.Q<Button>("LoginCodeButton");

        if (m_StatisticButton != null) m_StatisticButton.clicked += OpenStatisticPanel;
        if (m_CloseStatisticButton != null) m_CloseStatisticButton.clicked += CloseStatisticPanel;
        if (m_GenerateCodeButton != null) m_GenerateCodeButton.clicked += OnGenerateCodeClicked;
        if (m_LoginCodeButton != null) m_LoginCodeButton.clicked += OnLoginCodeClicked;

        if (m_StatisticPanel != null) m_StatisticPanel.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (m_MainMenuPanel != null && m_MainMenuPanel.style.visibility == Visibility.Visible)
            {
                if (m_ExitConfirmPanel != null)
                {
                    if (m_ExitConfirmPanel.style.display == DisplayStyle.None) ShowExitConfirmation();
                    else CancelExit();
                }
            }
            else if (m_MainMenuPanel.style.visibility == Visibility.Hidden &&
                     m_GameOverPanel.style.visibility == Visibility.Hidden &&
                     m_CharacterSelectionPanel.style.visibility == Visibility.Hidden &&
                     (m_StatisticPanel == null || m_StatisticPanel.style.display == DisplayStyle.None) &&
                     (m_LeaderboardPanel == null || m_LeaderboardPanel.style.display == DisplayStyle.None))
            {
                if (IsPaused) ResumeGame();
                else PauseGame();
            }
        }
    }

    private void CloseGameOverAndReturnToMenu()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Returned to Main Menu from Game Over screen."); // === BREADCRUMB ===

        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Hidden;
        m_DayLabel.style.visibility = Visibility.Hidden;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
        if (m_ContinueButton != null) m_ContinueButton.style.display = DisplayStyle.None;
        if (SoundManager.Instance != null) SoundManager.Instance.StopMusic();

        BoardManager.Clean();
        if (PlayerController != null) Destroy(PlayerController.gameObject);
    }

    private void PauseGame()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Paused Game (Pressed Escape)."); // === BREADCRUMB ===

        IsPaused = true;
        m_PauseMenuPanel.style.visibility = Visibility.Visible;
    }

    private void ResumeGame()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Resumed Game."); // === BREADCRUMB ===

        IsPaused = false;
        m_PauseMenuPanel.style.visibility = Visibility.Hidden;
    }

    private void ReturnToMainMenu()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Clicked 'Exit to Main Menu'."); // === BREADCRUMB ===

        SecurePrefs.SetInt("SavedDay", m_CurrentLevel);
        SecurePrefs.SetInt("SavedFood", m_FoodAmount);
        SecurePrefs.SetInt("SavedChar", SelectedCharacter);
        SecurePrefs.SetInt("HasSave", 1);

        if (PlayerController != null)
        {
            SecurePrefs.SetInt("PlayerX", PlayerController.Cell.x);
            SecurePrefs.SetInt("PlayerY", PlayerController.Cell.y);
        }
        string mapJson = BoardManager.SaveMap();
        SecurePrefs.SetString("SavedMap", mapJson);
        SecurePrefs.Save();

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
        GlobalErrorHandler.AddBreadcrumb("UI: Clicked 'Play' Button."); // === BREADCRUMB ===

        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_CharacterSelectionPanel.style.visibility = Visibility.Visible;
    }

    private void CancelCharacterSelection()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Clicked 'Back' on Character Select."); // === BREADCRUMB ===

        m_CharacterSelectionPanel.style.visibility = Visibility.Hidden;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
    }

    private void ChooseCharacterAndPlay(int characterIndex)
    {
        string charName = characterIndex == 1 ? "Bob" : (characterIndex == 2 ? "Steve" : "Caso");
        GlobalErrorHandler.AddBreadcrumb($"UI: Chose Character ({charName})."); // === BREADCRUMB ===

        SelectedCharacter = characterIndex;
        m_CharacterSelectionPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Visible;
        m_DayLabel.style.visibility = Visibility.Visible;
        StartNewGame();
    }

    private void ContinueSavedGame()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Clicked 'Continue Game'."); // === BREADCRUMB ===

        m_CurrentLevel = SecurePrefs.GetInt("SavedDay");
        m_FoodAmount = SecurePrefs.GetInt("SavedFood");
        SelectedCharacter = SecurePrefs.GetInt("SavedChar");

        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_FoodLabel.style.visibility = Visibility.Visible;
        m_DayLabel.style.visibility = Visibility.Visible;

        m_FoodLabel.text = "Food : " + m_FoodAmount;
        if (m_DayLabel != null) m_DayLabel.text = "Day : " + m_CurrentLevel;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayRandomTrack();

        BoardManager.Clean();
        string savedMap = SecurePrefs.GetString("SavedMap");
        BoardManager.LoadMap(m_CurrentLevel, savedMap);

        if (CharacterPrefabs != null && CharacterPrefabs.Length > 0)
        {
            int index = SelectedCharacter - 1;
            PlayerController = Instantiate(CharacterPrefabs[index]);
            PlayerController.Init();
            SetFoodExact(m_FoodAmount);

            int pX = SecurePrefs.GetInt("PlayerX", 1);
            int pY = SecurePrefs.GetInt("PlayerY", 1);
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
                StatisticsManager.Instance.EndRun(30);
            }

            if (PlayerController != null) PlayerController.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "You are the winner. Thank you for play my video game";
            ClearSaveData();
            return;
        }

        BoardManager.Clean();
        BoardManager.Init(m_CurrentLevel);
        if (PlayerController != null) PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));

        AutoSaveGame();
    }

    void OnTurnHappen()
    {
        ChangeFood(-1);
        if (StatisticsManager.Instance != null) StatisticsManager.Instance.AddTurn();
    }

    public void ChangeFood(int amount)
    {
        m_FoodAmount += amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (StatisticsManager.Instance != null) StatisticsManager.Instance.UpdateHighestFood(m_FoodAmount);

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
        SecurePrefs.DeleteKey("HasSave");
        if (m_ContinueButton != null) m_ContinueButton.style.display = DisplayStyle.None;
        if (UGSManager.Instance != null) UGSManager.Instance.DeleteCloudSave();
    }

    public void ShowFloatingText(string message, Vector3 position, bool isDamage = true)
    {
        if (FloatingTextPrefab != null && ObjectPooler.Instance != null)
        {
            Vector3 spawnPos = position + new Vector3(0, 0.5f, -2f);

            GameObject go = ObjectPooler.Instance.SpawnFromPool(FloatingTextPrefab, spawnPos);

            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) ft.Setup(message, isDamage);
        }
    }

    private void OpenStatisticPanel()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Opened Statistic Panel."); // === BREADCRUMB ===

        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_StatisticPanel.style.display = DisplayStyle.Flex;

        var lifetime = StatisticsManager.Instance.GameStats.Lifetime;
        RunStats bestRun = lifetime.BestRun;
        string bestRunText = "You haven't beaten the game yet!";

        if (bestRun != null && bestRun.DaysSurvived >= 31)
        {
            bestRunText = $"The {bestRun.RunNumber} try: {bestRun.CharacterName} - Day {bestRun.DaysSurvived} | {bestRun.StepsTaken} Moves | {bestRun.TurnsTaken} Turns | {bestRun.MonstersKilled} Kills | {bestRun.WallsBroken} Walls";
        }

        m_StatsText.text =
            $"<color=#FACC15><b>--- PERSONAL BESTS (Single Run) ---</b></color>\n" +
            $"Greatest Attempt: <color=#FFFFFF>{bestRunText}</color>\n" +
            $"Longest Survival: <color=#FFFFFF>Day {lifetime.HighestDaySurvived}</color>\n" +
            $"Highest Food: <color=#FFFFFF>{lifetime.HighestFoodHeld}</color> | Most Kills: <color=#FFFFFF>{lifetime.MostMonstersKilledInOneRun}</color> | Most Mined: <color=#FFFFFF>{lifetime.MostWallsBrokenInOneRun}</color>\n\n" +

            $"<color=#38BDF8><b>--- LIFETIME TOTALS ---</b></color>\n" +
            $"Total Runs: <color=#FFFFFF>{lifetime.TotalRuns}</color> | Wins: <color=#FFFFFF>{lifetime.TotalWins}</color> | Steps: <color=#FFFFFF>{lifetime.TotalSteps}</color>\n\n" +

            $"<color=#4ADE80><b>--- FOOD CONSUMED (Total: {lifetime.TotalFoodEaten}) ---</b></color>\n" +
            $"Fruit: {lifetime.FruitEaten} | Soda: {lifetime.SodaDrank} | Burger: {lifetime.BurgersEaten}\n" +
            $"Chicken: {lifetime.ChickenEaten} | Fish: {lifetime.FishEaten} | Salad: {lifetime.SaladEaten}\n\n" +

            $"<color=#A78BFA><b>--- WALLS DESTROYED (Total: {lifetime.TotalWallsBroken}) ---</b></color>\n" +
            $"Dirt: {lifetime.NormalWallBroken} | Dirt V2: {lifetime.WallType2Broken} | Dirt V3: {lifetime.WallType3Broken}\n" +
            $"Cactus: {lifetime.CactusBroken} | Cactus V2: {lifetime.CactusType2Broken} | Cactus V3: {lifetime.CactusType3Broken}\n" +
            $"Pink Rock: {lifetime.PinkRockBroken}\n\n" +

            $"<color=#F87171><b>--- MONSTERS DEFEATED (Total: {lifetime.TotalMonstersKilled}) ---</b></color>\n" +
            $"Zombie: {lifetime.ZombieKilled} | Elite: {lifetime.EliteZombieKilled} | Mummy: {lifetime.MummyKilled}\n" +
            $"Fly-GH28: {lifetime.FlyGH28Killed} | Slime: {lifetime.SlimeKilled} | Mutant: {lifetime.MutantSlimeKilled}\n\n" +

            $"Damage Taken: <color=#FF8A65>{lifetime.TotalHitsTaken}</color>";

        if (m_CodeDisplayLabel != null)
        {
            string savedCode = SecurePrefs.GetString("TransferCode", "");
            if (!string.IsNullOrEmpty(savedCode))
            {
                m_CodeDisplayLabel.text = "Your Code: " + savedCode;
                m_GeneratedCode = savedCode;
            }
            else m_CodeDisplayLabel.text = "Your Code: Not Generated";
        }

        if (m_CopyCodeButton != null) m_CopyCodeButton.text = "Copy Code";
    }

    private void CloseStatisticPanel()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Closed Statistic Panel."); // === BREADCRUMB ===

        m_StatisticPanel.style.display = DisplayStyle.None;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
    }

    private async void OnGenerateCodeClicked()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Clicked 'Generate Code'."); // === BREADCRUMB ===

        if (UGSManager.Instance != null && m_CodeDisplayLabel != null)
        {
            m_CodeDisplayLabel.text = "Generating...";
            m_GeneratedCode = await UGSManager.Instance.GenerateTransferCode();
            m_CodeDisplayLabel.text = "Your Code: " + m_GeneratedCode;
            if (m_CopyCodeButton != null) m_CopyCodeButton.text = "Copy Code";
        }
    }

    private void CopyCodeToClipboard()
    {
        if (!string.IsNullOrEmpty(m_GeneratedCode))
        {
            GUIUtility.systemCopyBuffer = m_GeneratedCode;
            if (m_CopyCodeButton != null) m_CopyCodeButton.text = "Copied!";
        }
    }

    private void OnLoginCodeClicked()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Clicked 'Login with Code'."); // === BREADCRUMB ===

        if (UGSManager.Instance != null && m_InputCodeField != null)
        {
            string codeToUse = m_InputCodeField.value.Trim();
            if (!string.IsNullOrEmpty(codeToUse)) UGSManager.Instance.LoginWithTransferCode(codeToUse);
        }
    }

    public void ShowKickPopup()
    {
        IsPaused = true;
        if (m_PauseMenuPanel != null) m_PauseMenuPanel.style.visibility = Visibility.Hidden;
        if (m_StatisticPanel != null) m_StatisticPanel.style.display = DisplayStyle.None;
        if (m_GameOverPanel != null) m_GameOverPanel.style.visibility = Visibility.Hidden;
        if (m_CharacterSelectionPanel != null) m_CharacterSelectionPanel.style.visibility = Visibility.Hidden;
        if (m_KickPopupPanel != null) m_KickPopupPanel.style.display = DisplayStyle.Flex;
    }

    private void OnKickOkClicked()
    {
        if (UGSManager.Instance != null) UGSManager.Instance.ExecuteKickAndReload();
    }

    public void AutoSaveGame()
    {
        SecurePrefs.SetInt("SavedDay", m_CurrentLevel);
        SecurePrefs.SetInt("SavedFood", m_FoodAmount);
        SecurePrefs.SetInt("SavedChar", SelectedCharacter);
        SecurePrefs.SetInt("HasSave", 1);

        if (PlayerController != null)
        {
            SecurePrefs.SetInt("PlayerX", PlayerController.Cell.x);
            SecurePrefs.SetInt("PlayerY", PlayerController.Cell.y);
        }

        string mapJson = BoardManager.SaveMap();
        SecurePrefs.SetString("SavedMap", mapJson);
        SecurePrefs.Save();

        if (UGSManager.Instance != null) UGSManager.Instance.SyncLocalToCloud();
    }

    private void OpenLeaderboard()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Opened Leaderboard."); // === BREADCRUMB ===

        m_MainMenuPanel.style.visibility = Visibility.Hidden;
        m_LeaderboardPanel.style.display = DisplayStyle.Flex;
        if (UGSManager.Instance != null && m_LeaderboardContainer != null) _ = UGSManager.Instance.PopulateLeaderboardUI(m_LeaderboardContainer);
    }

    private void ReloadLeaderboard()
    {
        if (UGSManager.Instance != null && m_LeaderboardContainer != null) _ = UGSManager.Instance.PopulateLeaderboardUI(m_LeaderboardContainer);
    }

    private void CloseLeaderboard()
    {
        GlobalErrorHandler.AddBreadcrumb("UI: Closed Leaderboard."); // === BREADCRUMB ===

        m_LeaderboardPanel.style.display = DisplayStyle.None;
        m_MainMenuPanel.style.visibility = Visibility.Visible;
    }

    private void ShowExitConfirmation()
    {
        if (m_ExitConfirmPanel != null) m_ExitConfirmPanel.style.display = DisplayStyle.Flex;
    }

    private void CancelExit()
    {
        if (m_ExitConfirmPanel != null) m_ExitConfirmPanel.style.display = DisplayStyle.None;
    }

    private void ExecuteExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}