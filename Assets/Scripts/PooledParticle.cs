using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour
{
    // This Unity function is automatically called when the Stop Action is set to "Callback"
    private void OnParticleSystemStopped()
    {
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}