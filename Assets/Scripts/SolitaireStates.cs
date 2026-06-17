using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SolitaireStateBase : ISolitaireState
{
    protected SolitaireManager manager;

    public SolitaireStateBase(SolitaireManager manager)
    {
        this.manager = manager;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}

public class SolitaireInitState : SolitaireStateBase
{
    private LevelData customLevel;

    public SolitaireInitState(SolitaireManager manager, LevelData customLevel = null) : base(manager)
    {
        this.customLevel = customLevel;
    }

    public override void Enter()
    {
        if (CelestialVFXManager.Instance != null) 
        {
            CelestialVFXManager.Instance.ClearWinConstellation();
            CelestialVFXManager.Instance.FadeHUD(1, 0.2f);
        }

        manager.StopAllCoroutines();
manager.ClearBoard();
manager.solitaireHistory.Clear();
        if (manager.victoryPanel != null) manager.victoryPanel.SetActive(false);
        if (manager.gameOverPanel != null) manager.gameOverPanel.SetActive(false);

        int activeSlotLimit;

        if (customLevel != null)
        {
            manager.activeLevel = customLevel;
            activeSlotLimit = manager.activeLevel.foundationSlots;
            manager.totalCardsInCurrentGame = 0;
            manager.currentDeckPool = new List<CardData>();
            manager.activeCategorySizes = new Dictionary<CardCategory, int>();
            
            manager.levelManager.LoadLevel(-1, out _, out manager.currentDeckPool, out manager.activeCategorySizes, out activeSlotLimit, customLevel);
        }
        else
        {
            manager.levelManager.LoadLevel(manager.levelManager.currentLevelIndex, out manager.activeLevel, out manager.currentDeckPool, out manager.activeCategorySizes, out activeSlotLimit);
        }

        manager.currentLevelTheme = manager.activeLevel != null ? manager.activeLevel.theme : null;
        if (manager.currentLevelTheme == null)
        {
            if (manager.levelManager != null && manager.levelManager.levels != null)
            {
                foreach (var lvl in manager.levelManager.levels)
                {
                    if (lvl != null && lvl.theme != null)
                    {
                        manager.currentLevelTheme = lvl.theme;
                        break;
                    }
                }
            }
        }
        manager.activeTableauCapacity = manager.activeLevel != null ? manager.activeLevel.tableauCapacity : 0;
        manager.activeUseMysteryCards = false; // Mystery card feature disabled
        manager.activeMysteryChance = 0f; // Mystery card feature disabled
        manager.activeLockedSlots = manager.activeLevel != null ? manager.activeLevel.lockedFoundationSlots : 0;

        manager.ApplyGlobalTheme(manager.currentLevelTheme);

        if (manager.moveCountText != null) manager.moveCountText.text = "Moves: 0";

        List<CardData> solveDeck = new List<CardData>(manager.currentDeckPool);
        int attempts = 0;
        bool solvable = false;
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        while (!solvable && attempts < 5000)
        {
            attempts++;
            manager.ShuffleList(solveDeck);
            solvable = SolitaireSolver.FastSimulation(solveDeck, activeSlotLimit, manager.foundationPiles.Length, manager.currentDeckPool.Count, manager.activeCategorySizes, manager.currentDeckPool, manager.activeTableauCapacity, manager.activeLockedSlots);
        }
        sw.Stop();
        Debug.Log($"Level initialization: Solvable={solvable} in {attempts} attempts ({sw.ElapsedMilliseconds}ms).");

        manager.completedCardsCount = 0;
        manager.moveCount = 0;
        manager.totalCardsInCurrentGame = manager.currentDeckPool.Count;
        manager.foundationCategories = new CardCategory?[manager.foundationPiles.Length];
        manager.stock.Clear();
        manager.stock.AddRange(solveDeck);

        manager.foundations = new List<CardData>[manager.foundationPiles.Length];
        for (int i = 0; i < manager.foundations.Length; i++) manager.foundations[i] = new List<CardData>();
        manager.tableaus = new List<CardData>[7];
        for (int i = 0; i < 7; i++) manager.tableaus[i] = new List<CardData>();

        List<CardData> tempStock = new List<CardData>(manager.stock);
        for (int i = 0; i < 7; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                if (tempStock.Count > 0)
                {
                    manager.tableaus[i].Add(tempStock[0]);
                    tempStock.RemoveAt(0);
                }
            }
        }
        manager.stock = tempStock;

        manager.SetupPileComponents(activeSlotLimit);
        if (manager.GetComponent<AbilitySystem>() != null) manager.GetComponent<AbilitySystem>().Initialize(manager.activeLevel);
        manager.UpdateMoveCountText();
        
        manager.ChangeState(new SolitaireDealingState(manager));
    }
}

