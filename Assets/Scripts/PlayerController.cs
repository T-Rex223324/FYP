// Import the base Unity library
using UnityEngine;
// Import the new Unity Input System library
using UnityEngine.InputSystem;

// Define a new component called PlayerController
public class PlayerController : MonoBehaviour
{
    // A private reference to the BoardManager, which holds the grid data
    private BoardManager m_Board;
    // A private variable to store the player's current grid position (e.g., [1, 1])
    private Vector2Int m_CellPosition;

    // This is an initialization function, called by another script (like GameManager)
    // It "spawns" the player onto the board.
    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        // Store the reference to the board so we can use it later
        m_Board = boardManager;
        // Store the player's starting grid cell
        m_CellPosition = cell;

        // Move the player's actual GameObject (its 'transform') to the
        // real-world 3D position that corresponds to the grid cell.
        transform.position = m_Board.CellToWorld(cell);
    }

    // A public function that moves the player to a new grid cell
    public void MoveTo(Vector2Int cell)
    {
        // A safety check. If m_Board is null, it means Spawn() hasn't been called.
        if (m_Board == null)
        {
            // Log an error to the Unity console to help with debugging.
            Debug.LogError("PlayerController: m_Board is null in MoveTo! Did you forget to call Spawn?");
            // Stop running this function to avoid more errors.
            return;
        }

        // Update the player's stored grid position
        m_CellPosition = cell;
        // Move the player's GameObject to the new cell's world position
        transform.position = m_Board.CellToWorld(m_CellPosition);
    }



    /*private void Update()
    {
        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x -= 1;
            hasMoved = true;
        }

        if (hasMoved)
        {
            //check if the new position is passable, then move there if it is.
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            if (cellData != null && cellData.Passable)
            {
                MoveTo(newCellTarget);
            }
        }
    }*/

    /*private void Update()
    {
        if (m_Board == null)
        {
            Debug.LogError("PlayerController: m_Board is null! Did you forget to call Spawn?");
            return;
        }

        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x -= 1;
            hasMoved = true;
        }

        if (hasMoved)
        {
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.TurnManager.Tick();
                MoveTo(newCellTarget);
            }
        }
    }*/

    // Update is called by Unity once every frame
    private void Update()
    {
        // A "guard clause". If m_Board is not set (Spawn() wasn't called),
        // stop running Update immediately.
        if (m_Board == null)
            return; // wait until Spawn() sets it

        // Start by assuming the target cell is the one we're already on
        Vector2Int newCellTarget = m_CellPosition;
        // A flag to track if the player has tried to move this frame
        bool hasMoved = false;

        // Check if the 'Up Arrow' key was pressed *this exact frame*
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            // If so, set the target to one cell up
            newCellTarget.y += 1;
            hasMoved = true;
        }
        // Check 'Down Arrow'
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        // Check 'Right Arrow'
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        // Check 'Left Arrow'
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x -= 1;
            hasMoved = true;
        }

        /*if (hasMoved)
        {
            //check if the new position is passable, then move there if it is.
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.TurnManager.Tick();
                MoveTo(newCellTarget);
            }
        }*/

        // If the player pressed a movement key...
        if (hasMoved)
        {
            // ...ask the BoardManager for the data of the *target* cell.
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            // Check if the cell data is valid (not null, meaning not off-grid)
            // AND if the cell is marked as 'Passable' (not a wall)
            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.TurnManager.Tick();

                if (cellData.ContainedObject == null)
                {
                    MoveTo(newCellTarget);
                }
                else if (cellData.ContainedObject.PlayerWantsToEnter())
                {
                    MoveTo(newCellTarget);
                    //Call PlayerEntered AFTER moving the player! Otherwise not in cell yet
                    cellData.ContainedObject.PlayerEntered();
                }
            }
        }



    }

}