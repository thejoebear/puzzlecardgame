using UnityEngine;
using System.Linq;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance;

    public bool unlockAllLevels = false; // Debug/Cheat flag

    private SaveData currentSave;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }

    public void UnlockNextLevel(int completedIndex)
    {
        if (completedIndex >= currentSave.lastUnlockedLevelIndex)
        {
            currentSave.lastUnlockedLevelIndex = completedIndex + 1;
            
            // Update the list entry if it exists or add it
            var level = GetOrCreateLevelProgress(completedIndex + 1);
            level.isUnlocked = true;

            SaveProgress();
            Debug.Log($"<color=cyan>New Level Unlocked:</color> Level {currentSave.lastUnlockedLevelIndex + 1}");
        }
    }

    public void SaveStars(int levelIndex, int stars)
    {
        var level = GetOrCreateLevelProgress(levelIndex);
        if (stars > level.stars)
        {
            level.stars = stars;
            SaveProgress();
        }
    }

    public int GetStars(int levelIndex)
    {
        var level = currentSave.levelProgressList.FirstOrDefault(l => l.levelIndex == levelIndex);
        return level != null ? level.stars : 0;
    }

    public int GetCoins() => currentSave.astralCoins;

    public void AddCoins(int amount)
    {
        currentSave.astralCoins += amount;
        SaveProgress();
        Debug.Log($"<color=yellow>Astral Coins Added:</color> {amount}. Total: {currentSave.astralCoins}");
    }

    public bool SpendCoins(int amount)
    {
        if (currentSave.astralCoins >= amount)
        {
            currentSave.astralCoins -= amount;
            SaveProgress();
            Debug.Log($"<color=orange>Astral Coins Spent:</color> {amount}. Remaining: {currentSave.astralCoins}");
            return true;
        }
        return false;
    }

    public bool IsLevelUnlocked(int index)
    {
        if (unlockAllLevels || index == 0) return true;
        var level = currentSave.levelProgressList.FirstOrDefault(l => l.levelIndex == index);
        return level != null ? level.isUnlocked : (index <= currentSave.lastUnlockedLevelIndex);
    }

    private LevelProgress GetOrCreateLevelProgress(int index)
    {
        var level = currentSave.levelProgressList.FirstOrDefault(l => l.levelIndex == index);
        if (level == null)
        {
            level = new LevelProgress { levelIndex = index, isUnlocked = index <= currentSave.lastUnlockedLevelIndex };
            currentSave.levelProgressList.Add(level);
        }
        return level;
    }

    public void SaveProgress()
    {
        SaveSystem.Save(currentSave);
    }

    private void LoadProgress()
    {
        currentSave = SaveSystem.Load();
        
        // Initial setup for level 0 if list is empty
        if (currentSave.levelProgressList.Count == 0)
        {
            currentSave.levelProgressList.Add(new LevelProgress { levelIndex = 0, isUnlocked = true });
        }
    }

    // Helper for potential future features
    public SaveData GetSaveData() => currentSave;
}
