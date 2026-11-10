using System;
using System.Collections.Generic;
using UnityEngine;
using Api;
using Api.Endpoints;
using Api.Models;
using Avatar;

namespace UI
{
    // Orchestrates the Main Menu leaderboard page: one board page fetch feeds both the
    // podium and the rank list; the own-rank card comes from /rank/{childId} plus the
    // locally cached avatarState (the rank endpoint returns no avatar).
    public class LeaderboardPageController : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private LeaderboardBoardType boardType = LeaderboardBoardType.Weekly;
        [SerializeField, Range(1, 100)] private int pageSize = 10;

        private const int PodiumSize = 3;

        [Header("Views")]
        [SerializeField] private LeaderboardPodiumView podiumView;
        [SerializeField] private LeaderboardOwnRankView ownRankView;
        [SerializeField] private LeaderboardRankListView rankListView;

        private int requestId;
        private LeaderboardApi leaderboardApi;

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            int id = ++requestId;

            var apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            leaderboardApi = new LeaderboardApi(apiClient);

            if (rankListView != null)
            {
                rankListView.ResetList();
                rankListView.PageFetcher = FetchPageAsync;
            }

            bool hasChildSession = SessionManager.Instance != null && SessionManager.Instance.IsChildSession;
            if (ownRankView != null)
                ownRankView.gameObject.SetActive(hasChildSession);

            // Board is anonymous; the rank endpoint needs the child's id (404 if deleted).
            string childId = hasChildSession ? SessionManager.Instance.ChildId : null;
            Awaitable<ChildRank> rankTask = !string.IsNullOrEmpty(childId)
                ? leaderboardApi.GetRankAsync(childId)
                : null;

            try
            {
                LeaderboardPage page1 = await FetchPageAsync(1);

                // Panel disabled or re-entered while awaiting.
                if (id != requestId || !isActiveAndEnabled || this == null)
                    return;

                if (podiumView != null)
                    podiumView.Render(TopThree(page1));

                // The podium shows the top three; the list continues from rank 4.
                if (rankListView != null)
                    await rankListView.AppendPageAsync(page1, skipFirst: PodiumSize);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardPageController] Failed to load leaderboard: {ex.Message}");
            }

            if (rankTask == null || id != requestId)
                return;

            try
            {
                ChildRank rank = await rankTask;
                if (id != requestId || !isActiveAndEnabled || this == null)
                    return;

                ownRankView?.SetRank(rank, boardType, AvatarSyncService.GetCachedAvatarState());
            }
            catch (ApiException ex) when (ex.responseCode == 404)
            {
                // Child deleted server-side — render the unranked card instead.
                if (id == requestId && ownRankView != null)
                    ownRankView.SetRank(null, boardType, AvatarSyncService.GetCachedAvatarState());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardPageController] Failed to load rank: {ex.Message}");
            }
        }

        private async Awaitable<LeaderboardPage> FetchPageAsync(int page)
        {
            return boardType == LeaderboardBoardType.AllTime
                ? await leaderboardApi.GetAllTimePageAsync(page, pageSize)
                : await leaderboardApi.GetWeeklyPageAsync(page, pageSize);
        }

        private static IReadOnlyList<LeaderboardEntry> TopThree(LeaderboardPage page)
        {
            List<LeaderboardEntry> top = new List<LeaderboardEntry>(3);

            if (page?.entries == null)
                return top;

            for (int i = 0; i < page.entries.Count && i < PodiumSize; i++)
            {
                if (page.entries[i] != null)
                    top.Add(page.entries[i]);
            }

            return top;
        }
    }
}
