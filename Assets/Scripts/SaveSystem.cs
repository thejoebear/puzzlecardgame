using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"<color=green>Game Saved to:</color> {SavePath}");
    }

    public static SaveData Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        
        Debug.Log("No save file found, creating new SaveData.");
        return new SaveData();
    }

    public static void ClearSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }
}
