using System.Collections.Generic;
using UnityEngine;

internal static class SolitaireMoveAccounting
{
    public static void RecordExecute(SolitaireManager manager, out bool countedMove, out bool nebulaWasActive, out int nebulaMovesBefore)
    {
        nebulaWasActive = AbilitySystem.Instance != null && AbilitySystem.Instance.isNebulaActive;
        nebulaMovesBefore = AbilitySystem.Instance != null ? AbilitySystem.Instance.nebulaMovesRemaining : 0;
        countedMove = !nebulaWasActive;

        if (countedMove)
            manager.moveCount++;

        manager.UpdateMoveCountText();
        if (AbilitySystem.Instance != null)
            AbilitySystem.Instance.OnMoveMade();
    }

    public static void RecordUndo(SolitaireManager manager, bool countedMove, bool nebulaWasActive, int nebulaMovesBefore)
    {
        if (countedMove)
            manager.moveCount = Mathf.Max(0, manager.moveCount - 1);

        if (AbilitySystem.Instance != null)
            AbilitySystem.Instance.OnMoveUndone(nebulaWasActive, nebulaMovesBefore);

        manager.UpdateMoveCountText();
    }
}

public class MoveCardCommand : ISolitaireCommand
{
    private SolitaireManager manager;
    private List<CardData> cards;
    private Pile sourcePile;
    private Pile targetPile;
    private bool revealedCard;
    private CardData revealedCardData;
    private bool revealedMystery;
    private CardData revealedMysteryData;
    private CardCategory? prevTargetFoundationCategory;
    private CardCategory? prevSourceFoundationCategory;
    private int prevCompletedCount;
    private bool[] prevFoundationLocked;
    private bool offset;
    private bool countedMove;
    private bool nebulaWasActive;
    private int nebulaMovesBefore;
    private List<CardData> pileCardsSnapshot;

    public MoveCardCommand(SolitaireManager manager, List<CardData> cards, Pile source, Pile target, bool offset = false)
    {
        this.manager = manager;
        this.cards = new List<CardData>(cards);
        this.sourcePile = source;
        this.targetPile = target;
        this.offset = offset;
    }

    public void Execute()
    {
        prevTargetFoundationCategory = targetPile.type == PileType.Foundation
            ? manager.foundationCategories[targetPile.index]
            : null;
        prevSourceFoundationCategory = sourcePile.type == PileType.Foundation
            ? manager.foundationCategories[sourcePile.index]
            : null;
        prevCompletedCount = manager.completedCardsCount;
        prevFoundationLocked = SnapshotFoundationLocks();

        float spacing = 30f;
        if (targetPile.type == PileType.Tableau)
        {
            int finalCount = manager.GetPileList(targetPile.type, targetPile.index).Count + cards.Count;
            spacing = manager.GetVerticalSpacing(finalCount, manager.tableausContainer.rect.height);
        }

        foreach (var card in cards)
        {
            manager.GetPileList(targetPile.type, targetPile.index).Add(card);

            CardDisplay display = manager.FindCardDisplay(card);
            if (display != null)
            {
                display.transform.SetParent(targetPile.transform, true);

                Vector3 targetLocalPos = Vector3.zero;
                if (offset)
                    targetLocalPos = new Vector3(0, -(manager.GetPileList(targetPile.type, targetPile.index).Count - 1) * spacing, 0);

                CardMotion motion = display.GetComponent<CardMotion>();
                if (motion == null) motion = display.gameObject.AddComponent<CardMotion>();

                motion.MoveTo(targetLocalPos, 0.25f, () =>
                {
                    if (display != null)
                    {
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

        foreach (var card in cards)
        {
            manager.GetPileList(sourcePile.type, sourcePile.index).Remove(card);
        }

        manager.RefreshPileVisuals(sourcePile.type, sourcePile.index);

        if (sourcePile.type == PileType.Foundation && manager.GetPileList(sourcePile.type, sourcePile.index).Count == 0)
        {
            manager.foundationCategories[sourcePile.index] = null;
        }

        int countBefore = manager.completedCardsCount;
        pileCardsSnapshot = new List<CardData>(manager.GetPileList(targetPile.type, targetPile.index));

        if (targetPile.type == PileType.Foundation)
        {
            if (manager.foundationCategories[targetPile.index] == null)
                manager.foundationCategories[targetPile.index] = cards[0].category;

            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.PulseBloom(2.0f, 0.3f);
                JuiceManager.Instance.FlashColor(Color.white, 0.3f);
            }
        }

        manager.CheckAllCompletions();

        bool completed = manager.completedCardsCount > countBefore;
        if (!completed) pileCardsSnapshot = null;

        if (sourcePile.type == PileType.Tableau && sourcePile.transform.childCount > 0)
        {
            Transform lastChild = sourcePile.transform.GetChild(sourcePile.transform.childCount - 1);
            CardDisplay nextDisplay = lastChild.GetComponent<CardDisplay>();
            if (nextDisplay != null)
            {
                if (!nextDisplay.IsFaceUp)
                {
                    revealedCard = true;
                    revealedCardData = nextDisplay.cardData;
                    nextDisplay.Flip(true);
                    if (lastChild.GetComponent<CardDraggable>() == null)
                        lastChild.gameObject.AddComponent<CardDraggable>();
                }

                if (nextDisplay.IsMystery)
                {
                    revealedMystery = true;
                    revealedMysteryData = nextDisplay.cardData;
                    nextDisplay.RevealMystery();
                }
            }
        }

        SolitaireMoveAccounting.RecordExecute(manager, out countedMove, out nebulaWasActive, out nebulaMovesBefore);
    }

    public void Undo()
    {
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
                }
            }
        }

        if (revealedMystery && revealedMysteryData != null)
        {
            CardDisplay mysteryDisplay = manager.FindCardDisplay(revealedMysteryData);
            if (mysteryDisplay != null)
                mysteryDisplay.SetMystery(true);
        }

        if (revealedCard && revealedCardData != null)
        {
            CardDisplay revDisplay = manager.FindCardDisplay(revealedCardData);
            if (revDisplay != null)
            {
                revDisplay.Flip(false);
                CardDraggable drag = revDisplay.GetComponent<CardDraggable>();
                if (drag != null)
                    Object.Destroy(drag);
            }
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            CardDisplay display = manager.FindCardDisplay(card);
            manager.GetPileList(targetPile.type, targetPile.index).Remove(card);
            manager.GetPileList(sourcePile.type, sourcePile.index).Add(card);

            if (display != null)
                display.transform.SetParent(sourcePile.transform, true);
        }

        manager.RefreshPileVisuals(sourcePile.type, sourcePile.index);
        manager.RefreshPileVisuals(targetPile.type, targetPile.index);

        if (targetPile.type == PileType.Foundation)
            manager.foundationCategories[targetPile.index] = prevTargetFoundationCategory;

        if (sourcePile.type == PileType.Foundation)
            manager.foundationCategories[sourcePile.index] = prevSourceFoundationCategory;

        manager.completedCardsCount = prevCompletedCount;
        RestoreFoundationLocks();

        SolitaireMoveAccounting.RecordUndo(manager, countedMove, nebulaWasActive, nebulaMovesBefore);
    }

    private bool[] SnapshotFoundationLocks()
    {
        bool[] locks = new bool[manager.foundationPiles.Length];
        for (int i = 0; i < manager.foundationPiles.Length; i++)
        {
            Pile pile = manager.foundationPiles[i].GetComponent<Pile>();
            locks[i] = pile != null && pile.isLocked;
        }
        return locks;
    }

    private void RestoreFoundationLocks()
    {
        if (prevFoundationLocked == null) return;

        for (int i = 0; i < prevFoundationLocked.Length && i < manager.foundationPiles.Length; i++)
        {
            Pile pile = manager.foundationPiles[i].GetComponent<Pile>();
            if (pile != null)
                pile.SetLocked(prevFoundationLocked[i]);
        }
    }
}

public class DrawStockCommand : ISolitaireCommand
{
    private SolitaireManager manager;
    private CardData card;
    private bool countedMove;
    private bool nebulaWasActive;
    private int nebulaMovesBefore;

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
            if (CardPool.Instance != null)
                cardObj = CardPool.Instance.GetCard(manager.wastePile);
            else
                cardObj = Object.Instantiate(manager.cardPrefab, manager.wastePile);

