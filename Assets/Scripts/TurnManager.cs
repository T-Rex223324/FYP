using UnityEngine;
public class TurnManager
{
    public event System.Action OnTick;

    private int m_TurnCount;

    public TurnManager()
    {
        m_TurnCount = 1;
    }

    public void Tick()
    {
        if (OnTick != null)
        {
            OnTick.Invoke();
        }
        m_TurnCount += 1;
        Debug.Log("Current turn count : " + m_TurnCount);
    }
}