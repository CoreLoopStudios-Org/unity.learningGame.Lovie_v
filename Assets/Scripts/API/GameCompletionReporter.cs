using UnityEngine;
using Api.Models;

namespace Api
{
    public class GameCompletionReporter : MonoBehaviour
    {
        [Header("Game Identity")]
        [SerializeField] private string gameId = "story_quest";

        [Header("Score Tracking")]
        [SerializeField] private bool trackDuration = true;
        [SerializeField] private int scorePerCorrectItem = 10;
        [SerializeField] private int maxScore = 100;

        [Header("Optional Direct Bindings")]
        [SerializeField] private bool autoReportOnDisable = false;

        private float sessionStartTime;
        private int currentScore;
        private int correctCount;
        private int totalCount;
        private bool hasReported;

        private void Start()
        {
            sessionStartTime = Time.time;
        }

        private void OnDisable()
        {
            if (autoReportOnDisable && !hasReported && currentScore > 0)
            {
                ReportCompletion(correctCount, totalCount);
            }
        }

        public void ReportCompletion(int correct, int total, int overrideScore = -1)
        {
            if (hasReported) return;
            hasReported = true;

            correctCount = correct;
            totalCount = total;
            currentScore = overrideScore >= 0 ? overrideScore : CalculateScore(correct, total);

            float duration = trackDuration ? (Time.time - sessionStartTime) : 0f;

            var result = new GameResult
            {
                GameId = gameId,
                Score = currentScore,
                CorrectCount = correctCount,
                TotalCount = totalCount,
                DurationSeconds = duration
            };

            GameCompletionService.Instance?.ReportCompletion(result);
        }

        public void ReportCompletionWithScore(int score, int correct, int total)
        {
            ReportCompletion(correct, total, score);
        }

        private int CalculateScore(int correct, int total)
        {
            if (total == 0) return 0;
            int calculated = correct * scorePerCorrectItem;
            return Mathf.Min(calculated, maxScore);
        }

        public void SetGameId(string id)
        {
            gameId = id;
        }

        public void ResetTracking()
        {
            sessionStartTime = Time.time;
            currentScore = 0;
            correctCount = 0;
            totalCount = 0;
            hasReported = false;
        }
    }
}

namespace ImagineMe.API
{
    // Alias forwarder
    public class GameCompletionReporter : Api.GameCompletionReporter
    {
    }
}
