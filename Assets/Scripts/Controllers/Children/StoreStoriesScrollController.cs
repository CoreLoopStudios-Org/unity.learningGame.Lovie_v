using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class StoreStoriesScrollController : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform storiesContainer;
        [SerializeField] private StoreStoryCard storyCardPrefab;

        [Header("Purchase")]
        [SerializeField] private ConfirmationPopupPanel confirmationPopupPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private readonly List<StoreStoryCard> spawnedCards = new();
        private ConfirmationPopupPanel activePopup;
        private bool isPurchasing;
        private int requestId;

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[StoreStoriesScrollController] No child session — cannot load store stories.");
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
                    ShowStatus($"Failed to load store stories: {ex.Message}");
            }
        }

        // Only purchasable stories: priced above zero and not already owned.
        private void PopulateStories(Story[] stories)
        {
            ClearSpawnedCards();

            if (stories == null || stories.Length == 0)
            {
                ShowStatus("No stories available.");
                return;
            }

            if (storiesContainer == null || storyCardPrefab == null)
            {
                Debug.LogWarning("[StoreStoriesScrollController] Stories container or card prefab not assigned.");
                return;
            }

            foreach (Story story in stories)
            {
                if (story == null) continue;
                if (story.priceInCoins <= 0 || story.isUnlocked) continue;

                StoreStoryCard card = Instantiate(storyCardPrefab, storiesContainer);
                card.Setup(story);
                card.PurchaseClicked += OnPurchaseClicked;
                spawnedCards.Add(card);
            }

            if (spawnedCards.Count == 0)
            {
                ShowStatus("No new stories to purchase.");
                return;
            }

            ClearStatus();
        }

        private void OnPurchaseClicked(StoreStoryCard card)
        {
            if (card == null || card.Story == null || isPurchasing) return;

            if (confirmationPopupPrefab == null)
            {
                Debug.LogWarning("[StoreStoriesScrollController] Confirmation popup prefab not assigned — buying directly.", this);
                _ = PurchaseStoryAsync(card);
                return;
            }

            if (activePopup != null) Destroy(activePopup.gameObject);

            Canvas canvas = GetComponentInParent<Canvas>();
            activePopup = canvas != null
                ? Instantiate(confirmationPopupPrefab, canvas.rootCanvas.transform)
                : Instantiate(confirmationPopupPrefab);
            activePopup.Setup(
                () => _ = PurchaseStoryAsync(card),
                $"Buy \"{card.Story.title}\" for {card.Story.priceInCoins} coins?");
        }

        private async Awaitable PurchaseStoryAsync(StoreStoryCard card)
        {
            if (isPurchasing || card == null || card.Story == null) return;

            isPurchasing = true;
            card.SetPurchasing(true);

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                bool success = await childApi.PurchaseStoryAsync(card.Story.id);

                // Card may have been destroyed (list refreshed) while awaiting.
                if (this == null) return;

                if (success)
                {
                    spawnedCards.Remove(card);
                    Destroy(card.gameObject);

                    if (CoinWallet.Instance != null)
                        _ = CoinWallet.Instance.RefreshAsync();

                    ShowStatus($"\"{card.Story.title}\" unlocked! Find it on the Stories page.");
                }
                else
                {
                    ShowStatus("Purchase failed. Please try again.");
                }
            }
            catch (ApiException ex) when (ex.responseCode == 400)
            {
                ShowStatus(string.IsNullOrWhiteSpace(ex.errorMessage)
                    ? "Not enough coins or already owned."
                    : ex.errorMessage);
            }
            catch (Exception ex)
            {
                if (this != null)
                    ShowStatus($"Purchase failed: {ex.Message}");
            }
            finally
            {
                if (this != null)
                    isPurchasing = false;

                if (card != null)
                    card.SetPurchasing(false);
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
            foreach (StoreStoryCard card in spawnedCards)
            {
                if (card != null)
                {
                    card.PurchaseClicked -= OnPurchaseClicked;
                    Destroy(card.gameObject);
                }
            }
            spawnedCards.Clear();
        }
    }
}
