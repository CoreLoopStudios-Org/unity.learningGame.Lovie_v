using System;
using UnityEngine;
using Api.Models;

namespace Api.Endpoints
{
    // Leaderboard reads are fully anonymous — no token required
    // (LEADERBOARD_API_UNITY.md §1). ApiClient attaching a stale token is harmless.
    public class LeaderboardApi
    {
        private readonly ApiClient client;

        public LeaderboardApi(ApiClient apiClient)
        {
            client = apiClient;
        }

        // page >= 1, pageSize 1–100 (server clamps). Pages are cached server-side
        // 30–60s — do not poll; refresh on screen entry instead (§5).
        public async Awaitable<LeaderboardPage> GetWeeklyPageAsync(int page = 1, int pageSize = 10)
        {
            return await client.GetAsync<LeaderboardPage>($"/api/leaderboard/weekly?page={page}&pageSize={pageSize}");
        }

        public async Awaitable<LeaderboardPage> GetAllTimePageAsync(int page = 1, int pageSize = 10)
        {
            return await client.GetAsync<LeaderboardPage>($"/api/leaderboard/alltime?page={page}&pageSize={pageSize}");
        }

        // 404 when the childId doesn't exist (deleted child).
        public async Awaitable<ChildRank> GetRankAsync(string childId)
        {
            return await client.GetAsync<ChildRank>($"/api/leaderboard/rank/{childId}");
        }
    }
}
