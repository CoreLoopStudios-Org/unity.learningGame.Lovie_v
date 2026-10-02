using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class StoriesPageController : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform storiesContainer;
        [SerializeField] private ChildStoryCard storyCardPrefab;

        [Header("Search")]
        [SerializeField] private TMP_InputField searchField;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        [Header("Story Reading")]
        [SerializeField] private GameObject bookReadingPanelPrefab;
        [SerializeField] private Transform readingPanelParent;

        private readonly List<ChildStoryCard> spawnedCards = new();
        private int requestId;

        private void OnEnable()
        {
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener(HandleSearchChanged);
            }

            _ = RefreshAsync();
        }

        private void OnDisable()
        {
            if (searchField != null)
            {
                searchField.onValueChanged.RemoveListener(HandleSearchChanged);
            }
        }

        private async Awaitable RefreshAsync()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[StoriesPageController] No child session — cannot load stories.");
                return;
            }

            int id = ++requestId;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                Story[] stories = await childApi.GetStoriesAsync();

                // A newer request started or the panel was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                PopulateStories(stories);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load stories: {ex.Message}");
            }
        }

        private void PopulateStories(Story[] stories)
        {
            ClearSpawnedCards();

            if (stories == null || stories.Length == 0)
            {
                ShowStatus("No stories found.");
                return;
            }

            if (storiesContainer == null || storyCardPrefab == null)
            {
                Debug.LogWarning("[StoriesPageController] Stories container or card prefab not assigned.");
                return;
            }

            // Only free stories and stories already purchased with coins.
            foreach (Story story in stories)
            {
                if (story == null) continue;
                if (story.priceInCoins != 0 && !story.isUnlocked) continue;

                ChildStoryCard card = Instantiate(storyCardPrefab, storiesContainer);
                card.Setup(story);
                card.PlayClicked += OnCardPlayClicked;
                spawnedCards.Add(card);
            }

            if (spawnedCards.Count == 0)
            {
                ShowStatus($"No free or purchased stories yet ({stories.Length} locked).");
                return;
            }

            ClearStatus();
            ApplySearchFilter(searchField != null ? searchField.text : string.Empty);
        }

        private void OnCardPlayClicked(ChildStoryCard card)
        {
            Story story = card?.Story;
            if (story == null) return;

            if (bookReadingPanelPrefab == null)
            {
                Debug.LogWarning("[StoriesPageController] Book reading panel prefab not assigned.");
                return;
            }

            Transform parent = readingPanelParent != null
                ? readingPanelParent
                : storiesContainer != null ? storiesContainer.root : transform;

            GameObject panel = Instantiate(bookReadingPanelPrefab, parent);
            panel.GetComponent<BookReadingPanelController>()?.Setup(story);
        }

        private void HandleSearchChanged(string text)
        {
            ApplySearchFilter(text);
        }

        private void ApplySearchFilter(string search)
        {
            search = search ?? string.Empty;
            search = search.Trim();

            foreach (ChildStoryCard card in spawnedCards)
            {
                if (card == null) continue;

                string title = card.Story?.title ?? string.Empty;
                bool visible = search.Length == 0
                    || title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                card.gameObject.SetActive(visible);
            }
        }

        private void ShowStatus(string message)
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = message;
                statusFeedbackText.gameObject.SetActive(true);
            }
        }

        private void ClearStatus()
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = string.Empty;
                statusFeedbackText.gameObject.SetActive(false);
            }
        }

        private void ClearSpawnedCards()
        {
            foreach (ChildStoryCard card in spawnedCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            spawnedCards.Clear();
        }
    }
}
