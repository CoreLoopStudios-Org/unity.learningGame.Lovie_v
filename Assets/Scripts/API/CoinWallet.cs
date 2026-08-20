using UnityEngine;
using System;
using Api.Endpoints;

namespace Api
{
    public class CoinWallet : MonoBehaviour
    {
        private static CoinWallet instance;
        public static CoinWallet Instance => instance;

        private int currentBalance;
        private int currentStreak;
        private bool isRefreshing;

        public int Balance => currentBalance;
        public int Streak => currentStreak;

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnStreakChanged;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Awaitable RefreshAsync()
        {
            if (isRefreshing)
                return;

            if (!SessionManager.Instance.IsAuthenticated)
                return;

            isRefreshing = true;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                var stats = await childApi.GetStatsAsync();

                if (stats != null)
                {
                    UpdateBalance(stats.coins);
                    UpdateStreak(stats.loginStreak);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to refresh coin wallet: {e.Message}");
            }
            finally
            {
                isRefreshing = false;
            }
        }

        public void UpdateBalance(int newBalance)
        {
            if (currentBalance != newBalance)
            {
                currentBalance = newBalance;
                OnBalanceChanged?.Invoke(currentBalance);
            }
        }

        public void UpdateStreak(int newStreak)
        {
            if (currentStreak != newStreak)
            {
                currentStreak = newStreak;
                OnStreakChanged?.Invoke(currentStreak);
            }
        }

        public void SetBalance(int balance)
        {
            currentBalance = balance;
        }

        public void SetStreak(int streak)
        {
            currentStreak = streak;
        }
    }
}
