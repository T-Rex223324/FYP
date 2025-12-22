// Import base Unity library
using UnityEngine;
// Import Unity library for Tilemaps
using UnityEngine.Tilemaps;
// Import System library for data structures like List
using System.Collections.Generic;


// Defines the BoardManager component
public class BoardManager : MonoBehaviour
{
    // This is a "nested class". It's a simple data container
    // used to store information *about* a single cell in the grid.
    public class CellData
    {
        // Is this cell passable (e.g., not a wall)?
        public bool Passable;
        // What GameObject is currently in this cell (e.g., food, player)?
        public CellObject ContainedObject;
    }

    public WallObject WallPrefab;

    // Public reference to the Food prefab (set in Inspector)
    public FoodObject FoodPrefab;
    // Public reference to the Player (set in Inspector)
    public PlayerController Player;

    // This is the logical representation of the game board.
    // It's a 2D array that stores a 'CellData' object for every (x, y) coordinate.
    private CellData[,] m_BoardData;

    // A private reference to the visual Tilemap component
    private Tilemap m_Tilemap;
    // A private reference to the Grid component (parent of Tilemap)
    private Grid m_Grid;
    // A list to store the coordinates of all cells that are empty (passable)
    private List<Vector2Int> m_EmptyCellsList;

    // Public variables to set the board size in the Inspector
    public int Width;
    public int Height;
    // Arrays of possible tiles to use for drawing (set in Inspector)
    public Tile[] GroundTiles;
    public Tile[] WallTiles;

    // The main initialization function for the board
    public void Init()
    {
        // Find the Tilemap and Grid components that are children of this GameObject
        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_Grid = GetComponentInChildren<Grid>();

        // Initialize the empty cell list and the 2D board data array
        m_EmptyCellsList = new List<Vector2Int>();
        m_BoardData = new CellData[Width, Height];

        // Loop through every 'y' coordinate (rows)
        for (int y = 0; y < Height; ++y)
        {
            // Loop through every 'x' coordinate (columns)
            for (int x = 0; x < Width; ++x)
            {
                Tile tile;
                // Create a new CellData object for this grid coordinate
                m_BoardData[x, y] = new CellData();

                // Check if this cell is on the outer edge (the border)
                if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                {
                    // If it's a border cell, pick a random Wall tile
                    tile = WallTiles[Random.Range(0, WallTiles.Length)];
                    // Mark this cell as NOT passable in our logical grid
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    // If it's an inner cell, pick a random Ground tile
                    tile = GroundTiles[Random.Range(0, GroundTiles.Length)];
                    // Mark this cell as passable
                    m_BoardData[x, y].Passable = true;
                    // Add this cell's coordinate to the list of empty cells
                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }

                // Tell the Tilemap to visually draw the chosen tile at this (x, y) position
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        // Manually remove cell (0, 0) from the empty list (this is a wall cell)
        m_EmptyCellsList.Remove(new Vector2Int(0, 0));

        GenerateWall();

        // After building the board, call the function to spawn food
        GenerateFood();
    }

    // A utility function to convert a grid coordinate (e.g., [2, 3])
    // into a 3D world position (e.g., [2.5, 3.5, 0])
    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        // Use the Grid component's built-in function for this
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    // A public "getter" function to safely get the CellData for a coordinate
    public CellData GetCellData(Vector2Int cellIndex)
    {

        // This is a "boundary check". It checks if the requested
        // coordinate is *outside* the valid dimensions of the board.
        if (cellIndex.x < 0 || cellIndex.x >= Width
            || cellIndex.y < 0 || cellIndex.y >= Height)
        {
            // If it's out of bounds, return null (nothing)
            return null;
        }

        // If it's in-bounds, return the data from our 2D array
        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    // This function spawns food on the board
    void GenerateFood()
    {
        // Set how many food items to create
        int foodCount = 5;
        // Loop 5 times
        for (int i = 0; i < foodCount; ++i)
        {
            // Pick a random index from our list of empty cells
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            // Get the (x, y) coordinate at that random index
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            // IMPORTANT: Remove that coordinate from the list
            // so we don't spawn two items in the same place.
            m_EmptyCellsList.RemoveAt(randomIndex);

            // Get the logical CellData for that coordinate
            CellData data = m_BoardData[coord.x, coord.y];

            // Create a new Food GameObject from the prefab
            FoodObject newFood = Instantiate(FoodPrefab);

            // Set the food's 3D world position
            newFood.transform.position = CellToWorld(coord);

            // Store a reference to this new food object in our logical grid
            data.ContainedObject = newFood;
        }
    }

    void GenerateWall()
    {
        int wallCount = Random.Range(6, 10);
        for (int i = 0; i < wallCount; ++i)
        {
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList.RemoveAt(randomIndex);
            CellData data = m_BoardData[coord.x, coord.y];
            WallObject newWall = Instantiate(WallPrefab);

            //init the wall
            newWall.Init(coord);

            newWall.transform.position = CellToWorld(coord);

            data.ContainedObject = newWall;
        }
    }
    public void SetCellTile(Vector2Int cellIndex, Tile tile)
    {
        m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
    }

}