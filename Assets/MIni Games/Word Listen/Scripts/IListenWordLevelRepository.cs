using UnityEngine;

namespace CoreLoop.ListenWord
{
    public interface IListenWordLevelRepository
    {
        ListenWordLevelSO LoadLevel(string levelKey = null);
    }

    public class ScriptableObjectListenWordLevelRepository : IListenWordLevelRepository
    {
        private readonly ListenWordLevelSO _levelSO;

        public ScriptableObjectListenWordLevelRepository(ListenWordLevelSO levelSO)
        {
            _levelSO = levelSO;
        }

        public ListenWordLevelSO LoadLevel(string levelKey = null)
        {
            if (_levelSO != null) return _levelSO;
            return Resources.Load<ListenWordLevelSO>("ListenWord/Level 1");
        }
    }

    public class ApiListenWordLevelRepository : IListenWordLevelRepository
    {
        private readonly ListenWordLevelSO _fallbackSO;

        public ApiListenWordLevelRepository(ListenWordLevelSO fallbackSO = null)
        {
            _fallbackSO = fallbackSO;
        }

        public ListenWordLevelSO LoadLevel(string levelKey = null)
        {
            if (Api.ContentCacheService.TryGetFromCache("WordListen", out string json))
            {
                var level = ScriptableObject.CreateInstance<ListenWordLevelSO>();
                JsonUtility.FromJsonOverwrite(json, level);
                if (level != null && level.wordsToSpell != null && level.wordsToSpell.Count > 0)
                {
                    return level;
                }
            }

            _ = Api.ContentCacheService.SyncRemoteContentAsync("WordListen", levelKey);

            if (_fallbackSO != null) return _fallbackSO;
            return Resources.Load<ListenWordLevelSO>("ListenWord/Level 1");
        }
    }
}
