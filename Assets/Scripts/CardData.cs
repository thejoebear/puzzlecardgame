using UnityEngine;

public enum CardCategory
{
    Science,
    History,
    Geography,
    Arts,
    Nature,
    Technology,
    Mythology,
    Literature,
    Music
}

public enum AnomalyType
{
    None,
    Supernova,  // Reveals neighbors when played
    Binary,     // Linked to another card
    Nebula      // Obscured until moved
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Solitaire/CardData")]
public class CardData : ScriptableObject
{
    public CardCategory category;
    [Range(1, 13)]
    public int rank;
    [TextArea]
    public string fact;
    public Sprite icon;
    public AnomalyType anomaly = AnomalyType.None;

    public string GetRankString()
    {
        switch (rank)
        {
            case 1: return "A";
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            default: return rank.ToString();
        }
    }

    public bool IsSameColorGroup(CardCategory other)
    {
        // Define groups: Science & History (Red?), Geography & Arts (Black?)
        bool thisInGroup1 = (category == CardCategory.Science || category == CardCategory.History);
        bool otherInGroup1 = (other == CardCategory.Science || other == CardCategory.History);
        return thisInGroup1 == otherInGroup1;
    }
}
