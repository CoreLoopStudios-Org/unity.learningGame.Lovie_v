using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;

namespace UI
{
    // Attach to the spawned Add Children Panel prefab. Creates a child account for the logged-in
    // parent on Add Child, then refreshes the Child List panel behind it.
    public class AddChildrenPanelController : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField nickNameInput;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Buttons")]
        [SerializeField] private Button addChildButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        private ParentApi parentApi;
        private int requestId;
        private bool isSubmitting;

        [Serializable]
        private class BackendError
        {
            public string message;
        }

        private void Awake()
        {
            ApiClient apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);

            if (addChildButton != null) addChildButton.onClick.AddListener(OnAddChildClicked);
        }

        private void OnEnable()
        {
            ClearForm();
        }

        private void OnDestroy()
        {
            if (addChildButton != null) addChildButton.onClick.RemoveListener(OnAddChildClicked);
        }

        private void ClearForm()
        {
            if (nickNameInput != null) nickNameInput.text = string.Empty;
            if (usernameInput != null) usernameInput.text = string.Empty;
            if (passwordInput != null) passwordInput.text = string.Empty;

            isSubmitting = false;
            SetAddButtonInteractable(true);
            ShowFeedback(string.Empty);
        }

        private void OnAddChildClicked()
        {
            if (isSubmitting) return;

            string nickName = nickNameInput != null ? nickNameInput.text.Trim() : string.Empty;
            string username = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;

            if (string.IsNullOrEmpty(nickName))
            {
                ShowFeedback("Please enter a nick name.");
                return;
            }

            if (username.Length < 3)
            {
                ShowFeedback("Username must be at least 3 characters.");
                return;
            }

            if (password.Length < 6)
            {
                ShowFeedback("Password must be at least 6 characters.");
                return;
            }

            _ = CreateChildAsync(nickName, username, password);
        }

        private async Awaitable CreateChildAsync(string fullName, string username, string password)
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                ShowFeedback("Session expired. Please log in again.");
                return;
            }

            isSubmitting = true;
            SetAddButtonInteractable(false);
            ShowFeedback(string.Empty);

            int id = ++requestId;

            try
            {
                await parentApi.CreateChildAsync(fullName, username, password);

                if (id != requestId || !isActiveAndEnabled) return;

                ShowFeedback("Child added successfully.");
                ClearForm();

                FindAnyObjectByType<ChildListPanelController>()?.Refresh();
            }
            catch (ApiException ex)
            {
                if (id != requestId) return;

                string backendMessage = ExtractBackendMessage(ex.errorMessage);

                if (!string.IsNullOrEmpty(backendMessage) &&
                    backendMessage.Contains("already taken", StringComparison.OrdinalIgnoreCase))
                {
                    ShowFeedback("Username must be unique.");
                }
                else
                {
                    ShowFeedback($"Failed to add child: {backendMessage ?? ex.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowFeedback($"Failed to add child: {ex.Message}");
            }
            finally
            {
                if (id == requestId)
                {
                    isSubmitting = false;
                    SetAddButtonInteractable(true);
                }
            }
        }

        private static string ExtractBackendMessage(string rawError)
        {
            if (string.IsNullOrWhiteSpace(rawError)) return null;

            // Backend error bodies are JSON like {"status":"Fail","message":"..."} but ApiClient
            // surfaces them as raw text; pull the message out for readable feedback.
            try
            {
                return JsonUtility.FromJson<BackendError>(rawError)?.message;
            }
            catch
            {
                return null;
            }
        }

        private void SetAddButtonInteractable(bool value)
        {
            if (addChildButton != null) addChildButton.interactable = value;
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText == null) return;

            feedbackText.text = message;
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
