using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Solitaire/Deck")]
public class Deck : ScriptableObject
{
    public List<CardData> cards = new List<CardData>();

    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            CardData temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}
