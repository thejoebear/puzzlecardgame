using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class CardDebugger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private GameObject debugPanel;
    private SolitaireManager manager;

    void Awake()
    {
        manager = Object.FindAnyObjectByType<SolitaireManager>();
        if (debugPanel != null) debugPanel.SetActive(false);
    }

    public void ToggleDebug()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
            if (debugPanel.activeSelf) RefreshReport();
        }
    }

    public void RefreshReport()
    {
        if (manager == null || debugText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<size=120%><b>CARD INTEGRITY REPORT</b></size>");
        sb.AppendLine($"Total Expected: {manager.totalCardsInCurrentGame}");
        sb.AppendLine($"Completed: {manager.completedCardsCount}");
        sb.AppendLine("--------------------------");

        foreach (var kvp in manager.activeCategorySizes)
        {
            CardCategory cat = kvp.Key;
            int target = kvp.Value;
            
            sb.AppendLine($"<b>Category: {cat} ({target} cards)</b>");
            
            for (int r = 1; r <= target; r++)
            {
                string loc = FindCardLocation(cat, r);
                sb.AppendLine($"  Rank {r}: {loc}");
            }
        }

        debugText.text = sb.ToString();
    }

    private string FindCardLocation(CardCategory cat, int rank)
    {
        // 1. Check Foundations
        if (manager.foundations != null)
        {
            for (int i = 0; i < manager.foundations.Length; i++)
            {
                foreach (var c in manager.foundations[i])
                    if (c.category == cat && c.rank == rank) return $"Foundation {i}";
            }
        }

        // 2. Check Tableaus
        if (manager.tableaus != null)
        {
            for (int i = 0; i < manager.tableaus.Length; i++)
            {
                foreach (var c in manager.tableaus[i])
                    if (c.category == cat && c.rank == rank) return $"Tableau {i}";
            }
        }

        // 3. Check Stock
        foreach (var c in manager.stock)
            if (c.category == cat && c.rank == rank) return "Stock";

        // 4. Check Waste
        foreach (var c in manager.waste)
            if (c.category == cat && c.rank == rank) return "Waste";

        // 5. Check Completed
        // We can't check the list, but we can check visual objects in CompletedPile
        if (manager.completedPile != null)
        {
            foreach (Transform child in manager.completedPile)
            {
                CardDisplay cd = child.GetComponent<CardDisplay>();
                if (cd != null && cd.cardData != null && cd.cardData.category == cat && cd.cardData.rank == rank)
                    return "Completed Pile (Visual Found)";
            }
        }

        return "<color=red>NOT FOUND IN ANY LIST</color>";
    }
}
