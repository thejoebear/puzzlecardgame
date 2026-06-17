using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolitaireManager : MonoBehaviour
{
public Deck deck;
    public GameObject cardPrefab;

    [Header("Piles")]
    public Transform stockPile;
    public Transform wastePile;
    public Transform[] foundationPiles; // 4
    public Transform[] tableauPiles;    // 7

    [Header("UI")]
    public GameObject victoryPanel;
    public GameObject gameOverPanel;

    [Header("Level Configuration")]
    public SolitaireLevelManager levelManager;

    [Header("Layout Containers")]
    public RectTransform headerBar;
    public RectTransform topPilesContainer;
    public RectTransform tableausContainer;
    public TMPro.TextMeshProUGUI moveCountText;
    public TMPro.TextMeshProUGUI totalCardsText;
    public StarProgressBar starProgressBar;

    [Header("Dynamic Spacing")]
    public float baseCardSpacing = 45f;
    public float minCardSpacing = 2f;
    public float bottomPadding = 20f;

    public Transform completedPile;

    [Header("Effects")]
    public ParticleSystem winParticles;
    public ParticleSystem categoryParticles;

    public List<CardData> currentDeckPool = new List<CardData>();
    public Dictionary<CardCategory, int> activeCategorySizes = new Dictionary<CardCategory, int>();
    public CardCategory?[] foundationCategories = new CardCategory?[4]; 
    public int totalCardsInCurrentGame = 0;
    public int completedCardsCount = 0;
    public int moveCount = 0;

    public List<CardData> stock = new List<CardData>();
    public List<CardData> waste = new List<CardData>();
    public List<CardData>[] foundations; 
    public List<CardData>[] tableaus = new List<CardData>[7];
    internal LevelData activeLevel;
    internal VisualTheme currentLevelTheme;
    internal int activeTableauCapacity = 0;
    internal bool activeUseMysteryCards = false;
    internal float activeMysteryChance = 0f;
    internal int activeLockedSlots = 0;

    public SolitaireHistory solitaireHistory = new SolitaireHistory();
    private bool isResetting = false;
    private CardData lastMovedCard;

    private ISolitaireState currentState;

    void Awake()
    {
        levelManager = GetComponent<SolitaireLevelManager>();
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    public void ChangeState(ISolitaireState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
        }
    }

    public void InitializeGame(LevelData customLevel = null)
    {
        ChangeState(new SolitaireInitState(this, customLevel));
    }

    internal IEnumerator InternalDealRoutine()
    {
        for (int i = 0; i < 7; i++)
        {
            float spacing = GetVerticalSpacing(tableaus[i].Count, tableausContainer.rect.height);
            for (int j = 0; j < tableaus[i].Count; j++)
            {
                CardData card = tableaus[i][j];
// Use Pool instead of Instantiate
                GameObject cardObj;
                if (CardPool.Instance != null)
                {
                    cardObj = CardPool.Instance.GetCard(stockPile);
                }
                else
                {
                    cardObj = Instantiate(cardPrefab, stockPile);
                }

                cardObj.transform.localPosition = Vector3.zero;
cardObj.transform.localScale = Vector3.zero; // Start small
                cardObj.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-20f, 20f));

                CardDisplay display = cardObj.GetComponent<CardDisplay>();
                display.SetCard(card);
                bool faceUp = (j == tableaus[i].Count - 1);
                display.Flip(faceUp);
                display.ApplyTheme(currentLevelTheme);

                if (faceUp && activeUseMysteryCards && UnityEngine.Random.value < activeMysteryChance && card.rank != 1)
                {
                    display.SetMystery(true);
                }
                
                // Reparent to Tableau but keep visual position
                cardObj.transform.SetParent(tableauPiles[i], true);
                Vector3 targetLocalPos = new Vector3(0, -j * spacing, 0);
                
                CardMotion motion = cardObj.GetComponent<CardMotion>();
if (motion == null) motion = cardObj.AddComponent<CardMotion>();
                
                // Glide to position with scale-up and rotation reset
                motion.MoveTo(targetLocalPos, 0.35f, null, Quaternion.identity, Vector3.one);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCardFlick();
                
                if (faceUp) cardObj.AddComponent<CardDraggable>();
                
                yield return new WaitForSeconds(0.06f); // Slightly slower rhythmic deal
            }
        }
        
        ChangeState(new SolitairePlayState(this));
    }

    internal void ShuffleList(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            CardData temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    internal void ClearBoard()
    {
        // Use Registry to find all active cards instead of FindObjectsByType
        List<CardDisplay> allCards = new List<CardDisplay>();
        if (CardRegistry.Instance != null)
        {
            allCards.AddRange(CardRegistry.Instance.GetAllCards());
        }
        else
        {
            allCards.AddRange(UnityEngine.Object.FindObjectsByType<CardDisplay>(FindObjectsInactive.Include));
        }

        foreach (var card in allCards)
        {
            if (CardPool.Instance != null)
            {
                CardPool.Instance.ReturnCard(card.gameObject);
            }
            else
            {
                if (Application.isPlaying) Destroy(card.gameObject);
                else DestroyImmediate(card.gameObject);
            }
        }

        // Clear all internal lists
        if (stock != null) stock.Clear();
        if (waste != null) waste.Clear();
        if (tableaus != null) foreach (var t in tableaus) if (t != null) t.Clear();
        if (foundations != null) foreach (var f in foundations) if (f != null) f.Clear();
        if (currentDeckPool != null) currentDeckPool.Clear();
        
        completedCardsCount = 0;
        moveCount = 0;
    }

    public void SetupPileComponents(int slotLimit)
    {
if (stockPile.GetComponent<Pile>() == null) stockPile.gameObject.AddComponent<Pile>().type = PileType.Stock;
UnityEngine.UI.Button btn = stockPile.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) btn = stockPile.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnStockClicked);

        if (wastePile.GetComponent<Pile>() == null) wastePile.gameObject.AddComponent<Pile>().type = PileType.Waste;

        for (int i = 0; i < foundationPiles.Length; i++)
        {
            bool isActive = i < slotLimit;
            foundationPiles[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                Pile p = foundationPiles[i].GetComponent<Pile>();
                if (p == null) p = foundationPiles[i].gameObject.AddComponent<Pile>();
                p.type = PileType.Foundation; p.index = i;

                // Initial Locking
                if (i >= (slotLimit - activeLockedSlots))
                {
                    p.SetLocked(true);
                }
                else
                {
                    p.SetLocked(false);
                }
            }
}

        for (int i = 0; i < tableauPiles.Length; i++)
        {
            if (tableauPiles[i].GetComponent<Pile>() == null)
            {
                Pile p = tableauPiles[i].gameObject.AddComponent<Pile>();
                p.type = PileType.Tableau; p.index = i;
            }
        }

        // Add visual highlights to all piles
        SetupHighlightVisuals();
    }

    public void SetupHighlightVisuals()
{
        List<Transform> allPiles = new List<Transform>();
        allPiles.Add(stockPile);
        allPiles.Add(wastePile);
        allPiles.AddRange(foundationPiles);
        allPiles.AddRange(tableauPiles);

        foreach (Transform pTransform in allPiles)
        {
            if (pTransform == null) continue;
            Pile p = pTransform.GetComponent<Pile>();
            if (p == null) continue;

            if (p.highlightImage == null)
            {
                // Create a simple highlight child
                GameObject hl = new GameObject("Highlight", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                hl.transform.SetParent(pTransform, false);
                hl.transform.SetAsFirstSibling();
                
                RectTransform rt = hl.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = new Vector2(10, 10); // Slightly larger than parent

                UnityEngine.UI.Image img = hl.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(1, 1, 0, 0.4f); // Golden glow
                
                p.highlightImage = img;
                hl.SetActive(false);
            }

            if (p.silhouetteImage == null)
            {
                // Create a silhouette child
                GameObject sil = new GameObject("Silhouette", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                sil.transform.SetParent(pTransform, false);
                sil.transform.SetAsFirstSibling(); // Silhouette behind cards but potentially above highlight? 
                // Let's put highlight first, then silhouette.
                
                RectTransform rt = sil.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                UnityEngine.UI.Image img = sil.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(1, 1, 1, 0.3f); // Faint white
                img.raycastTarget = false;
                
                p.silhouetteImage = img;

                // Apply initial sprite from theme if possible
                if (currentLevelTheme != null)
                {
                    if (p.type == PileType.Foundation) img.sprite = currentLevelTheme.foundationSilhouette;
                    if (p.type == PileType.Tableau) img.sprite = currentLevelTheme.tableauSilhouette;
                }
            }

            // Fix Tableau pile alignment discrepancy (align Highlight and Silhouette with the first card)
            if (p.type == PileType.Tableau)
            {
                float cardWidth = 150f;
                float cardHeight = 210f;
                if (cardPrefab != null)
                {
                    RectTransform cardRt = cardPrefab.GetComponent<RectTransform>();
                    if (cardRt != null)
                    {
                        cardWidth = cardRt.rect.width;
                        cardHeight = cardRt.rect.height;
                    }
                }

                if (p.highlightImage != null)
                {
                    RectTransform rt = p.highlightImage.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1.0f);
                    rt.anchorMax = new Vector2(0.5f, 1.0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(cardWidth + 10f, cardHeight + 10f);
                }

                if (p.silhouetteImage != null)
                {
                    RectTransform rt = p.silhouetteImage.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1.0f);
                    rt.anchorMax = new Vector2(0.5f, 1.0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                }
            }
        }
    }

    public void HighlightValidMoves(CardData cardData)
    {
        if (cardData == null) return;

        Color highlightColor = Color.yellow;
        if (currentLevelTheme != null) highlightColor = currentLevelTheme.primaryUIColor;
        highlightColor.a = 0.5f;

        // Check Foundations
        for (int i = 0; i < foundationPiles.Length; i++)
        {
            if (foundationPiles[i].gameObject.activeSelf && CanMoveToFoundation(cardData, i))
            {
                foundationPiles[i].GetComponent<Pile>()?.SetHighlight(true, highlightColor);
            }
        }

        // Check Tableaus
        for (int i = 0; i < tableauPiles.Length; i++)
        {
            if (MoveToTableauCheck(cardData, i))
            {
                tableauPiles[i].GetComponent<Pile>()?.SetHighlight(true, highlightColor);
            }
        }
    }

    public void ClearHighlights()
    {
        List<Transform> allPiles = new List<Transform>();
        allPiles.Add(stockPile);
        allPiles.Add(wastePile);
        allPiles.AddRange(foundationPiles);
        allPiles.AddRange(tableauPiles);

        foreach (Transform p in allPiles)
        {
            if (p != null) p.GetComponent<Pile>()?.SetHighlight(false);
        }
    }

    public void TryAutoMove(GameObject cardObj, Transform originalParent)
    {
        CardDisplay display = cardObj.GetComponent<CardDisplay>();
        if (display == null || !display.IsFaceUp) return;
        
        CardData cardData = display.cardData;
        Pile sourcePile = originalParent.GetComponent<Pile>();
        if (sourcePile == null) return;

        // Gather the stack from this card up
        List<CardData> stackData = new List<CardData>();
        stackData.Add(cardData);
        int myIndex = cardObj.transform.GetSiblingIndex();
        
        for (int i = myIndex + 1; i < originalParent.childCount; i++)
        {
            CardDisplay childDisplay = originalParent.GetChild(i).GetComponent<CardDisplay>();
            if (childDisplay != null)
            {
                if (!childDisplay.IsFaceUp) return; // Cannot move card with face-down cards on top
                stackData.Add(childDisplay.cardData);
            }
        }

        // Verify sequence
        if (!SolitaireRules.IsValidSequence(stackData)) return;

        // 1. Try Foundation slots (Only for single top cards)
        if (stackData.Count == 1)
        {
            for (int i = 0; i < foundationPiles.Length; i++)
            {
                if (foundationPiles[i].gameObject.activeSelf && CanMoveToFoundation(cardData, i))
                {
                    Pile targetPile = foundationPiles[i].GetComponent<Pile>();
                    solitaireHistory.PushAndExecute(new MoveCardCommand(this, stackData, sourcePile, targetPile));
                    return;
                }
            }
        }

        // 2. Try Tableau slots
        for (int i = 0; i < tableauPiles.Length; i++)
        {
            if (MoveToTableauCheck(cardData, i))
            {
                // Prevent moving to itself
                if (sourcePile.type == PileType.Tableau && sourcePile.index == i) continue;

                Pile targetPile = tableauPiles[i].GetComponent<Pile>();
                solitaireHistory.PushAndExecute(new MoveCardCommand(this, stackData, sourcePile, targetPile, true));
                return;
            }
        }
        
        // No valid move: juice feedback
        cardObj.GetComponent<CardMotion>()?.Shake();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
    }

    private bool CanMoveToFoundation(CardData cardData, int index)
    {
        Pile p = foundationPiles[index].GetComponent<Pile>();
        if (p != null && p.isLocked) return false;
        
        return SolitaireRules.CanMoveToFoundation(cardData, foundations[index], foundationCategories[index]);
    }

    CardData DrawFromStock()
{
        if (stock.Count == 0) return null;
        CardData card = stock[0]; stock.RemoveAt(0);
        return card;
    }

    public void OnStockClicked()
    {
        if (isResetting) return;

        if (stock.Count > 0)
        {
            solitaireHistory.PushAndExecute(new DrawStockCommand(this, stock[0]));
        }
        else if (waste.Count > 0)
        {
            solitaireHistory.PushAndExecute(new ResetStockCommand(this));
            StartCoroutine(ResetStockRoutine());
        }
    }

    private IEnumerator ResetStockRoutine()
    {
        isResetting = true;
        // Get all cards in waste in reverse order (top to bottom)
        List<Transform> wasteCards = new List<Transform>();
        foreach (Transform child in wastePile) wasteCards.Add(child);
        wasteCards.Reverse();

        foreach (Transform card in wasteCards)
        {
            card.SetParent(stockPile, true);
            card.GetComponent<CardMotion>()?.MoveTo(Vector3.zero, 0.2f, () => {
                if (CardPool.Instance != null)
                {
                    CardPool.Instance.ReturnCard(card.gameObject);
                }
                else
                {
                    if (Application.isPlaying) Destroy(card.gameObject);
                    else DestroyImmediate(card.gameObject);
                }
            }, Quaternion.identity, Vector3.zero);
            
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCardFlick();
            yield return new WaitForSeconds(0.02f);
        }
isResetting = false;
    }

    public bool TryMoveCard(GameObject cardObj, GameObject target, Transform originalParent)
    {
        CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
        CardData cardData = cardDisplay.cardData;
        Pile targetPile = target.GetComponentInParent<Pile>();
        if (targetPile == null) return false;
        Pile sourcePile = originalParent.GetComponent<Pile>();

        // Detect if we are moving a stack (children of cardObj)
        List<CardData> stackData = new List<CardData>();
        stackData.Add(cardData);
        foreach (Transform child in cardObj.transform)
        {
            CardDisplay childDisplay = child.GetComponent<CardDisplay>();
            if (childDisplay != null) stackData.Add(childDisplay.cardData);
        }

        bool success = false;
        switch (targetPile.type)
        {
            case PileType.Foundation:
                if (stackData.Count == 1) success = CanMoveToFoundation(cardData, targetPile.index);
                break;
            case PileType.Tableau:
                success = MoveToTableauCheck(cardData, targetPile.index);
                break;
        }

        if (success)
        {
            solitaireHistory.PushAndExecute(new MoveCardCommand(this, stackData, sourcePile, targetPile, targetPile.type == PileType.Tableau));
            lastMovedCard = cardData;

            // Post-move Anomaly Logic
            if (cardData.anomaly != AnomalyType.None && AnomalySystem.Instance != null)
            {
                AnomalySystem.Instance.TriggerAnomaly(cardData.anomaly, this, cardDisplay, targetPile);
            }
        }
        return success;
    }

    void RemoveFromSource(CardData cardData, Transform originalParent)
{
        Pile sourcePile = originalParent.GetComponent<Pile>();
        if (sourcePile == null) return;

        switch (sourcePile.type)
        {
            case PileType.Waste: waste.Remove(cardData); break;
            case PileType.Foundation: 
                foundations[sourcePile.index].Remove(cardData); 
                // Fix: If foundation becomes empty, clear the category so it can be reused
                if (foundations[sourcePile.index].Count == 0)
                {
                    foundationCategories[sourcePile.index] = null;
                }
                break;
            case PileType.Tableau:
                tableaus[sourcePile.index].Remove(cardData);
                // Reveal logic: Flip the next top card
                if (originalParent.childCount > 0)
                {
                    Transform lastChild = originalParent.GetChild(originalParent.childCount - 1);
                    CardDisplay nextDisplay = lastChild.GetComponent<CardDisplay>();
                    if (nextDisplay != null)
                    {
                        if (!nextDisplay.IsFaceUp)
                        {
                            nextDisplay.Flip(true);
                            if (lastChild.GetComponent<CardDraggable>() == null)
                                lastChild.gameObject.AddComponent<CardDraggable>();
                        }
                        
                        // If it's face-up but was a mystery card, reveal it now
                        if (nextDisplay.IsMystery)
                        {
                            nextDisplay.RevealMystery();
                        }
                    }
}
                break;
        }
    }

    public bool IsDifferentColorGroup(CardCategory cat1, CardCategory cat2)
    {
        return levelManager.IsDifferentColorGroup(cat1, cat2);
    }

    public void CheckAllCompletions()
    {
        for (int i = 0; i < foundationPiles.Length; i++) InternalCheckCategoryCompletion(i);
        for (int i = 0; i < tableauPiles.Length; i++) CheckTableauCompletion(i);
        
        // Always check win condition after completions to ensure any state change triggers it
        CheckWinCondition();
    }

    internal void InternalCheckCategoryCompletion(int index)
    {
        CardCategory? cat = foundationCategories[index];
        List<CardData> foundation = foundations[index];
        
        // Update Milestone Aura
        Pile p = foundationPiles[index].GetComponent<Pile>();
        if (p != null)
        {
            if (cat == null)
            {
                p.UpdateFoundationAura(0, 1, Color.white);
            }
            else if (activeCategorySizes.TryGetValue(cat.Value, out int targetSize))
            {
                Color categoryColor = Color.white;
                if (currentLevelTheme != null)
                {
                    var visuals = currentLevelTheme.GetVisuals(cat.Value);
                    if (visuals != null) categoryColor = visuals.categoryColor;
                }
                p.UpdateFoundationAura(foundation.Count, targetSize, categoryColor);
            }
        }

        if (cat == null) return;
        
        int targetSizeFinal = 0;
        if (activeCategorySizes.TryGetValue(cat.Value, out targetSizeFinal))
        {
            if (foundation.Count >= targetSizeFinal)
            {
Debug.Log($"[Completion] Foundation {index} completed category {cat.Value} ({foundation.Count}/{targetSizeFinal})!");

                if (CollectionManager.Instance != null)
                {
                    foreach (var card in foundation) CollectionManager.Instance.DiscoverFact(card.fact);
                }

                completedCardsCount += foundation.Count;
                
                bool isFinalPile = completedCardsCount >= totalCardsInCurrentGame;

                List<Transform> cardsToMove = new List<Transform>();
                foreach (Transform child in foundationPiles[index]) 
                {
                    if (child.name != "Highlight") cardsToMove.Add(child);
                }

                if (!isFinalPile)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayCelestialCleanup();

                    if (CelestialVFXManager.Instance != null)
                    {
                        CelestialVFXManager.Instance.PlayCleanupEffect(cardsToMove, Vector3.zero);
                    }
                    else
                    {
                        for (int i = 0; i < cardsToMove.Count; i++)
                        {
                            Transform card = cardsToMove[i];
                            card.SetParent(completedPile, true);
                            CardMotion cm = card.GetComponent<CardMotion>();
                            if (cm == null) cm = card.gameObject.AddComponent<CardMotion>();
                            StartCoroutine(DelayedMoveToCompleted(cm, i * 0.05f));
                        }
                    }
                }

                foundation.Clear(); 
foundationCategories[index] = null; // Release the slot

                if (categoryParticles != null)
                {
                    categoryParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    categoryParticles.transform.position = foundationPiles[index].position;
                    categoryParticles.Play();
                }

                // Professional Polish: Subtle camera shake on completion
                if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(0.15f, 0.1f); 
                
                CheckWinCondition();

                // Unlock the next locked slot
                for (int i = 0; i < foundationPiles.Length; i++)
                {
                    Pile lockedPile = foundationPiles[i].GetComponent<Pile>();
                    if (lockedPile != null && lockedPile.isLocked)
                    {
                        lockedPile.SetLocked(false);
                        Debug.Log($"[Unlocking] Foundation Slot {i} has been unlocked by celestial progress!");
                        break;
                    }
                }
}
        }
    }

    private void CheckTableauCompletion(int index)
    {
        List<CardData> tableau = tableaus[index];
        if (tableau.Count == 0) return;

        CardCategory cat = tableau[0].category;
        int requiredSize = 0;
        if (!activeCategorySizes.TryGetValue(cat, out requiredSize)) return;

        if (tableau.Count >= requiredSize)
        {
            // Verify all cards in the column are of the same category (ranks no longer need to be sequential)
            for (int i = 0; i < requiredSize; i++)
            {
                if (tableau[i].category != cat) 
                    return;
            }

            Debug.Log($"[Completion] Tableau {index} completed category {cat}!");

            if (CollectionManager.Instance != null)
            {
                foreach (var card in tableau) CollectionManager.Instance.DiscoverFact(card.fact);
            }

            completedCardsCount += requiredSize;
            bool isFinal = completedCardsCount >= totalCardsInCurrentGame;
            
            List<Transform> cardsToMove = new List<Transform>();
            foreach (Transform child in tableauPiles[index])
            {
                if (child.name != "Highlight") cardsToMove.Add(child);
            }

            if (!isFinal)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCelestialCleanup();

                if (CelestialVFXManager.Instance != null)
                {
                    CelestialVFXManager.Instance.PlayCleanupEffect(cardsToMove, Vector3.zero);
                }
                else
                {
                    for (int i = 0; i < cardsToMove.Count; i++)
                    {
                        Transform card = cardsToMove[i];
                        card.SetParent(completedPile, true);
                        CardMotion cm = card.GetComponent<CardMotion>();
                        if (cm == null) cm = card.gameObject.AddComponent<CardMotion>();
                        StartCoroutine(DelayedMoveToCompleted(cm, i * 0.05f));
                    }
                }
            }

            tableau.Clear();
            
            if (categoryParticles != null)
            {
                categoryParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                categoryParticles.transform.position = tableauPiles[index].position;
                categoryParticles.Play();
            }

            CheckWinCondition();
        }
    }

    private IEnumerator DelayedMoveToCompleted(CardMotion cm, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cm != null && cm.gameObject.activeInHierarchy)
        {
            cm.MoveTo(Vector3.zero, 0.5f, () => {
                if (cm != null) cm.gameObject.SetActive(false); // Hide after reaching
            }, Quaternion.identity, Vector3.one);
        }
    }

    internal void InternalCheckWinCondition()
    {
        CheckWinCondition();
    }

    public List<Transform> GetAllCardTransforms()
    {
        List<Transform> transforms = new List<Transform>();
        if (CardRegistry.Instance != null)
        {
            foreach (var card in CardRegistry.Instance.GetAllCards())
            {
                if (card.gameObject.activeInHierarchy)
                    transforms.Add(card.transform);
            }
        }
        return transforms;
    }

    internal void CheckWinCondition()
{
        if (totalCardsInCurrentGame <= 0) return;

        // The win condition is that all category piles are completed (cleared from board).
        if (completedCardsCount >= totalCardsInCurrentGame)
        {
            ChangeState(new SolitaireWinState(this));
        }
    }

    internal int lastStarCount = 3;

    public void ShowVictoryUI(int stars, int coinsEarned)
    {
        if (victoryPanel == null) return;
        victoryPanel.SetActive(true);
        
        // Move Count Rolling Animation
        MoveCountRoller roller = victoryPanel.GetComponentInChildren<MoveCountRoller>(true);
        if (roller != null)
        {
            roller.RollTo(moveCount);
        }
        else
        {
            TMPro.TextMeshProUGUI moveText = victoryPanel.transform.Find("MoveCountText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (moveText != null) moveText.text = $"MOVES: {moveCount}";
        }

        // Coin Display
        Transform coinGroup = victoryPanel.transform.Find("CoinsEarnedText");
        if (coinGroup != null)
        {
            TMPro.TextMeshProUGUI coinText = coinGroup.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (coinText != null) coinText.text = $"+{coinsEarned}";
        }

        // Star Ignition Animation
        VictoryStarIgnition ignition = victoryPanel.GetComponentInChildren<VictoryStarIgnition>(true);
        if (ignition != null)
        {
            ignition.IgniteStars(stars);
        }
    }

    private CardDisplay GetTopCard(Transform pile)
    {
        for (int i = pile.childCount - 1; i >= 0; i--)
        {
            CardDisplay display = pile.GetChild(i).GetComponent<CardDisplay>();
            if (display != null) return display;
        }
        return null;
    }

    public void ShowHint()
    {
        // 1. Prioritize Foundation moves
        // From Tableaus
        for (int i = 0; i < tableauPiles.Length; i++)
        {
            CardDisplay display = GetTopCard(tableauPiles[i]);
            if (display != null)
            {
                GameObject cardObj = display.gameObject;
                CardData data = display.cardData;
                for (int f = 0; f < foundationPiles.Length; f++)
                {
                    if (foundationPiles[f].gameObject.activeSelf && CanMoveToFoundation(data, f))
                    {
                        cardObj.GetComponent<CardMotion>()?.Shake(); 
                        if (CelestialVFXManager.Instance != null) CelestialVFXManager.Instance.ShowStarlightPath(cardObj.transform.position, foundationPiles[f].position);
                        return;
                    }
                }
            }
        }

        // From Waste
        CardDisplay wasteDisplay = GetTopCard(wastePile);
        if (wasteDisplay != null)
        {
            GameObject cardObj = wasteDisplay.gameObject;
            CardData data = wasteDisplay.cardData;
            for (int f = 0; f < foundationPiles.Length; f++)
            {
                if (foundationPiles[f].gameObject.activeSelf && CanMoveToFoundation(data, f))
                {
                    cardObj.GetComponent<CardMotion>()?.Shake(); 
                    if (CelestialVFXManager.Instance != null) CelestialVFXManager.Instance.ShowStarlightPath(cardObj.transform.position, foundationPiles[f].position);
                    return;
                }
            }
        }

        // 2. Tableau moves
        for (int i = 0; i < tableauPiles.Length; i++)
        {
            CardDisplay display = GetTopCard(tableauPiles[i]);
            if (display != null)
            {
                GameObject cardObj = display.gameObject;
                CardData data = display.cardData;
                for (int k = 0; k < tableauPiles.Length; k++)
                {
                    if (i == k) continue;
                    if (MoveToTableauCheck(data, k)) 
                    { 
                        cardObj.GetComponent<CardMotion>()?.Shake(); 
                        if (CelestialVFXManager.Instance != null) CelestialVFXManager.Instance.ShowStarlightPath(cardObj.transform.position, tableauPiles[k].position);
                        return; 
                    }
                }
            }
        }

        // 3. Highlight Stock if nothing else
        stockPile.GetComponent<CardMotion>()?.Shake();
    }

    private bool MoveToTableauCheck(CardData cardData, int index)
    {
        return SolitaireRules.CanMoveToTableau(cardData, tableaus[index], currentDeckPool, activeTableauCapacity);
    }

    public void NextLevel()
    {
        levelManager.currentLevelIndex++;
        if (levelManager.currentLevelIndex >= levelManager.levels.Count)
        {
            levelManager.currentLevelIndex = 0; // Loop back or go to main menu
        }
        InitializeGame();
    }

    public void RestartRound()
    {
        InitializeGame(); // Just re-initialize same level
    }

public void ApplyGlobalTheme(VisualTheme theme)
{
    if (theme == null) return;

    // 1. Board
    GameObject board = GameObject.Find("Board");
    if (board != null)
    {
        UnityEngine.UI.Image img = board.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            // Clear the static background if we have a parallax one
            Transform parallax = board.transform.Find("ParallaxBackground");
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
                img.sprite = theme.boardSprite;
                img.color = theme.boardColor;
                img.type = UnityEngine.UI.Image.Type.Tiled;
            }
        }
    }

    // 1.5 Pile Silhouettes
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

    // 2. Panels
