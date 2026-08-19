using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Api.Endpoints;
using Api.Models;
using Newtonsoft.Json;

namespace Api
{
    [Serializable]
    public class StoreItemMetadata
    {
        public string avatarPartId;
        public string category;
        public string description;
    }

    /// <summary>
    /// Service managing store catalog, purchases, and avatar unlock mappings.
    /// </summary>
    public class StoreService : MonoBehaviour
    {
        private static StoreService instance;
        public static StoreService Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("StoreService");
                    instance = go.AddComponent<StoreService>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly List<StoreItem> _cachedCatalog = new();
        private readonly List<Purchase> _cachedPurchases = new();
        private readonly HashSet<string> _ownedItemIds = new();
        private readonly HashSet<string> _unlockedAvatarPartIds = new();

        public event Action<Purchase> OnPurchaseCompleted;
        public event Action OnCatalogRefreshed;
        public event Action OnPurchasesRefreshed;

        public IReadOnlyList<StoreItem> Catalog => _cachedCatalog;
        public IReadOnlyList<Purchase> Purchases => _cachedPurchases;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Awaitable<List<StoreItem>> GetStoreItemsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedCatalog.Count > 0)
            {
                return _cachedCatalog;
            }

            try
            {
                var childApi = new ChildApi(ApiClient.Instance);
                var items = await childApi.GetStoreItemsAsync();
                _cachedCatalog.Clear();
                if (items != null)
                {
                    _cachedCatalog.AddRange(items);
                }
                OnCatalogRefreshed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StoreService] Failed to load store catalog: {ex.Message}");
            }

            return _cachedCatalog;
        }

        public async Awaitable<List<Purchase>> GetMyPurchasesAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedPurchases.Count > 0)
            {
                return _cachedPurchases;
            }

            try
            {
                var childApi = new ChildApi(ApiClient.Instance);
                var purchases = await childApi.GetMyItemsAsync();
                _cachedPurchases.Clear();
                _ownedItemIds.Clear();
                _unlockedAvatarPartIds.Clear();

                if (purchases != null)
                {
                    _cachedPurchases.AddRange(purchases);
                    foreach (var p in purchases)
                    {
                        if (p == null) continue;
                        if (!string.IsNullOrEmpty(p.storeItemId))
                        {
                            _ownedItemIds.Add(p.storeItemId);
                        }

                        // Map avatar part ID from store items in catalog
                        var matchingItem = _cachedCatalog.FirstOrDefault(i => i.id == p.storeItemId);
                        if (matchingItem != null && !string.IsNullOrEmpty(matchingItem.metadata))
                        {
                            ParseAndRegisterMetadata(matchingItem.metadata);
                        }
                    }
                }

                OnPurchasesRefreshed?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StoreService] Failed to load purchases: {ex.Message}");
            }

            return _cachedPurchases;
        }

        public bool IsItemPurchased(string storeItemId)
        {
            if (string.IsNullOrEmpty(storeItemId)) return false;
            return _ownedItemIds.Contains(storeItemId);
        }

        public bool IsPartUnlocked(string avatarPartId)
        {
            if (string.IsNullOrEmpty(avatarPartId)) return false;
            return _unlockedAvatarPartIds.Contains(avatarPartId);
        }

        public async Awaitable<(bool Success, string Message, Purchase Purchase)> PurchaseItemAsync(StoreItem item)
        {
            if (item == null)
            {
                return (false, "Invalid item selected.", null);
            }

            if (IsItemPurchased(item.id))
            {
                return (false, "You already own this item.", null);
            }

            int currentCoins = CoinWallet.Instance != null ? CoinWallet.Instance.Balance : 0;
            if (currentCoins < item.priceInCoins)
            {
                return (false, "Not enough coins to complete purchase.", null);
            }

            try
            {
                var childApi = new ChildApi(ApiClient.Instance);
                var purchase = await childApi.PurchaseItemAsync(item.id);

                if (purchase != null)
                {
                    _cachedPurchases.Add(purchase);
                    _ownedItemIds.Add(item.id);

                    if (!string.IsNullOrEmpty(item.metadata))
                    {
                        ParseAndRegisterMetadata(item.metadata);
                    }

                    // Instant wallet balance refresh
                    if (CoinWallet.Instance != null)
                    {
                        await CoinWallet.Instance.RefreshAsync();
                    }

                    OnPurchaseCompleted?.Invoke(purchase);
                    return (true, "Purchase successful!", purchase);
                }

                return (false, "Purchase failed on server.", null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StoreService] Purchase exception: {ex.Message}");
                return (false, $"Purchase failed: {ex.Message}", null);
            }
        }

        private void ParseAndRegisterMetadata(string metadataJson)
        {
            try
            {
                var meta = JsonConvert.DeserializeObject<StoreItemMetadata>(metadataJson);
                if (meta != null && !string.IsNullOrEmpty(meta.avatarPartId))
                {
                    _unlockedAvatarPartIds.Add(meta.avatarPartId);
                }
            }
            catch
            {
                // Metadata might not be JSON or could be empty string
            }
        }
    }
}
