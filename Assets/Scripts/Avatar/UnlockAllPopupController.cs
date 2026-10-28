using Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Avatar
{
    /// <summary>
    /// "Unlock all avatars" popup — local only. Deducts the price from the
    /// wallet and clears the lock flag on every avatar part in the database
    /// for the current session. Attach to the popup panel and wire refs.
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

        private void OnUnlockClicked()
        {
            if (processing)
                return;

            if (CoinWallet.Instance == null)
            {
                ShowStatus("Wallet is not available.");
                return;
            }

            if (CoinWallet.Instance.Balance < unlockPrice)
            {
                ShowStatus("Not enough coins.");
                return;
            }

            processing = true;
            SetUnlockButtonInteractable(false);

            CoinWallet.Instance.UpdateBalance(CoinWallet.Instance.Balance - unlockPrice);

            UnlockAllParts();

            // Unlock done — nothing left to buy in this session.
            if (unlockAllBanner != null)
            {
                unlockAllBanner.interactable = false;
            }

            processing = false;
            SetUnlockButtonInteractable(true);
            ClosePopup();
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

        private int UnlockAllParts()
        {
            var database = customizationManager != null ? customizationManager.Database : null;
            if (database == null)
            {
                ShowStatus("Avatar database is not assigned.");
                return 0;
            }

            int unlockedCount = 0;

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
                        unlockedCount++;
                    }
                }
            }

            return unlockedCount;
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