GameObject canvas = GameObject.Find("Canvas");
    if (canvas != null)
    {
        string[] panelNames = { "VictoryPanel", "MainMenuPanel", "LevelSelectPanel", "JournalPanel", "AbilityBar", "SettingsPanel", "MenuPanel", "CreditsPanel" };
foreach (string pName in panelNames)
        {
            Transform t = canvas.transform.Find(pName);
            if (t != null)
            {
                UnityEngine.UI.Image img = t.GetComponent<UnityEngine.UI.Image>();
                if (img == null) img = t.gameObject.AddComponent<UnityEngine.UI.Image>();
                        
                if (theme.panelSprite != null)
                {
                    img.sprite = theme.panelSprite;
                    img.type = UnityEngine.UI.Image.Type.Sliced;
                    img.color = Color.white;
                }
            }
        }

        // 3. Buttons
        if (theme.buttonSprite != null)
        {
            foreach (UnityEngine.UI.Button btn in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                UnityEngine.UI.Image img = btn.GetComponent<UnityEngine.UI.Image>();
                if (img == null) img = btn.gameObject.AddComponent<UnityEngine.UI.Image>();
                        
                img.sprite = theme.buttonSprite;
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.color = Color.white;
            }
        }
    }
}

public void UpdateMoveCountText()
{
    AuditCards();

    if (totalCardsText != null)
    {
        int remaining = totalCardsInCurrentGame - completedCardsCount;
        int inStock = stock != null ? stock.Count : 0;
        totalCardsText.text = $"Cards: {remaining} / {totalCardsInCurrentGame} (Stock: {inStock})";
    }

    int currentStars = 3;
    if (activeLevel != null)
    {
        if (moveCount <= activeLevel.threeStarMoves) currentStars = 3;
        else if (moveCount <= activeLevel.twoStarMoves) currentStars = 2;
        else currentStars = 1;
    }

    if (currentStars < lastStarCount)
    {
        if (moveCountText != null) StartCoroutine(FlashMoveCountRed());
    }
    lastStarCount = currentStars;

    if (moveCountText != null)
    {
        if (activeLevel != null)
        {
            moveCountText.text = $"Moves: {moveCount} / {activeLevel.threeStarMoves} (3*)";
        }
        else
        {
            moveCountText.text = $"Moves: {moveCount}";
        }
    }

    if (starProgressBar != null && activeLevel != null)
    {
        starProgressBar.UpdateProgress(moveCount, activeLevel.threeStarMoves, activeLevel.twoStarMoves);
    }

    if (currentState is SolitairePlayState)
    {
        CheckGameOver();
    }
}

