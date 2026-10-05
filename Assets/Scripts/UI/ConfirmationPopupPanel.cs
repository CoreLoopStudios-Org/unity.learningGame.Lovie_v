using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class ConfirmationPopupPanel : MonoBehaviour
    {
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text contextText;

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

        public void Setup(Action onConfirm, string context)
        {
            Setup(onConfirm);
            if (contextText != null)
                contextText.text = context;
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
