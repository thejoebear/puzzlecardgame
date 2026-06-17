using System.Collections.Generic;

[System.Serializable]
public class LevelProgress
{
    public int levelIndex;
    public int stars;
    public bool isUnlocked;
}

[System.Serializable]
public class SaveData
{
    public int lastUnlockedLevelIndex = 0;
    public List<LevelProgress> levelProgressList = new List<LevelProgress>();
    public List<string> discoveredFactIds = new List<string>();
    
    // Add other persistent fields here (e.g. currency, settings)
    public int astralCoins = 0;
    public float musicVolume = 0.75f;
    public float sfxVolume = 0.75f;
}
