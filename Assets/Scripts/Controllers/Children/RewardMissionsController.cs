using System.Collections.Generic;
using UnityEngine;
using Api;
using Api.Endpoints;

namespace UI
{
    public class RewardMissionsController : MonoBehaviour
    {
        [Header("Definitions")]
        [SerializeField] private RewardCatalog catalog;

        [Header("List")]
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private RewardCard rewardCardPrefab;

        private readonly List<RewardCard> spawnedCards = new List<RewardCard>();

        private void OnEnable()
        {
            RewardProgressStore.OnProgressChanged += HandleProgressChanged;
            SpawnCards();
        }

        private void OnDisable()
        {
            RewardProgressStore.OnProgressChanged -= HandleProgressChanged;
            ClearSpawnedCards();
        }

        private void SpawnCards()
        {
            ClearSpawnedCards();

            if (catalog == null || catalog.Rewards == null || catalog.Rewards.Count == 0) return;

            if (rewardsContainer == null || rewardCardPrefab == null)
            {
                Debug.LogWarning("[RewardMissionsController] Rewards container or card prefab not assigned.", this);
                return;
            }

            foreach (RewardDefinition definition in catalog.Rewards)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id)) continue;

                RewardCard card = Instantiate(rewardCardPrefab, rewardsContainer);
                card.Setup(definition);
                card.ClaimClicked += OnClaimClicked;
                spawnedCards.Add(card);
            }
        }

        private void HandleProgressChanged(string rewardId)
        {
            foreach (RewardCard card in spawnedCards)
            {
                if (card != null && card.Definition != null && card.Definition.id == rewardId)
                {
                    card.Refresh();
                }
            }
        }

        private void OnClaimClicked(RewardCard card)
        {
            if (card == null || card.Definition == null) return;

            ClaimReward(card.Definition);
            card.Refresh();
        }
        
        private void ClaimReward(RewardDefinition definition)
        {
            if (RewardProgressStore.IsClaimed(definition.id, definition.resetType)) return;
            if (!RewardProgressStore.IsComplete(definition.id, definition.targetCount, definition.resetType)) return;

            RewardProgressStore.MarkClaimed(definition.id, definition.resetType);

            // Instant UI grant; the backend call then reconciles the authoritative total.
            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.UpdateBalance(CoinWallet.Instance.Balance + definition.coinReward);
            }

            SyncClaimToBackendAsync(definition);
        }

        private async void SyncClaimToBackendAsync(RewardDefinition definition)
        {
            // The backend add-coins endpoint does no dedup — RewardProgressStore's local
            // claim record is the only double-claim guard, so it must be set before this call.
            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                int totalCoins = await childApi.AddCoinsAsync(definition.coinReward);
                if (CoinWallet.Instance != null)
                {
                    CoinWallet.Instance.UpdateBalance(totalCoins);
                }
            }
            catch (System.Exception ex)
            {
                // The grant never reached the server while the UI already credited it —
                // resync the authoritative balance to undo the local credit.
                Debug.LogWarning($"[RewardMissionsController] Reward claim sync failed: {ex.Message}");
                if (CoinWallet.Instance != null)
                {
                    await CoinWallet.Instance.RefreshAsync();
                }
            }
        }

        private void ClearSpawnedCards()
        {
            foreach (RewardCard card in spawnedCards)
            {
                if (card != null)
                {
                    card.ClaimClicked -= OnClaimClicked;
                    Destroy(card.gameObject);
                }
            }
            spawnedCards.Clear();
        }
    }
}
