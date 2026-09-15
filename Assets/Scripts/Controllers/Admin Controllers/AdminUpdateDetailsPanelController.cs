using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminUpdateDetailsPanelController : MonoBehaviour
    {
        private const string MaskedPassword = "••••••••";

        [Header("Current Password (required for any update)")]
        [SerializeField] private TMP_InputField currentPasswordInput;

        [Header("Input Fields (empty = unchanged)")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Update")]
        [SerializeField] private Button updateButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject loadingPrefab;

        [Header("On Success")]
        [SerializeField] private AdminProfilePanelController profilePanel;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private GameObject loadingInstance;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

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
                Debug.LogWarning("[AdminUpdateDetailsPanelController] No valid session, skipping profile load.");
                return;
            }

            int id = ++requestId;
            ShowLoading();

            try
            {
                AdminProfile profile = await adminApi.GetProfileAsync();
                if (id != requestId || !isActiveAndEnabled) return;

                ApplyProfile(profile);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminUpdateDetailsPanelController] Failed to load profile: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminUpdateDetailsPanelController] Failed to load profile: {ex.Message}");
            }
            finally
            {
                HideLoading();
            }
        }

        private void ApplyProfile(AdminProfile profile)
        {
            if (profile == null) return;

            SetField(nameInput, profile.fullName);
            SetField(emailInput, profile.email);
            // The API never returns the password, so the placeholders are masked dots.
            SetField(currentPasswordInput, MaskedPassword);
            SetField(passwordInput, MaskedPassword);
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
            string currentPassword = currentPasswordInput != null ? currentPasswordInput.text : null;
            string newName = nameInput != null ? nameInput.text.Trim() : null;
            string newEmail = emailInput != null ? emailInput.text.Trim() : null;
            string newPassword = passwordInput != null ? passwordInput.text : null;

            bool hasCurrent = !string.IsNullOrEmpty(currentPassword);
            bool hasName = !string.IsNullOrEmpty(newName);
            bool hasEmail = !string.IsNullOrEmpty(newEmail);
            bool hasPassword = !string.IsNullOrEmpty(newPassword);

            if (!hasName && !hasEmail && !hasPassword)
                return;

            if (!hasCurrent)
            {
                ShowFeedback("Please enter your current password.");
                return;
            }

            if (updateButton != null)
                updateButton.interactable = false;

            ShowLoading();
            ClearFeedback();

            try
            {
                await adminApi.UpdateProfileAsync(
                    email: hasEmail ? newEmail : null,
                    fullName: hasName ? newName : null,
                    currentPassword: currentPassword,
                    newPassword: hasPassword ? newPassword : null);

                if (!isActiveAndEnabled) return;

                if (currentPasswordInput != null)
                    currentPasswordInput.text = string.Empty;
                if (passwordInput != null)
                    passwordInput.text = string.Empty;

                if (profilePanel != null)
                    profilePanel.Refresh();

                gameObject.SetActive(false);
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[AdminUpdateDetailsPanelController] Failed to update profile: {ex.Message}");
                ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Failed to update details." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdminUpdateDetailsPanelController] Failed to update profile: {ex.Message}");
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
