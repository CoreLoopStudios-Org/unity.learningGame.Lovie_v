using UnityEngine;
using Api;
using Avatar;

namespace UI
{
    public class ChildSessionInitializer : MonoBehaviour
    {
        [Header("Initialization Order")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private float initializationDelay = 0.1f;

        private async void Start()
        {
            if (!autoInitializeOnStart)
                return;

            if (!SessionManager.Instance.IsChildSession)
                return;

            await new WaitForSeconds(initializationDelay);

            await InitializeAsync();
        }

        public async Awaitable InitializeAsync()
        {
            EnsureCoinWallet();
            await InitializeCoinWallet();
            await InitializeAvatarSync();
        }

        private void EnsureCoinWallet()
        {
            if (CoinWallet.Instance == null)
            {
                var go = new GameObject("CoinWallet");
                go.AddComponent<CoinWallet>();
            }
        }

        private async Awaitable InitializeCoinWallet()
        {
            if (CoinWallet.Instance != null)
            {
                await CoinWallet.Instance.RefreshAsync();
            }
        }

        private async Awaitable InitializeAvatarSync()
        {
            if (AvatarSyncService.Instance == null)
            {
                var go = new GameObject("AvatarSyncService");
                go.AddComponent<AvatarSyncService>();
            }

            if (AvatarSyncService.Instance != null)
            {
                var avatarManager = FindObjectOfType<AvatarCustomizationManager>();
                if (avatarManager != null)
                {
                    AvatarSyncService.Instance.SetAvatarManager(avatarManager);
                }

                await AvatarSyncService.Instance.LoadFromServerAsync();
            }
        }
    }
}
