using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminStoreManagePanelController : MonoBehaviour
    {
        private const float SearchDebounceSeconds = 0.4f;

        [Header("List")]
        [SerializeField] private Transform storiesContainer;
        [SerializeField] private AdminStoreStoryCard storyCardPrefab;

        [Header("Search")]
        [SerializeField] private TMP_InputField searchInput;

        [Header("Price Edit Popup")]
        [SerializeField] private AdminEditStoryPricePopup editPricePopupPrefab;
        [SerializeField] private Transform popupParent;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private int searchRequestId;
        private bool isUpdatingPrice;
        private readonly List<AdminStoreStoryCard> spawnedCards = new();

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

            if (searchInput != null)
                searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        private void OnDestroy()
        {
            if (searchInput != null)
                searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private void OnSearchChanged(string newText)
        {
            int id = ++searchRequestId;
            _ = SearchDebouncedAsync(id);
        }

        private async Awaitable SearchDebouncedAsync(int id)
        {
            await Awaitable.WaitForSecondsAsync(SearchDebounceSeconds);

            // Another keystroke arrived or the panel was closed meanwhile.
            if (id != searchRequestId || !isActiveAndEnabled) return;

            await RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminStoreManagePanelController] No valid session, skipping story list load.");
                return;
            }

            int id = ++requestId;
            string titleSearch = GetSearchText();

            try
            {
                Story[] stories = await adminApi.GetStoriesAsync(titleSearch: titleSearch);

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

        private string GetSearchText()
        {
            return searchInput != null ? searchInput.text.Trim() : string.Empty;
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
                Debug.LogWarning("[AdminStoreManagePanelController] Stories container or card prefab not assigned.");
                return;
            }

            ClearStatus();

            foreach (Story story in stories)
            {
                if (story == null) continue;

                AdminStoreStoryCard card = Instantiate(storyCardPrefab, storiesContainer);
                card.Setup(story);
                card.EditClicked += OnCardEditClicked;
                spawnedCards.Add(card);
            }
        }

        private void OnCardEditClicked(AdminStoreStoryCard card)
        {
            if (card?.Story == null || editPricePopupPrefab == null)
                return;

            Transform parent = popupParent != null ? popupParent : transform;
            AdminEditStoryPricePopup popup = Instantiate(editPricePopupPrefab, parent);
            popup.Setup(card.Story);
            popup.UpdateConfirmed += (p, newPrice) => _ = UpdateStoryPriceAsync(p, card, newPrice);
            popup.Dismissed += p => Destroy(p.gameObject);
        }

        // Story price lives on the Story row; the store-items endpoints are for
        // generic storefront items and have no link to stories.
        private async Awaitable UpdateStoryPriceAsync(AdminEditStoryPricePopup popup, AdminStoreStoryCard card, int newPrice)
        {
            if (isUpdatingPrice) return;

            if (!SessionManager.Instance.IsValidToken())
            {
                popup.ShowError("No valid session.");
                return;
            }

            // The card can be destroyed while awaiting, so grab what we need up front.
            string storyId = card.Story.id;
            string storyTitle = card.Story.title;

            isUpdatingPrice = true;
            popup.SetInteractable(false);

            try
            {
                await adminApi.UpdateStoryPriceAsync(storyId, newPrice);

                if (popup != null) Destroy(popup.gameObject);
                ShowStatus(newPrice == 0
                    ? $"\"{storyTitle}\" is now FREE."
                    : $"Price updated to {newPrice} coins.");

                // Re-fetch so the UI reflects server truth instead of an assumed price.
                await RefreshAsync();
            }
            catch (ApiException ex)
            {
                popup.SetInteractable(true);
                popup.ShowError($"Failed to update price: {ex.Message}");
            }
            catch (Exception ex)
            {
                popup.SetInteractable(true);
                popup.ShowError($"Failed to update price: {ex.Message}");
            }
            finally
            {
                isUpdatingPrice = false;
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
            foreach (AdminStoreStoryCard card in spawnedCards)
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
