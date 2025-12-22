// Import base Unity library
using UnityEngine;
// Import System library (used for 'Action')
using System;
// Import Unity library for UI Toolkit (UI Documents)
using UnityEngine.UIElements;



// Defines the main GameManager component
public class GameManager : MonoBehaviour
{





    // A variable to store the player's food amount
    private int m_FoodAmount = 100;
    // A public reference to the UIDocument in the scene
    public UIDocument UIDoc;
    // A private reference to the UI 'Label' element for food
    private Label m_FoodLabel;
    // A public reference to the Food prefab (a template GameObject)
    //public GameObject FoodPrefab;








    // This is the "Singleton" pattern.
    // 'Instance' is a static variable, meaning it's shared by all copies
    // of GameManager. It allows other scripts to easily access this
    // specific GameManager by writing 'GameManager.Instance'.
    public static GameManager Instance { get; private set; }

    // Public reference to the BoardManager in the scene (set in Unity Inspector)
    public BoardManager BoardManager;
    // Public reference to the PlayerController in the scene (set in Unity Inspector)
    public PlayerController PlayerController;

    // A public property to hold the TurnManager instance
    // Other scripts can 'get' it, but only this script can 'set' it.
    public TurnManager TurnManager { get; private set; }

    // Awake is called by Unity before Start(), when the object is first created.
    // It's used to set up the Singleton.







    private void Awake()
    {
        // Check if an 'Instance' of GameManager *already* exists
        if (Instance != null)
        {
            // If yes, destroy this *new* one. We only want one.
            Destroy(gameObject);
            return;
        }

        // If no Instance exists, this one becomes the *official* Instance.
        Instance = this;
        // Create a new TurnManager object.
        TurnManager = new TurnManager();
    }

    /*void Start()
    {
        

        BoardManager.Init();
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }*/






    // Start is called by Unity after Awake(), just before the first frame.
    void Start()
    {
        // Find the UI element named "FoodLabel" inside the UIDocument
        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        // Set the initial text of the food label
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        // Create a new TurnManager object (this line is redundant,
        // as one was already created in Awake())
        //TurnManager = new TurnManager();

        // This is "event subscription".
        // It tells the TurnManager: "When your 'OnTick' event happens,
        // please also call my 'OnTurnHappen' function."
        TurnManager.OnTick += OnTurnHappen;

        // Tell the BoardManager to run its initialization logic (create the grid)
        BoardManager.Init();
        // Tell the PlayerController to spawn onto the board at grid cell (1, 1)
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }

    // This function is an "event handler".
    // It is called *by the TurnManager* every time TurnManager.Tick() happens.





    void OnTurnHappen()
    {
        ChangeFood(-1);
    }
    public void ChangeFood(int amount)
    {
        m_FoodAmount += amount;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
    }
}