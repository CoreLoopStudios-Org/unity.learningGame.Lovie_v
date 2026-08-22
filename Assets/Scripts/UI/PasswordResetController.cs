using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using System;

namespace UI
{
    public class PasswordResetController : MonoBehaviour
    {
        [Header("Request OTP UI")]
        [SerializeField] private GameObject requestPanel;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private Button requestOtpButton;

        [Header("Reset Password UI")]
        [SerializeField] private GameObject resetPanel;
        [SerializeField] private TMP_InputField otpInput;
        [SerializeField] private TMP_InputField newPasswordInput;
        [SerializeField] private Button resetPasswordButton;

        [Header("Common UI")]
        [SerializeField] private TextMeshProUGUI errorMessage;
        [SerializeField] private TextMeshProUGUI successMessage;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private Button backToLoginButton;

        [Header("Scene Navigation")]
        [SerializeField] private string loginScene = "Main Game/Parent/Parent Login";

        private ApiClient apiClient;
        private AuthApi authApi;
        private string resetEmail = string.Empty;

        void Start()
        {
            var config = ApiConfig.Instance;
            apiClient = ApiClient.Instance;
            apiClient.Initialize(config);
            authApi = new AuthApi(apiClient);

            if (requestOtpButton != null) requestOtpButton.onClick.AddListener(OnRequestOtpClicked);
            if (resetPasswordButton != null) resetPasswordButton.onClick.AddListener(OnResetPasswordClicked);
            if (backToLoginButton != null) backToLoginButton.onClick.AddListener(OnBackToLoginClicked);

            ShowPanel(requestPanel);
            ClearMessages();
            HideLoading();
        }

        async void OnRequestOtpClicked()
        {
            string email = emailInput?.text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email address.");
                return;
            }

            ShowLoading();
            ClearMessages();

            try
            {
                await authApi.SendResetOtpAsync(email);
                
                resetEmail = email;
                ShowSuccess("A password reset OTP has been sent to your email.");
                ShowPanel(resetPanel);
            }
            catch (ApiException ex)
            {
                ShowError(ex.Message ?? "Failed to send OTP.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Request reset OTP error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void OnResetPasswordClicked()
        {
            string otp = otpInput?.text?.Trim() ?? string.Empty;
            string newPassword = newPasswordInput?.text ?? string.Empty;

            if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(newPassword))
            {
                ShowError("Please enter the OTP and your new password.");
                return;
            }

            ShowLoading();
            ClearMessages();

            try
            {
                await authApi.ResetPasswordAsync(resetEmail, otp, newPassword);
                ShowSuccess("Password reset successfully! You can now log in.");
                Invoke(nameof(OnBackToLoginClicked), 2f);
            }
            catch (ApiException ex)
            {
                ShowError(ex.Message ?? "Failed to reset password.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Reset password error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        void OnBackToLoginClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
        }

        void ShowPanel(GameObject panelToShow)
        {
            if (requestPanel != null) requestPanel.SetActive(panelToShow == requestPanel);
            if (resetPanel != null) resetPanel.SetActive(panelToShow == resetPanel);
        }

        void ShowError(string message)
        {
            if (errorMessage != null)
            {
                errorMessage.text = message;
                errorMessage.gameObject.SetActive(true);
            }
            if (successMessage != null) successMessage.gameObject.SetActive(false);
        }

        void ShowSuccess(string message)
        {
            if (successMessage != null)
            {
                successMessage.text = message;
                successMessage.gameObject.SetActive(true);
            }
            if (errorMessage != null) errorMessage.gameObject.SetActive(false);
        }

        void ClearMessages()
        {
            if (errorMessage != null) errorMessage.gameObject.SetActive(false);
            if (successMessage != null) successMessage.gameObject.SetActive(false);
        }

        void ShowLoading()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            SetButtonsInteractable(false);
        }

        void HideLoading()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            SetButtonsInteractable(true);
        }

        void SetButtonsInteractable(bool state)
        {
            if (requestOtpButton != null) requestOtpButton.interactable = state;
            if (resetPasswordButton != null) resetPasswordButton.interactable = state;
            if (backToLoginButton != null) backToLoginButton.interactable = state;
        }

        void OnDestroy()
        {
            if (requestOtpButton != null) requestOtpButton.onClick.RemoveListener(OnRequestOtpClicked);
            if (resetPasswordButton != null) resetPasswordButton.onClick.RemoveListener(OnResetPasswordClicked);
            if (backToLoginButton != null) backToLoginButton.onClick.RemoveListener(OnBackToLoginClicked);
        }
    }
}
