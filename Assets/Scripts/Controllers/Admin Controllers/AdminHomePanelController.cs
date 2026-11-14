using System;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Attach to the "Admin Dashboard (Home Page)" panel.
    /// Fetches fresh stats every time the panel is enabled (navigation switches
    /// panels via SetActive, which re-triggers OnEnable).
    /// </summary>
    public class AdminHomePanelController : MonoBehaviour
    {
        [Header("Summary Stats")]
        [SerializeField] private TextMeshProUGUI totalEarningsText;
        [SerializeField] private TextMeshProUGUI totalStoriesText;
        [SerializeField] private TextMeshProUGUI totalKidsUsersText;
        [SerializeField] private TextMeshProUGUI totalParentsText;

        [Header("Top Content Slots (3 each, in display order)")]
        [SerializeField] private AdminTopContentCard[] storySlots;
        [SerializeField] private AdminTopContentCard[] gameSlots;

        // topN for the analytics endpoints — matches the fixed slot count.
        private const int TopContentCount = 3;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminHomePanelController] No valid session, skipping stats load.");
                return;
            }

            int id = ++requestId;

            AdminStats stats = await FetchQuietlyAsync(adminApi.GetStatsAsync(), "stats");
            StoryWatchStats[] watchedStories = await FetchQuietlyAsync(adminApi.GetMostWatchedStoriesAsync(TopContentCount), "most-watched-stories");
            GamePlayStats[] playedGames = await FetchQuietlyAsync(adminApi.GetMostPlayedGamesAsync(TopContentCount), "most-played-games");

            // A newer request started or panel was disabled while awaiting.
            if (id != requestId || !isActiveAndEnabled) return;

            ApplySummary(stats);
            FillSlots(storySlots, MapContent(watchedStories, s => new TopContent { name = s.title, category = s.category, thumbnailUrl = s.thumbnailUrl }));
            FillSlots(gameSlots, MapContent(playedGames, g => new TopContent { name = g.title, category = g.gameType, thumbnailUrl = g.thumbnailUrl }));
        }

        // One list failing (e.g. analytics endpoint not deployed yet) must not
        // blank the rest of the panel — return null, the slots just clear.
        private async Awaitable<T> FetchQuietlyAsync<T>(Awaitable<T> request, string label) where T : class
        {
            try
            {
                return await request;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdminHomePanelController] Failed to load {label}: {ex.Message}");
                return null;
            }
        }

        private void ApplySummary(AdminStats stats)
        {
            if (stats == null) return;

            if (totalEarningsText != null) totalEarningsText.text = $"${stats.totalEarnings}";
            if (totalStoriesText != null) totalStoriesText.text = stats.totalStories.ToString();
            if (totalKidsUsersText != null) totalKidsUsersText.text = stats.totalChildren.ToString();
            if (totalParentsText != null) totalParentsText.text = stats.totalParents.ToString();
        }

        private static TopContent[] MapContent<T>(T[] items, Func<T, TopContent> map)
        {
            if (items == null) return null;

            var result = new TopContent[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                result[i] = items[i] == null ? null : map(items[i]);
            }
            return result;
        }

        /// <summary>
        /// Fills the fixed design slots in order. Slots beyond the returned
        /// data (or all slots when there is no data) are cleared to empty.
        /// </summary>
        private void FillSlots(AdminTopContentCard[] slots, TopContent[] items)
        {
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                TopContent content = (items != null && i < items.Length) ? items[i] : null;
                slots[i].Setup(content);
            }
        }
    }
}
