using UnityEngine;
using System.Collections.Generic;

public static class SolitaireRules
{
    public static bool CanMoveToFoundation(CardData card, List<CardData> foundation, CardCategory? foundationCategory)
    {
        if (foundation.Count == 0) return card.rank == 1;
        
        // No longer sequential: just must match the category assigned to this slot
        return card.category == foundationCategory;
    }

    public static bool CanMoveToTableau(CardData card, List<CardData> targetTableau, List<CardData> deckPool, int capacity = 0)
    {
        // Tableau capacity checks bypassed to allow infinite stacking as requested
        // bool nebulaActive = AbilitySystem.Instance != null && AbilitySystem.Instance.isNebulaActive;
        // if (!nebulaActive && capacity > 0 && targetTableau.Count >= capacity) return false;

        if (AbilitySystem.Instance != null && AbilitySystem.Instance.isNebulaActive)
        {
            // Bending the Rules: stack any card regardless of category
            return true;
        }

        if (targetTableau.Count == 0)
        {
            // Allow any card to start an empty tableau pile
            return true;
        }
        else
        {
            CardData top = targetTableau[targetTableau.Count - 1];
            
            // RELAXED RULES: 
            // 1. No rank check (stackable in any order)
            // 2. Must be the same category to keep topics grouped
            return card.category == top.category;
        }
    }

    public static bool IsDifferentColorGroup(CardCategory cat1, CardCategory cat2, Dictionary<CardCategory, int> colorGroups)
    {
        // Simple helper for LevelManager
        if (colorGroups.ContainsKey(cat1) && colorGroups.ContainsKey(cat2))
            return colorGroups[cat1] != colorGroups[cat2];
        return false;
    }

    public static bool IsValidSequence(List<CardData> cards)
    {
        if (cards == null || cards.Count <= 1) return true;

        if (AbilitySystem.Instance != null && AbilitySystem.Instance.isNebulaActive)
        {
            // Bending the Rules: allow dragging any stack sequence regardless of categories
            return true;
        }

        for (int i = 0; i < cards.Count - 1; i++)
        {
            // A stack is movable if it is of the same category (rank no longer matters)
            if (cards[i].category != cards[i + 1].category)
            {
                return false;
            }
        }
        return true;
    }
}
