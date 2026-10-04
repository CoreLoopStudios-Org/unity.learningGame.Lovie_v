using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Api;
using Api.Endpoints;
using Api.Models;
using Avatar;

namespace UI
{
    public class ChildProfilePanelController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI storiesReadText;
        [SerializeField] private TextMeshProUGUI gamesPlayedText;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Avatar")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image dressImage;
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        [Header("Logout")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private string loginScene = "Login";

        [Header("Behaviour")]
        [SerializeField] private bool refreshOnEnable = true;

        private bool isRefreshing;

        private void Awake()
        {
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void OnDestroy()
        {
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        private void OnLogoutClicked()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.ClearSession();

            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
        }

        private void OnEnable()
        {
            if (refreshOnEnable)
            {
                Refresh();
            }
        }

        public async void Refresh()
        {
            if (isRefreshing)
                return;

            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[ChildProfilePanelController] No child session — cannot load profile.");
                return;
            }

            isRefreshing = true;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                ChildProfile profile = await childApi.GetProfileAsync();
                ChildStats stats = await childApi.GetStatsAsync();

                if (this == null)
                    return;

                if (profile != null)
                {
                    if (nameText != null)
                        nameText.text = profile.username;

                    if (levelText != null)
                        levelText.text = ParseLevel(profile.additionalData);

                    AvatarSlotRenderer.Apply(profile.avatarState, ResolveDatabase(), bodyImage, hairImage, dressImage);
                }

                if (stats != null)
                {
                    if (storiesReadText != null)
                        storiesReadText.text = stats.storiesRead.ToString();

                    if (gamesPlayedText != null)
                        gamesPlayedText.text = stats.gamesPlayed.ToString();

                    if (coinsText != null)
                        coinsText.text = stats.coins.ToString();

                    if (CoinWallet.Instance != null)
                    {
                        CoinWallet.Instance.UpdateBalance(stats.coins);
                        CoinWallet.Instance.UpdateStreak(stats.loginStreak);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ChildProfilePanelController] Failed to load profile: {e.Message}");
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private AvatarPartDatabase ResolveDatabase()
        {
            if (avatarDatabase != null)
                return avatarDatabase;

            var manager = FindObjectOfType<AvatarCustomizationManager>();
            return manager != null ? manager.Database : null;
        }

        [Serializable]
        private class ChildAdditionalData
        {
            public int level;
        }

        private static string ParseLevel(string additionalData)
        {
            if (string.IsNullOrWhiteSpace(additionalData))
                return "-";

            try
            {
                var data = JsonUtility.FromJson<ChildAdditionalData>(additionalData);
                return data != null && data.level > 0 ? data.level.ToString() : "-";
            }
            catch
            {
                return "-";
            }
        }
    }
}
