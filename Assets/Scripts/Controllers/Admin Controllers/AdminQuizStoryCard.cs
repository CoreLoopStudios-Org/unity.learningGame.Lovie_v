using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class AdminQuizStoryCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image storyImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI quizCountText;
        [SerializeField] private Button viewQuizzesButton;

        public Story Story { get; private set; }

        // The panel subscribes and handles navigation — the card stays a dumb view.
        public event Action<AdminQuizStoryCard> ViewQuizzesClicked;

        private void Awake()
        {
            if (viewQuizzesButton != null) viewQuizzesButton.onClick.AddListener(OnViewQuizzesClicked);
        }

        private void OnDestroy()
        {
            if (viewQuizzesButton != null) viewQuizzesButton.onClick.RemoveListener(OnViewQuizzesClicked);
        }

        public void Setup(Story story, int quizCount)
        {
            Story = story;

            if (titleText != null)
                titleText.text = story?.title ?? string.Empty;

            if (quizCountText != null)
                quizCountText.text = quizCount.ToString() + " Quizzes";

            _ = LoadCoverImageAsync();
        }

        private async Awaitable LoadCoverImageAsync()
        {
            string url = Story?.coverImageUrl;
            if (string.IsNullOrEmpty(url)) return;

            Sprite sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);

            // Card may have been destroyed (list refreshed) while downloading,
            // or Setup may have been called again for a different story.
            if (this == null || Story == null || Story.coverImageUrl != url) return;

            if (storyImage != null)
                storyImage.sprite = sprite;
        }

        private void OnViewQuizzesClicked()
        {
            ViewQuizzesClicked?.Invoke(this);
        }
    }
}
