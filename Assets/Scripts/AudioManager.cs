using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer masterMixer;
    public AudioMixerSnapshot defaultSnapshot;
    public AudioMixerSnapshot cosmicSnapshot;

    [Header("Audio Clips")]
    public AudioClip cardFlick;
    public AudioClip cardSnap;
    public AudioClip levelWin;
    public AudioClip errorBuzzer;
    public AudioClip celestialCleanup;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Dictionary<AudioClip, float> cooldowns = new Dictionary<AudioClip, float>();
    private const float MIN_INTERVAL = 0.05f;

    private const string MUSIC_VOL_KEY = "MusicVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;

            if (masterMixer != null)
            {
                var musicGroups = masterMixer.FindMatchingGroups("Music");
                if (musicGroups.Length > 0) musicSource.outputAudioMixerGroup = musicGroups[0];

                var sfxGroups = masterMixer.FindMatchingGroups("SFX");
                if (sfxGroups.Length > 0)
                {
                    audioSource.outputAudioMixerGroup = sfxGroups[0];
                    sfxSource.outputAudioMixerGroup = sfxGroups[0];
                }
            }

            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadSettings()
    {
        float musicVol = 0.75f;
        float sfxVol = 0.75f;

        if (ProgressionManager.Instance != null)
        {
            var data = ProgressionManager.Instance.GetSaveData();
            musicVol = data.musicVolume;
            sfxVol = data.sfxVolume;
        }
        else
        {
            musicVol = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.75f);
            sfxVol = PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f);
        }
        
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    public void SetMusicVolume(float volume)
    {
        if (masterMixer != null)
        {
            masterMixer.SetFloat("MusicVolume", LinearToDecibel(volume));
        }
        else
        {
            musicSource.volume = volume;
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.GetSaveData().musicVolume = volume;
            ProgressionManager.Instance.SaveProgress();
        }
        else
        {
            PlayerPrefs.SetFloat(MUSIC_VOL_KEY, volume);
            PlayerPrefs.Save();
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (masterMixer != null)
        {
            masterMixer.SetFloat("SFXVolume", LinearToDecibel(volume));
        }
        else
        {
            audioSource.volume = volume;
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.GetSaveData().sfxVolume = volume;
            ProgressionManager.Instance.SaveProgress();
        }
        else
        {
            PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
            PlayerPrefs.Save();
        }
    }

    public void TransitionToCosmic(bool active, float duration = 1.0f)
    {
        if (active && cosmicSnapshot != null)
        {
            cosmicSnapshot.TransitionTo(duration);
        }
        else if (!active && defaultSnapshot != null)
        {
            defaultSnapshot.TransitionTo(duration);
        }
    }

    private float LinearToDecibel(float linear)
    {
        float dB;
        if (linear != 0)
            dB = 20.0f * Mathf.Log10(linear);
        else
            dB = -80.0f;
        return dB;
    }

    public float GetMusicVolume()
    {
        if (masterMixer != null && masterMixer.GetFloat("MusicVolume", out float value))
            return DecibelToLinear(value);
        return musicSource.volume;
    }

    public float GetSFXVolume()
    {
        if (masterMixer != null && masterMixer.GetFloat("SFXVolume", out float value))
            return DecibelToLinear(value);
        return audioSource.volume;
    }

    private float DecibelToLinear(float dB)
    {
        return Mathf.Pow(10.0f, dB / 20.0f);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.PlayMusicDirectly(clip);
            return;
        }

        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayCardFlick() => PlaySound(cardFlick, 0.9f, 1.1f, 0.03f);
    public void PlayCardSnap() => PlaySound(cardSnap, 0.95f, 1.05f, 0.05f);
    public void PlayLevelWin() => PlayOneShot(levelWin);
    public void PlayError() => PlaySound(errorBuzzer, 1.0f, 1.0f, 0.2f);
    public void PlayCelestialCleanup() => PlayOneShot(celestialCleanup);

    private void PlaySound(AudioClip clip, float minPitch, float maxPitch, float cooldown = 0.05f)
    {
        if (clip == null) return;
        
        if (cooldowns.ContainsKey(clip) && Time.time < cooldowns[clip])
        {
            return;
        }

        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip);
        
        cooldowns[clip] = Time.time + cooldown;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.pitch = 1.0f;
        audioSource.PlayOneShot(clip);
    }
}
