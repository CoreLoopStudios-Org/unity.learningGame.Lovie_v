using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ConfirmationPopupPanel : MonoBehaviour
    {
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
        }

        public void Setup(Action onConfirm)
        {
            this.onConfirm = onConfirm;
        }

        private void OnConfirmClicked()
        {
            onConfirm?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
