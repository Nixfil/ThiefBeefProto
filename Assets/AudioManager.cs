using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Boomerang SFX")]
    public AudioClip boomerangSwoosh;
    public AudioSource swooshSource;

    [Header("SFX Source")]
    public AudioSource sfxSource;
    public AudioClip Fall;
    public AudioClip Throw;
    public AudioClip CancelThrow;

    private Coroutine swooshRoutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Play a one-shot sound effect through the SFX AudioSource.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Starts looping the boomerang swoosh sound for the given duration.
    /// </summary>
    public void PlayBoomerangLoop(float throwDuration, float pitch = 1f)
    {
        if (boomerangSwoosh == null || swooshSource == null) return;

        if (swooshRoutine != null)
            StopCoroutine(swooshRoutine);

        swooshRoutine = StartCoroutine(PlayLoopForDuration(throwDuration, pitch));
    }

    /// <summary>
    /// Stops the boomerang swoosh loop immediately.
    /// </summary>
    public void StopBoomerangLoop()
    {
        if (swooshRoutine != null)
            StopCoroutine(swooshRoutine);

        swooshSource.Stop();
        swooshRoutine = null;
    }

    private IEnumerator PlayLoopForDuration(float duration, float pitch)
    {
        swooshSource.clip = boomerangSwoosh;
        swooshSource.pitch = pitch;
        swooshSource.loop = false;

        float swooshLength = boomerangSwoosh.length / pitch;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            swooshSource.Play();
            yield return new WaitForSeconds(swooshLength);
            timeElapsed += swooshLength;
        }

        swooshRoutine = null;
    }
}
