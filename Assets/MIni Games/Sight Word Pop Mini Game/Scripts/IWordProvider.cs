using System.Collections.Generic;
using UnityEngine;

public interface IWordProvider
{
    List<WordEntry> GetWords(int count);
    LevelDataSO GetLevelData();
}

public class ScriptableObjectWordProvider : IWordProvider
{
    private readonly LevelDataSO _levelData;

    public ScriptableObjectWordProvider(LevelDataSO levelData)
    {
        _levelData = levelData;
    }

    public List<WordEntry> GetWords(int count)
    {
        return _levelData != null ? _levelData.GetShuffledWords(count) : new List<WordEntry>();
    }

    public LevelDataSO GetLevelData() => _levelData;
}

public class ApiWordProvider : IWordProvider
{
    private readonly LevelDataSO _fallbackData;

    public ApiWordProvider(LevelDataSO fallbackData)
    {
        _fallbackData = fallbackData;
    }

    public List<WordEntry> GetWords(int count)
    {
        if (Api.ContentCacheService.TryGetFromCache("SightWordPop", out string json))
        {
            var level = ScriptableObject.CreateInstance<LevelDataSO>();
            JsonUtility.FromJsonOverwrite(json, level);
            if (level != null && level.allWords != null && level.allWords.Count > 0)
            {
                return level.GetShuffledWords(count);
            }
        }

        _ = Api.ContentCacheService.SyncRemoteContentAsync("SightWordPop");
        return _fallbackData != null ? _fallbackData.GetShuffledWords(count) : new List<WordEntry>();
    }

    public LevelDataSO GetLevelData() => _fallbackData;
}
