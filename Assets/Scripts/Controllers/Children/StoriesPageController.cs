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

        [Header("Continue Reading")]
        [SerializeField] private ContinueReadingCard continueReadingCard;

        private readonly List<ChildStoryCard> spawnedCards = new();
        private int requestId;

        private void Awake()
        {
            if (continueReadingCard != null)
            {
                continueReadingCard.PlayClicked += HandleContinuePlayClicked;
            }
        }

        private void OnDestroy()
        {
            if (continueReadingCard != null)
            {
                continueReadingCard.PlayClicked -= HandleContinuePlayClicked;
            }
        }

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
            var visibleStories = ContinueReading.FilterVisible(stories);
            foreach (Story story in visibleStories)
            {
                ChildStoryCard card = Instantiate(storyCardPrefab, storiesContainer);
                card.Setup(story);
                card.PlayClicked += OnCardPlayClicked;
                spawnedCards.Add(card);
            }

            UpdateContinueReadingCard(visibleStories);

            if (spawnedCards.Count == 0)
            {
                ShowStatus($"No free or purchased stories yet ({stories.Length} locked).");
                return;
            }

            ClearStatus();
            ApplySearchFilter(searchField != null ? searchField.text : string.Empty);
        }

        // The story the child opened last; falls back to the first story of the list at 0%.
        private void UpdateContinueReadingCard(List<Story> visibleStories)
        {
            if (continueReadingCard == null) return;

            string childId = SessionManager.Instance != null ? SessionManager.Instance.ChildId : null;
            var (target, progress) = ContinueReading.Select(visibleStories, childId);

            if (target != null)
            {
                continueReadingCard.Setup(target, progress);
            }
        }

        private void HandleContinuePlayClicked()
        {
            SpawnReadingPanel(continueReadingCard != null ? continueReadingCard.Story : null);
        }

        private void OnCardPlayClicked(ChildStoryCard card)
        {
            SpawnReadingPanel(card?.Story);
        }

        private void SpawnReadingPanel(Story story)
        {
            if (story == null) return;

            Transform parent = readingPanelParent != null
                ? readingPanelParent
                : storiesContainer != null ? storiesContainer.root : transform;

            ContinueReading.SpawnReadingPanel(bookReadingPanelPrefab, story, parent);
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
