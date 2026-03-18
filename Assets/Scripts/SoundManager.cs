using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance = null;

    public AudioSource efxSource;
    public AudioSource musicSource;

    public float lowPitchRange = 0.95f;
    public float highPitchRange = 1.05f;

    // === NEW: We brought your background music list over here! ===
    public AudioClip[] BackgroundTracks;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Start playing music right when the game boots up
        PlayRandomTrack();
    }

    void Update()
    {
        // If the song finishes, play a new random one!
        if (musicSource != null && !musicSource.isPlaying && BackgroundTracks.Length > 0)
        {
            PlayRandomTrack();
        }
    }

    void PlayRandomTrack()
    {
        if (BackgroundTracks.Length == 0 || musicSource == null) return;

        int randomIndex = Random.Range(0, BackgroundTracks.Length);
        musicSource.clip = BackgroundTracks[randomIndex];
        musicSource.Play();
    }

    public void RandomizeSfx(params AudioClip[] clips)
    {
        if (clips.Length == 0 || efxSource == null) return;
        int randomIndex = Random.Range(0, clips.Length);
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        efxSource.pitch = randomPitch;
        efxSource.clip = clips[randomIndex];
        efxSource.Play();
    }

    // === Volume Controls ===
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (efxSource != null) efxSource.volume = volume;
    }
}