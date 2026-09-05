using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminProfilePanelController : MonoBehaviour
    {
        [Header("Profile Fields")]
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI emailText;
        [SerializeField] private TextMeshProUGUI phoneText;

        [Header("Logout")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private string adminLoginScene = "Admin Login";

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void OnDestroy()
        {
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminProfilePanelController] No valid session, skipping profile load.");
                return;
            }

            int id = ++requestId;

            try
            {
                AdminProfile profile = await adminApi.GetProfileAsync();
                if (id != requestId || !isActiveAndEnabled) return;

                ApplyProfile(profile);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminProfilePanelController] Failed to load profile: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminProfilePanelController] Failed to load profile: {ex.Message}");
            }
        }

        private void ApplyProfile(AdminProfile profile)
        {
            if (profile == null) return;

            if (nameText != null)
                nameText.text = profile.fullName ?? string.Empty;

            if (emailText != null)
                emailText.text = profile.email ?? string.Empty;

            if (phoneText != null)
                phoneText.text = string.IsNullOrEmpty(profile.phone) ? string.Empty : profile.phone;

            if (profileImage != null)
            {
                if (string.IsNullOrEmpty(profile.profileImageUrl))
                {
                    profileImage.sprite = null;
                    return;
                }
                LoadImageAsync(profile.profileImageUrl);
            }
        }

        private async void LoadImageAsync(string url)
        {
            var sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);
            if (sprite != null && profileImage != null)
            {
                profileImage.sprite = sprite;
                profileImage.enabled = true;
            }
        }

        private void OnLogoutClicked()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.ClearSession();

            UnityEngine.SceneManagement.SceneManager.LoadScene(adminLoginScene);
        }
    }
}
