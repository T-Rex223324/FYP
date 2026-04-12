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
        // Awake only gets components now! No destroying allowed!
        m_TextMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(string text, bool isDamage)
    {
        if (m_TextMesh == null) m_TextMesh = GetComponent<TextMeshPro>();

        m_TextMesh.text = text;
        m_TextMesh.color = isDamage ? DamageColor : HealColor;

        // === FIXED: Apply jitter AFTER the pooler has assigned the position! ===
        transform.localPosition += new Vector3(
            Random.Range(-RandomizeOffset.x, RandomizeOffset.x),
            Random.Range(-RandomizeOffset.y, RandomizeOffset.y), 0);
    }

    private void OnEnable()
    {
        if (m_TextMesh == null) m_TextMesh = GetComponent<TextMeshPro>();
        m_TextMesh.color = new Color(m_TextMesh.color.r, m_TextMesh.color.g, m_TextMesh.color.b, 1f);

        Invoke(nameof(HideAndReturn), DestroyTime);
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


    private void HideAndReturn()
    {
        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }
}