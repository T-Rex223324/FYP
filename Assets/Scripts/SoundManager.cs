using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Allows other scripts to easily call the SoundManager
    public static SoundManager Instance = null;

    // The speaker that will play the sound effects
    public AudioSource efxSource;

    // Pitch variations to make sounds feel dynamic
    public float lowPitchRange = 0.95f;
    public float highPitchRange = 1.05f;

    void Awake()
    {
        // Setup the Singleton so there is only ever ONE SoundManager
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Plays a random sound from a list of clips and slightly randomizes the pitch
    public void RandomizeSfx(params AudioClip[] clips)
    {
        if (clips.Length == 0) return;

        // Pick a random clip
        int randomIndex = Random.Range(0, clips.Length);

        // Pick a random pitch
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);

        // Apply pitch, assign clip, and play!
        efxSource.pitch = randomPitch;
        efxSource.clip = clips[randomIndex];
        efxSource.Play();
    }
}