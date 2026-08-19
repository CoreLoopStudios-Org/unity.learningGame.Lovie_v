using System;

namespace Api.Models
{
    [Serializable]
    public sealed class GameResult
    {
        public string GameId;           // "story_quest", "rhyme_time", "word_match", etc.
        public int Score;
        public int CorrectCount;
        public int TotalCount;
        public float DurationSeconds;
    }

    [Serializable]
    public sealed class GameCompletionRequest
    {
        public string gameId;
        public int score;
        public int correctCount;
        public int totalCount;
        public float durationSeconds;
    }

    [Serializable]
    public sealed class GameCompletionResponse
    {
        public string id;
        public int activityType;
        public string createdAt;
        public int coinsEarned;
        public int totalCoins;
        public string message;
    }
}

namespace ImagineMe.API.Models
{
    // Alias / backward-compatibility forwarder
    [Serializable]
    public sealed class GameResult
    {
        public string GameId;
        public int Score;
        public int CorrectCount;
        public int TotalCount;
        public float DurationSeconds;

        public static implicit operator Api.Models.GameResult(GameResult r)
        {
            if (r == null) return null;
            return new Api.Models.GameResult
            {
                GameId = r.GameId,
                Score = r.Score,
                CorrectCount = r.CorrectCount,
                TotalCount = r.TotalCount,
                DurationSeconds = r.DurationSeconds
            };
        }
    }
}