public void CheckGameOver()
{
    if (completedCardsCount < totalCardsInCurrentGame && !CheckForPossibleMoves())
    {
        ChangeState(new SolitaireGameOverState(this));
    }
}

public bool CheckForPossibleMoves()
{
    // 1. Stock/Waste check
    if (stock.Count > 0 || waste.Count > 0) return true;

    // 2. Waste Top check
    CardDisplay wasteTop = GetTopCard(wastePile);
    if (wasteTop != null)
    {
        for (int f = 0; f < foundationPiles.Length; f++)
        {
            if (foundationPiles[f].gameObject.activeSelf && CanMoveToFoundation(wasteTop.cardData, f))
                return true;
        }
        for (int t = 0; t < tableauPiles.Length; t++)
        {
            if (MoveToTableauCheck(wasteTop.cardData, t))
                return true;
        }
    }

    // 3. Tableau to Foundation
    for (int i = 0; i < tableauPiles.Length; i++)
    {
        CardDisplay top = GetTopCard(tableauPiles[i]);
        if (top != null)
        {
            for (int f = 0; f < foundationPiles.Length; f++)
            {
                if (foundationPiles[f].gameObject.activeSelf && CanMoveToFoundation(top.cardData, f))
                    return true;
            }
        }
    }

    // 4. Tableau to Tableau
    for (int i = 0; i < tableauPiles.Length; i++)
    {
        // Find the bottom-most face-up card in this tableau
        CardDisplay bottomFaceUp = null;
        for (int j = 0; j < tableauPiles[i].childCount; j++)
        {
            CardDisplay d = tableauPiles[i].GetChild(j).GetComponent<CardDisplay>();
            if (d != null && d.IsFaceUp)
            {
                bottomFaceUp = d;
                break;
            }
        }

        if (bottomFaceUp == null) continue;

        for (int k = 0; k < tableauPiles.Length; k++)
        {
            if (i == k) continue;
            if (MoveToTableauCheck(bottomFaceUp.cardData, k))
            {
                // Meaningful if it reveals a face-down card or consolidates categories
                int firstFaceUpIndex = bottomFaceUp.transform.GetSiblingIndex();
                if (firstFaceUpIndex > 0) return true;
                if (tableaus[k].Count > 0) return true;
            }
        }
    }

    return false;
}

