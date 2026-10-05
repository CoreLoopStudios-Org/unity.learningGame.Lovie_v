using System.Collections.Generic;
using UnityEngine;
using Api;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Shared Continue Reading logic (stories page + home page): which stories are
    /// visible, which one the card shows, and how the book panel is spawned.
    /// </summary>
    public static class ContinueReading
    {
        // Only free stories and stories already purchased with coins.
        public static List<Story> FilterVisible(Story[] stories)
        {
            var visible = new List<Story>();
            if (stories == null) return visible;

            foreach (Story story in stories)
            {
                if (story == null) continue;
                if (story.priceInCoins != 0 && !story.isUnlocked) continue;
                visible.Add(story);
            }

            return visible;
        }

        // The story the child opened last; falls back to the first story of the list at 0%.
        public static (Story story, float progress) Select(List<Story> visibleStories, string childId)
        {
            if (visibleStories == null || visibleStories.Count == 0)
                return (null, 0f);

            StoryReadingRecord record = StoryProgressStore.GetLastReading(childId);

            if (record != null)
            {
                Story target = visibleStories.Find(s => s.id == record.storyId);
                if (target != null)
                {
                    float progress = record.isComplete
                        ? 1f
                        : record.totalPages > 0 ? (float)record.pagesRead / record.totalPages : 0f;
                    return (target, progress);
                }
            }

            return (visibleStories[0], 0f);
        }

        public static GameObject SpawnReadingPanel(GameObject bookReadingPanelPrefab, Story story, Transform parent)
        {
            if (story == null) return null;

            if (bookReadingPanelPrefab == null)
            {
                Debug.LogWarning("[ContinueReading] Book reading panel prefab not assigned.");
                return null;
            }

            GameObject panel = Object.Instantiate(bookReadingPanelPrefab, parent);
            panel.GetComponent<BookReadingPanelController>()?.Setup(story);
            return panel;
        }
    }
}
