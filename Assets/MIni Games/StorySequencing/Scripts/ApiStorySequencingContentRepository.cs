using UnityEngine;
using Api;

namespace Modules.GameFramework.Content
{
    /// <summary>
    /// Remote API repository for Story Sequencing stories.
    /// Fetches from GET /api/child/minigames/content/StorySequencing?key={storyId},
    /// caches to disk, and falls back to bundled Resources if offline.
    /// </summary>
    public class ApiStorySequencingContentRepository : IStorySequencingContentRepository
    {
        private const string RESOURCES_FOLDER = "Stories";

        public StorySequencingEntry LoadStory(string storyId)
        {
            string cacheKey = $"StorySequencing_{storyId}";
            string json = null;

            // 1. Try disk cache
            if (!ContentCacheService.TryGetFromCache(cacheKey, out json))
            {
                // 2. Fall back to bundled Resources
                string resourcePath = $"{RESOURCES_FOLDER}/{storyId}";
                json = ContentCacheService.GetResourcesFallback(resourcePath);
            }

            // 3. Trigger background sync to refresh cache for next launch
            _ = ContentCacheService.SyncRemoteContentAsync("StorySequencing", storyId);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[ApiStorySequencingContentRepository] Could not load content for {storyId}");
                return null;
            }

            StorySequencingEntry entry = JsonUtility.FromJson<StorySequencingEntry>(json);
            if (entry == null)
            {
                Debug.LogError($"[ApiStorySequencingContentRepository] Failed to parse story sequencing for {storyId}");
            }

            return entry;
        }
    }
}
