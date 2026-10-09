using UnityEngine;
using System;
using Api;
using Api.Endpoints;
using Avatar;

namespace UI
{
    public class ChildSessionInitializer : MonoBehaviour
    {
        [Header("Initialization Order")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private float initializationDelay = 0.1f;

        private async void Start()
        {
            if (!autoInitializeOnStart)
                return;

            if (!SessionManager.Instance.IsChildSession)
                return;

            await Awaitable.WaitForSecondsAsync(initializationDelay);

            await InitializeAsync();
        }

        public async Awaitable InitializeAsync()
        {
            EnsureCoinWallet();
            await TryClaimDailyRewardAsync();
            await InitializeCoinWallet();
            await InitializeAvatarSync();
        }

        // Session auto-resume skips ChildLoginController, which is where the daily
        // reward was claimed — so claim here instead.
        private async Awaitable TryClaimDailyRewardAsync()
        {
            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                var stats = await childApi.GetStatsAsync();
                if (stats == null || !stats.canClaimDailyReward)
                    return;

                var result = await childApi.ClaimDailyRewardAsync();

                if (result != null && !result.alreadyClaimed && CoinWallet.Instance != null)
                {
                    CoinWallet.Instance.UpdateBalance(result.totalCoins);
                    CoinWallet.Instance.UpdateStreak(result.loginStreak);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ChildSessionInitializer] Daily reward claim failed: {ex.Message}");
            }
        }

        private void EnsureCoinWallet()
        {
            if (CoinWallet.Instance == null)
            {
                var go = new GameObject("CoinWallet");
                go.AddComponent<CoinWallet>();
            }
        }

        private async Awaitable InitializeCoinWallet()
        {
            if (CoinWallet.Instance != null)
            {
                await CoinWallet.Instance.RefreshAsync();
            }
        }

        private async Awaitable InitializeAvatarSync()
        {
            if (AvatarSyncService.Instance == null)
            {
                var go = new GameObject("AvatarSyncService");
                go.AddComponent<AvatarSyncService>();
            }

            if (AvatarSyncService.Instance != null)
            {
                var avatarManager = FindObjectOfType<AvatarCustomizationManager>();
                if (avatarManager != null)
                {
                    AvatarSyncService.Instance.SetAvatarManager(avatarManager);
                }

                await AvatarSyncService.Instance.LoadFromServerAsync();
            }
        }
    }
}
