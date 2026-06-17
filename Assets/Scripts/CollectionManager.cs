using UnityEngine;
using System.Collections.Generic;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance;

    private HashSet<string> discoveredFacts = new List<string>().ToHashSet();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCollection();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private HashSet<string> ToHashSet(IEnumerable<string> items) => new HashSet<string>(items);

    public void DiscoverFact(string fact)
    {
        if (string.IsNullOrEmpty(fact)) return;
        
        if (!discoveredFacts.Contains(fact))
        {
            discoveredFacts.Add(fact);
            SaveCollection();
            Debug.Log($"<color=green>New Fact Collected:</color> {fact}");
        }
    }

    public bool IsFactDiscovered(string fact) => discoveredFacts.Contains(fact);

    public int GetDiscoveredCount() => discoveredFacts.Count;

    private void SaveCollection()
    {
        string data = string.Join("|", discoveredFacts);
        PlayerPrefs.SetString("DiscoveredFacts", data);
        PlayerPrefs.Save();
    }

    private void LoadCollection()
    {
        string data = PlayerPrefs.GetString("DiscoveredFacts", "");
        if (!string.IsNullOrEmpty(data))
        {
            string[] facts = data.Split('|');
            discoveredFacts = new HashSet<string>(facts);
        }
    }
}

// Extension method for older .NET compatibility if needed
public static class HashSetExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => new HashSet<T>(source);
}
