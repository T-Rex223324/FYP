using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

<<<<<<< HEAD
=======
// === CHANGED: Added a specific SaveWall struct to remember the exact wall type! ===
[System.Serializable]
public struct SavePos { public int x, y; }

[System.Serializable]
public struct SaveWall { public int x, y, typeIndex; }

[System.Serializable]
public class BoardSaveData
{
    public List<SaveWall> walls = new List<SaveWall>();
    public List<SavePos> smallFoods = new List<SavePos>();
    public List<SavePos> bigFoods = new List<SavePos>();
    public List<SavePos> enemies = new List<SavePos>();
    public List<SavePos> elites = new List<SavePos>();
    public SavePos exit;
}
// =================================================================================

>>>>>>> origin/matser
public class BoardManager : MonoBehaviour
{
    public class CellData
    {
        public bool Passable;
        public CellObject ContainedObject;
    }

    [Header("--- URBAN THEME (Days 1 - 10) ---")]
    public GameObject[] FoodPrefabs;
    public WallObject[] WallPrefabs;
    public GameObject EnemyPrefab;
    public GameObject EliteEnemyPrefab;
    public Tile[] GroundTiles;
    public Tile[] WallTiles;
<<<<<<< HEAD
    public ExitCellObject ExitCellPrefab; // <-- HERE IS THE URBAN EXIT
=======
    public ExitCellObject ExitCellPrefab;
>>>>>>> origin/matser

    [Header("--- SAND THEME (Days 11 - 20) ---")]
    public GameObject[] SandFoodPrefabs;
    public WallObject[] SandWallPrefabs;
    public GameObject SandEnemyPrefab;
    public GameObject SandEliteEnemyPrefab;
    public Tile[] SandGroundTiles;
    public Tile[] SandWallTiles;
<<<<<<< HEAD
    public ExitCellObject SandExitCellPrefab; // <-- HERE IS THE NEW SAND EXIT
=======
    public ExitCellObject SandExitCellPrefab;
>>>>>>> origin/matser

    [Header("--- SNOW THEME (Days 21 - 30+) ---")]
    public GameObject[] SnowFoodPrefabs;
    public WallObject[] SnowWallPrefabs;
    public GameObject SnowEnemyPrefab;
    public GameObject SnowEliteEnemyPrefab;
    public Tile[] SnowGroundTiles;
    public Tile[] SnowWallTiles;
<<<<<<< HEAD
    public ExitCellObject SnowExitCellPrefab; // <-- HERE IS THE NEW SNOW EXIT
=======
    public ExitCellObject SnowExitCellPrefab;
>>>>>>> origin/matser

    [Header("--- GENERAL SETTINGS ---")]
    public int MinFoodCount = 5;
    public int MaxFoodCount = 10;
    public PlayerController Player;
    public int Width;
    public int Height;

    private CellData[,] m_BoardData;
    private Tilemap m_Tilemap;
    private Grid m_Grid;
    private List<Vector2Int> m_EmptyCellsList;
    private int m_CurrentDayCycle;

<<<<<<< HEAD
    // These variables will temporarily hold whichever theme is currently active!
=======
>>>>>>> origin/matser
    private Tile[] m_ActiveGroundTiles;
    private Tile[] m_ActiveWallTiles;
    private WallObject[] m_ActiveWallPrefabs;
    private GameObject[] m_ActiveFoodPrefabs;
    private GameObject m_ActiveEnemyPrefab;
    private GameObject m_ActiveEliteEnemyPrefab;
<<<<<<< HEAD
    private ExitCellObject m_ActiveExitCellPrefab; // <-- THIS HOLDS THE CORRECT EXIT GATE!

