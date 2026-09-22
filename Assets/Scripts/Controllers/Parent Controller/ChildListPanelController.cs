using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    // Attach to the spawned Child List Scroll View prefab. Fetches the parent's children on enable
    // and instantiates a ChildDetailsCard per child; selecting one updates the dashboard.
    public class ChildListPanelController : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform childrenContainer;
        [SerializeField] private ChildDetailsCard childCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ParentApi parentApi;
        private int requestId;
        private readonly List<ChildDetailsCard> spawnedCards = new();

        private void Awake()
        {
            ApiClient apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);
        }

        private void OnEnable()
        {
            ResetAndLoad();
        }

        private void ResetAndLoad()
        {
            ClearSpawnedCards();
            _ = LoadChildrenAsync();
        }

        private async Awaitable LoadChildrenAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[ChildListPanel] No valid session.");
                return;
            }

            int id = ++requestId;

            try
            {
                ChildListItem[] children = await parentApi.GetChildrenAsync();

                if (id != requestId || !isActiveAndEnabled) return;

                ClearSpawnedCards();

                if (children == null || children.Length == 0)
                {
                    ShowStatus("No children found.");
                    return;
                }

                foreach (ChildListItem child in children)
                {
                    if (child == null) continue;

                    ChildDetailsCard card = Instantiate(childCardPrefab, childrenContainer);
                    card.transform.SetSiblingIndex(0); // keep the add-children button (last sibling) at the bottom
                    card.Setup(child, OnChildSelected);
                    spawnedCards.Add(card);
                }
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load children: {ex.Message}");
            }
        }

        private void OnChildSelected(ChildListItem child)
        {
            if (ParentDashboardController.Instance != null)
                ParentDashboardController.Instance.SelectChild(child);

            Destroy(gameObject);
        }

        private void ShowStatus(string message)
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = message;
                statusFeedbackText.gameObject.SetActive(true);
            }
        }

        private void ClearSpawnedCards()
        {
            foreach (ChildDetailsCard card in spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            spawnedCards.Clear();
        }
    }
}
