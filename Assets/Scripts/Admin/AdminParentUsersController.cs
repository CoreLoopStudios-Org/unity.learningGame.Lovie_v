using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Attach to the "Admin Dashboard (All Users — Parents)" section.
    /// Fetches the user list every time the section is enabled and spawns one
    /// card per parent user into the container.
    /// </summary>
    public class AdminParentUsersController : MonoBehaviour
    {
        private const int PageSize = 50;

        [Header("List")]
        [SerializeField] private Transform usersContainer;
        [SerializeField] private AdminParentUserCard userCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private readonly List<AdminParentUserCard> spawnedCards = new();

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
                Debug.LogWarning("[AdminParentUsersController] No valid session, skipping user list load.");
                return;
            }

            int id = ++requestId;

            try
            {
                List<UserSummary> parents = await FetchUsersByTypeAsync(UserType.Parent);

                // A newer request started or section was disabled while awaiting.
                if (id != requestId || !isActiveAndEnabled) return;

                PopulateUsers(parents);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load parents: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load parents: {ex.Message}");
            }
        }

        private async Awaitable<List<UserSummary>> FetchUsersByTypeAsync(UserType type)
        {
            var users = new List<UserSummary>();
            int page = 1;

            while (true)
            {
                PaginatedUsers result = await adminApi.GetUsersAsync(page, PageSize);
                if (result?.users == null) break;

                foreach (UserSummary user in result.users)
                {
                    if (user != null && user.userType == (int)type)
                    {
                        users.Add(user);
                    }
                }

                if (page >= result.totalPages) break;
                page++;
            }

            return users;
        }

        private void PopulateUsers(List<UserSummary> users)
        {
            ClearSpawnedCards();

            if (users.Count == 0)
            {
                ShowStatus("No parents found.");
                return;
            }

            if (usersContainer == null || userCardPrefab == null)
            {
                Debug.LogWarning("[AdminParentUsersController] Users container or card prefab not assigned.");
                return;
            }

            ClearStatus();

            foreach (UserSummary user in users)
            {
                AdminParentUserCard card = Instantiate(userCardPrefab, usersContainer);
                card.Setup(user);
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
            foreach (AdminParentUserCard card in spawnedCards)
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
