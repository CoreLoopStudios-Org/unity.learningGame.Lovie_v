using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    /// <summary>
    /// "Continue Reading" card: shows the story the child opened last (or the first
    /// story of the list by default) with its cover, category, title and reading
    /// progress (0% untouched, 100% complete).
    /// </summary>
    public class ContinueReadingCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image coverImage;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI percentText;
        [Tooltip("Fill rect (Rectangle 4) — its width scales with reading progress.")]
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Button playButton;

        public Story Story { get; private set; }

        // The controller decides what "play" does (spawns the book reading panel).
        public event Action PlayClicked;

        private float fillWidth;

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

        public void Setup(Story story, float progress01)
        {
            Story = story;
            progress01 = Mathf.Clamp01(progress01);

            if (titleText != null)
            {
                titleText.text = story != null ? story.title : string.Empty;
            }

            if (typeText != null)
            {
                typeText.text = ExtractCategory(story?.contentPayload);
            }

            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
            }

            if (coverImage != null)
            {
                coverImage.enabled = false;
            }

            _ = LoadCoverAsync(story?.coverImageUrl);
            _ = UpdateFillDeferredAsync(progress01);
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

        private async Awaitable UpdateFillDeferredAsync(float progress01)
        {
            // Wait a frame so the card has a laid-out rect to measure.
            await Awaitable.NextFrameAsync();
            if (this == null || fillRect == null) return;

            if (fillWidth <= 0f)
            {
                fillWidth = fillRect.rect.width;
                if (fillWidth <= 0f && fillRect.parent is RectTransform parent)
                {
                    fillWidth = parent.rect.width;
                }

                // Grow from the left edge, not the center — move the pivot to the left
                // edge and shift the position so the bar stays visually in place.
                if (fillWidth > 0f && !Mathf.Approximately(fillRect.pivot.x, 0f))
                {
                    fillRect.anchoredPosition += new Vector2(-fillRect.pivot.x * fillWidth, 0f);
                    fillRect.pivot = new Vector2(0f, fillRect.pivot.y);
                }
            }

            if (fillWidth <= 0f) return;

            fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth * progress01);
        }

        private void HandlePlayClicked()
        {
            PlayClicked?.Invoke();
        }
    }
}
