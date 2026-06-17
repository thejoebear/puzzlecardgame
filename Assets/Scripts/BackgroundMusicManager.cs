using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    [Header("Playlist Settings")]
    [Tooltip("The list of background music tracks to play.")]
    public List<AudioClip> playlist = new List<AudioClip>();
    
    [Tooltip("Whether the playlist should play in shuffle mode.")]
    [SerializeField] private bool shuffle = false;
    
    [Tooltip("Whether to automatically start playing when the game begins.")]
    [SerializeField] private bool autoPlay = true;

    [Header("Transition Settings")]
    [Tooltip("Duration of the crossfade transition between tracks in seconds.")]
    [SerializeField] private float crossfadeDuration = 2.0f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool isSourceAActive = true;
    
    private List<int> playOrder = new List<int>();
    private int playOrderIndex = -1;
    private int currentTrackIndex = -1;
    private Coroutine crossfadeCoroutine;
    private bool isPaused = false;

    public bool Shuffle
    {
        get => shuffle;
        set
        {
            if (shuffle != value)
            {
                shuffle = value;
                RebuildPlayOrder();
            }
        }
    }

    public bool IsPlaying => (isSourceAActive ? sourceA : sourceB).isPlaying;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // BackgroundMusicManager should persist along with AudioManager
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ConfigureMixerGroup();
        RebuildPlayOrder();

        if (autoPlay && playlist.Count > 0)
        {
            PlayNext();
        }
    }

    private void InitializeAudioSources()
    {
        // Add two separate AudioSources for crossfading
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(sourceA);
        ConfigureAudioSource(sourceB);
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false; // We manage track transitions ourselves via Update and crossfading
        source.volume = 0f;
    }

    private void ConfigureMixerGroup()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.masterMixer != null)
        {
            AudioMixerGroup[] musicGroups = AudioManager.Instance.masterMixer.FindMatchingGroups("Music");
            if (musicGroups.Length > 0)
            {
                sourceA.outputAudioMixerGroup = musicGroups[0];
                sourceB.outputAudioMixerGroup = musicGroups[0];
            }
        }
    }

    private void RebuildPlayOrder()
    {
        if (playlist == null || playlist.Count == 0) return;

        playOrder.Clear();
        for (int i = 0; i < playlist.Count; i++)
        {
            playOrder.Add(i);
        }

        if (shuffle)
        {
            // Fisher-Yates Shuffle
            for (int i = playOrder.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                int tmp = playOrder[i];
                playOrder[i] = playOrder[r];
                playOrder[r] = tmp;
            }
        }

        // Try to sync playOrderIndex with currentTrackIndex if possible
        if (currentTrackIndex != -1)
        {
            playOrderIndex = playOrder.IndexOf(currentTrackIndex);
        }
        else
        {
            playOrderIndex = -1;
        }
    }

    private void Update()
    {
        if (playlist == null || playlist.Count == 0 || isPaused) return;

        AudioSource activeSource = isSourceAActive ? sourceA : sourceB;

        // If active source has stopped playing and we are not currently crossfading, advance to the next track
        if (!activeSource.isPlaying && crossfadeCoroutine == null)
        {
            PlayNext();
        }
    }

    public void PlayNext()
    {
        if (playlist == null || playlist.Count == 0) return;

        playOrderIndex++;
        if (playOrderIndex >= playOrder.Count)
        {
            playOrderIndex = 0;
            if (shuffle)
            {
                // Reshuffle for a new sequence
                RebuildPlayOrder();
                playOrderIndex = 0;
            }
        }

        int nextTrackIndex = playOrder[playOrderIndex];
        PlayTrack(nextTrackIndex);
    }

    public void PlayPrevious()
    {
        if (playlist == null || playlist.Count == 0) return;

        playOrderIndex--;
        if (playOrderIndex < 0)
        {
            playOrderIndex = playOrder.Count - 1;
        }

        int prevTrackIndex = playOrder[playOrderIndex];
        PlayTrack(prevTrackIndex);
    }

    public void PlayTrack(int index)
    {
        if (playlist == null || index < 0 || index >= playlist.Count) return;

        currentTrackIndex = index;
        isPaused = false;

        AudioClip newClip = playlist[currentTrackIndex];

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(Crossfade(newClip, crossfadeDuration));
    }

    public void PlayMusicDirectly(AudioClip clip)
    {
        if (clip == null) return;

        // Allow direct playback of a custom clip (e.g. dynamic/event-driven triggers)
        // If it's already in the playlist, align indexes; otherwise, we play it outside the order.
        int index = playlist.IndexOf(clip);
        if (index != -1)
        {
            currentTrackIndex = index;
            playOrderIndex = playOrder.IndexOf(index);
        }
        else
        {
            currentTrackIndex = -1;
        }

        isPaused = false;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(Crossfade(clip, crossfadeDuration));
    }

    private System.Collections.IEnumerator Crossfade(AudioClip newClip, float duration)
    {
        AudioSource activeSource = isSourceAActive ? sourceA : sourceB;
        AudioSource inactiveSource = isSourceAActive ? sourceB : sourceA;

        inactiveSource.clip = newClip;
        inactiveSource.volume = 0f;
        inactiveSource.time = 0f;

        if (newClip != null)
        {
            inactiveSource.Play();
        }

        float elapsed = 0f;
        float startVolume = activeSource.volume;
        float targetVolume = 1.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (activeSource.isPlaying)
            {
                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            if (inactiveSource.isPlaying)
            {
                inactiveSource.volume = Mathf.Lerp(0f, targetVolume, t);
            }

            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();

        inactiveSource.volume = targetVolume;

        isSourceAActive = !isSourceAActive;
        crossfadeCoroutine = null;
    }

    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        sourceA.Pause();
        sourceB.Pause();
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        sourceA.UnPause();
        sourceB.UnPause();
    }

    public string GetCurrentTrackName()
    {
        if (currentTrackIndex >= 0 && currentTrackIndex < playlist.Count)
        {
            return playlist[currentTrackIndex].name;
        }
        return "None";
    }
}