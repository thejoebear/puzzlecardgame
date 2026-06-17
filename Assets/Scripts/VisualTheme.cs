using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CategoryVisuals
{
    public CardCategory category;
    public Color categoryColor = Color.white;
    public Sprite categoryIcon;
    public Sprite standardCardArt;
}

[CreateAssetMenu(fileName = "NewTheme", menuName = "Solitaire/VisualTheme")]
public class VisualTheme : ScriptableObject
{
    [Header("Board")]
    public Color boardColor = new Color(0.1f, 0.4f, 0.2f);
    public Sprite boardSprite;

    [Header("Empty Piles")]
    public Sprite foundationSilhouette;
    public Sprite tableauSilhouette;

    [Header("Cards")]
    public Sprite cardBackSprite;
    public Color cardFrontColor = Color.white;
    public Color headerColor = new Color(0.9f, 0.9f, 0.6f);
    
    [Header("Categories")]
    public List<CategoryVisuals> categoryVisuals = new List<CategoryVisuals>();

    [Header("UI")]
    public Color primaryUIColor = new Color(0.1f, 0.4f, 0.6f);
    public Color textOnPrimaryColor = Color.white;
    public Sprite panelSprite;
    public Sprite buttonSprite;

    public CategoryVisuals GetVisuals(CardCategory category)
    {
        return categoryVisuals.Find(v => v.category == category);
    }
}
