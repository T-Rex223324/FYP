using UnityEngine;
using TMPro; // This is required to talk to TextMeshPro!

public class FloatingText : MonoBehaviour
{
    public float DestroyTime = 1.0f;
    public float FloatSpeed = 1.5f;
    public Vector3 RandomizeOffset = new Vector3(0.5f, 0, 0);

    public Color DamageColor = Color.red;
    public Color HealColor = Color.green;

    private TextMeshPro m_TextMesh;

    void Awake()
    {
        m_TextMesh = GetComponent<TextMeshPro>();

        // Give it a little random jitter so numbers don't perfectly overlap
        transform.localPosition += new Vector3(
            Random.Range(-RandomizeOffset.x, RandomizeOffset.x),
            Random.Range(-RandomizeOffset.y, RandomizeOffset.y),
            0);

        // Tell Unity to destroy this object after 'DestroyTime' seconds
        Destroy(gameObject, DestroyTime);
    }

    public void Setup(string text, bool isDamage)
    {
        if (m_TextMesh == null) m_TextMesh = GetComponent<TextMeshPro>();

        m_TextMesh.text = text;
        m_TextMesh.color = isDamage ? DamageColor : HealColor;
    }

    void Update()
    {
        // 1. Float upwards
        transform.position += new Vector3(0, FloatSpeed * Time.deltaTime, 0);

        // 2. Fade out smoothly
        if (m_TextMesh != null)
        {
            Color c = m_TextMesh.color;
            c.a -= (1f / DestroyTime) * Time.deltaTime; // Reduce the alpha (transparency)
            m_TextMesh.color = c;
        }
    }
}