using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;

namespace UI
{
    public class ChildDetailsCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image profilePicture;
        [SerializeField] private Button selectButton;

        private ChildListItem currentChild;
        private Action<ChildListItem> onSelect;

        private void Awake()
        {
            if (selectButton == null) selectButton = GetComponent<Button>();
            if (selectButton != null) selectButton.onClick.AddListener(OnSelectClicked);
        }

        private void OnDestroy()
        {
            if (selectButton != null) selectButton.onClick.RemoveListener(OnSelectClicked);
        }

        public void Setup(ChildListItem child, Action<ChildListItem> onSelect)
        {
            currentChild = child;
            this.onSelect = onSelect;

            if (nameText != null)
                nameText.text = child?.username ?? string.Empty;
        }

        private void OnSelectClicked()
        {
            if (currentChild != null) onSelect?.Invoke(currentChild);
        }
    }
}
