using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Api;
using Api.Endpoints;

namespace UI
{
    public class ForgotPasswordVerifyCodeController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField codeInput;

        [Header("Buttons")]
        [SerializeField] private Button verifyButton;
        [SerializeField] private Button resendButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject loadingPrefab;

        [Header("On Success — wire panel swaps or any other calls here")]
        [SerializeField] private UnityEvent onVerified;

        [SerializeField] private GameObject setPasswordPanel;

        private ApiClient apiClient;
        private AuthApi authApi;
        private GameObject loadingInstance;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            authApi = new AuthApi(apiClient);

            if (verifyButton != null)
                verifyButton.onClick.AddListener(OnVerifyClicked);
            if (resendButton != null)
                resendButton.onClick.AddListener(OnResendClicked);
        }

        private void OnDestroy()
        {
            if (verifyButton != null)
                verifyButton.onClick.RemoveListener(OnVerifyClicked);
            if (resendButton != null)
                resendButton.onClick.RemoveListener(OnResendClicked);
        }

        private void OnEnable()
        {
            ClearFeedback();
        }

        private void OnVerifyClicked()
        {
            _ = VerifyCodeAsync();
        }

        private void OnResendClicked()
        {
            _ = ResendCodeAsync();
        }

        private async Awaitable VerifyCodeAsync()
        {
            string code = codeInput != null ? codeInput.text.Trim() : null;

            if (string.IsNullOrEmpty(code))
            {
                ShowFeedback("Please enter the code sent to your email.");
                return;
            }

            string email = ForgotPasswordContext.Email;
            if (string.IsNullOrEmpty(email))
            {
                ShowFeedback("Please enter your email first.");
                return;
            }

            SetButtonsInteractable(false);
            ShowLoading();
            ClearFeedback();

            try
            {
                await authApi.VerifyEmailAsync(email, code);

                if (!isActiveAndEnabled) return;

                // The reset controller resubmits email + code with the new password.
                ForgotPasswordContext.Otp = code;

                if (setPasswordPanel != null)
                    setPasswordPanel.SetActive(true);

                onVerified?.Invoke();
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[ForgotPasswordVerifyCode] Failed to verify code: {ex.Message}");
                ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Invalid or expired code." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForgotPasswordVerifyCode] {ex.Message}");
                ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
                SetButtonsInteractable(true);
            }
        }

        private async Awaitable ResendCodeAsync()
        {
            string email = ForgotPasswordContext.Email;
            if (string.IsNullOrEmpty(email))
            {
                ShowFeedback("Please enter your email first.");
                return;
            }

            SetButtonsInteractable(false);
            ShowLoading();
            ClearFeedback();

            try
            {
                await authApi.SendResetOtpAsync(email);

                if (!isActiveAndEnabled) return;

                ShowFeedback("A new code has been sent to your email.");
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[ForgotPasswordVerifyCode] Failed to resend code: {ex.Message}");
                ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Failed to resend the code." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForgotPasswordVerifyCode] {ex.Message}");
                ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
                SetButtonsInteractable(true);
            }
        }

        private void SetButtonsInteractable(bool state)
        {
            if (verifyButton != null) verifyButton.interactable = state;
            if (resendButton != null) resendButton.interactable = state;
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
