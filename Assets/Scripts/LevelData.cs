using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public class CategoryFactMapping
{
    public CardCategory category;
    public FactPack factPack;
    public int cardCount = 13;
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "Solitaire/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;
    public List<CategoryFactMapping> categoryMappings = new List<CategoryFactMapping>();
    [Range(1, 4)]
public int foundationSlots = 2;
    public VisualTheme theme;
    
    [Header("Mastery Thresholds (Moves)")]
    public int threeStarMoves = 80;
    public int twoStarMoves = 120;

    [Header("Puzzle Modifiers")]
    public bool useMysteryCards = false;
    [Range(0f, 1f)]
    public float mysteryChance = 0.2f;
    public bool useAnomalies = false;
    [Range(0f, 1f)]
    public float anomalyChance = 0.1f;
    public int tableauCapacity = 0; // 0 = infinite
public int lockedFoundationSlots = 0;

    [Header("Abilities (Scholar's Insight)")]
    [FormerlySerializedAs("focusCharges")]
    public int astralSightCharges = 2;
    [FormerlySerializedAs("gravityCharges")]
    public int gravitationalPullCharges = 1;
    public int nebulaCharges = 1;

    [Header("Saga Map Settings")]
    public bool isNebulaNode = false;
    public int branchFromIndex = -1;
    public Vector2 branchOffset = new Vector2(200f, 0f);

    [Header("Win Visuals")]
    public List<Vector2> constellationShape; // Points in local space for the win constellation

    // Helper to get categories list
public List<CardCategory> GetCategories()
    {
        List<CardCategory> cats = new List<CardCategory>();
        foreach(var m in categoryMappings) cats.Add(m.category);
        return cats;
    }
}
