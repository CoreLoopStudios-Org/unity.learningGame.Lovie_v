using System;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class StoreCoinPacksScrollController : MonoBehaviour
    {
        [Header("Cards (matched to packs by Store Product Id)")]
        [SerializeField] private StoreCoinPackCard[] cards;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private int requestId;

        private void OnEnable()
        {
            _ = LoadPacksAsync();
        }

        private async Awaitable LoadPacksAsync()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[StoreCoinPacksScrollController] No child session — cannot load coin packs.");
                return;
            }

            int id = ++requestId;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                StoreItem[] packs = await childApi.GetStoreItemsAsync();

                // A newer request started or the panel was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                BindPacks(packs);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load coin packs: {ex.Message}");
            }
        }

        private void BindPacks(StoreItem[] packs)
        {
            if (packs == null || packs.Length == 0)
            {
                Debug.LogWarning("[StoreCoinPacksScrollController] Server returned no coin packs.");
                ShowStatus("No coin packs available.");
                return;
            }

            if (cards == null) return;

            ClearStatus();

            foreach (StoreCoinPackCard card in cards)
            {
                if (card == null) continue;

                StoreItem match = FindPack(packs, card.StoreProductId);
                if (match != null)
                {
                    card.Bind(match);
                }
                else
                {
                    Debug.LogWarning(
                        $"[StoreCoinPacksScrollController] No pack matches product id '{card.StoreProductId}' (card '{card.name}').");
                }
            }
        }

        private static StoreItem FindPack(StoreItem[] packs, string storeProductId)
        {
            if (packs == null) return null;

            foreach (StoreItem item in packs)
            {
                if (item != null && string.Equals(item.storeProductId, storeProductId, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
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
                statusFeedbackText.gameObject.SetActive(false);
            }
        }
    }
}
