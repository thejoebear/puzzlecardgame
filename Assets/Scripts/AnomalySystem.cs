using UnityEngine;
using System.Collections.Generic;

public class AnomalySystem : MonoBehaviour
{
    public static AnomalySystem Instance;

    public List<AnomalyEffect> anomalyDefinitions = new List<AnomalyEffect>();
    private Dictionary<AnomalyType, AnomalyEffect> anomalyMap = new Dictionary<AnomalyType, AnomalyEffect>();

    void Awake()
    {
        Instance = this;
        foreach (var def in anomalyDefinitions)
        {
            if (def != null && !anomalyMap.ContainsKey(def.anomalyType))
                anomalyMap.Add(def.anomalyType, def);
        }
    }

    public void TriggerAnomaly(AnomalyType type, SolitaireManager manager, CardDisplay card, Pile targetPile)
    {
        if (anomalyMap.TryGetValue(type, out AnomalyEffect effect))
        {
            effect.OnCardPlayed(manager, card, targetPile);
        }
    }

    public AnomalyEffect GetDefinition(AnomalyType type)
    {
        anomalyMap.TryGetValue(type, out AnomalyEffect effect);
        return effect;
    }
}
