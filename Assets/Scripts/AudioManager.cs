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
    [Tooltip("Used to play OneShot SFX (like UI clicks)")]
    public AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip titleBGM;
    public AudioClip gameBGM;
    public AudioClip gameOverBGM;

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
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip) return; // Don't interrupt if it's already playing the same song

        musicSource.clip = clip;
        musicSource.Play();
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
    public void PlayUIButton() { PlaySFX(buttonClickSFX); }
    public void PlayPause() { PlaySFX(pauseSFX); }
}
