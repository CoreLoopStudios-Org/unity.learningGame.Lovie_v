using UnityEngine;
using System;
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
                var apiClient = new ApiClient(ApiConfig.Instance);
                var childApi = new ChildApi(apiClient);

                var profile = await childApi.GetProfileAsync();

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

                var apiClient = new ApiClient(ApiConfig.Instance);
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

        private string ExportAvatarState()
        {
            if (avatarManager == null)
                return "{}";

            var selections = avatarManager.GetAllSelections();
            var state = new AvatarStateData();

            foreach (var kvp in selections)
            {
                string categoryKey = kvp.Key.ToString();
                state.partIds.Add(categoryKey, kvp.Value.ItemId);
            }

            return JsonUtility.ToJson(state);
        }

        private void ApplyAvatarState(string avatarStateJson)
        {
            if (avatarManager == null)
                return;

            try
            {
                var state = JsonUtility.FromJson<AvatarStateData>(avatarStateJson);

                avatarManager.ClearSavedData();

                foreach (var kvp in state.partIds)
                {
                    if (Enum.TryParse(kvp.Key, out AvatarPartCategory category))
                    {
                        var part = avatarManager.Database?.GetPartById(kvp.Value);
                        if (part != null)
                        {
                            avatarManager.SelectPartWithoutSave(category, part);
                        }
                    }
                }

                avatarManager.SaveAvatar();
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

        [Serializable]
        private class AvatarStateData
        {
            public System.Collections.Generic.Dictionary<string, string> partIds = new();
        }
    }
}
