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
    public class AdminKidUsersController : MonoBehaviour
    {
        private const int PageSize = 10;

        [Header("UI Elements")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private ScrollRect scrollRect;

        [Header("List")]
        [SerializeField] private Transform usersContainer;
        [SerializeField] private AdminKidUserCard userCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private readonly List<AdminKidUserCard> spawnedCards = new();
        
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
                Debug.LogWarning("[AdminKidUsersController] No valid session.");
                return;
            }

            int id = ++requestId;
            isLoading = true;

            if (clearFirst) ClearStatus();

            try
            {
                PaginatedChildren result = await adminApi.GetChildrenAsync(page, PageSize, currentSearch, currentSort, currentSortDescending);
                
                if (id != requestId || !isActiveAndEnabled) return;

                if (result != null)
                {
                    totalPages = result.totalPages;
                    PopulateUsers(result.children);
                }
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load kids: {ex.Message}");
            }
            finally
            {
                if (id == requestId) isLoading = false;
            }
        }

        private void PopulateUsers(AdminChild[] users)
        {
            if (users == null || users.Length == 0)
            {
                if (spawnedCards.Count == 0) ShowStatus("No kids found.");
                return;
            }

            ClearStatus();

            foreach (AdminChild child in users)
            {
                if (child != null)
                {
                    AdminKidUserCard card = Instantiate(userCardPrefab, usersContainer);
                    card.Setup(child, OnBanChild, OnDeleteChild);
                    spawnedCards.Add(card);
                }
            }
        }

        private async void OnBanChild(AdminChild child)
        {
            if (child == null) return;
            try
            {
                await adminApi.DisableChildAsync(child.id, true);
                Debug.Log($"Banned child: {child.id}");
                ResetAndLoad();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to ban child {child.id}: {ex.Message}");
            }
        }

        private async void OnDeleteChild(AdminChild child)
        {
            if (child == null) return;
            try
            {
                await adminApi.DeleteChildAsync(child.id);
                Debug.Log($"Deleted child: {child.id}");
                ResetAndLoad();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete child {child.id}: {ex.Message}");
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
            foreach (AdminKidUserCard card in spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            spawnedCards.Clear();
        }
    }
}