private IEnumerator FlashMoveCountRed()
{
    if (moveCountText == null) yield break;
    Color originalColor = moveCountText.color;
    moveCountText.color = Color.red;
    // Simple scale pop
    Vector3 originalScale = moveCountText.transform.localScale;
    moveCountText.transform.localScale = originalScale * 1.2f;
    
    yield return new WaitForSeconds(0.3f);
    
    moveCountText.color = originalColor;
    moveCountText.transform.localScale = originalScale;
}

public void Undo()
{
    solitaireHistory.Undo();
}

public CardDisplay FindCardDisplay(CardData data)
{
    if (CardRegistry.Instance != null)
    {
        foreach (var display in CardRegistry.Instance.GetAllCards())
        {
            if (display.cardData == data) return display;
        }
    }
    else
    {
        foreach (CardDisplay display in UnityEngine.Object.FindObjectsByType<CardDisplay>(FindObjectsInactive.Include))
        {
            if (display.cardData == data) return display;
        }
    }
    return null;
}

public Transform GetPileTransform(PileType type, int index)
{
    switch (type)
    {
        case PileType.Stock: return stockPile;
        case PileType.Waste: return wastePile;
        case PileType.Foundation: return foundationPiles[index];
        case PileType.Tableau: return tableauPiles[index];
        default: return null;
    }
}

