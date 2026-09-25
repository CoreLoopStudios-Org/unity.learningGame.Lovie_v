using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class ParentUpdateDetailsPanelController : MonoBehaviour
    {
        [Header("Input Field (empty = unchanged)")]
        [SerializeField] private TMP_InputField fullNameInput;

        [Header("Update")]
        [SerializeField] private Button updateButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject loadingPrefab;

        [Header("On Success")]
        [SerializeField] private ParentProfilePanelController profilePanel;

        private ApiClient apiClient;
        private ParentApi parentApi;
        private int requestId;
        private GameObject loadingInstance;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);

            if (updateButton != null)
                updateButton.onClick.AddListener(OnUpdateClicked);
        }

        private void OnDestroy()
        {
            if (updateButton != null)
                updateButton.onClick.RemoveListener(OnUpdateClicked);
        }

        private void OnEnable()
        {
            ClearFeedback();
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[ParentUpdateDetailsPanelController] No valid session, skipping profile load.");
                return;
            }

            int id = ++requestId;
            ShowLoading();

            try
            {
                ParentProfile profile = await parentApi.GetProfileAsync();
                if (id != requestId || !isActiveAndEnabled) return;

                ApplyProfile(profile);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentUpdateDetailsPanelController] Failed to load profile: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentUpdateDetailsPanelController] Failed to load profile: {ex.Message}");
            }
            finally
            {
                HideLoading();
            }
        }

        private void ApplyProfile(ParentProfile profile)
        {
            if (profile == null) return;

            SetField(fullNameInput, profile.fullName);
        }

        private static void SetField(TMP_InputField input, string value)
        {
            if (input == null) return;

            input.text = string.Empty;
            if (input.placeholder is TextMeshProUGUI placeholder)
                placeholder.text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        private void OnUpdateClicked()
        {
            _ = UpdateDetailsAsync();
        }

        private async Awaitable UpdateDetailsAsync()
        {
            string newFullName = fullNameInput != null ? fullNameInput.text.Trim() : null;

            if (string.IsNullOrEmpty(newFullName))
            {
                ShowFeedback("Nothing to update.");
                return;
            }

            if (updateButton != null)
                updateButton.interactable = false;

            ShowLoading();
            ClearFeedback();

            try
            {
                await parentApi.UpdateProfileAsync(fullName: newFullName);

                if (!isActiveAndEnabled) return;


                ShowFeedback("Details updated successfully.");

                if (profilePanel != null)
                    profilePanel.Refresh();

                _ = RefreshAsync();
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[ParentUpdateDetailsPanelController] Failed to update profile: {ex.Message}");
                ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Failed to update details." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ParentUpdateDetailsPanelController] Failed to update profile: {ex.Message}");
                ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
                if (updateButton != null)
                    updateButton.interactable = true;
            }
        }

        private void ShowLoading()
        {
            if (loadingPrefab == null || loadingInstance != null) return;

            loadingInstance = Instantiate(loadingPrefab, transform);
        }

        private void HideLoading()
        {
            if (loadingInstance == null) return;

            Destroy(loadingInstance);
            loadingInstance = null;
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText == null) return;

            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
        }

        private void ClearFeedback()
        {
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
    }
}
