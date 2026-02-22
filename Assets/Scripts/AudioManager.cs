using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer (For Sliders)")]
    [Tooltip("Expose parameters 'BgmVolume' and 'SfxVolume' in the mixer")]
    public AudioMixer masterMixer;

    [Header("Global Audio Sources")]
    [Tooltip("Used to play looping Background Music")]
    public AudioSource musicSource;
    private AudioSource secondaryMusicSource; // Created automatically at runtime for crossfading
    
    [Tooltip("Used to play OneShot SFX (like UI clicks)")]
    public AudioSource sfxSource;
    
    private Coroutine crossfadeRoutine;
    private float defaultBgmVolume = 1f;

    [Header("BGM Clips")]
    public AudioClip titleBGM;
    public AudioClip gameBGM;
    [Tooltip("Seamlessly replaces gameBGM during speed power-ups")]
    public AudioClip speedBGM;
    public AudioClip gameOverBGM;

    [Header("Game Over Sequencing")]
    [Tooltip("Plays immediately on death. The Game Over BGM will automatically wait for this to finish before looping.")]
    public AudioClip gameOverSFX;

    [Header("UI SFX")]
    public AudioClip buttonClickSFX;
    public AudioClip pauseSFX;

    private const string BGM_VOL_KEY = "BGMVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep audio playing across scenes
            
            // Create a secondary music source to handle crossfading
            if (musicSource != null)
            {
                defaultBgmVolume = musicSource.volume;
                secondaryMusicSource = gameObject.AddComponent<AudioSource>();
                secondaryMusicSource.outputAudioMixerGroup = musicSource.outputAudioMixerGroup;
                secondaryMusicSource.loop = musicSource.loop;
                secondaryMusicSource.playOnAwake = false;
                secondaryMusicSource.volume = 0f;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Apply saved volumes on start
        SetBgmVolume(PlayerPrefs.GetFloat(BGM_VOL_KEY, 0.75f));
        SetSfxVolume(PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f));
    }

    public void PlayMusic(AudioClip clip)
    {
        CrossfadeTo(clip, false);
    }

    /// <summary>
    /// Swaps the current playing music with a variation, syncing to the exact same second/beat of the track, WITH crossfade.
    /// Perfect for dynamic music layers like getting a PowerUp!
    /// </summary>
    public void PlayMusicSynced(AudioClip newClip)
    {
        CrossfadeTo(newClip, true);
    }

    private void CrossfadeTo(AudioClip newClip, bool syncTime)
    {
        if (musicSource == null || newClip == null) return;

        // Determine which source is currently playing at full volume
        AudioSource activeSource = (musicSource.volume > 0.01f) ? musicSource : secondaryMusicSource;
        if (activeSource == null) activeSource = musicSource;

        if (activeSource.clip == newClip && activeSource.isPlaying) return;

        AudioSource fadingOut = activeSource;
        AudioSource fadingIn = (activeSource == musicSource) ? secondaryMusicSource : musicSource;

        float currentTime = fadingOut.isPlaying ? fadingOut.time : 0f;

        fadingIn.clip = newClip;
        fadingIn.Play();

        if (syncTime && currentTime > 0f && currentTime < newClip.length)
        {
            fadingIn.time = currentTime;
        }
        else
        {
            fadingIn.time = 0f; // Start over if not synced
        }

        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(CrossfadeFrames(fadingOut, fadingIn, 0.5f));
    }

    private System.Collections.IEnumerator CrossfadeFrames(AudioSource fadeOut, AudioSource fadeIn, float duration)
    {
        float t = 0f;
        float startVolOut = fadeOut.volume;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // Unscaled so we can still crossfade when timeScale = 0 (like paused)
            fadeOut.volume = Mathf.Lerp(startVolOut, 0f, t / duration);
            fadeIn.volume = Mathf.Lerp(0f, defaultBgmVolume, t / duration);
            yield return null;
        }

        fadeOut.volume = 0f;
        fadeOut.Stop();
        fadeIn.volume = defaultBgmVolume;
    }

    /// <summary>
    /// Called by CloudController to enter/exit the dynamic speed variation.
    /// </summary>
    public void SetSpeedBGMState(bool isSpeeding)
    {
        // Don't switch if we're dead/in-menu
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing) return;

        AudioClip targetClip = isSpeeding ? speedBGM : gameBGM;
        if (targetClip != null)
        {
            PlayMusicSynced(targetClip);
        }
    }

    /// <summary>
    /// Plays the Game Over failure stinger immediately, then waits for it to finish before fading in/playing the Game Over BGM.
    /// Runs in unscaled time since the game is paused.
    /// </summary>
    public void PlayGameOverSequence()
    {
        if (musicSource != null) musicSource.Stop();
        if (secondaryMusicSource != null) secondaryMusicSource.Stop(); // Cut all BGMs immediately
        
        float delay = 0f;
        if (sfxSource != null && gameOverSFX != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(gameOverSFX);
            delay = gameOverSFX.length; // Calculate exactly how long the death sound is
        }

        if (gameOverBGM != null)
        {
            StartCoroutine(PlayBGMAfterDelay(gameOverBGM, delay));
        }
    }

    private System.Collections.IEnumerator PlayBGMAfterDelay(AudioClip clip, float delay)
    {
        // Use Realtime so the delay works perfectly even when Time.timeScale = 0
        yield return new WaitForSecondsRealtime(delay);
        PlayMusic(clip);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.pitch = 1f; // Reset pitch to normal for standard sounds
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays an SFX with a randomized pitch to prevent repetitive sounds (e.g., raindrops, footsteps).
    /// </summary>
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.85f, float maxPitch = 1.15f)
    {
        if (sfxSource == null || clip == null) return;
        
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip);
    }

    // --- Volume Controls for sliders (Value 0.0001 to 1) ---
    public void SetBgmVolume(float sliderValue)
    {
        sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        PlayerPrefs.SetFloat(BGM_VOL_KEY, sliderValue);
        
        if (masterMixer != null)
        {
            // Convert linear slider 0-1 to logarithmic dB (-80 to 0)
            float db = Mathf.Log10(sliderValue) * 20f;
            masterMixer.SetFloat("BgmVolume", db);
        }
    }

    public void SetSfxVolume(float sliderValue)
    {
        sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        PlayerPrefs.SetFloat(SFX_VOL_KEY, sliderValue);

        if (masterMixer != null)
        {
            float db = Mathf.Log10(sliderValue) * 20f;
            masterMixer.SetFloat("SfxVolume", db);
        }
    }

    // --- Specific UI Hooks ---
    public void PlayUIButton() 
    { 
        if (buttonClickSFX != null) PlaySFX(buttonClickSFX); 
        else Debug.LogWarning("[AudioManager] No Button Click SFX assigned in Inspector!");
    }
    public void PlayPause() { PlaySFX(pauseSFX); }
}
