using UnityEngine;

public abstract class AbilityEffect : ScriptableObject
{
    public string abilityName;
    [TextArea]
    public string description;
    public Sprite icon;

    public abstract void Execute(SolitaireManager manager, AbilitySystem system);
}