    public void Init(int currentLevel)
    {
        // === THE MAGIC SWITCH: ASSIGN THEME BASED ON LEVEL ===
=======
    private ExitCellObject m_ActiveExitCellPrefab;

    private void SetupTheme(int currentLevel)
    {
>>>>>>> origin/matser
        if (currentLevel <= 10)
        {
            m_ActiveGroundTiles = GroundTiles;
            m_ActiveWallTiles = WallTiles;
            m_ActiveWallPrefabs = WallPrefabs;
            m_ActiveFoodPrefabs = FoodPrefabs;
            m_ActiveEnemyPrefab = EnemyPrefab;
            m_ActiveEliteEnemyPrefab = EliteEnemyPrefab;
<<<<<<< HEAD
            m_ActiveExitCellPrefab = ExitCellPrefab; // Sets Urban Exit
=======
            m_ActiveExitCellPrefab = ExitCellPrefab;
>>>>>>> origin/matser
        }
        else if (currentLevel <= 20)
        {
            m_ActiveGroundTiles = SandGroundTiles;
            m_ActiveWallTiles = SandWallTiles;
            m_ActiveWallPrefabs = SandWallPrefabs;
            m_ActiveFoodPrefabs = SandFoodPrefabs;
            m_ActiveEnemyPrefab = SandEnemyPrefab;
            m_ActiveEliteEnemyPrefab = SandEliteEnemyPrefab;
<<<<<<< HEAD
            m_ActiveExitCellPrefab = SandExitCellPrefab; // Sets Sand Exit
=======
            m_ActiveExitCellPrefab = SandExitCellPrefab;
>>>>>>> origin/matser
        }
        else
        {
            m_ActiveGroundTiles = SnowGroundTiles;
            m_ActiveWallTiles = SnowWallTiles;
            m_ActiveWallPrefabs = SnowWallPrefabs;
            m_ActiveFoodPrefabs = SnowFoodPrefabs;
            m_ActiveEnemyPrefab = SnowEnemyPrefab;
            m_ActiveEliteEnemyPrefab = SnowEliteEnemyPrefab;
<<<<<<< HEAD
            m_ActiveExitCellPrefab = SnowExitCellPrefab; // Sets Snow Exit
        }
        // =====================================================

        m_CurrentDayCycle = ((currentLevel - 1) % 10) + 1;

        int sizeIncrease = m_CurrentDayCycle / 3;
        int dynamicSize = 8 + sizeIncrease;

        Width = dynamicSize;
        Height = dynamicSize;
=======
            m_ActiveExitCellPrefab = SnowExitCellPrefab;
        }
    }

