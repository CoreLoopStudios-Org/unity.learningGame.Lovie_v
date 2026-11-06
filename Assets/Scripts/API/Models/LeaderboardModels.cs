using System;
using System.Collections.Generic;

namespace Api.Models
{
    public enum LeaderboardBoardType
    {
        Weekly,
        AllTime,
    }

    // GET /api/leaderboard/weekly + /api/leaderboard/alltime (LEADERBOARD_API_UNITY.md §3.1–3.2).
    // avatarState is the raw JSON config string — parse with AvatarSlotRenderer, do not deserialize here.
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string childId;
        public string username;
        public string fullName;
        public string avatarState;
        public int score;
    }

    [Serializable]
    public class LeaderboardPage
    {
        public List<LeaderboardEntry> entries;
        public int totalCount;
        public int page;
        public int pageSize;
        public int totalPages;
        public string weekStart; // null on the all-time board
    }

    // GET /api/leaderboard/rank/{childId} (LEADERBOARD_API_UNITY.md §3.3).
    // weeklyRank/totalRank are null while the child has 0 points on that board —
    // render "Unranked", never 0. The nextRank/pointsToNextRank pairs are null
    // while unranked or already #1 (nothing to chase).
    [Serializable]
    public class ChildRank
    {
        public string childId;
        public string username;
        public int weeklyScore;
        public int? weeklyRank;
        public int? weeklyNextRank;
        public int? weeklyPointsToNextRank;
        public int totalScore;
        public int? totalRank;
        public int? totalNextRank;
        public int? totalPointsToNextRank;
        public int currentStreakDays;
        public int longestStreakDays;
    }
}
