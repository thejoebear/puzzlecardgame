using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AstralSightAbility", menuName = "Solitaire/Abilities/Astral Sight")]
public class AstralSightAbility : AbilityEffect
{
    public override void Execute(SolitaireManager manager, AbilitySystem system)
    {
        bool revealedAny = false;

        if (CardRegistry.Instance != null)
        {
            foreach (var display in CardRegistry.Instance.GetAllCards())
            {
                if (display.IsMystery && display.gameObject.activeInHierarchy)
                {
                    display.RevealMystery();
                    revealedAny = true;
                    if (CelestialVFXManager.Instance != null)
                    {
                        CelestialVFXManager.Instance.PlayFocusEffect(display.transform.position);
                    }
                }
            }
        }

        if (!revealedAny)
        {
            system.RefundLastAbility();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayError();
        }
    }
}
