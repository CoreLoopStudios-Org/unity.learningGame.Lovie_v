using System;
using UnityEngine;
using TMPro;
using Api.Models;

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
        [SerializeField] private UnityEngine.UI.Button banButton;
        [SerializeField] private UnityEngine.UI.Button deleteButton;

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
        }

        private void OnBanClicked()
        {
            if (currentChild != null) onBanAction?.Invoke(currentChild);
        }

        private void OnDeleteClicked()
        {
            if (currentChild != null) onDeleteAction?.Invoke(currentChild);
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
