using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SolitaireLevelManager : MonoBehaviour
{
    [Header("Dynamic Deck Settings (Fallback)")]
    public int targetTotalDeckSize = 52;
    public int minCategories = 2;
    public int maxCategories = 4;
    public int minCardsPerCategory = 5;
    [Range(1, 4)]
    public int foundationSlotLimit = 2;

    [Header("Level Configuration")]
    public List<LevelData> levels = new List<LevelData>();
    public int currentLevelIndex = 0;

    [Header("Level UI")]
    public TextMeshProUGUI levelNameText;
    public GameObject victoryPanel;

    private Dictionary<CardCategory, int> categoryColorGroups = new Dictionary<CardCategory, int>();

    public void LoadLevel(int index, out LevelData level, out List<CardData> deckPool, out Dictionary<CardCategory, int> categorySizes, out int slotLimit, LevelData customLevel = null)
    {
        currentLevelIndex = index;
        if (customLevel != null)
        {
            level = customLevel;
        }
        else if (levels != null && currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            level = levels[currentLevelIndex];
        }
        else
        {
            level = null;
        }

        slotLimit = level != null ? level.foundationSlots : foundationSlotLimit;

        if (levelNameText != null) 
            levelNameText.text = level != null ? level.levelName : "Free Play";

        ApplyTheme(level != null ? level.theme : null);

        deckPool = new List<CardData>();
        categorySizes = new Dictionary<CardCategory, int>();
        categoryColorGroups.Clear();

        PrepareCardPool(level, deckPool, categorySizes);
    }

    public bool IsDifferentColorGroup(CardCategory cat1, CardCategory cat2)
    {
        return SolitaireRules.IsDifferentColorGroup(cat1, cat2, categoryColorGroups);
    }

    private void PrepareCardPool(LevelData level, List<CardData> currentDeckPool, Dictionary<CardCategory, int> activeCategorySizes)
    {
        if (level != null)
        {
            List<CardCategory> activeCategories = level.GetCategories();
            if (activeCategories.Count == 0)
            {
                Debug.LogError($"Level {level.levelName} has no categories assigned! Falling back to dynamic deck.");
                GenerateDynamicDeck(currentDeckPool, activeCategorySizes);
                return;
            }

            for (int i = 0; i < activeCategories.Count; i++) 
                categoryColorGroups[activeCategories[i]] = i % 2;

            foreach (var mapping in level.categoryMappings)
            {
                CardCategory cat = mapping.category;
                if (activeCategorySizes.ContainsKey(cat))
                {
                    Debug.LogError($"[LevelManager] Level {level.levelName} has duplicate mapping for category {cat}! This will break completion logic.");
                }
                int size = mapping.cardCount;
                activeCategorySizes[cat] = size;

                Debug.Log($"[PrepareCardPool] Category {cat}: Expecting {size} cards (Ranks 1 to {size}).");

                HashSet<int> ranksCreated = new HashSet<int>();
                for (int r = 1; r <= size; r++)
                {
                    CardData card = ScriptableObject.CreateInstance<CardData>();
                    card.category = cat;
                    card.rank = r;
                    
                    SolitaireManager managerComp = GetComponent<SolitaireManager>();
                    if (managerComp != null && managerComp.deck != null)
                    {
                        CardData preAuthored = managerComp.deck.cards.Find(c => c != null && c.category == cat && c.rank == r);
                        if (preAuthored != null)
                        {
                            card.icon = preAuthored.icon;
                        }
                    }
                    
                    if (level != null && level.useAnomalies && Random.value < level.anomalyChance)
                    {
                        card.anomaly = (AnomalyType)Random.Range(1, 4); // Supernova, Binary, or Nebula
                    }
                    
                    if (mapping.factPack != null && mapping.factPack.facts.Count >= r)
{
                        card.fact = mapping.factPack.facts[r - 1];
                    }
                    else
                    {
                        card.fact = $"{cat} Fact #{r}";
                    }
                    currentDeckPool.Add(card);
                    ranksCreated.Add(r);
                }

                // Double check sequence integrity
                for (int i = 1; i <= size; i++)
                {
                    if (!ranksCreated.Contains(i))
                        Debug.LogError($"[PrepareCardPool] ERROR: Rank {i} was NOT created for category {cat}!");
                }
            }
Debug.Log($"[PrepareCardPool] Total cards successfully created: {currentDeckPool.Count}");
}
        else
        {
            GenerateDynamicDeck(currentDeckPool, activeCategorySizes);
        }
    }

    private void GenerateDynamicDeck(List<CardData> currentDeckPool, Dictionary<CardCategory, int> activeCategorySizes)
    {
        int numCategories = Random.Range(minCategories, maxCategories + 1);
        List<CardCategory> allPossible = new List<CardCategory>((CardCategory[])System.Enum.GetValues(typeof(CardCategory)));
        List<CardCategory> activeCategories = new List<CardCategory>();

        for (int i = 0; i < numCategories && allPossible.Count > 0; i++)
        {
            int index = Random.Range(0, allPossible.Count);
            CardCategory cat = allPossible[index];
            activeCategories.Add(cat);
            categoryColorGroups[cat] = i % 2;
            allPossible.RemoveAt(index);
        }

        int remainingCards = targetTotalDeckSize;
        Dictionary<CardCategory, int> categorySizes = new Dictionary<CardCategory, int>();
        foreach(var cat in activeCategories) 
        { 
            categorySizes[cat] = minCardsPerCategory; 
            remainingCards -= minCardsPerCategory; 
        }

        while(remainingCards > 0) 
        { 
            CardCategory randomCat = activeCategories[Random.Range(0, activeCategories.Count)]; 
            categorySizes[randomCat]++; 
            remainingCards--; 
        }

        foreach(var cat in activeCategories)
        {
            int size = categorySizes[cat];
            activeCategorySizes[cat] = size;
            for(int r = 1; r <= size; r++)
            {
                CardData card = ScriptableObject.CreateInstance<CardData>();
                card.category = cat; 
                card.rank = r; 
                card.fact = $"Random {cat} Fact #{r}";
                
                SolitaireManager managerComp = GetComponent<SolitaireManager>();
                if (managerComp != null && managerComp.deck != null)
                {
                    CardData preAuthored = managerComp.deck.cards.Find(c => c != null && c.category == cat && c.rank == r);
                    if (preAuthored != null)
                    {
                        card.icon = preAuthored.icon;
                    }
                }
                
                currentDeckPool.Add(card);
            }
        }
    }

    public void ApplyTheme(VisualTheme theme)
    {
        if (theme == null) return;

        GameObject boardObj = GameObject.Find("Board");
        if (boardObj != null)
        {
            var img = boardObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                // Clear the static background if we have a parallax one
                Transform parallax = boardObj.transform.Find("ParallaxBackground");
                if (parallax != null)
                {
                    img.color = Color.clear;
                    Transform farLayer = parallax.Find("FarLayer");
                    if (farLayer != null)
                    {
                        UnityEngine.UI.Image farImg = farLayer.GetComponent<UnityEngine.UI.Image>();
                        if (farImg != null)
                        {
                            farImg.sprite = theme.boardSprite;
                            farImg.color = theme.boardColor;
                        }
                    }
                }
                else
                {
                    img.color = theme.boardColor;
                    if (theme.boardSprite != null) img.sprite = theme.boardSprite;
                }
            }
        }

        // Apply silhouettes to all piles in scene
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pile>(FindObjectsInactive.Include))
        {
            if (p.silhouetteImage != null)
            {
                if (p.type == PileType.Foundation) p.silhouetteImage.sprite = theme.foundationSilhouette;
                if (p.type == PileType.Tableau) p.silhouetteImage.sprite = theme.tableauSilhouette;
                if (p.type == PileType.Stock && theme.cardBackSprite != null)
                {
                    p.silhouetteImage.sprite = theme.cardBackSprite;
                    p.silhouetteImage.color = Color.white;
                }
            }
        }

        ThemeUIButton("HintButton", theme);
        ThemeUIButton("UndoButton", theme);
        ThemeUIButton("RestartButton", theme);
        ThemeUIButton("JournalButton", theme);
        ThemeUIButton("MenuButton", theme);

        if (victoryPanel != null)
        {
            Transform paBtn = victoryPanel.transform.Find("PlayAgainButton");
            if (paBtn != null) ThemeUIButton(paBtn.gameObject, theme);
            
            Transform nlBtn = victoryPanel.transform.Find("NextLevelButton");
            if (nlBtn != null) ThemeUIButton(nlBtn.gameObject, theme);
        }
    }

    private void ThemeUIButton(string name, VisualTheme theme)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) ThemeUIButton(go, theme);
    }

    private void ThemeUIButton(GameObject go, VisualTheme theme)
    {
        if (go == null) return;
        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = theme.primaryUIColor;
        var txt = go.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.color = theme.textOnPrimaryColor;
    }
}
