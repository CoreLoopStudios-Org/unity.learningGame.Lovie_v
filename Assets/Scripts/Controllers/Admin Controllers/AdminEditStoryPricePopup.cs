using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminEditStoryPricePopup : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private Button updateButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI errorText;

        public Story Story { get; private set; }

        // The panel subscribes and performs the API call — the popup stays a dumb view.
        public event Action<AdminEditStoryPricePopup, int> UpdateConfirmed;
        public event Action<AdminEditStoryPricePopup> Dismissed;

        private void Awake()
        {
            if (updateButton != null) updateButton.onClick.AddListener(OnUpdateClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            if (updateButton != null) updateButton.onClick.RemoveListener(OnUpdateClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        public void Setup(Story story)
        {
            Story = story;

            if (errorText != null) errorText.gameObject.SetActive(false);

            if (amountInput != null)
                amountInput.text = (story?.priceInCoins ?? 0).ToString(CultureInfo.InvariantCulture);
        }

        // Amount-only setup for non-story reuse (e.g. IAP coin tiers).
        public void Setup(int currentAmount)
        {
            Story = null;

            if (errorText != null) errorText.gameObject.SetActive(false);

            if (amountInput != null)
                amountInput.text = currentAmount.ToString(CultureInfo.InvariantCulture);
        }

        public void SetInteractable(bool interactable)
        {
            if (amountInput != null) amountInput.interactable = interactable;
            if (updateButton != null) updateButton.interactable = interactable;
            if (cancelButton != null) cancelButton.interactable = interactable;
        }

        public void ShowError(string message)
        {
            if (errorText == null) return;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        private void OnUpdateClicked()
        {
            string raw = amountInput != null ? amountInput.text.Trim() : string.Empty;

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int price) || price < 0)
            {
                ShowError("Price must be a whole number of 0 or more.");
                return;
            }

            UpdateConfirmed?.Invoke(this, price);
        }

        private void OnCancelClicked()
        {
            Dismissed?.Invoke(this);
        }
    }
}
