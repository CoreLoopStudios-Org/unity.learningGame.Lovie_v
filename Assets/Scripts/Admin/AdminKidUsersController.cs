using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminKidUsersController : MonoBehaviour
    {
        private const int PageSize = 50;

        [Header("List")]
        [SerializeField] private Transform usersContainer;
        [SerializeField] private AdminKidUserCard userCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private readonly List<AdminKidUserCard> spawnedCards = new();

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
                Debug.LogWarning("[AdminKidUsersController] No valid session, skipping user list load.");
                return;
            }

            int id = ++requestId;

            try
            {
                List<AdminChild> kids = await FetchChildrenAsync();

                // A newer request started or section was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                PopulateUsers(kids);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load kids: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load kids: {ex.Message}");
            }
        }

        private async Awaitable<List<AdminChild>> FetchChildrenAsync()
        {
            var kids = new List<AdminChild>();
            int page = 1;

            while (true)
            {
                PaginatedChildren result = await adminApi.GetChildrenAsync(page, PageSize);
                if (result?.children == null) break;

                foreach (AdminChild child in result.children)
                {
                    if (child != null)
                    {
                        kids.Add(child);
                    }
                }

                if (page >= result.totalPages) break;
                page++;
            }

            return kids;
        }

        private void PopulateUsers(List<AdminChild> users)
        {
            ClearSpawnedCards();

            if (users.Count == 0)
            {
                ShowStatus("No kids found.");
                return;
            }

            if (usersContainer == null || userCardPrefab == null)
            {
                Debug.LogWarning("[AdminKidUsersController] Users container or card prefab not assigned.");
                return;
            }

            ClearStatus();

            foreach (AdminChild child in users)
            {
                AdminKidUserCard card = Instantiate(userCardPrefab, usersContainer);
                card.Setup(child);
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
            foreach (AdminKidUserCard card in spawnedCards)
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
