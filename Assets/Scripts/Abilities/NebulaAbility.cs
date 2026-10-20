using UnityEngine;

[CreateAssetMenu(fileName = "NebulaAbility", menuName = "Solitaire/Abilities/Nebula")]
public class NebulaAbility : AbilityEffect
{
    public int movesCount = 5;

    public override void Execute(SolitaireManager manager, AbilitySystem system)
    {
        system.isNebulaActive = true;
        system.nebulaMovesRemaining = movesCount;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCelestialCleanup();
        if (CelestialVFXManager.Instance != null) CelestialVFXManager.Instance.PlayNebulaEffect(true);
        if (CelestialJuiceManager.Instance != null) CelestialJuiceManager.Instance.SetNebulaJuice(true, 1.0f);
    }
}
