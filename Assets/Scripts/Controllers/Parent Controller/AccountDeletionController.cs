using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;

namespace UI
{
    public class AccountDeletionController : MonoBehaviour
    {
        private const float RedirectDelaySeconds = 2f;

        [Header("Deletion Entry")]
        [SerializeField] private Button deleteButton;
        [SerializeField] private GameObject otpPanel;

        [Header("OTP Panel")]
        [SerializeField] private TMP_InputField otpInputField;
        [SerializeField] private Button verifyButton;
        [SerializeField] private Button resendButton;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Common")]
        [SerializeField] private GameObject loadingPrefab;
        [SerializeField] private string loginScene = "Main Game/Parent/Parent Login";

        private ApiClient apiClient;
        private ParentApi parentApi;
        private GameObject loadingInstance;
        private bool deletionConfirmed;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);

            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteClicked);
            if (verifyButton != null)
                verifyButton.onClick.AddListener(OnVerifyClicked);
            if (resendButton != null)
                resendButton.onClick.AddListener(OnResendClicked);

            // The OTP panel is only shown once the user proceeds with deletion.
            if (otpPanel != null && otpPanel != gameObject && otpPanel.activeSelf)
                otpPanel.SetActive(false);

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (deleteButton != null)
                deleteButton.onClick.RemoveListener(OnDeleteClicked);
            if (verifyButton != null)
                verifyButton.onClick.RemoveListener(OnVerifyClicked);
            if (resendButton != null)
                resendButton.onClick.RemoveListener(OnResendClicked);

            HideLoading();
        }

        private void OnDeleteClicked()
        {
            if (deletionConfirmed) return;
            _ = SendDeletionOtpAsync();
        }

        private void OnVerifyClicked()
        {
            if (deletionConfirmed) return;
            _ = VerifyDeletionAsync();
        }

        private void OnResendClicked()
        {
            if (deletionConfirmed) return;
            _ = SendDeletionOtpAsync(showPanel: false);
        }

        private async Awaitable SendDeletionOtpAsync(bool showPanel = true)
        {
            if (showPanel && otpPanel != null)
            {
                otpPanel.SetActive(true);
                if (otpInputField != null) otpInputField.text = string.Empty;
                ClearFeedback();
            }

            SetAllButtonsInteractable(false);
            ShowLoading();

            try
            {
                await parentApi.SendDeletionOtpAsync();
                if (!isActiveAndEnabled) return;

                ShowFeedback("An account deletion code has been sent to your email.");
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[AccountDeletion] Failed to send deletion code: {ex.Message}");
                if (isActiveAndEnabled)
                    ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Failed to send the deletion code. Please try again." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AccountDeletion] {ex.Message}");
                if (isActiveAndEnabled)
                    ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
                if (!deletionConfirmed)
                    SetAllButtonsInteractable(true);
            }
        }

        private async Awaitable VerifyDeletionAsync()
        {
            string otp = otpInputField != null ? otpInputField.text.Trim() : null;

            if (string.IsNullOrEmpty(otp))
            {
                ShowFeedback("Please enter the code sent to your email.");
                return;
            }

            SetAllButtonsInteractable(false);
            ShowLoading();
            ClearFeedback();

            try
            {
                await parentApi.DeleteAccountAsync(otp);

                deletionConfirmed = true;

                if (SessionManager.Instance != null)
                    SessionManager.Instance.ClearSession();

                HideLoading();
                ShowFeedback("Account deleted successfully. Taking you to the login page...");

                await Awaitable.WaitForSecondsAsync(RedirectDelaySeconds);

                SceneTransition.Load(loginScene);
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[AccountDeletion] Failed to delete account: {ex.Message}");
                if (isActiveAndEnabled)
                    ShowFeedback(string.IsNullOrEmpty(ex.Message) ? "Invalid or expired code. Please try again." : ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AccountDeletion] {ex.Message}");
                if (isActiveAndEnabled)
                    ShowFeedback("Something went wrong. Please try again.");
            }
            finally
            {
                if (!deletionConfirmed)
                {
                    HideLoading();
                    SetAllButtonsInteractable(true);
                }
            }
        }

        private void SetAllButtonsInteractable(bool state)
        {
            if (deleteButton != null) deleteButton.interactable = state;
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
