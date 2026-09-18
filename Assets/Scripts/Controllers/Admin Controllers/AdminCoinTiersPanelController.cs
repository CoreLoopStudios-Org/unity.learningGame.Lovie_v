using System;
using UnityEngine;
using Api;
using Api.Endpoints;

namespace UI
{
    public class AdminCoinTiersPanelController : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private AdminCoinTierCard[] cards;

        [Header("Edit Popup")]
        [SerializeField] private AdminEditStoryPricePopup editPopupPrefab;
        [SerializeField] private Transform popupParent;

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
        }

        private void OnDisable()
        {
            SetCardsActive(false);
        }

        private void SetCardsActive(bool subscribe)
        {
            if (cards == null) return;

            foreach (AdminCoinTierCard card in cards)
            {
                if (card == null) continue;

                if (subscribe) card.EditClicked += OnCardEditClicked;
                else card.EditClicked -= OnCardEditClicked;
            }
        }

        private void OnCardEditClicked(AdminCoinTierCard card)
        {
            if (card == null || editPopupPrefab == null || isUpdating)
                return;

            Transform parent = popupParent != null ? popupParent : transform;
            AdminEditStoryPricePopup popup = Instantiate(editPopupPrefab, parent);
            popup.Setup(card.CoinAmount);
            popup.UpdateConfirmed += (p, newAmount) => _ = UpdateTierAsync(p, card, newAmount);
            popup.Dismissed += p => Destroy(p.gameObject);
        }

        private async Awaitable UpdateTierAsync(AdminEditStoryPricePopup popup, AdminCoinTierCard card, int newAmount)
        {
            if (isUpdating) return;

            if (!SessionManager.Instance.IsValidToken())
            {
                popup.ShowError("No valid session.");
                return;
            }

            if (!card.HasCompleteTierConfig())
            {
                popup.ShowError("This card's Tier ID / Name / Product ID is not configured.");
                return;
            }

            isUpdating = true;
            popup.SetInteractable(false);

            try
            {
                await adminApi.UpdateIapTierAsync(card.TierId, card.TierName, card.StoreProductId, newAmount);

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
    }
}
