using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using System;
using System.Text;

namespace UI
{
    public class ParentDashboardController : MonoBehaviour
    {
        [Header("Dashboard UI")]
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI childrenListText;
        [SerializeField] private Button loadDashboardButton;
        [SerializeField] private Button logoutButton;

        [Header("Child Management UI")]
        [SerializeField] private TMP_InputField newChildUsernameInput;
        [SerializeField] private TMP_InputField newChildPasswordInput;
        [SerializeField] private Button createChildButton;
        
        [Header("Child Details & Activities UI")]
        [SerializeField] private TMP_InputField childIdInput;
        [SerializeField] private Button viewActivitiesButton;
        [SerializeField] private TextMeshProUGUI activitiesListText;

        [Header("Common UI")]
        [SerializeField] private TextMeshProUGUI errorMessage;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Scene Navigation")]
        [SerializeField] private string loginScene = "Main Game/Parent/Parent Login";

        private ApiClient apiClient;
        private ParentApi parentApi;

        void Start()
        {
            apiClient = ApiClient.Instance;
            
            if (apiClient == null || !SessionManager.Instance.IsValidToken())
            {
                OnLogoutClicked();
                return;
            }

            parentApi = new ParentApi(apiClient);

            if (loadDashboardButton != null) loadDashboardButton.onClick.AddListener(LoadDashboard);
            if (createChildButton != null) createChildButton.onClick.AddListener(OnCreateChildClicked);
            if (viewActivitiesButton != null) viewActivitiesButton.onClick.AddListener(OnViewActivitiesClicked);
            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);

            ClearError();
            HideLoading();
            LoadDashboard();
        }

        async void LoadDashboard()
        {
            ShowLoading();
            ClearError();

            try
            {
                var dashboard = await parentApi.GetDashboardAsync();
                
                if (statsText != null)
                {
                    statsText.text = $"Total Children: {dashboard.totalChildren} | Active: {dashboard.activeChildren}";
                }

                if (childrenListText != null)
                {
                    StringBuilder sb = new StringBuilder();
                    if (dashboard.childSummaries != null)
                    {
                        foreach (var child in dashboard.childSummaries)
                        {
                            sb.AppendLine($"ID: {child.childId}");
                            sb.AppendLine($"Username: {child.username}");
                            sb.AppendLine($"Coins: {child.totalCoins} | Streak: {child.loginStreak}");
                            sb.AppendLine($"Last Activity: {child.lastActivityAt}");
                            sb.AppendLine("-------------------");
                        }
                    }
                    childrenListText.text = sb.Length > 0 ? sb.ToString() : "No children found.";
                }
            }
            catch (ApiException ex)
            {
                ShowError(ex.Message ?? "Failed to load dashboard.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Dashboard error: {ex.Message}");
                ShowError("Something went wrong loading dashboard.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void OnCreateChildClicked()
        {
            string username = newChildUsernameInput?.text?.Trim() ?? string.Empty;
            string password = newChildPasswordInput?.text ?? string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter child username and password.");
                return;
            }

            ShowLoading();
            ClearError();

            try
            {
                await parentApi.CreateChildAsync(username, password);
                
                if (newChildUsernameInput != null) newChildUsernameInput.text = "";
                if (newChildPasswordInput != null) newChildPasswordInput.text = "";
                
                LoadDashboard(); // Refresh
            }
            catch (ApiException ex)
            {
                ShowError(ex.Message ?? "Failed to create child.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Create child error: {ex.Message}");
                ShowError("Something went wrong creating child.");
            }
            finally
            {
                HideLoading();
            }
        }

        async void OnViewActivitiesClicked()
        {
            string childId = childIdInput?.text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(childId))
            {
                ShowError("Please enter a child ID to view activities.");
                return;
            }

            ShowLoading();
            ClearError();

            try
            {
                var activities = await parentApi.GetChildActivitiesAsync(childId);
                
                if (activitiesListText != null)
                {
                    StringBuilder sb = new StringBuilder();
                    if (activities != null)
                    {
                        foreach (var activity in activities)
                        {
                            sb.AppendLine($"Type: {(ActivityType)activity.activityType}");
                            sb.AppendLine($"Date: {activity.createdAt}");
                            sb.AppendLine($"Details: {activity.payload}");
                            sb.AppendLine("---");
                        }
                    }
                    activitiesListText.text = sb.Length > 0 ? sb.ToString() : "No recent activities.";
                }
            }
            catch (ApiException ex)
            {
                ShowError(ex.Message ?? "Failed to load activities.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"View activities error: {ex.Message}");
                ShowError("Something went wrong loading activities.");
            }
            finally
            {
                HideLoading();
            }
        }

        void OnLogoutClicked()
        {
            SessionManager.Instance.ClearSession();
            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
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
        }

        void HideLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }

        void OnDestroy()
        {
            if (loadDashboardButton != null) loadDashboardButton.onClick.RemoveListener(LoadDashboard);
            if (createChildButton != null) createChildButton.onClick.RemoveListener(OnCreateChildClicked);
            if (viewActivitiesButton != null) viewActivitiesButton.onClick.RemoveListener(OnViewActivitiesClicked);
            if (logoutButton != null) logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }
    }
}
