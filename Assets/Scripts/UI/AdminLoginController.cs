using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using System;

namespace UI
{
    public class AdminLoginController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI errorMessage;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Scene Navigation")]
        [SerializeField] private string adminDashboardScene = "Admin Dashbaord";

        private ApiClient apiClient;
        private AuthApi authApi;
        private ApiConfig config;

        void Start()
        {
            config = ApiConfig.Instance;
            apiClient = ApiClient.Instance;
            apiClient.Initialize(config);
            authApi = new AuthApi(apiClient);

            Debug.Log($"[AdminLogin] Controller initialized. API BaseUrl: {config.BaseUrl}");

            if (SessionManager.Instance == null)
                Debug.LogWarning("[AdminLogin] SessionManager.Instance is NULL (bootstrap scene did not run). " +
                    "A SessionManager will be created automatically after a successful login, " +
                    "but the session won't auto-resume on next app start until the bootstrap is wired.");

            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            else
                Debug.LogError("[AdminLogin] Login button reference is not assigned in the Inspector!");

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

            Debug.Log($"[AdminLogin] Login requested. email={email}, endpoint={config.BaseUrl}/api/auth/login (POST)");

            try
            {
                var response = await authApi.LoginAsync(email, password);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    Debug.Log($"[AdminLogin] Server responded OK. Token received: yes, expiresAt: {response.expiresAt ?? "null"}");

                    string role = SessionManager.ExtractRoleFromTokenStatic(response.token);
                    Debug.Log($"[AdminLogin] Role decoded from JWT: {role ?? "null"}");

                    if (role != "Admin")
                    {
                        Debug.LogWarning($"[AdminLogin] Access denied — token role '{role}' is not Admin.");
                        ShowError("Access denied. Admin only!");
                        HideLoading();
                        return;
                    }

                    if (SessionManager.Instance == null)
                    {
                        Debug.LogWarning("[AdminLogin] SessionManager missing (bootstrap scene never ran) — creating it now.");
                        var sessionGo = new GameObject("SessionManager");
                        sessionGo.AddComponent<SessionManager>();
                    }

                    SessionManager.Instance.SetSession(response.token, response.expiresAt, "Admin", null);
                    Debug.Log($"[AdminLogin] Session saved. Loading scene '{adminDashboardScene}'...");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(adminDashboardScene);
                }
                else
                {
                    Debug.LogWarning($"[AdminLogin] Server responded but no token in body. response null? {response == null}");
                    ShowError("Login failed. Please try again.");
                }
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[AdminLogin] Login failed. HTTP {ex.responseCode}: {ex.errorMessage}");
                HandleApiError(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdminLogin] Unexpected error (likely network/DNS/timeout): {ex.GetType().Name} - {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        void HandleApiError(ApiException ex)
        {
            if (ex.responseCode == 401)
            {
                Debug.LogWarning("[AdminLogin] Got 401. Note: ApiClient fires its global OnSessionExpired on ANY 401, " +
                    "which can reload this scene via SessionManager before the error message is shown.");
                ShowError("Wrong email or password. Try again!");
            }
            else if (!string.IsNullOrEmpty(ex.Message))
            {
                ShowError(ex.Message);
            }
            else
            {
                Debug.LogError($"[AdminLogin] HTTP {ex.responseCode} with empty body — usually a network failure " +
                    $"(server unreachable, DNS failure, or server down). Could NOT reach: {config.BaseUrl}");
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
        }
    }
}