using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class StoreUIController : MonoBehaviour
    {
        [Header("Containers & Prefabs")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemCardPrefab;

        [Header("Header / Wallet UI")]
        [SerializeField] private TextMeshProUGUI coinBalanceText;
        [SerializeField] private TextMeshProUGUI statusFeedbackText;
        [SerializeField] private Button refreshButton;

        [Header("Tabs (Optional)")]
        [SerializeField] private Button allTabButton;
        [SerializeField] private Button avatarTabButton;

        private readonly List<StoreItemCard> _spawnedCards = new();
        private string _activeCategoryFilter = "All";

        private void Start()
        {
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(OnRefreshClicked);
            }

            if (allTabButton != null)
            {
                allTabButton.onClick.AddListener(() => FilterCategory("All"));
            }

            if (avatarTabButton != null)
            {
                avatarTabButton.onClick.AddListener(() => FilterCategory("Avatar"));
            }

            SubscribeEvents();
            UpdateWalletUI();
            _ = LoadStoreAsync();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.OnBalanceChanged += HandleBalanceChanged;
            }

            if (StoreService.Instance != null)
            {
                StoreService.Instance.OnPurchaseCompleted += HandlePurchaseCompleted;
            }
        }

        private void UnsubscribeEvents()
        {
            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.OnBalanceChanged -= HandleBalanceChanged;
            }

            if (StoreService.Instance != null)
            {
                StoreService.Instance.OnPurchaseCompleted -= HandlePurchaseCompleted;
            }
        }

        private async Awaitable LoadStoreAsync(bool forceRefresh = false)
        {
            ShowStatus("Loading store items...");

            var storeService = StoreService.Instance;
            if (storeService == null)
            {
                ShowStatus("Store service not ready.");
                return;
            }

            var catalog = await storeService.GetStoreItemsAsync(forceRefresh);
            await storeService.GetMyPurchasesAsync(forceRefresh);

            PopulateItems(catalog);
            ClearStatus();
        }

        private void PopulateItems(IReadOnlyList<StoreItem> items)
        {
            ClearSpawnedCards();

            if (items == null || items.Count == 0)
            {
                ShowStatus("No items available in store.");
                return;
            }

            if (itemsContainer == null || itemCardPrefab == null)
            {
                Debug.LogWarning("[StoreUIController] Items container or card prefab not assigned.");
                return;
            }

            foreach (var item in items)
            {
                if (item == null) continue;

                var cardGo = Instantiate(itemCardPrefab, itemsContainer);
                var card = cardGo.GetComponent<StoreItemCard>();
                if (card != null)
                {
                    card.Setup(item, OnBuyItemClicked);
                    _spawnedCards.Add(card);
                }
            }
        }

        private async void OnBuyItemClicked(StoreItem item)
        {
            if (item == null) return;

            ShowStatus($"Purchasing {item.name}...");

            var result = await StoreService.Instance.PurchaseItemAsync(item);
            if (result.Success)
            {
                ShowStatus($"Purchased {item.name}!");
                RefreshCardStates();
            }
            else
            {
                ShowStatus(result.Message);
            }
        }

        private void RefreshCardStates()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null)
                {
                    card.RefreshState();
                }
            }
        }

        private void FilterCategory(string category)
        {
            _activeCategoryFilter = category;
            foreach (var card in _spawnedCards)
            {
                if (card == null || card.Item == null) continue;

                if (category == "All")
                {
                    card.gameObject.SetActive(true);
                }
                else
                {
                    // Check metadata category
                    bool match = card.Item.metadata != null && card.Item.metadata.Contains(category);
                    card.gameObject.SetActive(match);
                }
            }
        }

        private void HandleBalanceChanged(int newBalance)
        {
            UpdateWalletUI();
            RefreshCardStates();
        }

        private void HandlePurchaseCompleted(Purchase purchase)
        {
            RefreshCardStates();
        }

        private void UpdateWalletUI()
        {
            if (coinBalanceText != null && CoinWallet.Instance != null)
            {
                coinBalanceText.text = CoinWallet.Instance.Balance.ToString("N0");
            }
        }

        private void ShowStatus(string message)
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = message;
                statusFeedbackText.gameObject.SetActive(true);
            }
        }

        private void ClearStatus()
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = string.Empty;
            }
        }

        private void OnRefreshClicked()
        {
            _ = LoadStoreAsync(forceRefresh: true);
        }

        private void ClearSpawnedCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _spawnedCards.Clear();
        }
    }
}
