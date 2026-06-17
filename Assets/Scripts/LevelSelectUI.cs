using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    public MainMenuManager mainMenuManager;
    public SolitaireManager solitaireManager;
    public Transform contentParent;
    public GameObject levelButtonPrefab;

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (contentParent == null || levelButtonPrefab == null) return;

        // Robust cleanup for both Edit and Play mode
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in contentParent) children.Add(child.gameObject);
        foreach (GameObject child in children)
        {
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        if (solitaireManager == null || solitaireManager.levelManager.levels == null) return;

        for (int i = 0; i < solitaireManager.levelManager.levels.Count; i++)
        {
            int index = i;
            LevelData level = solitaireManager.levelManager.levels[i];
bool unlocked = ProgressionManager.Instance == null || ProgressionManager.Instance.IsLevelUnlocked(index);

            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            btnObj.name = $"LevelButton_{index}";
            btnObj.SetActive(true); // Ensure visibility if prefab was hidden

            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            txt.text = level.levelName;
            
            if (unlocked)
            {
                btn.interactable = true;
                btn.onClick.AddListener(() => mainMenuManager.StartLevel(index));
                txt.color = Color.white;
                
                // Show stars
                int stars = 0;
                if (ProgressionManager.Instance != null)
                {
                    stars = ProgressionManager.Instance.GetStars(index);
                }
                
                string starStr = "";
                for(int s=0; s<stars; s++) starStr += "*";
                for(int s=stars; s<3; s++) starStr += "-";
                
                txt.text += $"\n{starStr}";
}
else
{
                btn.interactable = false;
                txt.text = "LOCKED";
                txt.color = new Color(1, 1, 1, 0.3f);
            }
        }
    }
}
