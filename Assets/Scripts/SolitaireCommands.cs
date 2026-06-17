using System.Collections.Generic;
using UnityEngine;

public class MoveCardCommand : ISolitaireCommand
{
    private SolitaireManager manager;
    private List<CardData> cards;
    private Pile sourcePile;
    private Pile targetPile;
    private bool revealedCard;
    private CardData revealedCardData;
    private CardCategory? prevFoundationCategory;
    private int prevCompletedCount;
    private bool offset;

    public MoveCardCommand(SolitaireManager manager, List<CardData> cards, Pile source, Pile target, bool offset = false)
    {
        this.manager = manager;
        this.cards = new List<CardData>(cards);
        this.sourcePile = source;
        this.targetPile = target;
        this.offset = offset;
    }

    private List<CardData> pileCardsSnapshot; // For undoing completion in foundations or tableaus

    public void Execute()
    {
        prevFoundationCategory = targetPile.type == PileType.Foundation ? manager.foundationCategories[targetPile.index] : null;
        prevCompletedCount = manager.completedCardsCount;

        float spacing = 30f;
        if (targetPile.type == PileType.Tableau)
        {
            int finalCount = manager.GetPileList(targetPile.type, targetPile.index).Count + cards.Count;
            spacing = manager.GetVerticalSpacing(finalCount, manager.tableausContainer.rect.height);
        }

        // Perform the move logic
        foreach (var card in cards)
        {
            // IMPORTANT: Always add to the target list regardless of visual presence
            manager.GetPileList(targetPile.type, targetPile.index).Add(card);

            CardDisplay display = manager.FindCardDisplay(card);
            if (display != null)
            {
                // Logic moved from SolitaireManager.PlaceInPile
                display.transform.SetParent(targetPile.transform, true);
                
                Vector3 targetLocalPos = Vector3.zero;
                if (offset) targetLocalPos = new Vector3(0, -(manager.GetPileList(targetPile.type, targetPile.index).Count - 1) * spacing, 0);

                CardMotion motion = display.GetComponent<CardMotion>();
if (motion == null) motion = display.gameObject.AddComponent<CardMotion>();
                
                motion.MoveTo(targetLocalPos, 0.25f, () => {
                    if (display != null) {
                        display.transform.SetParent(targetPile.transform, true);
                        display.transform.localPosition = targetLocalPos;
                    }
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayCardSnap();
                }, Quaternion.identity, Vector3.one);
            }
            else
            {
                Debug.LogWarning($"[MoveCardCommand] Card visual for {card.category} {card.rank} not found. List updated, but card is visually missing.");
            }
        }

        // Logic from RemoveFromSource
        foreach (var card in cards)
        {
            manager.GetPileList(sourcePile.type, sourcePile.index).Remove(card);
        }

        // Refresh source pile visuals (important for Waste fanning)
        manager.RefreshPileVisuals(sourcePile.type, sourcePile.index);

        // Fix: If foundation becomes empty, clear the category so it can be reused
        if (sourcePile.type == PileType.Foundation && manager.GetPileList(sourcePile.type, sourcePile.index).Count == 0)
        {
            manager.foundationCategories[sourcePile.index] = null;
        }

        // Completion checks
        int countBefore = manager.completedCardsCount;
        
        // Take a snapshot of the target pile BEFORE completion clears it
        pileCardsSnapshot = new List<CardData>(manager.GetPileList(targetPile.type, targetPile.index));

        if (targetPile.type == PileType.Foundation)
        {
            if (manager.foundationCategories[targetPile.index] == null)
                manager.foundationCategories[targetPile.index] = cards[0].category;
            
            // Trigger Juice
            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.PulseBloom(2.0f, 0.3f);
                JuiceManager.Instance.FlashColor(Color.white, 0.3f);
            }
        }

        manager.CheckAllCompletions();
            
        bool completed = manager.completedCardsCount > countBefore;
        if (!completed) pileCardsSnapshot = null; 

        // Reveal logic
if (sourcePile.type == PileType.Tableau && sourcePile.transform.childCount > 0)
        {
            Transform lastChild = sourcePile.transform.GetChild(sourcePile.transform.childCount - 1);
            CardDisplay nextDisplay = lastChild.GetComponent<CardDisplay>();
            if (nextDisplay != null && !nextDisplay.IsFaceUp)
            {
                revealedCard = true;
                revealedCardData = nextDisplay.cardData;
                nextDisplay.Flip(true);
                if (lastChild.GetComponent<CardDraggable>() == null)
                    lastChild.gameObject.AddComponent<CardDraggable>();
            }
        }

