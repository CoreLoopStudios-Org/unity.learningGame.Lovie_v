using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class DailyStreakController : MonoBehaviour
    {
        [Header("Day Items")]
        [SerializeField] private DailyStreakDayView[] dayViews;

        [Tooltip("Display fallback until the backend sends dailyRewardCoins — keep in sync with the backend table.")]
        [SerializeField] private int[] fallbackDayCoins = { 10, 20, 30, 40, 50, 75, 100 };

        [Header("Reset Timer")]
        [SerializeField] private TMP_Text resetsInText;
        [SerializeField] private string resetsTextPrefix = "Resets in: ";

        [Tooltip("The backend resets streaks on UTC calendar days.")]
        [SerializeField] private float timerRefreshSeconds = 30f;

        private const int DaysPerCycle = 7;

        private int requestId;
        private Coroutine timerRoutine;

        private void OnEnable()
        {
            _ = RefreshAsync();
            timerRoutine = StartCoroutine(TimerRoutine());
        }

        private void OnDisable()
        {
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private async Awaitable RefreshAsync()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[DailyStreakController] No child session — cannot load streak stats.");
                return;
            }

            int id = ++requestId;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                ChildStats stats = await childApi.GetStatsAsync();

                // A newer request started or the panel was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                if (stats != null)
                {
                    Render(stats);
                }
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[DailyStreakController] Failed to load streak stats: {ex.Message}");
            }
        }

        private void Render(ChildStats stats)
        {
            int[] coins = ResolveCoinTable(stats);

            // Days 1..7 repeat every week of the streak.
            int claimedDays = stats.loginStreak > 0 ? ((stats.loginStreak - 1) % DaysPerCycle) + 1 : 0;

            if (stats.canClaimDailyReward && IsStreakBroken(stats))
            {
                // Missed a day — next claim restarts the track at day 1.
                claimedDays = 0;
            }

            if (dayViews != null)
            {
                for (int i = 0; i < dayViews.Length; i++)
                {
                    if (dayViews[i] == null) continue;

                    int coinAmount = i < coins.Length ? coins[i] : 0;
                    dayViews[i].SetState(i < claimedDays, coinAmount);
                }
            }
        }

        private int[] ResolveCoinTable(ChildStats stats)
        {
            if (stats.dailyRewardCoins != null && stats.dailyRewardCoins.Length == DaysPerCycle)
            {
                return stats.dailyRewardCoins;
            }

            return fallbackDayCoins;
        }

        private static bool IsStreakBroken(ChildStats stats)
        {
            // loginStreak still reports the old run until the next claim resets it;
            // if the last claim was 2+ UTC days ago that run is already dead.
            if (string.IsNullOrEmpty(stats.lastLoginDate)) return false;

            if (!DateTime.TryParse(
                    stats.lastLoginDate,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime lastLogin))
            {
                return false;
            }

            return DateTime.UtcNow.Date > lastLogin.ToUniversalTime().Date.AddDays(1);
        }

        private IEnumerator TimerRoutine()
        {
            var wait = new WaitForSeconds(timerRefreshSeconds);

            while (true)
            {
                UpdateResetTimer();
                yield return wait;
            }
        }

        private void UpdateResetTimer()
        {
            if (resetsInText == null) return;

            TimeSpan remaining = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            resetsInText.text = $"{resetsTextPrefix}{remaining.Hours}h {remaining.Minutes}m";
        }
    }
}
