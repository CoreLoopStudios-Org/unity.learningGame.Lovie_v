using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;
using Avatar;

namespace UI
{
    // The fixed "Own Rank Card" — the signed-in child's rank, score, name, chase line
    // and Body/Dress/Hair avatar. Rank/points data comes from GET /api/leaderboard/rank;
    // the avatar comes from the locally cached avatarState (AvatarSyncService).
    public class LeaderboardOwnRankView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI scoreToNextText; // "N pts to #M" chase line
        [SerializeField] private TextMeshProUGUI positionToNext;

        [Header("Avatar")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image dressImage;
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        public void SetRank(ChildRank rank, LeaderboardBoardType boardType, string avatarStateJson)
        {
            int? rankValue = null;
            int score = 0;
            int? nextRank = null;
            int? pointsToNext = null;
            string username = null;

            if (rank != null)
            {
                bool weekly = boardType == LeaderboardBoardType.Weekly;
                rankValue = weekly ? rank.weeklyRank : rank.totalRank;
                score = weekly ? rank.weeklyScore : rank.totalScore;
                nextRank = weekly ? rank.weeklyNextRank : rank.totalNextRank;
                pointsToNext = weekly ? rank.weeklyPointsToNextRank : rank.totalPointsToNextRank;
                username = rank.username;
            }

            if (rankText != null)
                rankText.text = rankValue.HasValue ? $"#{rankValue.Value}" : "Unranked";

            if (nameText != null)
                nameText.text = username ?? string.Empty;

            if (scoreText != null)
                scoreText.text = score.ToString();

            UpdateChaseLine(nextRank, pointsToNext);

            AvatarSlotRenderer.Apply(
                avatarStateJson,
                AvatarSlotRenderer.ResolveDatabase(avatarDatabase),
                bodyImage, hairImage, dressImage);
        }

        private void UpdateChaseLine(int? nextRank, int? pointsToNext)
        {
            if (scoreToNextText == null)
                return;

            // Null (unranked or already #1) or zero — nothing to chase, hide the line.
            if (nextRank.HasValue && pointsToNext.HasValue && pointsToNext.Value > 0)
            {
                scoreToNextText.text = $"{pointsToNext.Value}";
                positionToNext.text = $"#{nextRank.Value}";
                scoreToNextText.gameObject.SetActive(true);
            }
            else
            {
                scoreToNextText.text = string.Empty;
                scoreToNextText.gameObject.SetActive(false);
            }
        }
    }
}
