using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminStoriesPanelController : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform storiesContainer;
        [SerializeField] private AdminStoryCard storyCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private readonly List<AdminStoryCard> spawnedCards = new();

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminStoriesPanelController] No valid session, skipping story list load.");
                return;
            }

            int id = ++requestId;

            try
            {
                Story[] stories = await adminApi.GetStoriesAsync();

                // A newer request started or panel was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                PopulateStories(stories);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load stories: {ex.Message}");
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
                Debug.LogWarning("[AdminStoriesPanelController] Stories container or card prefab not assigned.");
                return;
            }

            ClearStatus();

            foreach (Story story in stories)
            {
                if (story == null) continue;

                AdminStoryCard card = Instantiate(storyCardPrefab, storiesContainer);
                card.Setup(story);
                spawnedCards.Add(card);
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
            }
        }

        private void ClearSpawnedCards()
        {
            foreach (AdminStoryCard card in spawnedCards)
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
