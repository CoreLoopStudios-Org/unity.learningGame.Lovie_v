using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class AdminCoinTierCard : MonoBehaviour
    {
        [Header("Tier (must match the backend IapTiers row)")]
        [SerializeField] private string tierId;
        [SerializeField] private string tierName;
        [SerializeField] private string storeProductId;
        [SerializeField] private int currentCoinAmount;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button editButton;

        public string TierId => tierId;
        public string TierName => tierName;
        public string StoreProductId => storeProductId;
        public int CoinAmount => currentCoinAmount;

        public event Action<AdminCoinTierCard> EditClicked;

        private void Awake()
        {
            if (editButton != null)
                editButton.onClick.AddListener(() => EditClicked?.Invoke(this));

            SetAmount(currentCoinAmount);
        }

        private void OnDestroy()
        {
            if (editButton != null)
                editButton.onClick.RemoveAllListeners();
        }

        // The backend PUT replaces the whole tier, so every field must be present;
        // the id also has to parse as a Guid or the route itself won't match.
        public bool HasCompleteTierConfig()
        {
            return !string.IsNullOrWhiteSpace(tierId)
                && Guid.TryParse(tierId, out _)
                && !string.IsNullOrWhiteSpace(tierName)
                && !string.IsNullOrWhiteSpace(storeProductId);
        }

        public void SetAmount(int amount)
        {
            currentCoinAmount = amount;
            if (amountText != null)
                amountText.text = amount.ToString(CultureInfo.InvariantCulture);
        }
    }
}
