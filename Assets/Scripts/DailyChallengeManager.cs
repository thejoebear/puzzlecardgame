using UnityEngine;
using System.Collections.Generic;
using System;

public class DailyChallengeManager : MonoBehaviour
{
    public static DailyChallengeManager Instance;

    public LevelData dailyLevelTemplate; // To copy theme and basic settings
    public List<FactPack> allFactPacks;

    void Awake()
    {
        Instance = this;
    }

    public LevelData GenerateDailyLevel()
    {
        // 1. Seed based on current date
        int seed = DateTime.Today.Year * 10000 + DateTime.Today.Month * 100 + DateTime.Today.Day;
        UnityEngine.Random.InitState(seed);

        LevelData level = ScriptableObject.CreateInstance<LevelData>();
        level.levelName = $"Daily Challenge: {DateTime.Today.ToShortDateString()}";
        
        if (dailyLevelTemplate != null)
        {
            level.theme = dailyLevelTemplate.theme;
        }

        // 2. Randomize Categories (2 to 4)
        int numCats = UnityEngine.Random.Range(2, 5);
        List<FactPack> selectedPacks = new List<FactPack>(allFactPacks);
        // Shuffle and take N
        for (int i = 0; i < selectedPacks.Count; i++)
        {
            FactPack temp = selectedPacks[i];
            int r = UnityEngine.Random.Range(i, selectedPacks.Count);
            selectedPacks[i] = selectedPacks[r];
            selectedPacks[r] = temp;
        }

        int totalCards = 0;
        for (int i = 0; i < numCats && i < selectedPacks.Count; i++)
        {
            FactPack pack = selectedPacks[i];
            // Random count between 6 and 12
            int count = UnityEngine.Random.Range(6, 13);
            
            CategoryFactMapping mapping = new CategoryFactMapping();
            mapping.category = pack.category;
            mapping.factPack = pack;
            mapping.cardCount = count;
            level.categoryMappings.Add(mapping);
            totalCards += count;
}

        // 3. Randomize Slots and Thresholds
        bool isTheVoid = (UnityEngine.Random.value < 0.15f); // 15% chance for The Void
        level.foundationSlots = isTheVoid ? 0 : UnityEngine.Random.Range(2, 5);
        
        if (isTheVoid) level.levelName = "DAILY ARCHive: THE VOID";

        level.threeStarMoves = (int)(totalCards * 4.5f);
level.twoStarMoves = (int)(totalCards * 6.5f);

        // 4. Randomize Modifiers
        level.useMysteryCards = (UnityEngine.Random.value > 0.3f);
        level.mysteryChance = UnityEngine.Random.Range(0.1f, 0.4f);
        
        // Tableau Capacity (Master challenge)
        if (UnityEngine.Random.value > 0.5f)
        {
            level.tableauCapacity = UnityEngine.Random.Range(8, 13);
        }
        else
        {
            level.tableauCapacity = 0; // Infinite
        }

        // Locked Slots
        if (numCats > 2 && UnityEngine.Random.value > 0.5f)
        {
            level.lockedFoundationSlots = UnityEngine.Random.Range(1, numCats - 1);
        }

        // Ability Charges
        level.astralSightCharges = UnityEngine.Random.Range(2, 4);
        level.gravitationalPullCharges = UnityEngine.Random.Range(2, 3);
        level.nebulaCharges = UnityEngine.Random.Range(1, 3);

        // 5. Generate Random Constellation Shape
        level.constellationShape = GenerateRandomShape(UnityEngine.Random.Range(5, 9));

        return level;
    }

    private List<Vector2> GenerateRandomShape(int points)
    {
        List<Vector2> shape = new List<Vector2>();
        float radius = 4f;
        for (int i = 0; i < points; i++)
        {
            float angle = (i / (float)points) * Mathf.PI * 2;
            float r = UnityEngine.Random.Range(radius * 0.5f, radius);
            shape.Add(new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r));
        }
        // Close the loop
        if (shape.Count > 0) shape.Add(shape[0]);
        return shape;
    }
}
