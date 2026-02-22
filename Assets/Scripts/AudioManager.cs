using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Expose parameters 'BgmVolume' and 'SfxVolume'")]
    public AudioMixer masterMixer;

    [Header("Global Audio Sources")]
    [Tooltip("Looping BGM")]
    public AudioSource musicSource;
    private AudioSource secondaryMusicSource;
    
    [Tooltip("OneShot SFX")]
    public AudioSource sfxSource;
    
    private Coroutine crossfadeRoutine;
    private float defaultBgmVolume = 1f;

    [Header("BGM Clips")]
    public AudioClip titleBGM;
    public AudioClip gameBGM;
    [Tooltip("Seamless variety during speed power-ups")]
    public AudioClip speedBGM;
    public AudioClip gameOverBGM;

    [Header("Game Over Sequencing")]
    [Tooltip("Plays immediately on death.")]
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
            DontDestroyOnLoad(gameObject);
            
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
        SetBgmVolume(PlayerPrefs.GetFloat(BGM_VOL_KEY, 0.75f));
        SetSfxVolume(PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f));
    }

    public void PlayMusic(AudioClip clip)
    {
        CrossfadeTo(clip, false);
    }

    // Swaps music variation with crossfade, syncing time if needed.
    public void PlayMusicSynced(AudioClip newClip)
    {
        CrossfadeTo(newClip, true);
    }

    private void CrossfadeTo(AudioClip newClip, bool syncTime)
    {
        if (musicSource == null || newClip == null) return;

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
            fadingIn.time = 0f;
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
            t += Time.unscaledDeltaTime;
            fadeOut.volume = Mathf.Lerp(startVolOut, 0f, t / duration);
            fadeIn.volume = Mathf.Lerp(0f, defaultBgmVolume, t / duration);
            yield return null;
        }

        fadeOut.volume = 0f;
        fadeOut.Stop();
        fadeIn.volume = defaultBgmVolume;
    }

    // Called by CloudController to enter/exit speed variation.
    public void SetSpeedBGMState(bool isSpeeding)
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing) return;

        AudioClip targetClip = isSpeeding ? speedBGM : gameBGM;
        if (targetClip != null)
        {
            PlayMusicSynced(targetClip);
        }
    }

    // Plays game over sequence stinger followed by BGM.
    public void PlayGameOverSequence()
    {
        if (musicSource != null) musicSource.Stop();
        if (secondaryMusicSource != null) secondaryMusicSource.Stop();
        
        float delay = 0f;
        if (sfxSource != null && gameOverSFX != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(gameOverSFX);
            delay = gameOverSFX.length;
        }

        if (gameOverBGM != null)
        {
            StartCoroutine(PlayBGMAfterDelay(gameOverBGM, delay));
        }
    }

    private System.Collections.IEnumerator PlayBGMAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayMusic(clip);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip);
    }

    // Plays SFX with randomized pitch.
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.85f, float maxPitch = 1.15f)
    {
        if (sfxSource == null || clip == null) return;
        
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip);
    }

    public void SetBgmVolume(float sliderValue)
    {
        sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        PlayerPrefs.SetFloat(BGM_VOL_KEY, sliderValue);
        
        if (masterMixer != null)
        {
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

    public void PlayUIButton() 
    { 
        if (buttonClickSFX != null) PlaySFX(buttonClickSFX); 
        // else Debug.LogWarning("[AudioManager] No Button Click SFX assigned in Inspector!");
    }
    public void PlayPause() { PlaySFX(pauseSFX); }
}