    public void Init(int currentLevel)
    {
        SetupTheme(currentLevel);

        m_CurrentDayCycle = ((currentLevel - 1) % 10) + 1;
        int sizeIncrease = m_CurrentDayCycle / 3;
        Width = 8 + sizeIncrease;
        Height = 8 + sizeIncrease;
>>>>>>> origin/matser

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
                    tile = m_ActiveWallTiles[Random.Range(0, m_ActiveWallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    tile = m_ActiveGroundTiles[Random.Range(0, m_ActiveGroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }
<<<<<<< HEAD

=======
>>>>>>> origin/matser
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        m_EmptyCellsList.Remove(new Vector2Int(1, 1));
<<<<<<< HEAD

        Vector2Int endCoord = new Vector2Int(Width - 2, Height - 2);

        // === HERE IS WHERE IT SPAWNS THE ACTIVE EXIT GATE ===
        AddObject(Instantiate(m_ActiveExitCellPrefab), endCoord);
        // ====================================================

=======
        Vector2Int endCoord = new Vector2Int(Width - 2, Height - 2);
        AddObject(Instantiate(m_ActiveExitCellPrefab), endCoord);
>>>>>>> origin/matser
        m_EmptyCellsList.Remove(endCoord);

        GenerateWall();
        GenerateFood();
        GenerateEnemy();

<<<<<<< HEAD
=======
        SetupCamera();
    }

    public string SaveMap()
    {
        BoardSaveData data = new BoardSaveData();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                CellData cell = m_BoardData[x, y];
                if (cell != null && cell.ContainedObject != null)
                {
                    SavePos p = new SavePos { x = x, y = y };

                    if (cell.ContainedObject.GetComponent<WallObject>() != null)
                    {
                        // === NEW: Figure out exactly WHICH wall prefab this is! ===
                        string wallName = cell.ContainedObject.gameObject.name.Replace("(Clone)", "").Trim();
                        int prefabIndex = 0; // Default to 0
                        for (int i = 0; i < m_ActiveWallPrefabs.Length; i++)
                        {
                            if (m_ActiveWallPrefabs[i].name == wallName)
                            {
                                prefabIndex = i;
                                break;
                            }
                        }

                        data.walls.Add(new SaveWall { x = x, y = y, typeIndex = prefabIndex });
                        // ===========================================================
                    }
                    else if (cell.ContainedObject.GetComponent<FoodObject>() != null)
                    {
                        if (cell.ContainedObject.gameObject.name.Contains("Big")) data.bigFoods.Add(p);
                        else data.smallFoods.Add(p);
                    }
                    else if (cell.ContainedObject.GetComponent<Enemy>() != null)
                    {
                        if (cell.ContainedObject.gameObject.name.Contains("Elite")) data.elites.Add(p);
                        else data.enemies.Add(p);
                    }
                    else if (cell.ContainedObject.GetComponent<ExitCellObject>() != null)
                        data.exit = p;
                }
            }
        }
        return JsonUtility.ToJson(data);
    }

    public void LoadMap(int currentLevel, string jsonMap)
    {
        SetupTheme(currentLevel);

        m_CurrentDayCycle = ((currentLevel - 1) % 10) + 1;
        int sizeIncrease = m_CurrentDayCycle / 3;
        Width = 8 + sizeIncrease;
        Height = 8 + sizeIncrease;

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
                    tile = m_ActiveWallTiles[Random.Range(0, m_ActiveWallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    tile = m_ActiveGroundTiles[Random.Range(0, m_ActiveGroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        BoardSaveData data = JsonUtility.FromJson<BoardSaveData>(jsonMap);

        // === CHANGED: Spawn the exact wall prefab that was saved! ===
        foreach (SaveWall w in data.walls)
        {
            // Safety check to ensure the index isn't out of bounds
            int safeIndex = Mathf.Clamp(w.typeIndex, 0, m_ActiveWallPrefabs.Length - 1);
            WallObject prefab = m_ActiveWallPrefabs[safeIndex];
            AddObject(Instantiate(prefab), new Vector2Int(w.x, w.y));
        }
        // ============================================================

        foreach (SavePos p in data.smallFoods)
        {
            AddObject(Instantiate(m_ActiveFoodPrefabs[0]).GetComponent<FoodObject>(), new Vector2Int(p.x, p.y));
        }

        foreach (SavePos p in data.bigFoods)
        {
            GameObject prefab = m_ActiveFoodPrefabs.Length > 1 ? m_ActiveFoodPrefabs[1] : m_ActiveFoodPrefabs[0];
            AddObject(Instantiate(prefab).GetComponent<FoodObject>(), new Vector2Int(p.x, p.y));
        }

        foreach (SavePos p in data.enemies)
        {
            AddObject(Instantiate(m_ActiveEnemyPrefab).GetComponent<Enemy>(), new Vector2Int(p.x, p.y));
        }

        foreach (SavePos p in data.elites)
        {
            AddObject(Instantiate(m_ActiveEliteEnemyPrefab).GetComponent<Enemy>(), new Vector2Int(p.x, p.y));
        }

        AddObject(Instantiate(m_ActiveExitCellPrefab), new Vector2Int(data.exit.x, data.exit.y));

        SetupCamera();
    }

    private void SetupCamera()
    {
>>>>>>> origin/matser
        float centerX = (Width - 1) / 2.0f;
        float centerY = (Height - 1) / 2.0f;
        Camera.main.transform.position = new Vector3(centerX, centerY, -10f);
        Camera.main.orthographicSize = (Height / 2.0f) + 2.0f;
    }

    public void Clean()
    {
        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Tilemap != null) m_Tilemap.ClearAllTiles();

        CellObject[] allObjects = FindObjectsByType<CellObject>(FindObjectsSortMode.None);
        foreach (CellObject obj in allObjects)
        {
            Destroy(obj.gameObject);
        }

        m_BoardData = null;
    }

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData GetCellData(Vector2Int cellIndex)
    {
<<<<<<< HEAD
        if (cellIndex.x < 0 || cellIndex.x >= Width || cellIndex.y < 0 || cellIndex.y >= Height)
        {
            return null;
        }
=======
        if (cellIndex.x < 0 || cellIndex.x >= Width || cellIndex.y < 0 || cellIndex.y >= Height) return null;
>>>>>>> origin/matser
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
        int sizeIncrease = m_CurrentDayCycle / 3;
        int foodCount = Random.Range(MinFoodCount + sizeIncrease, MaxFoodCount + sizeIncrease + 1);

        for (int i = 0; i < foodCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
<<<<<<< HEAD

            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);

            GameObject prefabToSpawn = m_ActiveFoodPrefabs[Random.Range(0, m_ActiveFoodPrefabs.Length)];

            GameObject newFood = Instantiate(prefabToSpawn);
=======
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);
            GameObject newFood = Instantiate(m_ActiveFoodPrefabs[Random.Range(0, m_ActiveFoodPrefabs.Length)]);
>>>>>>> origin/matser
            AddObject(newFood.GetComponent<FoodObject>(), coord);
        }
    }

    void GenerateWall()
    {
        int sizeIncrease = m_CurrentDayCycle / 3;
<<<<<<< HEAD
        int extraWalls = sizeIncrease * 2;
        int wallCount = Random.Range(6 + extraWalls, 10 + extraWalls);

        List<WallObject> allowedWalls = new List<WallObject>();

        if (m_ActiveWallPrefabs.Length >= 3)
        {
            if (m_CurrentDayCycle >= 1 && m_CurrentDayCycle <= 3)
            {
                allowedWalls.Add(m_ActiveWallPrefabs[0]);
            }
            else if (m_CurrentDayCycle >= 4 && m_CurrentDayCycle <= 7)
            {
                allowedWalls.Add(m_ActiveWallPrefabs[0]);
                allowedWalls.Add(m_ActiveWallPrefabs[1]);
            }
            else
            {
                allowedWalls.AddRange(m_ActiveWallPrefabs);
            }
        }
        else
        {
            allowedWalls.AddRange(m_ActiveWallPrefabs);
        }
=======
        int wallCount = Random.Range(6 + (sizeIncrease * 2), 10 + (sizeIncrease * 2));

        List<WallObject> allowedWalls = new List<WallObject>();
        if (m_ActiveWallPrefabs.Length >= 3)
        {
            if (m_CurrentDayCycle >= 1 && m_CurrentDayCycle <= 3) allowedWalls.Add(m_ActiveWallPrefabs[0]);
            else if (m_CurrentDayCycle >= 4 && m_CurrentDayCycle <= 7) { allowedWalls.Add(m_ActiveWallPrefabs[0]); allowedWalls.Add(m_ActiveWallPrefabs[1]); }
            else allowedWalls.AddRange(m_ActiveWallPrefabs);
        }
        else allowedWalls.AddRange(m_ActiveWallPrefabs);
>>>>>>> origin/matser

        for (int i = 0; i < wallCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
<<<<<<< HEAD

            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);

            WallObject prefab = allowedWalls[Random.Range(0, allowedWalls.Count)];
            WallObject newWall = Instantiate(prefab);
            AddObject(newWall, coord);
=======
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);
            AddObject(Instantiate(allowedWalls[Random.Range(0, allowedWalls.Count)]), coord);
>>>>>>> origin/matser
        }
    }

