using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Shows a story inside the Book reading Panel a couple of lines at a time.
    /// Next/back turn pages with the book page-swap animation; the text fades in
    /// once the animation finishes and vanishes instantly on the next turn.
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
            pages = BuildPages(ExtractContent(story?.contentPayload));
            currentPage = 0;
            isTurningPage = false;

            if (storyText != null)
            {
                storyText.text = pages.Length > 0 ? pages[0] : string.Empty;
                storyText.alpha = 0f;
            }

            UpdateControls();
            _ = RevealFirstPageAsync();
        }

        [Serializable]
        private class StoryContentPayload
        {
            public string content;
        }

        // Story text rides inside the contentPayload JSON string ("content": "...").
        private static string ExtractContent(string contentPayload)
        {
            if (string.IsNullOrEmpty(contentPayload)) return string.Empty;

            try
            {
                return JsonUtility.FromJson<StoryContentPayload>(contentPayload)?.content ?? string.Empty;
            }
            catch
            {
                return contentPayload;
            }
        }

        private string[] BuildPages(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();

            List<string> pageList = new List<string>();
            StringBuilder page = new StringBuilder();

            // Sentences keep kids' reading natural; a page is a few sentences up to charsPerPage.
            foreach (string sentence in Regex.Split(content.Trim(), @"(?<=[.!?])\s+"))
            {
                if (string.IsNullOrWhiteSpace(sentence)) continue;

                if (page.Length > 0 && page.Length + sentence.Length + 1 > charsPerPage)
                {
                    pageList.Add(page.ToString().Trim());
                    page.Clear();
                }

                if (page.Length > 0)
                {
                    page.Append(' ');
                }

                page.Append(sentence.Trim());

                // Single sentence longer than a page is split by words so it still fits.
                while (page.Length > charsPerPage)
                {
                    int cut = page.ToString().LastIndexOf(' ', Math.Min(charsPerPage, page.Length - 1));
                    if (cut <= 0) break;

                    pageList.Add(page.ToString(0, cut).Trim());
                    page.Remove(0, cut + 1);
                }
            }

            if (page.Length > 0)
            {
                pageList.Add(page.ToString().Trim());
            }

            return pageList.ToArray();
        }

        private async Awaitable RevealFirstPageAsync()
        {
            // The book plays its page-swap animation when the panel spawns; fade in after it.
            await WaitForPageSwapAsync();
            if (this == null) return;

            await FadeInTextAsync();
        }

        private void HandleNextClicked()
        {
            if (isTurningPage || currentPage >= pages.Length - 1) return;
            _ = TurnPageAsync(currentPage + 1);
        }

        private void HandleBackClicked()
        {
            if (isTurningPage || currentPage <= 0) return;
            _ = TurnPageAsync(currentPage - 1);
        }

        private async Awaitable TurnPageAsync(int newPage)
        {
            isTurningPage = true;
            SetButtonsInteractive(false);

            // Vanish the current text instantly, then swap the page.
            if (storyText != null)
            {
                storyText.alpha = 0f;
            }

            if (bookAnimator != null)
            {
                bookAnimator.Play(PageSwapStateName, 0, 0f);
            }

            await WaitForPageSwapAsync();
            if (this == null) return;

            currentPage = newPage;
            if (storyText != null)
            {
                storyText.text = pages[currentPage];
            }

            UpdatePageCountText();
            await FadeInTextAsync();
            if (this == null) return;

            isTurningPage = false;
            UpdateControls();
        }

        private async Awaitable WaitForPageSwapAsync()
        {
            if (bookAnimator == null || !bookAnimator.gameObject.activeInHierarchy)
            {
                await Awaitable.WaitForSecondsAsync(0.2f);
                return;
            }

            int stateHash = Animator.StringToHash(PageSwapStateName);

            // Wait for the animation to (re)start — the state info is one frame stale after Play.
            // Timeout guards against a disabled animator so the text always shows.
            for (float elapsed = 0f; elapsed < 3f; elapsed += Time.deltaTime)
            {
                if (this == null) return;

                AnimatorStateInfo state = bookAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash == stateHash && state.normalizedTime < 0.9f) break;

                await Awaitable.NextFrameAsync();
            }

            for (float elapsed = 0f; elapsed < 3f; elapsed += Time.deltaTime)
            {
                if (this == null) return;

                AnimatorStateInfo state = bookAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash != stateHash || state.normalizedTime >= 1f) return;

                await Awaitable.NextFrameAsync();
            }
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
