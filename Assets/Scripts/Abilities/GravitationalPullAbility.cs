using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GravitationalPullAbility", menuName = "Solitaire/Abilities/Gravitational Pull")]
public class GravitationalPullAbility : AbilityEffect
{
    public override void Execute(SolitaireManager manager, AbilitySystem system)
    {
        bool movedAny = false;
        bool foundInLoop = true;

        while (foundInLoop)
        {
            foundInLoop = false;
            
            // Check Waste
            if (manager.waste.Count > 0)
            {
                CardData card = manager.waste[manager.waste.Count - 1];
                if (TryMoveToAnyFoundation(card, manager.wastePile.GetComponent<Pile>(), manager))
                {
                    movedAny = true;
                    foundInLoop = true;
                    continue;
                }
            }

            // Check Tableaus
            for (int i = 0; i < manager.tableauPiles.Length; i++)
            {
                if (manager.tableaus[i].Count > 0)
                {
                    CardData card = manager.tableaus[i][manager.tableaus[i].Count - 1];
                    if (TryMoveToAnyFoundation(card, manager.tableauPiles[i].GetComponent<Pile>(), manager))
                    {
                        movedAny = true;
                        foundInLoop = true;
                        break;
                    }
                }
            }
        }

        if (movedAny)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCelestialCleanup();
            if (CelestialVFXManager.Instance != null) CelestialVFXManager.Instance.PlayGravityEffect(manager.stockPile.position);
        }
        else
        {
            // Refund handled in AbilitySystem
            system.RefundLastAbility();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
        }
    }

    private bool TryMoveToAnyFoundation(CardData card, Pile source, SolitaireManager manager)
    {
        for (int f = 0; f < manager.foundationPiles.Length; f++)
        {
            if (manager.foundationPiles[f].gameObject.activeSelf && !manager.foundationPiles[f].GetComponent<Pile>().isLocked)
            {
                if (SolitaireRules.CanMoveToFoundation(card, manager.foundations[f], manager.foundationCategories[f]))
                {
                    List<CardData> stack = new List<CardData> { card };
                    manager.solitaireHistory.PushAndExecute(new MoveCardCommand(manager, stack, source, manager.foundationPiles[f].GetComponent<Pile>()));
                    return true;
                }
            }
        }
        return false;
    }
}
