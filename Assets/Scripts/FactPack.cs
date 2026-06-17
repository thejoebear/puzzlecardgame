using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewFactPack", menuName = "Solitaire/FactPack")]
public class FactPack : ScriptableObject
{
    public CardCategory category;
    [TextArea(2, 5)]
    public List<string> facts = new List<string>();
}
