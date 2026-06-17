using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EternalAtlasUI : MonoBehaviour
{
    public static EternalAtlasUI Instance;

    [Header("UI Panels")]
    public GameObject panel;
    public Transform categorySidebar;
    public Transform factsContent;
    
    [Header("Prefabs")]
    public GameObject categoryButtonPrefab;
    public GameObject factEntryPrefab;

    [Header("Detail View")]
    public TextMeshProUGUI categoryTitleText;
    public TextMeshProUGUI progressText;
    public UnityEngine.UI.Image progressFill;

    [Header("Data")]
    public List<FactPack> allFactPacks;
    private FactPack selectedPack;

    void Awake()
    {
        Instance = this;
    }

    public void OpenAtlas()
    {
        panel.SetActive(true);
        InitializeSidebar();
        if (allFactPacks.Count > 0) SelectCategory(allFactPacks[0]);
    }

    public void CloseAtlas()
    {
        panel.SetActive(false);
    }

    private void InitializeSidebar()
    {
        foreach (Transform child in categorySidebar) Destroy(child.gameObject);

        foreach (var pack in allFactPacks)
        {
            GameObject btnObj = Instantiate(categoryButtonPrefab, categorySidebar);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = pack.category.ToString();
            
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectCategory(pack));

            // Show mini completion count
            int count = GetCollectedCount(pack);
            // We could add a small star icon if count == pack.facts.Count
        }
    }

    public void SelectCategory(FactPack pack)
    {
        selectedPack = pack;
        RefreshDetailView();
    }

    private void RefreshDetailView()
    {
        if (selectedPack == null) return;

        categoryTitleText.text = selectedPack.category.ToString().ToUpper();
        
        // Clear old entries
        foreach (Transform child in factsContent) Destroy(child.gameObject);

        int collected = 0;
        foreach (string fact in selectedPack.facts)
        {
            bool discovered = CollectionManager.Instance.IsFactDiscovered(fact);
            
            GameObject entry = Instantiate(factEntryPrefab, factsContent);
            TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>();

            if (discovered)
            {
                txt.text = $"• {fact}";
                txt.color = Color.white;
                collected++;
            }
            else
            {
                txt.text = "• <i>Hidden Archive</i> (Complete this topic to reveal)";
                txt.color = new Color(1, 1, 1, 0.2f);
            }
        }

        float percent = (float)collected / selectedPack.facts.Count;
        progressText.text = $"Restoration: {collected}/{selectedPack.facts.Count} ({Mathf.RoundToInt(percent * 100)}%)";
        if (progressFill != null) progressFill.fillAmount = percent;
        
        // Golden illumination effect if 100%
        if (collected == selectedPack.facts.Count)
        {
            categoryTitleText.color = new Color(1f, 0.84f, 0f); // Gold
        }
        else
        {
            categoryTitleText.color = Color.white;
        }
    }

    private int GetCollectedCount(FactPack pack)
    {
        int count = 0;
        foreach (var f in pack.facts) if (CollectionManager.Instance.IsFactDiscovered(f)) count++;
        return count;
    }
}
