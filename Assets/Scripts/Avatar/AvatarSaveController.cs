using UnityEngine;
using UnityEngine.UI;

namespace Avatar
{
    public class AvatarSaveController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarCustomizationManager avatarManager;
        [SerializeField] private Button saveButton;

        [Header("Settings")]
        [SerializeField] private bool autoSyncToServer = true;

        private void OnEnable()
        {
            if (avatarManager != null)
            {
                avatarManager.OnAvatarSaved += HandleAvatarSaved;
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(HandleSaveButton);
            }
        }

        private void OnDisable()
        {
            if (avatarManager != null)
            {
                avatarManager.OnAvatarSaved -= HandleAvatarSaved;
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(HandleSaveButton);
            }
        }

        private void HandleAvatarSaved()
        {
            if (autoSyncToServer)
            {
                SyncToServer();
            }
        }

        private void HandleSaveButton()
        {
            if (avatarManager != null)
            {
                avatarManager.SaveAvatar();
            }
        }

        private async void SyncToServer()
        {
            if (AvatarSyncService.Instance != null)
            {
                await AvatarSyncService.Instance.SaveToServerAsync();
            }
        }

        public void SetAvatarManager(AvatarCustomizationManager manager)
        {
            avatarManager = manager;
        }
    }
}
