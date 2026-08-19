using System.Collections.Generic;
using UnityEngine;

namespace CoreLoop.WordMatch
{
    public interface IWordMatchLevelRepository
    {
        WordMatchLevelSO LoadLevel(string levelKey = null);
    }

    public class ScriptableObjectWordMatchLevelRepository : IWordMatchLevelRepository
    {
        private readonly WordMatchLevelSO _levelSO;

        public ScriptableObjectWordMatchLevelRepository(WordMatchLevelSO levelSO)
        {
            _levelSO = levelSO;
        }

        public WordMatchLevelSO LoadLevel(string levelKey = null)
        {
            if (_levelSO != null) return _levelSO;
            return Resources.Load<WordMatchLevelSO>("WordMatch/Level 1");
        }
    }

    public class ApiWordMatchLevelRepository : IWordMatchLevelRepository
    {
        private readonly WordMatchLevelSO _fallbackSO;

        public ApiWordMatchLevelRepository(WordMatchLevelSO fallbackSO = null)
        {
            _fallbackSO = fallbackSO;
        }

        public WordMatchLevelSO LoadLevel(string levelKey = null)
        {
            if (Api.ContentCacheService.TryGetFromCache("WordMatch", out string json))
            {
                var level = ScriptableObject.CreateInstance<WordMatchLevelSO>();
                JsonUtility.FromJsonOverwrite(json, level);
                if (level != null && level.rounds != null && level.rounds.Count > 0)
                {
                    return level;
                }
            }

            _ = Api.ContentCacheService.SyncRemoteContentAsync("WordMatch", levelKey);

            if (_fallbackSO != null) return _fallbackSO;
            return Resources.Load<WordMatchLevelSO>("WordMatch/Level 1");
        }
    }
}
