using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;
using Avatar;

namespace UI
{
    public class AdminKidUserCard : MonoBehaviour
    {
        [Serializable]
        private class ChildAdditionalData
        {
            public int level;
        }

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button banButton;
        [SerializeField] private Button deleteButton;

        [Header("Card Menu")]
        [SerializeField] private AdminCardMenu cardMenu;

        [Header("Avatar")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image dressImage;
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        private AdminChild currentChild;
        private System.Action<AdminChild> onBanAction;
        private System.Action<AdminChild> onDeleteAction;

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

        public void Setup(AdminChild child, System.Action<AdminChild> onBan = null, System.Action<AdminChild> onDelete = null)
        {
            currentChild = child;
            onBanAction = onBan;
            onDeleteAction = onDelete;

            if (nameText != null)
            {
                nameText.text = child?.username ?? string.Empty;
            }

            if (idText != null)
            {
                idText.text = child?.id ?? string.Empty;
            }

            if (levelText != null)
            {
                levelText.text = ExtractLevel(child?.additionalData);
            }

            // No defaults fallback: an admin list should not show identical default avatars for kids without one
            AvatarSlotRenderer.Apply(child?.avatarState, ResolveDatabase(), bodyImage, hairImage, dressImage, fallbackToDefaults: false);
        }

        private AvatarPartDatabase ResolveDatabase()
        {
            if (avatarDatabase != null)
                return avatarDatabase;

            var manager = FindObjectOfType<AvatarCustomizationManager>();
            return manager != null ? manager.Database : null;
        }

        private void OnBanClicked()
        {
            if (currentChild != null) onBanAction?.Invoke(currentChild);
            if (cardMenu != null) cardMenu.Close();
        }

        private void OnDeleteClicked()
        {
            if (currentChild != null) onDeleteAction?.Invoke(currentChild);
            if (cardMenu != null) cardMenu.Close();
        }

        private static string ExtractLevel(string additionalData)
        {
            if (string.IsNullOrWhiteSpace(additionalData)) return null;

            try
            {
                ChildAdditionalData data = JsonUtility.FromJson<ChildAdditionalData>(additionalData);
                return data != null && data.level > 0 ? data.level.ToString() : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
