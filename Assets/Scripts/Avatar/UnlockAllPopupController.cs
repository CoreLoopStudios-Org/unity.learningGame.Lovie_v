using System;
using Api;
using Api.Endpoints;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Avatar
{
    /// <summary>
    /// "Unlock all avatars" popup — the purchase lives on the backend
    /// (POST /api/child/avatar/unlock-all, fixed server-owned 999 coins,
    /// ECONOMY-INTEGRATION-GUIDE §6). On success the wallet is synced to the
    /// returned balance, every part unlocks for the session, and the flag is
    /// persisted through the avatar state. Attach to the popup panel and wire refs.
    /// </summary>
    public class UnlockAllPopupController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarCustomizationManager customizationManager;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button unlockAllBanner;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI statusText;
        [Tooltip("Panel to close on success. Defaults to this GameObject.")]
        [SerializeField] private GameObject popupPanel;

        [Header("Settings")]
        [Tooltip("Display only — the server owns the real price.")]
        [SerializeField] private int unlockPrice = 999;

        private bool processing;

        private void OnEnable()
        {
            if (unlockButton != null)
            {
                unlockButton.onClick.AddListener(OnUnlockClicked);
            }

            if (priceText != null)
            {
                priceText.text = $"{unlockPrice} coins";
            }

            ClearStatus();
        }

        private void OnDisable()
        {
            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveListener(OnUnlockClicked);
            }
        }

        private async void OnUnlockClicked()
        {
            if (processing)
                return;

            processing = true;
            SetUnlockButtonInteractable(false);

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                var result = await childApi.UnlockAllAvatarsAsync();

                if (result == null || !result.allAvatarsUnlocked)
                {
                    ShowStatus("Unlock failed. Please try again.");
                    return;
                }

                // Server already deducted the coins — sync the wallet to its balance.
                if (CoinWallet.Instance != null)
                {
                    CoinWallet.Instance.UpdateBalance(result.coins);
                }

                AvatarUnlockState.SetAllUnlocked(true);
                UnlockAllParts();
                PersistToServer();

                // Unlock done — nothing left to buy.
                if (unlockAllBanner != null)
                {
                    unlockAllBanner.interactable = false;
                }

                ClosePopup();
            }
            catch (ApiException ex)
            {
                // Server messages are player-facing ("Not enough coins. Required: 999,
                // Available: X." / "All avatars are already unlocked.") — show verbatim.
                ShowStatus(string.IsNullOrEmpty(ex.errorMessage)
                    ? "Something went wrong. Please try again."
                    : ex.errorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unlock-all failed: {ex.Message}");
                ShowStatus("Something went wrong. Please try again.");
            }
            finally
            {
                processing = false;
                SetUnlockButtonInteractable(true);
            }
        }

        // Rewrites the avatar state JSON with "isUnlocked": true and PUTs it to
        // /api/child/avatar so the unlock survives restarts (alongside the server's
        // additionalData.allAvatarsUnlocked record).
        private void PersistToServer()
        {
            if (AvatarSyncService.Instance == null)
                return;

            if (customizationManager != null)
            {
                AvatarSyncService.Instance.SetAvatarManager(customizationManager);
            }

            _ = AvatarSyncService.Instance.SaveToServerAsync();
        }

        private void ClosePopup()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void UnlockAllParts()
        {
            var database = customizationManager != null ? customizationManager.Database : null;
            if (database == null)
            {
                ShowStatus("Avatar database is not assigned.");
                return;
            }

            var categories = System.Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];
            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None)
                    continue;

                foreach (var part in database.GetPartsByCategory(category))
                {
                    if (part != null && part.IsLocked)
                    {
                        part.SetLocked(false);
                    }
                }
            }
        }

        private void SetUnlockButtonInteractable(bool value)
        {
            if (unlockButton != null)
            {
                unlockButton.interactable = value;
            }
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.gameObject.SetActive(true);
            }
        }

        private void ClearStatus()
        {
            if (statusText != null)
            {
                statusText.text = string.Empty;
                statusText.gameObject.SetActive(false);
            }
        }
    }
}
