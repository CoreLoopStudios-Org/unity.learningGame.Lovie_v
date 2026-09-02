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
    public class AdminParentUsersController : MonoBehaviour
    {
        private const int PageSize = 10;

        [Header("UI Elements")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private ScrollRect scrollRect;

        [Header("List")]
        [SerializeField] private Transform usersContainer;
        [SerializeField] private AdminParentUserCard userCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private readonly List<AdminParentUserCard> spawnedCards = new();
        
        private int currentPage = 1;
        private int totalPages = 1;
        private bool isLoading = false;
        private string currentSearch = "";
        private string currentSort = "Newest";
        private bool currentSortDescending = true;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

            if (searchInput != null)
                searchInput.onEndEdit.AddListener(OnSearchChanged);
            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
            if (scrollRect != null)
                scrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void OnDestroy()
        {
            if (searchInput != null)
                searchInput.onEndEdit.RemoveListener(OnSearchChanged);
            if (sortDropdown != null)
                sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScroll);
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

        private void OnSortChanged(int index)
        {
            if (sortDropdown != null)
            {
                string option = sortDropdown.options[index].text;
                if (option.Contains("Alphabet", StringComparison.OrdinalIgnoreCase))
                {
                    currentSort = "Alphabetical";
                    currentSortDescending = false;
                }
                else
                {
                    currentSort = "Newest";
                    currentSortDescending = true;
                }
                ResetAndLoad();
            }
        }

        private void OnScroll(Vector2 position)
        {
            if (isLoading || currentPage >= totalPages) return;

            if (position.y < 0.05f)
            {
                LoadNextPage();
            }
        }

        private void ResetAndLoad()
        {
            currentPage = 1;
            totalPages = 1;
            ClearSpawnedCards();
            _ = LoadPageAsync(currentPage, true);
        }

        private void LoadNextPage()
        {
            currentPage++;
            _ = LoadPageAsync(currentPage, false);
        }

        private async Awaitable LoadPageAsync(int page, bool clearFirst)
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminParentUsersController] No valid session.");
                return;
            }

            int id = ++requestId;
            isLoading = true;

            if (clearFirst) ClearStatus();

            try
            {
                PaginatedUsers result = await adminApi.GetUsersAsync(page, PageSize, currentSearch, currentSort, currentSortDescending);
                
                if (id != requestId || !isActiveAndEnabled) return;

                if (result != null)
                {
                    totalPages = result.totalPages;
                    PopulateUsers(result.users);
                }
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load parents: {ex.Message}");
            }
            finally
            {
                if (id == requestId) isLoading = false;
            }
        }

        private void PopulateUsers(UserSummary[] users)
        {
            if (users == null || users.Length == 0)
            {
                if (spawnedCards.Count == 0) ShowStatus("No parents found.");
                return;
            }

            ClearStatus();

            foreach (UserSummary user in users)
            {
                if (user != null && user.userType == (int)UserType.Parent)
                {
                    AdminParentUserCard card = Instantiate(userCardPrefab, usersContainer);
                    card.Setup(user, OnBanUser, OnDeleteUser);
                    spawnedCards.Add(card);
                }
            }
        }

        private async void OnBanUser(UserSummary user)
        {
            if (user == null) return;
            try
            {
                await adminApi.DisableUserAsync(user.id, true);
                Debug.Log($"Banned user: {user.id}");
                ResetAndLoad();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to ban user {user.id}: {ex.Message}");
            }
        }

        private async void OnDeleteUser(UserSummary user)
        {
            if (user == null) return;
            try
            {
                await adminApi.DeleteUserAsync(user.id);
                Debug.Log($"Deleted user: {user.id}");
                ResetAndLoad();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete user {user.id}: {ex.Message}");
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
            foreach (AdminParentUserCard card in spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            spawnedCards.Clear();
        }
    }
}
