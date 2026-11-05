using UnityEngine;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class AdminParentUserCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private UnityEngine.UI.Image profileImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private UnityEngine.UI.Button banButton;
        [SerializeField] private UnityEngine.UI.Button deleteButton;

        [Header("Card Menu")]
        [SerializeField] private AdminCardMenu cardMenu;

        private UserSummary currentUser;
        private System.Action<UserSummary> onBanAction;
        private System.Action<UserSummary> onDeleteAction;

        private void Awake()
        {
            if (banButton != null) banButton.onClick.AddListener(OnBanClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        private void OnDestroy()
        {
            if (banButton != null) banButton.onClick.RemoveListener(OnBanClicked);
            if (deleteButton != null) deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }

        public void Setup(UserSummary user, System.Action<UserSummary> onBan = null, System.Action<UserSummary> onDelete = null)
        {
            currentUser = user;
            onBanAction = onBan;
            onDeleteAction = onDelete;

            if (nameText != null)
            {
                nameText.text = user?.fullName ?? string.Empty;
            }

            if (idText != null)
            {
                idText.text = user?.id ?? string.Empty;
            }

            ApplyProfilePicture(user?.profilePictureUrl);
        }

        private void ApplyProfilePicture(string url)
        {
            if (profileImage == null) return;

            if (string.IsNullOrEmpty(url))
            {
                profileImage.sprite = null;
                return;
            }

            _ = LoadProfileImageAsync(url);
        }

        private async Awaitable LoadProfileImageAsync(string url)
        {
            Sprite sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);

            // The card can be destroyed while the download is in flight.
            if (sprite != null && profileImage != null)
                profileImage.sprite = sprite;
        }

        private void OnBanClicked()
        {
            if (currentUser != null) onBanAction?.Invoke(currentUser);
            if (cardMenu != null) cardMenu.Close();
        }

        private void OnDeleteClicked()
        {
            if (currentUser != null) onDeleteAction?.Invoke(currentUser);
            if (cardMenu != null) cardMenu.Close();
        }
    }
}
