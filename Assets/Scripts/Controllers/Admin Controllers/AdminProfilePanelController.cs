using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using Modules.Profile;

namespace UI
{
    public class AdminProfilePanelController : MonoBehaviour
    {
        private const string MaskedPassword = "••••••••";

        [Header("Profile Fields")]
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI emailText;
        [SerializeField] private TextMeshProUGUI passwordText;

        [Header("Logout")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private string adminLoginScene = "Admin Login";

        [Header("Profile Picture")]
        [SerializeField] private ProfilePictureUploadPanel profilePictureUploadPanel;

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

            if (profilePictureUploadPanel != null)
            {
                profilePictureUploadPanel.PanelOpened += OnProfilePicturePanelOpened;
                profilePictureUploadPanel.ProfilePictureUpdated += OnProfilePictureUpdated;
            }
        }

        private void OnDestroy()
        {
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);

            if (profilePictureUploadPanel != null)
            {
                profilePictureUploadPanel.PanelOpened -= OnProfilePicturePanelOpened;
                profilePictureUploadPanel.ProfilePictureUpdated -= OnProfilePictureUpdated;
            }
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        public void Refresh()
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

            // The API never returns the password, so it is always shown masked.
            if (passwordText != null)
                passwordText.text = MaskedPassword;

            if (profileImage != null)
            {
                if (string.IsNullOrEmpty(profile.profilePictureUrl))
                {
                    profileImage.sprite = null;
                    return;
                }
                LoadImageAsync(profile.profilePictureUrl);
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

        private void OnProfilePicturePanelOpened()
        {
            if (profilePictureUploadPanel == null) return;

            profilePictureUploadPanel.Initialize(new AdminProfilePictureRepository(adminApi), profileImage != null ? profileImage.sprite : null);
        }

        private void OnProfilePictureUpdated()
        {
            _ = RefreshAsync();
        }

        private void OnLogoutClicked()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.ClearSession();

            UnityEngine.SceneManagement.SceneManager.LoadScene(adminLoginScene);
        }
    }
}