public List<CardData> GetPileList(PileType type, int index)
{
    switch (type)
    {
        case PileType.Stock: return stock;
        case PileType.Waste: return waste;
        case PileType.Foundation: return foundations[index];
        case PileType.Tableau: return tableaus[index];
        default: return null;
    }
}

internal float GetVerticalSpacing(int cardCount, float containerHeight)
{
    if (cardCount <= 1) return baseCardSpacing;
float cardHeight = 210f; 
    if (cardPrefab != null) {
        RectTransform rt = cardPrefab.GetComponent<RectTransform>();
        if (rt != null) cardHeight = rt.rect.height;
    }
    float availableHeight = containerHeight - cardHeight - bottomPadding;
    float spacing = availableHeight / (cardCount - 1);
    return Mathf.Clamp(spacing, minCardSpacing, baseCardSpacing);
}

public void RefreshPileVisuals(PileType type, int index)
    {
        Transform pileTransform = GetPileTransform(type, index);
        if (pileTransform == null) return;

        int totalCards = 0;
        foreach (Transform t in pileTransform) if (t.GetComponent<CardDisplay>() != null) totalCards++;

        float tableauSpacing = (type == PileType.Tableau) ? GetVerticalSpacing(totalCards, tableausContainer.rect.height) : 0;

        int cardIndex = 0;
        foreach (Transform child in pileTransform)
        {
            if (child.GetComponent<CardDisplay>() == null)
            {
                if (child.name != "Aura") child.localPosition = Vector3.zero;
                continue;
            }
            
            if (type == PileType.Tableau)
            {
                child.localPosition = new Vector3(0, -cardIndex * tableauSpacing, 0);
            }
            else if (type == PileType.Waste)
            {
                int fanStartIndex = Mathf.Max(0, totalCards - 3);
                int fanRelativeIndex = Mathf.Max(0, cardIndex - fanStartIndex);
                child.localPosition = new Vector3(fanRelativeIndex * 20f, 0, 0);
            }
            else
            {
                child.localPosition = Vector3.zero;
            }
            cardIndex++;
        }
    }