        if (AbilitySystem.Instance == null || !AbilitySystem.Instance.isNebulaActive)
        {
            manager.moveCount++;
        }
        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.OnMoveMade();
    }

    public void Undo()
    {
        // Restore from completion if it happened
        if (pileCardsSnapshot != null)
        {
            manager.GetPileList(targetPile.type, targetPile.index).AddRange(pileCardsSnapshot);
            foreach (var cData in pileCardsSnapshot)
            {
                CardDisplay cd = manager.FindCardDisplay(cData);
                if (cd != null)
                {
                    cd.gameObject.SetActive(true);
                    cd.transform.SetParent(targetPile.transform, true);
                    // Position is fixed by RefreshPileVisuals below
                }
            }
        }

        // Reverse reveal
        if (revealedCard && revealedCardData != null)
{
            CardDisplay revDisplay = manager.FindCardDisplay(revealedCardData);
            if (revDisplay != null)
            {
                revDisplay.Flip(false);
                if (revDisplay.GetComponent<CardDraggable>() != null)
                    Object.Destroy(revDisplay.GetComponent<CardDraggable>());
            }
        }

        // Move cards back in original order to maintain hierarchy
        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            CardDisplay display = manager.FindCardDisplay(card);
            if (display != null)
            {
                manager.GetPileList(targetPile.type, targetPile.index).Remove(card);
                manager.GetPileList(sourcePile.type, sourcePile.index).Add(card);

                display.transform.SetParent(sourcePile.transform, true);
            }
        }

        manager.RefreshPileVisuals(sourcePile.type, sourcePile.index);
        manager.RefreshPileVisuals(targetPile.type, targetPile.index);

        if (targetPile.type == PileType.Foundation)
        {
            manager.foundationCategories[targetPile.index] = prevFoundationCategory;
        }
        
        manager.completedCardsCount = prevCompletedCount;

        if (AbilitySystem.Instance == null || !AbilitySystem.Instance.isNebulaActive)
        {
            manager.moveCount++;
        }
        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.OnMoveMade();
    }
}

public class DrawStockCommand : ISolitaireCommand
{
    private SolitaireManager manager;
    private CardData card;

    public DrawStockCommand(SolitaireManager manager, CardData card)
    {
        this.manager = manager;
        this.card = card;
    }

    public void Execute()
    {
        manager.stock.Remove(card);
        manager.waste.Add(card);
        
        GameObject cardObj = manager.FindCardDisplay(card)?.gameObject;
        if (cardObj == null)
        {
            cardObj = Object.Instantiate(manager.cardPrefab, manager.wastePile);
            cardObj.GetComponent<CardDisplay>().SetCard(card);
        }

        cardObj.transform.SetParent(manager.wastePile, true);
        CardDisplay display = cardObj.GetComponent<CardDisplay>();
        display.Flip(true);
        display.ApplyTheme(manager.currentLevelTheme);
        
        // Refresh all waste visuals to ensure correct fanning for older cards
        manager.RefreshPileVisuals(PileType.Waste, 0);
        
        // Find the specific motion for the new card and animate it to its refreshed position
        CardMotion motion = cardObj.GetComponent<CardMotion>();
        if (motion != null)
        {
            Vector3 targetPos = cardObj.transform.localPosition;
            cardObj.transform.localPosition = Vector3.zero; // Start from center or stock
            motion.MoveTo(targetPos, 0.25f, null, Quaternion.identity, Vector3.one);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCardFlick();
        if (cardObj.GetComponent<CardDraggable>() == null) cardObj.AddComponent<CardDraggable>();

        if (AbilitySystem.Instance == null || !AbilitySystem.Instance.isNebulaActive)
        {
            manager.moveCount++;
        }
        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.OnMoveMade();
    }

    public void Undo()
    {
        manager.waste.Remove(card);
        manager.stock.Insert(0, card);
        
        CardDisplay display = manager.FindCardDisplay(card);
        if (display != null)
        {
            display.transform.SetParent(manager.stockPile, true);
            display.Flip(false);
            if (display.GetComponent<CardDraggable>() != null)
                Object.Destroy(display.GetComponent<CardDraggable>());
            display.transform.localPosition = Vector3.zero;
        }

        if (AbilitySystem.Instance == null || !AbilitySystem.Instance.isNebulaActive)
        {
            manager.moveCount++;
        }
        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.OnMoveMade();
    }
}

public class ResetStockCommand : ISolitaireCommand
{
    private SolitaireManager manager;
    private List<CardData> wasteSnapshot;

    public ResetStockCommand(SolitaireManager manager)
    {
        this.manager = manager;
        this.wasteSnapshot = new List<CardData>(manager.waste);
    }

    public void Execute()
    {
        manager.stock.AddRange(manager.waste);
        manager.waste.Clear();
        // Visuals are handled by coroutine in manager for now to keep it smooth
        // but we record the state.
        
        if (AbilitySystem.Instance == null || !AbilitySystem.Instance.isNebulaActive)
        {
            manager.moveCount++;
        }
        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.OnMoveMade();
    }

    public void Undo()
    {
        manager.stock.Clear(); // Not entirely correct if stock had something, but usually it's empty
        manager.waste.AddRange(wasteSnapshot);
        
        // Re-instantiate cards in waste
        foreach (var card in wasteSnapshot)
        {
            GameObject cardObj = Object.Instantiate(manager.cardPrefab, manager.wastePile);
            CardDisplay display = cardObj.GetComponent<CardDisplay>();
            display.SetCard(card);
            display.Flip(true);
            display.ApplyTheme(manager.currentLevelTheme);
            cardObj.AddComponent<CardDraggable>();
        }
        
        if (AbilitySystem.Instance == null || !AbilitySystem.Instance.isNebulaActive)
        {
            manager.moveCount++;
        }
        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.OnMoveMade();
    }
}
