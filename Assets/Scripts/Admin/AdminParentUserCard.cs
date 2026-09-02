using UnityEngine;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminParentUserCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private UnityEngine.UI.Button banButton;
        [SerializeField] private UnityEngine.UI.Button deleteButton;

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
        }

        private void OnBanClicked()
        {
            if (currentUser != null) onBanAction?.Invoke(currentUser);
        }

        private void OnDeleteClicked()
        {
            if (currentUser != null) onDeleteAction?.Invoke(currentUser);
        }
    }
}
