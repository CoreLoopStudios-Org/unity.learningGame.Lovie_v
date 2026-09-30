using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class ChildStoryCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image coverImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private Button playButton;

        public Story Story { get; private set; }

        // The controller decides what "play" does (story reading panel later).
        public event Action<ChildStoryCard> PlayClicked;

        private void Awake()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(HandlePlayClicked);
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandlePlayClicked);
            }
        }

        public void Setup(Story story)
        {
            Story = story;

            if (nameText != null)
            {
                nameText.text = story.title;
            }

            if (typeText != null)
            {
                typeText.text = ExtractCategory(story.contentPayload);
            }

            if (coverImage != null)
            {
                coverImage.enabled = false;
            }

            _ = LoadCoverAsync(story.coverImageUrl);
        }

        [Serializable]
        private class StoryContentPayload
        {
            public string category;
        }

        // Category rides inside the contentPayload JSON string ("category": "Fantasy").
        private static string ExtractCategory(string contentPayload)
        {
            if (string.IsNullOrEmpty(contentPayload)) return "-";

            try
            {
                var payload = JsonUtility.FromJson<StoryContentPayload>(contentPayload);
                return string.IsNullOrEmpty(payload?.category) ? "-" : payload.category;
            }
            catch
            {
                return "-";
            }
        }

        private async Awaitable LoadCoverAsync(string url)
        {
            if (string.IsNullOrEmpty(url) || coverImage == null) return;

            var sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);
            if (sprite != null && coverImage != null)
            {
                coverImage.sprite = sprite;
                coverImage.enabled = true;
            }
        }

        private void HandlePlayClicked()
        {
            PlayClicked?.Invoke(this);
        }
    }
}
