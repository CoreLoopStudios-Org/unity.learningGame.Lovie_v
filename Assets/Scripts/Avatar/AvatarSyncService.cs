using UnityEngine;
using System;
using System.Collections.Generic;
using Api;
using Api.Endpoints;

namespace Avatar
{
    public class AvatarSyncService : MonoBehaviour
    {
        private static AvatarSyncService instance;
        public static AvatarSyncService Instance => instance;

        private AvatarCustomizationManager avatarManager;
        private bool isLoadingFromServer;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            avatarManager = FindObjectOfType<AvatarCustomizationManager>();
            if (avatarManager == null)
            {
                Debug.LogWarning("AvatarSyncService: No AvatarCustomizationManager found in scene.");
            }
        }

        public async Awaitable LoadFromServerAsync()
        {
            if (!SessionManager.Instance.IsChildSession)
                return;

            isLoadingFromServer = true;

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                var profile = await childApi.GetProfileAsync();

                // Server truth for the unlock-all purchase; overwrites any stale session flag.
                AvatarUnlockState.ReadFromAdditionalData(profile?.additionalData);

                // Refresh store purchases to populate unlocked avatar items
                if (StoreService.Instance != null)
                {
                    _ = StoreService.Instance.GetMyPurchasesAsync();
                }

                if (profile != null && !string.IsNullOrEmpty(profile.avatarState))
                {
                    ApplyAvatarState(profile.avatarState);
                    SaveToPlayerPrefs(profile.avatarState);
                }
                else
                {
                    if (avatarManager != null)
                    {
                        avatarManager.LoadAvatar();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load avatar from server: {e.Message}");
                if (avatarManager != null)
                {
                    avatarManager.LoadAvatar();
                }
            }
            finally
            {
                isLoadingFromServer = false;
            }
        }

        public async Awaitable SaveToServerAsync()
        {
            if (!SessionManager.Instance.IsChildSession)
                return;

            if (isLoadingFromServer)
                return;

            if (avatarManager == null)
                return;

            try
            {
                string avatarState = ExportAvatarState();

                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                bool success = await childApi.UpdateAvatarAsync(avatarState);

                if (success)
                {
                    SaveToPlayerPrefs(avatarState);
                }
                else
                {
                    Debug.LogWarning("Server rejected avatar update");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save avatar to server: {e.Message}");
            }
        }

        private const string UnlockedStateKey = "isUnlocked";

        // Backend stores this JSON as-is in the Child.AvatarState column.
        // Carries "isUnlocked": true once unlock-all is purchased so the flag
        // persists on the backend alongside additionalData.allAvatarsUnlocked.
        private string ExportAvatarState()
        {
            if (avatarManager == null)
                return "{}";

            var selections = avatarManager.GetAllSelections();
            var state = new Newtonsoft.Json.Linq.JObject();

            foreach (var category in selections)
            {
                if (category.Value != null)
                {
                    state[category.Key.ToString()] = category.Value.ItemId;
                }
            }

            if (AvatarUnlockState.AllUnlocked)
                state[UnlockedStateKey] = true;

            return state.ToString(Newtonsoft.Json.Formatting.None);
        }

        private void ApplyAvatarState(string avatarStateJson)
        {
            if (avatarManager == null || string.IsNullOrEmpty(avatarStateJson))
                return;

            try
            {
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(avatarStateJson);
                if (dict == null) return;

                // Newtonsoft coerces the JSON bool to "True" here — only ever sets the flag true.
                if (dict.TryGetValue(UnlockedStateKey, out string unlockedRaw) &&
                    bool.TryParse(unlockedRaw, out bool unlocked) && unlocked)
                {
                    AvatarUnlockState.SetAllUnlocked(true);
                }

                dict.Remove(UnlockedStateKey);

                avatarManager.ApplyServerState(dict);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to apply avatar state: {e.Message}");
            }
        }

        private void SaveToPlayerPrefs(string avatarStateJson)
        {
            PlayerPrefs.SetString("AvatarState_Server", avatarStateJson);
            PlayerPrefs.Save();
        }

        public void SetAvatarManager(AvatarCustomizationManager manager)
        {
            avatarManager = manager;
        }
    }
}
