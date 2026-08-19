using UnityEngine;

namespace CoreLoop.SentenceBuilder
{
    public interface ISentenceBuilderLevelRepository
    {
        SentenceBuilderLevelSO LoadLevel(string levelKey = null);
    }

    public class ScriptableObjectSentenceBuilderLevelRepository : ISentenceBuilderLevelRepository
    {
        private readonly SentenceBuilderLevelSO _levelSO;

        public ScriptableObjectSentenceBuilderLevelRepository(SentenceBuilderLevelSO levelSO)
        {
            _levelSO = levelSO;
        }

        public SentenceBuilderLevelSO LoadLevel(string levelKey = null)
        {
            if (_levelSO != null) return _levelSO;
            return Resources.Load<SentenceBuilderLevelSO>("SentenceBuilder/Level 1");
        }
    }

    public class ApiSentenceBuilderLevelRepository : ISentenceBuilderLevelRepository
    {
        private readonly SentenceBuilderLevelSO _fallbackSO;

        public ApiSentenceBuilderLevelRepository(SentenceBuilderLevelSO fallbackSO = null)
        {
            _fallbackSO = fallbackSO;
        }

        public SentenceBuilderLevelSO LoadLevel(string levelKey = null)
        {
            if (Api.ContentCacheService.TryGetFromCache("SentenceBuilder", out string json))
            {
                var level = ScriptableObject.CreateInstance<SentenceBuilderLevelSO>();
                JsonUtility.FromJsonOverwrite(json, level);
                if (level != null && level.sentences != null && level.sentences.Count > 0)
                {
                    return level;
                }
            }

            _ = Api.ContentCacheService.SyncRemoteContentAsync("SentenceBuilder", levelKey);

            if (_fallbackSO != null) return _fallbackSO;
            return Resources.Load<SentenceBuilderLevelSO>("SentenceBuilder/Level 1");
        }
    }
}
