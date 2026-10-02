using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class StoreStoryCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image storyCardImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI coinAmountText;

        public Story Story { get; private set; }

        public void Setup(Story story)
        {
            Story = story;

            if (nameText != null)
            {
                nameText.text = story?.title ?? string.Empty;
            }

            if (typeText != null)
            {
                typeText.text = ExtractCategory(story?.contentPayload);
            }

            if (coinAmountText != null)
            {
                coinAmountText.text = (story?.priceInCoins ?? 0).ToString();
            }

            if (storyCardImage != null)
            {
                storyCardImage.enabled = false;
            }

            _ = LoadCardImageAsync();
        }

        [Serializable]
        private class StoryContentPayload
        {
            public string category;
        }

        // Category rides inside the contentPayload JSON string ("category": "Fantasy").
        private static string ExtractCategory(string contentPayload)
        {
            if (string.IsNullOrEmpty(contentPayload)) return "Story";

            try
            {
                var payload = JsonUtility.FromJson<StoryContentPayload>(contentPayload);
                return string.IsNullOrWhiteSpace(payload?.category) ? "Story" : payload.category;
            }
            catch
            {
                return "Story";
            }
        }

        private async Awaitable LoadCardImageAsync()
        {
            string url = Story?.coverImageUrl;
            if (string.IsNullOrEmpty(url) || storyCardImage == null) return;

            Sprite sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);

            // Card may have been destroyed (list refreshed) while downloading,
            // or Setup may have been called again for a different story.
            if (this == null || Story == null || Story.coverImageUrl != url) return;

            if (sprite != null && storyCardImage != null)
            {
                storyCardImage.sprite = sprite;
                storyCardImage.enabled = true;
            }
        }
    }
}
