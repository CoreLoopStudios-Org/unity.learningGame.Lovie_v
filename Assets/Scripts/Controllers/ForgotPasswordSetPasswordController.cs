using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Api;
using Api.Endpoints;

namespace UI
{
    public class ForgotPasswordSetPasswordController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField retypePasswordInput;

        [Header("Update")]
        [SerializeField] private Button updatePasswordButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject loadingPrefab;

        [Header("On Success — wire panel swaps or any other calls here")]
        [SerializeField] private UnityEvent onPasswordReset;

        private ApiClient apiClient;
        private AuthApi authApi;
        private GameObject loadingInstance;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            authApi = new AuthApi(apiClient);

            if (updatePasswordButton != null)
                updatePasswordButton.onClick.AddListener(OnUpdatePasswordClicked);
        }

        private void OnDestroy()
        {
            if (updatePasswordButton != null)
                updatePasswordButton.onClick.RemoveListener(OnUpdatePasswordClicked);
        }

        private void OnEnable()
        {
            ClearFeedback();
            SetInputsInteractable(true);
        }

        private void OnUpdatePasswordClicked()
        {
            _ = UpdatePasswordAsync();
        }

        private async Awaitable UpdatePasswordAsync()
        {
            string password = passwordInput != null ? passwordInput.text : null;
            string retypePassword = retypePasswordInput != null ? retypePasswordInput.text : null;

            if (string.IsNullOrEmpty(password))
            {
                ShowFeedback("Please enter a new password.");
                return;
            }

            if (password != retypePassword)
            {
                ShowFeedback("Passwords do not match.");
                return;
            }

            string email = ForgotPasswordContext.Email;
            string otp = ForgotPasswordContext.Otp;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                ShowFeedback("Password reset session expired. Please start again.");
                return;
            }

            if (updatePasswordButton != null)
                updatePasswordButton.interactable = false;

            ShowLoading();
            ClearFeedback();

            try
            {
                await authApi.ResetPasswordAsync(email, otp, password);

                if (!isActiveAndEnabled) return;

                if (passwordInput != null)
                {
                    passwordInput.text = string.Empty;
                    passwordInput.interactable = false;
                }
                if (retypePasswordInput != null)
                {
                    retypePasswordInput.text = string.Empty;
                    retypePasswordInput.interactable = false;
                }

                // Flow complete — clear the shared state.
                ForgotPasswordContext.Email = null;
                ForgotPasswordContext.Otp = null;

                ShowFeedback("Password updated. Redirecting to the login panel in 2 seconds.");

                await Awaitable.WaitForSecondsAsync(2f);

                if (!isActiveAndEnabled) return;

                onPasswordReset?.Invoke();
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[ForgotPasswordSetPassword] Failed to update password: {ex.Message}");
                ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Failed to update password." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForgotPasswordSetPassword] {ex.Message}");
                ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
                if (updatePasswordButton != null)
                    updatePasswordButton.interactable = true;
            }
        }

        private void SetInputsInteractable(bool state)
        {
            if (passwordInput != null) passwordInput.interactable = state;
            if (retypePasswordInput != null) retypePasswordInput.interactable = state;
            if (updatePasswordButton != null) updatePasswordButton.interactable = state;
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
