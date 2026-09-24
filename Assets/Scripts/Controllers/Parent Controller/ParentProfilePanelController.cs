using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class ParentProfilePanelController : MonoBehaviour
    {
        private const string MaskedPassword = "••••••••";

        [Header("Profile Fields")]
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private TextMeshProUGUI emailText;
        [SerializeField] private TextMeshProUGUI passwordText;

        [Header("Logout")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private string loginScene = "Main Game/Parent/Parent Login";

        private ApiClient apiClient;
        private ParentApi parentApi;
        private int requestId;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);

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

        public void Refresh()
        {
            _ = RefreshAsync();
        }

        private async Awaitable RefreshAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[ParentProfilePanelController] No valid session, skipping profile load.");
                return;
            }

            int id = ++requestId;

            try
            {
                ParentProfile profile = await parentApi.GetProfileAsync();
                if (id != requestId || !isActiveAndEnabled) return;

                ApplyProfile(profile);
            }
            catch (ApiException ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentProfilePanelController] Failed to load profile: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentProfilePanelController] Failed to load profile: {ex.Message}");
            }
        }

        private void ApplyProfile(ParentProfile profile)
        {
            if (profile == null) return;

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(profile.fullName) ? profile.username : profile.fullName;

            if (userIdText != null)
                userIdText.text = string.IsNullOrEmpty(profile.id) ? "-" : profile.id;

            if (emailText != null)
                emailText.text = profile.email ?? string.Empty;

            // The API never returns the password, so it is always shown masked.
            if (passwordText != null)
                passwordText.text = MaskedPassword;

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

            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
        }
    }
}
