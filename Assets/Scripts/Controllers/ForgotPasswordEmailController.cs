using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Api;
using Api.Endpoints;

namespace UI
{
    public class ForgotPasswordEmailController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField emailInput;

        [Header("Verify")]
        [SerializeField] private Button verifyButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject loadingPrefab;

        [Header("On Success — wire panel swaps or any other calls here")]
        [SerializeField] private UnityEvent onOtpSent;

        [SerializeField] private GameObject verifyPanel;

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
        }

        private void OnDestroy()
        {
            if (verifyButton != null)
                verifyButton.onClick.RemoveListener(OnVerifyClicked);
        }

        private void OnEnable()
        {
            ClearFeedback();
        }

        private void OnVerifyClicked()
        {
            _ = VerifyEmailAsync();
        }

        private async Awaitable VerifyEmailAsync()
        {
            string email = emailInput != null ? emailInput.text.Trim() : null;

            if (string.IsNullOrEmpty(email))
            {
                ShowFeedback("Please enter your email.");
                return;
            }

            if (verifyButton != null)
                verifyButton.interactable = false;

            ShowLoading();
            ClearFeedback();

            try
            {
                // One call checks the user table AND mails the OTP — an error
                // response means the email is not registered.
                await authApi.SendResetOtpAsync(email);

                if (!isActiveAndEnabled) return;

                ForgotPasswordContext.Email = email;

                if (verifyPanel != null)
                    verifyPanel.SetActive(true);

                onOtpSent?.Invoke();
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[ForgotPasswordEmail] Failed to send reset OTP: {ex.Message}");
                ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "User does not exist." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ForgotPasswordEmail] {ex.Message}");
                ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
                if (verifyButton != null)
                    verifyButton.interactable = true;
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
