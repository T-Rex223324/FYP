using UnityEngine;
using System;
using UnityEngine.UIElements;



public class GameManager : MonoBehaviour
{
    private int m_FoodAmount = 100;
    public UIDocument UIDoc;
    private Label m_FoodLabel;
    public GameObject FoodPrefab;
    public static GameManager Instance { get; private set; }

    public BoardManager BoardManager;
    public PlayerController PlayerController;

    public TurnManager TurnManager { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TurnManager = new TurnManager();
    }

    /*void Start()
    {
        

        BoardManager.Init();
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }*/

    void Start()
    {
        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        m_FoodLabel.text = "Food : " + m_FoodAmount;
        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;
        BoardManager.Init();
        PlayerController.Spawn(BoardManager, new Vector2Int(1, 1));
    }

    void OnTurnHappen()
    {
        m_FoodAmount -= 1;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
    }


}
