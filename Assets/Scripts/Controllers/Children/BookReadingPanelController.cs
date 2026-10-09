using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Shows a story inside the Book reading Panel a couple of lines at a time.
    /// Next/back turn pages with the book page-swap animation; the text fades in
    /// once the animation finishes and vanishes instantly on the next turn.
    /// Text paging: StoryPaginator — page-turn playback: BookPageTurnPlayer —
    /// progress/completion: StoryProgressService.
    /// </summary>
    public class BookReadingPanelController : MonoBehaviour
    {
        private const string PageSwapStateName = "Page swapping anim";

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI storyText;
        [SerializeField] private TextMeshProUGUI pageCountText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;

        [Header("Book Animation")]
        [SerializeField] private Animator bookAnimator;

        [Header("Paging")]
        [SerializeField, Min(40)] private int charsPerPage = 220;
        [SerializeField, Min(0.1f)] private float fadeInDuration = 0.5f;

        private string[] pages = Array.Empty<string>();
        private int currentPage;
        private bool isTurningPage;
        private bool completionRecorded;
        private float readingStartTime;

        public Story Story { get; private set; }

        private void Awake()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(HandleNextClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackClicked);
            }

            // Book-page swipes: right-to-left flips forward, left-to-right flips back.
            SwipeDetector swipe = gameObject.AddComponent<SwipeDetector>();
            swipe.SwipeLeft += HandleNextClicked;
            swipe.SwipeRight += HandleBackClicked;
        }

        private void OnDestroy()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        public void Setup(Story story)
        {
            Story = story;
            pages = StoryPaginator.BuildPages(StoryPaginator.ExtractContent(story?.contentPayload), charsPerPage);
            currentPage = 0;
            isTurningPage = false;
            readingStartTime = Time.unscaledTime;
            completionRecorded = StoryProgressService.IsStoryCompleted(story);

            // Continue Reading: unfinished stories resume where the child left off.
            if (!completionRecorded)
            {
                int resumePage = StoryProgressService.GetResumePage(story, pages.Length);
                if (resumePage >= 0)
                {
                    currentPage = resumePage;
                }
            }

            if (storyText != null)
            {
                storyText.text = pages.Length > 0 ? pages[currentPage] : string.Empty;
                storyText.alpha = 0f;
            }

            UpdateControls();

            // A one-page story is fully revealed on open — no page turn will ever fire.
            if (pages.Length == 1)
            {
                MarkStoryCompleted();
            }

            _ = RevealFirstPageAsync();
        }

        private async Awaitable RevealFirstPageAsync()
        {
            // The book plays its page-swap animation when the panel spawns; fade in after it.
            await BookPageTurnPlayer.WaitAsync(bookAnimator, PageSwapStateName);
            if (this == null) return;

            await FadeInTextAsync();
        }

        private void HandleNextClicked()
        {
            if (isTurningPage || currentPage >= pages.Length - 1) return;
            _ = TurnPageAsync(currentPage + 1, goingBack: false);
        }

        private void HandleBackClicked()
        {
            if (isTurningPage || currentPage <= 0) return;
            _ = TurnPageAsync(currentPage - 1, goingBack: true);
        }

        private async Awaitable TurnPageAsync(int newPage, bool goingBack)
        {
            isTurningPage = true;
            SetButtonsInteractive(false);

            // Vanish the current text instantly, then swap the page.
            if (storyText != null)
            {
                storyText.alpha = 0f;
            }

            await BookPageTurnPlayer.PlayAsync(bookAnimator, PageSwapStateName, goingBack);
            if (this == null) return;

            currentPage = newPage;
            if (storyText != null)
            {
                storyText.text = pages[currentPage];
            }

            UpdatePageCountText();
            StoryProgressService.SaveReadingProgress(Story, currentPage + 1, pages.Length);

            // Reaching the last page counts as finishing the story.
            if (currentPage >= pages.Length - 1)
            {
                MarkStoryCompleted();
            }

            await FadeInTextAsync();
            if (this == null) return;

            isTurningPage = false;
            UpdateControls();
        }

        private void MarkStoryCompleted()
        {
            if (completionRecorded) return;
            completionRecorded = true;

            StoryProgressService.MarkStoryCompleted(Story, pages.Length, readingStartTime);
        }

        private async Awaitable FadeInTextAsync()
        {
            if (storyText == null) return;

            float elapsed = 0f;
            storyText.alpha = 0f;

            while (elapsed < fadeInDuration)
            {
                await Awaitable.NextFrameAsync();
                if (this == null || storyText == null) return;

                elapsed += Time.deltaTime;
                storyText.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            }

            storyText.alpha = 1f;
        }

        private void SetButtonsInteractive(bool interactive)
        {
            if (nextButton != null)
            {
                nextButton.interactable = interactive;
            }

            if (backButton != null)
            {
                backButton.interactable = interactive;
            }
        }

        private void UpdateControls()
        {
            if (nextButton != null)
            {
                nextButton.interactable = pages.Length > 0 && currentPage < pages.Length - 1;
            }

            if (backButton != null)
            {
                backButton.interactable = currentPage > 0;
            }

            UpdatePageCountText();
        }

        private void UpdatePageCountText()
        {
            if (pageCountText == null) return;

            pageCountText.text = pages.Length == 0 ? string.Empty : $"{currentPage + 1} / {pages.Length}";
        }
    }
}
