using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Api.Models;

namespace UI
{
    // Paginated leaderboard list: spawns LeaderboardUserCard entries under the scroll
    // Content and loads the next page when the user scrolls near the bottom. The page
    // controller seeds page 1 (shared with the podium) and supplies the fetcher.
    public class LeaderboardRankListView : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private RectTransform content;
        [SerializeField] private LeaderboardUserCard cardPrefab;

        [Header("Pagination")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField, Range(0.01f, 0.3f)] private float loadMoreThreshold = 0.05f;

        // Set by LeaderboardPageController — fetches a page of the active board.
        public Func<int, Awaitable<LeaderboardPage>> PageFetcher { get; set; }

        private readonly List<LeaderboardUserCard> spawnedCards = new();
        private int nextPage = 1;
        private bool hasMore;
        private bool isLoading;

        private bool CanLoadMore => !isLoading && PageFetcher != null && hasMore;

        private void Awake()
        {
            if (scrollRect == null)
                scrollRect = GetComponentInParent<ScrollRect>();

            if (scrollRect != null)
                scrollRect.onValueChanged.AddListener(HandleScrollChanged);
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
        }

        // Clears the whole Content — the list must own it exclusively (a card placed
        // in the scene as a template is destroyed at runtime by this).
        public void ResetList()
        {
            if (content == null)
            {
                Debug.LogWarning("[LeaderboardRankListView] Content not assigned.");
                return;
            }

            foreach (Transform child in content)
                Destroy(child.gameObject);

            spawnedCards.Clear();
            nextPage = 1;
            hasMore = false;
            isLoading = false;
        }

        // Seeds the view with an already-fetched page (page 1, shared with the podium).
        public async Awaitable AppendPageAsync(LeaderboardPage page)
        {
            if (page == null || content == null || cardPrefab == null)
            {
                Debug.LogWarning("[LeaderboardRankListView] Page, content or card prefab not assigned.");
                return;
            }

            nextPage = page.page + 1;
            hasMore = page.page < page.totalPages && page.entries != null && page.entries.Count > 0;

            Spawn(page.entries);
            await FillViewportIfShortAsync();
        }

        private void HandleScrollChanged(Vector2 _ignored)
        {
            if (!CanLoadMore || scrollRect == null)
                return;

            if (scrollRect.verticalNormalizedPosition <= loadMoreThreshold)
                _ = LoadNextPageAsync();
        }

        // A short first page may never scroll — keep loading until the viewport fills
        // or the board ends.
        private async Awaitable FillViewportIfShortAsync()
        {
            int guard = 0;
            while (guard++ < 10 && this != null && CanLoadMore && !ViewportFilled())
            {
                await LoadNextPageAsync();
                await Awaitable.NextFrameAsync(); // let the layout rebuild before re-measuring
            }
        }

        private async Awaitable LoadNextPageAsync()
        {
            if (!CanLoadMore)
                return;

            isLoading = true;
            int page = nextPage;

            try
            {
                LeaderboardPage result = await PageFetcher(page);
                if (this == null)
                    return;

                // An empty page past the end means no more data (API doc §3.1).
                nextPage = page + 1;
                hasMore = result != null
                    && page < result.totalPages
                    && result.entries != null
                    && result.entries.Count > 0;

                Spawn(result?.entries);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardRankListView] Failed to load page {page}: {ex.Message}");
            }
            finally
            {
                if (this != null)
                    isLoading = false;
            }
        }

        private void Spawn(List<LeaderboardEntry> entries)
        {
            if (entries == null)
                return;

            foreach (LeaderboardEntry entry in entries)
            {
                if (entry == null)
                    continue;

                LeaderboardUserCard card = Instantiate(cardPrefab, content);
                card.Setup(entry);
                spawnedCards.Add(card);
            }
        }

        private bool ViewportFilled()
        {
            if (content == null || scrollRect == null || scrollRect.viewport == null)
                return true;

            return content.rect.height >= scrollRect.viewport.rect.height;
        }
    }
}
