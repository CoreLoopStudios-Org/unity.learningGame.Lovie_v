using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using System;

namespace UI
{
    public class ParentLoginController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI errorMessage;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Social Sign-In")]
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button appleSignInButton;

        private bool socialFlowRunning;

        [Header("Scene Navigation")]
        [SerializeField] private string dashboardScene = "Main Game/Parent/Parent Dashboard";

        private ApiClient apiClient;
        private AuthApi authApi;

        void Start()
        {
            var config = ApiConfig.Instance;
            apiClient = ApiClient.Instance;
            apiClient.Initialize(config);
            authApi = new AuthApi(apiClient);

            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);

            if (googleSignInButton != null)
            {
                googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);

                bool googleAvailable = SocialLoginService.IsGoogleAvailable;
                googleSignInButton.gameObject.SetActive(googleAvailable);
            }

            if (appleSignInButton != null)
            {
                appleSignInButton.onClick.AddListener(OnAppleSignInClicked);

#if UNITY_IOS && !UNITY_EDITOR
    appleSignInButton.gameObject.SetActive(
        SocialLoginService.IsAppleAvailable
    );
#else
                appleSignInButton.gameObject.SetActive(false);
#endif
            }

// Set Google button width to 968 only when Apple Sign-In is inactive.
            if (googleSignInButton != null &&
                googleSignInButton.gameObject.activeSelf &&
                (appleSignInButton == null || !appleSignInButton.gameObject.activeSelf))
            {
                RectTransform googleRect = googleSignInButton.GetComponent<RectTransform>();

                if (googleRect != null)
                {
                    googleRect.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal, 968f
                    );
                }
            }

            ClearError();
            HideLoading();
        }

        async void OnLoginClicked()
        {
            string email = emailInput?.text?.Trim() ?? string.Empty;
            string password = passwordInput?.text ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both email and password!");
                return;
            }

            ShowLoading();
            ClearError();

            try
            {
                var response = await authApi.LoginAsync(email, password);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    string role = SessionManager.ExtractRoleFromTokenStatic(response.token);

                    if (role != "Parent")
                    {
                        ShowError("This panel is for parent accounts only!");
                        HideLoading();
                        return;
                    }

                    EnsureSessionManager();

                    SessionManager.Instance.SetSession(response.token, response.expiresAt, "Parent", null);
                    SceneTransition.Load(dashboardScene);
                }
                else
                {
                    ShowError("Login failed. Please try again.");
                }
            }
            catch (ApiException ex)
            {
                HandleApiError(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Parent login error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void OnGoogleSignInClicked()
        {
            if (socialFlowRunning) return;
            socialFlowRunning = true;
            ShowLoading();
            ClearError();

            try
            {
                EnsureSessionManager();
                await SocialLoginService.LoginWithGoogle(); // sheet → backend → session stored
                SceneTransition.Load(dashboardScene);
            }
            catch (SocialAuthException ex)
            {
                if (!ex.Cancelled) // user closed the sheet — no error spam, just stay put
                {
                    ShowError(ex.Code == "config_missing"
                        ? "Google sign-in is not set up on this build."
                        : ex.Message);
                }
            }
            catch (ApiException ex)
            {
                HandleApiError(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Google sign-in error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                socialFlowRunning = false;
                HideLoading();
            }
        }

        async void OnAppleSignInClicked()
        {
            if (socialFlowRunning) return;
            socialFlowRunning = true;
            ShowLoading();
            ClearError();

            try
            {
                EnsureSessionManager();
                await SocialLoginService.LoginWithApple();
                SceneTransition.Load(dashboardScene);
            }
            catch (SocialAuthException ex)
            {
                if (!ex.Cancelled) ShowError(ex.Message);
            }
            catch (ApiException ex)
            {
                HandleApiError(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Apple sign-in error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                socialFlowRunning = false;
                HideLoading();
            }
        }

        void EnsureSessionManager()
        {
            if (SessionManager.Instance == null)
            {
                var sessionGo = new GameObject("SessionManager");
                sessionGo.AddComponent<SessionManager>();
            }
        }

        void HandleApiError(ApiException ex)
        {
            if (ex.responseCode == 401)
            {
                if (ex.Message != null && ex.Message.ToLower().Contains("verify"))
                {
                    ShowError("Please verify your email before logging in!");
                }
                else
                {
                    ShowError("Wrong email or password. Try again!");
                }
            }
            else if (!string.IsNullOrEmpty(ex.Message))
            {
                ShowError(ex.Message);
            }
            else
            {
                ShowError("Connection problem. Check internet!");
            }
        }

        void ShowError(string message)
        {
            if (errorMessage != null)
            {
                errorMessage.text = message;
                errorMessage.gameObject.SetActive(true);
            }
        }

        void ClearError()
        {
            if (errorMessage != null)
                errorMessage.gameObject.SetActive(false);
        }

        void ShowLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);
            if (loginButton != null)
                loginButton.interactable = false;
        }

        void HideLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
            if (loginButton != null)
                loginButton.interactable = true;
        }

        void OnDestroy()
        {
            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnLoginClicked);
            if (googleSignInButton != null)
                googleSignInButton.onClick.RemoveListener(OnGoogleSignInClicked);
            if (appleSignInButton != null)
                appleSignInButton.onClick.RemoveListener(OnAppleSignInClicked);
        }
    }
}