public void AuditCards()
{
    if (totalCardsInCurrentGame <= 0) return;

    int stockCount = stock.Count;
    int wasteCount = waste.Count;
    int tableauCount = 0;
    if (tableaus != null) foreach (var t in tableaus) tableauCount += t.Count;
    int foundationCount = 0;
    if (foundations != null) foreach (var f in foundations) foundationCount += f.Count;
    int completed = completedCardsCount;

    int totalFound = stockCount + wasteCount + tableauCount + foundationCount + completed;
    
    if (totalFound != totalCardsInCurrentGame)
    {
        Debug.LogError($"[Card Audit] MISMATCH! Expected {totalCardsInCurrentGame}, Found {totalFound}. " +
                      $"S:{stockCount}, W:{wasteCount}, T:{tableauCount}, F:{foundationCount}, C:{completed}");
        
        // Detailed category breakdown
        Dictionary<CardCategory, List<int>> ranksFound = new Dictionary<CardCategory, List<int>>();
        void AddToFound(List<CardData> list) { 
            foreach(var c in list) { 
                if(!ranksFound.ContainsKey(c.category)) ranksFound[c.category] = new List<int>(); 
                ranksFound[c.category].Add(c.rank); 
            } 
        }
        
        AddToFound(stock); AddToFound(waste);
        if(tableaus!=null) foreach(var t in tableaus) AddToFound(t);
        if(foundations!=null) foreach(var f in foundations) AddToFound(f);
        
        foreach(var kvp in activeCategorySizes)
        {
            List<int> foundRanks = ranksFound.ContainsKey(kvp.Key) ? ranksFound[kvp.Key] : new List<int>();
            foundRanks.Sort();
            
            Debug.Log($"[Card Audit] Category {kvp.Key}: {foundRanks.Count} cards in play. Target: {kvp.Value}.");
            Debug.Log($"[Card Audit]   Ranks in play: {string.Join(", ", foundRanks)}");
            
            // Identify specific missing ranks
            for (int r = 1; r <= kvp.Value; r++)
            {
                if (!foundRanks.Contains(r))
                {
                    // Note: If completedCardsCount is > 0, we don't know which ranks were completed, 
                    // so we only log if it's suspicious.
                    if (completedCardsCount == 0)
                        Debug.LogError($"[Card Audit] MISSING: {kvp.Key} Rank {r} is completely missing from all piles!");
                }
            }
        }
    }

    // Check for stuck piles
    for (int i = 0; i < foundations.Length; i++)
    {
        CardCategory? cat = foundationCategories[i];
        if (cat.HasValue)
        {
            int required = 0;
            if (activeCategorySizes.TryGetValue(cat.Value, out required))
            {
                if (foundations[i].Count == required)
                {
                    Debug.LogWarning($"[Card Audit] Foundation {i} ({cat.Value}) is FULL ({required}/{required}) but not completed. Forcing completion check.");
                    InternalCheckCategoryCompletion(i);
                }
            }
        }
    }
}
}
