using UnityEngine;
using Api;

namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Remote API repository for Story Quest and Reading Detective levels.
    /// Fetches from GET /api/child/minigames/content/StoryQuest?key={storyId},
    /// caches to disk, and falls back to bundled Resources if offline.
    /// </summary>
    public class ApiStoryQuestContentRepository : IStoryQuestContentRepository
    {
        private const string RESOURCES_FOLDER = "Stories";

        public StoryQuestLevel LoadLevel(string storyId)
        {
            string gameType = (storyId != null && storyId.StartsWith("rd_")) ? "ReadingDetective" : "StoryQuest";
            string cacheKey = $"{gameType}_{storyId}";

            string json = null;

            // 1. Try disk cache
            if (!ContentCacheService.TryGetFromCache(cacheKey, out json))
            {
                // 2. Fall back to bundled Resources
                string resourcePath = $"{RESOURCES_FOLDER}/{storyId}";
                json = ContentCacheService.GetResourcesFallback(resourcePath);
            }

            // 3. Trigger background sync to refresh cache for subsequent plays
            _ = ContentCacheService.SyncRemoteContentAsync(gameType, storyId);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[ApiStoryQuestContentRepository] Could not load content for {storyId}");
                return null;
            }

            StoryQuestLevel level = JsonUtility.FromJson<StoryQuestLevel>(json);
            if (level == null)
            {
                Debug.LogError($"[ApiStoryQuestContentRepository] Failed to parse level for {storyId}");
            }

            return level;
        }
    }
}
