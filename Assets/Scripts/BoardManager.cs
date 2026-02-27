using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public class CellData
    {
        public bool Passable;
        public CellObject ContainedObject;
    }

    public GameObject[] FoodPrefabs;

    public int MinFoodCount = 5;
    public int MaxFoodCount = 10;

    public WallObject[] WallPrefabs;
    public ExitCellObject ExitCellPrefab;
    public GameObject EnemyPrefab;
    public PlayerController Player;

    private CellData[,] m_BoardData;
    private Tilemap m_Tilemap;
    private Grid m_Grid;
    private List<Vector2Int> m_EmptyCellsList;

    // Keep track of where we are in the 1-10 loop
    private int m_CurrentDayCycle;

    public int Width;
    public int Height;
    public Tile[] GroundTiles;
    public Tile[] WallTiles;

    public void Init(int currentLevel)
    {
        // Calculate our current day in the 1 to 10 loop
        m_CurrentDayCycle = ((currentLevel - 1) % 10) + 1;

        // Increase size on Day 3, Day 6, and Day 9. Day 10 stays the same as 9.
        int sizeIncrease = m_CurrentDayCycle / 3;
        int dynamicSize = 8 + sizeIncrease;

        Width = dynamicSize;
        Height = dynamicSize;

        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_Grid = GetComponentInChildren<Grid>();
        m_EmptyCellsList = new List<Vector2Int>();
        m_BoardData = new CellData[Width, Height];

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                Tile tile;
                m_BoardData[x, y] = new CellData();

                if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                {
                    tile = WallTiles[Random.Range(0, WallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    tile = GroundTiles[Random.Range(0, GroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }

                m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        m_EmptyCellsList.Remove(new Vector2Int(1, 1));

        Vector2Int endCoord = new Vector2Int(Width - 2, Height - 2);
        AddObject(Instantiate(ExitCellPrefab), endCoord);
        m_EmptyCellsList.Remove(endCoord);

        GenerateWall();
        GenerateFood();
        GenerateEnemy();

        // Auto-Center Camera
        float centerX = (Width - 1) / 2.0f;
        float centerY = (Height - 1) / 2.0f;
        Camera.main.transform.position = new Vector3(centerX, centerY, -10f);
        Camera.main.orthographicSize = (Height / 2.0f) + 2.0f;
    }

    public void Clean()
    {
        if (m_BoardData == null) return;

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                var cellData = m_BoardData[x, y];
                if (cellData.ContainedObject != null)
                {
                    Destroy(cellData.ContainedObject.gameObject);
                }
                SetCellTile(new Vector2Int(x, y), null);
            }
        }
    }

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData GetCellData(Vector2Int cellIndex)
    {
        if (cellIndex.x < 0 || cellIndex.x >= Width || cellIndex.y < 0 || cellIndex.y >= Height)
        {
            return null;
        }
        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    public void SetCellTile(Vector2Int cellIndex, Tile tile)
    {
        m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
    }

    public Tile GetCellTile(Vector2Int cellIndex)
    {
        return m_Tilemap.GetTile<Tile>(new Vector3Int(cellIndex.x, cellIndex.y, 0));
    }

    void AddObject(CellObject obj, Vector2Int coord)
    {
        CellData data = m_BoardData[coord.x, coord.y];
        obj.transform.position = CellToWorld(coord);
        data.ContainedObject = obj;
        obj.Init(coord);
    }

    void GenerateFood()
    {
        // === CHANGED: Add a little extra food based on map size so it doesn't look empty ===
        int sizeIncrease = m_CurrentDayCycle / 3;
        int foodCount = Random.Range(MinFoodCount + sizeIncrease, MaxFoodCount + sizeIncrease + 1);

        for (int i = 0; i < foodCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;

            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);

            GameObject prefabToSpawn = FoodPrefabs[Random.Range(0, FoodPrefabs.Length)];

            GameObject newFood = Instantiate(prefabToSpawn);
            AddObject(newFood.GetComponent<FoodObject>(), coord);
        }
    }

    void GenerateWall()
    {
        // === CHANGED: Increase walls by 2 for every map size upgrade ===
        int sizeIncrease = m_CurrentDayCycle / 3;
        int extraWalls = sizeIncrease * 2;
        int wallCount = Random.Range(6 + extraWalls, 10 + extraWalls);

        List<WallObject> allowedWalls = new List<WallObject>();

        if (WallPrefabs.Length >= 3)
        {
            if (m_CurrentDayCycle >= 1 && m_CurrentDayCycle <= 3)
            {
                allowedWalls.Add(WallPrefabs[2]);
            }
            else if (m_CurrentDayCycle >= 4 && m_CurrentDayCycle <= 7)
            {
                allowedWalls.Add(WallPrefabs[0]);
                allowedWalls.Add(WallPrefabs[2]);
            }
            else
            {
                allowedWalls.AddRange(WallPrefabs);
            }
        }
        else
        {
            allowedWalls.AddRange(WallPrefabs);
        }

        for (int i = 0; i < wallCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;

            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);

            WallObject prefab = allowedWalls[Random.Range(0, allowedWalls.Count)];
            WallObject newWall = Instantiate(prefab);
            AddObject(newWall, coord);
        }
    }

    void GenerateEnemy()
    {
        // === CHANGED: Dynamic Enemy count based on your specific rules ===
        int minEnemies = 1;
        int maxEnemies = 1;

        if (m_CurrentDayCycle == 1)
        {
            minEnemies = 1;
            maxEnemies = 1;
        }
        else if (m_CurrentDayCycle >= 2 && m_CurrentDayCycle <= 3)
        {
            minEnemies = 1;
            maxEnemies = 2;
        }
        else if (m_CurrentDayCycle >= 4 && m_CurrentDayCycle <= 6)
        {
            minEnemies = 1;
            maxEnemies = 3;
        }
        else // Days 7 through 10
        {
            minEnemies = 2;
            maxEnemies = 4;
        }

        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        for (int i = 0; i < enemyCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;

            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);

            GameObject newEnemyObj = Instantiate(EnemyPrefab);
            Enemy newEnemy = newEnemyObj.GetComponent<Enemy>();

            if (newEnemy != null) AddObject(newEnemy, coord);
        }
    }
}