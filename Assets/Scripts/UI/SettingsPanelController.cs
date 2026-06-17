using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UnityEngine.UI.Slider musicSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxSlider;
    [SerializeField] private UnityEngine.UI.Toggle batterySaverToggle;
    [SerializeField] private UnityEngine.UI.Button backButton;

    [Header("Managers")]
    [SerializeField] private MainMenuManager mainMenuManager;

    private bool isInitializing = false;
    private const string BATTERY_SAVER_KEY = "BatterySaver";

    private void OnEnable()
    {
        InitializeSettings();
    }

    private void Start()
    {
        // Find MainMenuManager in the scene if not assigned in the inspector
        if (mainMenuManager == null)
        {
            mainMenuManager = Object.FindAnyObjectByType<MainMenuManager>();
        }

        // Hook up UI listeners
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (batterySaverToggle != null)
        {
            batterySaverToggle.onValueChanged.AddListener(OnBatterySaverToggled);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void InitializeSettings()
    {
        isInitializing = true;

        // Load Volume Settings from AudioManager (or fallbacks)
        float musicVol = 0.75f;
        float sfxVol = 0.75f;

        if (AudioManager.Instance != null)
        {
            musicVol = AudioManager.Instance.GetMusicVolume();
            sfxVol = AudioManager.Instance.GetSFXVolume();
        }
        else
        {
            musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        }

        if (musicSlider != null)
        {
            musicSlider.value = musicVol;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVol;
        }

        // Load Battery Saver Setting
        bool batterySaverOn = PlayerPrefs.GetInt(BATTERY_SAVER_KEY, 0) == 1;
        if (batterySaverToggle != null)
        {
            batterySaverToggle.isOn = batterySaverOn;
        }

        isInitializing = false;
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (isInitializing) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (isInitializing) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    private void OnBatterySaverToggled(bool isOn)
    {
        if (isInitializing) return;

        if (mainMenuManager != null)
        {
            mainMenuManager.ToggleBatterySaver(isOn);
        }
        else
        {
            // Fallback implementation if MainMenuManager is absent
            Application.targetFrameRate = isOn ? 30 : 60;
            PlayerPrefs.SetInt(BATTERY_SAVER_KEY, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        // Play nice click sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardSnap();
        }
    }

    private void OnBackButtonClicked()
    {
        if (isInitializing) return;

        // Play nice close sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardFlick();
        }
    }
}