            cardObj.GetComponent<CardDisplay>().SetCard(card);
        }

        cardObj.transform.SetParent(manager.wastePile, true);
        CardDisplay display = cardObj.GetComponent<CardDisplay>();
        display.Flip(true);

        VisualTheme theme = manager.currentLevelTheme;
        if (theme == null && manager.activeLevel != null)
            theme = manager.activeLevel.theme;
        display.ApplyTheme(theme);

        manager.RefreshPileVisuals(PileType.Waste, 0);

        CardMotion motion = cardObj.GetComponent<CardMotion>();
        if (motion != null)
        {
            Vector3 targetPos = cardObj.transform.localPosition;
            cardObj.transform.localPosition = Vector3.zero;
            motion.MoveTo(targetPos, 0.25f, null, Quaternion.identity, Vector3.one);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCardFlick();
        if (cardObj.GetComponent<CardDraggable>() == null)
            cardObj.AddComponent<CardDraggable>();

        SolitaireMoveAccounting.RecordExecute(manager, out countedMove, out nebulaWasActive, out nebulaMovesBefore);
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
            CardDraggable drag = display.GetComponent<CardDraggable>();
            if (drag != null)
                Object.Destroy(drag);
            display.transform.localPosition = Vector3.zero;
        }

        manager.RefreshPileVisuals(PileType.Waste, 0);
        SolitaireMoveAccounting.RecordUndo(manager, countedMove, nebulaWasActive, nebulaMovesBefore);
    }
}

public class ResetStockCommand : ISolitaireCommand
{
    private SolitaireManager manager;
    private List<CardData> wasteSnapshot;
    private List<CardData> stockSnapshot;
    private bool countedMove;
    private bool nebulaWasActive;
    private int nebulaMovesBefore;

    public ResetStockCommand(SolitaireManager manager)
    {
        this.manager = manager;
        this.wasteSnapshot = new List<CardData>(manager.waste);
        this.stockSnapshot = new List<CardData>(manager.stock);
    }

    public void Execute()
    {
        manager.stock.AddRange(manager.waste);
        manager.waste.Clear();

        SolitaireMoveAccounting.RecordExecute(manager, out countedMove, out nebulaWasActive, out nebulaMovesBefore);
    }

    public void Undo()
    {
        manager.CancelStockReset();

        manager.stock.Clear();
        manager.stock.AddRange(stockSnapshot);
        manager.waste.Clear();
        manager.waste.AddRange(wasteSnapshot);

        manager.RebuildWasteVisuals(wasteSnapshot);
        SolitaireMoveAccounting.RecordUndo(manager, countedMove, nebulaWasActive, nebulaMovesBefore);
    }
}
