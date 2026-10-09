using System;
using UnityEngine;
using Api.Endpoints;
using Api.Models;
using Newtonsoft.Json;

namespace Api
{
    /// <summary>
    /// Single entry point for child story progress: local save/resume state,
    /// completion marking (local store + reward mission) and backend activity logging.
    /// </summary>
    public static class StoryProgressService
    {
        public static void SaveReadingProgress(Story story, int pagesRead, int totalPages)
        {
            if (story == null || string.IsNullOrEmpty(story.id)) return;

            StoryProgressStore.SaveReadingProgress(GetChildId(), story, pagesRead, totalPages);
        }

        public static bool IsStoryCompleted(Story story)
        {
            return story != null
                && !string.IsNullOrEmpty(story.id)
                && StoryProgressStore.IsStoryCompleted(GetChildId(), story.id);
        }

        // Page index to resume at (Continue Reading), or -1 when there is nothing to resume.
        public static int GetResumePage(Story story, int pageCount)
        {
            if (story == null || string.IsNullOrEmpty(story.id) || pageCount == 0)
                return -1;

            StoryReadingRecord record = StoryProgressStore.GetLastReading(GetChildId());
            if (record == null || record.storyId != story.id)
                return -1;

            return Mathf.Clamp(record.pagesRead - 1, 0, pageCount - 1);
        }

        public static void MarkStoryCompleted(Story story, int pageCount, float readingStartTime)
        {
            if (story == null || string.IsNullOrEmpty(story.id)) return;

            StoryProgressStore.MarkStoryCompleted(GetChildId(), story, pageCount);

            // Mission trigger — id must match a Reward Catalog entry ("read stories" reward).
            RewardProgressStore.ReportProgress("story_complete");

            // "isComplete" rides in the payload — the DB has no completion column.
            string payload = JsonConvert.SerializeObject(new
            {
                isComplete = true,
                pagesRead = pageCount,
                timeSpent = Mathf.RoundToInt(Time.unscaledTime - readingStartTime)
            });
            _ = ReportCompletionAsync(story, payload);
        }

        private static async Awaitable ReportCompletionAsync(Story story, string payload)
        {
            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                await new ChildApi(apiClient).LogStoryActivityAsync(story.id, payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StoryProgressService] Story completion log failed, queuing offline: {ex.Message}");
                OfflineActivityQueue.Instance?.EnqueueActivity(story.id, payload);
            }
        }

        private static string GetChildId()
        {
            return SessionManager.Instance != null ? SessionManager.Instance.ChildId : null;
        }
    }
}
