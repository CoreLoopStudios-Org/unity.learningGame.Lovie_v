using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class AdminStoreStoryCard : MonoBehaviour
    {
        // FREE price label color from the design (#9B5DE5).
        private static readonly Color FreePriceColor = new Color32(0x9B, 0x5D, 0xE5, 0xFF);

        [Header("UI Elements")]
        [SerializeField] private Image bannerImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private GameObject coinIcon;
        [SerializeField] private Button editButton;

        public Story Story { get; private set; }

        // The panel subscribes and performs the API call — the card stays a dumb view.
        public event Action<AdminStoreStoryCard> EditClicked;

        private void Awake()
        {
            if (editButton != null) editButton.onClick.AddListener(OnEditClicked);
        }

        private void OnDestroy()
        {
            if (editButton != null) editButton.onClick.RemoveListener(OnEditClicked);
        }

        public void Setup(Story story)
        {
            Story = story;

            if (titleText != null)
                titleText.text = story?.title ?? string.Empty;

            if (typeText != null)
                typeText.text = ExtractCategory(story?.contentPayload);

            SetPrice(story?.priceInCoins ?? 0);

            _ = LoadBannerAsync();
        }

        // Called by the panel after a successful price update so the card
        // reflects the new price without re-fetching the whole list.
        public void SetPrice(int priceInCoins)
        {
            if (Story != null)
                Story.priceInCoins = priceInCoins;

            if (amountText == null) return;

            if (priceInCoins <= 0)
            {
                amountText.text = "FREE";
                amountText.color = FreePriceColor;
                if (coinIcon != null) coinIcon.SetActive(false);
            }
            else
            {
                amountText.text = priceInCoins.ToString();
                amountText.color = Color.black;
                if (coinIcon != null) coinIcon.SetActive(true);
            }
        }

        private async Awaitable LoadBannerAsync()
        {
            string url = Story?.coverImageUrl;
            if (string.IsNullOrEmpty(url)) return;

            Sprite sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);

            // Card may have been destroyed (list refreshed) while downloading,
            // or Setup may have been called again for a different story.
            if (this == null || Story == null || Story.coverImageUrl != url) return;

            if (bannerImage != null)
                bannerImage.sprite = sprite;
        }

        // Stories carry no category column — the upload panel embeds it in
        // contentPayload as {"category":"...","content":"..."}.
        private static string ExtractCategory(string contentPayload)
        {
            if (string.IsNullOrEmpty(contentPayload)) return "Story";

            try
            {
                var payload = JsonUtility.FromJson<StoryCategoryPayload>(contentPayload);
                if (payload != null && !string.IsNullOrWhiteSpace(payload.category))
                    return payload.category;
            }
            catch (Exception)
            {
                // Payload may be a mini-game shape with no category.
            }

            return "Story";
        }

        private void OnEditClicked()
        {
            EditClicked?.Invoke(this);
        }

        [Serializable]
        private class StoryCategoryPayload
        {
            public string category;
        }
    }
}
