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
    public class ChildHomePageController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI streakText;

        [Header("Avatar")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image dressImage;
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        [Header("Behaviour")]
        [SerializeField] private bool refreshOnEnable = true;

        private bool isRefreshing;

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
                Debug.LogWarning("[ChildHomePageController] No child session — cannot load home page data.");
                return;
            }

            isRefreshing = true;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                ChildProfile profile = await childApi.GetProfileAsync();

                if (this == null)
                    return;

                if (profile == null)
                    return;

                if (nameText != null)
                    nameText.text = profile.username;

                if (coinText != null)
                    coinText.text = profile.coins.ToString();

                if (streakText != null)
                    streakText.text = profile.loginStreak.ToString();

                if (CoinWallet.Instance != null)
                {
                    CoinWallet.Instance.UpdateBalance(profile.coins);
                    CoinWallet.Instance.UpdateStreak(profile.loginStreak);
                }

                ApplyAvatarState(profile.avatarState);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ChildHomePageController] Failed to load profile: {e.Message}");
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private void ApplyAvatarState(string avatarStateJson)
        {
            AvatarSlotRenderer.Apply(avatarStateJson, ResolveDatabase(), bodyImage, hairImage, dressImage);
        }

        private AvatarPartDatabase ResolveDatabase()
        {
            if (avatarDatabase != null)
                return avatarDatabase;

            var manager = FindObjectOfType<AvatarCustomizationManager>();
            return manager != null ? manager.Database : null;
        }
    }
}
