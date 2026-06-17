using UnityEngine;
using TMPro;

public class CoinDisplayUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    
    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (coinText != null && ProgressionManager.Instance != null)
        {
            coinText.text = ProgressionManager.Instance.GetCoins().ToString();
        }
    }
}
