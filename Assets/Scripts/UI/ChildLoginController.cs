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
        private ApiConfig config;

        void Start()
        {
            config = ApiConfig.Instance;
            apiClient = ApiClient.Instance;
            apiClient.Initialize(config);
            authApi = new AuthApi(apiClient);
            childApi = new ChildApi(apiClient);

            Debug.Log($"[ChildLogin] Controller initialized. API BaseUrl: {config.BaseUrl}");

            if (SessionManager.Instance == null)
                Debug.LogWarning("[ChildLogin] SessionManager.Instance is NULL (bootstrap scene did not run). " +
                    "A SessionManager will be created automatically after a successful login, " +
                    "but the session won't auto-resume on next app start until the bootstrap is wired.");

            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            else
                Debug.LogError("[ChildLogin] Login button reference is not assigned in the Inspector!");

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

            Debug.Log($"[ChildLogin] Login requested. username={username}, endpoint={config.BaseUrl}/api/auth/child/login (POST)");

            try
            {
                var response = await authApi.ChildLoginAsync(username, password);

                if (response != null && !string.IsNullOrEmpty(response.token))
                {
                    Debug.Log($"[ChildLogin] Server responded OK. Token received: yes, expiresAt: {response.expiresAt ?? "null"}, " +
                        $"childId: {response.childId ?? "null"}, coins: {response.coins}, streak: {response.loginStreak}");

                    if (SessionManager.Instance == null)
                    {
                        Debug.LogWarning("[ChildLogin] SessionManager missing (bootstrap scene never ran) — creating it now.");
                        var sessionGo = new GameObject("SessionManager");
                        sessionGo.AddComponent<SessionManager>();
                    }

                    SessionManager.Instance.SetSession(
                        response.token,
                        response.expiresAt,
                        "Child",
                        response.childId
                    );
                    Debug.Log("[ChildLogin] Session saved. Initializing coin wallet...");

                    InitializeCoinWallet(response.coins, response.loginStreak);
                    await CheckDailyReward(response.token);

                    Debug.Log($"[ChildLogin] Loading scene '{mainMenuScene}'...");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
                }
                else
                {
                    Debug.LogWarning($"[ChildLogin] Server responded but no token in body. response null? {response == null}");
                    ShowError("Login failed. Please try again.");
                }
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[ChildLogin] Login failed. HTTP {ex.responseCode}: {ex.errorMessage}");
                HandleApiError(ex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChildLogin] Unexpected error (likely network/DNS/timeout): {ex.GetType().Name} - {ex.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                HideLoading();
            }
        }

        async System.Threading.Tasks.Task CheckDailyReward(string token)
        {
            try
            {
                var stats = await childApi.GetStatsAsync();
                if (stats != null && stats.canClaimDailyReward)
                {
                    var result = await childApi.ClaimDailyRewardAsync();
                    Debug.Log($"Daily reward: {(result.alreadyClaimed ? "Already claimed" : $"Awarded {result.coinsAwarded} coins")} | Streak: {result.loginStreak}");

                    if (!result.alreadyClaimed && CoinWallet.Instance != null)
                    {
                        CoinWallet.Instance.UpdateBalance(result.totalCoins);
                        CoinWallet.Instance.UpdateStreak(result.loginStreak);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Daily reward check failed: {ex.Message}");
            }
        }

        void InitializeCoinWallet(int coins, int streak)
        {
            if (CoinWallet.Instance == null)
            {
                var go = new GameObject("CoinWallet");
                go.AddComponent<CoinWallet>();
            }

            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.SetBalance(coins);
                CoinWallet.Instance.SetStreak(streak);
            }
        }

        void HandleApiError(ApiException ex)
        {
            if (ex.responseCode == 401)
            {
                Debug.LogWarning("[ChildLogin] Got 401. Note: ApiClient fires its global OnSessionExpired on ANY 401, " +
                    "which can reload this scene via SessionManager before the error message is shown.");
                ShowError("Oops! Wrong username or password. Try again!");
            }
            else if (!string.IsNullOrEmpty(ex.Message))
            {
                ShowError(ex.Message);
            }
            else
            {
                Debug.LogError($"[ChildLogin] HTTP {ex.responseCode} with empty body — usually a network failure " +
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
