using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance = null;

    public AudioSource efxSource;
    public AudioSource musicSource;

    public float lowPitchRange = 0.95f;
    public float highPitchRange = 1.05f;

    public AudioClip[] BackgroundTracks;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // === CHANGED: We leave this empty now so music doesn't play on the Main Menu! ===
    }

    void Update()
    {
        // If the song finishes, play a new random one!
        if (musicSource != null && !musicSource.isPlaying && BackgroundTracks.Length > 0)
        {
            PlayRandomTrack();
        }
    }

    // === CHANGED: Added 'public' so GameManager can trigger this ===
    public void PlayRandomTrack()
    {
        if (BackgroundTracks.Length == 0 || musicSource == null) return;

        int randomIndex = Random.Range(0, BackgroundTracks.Length);
        musicSource.clip = BackgroundTracks[randomIndex];
        musicSource.Play();
    }

    // === NEW: Added the missing StopMusic function! ===
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
    // ==================================================

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