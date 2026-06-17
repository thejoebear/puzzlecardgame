using UnityEngine;

[CreateAssetMenu(fileName = "SupernovaAnomaly", menuName = "Solitaire/Anomalies/Supernova")]
public class SupernovaAnomaly : AnomalyEffect
{
    public override void OnCardPlayed(SolitaireManager manager, CardDisplay card, Pile targetPile)
    {
        if (targetPile.type != PileType.Foundation) return;

        Debug.Log("<color=orange>[Anomaly]</color> Supernova triggered!");
        
        if (CelestialJuiceManager.Instance != null)
        {
            CelestialJuiceManager.Instance.PulseBloom(3.0f, 0.8f);
        }

        if (CelestialVFXManager.Instance != null)
{
            CelestialVFXManager.Instance.PlayFocusEffect(card.transform.position);
        }

        // Reveal adjacent tableau cards (Standard logic moved here)
        for (int i = 0; i < manager.tableauPiles.Length; i++)
        {
            if (manager.tableauPiles[i].childCount > 0)
            {
                Transform lastChild = manager.tableauPiles[i].GetChild(manager.tableauPiles[i].childCount - 1);
                CardDisplay display = lastChild.GetComponent<CardDisplay>();
                if (display != null && !display.IsFaceUp)
                {
                    display.Flip(true);
                    if (lastChild.GetComponent<CardDraggable>() == null)
                        lastChild.gameObject.AddComponent<CardDraggable>();
                }
            }
        }
    }
}
