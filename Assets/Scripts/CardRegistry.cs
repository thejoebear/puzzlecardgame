using System.Collections.Generic;
using UnityEngine;

public class CardRegistry : MonoBehaviour
{
    public static CardRegistry Instance { get; private set; }

    private readonly List<CardDisplay> activeCards = new List<CardDisplay>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(CardDisplay card)
    {
        if (!activeCards.Contains(card))
        {
            activeCards.Add(card);
        }
    }

    public void Unregister(CardDisplay card)
    {
        activeCards.Remove(card);
    }

    public List<CardDisplay> GetAllCards()
    {
        return activeCards;
    }
}
