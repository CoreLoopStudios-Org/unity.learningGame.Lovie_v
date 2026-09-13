using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminManageQuizPanelController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField searchInput;

        [Header("List")]
        [SerializeField] private Transform storiesContainer;
        [SerializeField] private AdminQuizStoryCard storyCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private string currentSearch = "";
        private readonly List<AdminQuizStoryCard> spawnedCards = new();

        // Filled after AdminQuizContext is set — subscribe to open the
        // per-story quiz management panel once that panel exists.
        public event Action<Story> ViewQuizzesClicked;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

            if (searchInput != null)
                searchInput.onEndEdit.AddListener(OnSearchChanged);
        }

        private void OnDestroy()
        {
            if (searchInput != null)
                searchInput.onEndEdit.RemoveListener(OnSearchChanged);
        }

        private void OnEnable()
        {
            ResetAndLoad();
        }

        private void OnSearchChanged(string newValue)
        {
            if (currentSearch == newValue) return;
            currentSearch = newValue;
            ResetAndLoad();
        }

        private void ResetAndLoad()
        {
            ClearSpawnedCards();
            _ = LoadStoriesAsync();
        }

        private async Awaitable LoadStoriesAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminManageQuizPanelController] No valid session.");
                return;
            }

            int id = ++requestId;

            try
            {
                Story[] stories = await adminApi.GetStoriesAsync(titleSearch: currentSearch);

                if (id != requestId || !isActiveAndEnabled) return;

                if (stories == null || stories.Length == 0)
                {
                    ShowStatus("No stories found.");
                    return;
                }

                Quiz[] quizzes = await adminApi.GetQuizzesAsync();

                if (id != requestId || !isActiveAndEnabled) return;

                PopulateStories(stories, CountQuizzesPerStory(quizzes));
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load stories: {ex.Message}");
            }
        }

        private static Dictionary<string, int> CountQuizzesPerStory(Quiz[] quizzes)
        {
            Dictionary<string, int> counts = new();
            if (quizzes == null) return counts;

            foreach (Quiz quiz in quizzes)
            {
                if (quiz?.storyId == null) continue;
                counts.TryGetValue(quiz.storyId, out int count);
                counts[quiz.storyId] = count + 1;
            }
            return counts;
        }

        private void PopulateStories(Story[] stories, Dictionary<string, int> quizCounts)
        {
            ClearSpawnedCards();

            if (storiesContainer == null || storyCardPrefab == null)
            {
                Debug.LogWarning("[AdminManageQuizPanelController] Stories container or card prefab not assigned.");
                return;
            }

            ClearStatus();

            foreach (Story story in stories)
            {
                if (story == null) continue;

                AdminQuizStoryCard card = Instantiate(storyCardPrefab, storiesContainer);
                quizCounts.TryGetValue(story.id, out int count);
                card.Setup(story, count);
                card.ViewQuizzesClicked += OnViewQuizzesClicked;
                spawnedCards.Add(card);
            }
        }

        private void OnViewQuizzesClicked(AdminQuizStoryCard card)
        {
            if (card?.Story == null) return;

            AdminQuizContext.SelectedStoryId = card.Story.id;
            AdminQuizContext.SelectedStoryTitle = card.Story.title;

            ViewQuizzesClicked?.Invoke(card.Story);
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
            foreach (AdminQuizStoryCard card in spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            spawnedCards.Clear();
        }
    }
}
