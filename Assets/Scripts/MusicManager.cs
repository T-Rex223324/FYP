using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    // This creates a list in the Unity Editor where you can drag your music files
    public AudioClip[] BackgroundTracks;

    private AudioSource m_AudioSource;

    void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();
        PlayRandomTrack();
    }

    void Update()
    {
        // If the current song finishes playing, pick a new random track!
        if (!m_AudioSource.isPlaying && BackgroundTracks.Length > 0)
        {
            PlayRandomTrack();
        }
    }

    void PlayRandomTrack()
    {
        if (BackgroundTracks.Length == 0) return;

        // Pick a random number between 0 and the amount of songs you have
        int randomIndex = Random.Range(0, BackgroundTracks.Length);

        // Put that random song into the Audio Source and play it
        m_AudioSource.clip = BackgroundTracks[randomIndex];
        m_AudioSource.Play();
    }
}