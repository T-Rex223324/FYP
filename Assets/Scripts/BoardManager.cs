using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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
    public ExitCellObject ExitCellPrefab;

    [Header("--- SAND THEME (Days 11 - 20) ---")]
    public GameObject[] SandFoodPrefabs;
    public WallObject[] SandWallPrefabs;
    public GameObject SandEnemyPrefab;
    public GameObject SandEliteEnemyPrefab;
    public Tile[] SandGroundTiles;
    public Tile[] SandWallTiles;
    public ExitCellObject SandExitCellPrefab;

    [Header("--- SNOW THEME (Days 21 - 30+) ---")]
    public GameObject[] SnowFoodPrefabs;
    public WallObject[] SnowWallPrefabs;
    public GameObject SnowEnemyPrefab;
    public GameObject SnowEliteEnemyPrefab;
    public Tile[] SnowGroundTiles;
    public Tile[] SnowWallTiles;
    public ExitCellObject SnowExitCellPrefab;

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

    private Tile[] m_ActiveGroundTiles;
    private Tile[] m_ActiveWallTiles;
    private WallObject[] m_ActiveWallPrefabs;
    private GameObject[] m_ActiveFoodPrefabs;
    private GameObject m_ActiveEnemyPrefab;
    private GameObject m_ActiveEliteEnemyPrefab;
    private ExitCellObject m_ActiveExitCellPrefab;

    private void SetupTheme(int currentLevel)
    {
        if (currentLevel <= 10)
        {
            m_ActiveGroundTiles = GroundTiles;
            m_ActiveWallTiles = WallTiles;
            m_ActiveWallPrefabs = WallPrefabs;
            m_ActiveFoodPrefabs = FoodPrefabs;
            m_ActiveEnemyPrefab = EnemyPrefab;
            m_ActiveEliteEnemyPrefab = EliteEnemyPrefab;
            m_ActiveExitCellPrefab = ExitCellPrefab;
        }
        else if (currentLevel <= 20)
        {
            m_ActiveGroundTiles = SandGroundTiles;
            m_ActiveWallTiles = SandWallTiles;
            m_ActiveWallPrefabs = SandWallPrefabs;
            m_ActiveFoodPrefabs = SandFoodPrefabs;
            m_ActiveEnemyPrefab = SandEnemyPrefab;
            m_ActiveEliteEnemyPrefab = SandEliteEnemyPrefab;
            m_ActiveExitCellPrefab = SandExitCellPrefab;
        }
        else
        {
            m_ActiveGroundTiles = SnowGroundTiles;
            m_ActiveWallTiles = SnowWallTiles;
            m_ActiveWallPrefabs = SnowWallPrefabs;
            m_ActiveFoodPrefabs = SnowFoodPrefabs;
            m_ActiveEnemyPrefab = SnowEnemyPrefab;
            m_ActiveEliteEnemyPrefab = SnowEliteEnemyPrefab;
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

        m_EmptyCellsList.Remove(new Vector2Int(1, 1));
        Vector2Int endCoord = new Vector2Int(Width - 2, Height - 2);

        ExitCellObject exitObj = ObjectPooler.Instance.SpawnFromPool(m_ActiveExitCellPrefab.gameObject, Vector3.zero).GetComponent<ExitCellObject>();
        exitObj.Type = CellObject.ObjectType.Exit;
        AddObject(exitObj, endCoord);

        m_EmptyCellsList.Remove(endCoord);

        GenerateWall();
        GenerateFood();
        GenerateEnemy();

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

                    CellObject.ObjectType type = cell.ContainedObject.Type;

                    if (type == CellObject.ObjectType.Wall) data.walls.Add(new SaveWall { x = x, y = y, typeIndex = cell.ContainedObject.PrefabIndex });
                    else if (type == CellObject.ObjectType.SmallFood) data.smallFoods.Add(p);
                    else if (type == CellObject.ObjectType.BigFood) data.bigFoods.Add(p);
                    else if (type == CellObject.ObjectType.Enemy) data.enemies.Add(p);
                    else if (type == CellObject.ObjectType.EliteEnemy) data.elites.Add(p);
                    else if (type == CellObject.ObjectType.Exit) data.exit = p;
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

        foreach (SaveWall w in data.walls)
        {
            int safeIndex = Mathf.Clamp(w.typeIndex, 0, m_ActiveWallPrefabs.Length - 1);
            WallObject wall = ObjectPooler.Instance.SpawnFromPool(m_ActiveWallPrefabs[safeIndex].gameObject, Vector3.zero).GetComponent<WallObject>();
            wall.Type = CellObject.ObjectType.Wall;
            wall.PrefabIndex = safeIndex;
            AddObject(wall, new Vector2Int(w.x, w.y));
            m_EmptyCellsList.Remove(new Vector2Int(w.x, w.y));
        }

        foreach (SavePos p in data.smallFoods)
        {
            CellObject f = ObjectPooler.Instance.SpawnFromPool(m_ActiveFoodPrefabs[0], Vector3.zero).GetComponent<CellObject>();
            f.Type = CellObject.ObjectType.SmallFood;
            AddObject(f, new Vector2Int(p.x, p.y));
            m_EmptyCellsList.Remove(new Vector2Int(p.x, p.y));
        }

        foreach (SavePos p in data.bigFoods)
        {
            GameObject prefab = m_ActiveFoodPrefabs.Length > 1 ? m_ActiveFoodPrefabs[1] : m_ActiveFoodPrefabs[0];
            CellObject f = ObjectPooler.Instance.SpawnFromPool(prefab, Vector3.zero).GetComponent<CellObject>();
            f.Type = CellObject.ObjectType.BigFood;
            AddObject(f, new Vector2Int(p.x, p.y));
            m_EmptyCellsList.Remove(new Vector2Int(p.x, p.y));
        }

        foreach (SavePos p in data.enemies)
        {
            CellObject e = ObjectPooler.Instance.SpawnFromPool(m_ActiveEnemyPrefab, Vector3.zero).GetComponent<CellObject>();
            e.Type = CellObject.ObjectType.Enemy;
            AddObject(e, new Vector2Int(p.x, p.y));
            m_EmptyCellsList.Remove(new Vector2Int(p.x, p.y));
        }

        foreach (SavePos p in data.elites)
        {
            CellObject e = ObjectPooler.Instance.SpawnFromPool(m_ActiveEliteEnemyPrefab, Vector3.zero).GetComponent<CellObject>();
            e.Type = CellObject.ObjectType.EliteEnemy;
            AddObject(e, new Vector2Int(p.x, p.y));
            m_EmptyCellsList.Remove(new Vector2Int(p.x, p.y));
        }

        ExitCellObject exit = ObjectPooler.Instance.SpawnFromPool(m_ActiveExitCellPrefab.gameObject, Vector3.zero).GetComponent<ExitCellObject>();
        exit.Type = CellObject.ObjectType.Exit;
        AddObject(exit, new Vector2Int(data.exit.x, data.exit.y));
        m_EmptyCellsList.Remove(new Vector2Int(data.exit.x, data.exit.y));

        SetupCamera();
    }

    private void SetupCamera()
    {
        float centerX = (Width - 1) / 2.0f;
        float centerY = (Height - 1) / 2.0f;
        Camera.main.transform.position = new Vector3(centerX, centerY, -10f);
        Camera.main.orthographicSize = (Height / 2.0f) + 2.0f;
    }

    public void Clean()
    {
        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Tilemap != null) m_Tilemap.ClearAllTiles();

        if (m_BoardData != null)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (m_BoardData[x, y] != null && m_BoardData[x, y].ContainedObject != null)
                    {
                        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(m_BoardData[x, y].ContainedObject.gameObject);
                        else Destroy(m_BoardData[x, y].ContainedObject.gameObject);
                    }
                }
            }
        }
        m_BoardData = null;
    }

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData GetCellData(Vector2Int cellIndex)
    {
        if (cellIndex.x < 0 || cellIndex.x >= Width || cellIndex.y < 0 || cellIndex.y >= Height) return null;
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

        // === THE FIX ===
        // I deleted "data.Passable = false;" from here! 
        // This makes sure the player's arrow keys actually work when bumping into things!
        // ===============

        obj.Init(coord);
    }

    void GenerateWall()
    {
        int sizeIncrease = m_CurrentDayCycle / 3;
        int wallCount = Random.Range(6 + (sizeIncrease * 2), 10 + (sizeIncrease * 2));

        List<WallObject> allowedWalls = new List<WallObject>();
        if (m_ActiveWallPrefabs.Length >= 3)
        {
            if (m_CurrentDayCycle >= 1 && m_CurrentDayCycle <= 3) allowedWalls.Add(m_ActiveWallPrefabs[0]);
            else if (m_CurrentDayCycle >= 4 && m_CurrentDayCycle <= 7) { allowedWalls.Add(m_ActiveWallPrefabs[0]); allowedWalls.Add(m_ActiveWallPrefabs[1]); }
            else allowedWalls.AddRange(m_ActiveWallPrefabs);
        }
        else allowedWalls.AddRange(m_ActiveWallPrefabs);

        for (int i = 0; i < wallCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList[randomIndex] = m_EmptyCellsList[m_EmptyCellsList.Count - 1];
            m_EmptyCellsList.RemoveAt(m_EmptyCellsList.Count - 1);

            int randomAllowedIndex = Random.Range(0, allowedWalls.Count);
            WallObject wallPrefab = allowedWalls[randomAllowedIndex];

            int trueIndex = System.Array.IndexOf(m_ActiveWallPrefabs, wallPrefab);
            if (trueIndex == -1) trueIndex = 0;

            WallObject newWall = ObjectPooler.Instance.SpawnFromPool(wallPrefab.gameObject, Vector3.zero).GetComponent<WallObject>();
            newWall.Type = CellObject.ObjectType.Wall;
            newWall.PrefabIndex = trueIndex;

            AddObject(newWall, coord);
        }
    }

    void GenerateFood()
    {
        int sizeIncrease = m_CurrentDayCycle / 3;
        int foodCount = Random.Range(MinFoodCount + sizeIncrease, MaxFoodCount + sizeIncrease + 1);

        for (int i = 0; i < foodCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList[randomIndex] = m_EmptyCellsList[m_EmptyCellsList.Count - 1];
            m_EmptyCellsList.RemoveAt(m_EmptyCellsList.Count - 1);

            int prefabIndex = Random.Range(0, m_ActiveFoodPrefabs.Length);
            GameObject newFood = ObjectPooler.Instance.SpawnFromPool(m_ActiveFoodPrefabs[prefabIndex], Vector3.zero);
            CellObject foodObj = newFood.GetComponent<CellObject>();

            foodObj.Type = m_ActiveFoodPrefabs[prefabIndex].name.Contains("Big") ? CellObject.ObjectType.BigFood : CellObject.ObjectType.SmallFood;
            AddObject(foodObj, coord);
        }
    }

    void GenerateEnemy()
    {
        int minEnemies = (m_CurrentDayCycle <= 3) ? 1 : (m_CurrentDayCycle <= 6 ? 1 : 2);
        int maxEnemies = (m_CurrentDayCycle == 1) ? 1 : (m_CurrentDayCycle <= 3 ? 2 : (m_CurrentDayCycle <= 6 ? 3 : 4));
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        if (m_CurrentDayCycle == 10 && m_ActiveEliteEnemyPrefab != null && m_EmptyCellsList.Count > 0)
        {
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList[randomIndex] = m_EmptyCellsList[m_EmptyCellsList.Count - 1];
            m_EmptyCellsList.RemoveAt(m_EmptyCellsList.Count - 1);

            CellObject elite = ObjectPooler.Instance.SpawnFromPool(m_ActiveEliteEnemyPrefab, Vector3.zero).GetComponent<CellObject>();
            elite.Type = CellObject.ObjectType.EliteEnemy;
            AddObject(elite, coord);
        }

        for (int i = 0; i < enemyCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList[randomIndex] = m_EmptyCellsList[m_EmptyCellsList.Count - 1];
            m_EmptyCellsList.RemoveAt(m_EmptyCellsList.Count - 1);

            CellObject normal = ObjectPooler.Instance.SpawnFromPool(m_ActiveEnemyPrefab, Vector3.zero).GetComponent<CellObject>();
            normal.Type = CellObject.ObjectType.Enemy;
            AddObject(normal, coord);
        }
    }
}