using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class StoreItemCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private TextMeshProUGUI buyButtonText;

        private StoreItem _item;
        private Action<StoreItem> _onBuyClicked;

        public StoreItem Item => _item;

        public void Setup(StoreItem item, Action<StoreItem> onBuyClicked)
        {
            _item = item;
            _onBuyClicked = onBuyClicked;

            if (nameText != null)
            {
                nameText.text = item.name;
            }

            if (priceText != null)
            {
                priceText.text = $"{item.priceInCoins} Coins";
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(HandleBuyClick);
            }

            LoadAssetImage(item.assetUrl);
            RefreshState();
        }

        public void RefreshState()
        {
            if (_item == null) return;

            bool isOwned = StoreService.Instance != null && StoreService.Instance.IsItemPurchased(_item.id);
            int currentCoins = CoinWallet.Instance != null ? CoinWallet.Instance.Balance : 0;
            bool canAfford = currentCoins >= _item.priceInCoins;

            if (ownedBadge != null)
            {
                ownedBadge.SetActive(isOwned);
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(!isOwned);
                buyButton.interactable = !isOwned && canAfford;

                if (buyButtonText != null)
                {
                    buyButtonText.text = isOwned ? "Owned" : (canAfford ? "Buy" : "Need Coins");
                }
            }
        }

        private async void LoadAssetImage(string url)
        {
            if (string.IsNullOrEmpty(url) || iconImage == null) return;

            var sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);
            if (sprite != null && iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
            }
        }

        private void HandleBuyClick()
        {
            _onBuyClicked?.Invoke(_item);
        }
    }
}
