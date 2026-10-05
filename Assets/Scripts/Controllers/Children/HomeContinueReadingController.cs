using System;
using UnityEngine;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Home page "Continue Reading" card — same selection logic as the stories
    /// page (shared ContinueReading helper), fed by its own stories fetch.
    /// </summary>
    public class HomeContinueReadingController : MonoBehaviour
    {
        [Header("Card")]
        [SerializeField] private ContinueReadingCard continueReadingCard;

        [Header("Story Reading")]
        [SerializeField] private GameObject bookReadingPanelPrefab;
        [SerializeField] private Transform readingPanelParent;

        private int requestId;

        private void Awake()
        {
            if (continueReadingCard != null)
            {
                continueReadingCard.PlayClicked += HandlePlayClicked;
            }
        }

        private void OnDestroy()
        {
            if (continueReadingCard != null)
            {
                continueReadingCard.PlayClicked -= HandlePlayClicked;
            }
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[HomeContinueReadingController] No child session — cannot load continue reading.");
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

                var visibleStories = ContinueReading.FilterVisible(stories);
                var (target, progress) = ContinueReading.Select(visibleStories, SessionManager.Instance.ChildId);

                if (target != null)
                {
                    continueReadingCard.Setup(target, progress);
                }
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[HomeContinueReadingController] Failed to load continue reading: {ex.Message}");
            }
        }

        private void HandlePlayClicked()
        {
            Story story = continueReadingCard != null ? continueReadingCard.Story : null;
            if (story == null) return;

            Transform parent = readingPanelParent;
            if (parent == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                parent = canvas != null ? canvas.rootCanvas.transform : transform;
            }

            ContinueReading.SpawnReadingPanel(bookReadingPanelPrefab, story, parent);
        }
    }
}
