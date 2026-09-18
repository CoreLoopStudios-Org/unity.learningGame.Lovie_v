using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminCoinPacksPanelController : MonoBehaviour
    {
        [Header("Cards (matched to packs by Store Product Id)")]
        [SerializeField] private AdminCoinPackCard[] cards;

        [Header("Edit Popup")]
        [SerializeField] private AdminEditStoryPricePopup editPopupPrefab;
        [SerializeField] private Transform popupParent;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private bool isUpdating;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);
        }

        private void OnEnable()
        {
            SetCardsActive(true);
            _ = LoadPacksAsync();
        }

        private void OnDisable()
        {
            SetCardsActive(false);
        }

        private void SetCardsActive(bool subscribe)
        {
            if (cards == null) return;

            foreach (AdminCoinPackCard card in cards)
            {
                if (card == null) continue;

                if (subscribe) card.EditClicked += OnCardEditClicked;
                else card.EditClicked -= OnCardEditClicked;
            }
        }

        private async Awaitable LoadPacksAsync()
        {
            ShowStatus("Loading coin packs...");

            try
            {
                StoreItem[] packs = await adminApi.GetStoreItemsAsync();

                BindPacks(packs);
                ClearStatus();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdminCoinPacksPanelController] Failed to load coin packs: {ex.Message}");
                ShowStatus($"Failed to load coin packs: {ex.Message}");
            }
        }

        private void BindPacks(StoreItem[] packs)
        {
            if (packs == null || packs.Length == 0)
            {
                Debug.LogWarning("[AdminCoinPacksPanelController] Server returned no coin packs.");
                ShowStatus("Server returned no coin packs.");
                return;
            }

            string availableIds = string.Join(", ",
                System.Linq.Enumerable.Select(packs, p => p == null ? "<null>" : $"'{p.storeProductId}' ({p.rewardCoinAmount} coins)"));
            Debug.Log($"[AdminCoinPacksPanelController] Packs from server: {availableIds}");

            if (cards == null) return;

            foreach (AdminCoinPackCard card in cards)
            {
                if (card == null) continue;

                if (string.IsNullOrWhiteSpace(card.StoreProductId))
                {
                    Debug.LogWarning($"[AdminCoinPacksPanelController] Card '{card.name}' has no Store Product Id set in the Inspector.");
                    ShowStatus($"Card '{card.name}' has no Store Product Id set.");
                    continue;
                }

                StoreItem match = FindPack(packs, card.StoreProductId);
                if (match != null)
                {
                    card.Bind(match);
                }
                else
                {
                    Debug.LogWarning($"[AdminCoinPacksPanelController] No pack matches product id '{card.StoreProductId}' (card '{card.name}'). See the server pack list logged above.");
                    ShowStatus($"No pack found for product id '{card.StoreProductId}'.");
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

        private void OnCardEditClicked(AdminCoinPackCard card)
        {
            if (card == null || editPopupPrefab == null || isUpdating)
                return;

            Transform parent = popupParent != null ? popupParent : transform;
            AdminEditStoryPricePopup popup = Instantiate(editPopupPrefab, parent);
            popup.Setup(card.CoinAmount);
            popup.UpdateConfirmed += (p, newAmount) => _ = UpdatePackAsync(p, card, newAmount);
            popup.Dismissed += p => Destroy(p.gameObject);
        }

        private async Awaitable UpdatePackAsync(AdminEditStoryPricePopup popup, AdminCoinPackCard card, int newAmount)
        {
            if (isUpdating) return;

            if (!SessionManager.Instance.IsValidToken())
            {
                popup.ShowError("No valid session.");
                return;
            }

            if (string.IsNullOrEmpty(card.PackId))
            {
                popup.ShowError("This card has no pack loaded yet.");
                return;
            }

            isUpdating = true;
            popup.SetInteractable(false);

            try
            {
                // PUT fields are optional per docs, so only the reward amount is sent.
                await adminApi.UpdateStoreItemRewardAsync(card.PackId, newAmount);

                if (popup != null) Destroy(popup.gameObject);
                card.SetAmount(newAmount);
            }
            catch (ApiException ex)
            {
                popup.SetInteractable(true);
                popup.ShowError($"Failed to update coins: {ex.Message}");
            }
            catch (Exception ex)
            {
                popup.SetInteractable(true);
                popup.ShowError($"Failed to update coins: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
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
    }
}
