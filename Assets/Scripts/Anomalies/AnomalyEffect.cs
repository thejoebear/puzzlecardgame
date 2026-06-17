using UnityEngine;

public abstract class AnomalyEffect : ScriptableObject
{
    public AnomalyType anomalyType;
    public Color themeColor = Color.white;
    public Sprite effectSprite;

    public abstract void OnCardPlayed(SolitaireManager manager, CardDisplay card, Pile targetPile);
}