public class SolitaireDealingState : SolitaireStateBase
{
    public SolitaireDealingState(SolitaireManager manager) : base(manager) { }

    public override void Enter()
    {
        manager.StartCoroutine(manager.InternalDealRoutine());
    }
}

public class SolitairePlayState : SolitaireStateBase
{
    public SolitairePlayState(SolitaireManager manager) : base(manager) { }

    public override void Update()
    {
        manager.CheckWinCondition();
        // CheckGameOver is called inside UpdateMoveCountText in manager, 
        // which is fine as long as manager knows it's in PlayState.
    }
}

public class SolitaireWinState : SolitaireStateBase
{
    public SolitaireWinState(SolitaireManager manager) : base(manager) { }

    public override void Enter()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWin();

        // Gather all cards and play the celestial sequence
        if (CelestialVFXManager.Instance != null && manager.activeLevel != null)
        {
            List<Transform> cards = manager.GetAllCardTransforms();
            CelestialVFXManager.Instance.PlayWinConstellation(cards, manager.activeLevel.constellationShape);
        }

        if (manager.victoryPanel != null && !manager.victoryPanel.activeSelf)
        {
            // Delay the victory panel slightly to allow the sequence to be seen
            manager.StartCoroutine(DelayedVictoryPanel());
        }
    }

    private IEnumerator DelayedVictoryPanel()
    {
        float delay = 2.0f;
        if (CelestialVFXManager.Instance != null) delay = CelestialVFXManager.Instance.winSequenceDelay;
        yield return new WaitForSeconds(delay);

        manager.victoryPanel.SetActive(true);
        if (manager.winParticles != null) manager.winParticles.Play();
        
        int stars = 0;
        if (manager.activeLevel != null)
        {
            if (manager.moveCount <= manager.activeLevel.threeStarMoves) stars = 3;
            else if (manager.moveCount <= manager.activeLevel.twoStarMoves) stars = 2;
            else stars = 1;
        }

        int coinsEarned = stars * 5;

        if (ProgressionManager.Instance != null && manager.levelManager.currentLevelIndex >= 0)
        {
            ProgressionManager.Instance.UnlockNextLevel(manager.levelManager.currentLevelIndex);
            ProgressionManager.Instance.SaveStars(manager.levelManager.currentLevelIndex, stars);
            ProgressionManager.Instance.AddCoins(coinsEarned);
        }

        if (manager.levelManager.currentLevelIndex == -1)
        {
            PlayerPrefs.SetInt("DailyArchive_" + DateTime.Today.ToString("yyyyMMdd"), 1);
            PlayerPrefs.Save();
        }

        if (CelestialVFXManager.Instance != null)
        {
            CelestialVFXManager.Instance.RefreshBackgroundStars();
        }

        manager.ShowVictoryUI(stars, coinsEarned);
    }
}

public class SolitaireGameOverState : SolitaireStateBase
{
    public SolitaireGameOverState(SolitaireManager manager) : base(manager) { }

    public override void Enter()
    {
        if (manager.gameOverPanel != null && !manager.gameOverPanel.activeSelf)
        {
            manager.gameOverPanel.SetActive(true);
        }
    }
}
