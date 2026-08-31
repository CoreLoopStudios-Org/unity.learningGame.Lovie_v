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

            try
            {
                AdminStats stats = await adminApi.GetStatsAsync();

                // A newer request started or panel was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                ApplyStats(stats);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminHomePanelController] Failed to load stats: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminHomePanelController] Failed to load stats: {ex.Message}");
            }
        }

        private void ApplyStats(AdminStats stats)
        {
            if (stats == null) return;

            if (totalEarningsText != null) totalEarningsText.text = $"${stats.totalEarnings}";
            if (totalStoriesText != null) totalStoriesText.text = stats.totalStories.ToString();
            if (totalKidsUsersText != null) totalKidsUsersText.text = stats.activeChildren.ToString();
            if (totalParentsText != null) totalParentsText.text = stats.totalUsers.ToString();

            FillSlots(storySlots, stats.mostWatchedStories);
            FillSlots(gameSlots, stats.mostPlayedGames);
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
