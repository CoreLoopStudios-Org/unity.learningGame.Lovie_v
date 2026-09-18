using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminCoinPackCard : MonoBehaviour
    {
        // Must equal the pack's storeProductId in the backend store-items table,
        // e.g. "store_pack_1" — it is how this premade card finds its pack.
        [SerializeField] private string storeProductId;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button editButton;

        public string StoreProductId => storeProductId;
        public string PackId => pack != null ? pack.id : null;
        public int CoinAmount => pack != null ? pack.rewardCoinAmount : 0;

        public event Action<AdminCoinPackCard> EditClicked;

        private StoreItem pack;

        private void Awake()
        {
            if (editButton != null)
                editButton.onClick.AddListener(() => EditClicked?.Invoke(this));
        }

        private void OnDestroy()
        {
            if (editButton != null)
                editButton.onClick.RemoveAllListeners();
        }

        public void Bind(StoreItem storeItem)
        {
            pack = storeItem;
            RefreshAmount();
        }

        public void SetAmount(int rewardCoinAmount)
        {
            if (pack != null)
                pack.rewardCoinAmount = rewardCoinAmount;
            RefreshAmount();
        }

        private void RefreshAmount()
        {
            if (amountText != null)
                amountText.text = CoinAmount.ToString();
        }
    }
}
