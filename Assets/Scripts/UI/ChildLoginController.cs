using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using System;

namespace UI
{
    public class ChildLoginController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI errorMessage;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuScene = "Main Game/Children/Main Menu";

        private ApiClient apiClient;
        private AuthApi authApi;
        private ChildApi childApi;

        void Start()
        {
            var config = ApiConfig.LoadConfig();
            apiClient = new ApiClient(config);
            authApi = new AuthApi(apiClient);
            childApi = new ChildApi(apiClient);

            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);

            ClearError();
            HideLoading();
        }

        async void OnLoginClicked()
        {
            string username = usernameInput?.text?.Trim() ?? string.Empty;
            string password = passwordInput?.text ?? string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password!");
                return;
            }

            ShowLoading();
            ClearError();

            try
            {
                var response = await authApi.ChildLoginAsync(username, password);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    SessionManager.Instance.SetSession(
                        response.token,
                        response.expiresAt,
                        "Child",
                        response.childId
                    );

                    await CheckDailyReward(response.token);

                    UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
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
                Debug.LogError($"Login error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void CheckDailyReward(string token)
        {
            try
            {
                var stats = await childApi.GetStatsAsync();
                if (stats != null && stats.canClaimDailyReward)
                {
                    var result = await childApi.ClaimDailyRewardAsync();
                    Debug.Log($"Daily reward: {(result.alreadyClaimed ? "Already claimed" : $"Awarded {result.coinsAwarded} coins")} | Streak: {result.loginStreak}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Daily reward check failed: {ex.Message}");
            }
        }

        void HandleApiError(ApiException ex)
        {
            if (ex.ResponseCode == 401)
            {
                ShowError("Oops! Wrong username or password. Try again!");
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
        }
    }
}
