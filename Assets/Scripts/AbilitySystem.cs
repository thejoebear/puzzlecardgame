using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class AbilitySlot
{
    public AbilityEffect effect;
    public TextMeshProUGUI chargeText;
    public UIButtonJuice buttonJuice;
}

public class AbilitySystem : MonoBehaviour
{
    public static AbilitySystem Instance;

    private SolitaireManager manager;

    [Header("Abilities")]
    public List<AbilitySlot> abilities = new List<AbilitySlot>();
    public int chargeCost = 5;
    public TextMeshProUGUI hudCoinText;

    [Header("State")]
    public bool isNebulaActive = false;
    public int nebulaMovesRemaining = 0;

    private AbilitySlot lastUsedSlot;

    void Awake()
    {
        Instance = this;
        manager = GetComponent<SolitaireManager>();
    }

    void Start()
    {
        UpdateHUDCoins();
    }

    public void UpdateHUDCoins()
    {
        if (hudCoinText != null && ProgressionManager.Instance != null)
        {
            hudCoinText.text = ProgressionManager.Instance.GetCoins().ToString();
        }
    }

    public void Initialize(LevelData level)
    {
        isNebulaActive = false;
        nebulaMovesRemaining = 0;
        UpdateUI();
        UpdateHUDCoins();
    }

    public void UseAbility(int index)
    {
        if (index < 0 || index >= abilities.Count) return;
        
        AbilitySlot slot = abilities[index];
        if (slot.effect == null) return;

        // If nebula is active, we don't want to spend more coins to "use" it again if it's already running
        // Or if the user wants to overlap? Usually abilities are one-off or toggle.
        // Nebula is a state.
        if (slot.effect is NebulaAbility && isNebulaActive) return;

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.SpendCoins(chargeCost))
        {
            lastUsedSlot = slot;
            slot.effect.Execute(manager, this);
            
            if (slot.effect is NebulaAbility)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.TransitionToCosmic(true, 1.5f);
            }

            UpdateUI();
            UpdateHUDCoins();
        }
        else
        {
            // Not enough coins feedback
            if (slot.buttonJuice != null) slot.buttonJuice.Shake();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
        }
    }

    public void RefundLastAbility()
    {
        if (lastUsedSlot != null)
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.AddCoins(chargeCost);
                UpdateHUDCoins();
            }
            UpdateUI();
            lastUsedSlot = null;
        }
    }

    public void OnMoveMade()
    {
        if (isNebulaActive)
        {
            nebulaMovesRemaining--;
            if (nebulaMovesRemaining <= 0)
            {
                isNebulaActive = false;
                if (CelestialVFXManager.Instance != null) CelestialVFXManager.Instance.PlayNebulaEffect(false);
                if (AudioManager.Instance != null) AudioManager.Instance.TransitionToCosmic(false, 2.0f);
                if (CelestialJuiceManager.Instance != null) CelestialJuiceManager.Instance.SetNebulaJuice(false, 2.0f);
            }
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        foreach (var slot in abilities)
        {
            if (slot.chargeText != null)
            {
                // Special case for nebula move counter display
                if (slot.effect is NebulaAbility && isNebulaActive)
                {
                    slot.chargeText.text = $"({nebulaMovesRemaining})";
                }
                else
                {
                    slot.chargeText.text = $"{chargeCost}★";
                }
            }

            if (slot.buttonJuice != null)
            {
                bool canAfford = ProgressionManager.Instance != null && ProgressionManager.Instance.GetCoins() >= chargeCost;
                slot.buttonJuice.SetPulse(canAfford && !isNebulaActive);
            }
        }
    }

    // Legacy methods for UI button compatibility if needed, but better to call UseAbility(index)
    [System.Obsolete("Use UseAstralSight instead")]
    public void UseFocus() => UseAstralSight();
    public void UseAstralSight() => UseAbility(0);
    [System.Obsolete("Use UseGravitationalPull instead")]
    public void UseCelestialPull() => UseGravitationalPull();
    public void UseGravitationalPull() => UseAbility(1);
    public void UseNebula() => UseAbility(2);
}
