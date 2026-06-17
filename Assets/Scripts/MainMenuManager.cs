using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject boardPanel;
    public GameObject abilityBar;
    public GameObject settingsPanel;
    public GameObject menuPanel;
    public GameObject creditsPanel;
    public GameObject shopPanel;
  //  public GameObject MenuPanel;

    public SolitaireManager solitaireManager;

    private const string BATTERY_SAVER_KEY = "BatterySaver";

    void Start()
    {
        ApplyBatterySaver(PlayerPrefs.GetInt(BATTERY_SAVER_KEY, 0) == 1);
        BackToMainMenu();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenShop()
    {
        if (shopPanel != null) shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void ToggleBatterySaver(bool isOn)
    {
        ApplyBatterySaver(isOn);
        PlayerPrefs.SetInt(BATTERY_SAVER_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyBatterySaver(bool isOn)
    {
        Application.targetFrameRate = isOn ? 30 : 60;
    }

    public void OpenLevelSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (boardPanel != null) boardPanel.SetActive(false);
        if (abilityBar != null) abilityBar.SetActive(false);
        
        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(true);
            
            CelestialSagaMap saga = levelSelectPanel.GetComponentInChildren<CelestialSagaMap>(true);
            if (saga != null)
            {
                saga.InitializeSagaMap();
            }
            else
            {
                GrandOrreryManager orrery = levelSelectPanel.GetComponent<GrandOrreryManager>();
                if (orrery != null && orrery.enabled) orrery.InitializeOrrery();
            }
        }
    }

    public void BackToMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (boardPanel != null) boardPanel.SetActive(false);
        if (abilityBar != null) abilityBar.SetActive(false);
        
    }

    public void BacktoMainmenu2()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
             
    }

    public void StartLevel(int index)
    {
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        //if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        
        if (boardPanel != null) boardPanel.SetActive(true);
        if (abilityBar != null) abilityBar.SetActive(true);

        if (solitaireManager != null)
        {
            solitaireManager.levelManager.currentLevelIndex = index;
            solitaireManager.InitializeGame();
        }
    }

    public void StartDailyChallenge()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);

        if (boardPanel != null) boardPanel.SetActive(true);
        if (abilityBar != null) abilityBar.SetActive(true);

        if (DailyChallengeManager.Instance != null && solitaireManager != null)
        {
            LevelData dailyLevel = DailyChallengeManager.Instance.GenerateDailyLevel();
            solitaireManager.InitializeGame(dailyLevel);
        }
    }
        public void CloseSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}
