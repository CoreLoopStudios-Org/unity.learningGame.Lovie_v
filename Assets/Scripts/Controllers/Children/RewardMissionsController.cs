using System.Collections.Generic;
using UnityEngine;
using Api;

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

            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.UpdateBalance(CoinWallet.Instance.Balance + definition.coinReward);
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
