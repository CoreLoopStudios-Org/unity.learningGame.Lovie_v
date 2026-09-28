using System;
using System.Threading.Tasks;
using Modules.Profile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ProfilePictureUploadPanel : MonoBehaviour
    {
        [SerializeField] private ProfilePicturePickerView _pickerView;
        [SerializeField] private Button _updateButton;
        [SerializeField] private ProfileUpdateConfirmView _confirmView;
        [SerializeField] private ProfileUploadStatusView _statusView;
        [SerializeField] private TextMeshProUGUI _feedbackText;

        private IProfilePictureRepository _repository;

        private void Awake()
        {
            if (_pickerView != null)
            {
                _pickerView.SelectionChanged += RefreshUpdateButtonState;
                _pickerView.SelectionFailed += ShowFeedback;
            }

            if (_updateButton != null)
                _updateButton.onClick.AddListener(HandleUpdateClicked);

            if (_confirmView != null)
                _confirmView.Confirmed += HandleConfirmConfirmed;

            RefreshUpdateButtonState();
        }

        private void OnEnable()
        {
            PanelOpened?.Invoke();
        }

        private void OnDestroy()
        {
            if (_pickerView != null)
            {
                _pickerView.SelectionChanged -= RefreshUpdateButtonState;
                _pickerView.SelectionFailed -= ShowFeedback;
            }

            if (_updateButton != null)
                _updateButton.onClick.RemoveListener(HandleUpdateClicked);

            if (_confirmView != null)
                _confirmView.Confirmed -= HandleConfirmConfirmed;
        }

        public void Initialize(IProfilePictureRepository repository, Sprite currentSprite)
        {
            _repository = repository;

            if (_pickerView != null)
                _pickerView.SetCurrentSprite(currentSprite);

            HideFeedback();
        }

        private void HandleUpdateClicked()
        {
            if (_pickerView == null || !_pickerView.HasSelection || _confirmView == null)
                return;

            _confirmView.Show();
        }

        private void HandleConfirmConfirmed()
        {
            _ = UploadAsync();
        }

        private async Task UploadAsync()
        {
            if (_pickerView == null || !_pickerView.HasSelection || _repository == null || _statusView == null)
                return;

            _pickerView.SetInteractable(false);
            if (_updateButton != null) _updateButton.interactable = false;
            await _statusView.ShowLoadingAsync();

            bool success = false;

            try
            {
                success = await _repository.UpdateProfilePictureAsync(
                    _pickerView.SelectedImageBytes, _pickerView.SelectedImageFileName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (this == null)
                return;

            if (success)
            {
                await _statusView.ShowSuccessAsync();
                ProfilePictureUpdated?.Invoke();
            }
            else
            {
                _statusView.Hide();
                ShowFeedback("Failed to update profile picture.");
            }

            if (this != null)
            {
                _pickerView.SetInteractable(true);
                RefreshUpdateButtonState();
            }
        }

        private void RefreshUpdateButtonState()
        {
            if (_updateButton != null)
                _updateButton.interactable = _pickerView != null && _pickerView.HasSelection;
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackText == null)
                return;

            _feedbackText.text = message;
            _feedbackText.gameObject.SetActive(true);
        }

        private void HideFeedback()
        {
            if (_feedbackText != null)
                _feedbackText.gameObject.SetActive(false);
        }

        public event Action ProfilePictureUpdated;
        public event Action PanelOpened;
    }
}
