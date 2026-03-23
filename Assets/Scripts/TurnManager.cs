// Import base Unity library
using UnityEngine;

// This is a "plain old C# object" (POCO).
// It does *not* inherit from MonoBehaviour, so it cannot be
// attached to a GameObject. It's just a class for managing logic.
public class TurnManager
{
    // This is an "event". It's a broadcast system.
    // Other scripts (like GameManager) can "subscribe" to this event.
    // 'System.Action' means it's an event that doesn't send any data.
    public event System.Action OnTick;

    // A private variable to count the number of turns
    private int m_TurnCount;

    // This is the "constructor". It's a special function that
    // is called automatically when a 'new TurnManager()' is created.
    public TurnManager()
    {
        // Initialize the turn count to 1.
        m_TurnCount = 1;
    }

    // This is the main public function of this class.
    // Other scripts (like PlayerController) call this to advance the turn.
    public void Tick()
    {
        // Check if any other script is currently subscribed to the OnTick event
        if (OnTick != null)
        {
            // If yes, "invoke" or "fire" the event.
            // This will call all subscribed functions (like GameManager.OnTurnHappen).
            OnTick.Invoke();
        }

        // Increment the turn counter
        m_TurnCount += 1;
        // Print the new turn count to the Unity Console for debugging
        Debug.Log("Current turn count : " + m_TurnCount);
    }
}