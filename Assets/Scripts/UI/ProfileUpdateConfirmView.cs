using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ProfileUpdateConfirmView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _confirmButton;

        private void Awake()
        {
            if (_cancelButton != null) _cancelButton.onClick.AddListener(HandleCancelClicked);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(HandleConfirmClicked);

            Hide();
        }

        private void OnDestroy()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(HandleCancelClicked);
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public void Show()
        {
            if (_root != null) _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void HandleCancelClicked()
        {
            Hide();
            Cancelled?.Invoke();
        }

        private void HandleConfirmClicked()
        {
            Confirmed?.Invoke();
        }

        public event Action Cancelled;
        public event Action Confirmed;
    }
}
