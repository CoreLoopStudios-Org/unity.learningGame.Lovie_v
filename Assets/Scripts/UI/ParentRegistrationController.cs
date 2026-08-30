using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using System;

namespace UI
{
    public class ParentRegistrationController : MonoBehaviour
    {
        [Header("Registration UI")]
        [SerializeField] private GameObject registrationPanel;
        [SerializeField] private TMP_InputField fullNameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button registerButton;

        [Header("OTP Verification UI")]
        [SerializeField] private GameObject otpPanel;
        [SerializeField] private TMP_InputField otpInput;
        [SerializeField] private Button verifyButton;
        [SerializeField] private Button resendOtpButton;

        [Header("Common UI")]
        [SerializeField] private TextMeshProUGUI errorMessage;
        [SerializeField] private TextMeshProUGUI successMessage;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private Button backToLoginButton;

        [Header("Scene Navigation")]
        [SerializeField] private string loginScene = "Main Game/Parent/Parent Login";

        private ApiClient apiClient;
        private AuthApi authApi;
        private ApiConfig config;
        private string registeredEmail = string.Empty;

        void Start()
        {
            config = ApiConfig.Instance;
            apiClient = ApiClient.Instance;
            apiClient.Initialize(config);
            authApi = new AuthApi(apiClient);

            Debug.Log($"[ParentRegister] Controller initialized. API BaseUrl: {config.BaseUrl}");

            if (registrationPanel == null) Debug.LogError("[ParentRegister] registrationPanel reference is not assigned!");
            if (otpPanel == null) Debug.LogError("[ParentRegister] otpPanel reference is not assigned!");
            if (registerButton == null) Debug.LogError("[ParentRegister] Register button reference is not assigned!");

            if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
            if (verifyButton != null) verifyButton.onClick.AddListener(OnVerifyClicked);
            if (resendOtpButton != null) resendOtpButton.onClick.AddListener(OnResendOtpClicked);
            if (backToLoginButton != null) backToLoginButton.onClick.AddListener(OnBackToLoginClicked);

            ShowPanel(registrationPanel);
            ClearMessages();
            HideLoading();
        }

        async void OnRegisterClicked()
        {
            string fullName = fullNameInput?.text?.Trim() ?? string.Empty;
            string email = emailInput?.text?.Trim() ?? string.Empty;
            string password = passwordInput?.text ?? string.Empty;

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please fill in all fields.");
                return;
            }

            ShowLoading();
            ClearMessages();

            Debug.Log($"[ParentRegister] Registration requested. email={email}, endpoint={config.BaseUrl}/api/auth/register (POST)");

            try
            {
                // Force UserType.Parent for public registration per A1 security fix
                var response = await authApi.RegisterAsync(email, password, fullName, UserType.Parent);
                Debug.Log("[ParentRegister] Registration succeeded on server.");

                registeredEmail = email;
                ShowSuccess("Registration successful! Please check your email for the OTP.");

                // Automatically request verification email.
                // Wrapped separately: if this fails, the account EXISTS and is unverified,
                // so the OTP panel must still open (user can hit Resend).
                try
                {
                    await authApi.SendVerificationAsync(email);
                    Debug.Log($"[ParentRegister] OTP email requested for {email}.");
                }
                catch (Exception sendEx)
                {
                    Debug.LogError($"[ParentRegister] Could not send OTP email: {sendEx.Message} — showing OTP panel anyway, use Resend.");
                }

                Debug.Log("[ParentRegister] Opening OTP panel.");
                ShowPanel(otpPanel);
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[ParentRegister] Registration failed. HTTP {ex.responseCode}: {ex.errorMessage}");
                ShowError(ex.Message ?? "Registration failed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ParentRegister] Unexpected error (likely network/DNS/timeout): {ex.GetType().Name} - {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void OnVerifyClicked()
        {
            string otp = otpInput?.text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(otp))
            {
                ShowError("Please enter the OTP sent to your email.");
                return;
            }

            ShowLoading();
            ClearMessages();

            try
            {
                Debug.Log($"[ParentRegister] Verifying OTP for {registeredEmail}, endpoint={config.BaseUrl}/api/auth/verify-email (POST)");
                await authApi.VerifyEmailAsync(registeredEmail, otp);
                Debug.Log($"[ParentRegister] Email verified OK. Loading '{loginScene}' in 2s...");
                ShowSuccess("Email verified successfully! You can now log in.");
                Invoke(nameof(OnBackToLoginClicked), 2f);
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[ParentRegister] Verification failed. HTTP {ex.responseCode}: {ex.errorMessage}");
                ShowError(ex.Message ?? "Verification failed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ParentRegister] OTP verification error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void OnResendOtpClicked()
        {
            if (string.IsNullOrEmpty(registeredEmail)) return;

            ShowLoading();
            ClearMessages();

            Debug.Log($"[ParentRegister] Resending OTP for {registeredEmail}...");

            try
            {
                await authApi.SendVerificationAsync(registeredEmail);
                Debug.Log("[ParentRegister] Resend OTP succeeded.");
                ShowSuccess("A new OTP has been sent to your email.");
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[ParentRegister] Resend OTP failed. HTTP {ex.responseCode}: {ex.errorMessage}");
                ShowError(ex.Message ?? "Failed to resend OTP.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ParentRegister] Resend OTP error: {ex.Message}");
                ShowError("Something went wrong.");
            }
            finally
            {
                HideLoading();
            }
        }

        void OnBackToLoginClicked()
        {
            Debug.Log($"[ParentRegister] Loading scene '{loginScene}'...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
        }

        void ShowPanel(GameObject panelToShow)
        {
            // NOTE: otpPanel is a CHILD of registrationPanel in the scene hierarchy.
            // Deactivating registrationPanel would hide the OTP panel with it, so the
            // registration panel must stay active while the OTP overlay is shown
            // (the OTP panel's full-screen image covers the form underneath).
            bool showRegistration = panelToShow == registrationPanel;
            bool showOtp = panelToShow == otpPanel;

            Debug.Log($"[ParentRegister] ShowPanel: registration={showRegistration}, otp={showOtp}");

            if (registrationPanel != null)
                registrationPanel.SetActive(showRegistration || showOtp);
            if (otpPanel != null)
                otpPanel.SetActive(showOtp);
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
                successMessage.color = Color.green;
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
            if (registerButton != null) registerButton.interactable = state;
            if (verifyButton != null) verifyButton.interactable = state;
            if (resendOtpButton != null) resendOtpButton.interactable = state;
            if (backToLoginButton != null) backToLoginButton.interactable = state;
        }

        void OnDestroy()
        {
            if (registerButton != null) registerButton.onClick.RemoveListener(OnRegisterClicked);
            if (verifyButton != null) verifyButton.onClick.RemoveListener(OnVerifyClicked);
            if (resendOtpButton != null) resendOtpButton.onClick.RemoveListener(OnResendOtpClicked);
            if (backToLoginButton != null) backToLoginButton.onClick.RemoveListener(OnBackToLoginClicked);
        }
    }
}
