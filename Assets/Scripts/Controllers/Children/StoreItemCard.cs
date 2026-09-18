using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    // Store items are IAP coin packs bought with real money via Unity IAP
    // (not yet integrated), so the card is display-only for now.
    public class StoreItemCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Image iconImage;

        private StoreItem _item;

        public StoreItem Item => _item;

        public void Setup(StoreItem item)
        {
            _item = item;

            if (nameText != null)
            {
                nameText.text = item.name;
            }

            if (priceText != null)
            {
                priceText.text = $"{item.rewardCoinAmount} Coins";
            }

            LoadAssetImage(item.assetUrl);
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
    }
}
