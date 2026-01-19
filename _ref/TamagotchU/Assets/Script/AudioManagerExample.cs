using UnityEngine;

public class AudioManagerExample : MonoBehaviour
{
    public static AudioManagerExample Instance;

    [Header("Overlapping Sound Settings")]
    public AudioSource sfxSource;       // One source for all overlapping SFX
    public AudioClip[] sfxClips;        // Assign your overlapping sound clips

    [Header("Threshold-Triggered Sound")]
    public AudioSource eventSource;     // Separate source for event sound
    public AudioClip thresholdClip;     // Clip that plays when A > threshold
    public float threshold = 0.6f;      // Trigger value

    private bool hasEventPlayed = false;

    // For example demonstration
    public float A = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Play a short sound effect by index. Uses PlayOneShot so clips can overlap.
    /// </summary>
    public void PlaySFX(int index, float volume = 1f)
    {
        if (index >= 0 && index < sfxClips.Length && sfxClips[index] != null)
        {
            sfxSource.PlayOneShot(sfxClips[index], volume);
        }
    }

    public void PlayThresholdSound(float A)
    {
        if (A > threshold && !hasEventPlayed)
        {
            eventSource.PlayOneShot(thresholdClip);
            hasEventPlayed = true;
        }
        else if (A <= threshold)
        {
            hasEventPlayed = false;
        }
    }

}
