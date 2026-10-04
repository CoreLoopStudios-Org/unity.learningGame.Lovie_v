using System;
using System.Collections.Generic;
using UnityEngine;
using Api.Models;

namespace Api
{
    [Serializable]
    public class StoryReadingRecord
    {
        public string storyId;
        public string title;
        public int pagesRead;
        public int totalPages;
        public bool isComplete;
        public string updatedAt;
    }

    [Serializable]
    internal class StoryProgressData
    {
        public List<StoryReadingRecord> stories = new List<StoryReadingRecord>();
        public List<string> takenQuizIds = new List<string>();
    }

    /// <summary>
    /// The backend has no reading-progress or story-completion fields, so they ride in a
    /// local JSON document (PlayerPrefs, keyed per child) plus an "isComplete" key inside
    /// the activity payload posted to /api/child/activities/story.
    /// </summary>
    public static class StoryProgressStore
    {
        private const string KeyPrefix = "lovie.storyprogress.";

        public static string GetStorageKey(string childId)
        {
            return KeyPrefix + (string.IsNullOrEmpty(childId) ? "default" : childId);
        }

        public static void SaveReadingProgress(string childId, Story story, int pagesRead, int totalPages)
        {
            Upsert(childId, story, pagesRead, totalPages, false);
        }

        public static void MarkStoryCompleted(string childId, Story story, int totalPages)
        {
            Upsert(childId, story, totalPages, totalPages, true);
        }

        private static void Upsert(string childId, Story story, int pagesRead, int totalPages, bool isComplete)
        {
            if (story == null || string.IsNullOrEmpty(story.id)) return;

            StoryProgressData data = Load(childId);
            StoryReadingRecord record = data.stories.Find(s => s.storyId == story.id);
            if (record == null)
            {
                record = new StoryReadingRecord { storyId = story.id, title = story.title };
                data.stories.Add(record);
            }

            record.title = story.title;
            record.totalPages = Mathf.Max(totalPages, record.totalPages);
            record.pagesRead = record.isComplete
                ? record.totalPages
                : Mathf.Clamp(Mathf.Max(pagesRead, record.pagesRead), 0, record.totalPages);
            record.isComplete = record.isComplete || isComplete;
            record.updatedAt = DateTime.UtcNow.ToString("o");
            Save(childId, data);
        }

        // Most recently touched story, complete or not — the Continue Reading card.
        public static StoryReadingRecord GetLastReading(string childId)
        {
            StoryReadingRecord latest = null;
            foreach (StoryReadingRecord record in Load(childId).stories)
            {
                if (latest == null || string.CompareOrdinal(record.updatedAt, latest.updatedAt) > 0)
                {
                    latest = record;
                }
            }
            return latest;
        }

        public static bool IsStoryCompleted(string childId, string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return false;
            return Load(childId).stories.Exists(s => s.storyId == storyId && s.isComplete);
        }

        // Completed stories newest first — the quiz page serves the most recent.
        public static List<StoryReadingRecord> GetCompletedStories(string childId)
        {
            List<StoryReadingRecord> completed = Load(childId).stories.FindAll(s => s.isComplete);
            completed.Sort((a, b) => string.CompareOrdinal(b.updatedAt, a.updatedAt));
            return completed;
        }

        public static bool IsQuizTaken(string childId, string quizId)
        {
            if (string.IsNullOrEmpty(quizId)) return false;
            return Load(childId).takenQuizIds.Contains(quizId);
        }

        public static List<string> GetTakenQuizIds(string childId)
        {
            return Load(childId).takenQuizIds;
        }

        public static void MarkQuizTaken(string childId, string quizId)
        {
            if (string.IsNullOrEmpty(quizId)) return;

            StoryProgressData data = Load(childId);
            if (data.takenQuizIds.Contains(quizId)) return;

            data.takenQuizIds.Add(quizId);
            Save(childId, data);
        }

        private static StoryProgressData Load(string childId)
        {
            string json = PlayerPrefs.GetString(GetStorageKey(childId), string.Empty);
            if (string.IsNullOrEmpty(json)) return new StoryProgressData();

            try
            {
                return JsonUtility.FromJson<StoryProgressData>(json) ?? new StoryProgressData();
            }
            catch
            {
                return new StoryProgressData();
            }
        }

        private static void Save(string childId, StoryProgressData data)
        {
            PlayerPrefs.SetString(GetStorageKey(childId), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
