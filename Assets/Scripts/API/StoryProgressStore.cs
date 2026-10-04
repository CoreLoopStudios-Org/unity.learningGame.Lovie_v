using System;
using System.Collections.Generic;
using UnityEngine;
using Api.Models;

namespace Api
{
    [Serializable]
    public class CompletedStoryRecord
    {
        public string storyId;
        public string title;
        public string completedAt;
    }

    [Serializable]
    internal class StoryProgressData
    {
        public List<CompletedStoryRecord> completedStories = new List<CompletedStoryRecord>();
        public List<string> takenQuizIds = new List<string>();
    }

    /// <summary>
    /// The backend has no story-completion field, so reading progress rides in a local
    /// JSON document (PlayerPrefs, keyed per child) plus an "isComplete" key inside the
    /// activity payload posted to /api/child/activities/story.
    /// </summary>
    public static class StoryProgressStore
    {
        private const string KeyPrefix = "lovie.storyprogress.";

        public static string GetStorageKey(string childId)
        {
            return KeyPrefix + (string.IsNullOrEmpty(childId) ? "default" : childId);
        }

        public static void MarkStoryCompleted(string childId, Story story)
        {
            if (story == null || string.IsNullOrEmpty(story.id)) return;

            StoryProgressData data = Load(childId);
            if (data.completedStories.Exists(s => s.storyId == story.id)) return;

            data.completedStories.Add(new CompletedStoryRecord
            {
                storyId = story.id,
                title = story.title,
                completedAt = DateTime.UtcNow.ToString("o")
            });
            Save(childId, data);
        }

        public static bool IsStoryCompleted(string childId, string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return false;
            return Load(childId).completedStories.Exists(s => s.storyId == storyId);
        }

        // Newest first — the quiz page serves the most recently finished story.
        public static List<CompletedStoryRecord> GetCompletedStories(string childId)
        {
            List<CompletedStoryRecord> stories = Load(childId).completedStories;
            stories.Sort((a, b) => string.CompareOrdinal(b.completedAt, a.completedAt));
            return stories;
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
