using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbienceController : MonoBehaviour
{
    [Header("Ambience Clips")]
    // Array to hold all your different environment sound clips
    public AudioClip[] ambienceClips;

    [Header("Playback Settings")]
    public float minDelay = 10f; // Minimum time before playing the next clip
    public float maxDelay = 20f; // Maximum time before playing the next clip

    private AudioSource audioSource;
    private float nextClipTime;

    void Start()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Set the initial time to play the first clip immediately
        nextClipTime = Time.time;

        // Ensure we have clips before continuing
        if (ambienceClips == null || ambienceClips.Length == 0)
        {
            Debug.LogError("AmbienceController is missing sound clips! Please assign them in the Inspector.");
            enabled = false; // Disable the script if no clips are present
        }
    }

    void Update()
    {
        // Check if it's time to play the next clip AND the AudioSource is currently not playing
        if (Time.time >= nextClipTime && !audioSource.isPlaying)
        {
            PlayRandomAmbience();
        }
    }

    private void PlayRandomAmbience()
    {
        // 1. Pick a random clip
        int randomIndex = Random.Range(0, ambienceClips.Length);
        AudioClip clipToPlay = ambienceClips[randomIndex];

        // 2. Play the clip
        audioSource.clip = clipToPlay;
        audioSource.Play();

        // 3. Determine the duration until the next clip starts
        // We wait for the current clip duration PLUS a random delay time
        float randomDelay = Random.Range(minDelay, maxDelay);
        
        // The next clip will start after the current clip is done playing AND the random delay has passed
        nextClipTime = Time.time + clipToPlay.length + randomDelay;
        
        Debug.Log($"Playing: {clipToPlay.name}. Next clip will play in approximately {clipToPlay.length + randomDelay:F2} seconds.");
    }
}