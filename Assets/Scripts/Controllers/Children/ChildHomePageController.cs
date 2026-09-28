using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
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
            if (bodyImage == null && hairImage == null && dressImage == null)
                return;

            var database = avatarDatabase != null ? avatarDatabase : FindAvatarDatabase();
            if (database == null)
            {
                Debug.LogWarning("[ChildHomePageController] No AvatarPartDatabase found — cannot render avatar.");
                return;
            }

            if (!string.IsNullOrEmpty(avatarStateJson))
            {
                Debug.Log($"[ChildHomePageController] avatarState: {avatarStateJson}");

                var state = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(avatarStateJson);
                if (state != null && state.Count > 0)
                {
                    ApplyPart(state, nameof(AvatarPartCategory.BodyColor), database, bodyImage);
                    ApplyPart(state, nameof(AvatarPartCategory.Hair), database, hairImage);
                    ApplyPart(state, nameof(AvatarPartCategory.Dress), database, dressImage);
                    return;
                }
            }

            // No avatar saved yet (new child) — fall back to default parts
            ApplyDefaultAvatar(database);
        }

        private void ApplyDefaultAvatar(AvatarPartDatabase database)
        {
            Debug.Log("[ChildHomePageController] No avatarState — applying default avatar.");
            SetSlot(database.GetDefaultPartForCategory(AvatarPartCategory.BodyColor), bodyImage);
            SetSlot(database.GetDefaultPartForCategory(AvatarPartCategory.Hair), hairImage);
            SetSlot(database.GetDefaultPartForCategory(AvatarPartCategory.Dress), dressImage);
        }

        private void SetSlot(AvatarPartItem part, Image target)
        {
            if (target == null)
                return;

            if (part != null && part.AvatarSprite != null)
            {
                target.sprite = part.AvatarSprite;
                target.enabled = true;
            }
            else
            {
                target.enabled = false;
            }
        }

        private void ApplyPart(Dictionary<string, string> state, string categoryKey, AvatarPartDatabase database, Image target)
        {
            if (target == null)
                return;

            if (state.TryGetValue(categoryKey, out string itemId))
            {
                var part = database.GetPartById(itemId);
                if (part != null && part.AvatarSprite != null)
                {
                    target.sprite = part.AvatarSprite;
                    target.enabled = true;
                    return;
                }

                Debug.LogWarning($"[ChildHomePageController] Could not resolve avatar part '{itemId}' for {categoryKey} — check the AvatarPartDatabase assignment.");
            }

            target.enabled = false;
        }

        private AvatarPartDatabase FindAvatarDatabase()
        {
            var manager = FindObjectOfType<AvatarCustomizationManager>();
            return manager != null ? manager.Database : null;
        }
    }
}
