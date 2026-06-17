using UnityEngine;
using System.Collections.Generic;

public static class SolitaireSolver
{
    public static bool FastSimulation(
        List<CardData> deckRef, 
        int slotLimit, 
        int foundationCount, 
        int totalCards, 
        Dictionary<CardCategory, int> categorySizes,
        List<CardData> deckPool,
        int tableauCapacity = 0,
        int initialLockedSlots = 0)
    {
        List<CardData> simStock = new List<CardData>(deckRef);
        List<CardData> simWaste = new List<CardData>();
        List<CardData>[] simTableaus = new List<CardData>[7];
        for (int i = 0; i < 7; i++) simTableaus[i] = new List<CardData>();

        for (int i = 0; i < 7; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                if (simStock.Count > 0)
                {
                    simTableaus[i].Add(simStock[0]);
                    simStock.RemoveAt(0);
                }
            }
        }

        List<CardData>[] simFoundations = new List<CardData>[foundationCount];
        for (int i = 0; i < simFoundations.Length; i++) simFoundations[i] = new List<CardData>();
        CardCategory?[] simSlots = new CardCategory?[foundationCount];

        int currentSlotLimit = slotLimit - initialLockedSlots;
        int finishedCards = 0;
        int moves = 0;
        bool changed;

        do
        {
            changed = false;
            moves++;

            for (int i = 0; i < 7; i++)
            {
                if (simTableaus[i].Count > 0)
                {
                    CardData c = simTableaus[i][simTableaus[i].Count - 1];
                    if (TrySimPush(c, simFoundations, simSlots, currentSlotLimit, out int slot))
                    {
                        simTableaus[i].RemoveAt(simTableaus[i].Count - 1);
                        changed = true;
                        if (SimCheckComplete(slot, simFoundations, simSlots, categorySizes, ref finishedCards)) 
                        {
                            changed = true;
                            if (currentSlotLimit < slotLimit) currentSlotLimit++; // Unlock one
                        }
                    }
                }
                if (changed) break;

                // Tableau Completion Check
                if (simTableaus[i].Count > 0)
                {
                    CardCategory cat = simTableaus[i][0].category;
                    if (simTableaus[i].Count == categorySizes[cat])
                    {
                        bool pure = true;
                        foreach(var card in simTableaus[i]) if(card.category != cat) { pure = false; break; }
                        if (pure)
                        {
                            finishedCards += simTableaus[i].Count;
                            simTableaus[i].Clear();
                            changed = true;
                            if (currentSlotLimit < slotLimit) currentSlotLimit++; // Unlock one
                        }
                    }
                }
                if (changed) break;
            }

            if (!changed && simWaste.Count > 0)
            {
                CardData c = simWaste[simWaste.Count - 1];
                if (TrySimPush(c, simFoundations, simSlots, currentSlotLimit, out int slot))
                {
                    simWaste.RemoveAt(simWaste.Count - 1);
                    changed = true;
                    if (SimCheckComplete(slot, simFoundations, simSlots, categorySizes, ref finishedCards)) 
                    {
                        changed = true;
                        if (currentSlotLimit < slotLimit) currentSlotLimit++; // Unlock
                    }
                }
            }

            if (!changed && simWaste.Count > 0)
            {
                CardData c = simWaste[simWaste.Count - 1];
                for (int k = 0; k < 7; k++)
                {
                    if (SolitaireRules.CanMoveToTableau(c, simTableaus[k], deckPool, tableauCapacity))
                    {
                        simTableaus[k].Add(c);
                        simWaste.RemoveAt(simWaste.Count - 1);
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (simTableaus[i].Count > 0)
                    {
                        for (int startIdx = 0; startIdx < simTableaus[i].Count; startIdx++)
                        {
                            List<CardData> subStack = simTableaus[i].GetRange(startIdx, simTableaus[i].Count - startIdx);
                            if (SolitaireRules.IsValidSequence(subStack))
                            {
                                CardData bottomCard = subStack[0];
                                for (int k = 0; k < 7; k++)
                                {
                                    if (i == k) continue;
                                    if (SolitaireRules.CanMoveToTableau(bottomCard, simTableaus[k], deckPool, tableauCapacity))
                                    {
                                        simTableaus[k].AddRange(subStack);
                                        simTableaus[i].RemoveRange(startIdx, subStack.Count);
                                        changed = true;
                                        break;
                                    }
                                }
                            }
                            if (changed) break;
                        }
                    }
                    if (changed) break;
                }
            }

            if (!changed && simStock.Count > 0)
            {
                simWaste.Add(simStock[0]);
                simStock.RemoveAt(0);
                changed = true;
            }
            else if (!changed && simWaste.Count > 0)
            {
                simStock.AddRange(simWaste);
                simWaste.Clear();
                changed = true;
            }

        } while (changed && finishedCards < totalCards && moves < 5000);

        return finishedCards == totalCards;
    }

    public static bool TrySimPush(CardData c, List<CardData>[] foundations, CardCategory?[] slots, int limit, out int slot)
    {
        slot = -1;
        for (int i = 0; i < limit; i++)
        {
            // RELAXED: If slot already has this category, any rank is fine
            if (slots[i] == c.category)
            {
                foundations[i].Add(c);
                slot = i;
                return true;
            }
        }
        
        // If slot is empty, card must be Rank 1 (topic card)
        if (c.rank == 1)
        {
            for (int i = 0; i < limit; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = c.category;
                    foundations[i].Add(c);
                    slot = i;
                    return true;
                }
            }
        }
        return false;
    }

    public static bool SimCheckComplete(int slot, List<CardData>[] foundations, CardCategory?[] slots, Dictionary<CardCategory, int> activeCategorySizes, ref int count)
    {
        if (slot == -1) return false;
        CardCategory? cat = slots[slot];
        if (cat.HasValue && foundations[slot].Count == activeCategorySizes[cat.Value])
        {
            count += foundations[slot].Count;
            foundations[slot].Clear();
            slots[slot] = null;
            return true;
        }
        return false;
    }
}