    void GenerateEnemy()
    {
<<<<<<< HEAD
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
        else
        {
            minEnemies = 2;
            maxEnemies = 4;
        }

        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        if (m_CurrentDayCycle == 10 && m_ActiveEliteEnemyPrefab != null)
        {
            if (m_EmptyCellsList.Count > 0)
            {
                int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
                Vector2Int coord = m_EmptyCellsList[randomIndex];
                m_EmptyCellsList.RemoveAt(randomIndex);

                GameObject newEliteObj = Instantiate(m_ActiveEliteEnemyPrefab);
                Enemy eliteEnemy = newEliteObj.GetComponent<Enemy>();

                if (eliteEnemy != null) AddObject(eliteEnemy, coord);
            }
=======
        int minEnemies = (m_CurrentDayCycle <= 3) ? 1 : (m_CurrentDayCycle <= 6 ? 1 : 2);
        int maxEnemies = (m_CurrentDayCycle == 1) ? 1 : (m_CurrentDayCycle <= 3 ? 2 : (m_CurrentDayCycle <= 6 ? 3 : 4));
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        if (m_CurrentDayCycle == 10 && m_ActiveEliteEnemyPrefab != null && m_EmptyCellsList.Count > 0)
        {
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);
            AddObject(Instantiate(m_ActiveEliteEnemyPrefab).GetComponent<Enemy>(), coord);
>>>>>>> origin/matser
        }

        for (int i = 0; i < enemyCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
<<<<<<< HEAD

            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);

            GameObject newEnemyObj = Instantiate(m_ActiveEnemyPrefab);
            Enemy newEnemy = newEnemyObj.GetComponent<Enemy>();

            if (newEnemy != null) AddObject(newEnemy, coord);
=======
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];
            m_EmptyCellsList.RemoveAt(randomIndex);
            AddObject(Instantiate(m_ActiveEnemyPrefab).GetComponent<Enemy>(), coord);
>>>>>>> origin/matser
        }
    }
}