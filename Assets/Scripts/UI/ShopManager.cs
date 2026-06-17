using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public Button[] buyButtons;
    public int[] costs = { 50, 100, 200 }; // Costs for some hypothetical unlocks

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (ProgressionManager.Instance != null)
        {
            coinText.text = ProgressionManager.Instance.GetCoins().ToString();
        }
    }

    public void BuyItem(int index)
    {
        if (index < 0 || index >= costs.Length) return;

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.SpendCoins(costs[index]))
        {
            Debug.Log($"Bought item {index}!");
            // Implementation of what was bought would go here
            // e.g., unlocking a permanent perk
            RefreshUI();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelWin();
        }
        else
        {
            Debug.Log("Not enough coins!");
            // Shake or feedback
        }
    }
}
