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
        [SerializeField] private TextMeshProUGUI _feedbackText;

        private IProfilePictureRepository _repository;
        private bool _isUploading;

        private void Awake()
        {
            if (_pickerView != null)
            {
                _pickerView.SelectionChanged += RefreshUpdateButtonState;
                _pickerView.SelectionFailed += ShowFeedback;
            }

            if (_updateButton != null)
                _updateButton.onClick.AddListener(HandleUpdateClicked);
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
        }

        private void OnEnable()
        {
            PanelOpened?.Invoke();
        }

        public void Initialize(IProfilePictureRepository repository, Sprite currentSprite)
        {
            _repository = repository;
            _isUploading = false;

            if (_pickerView != null)
            {
                _pickerView.SetInteractable(true);
                _pickerView.SetCurrentSprite(currentSprite);
            }

            RefreshUpdateButtonState();
            HideFeedback();
        }

        private void HandleUpdateClicked()
        {
            if (_pickerView == null || !_pickerView.HasSelection || _repository == null || _isUploading)
                return;

            _ = UploadAsync();
        }

        private async Task UploadAsync()
        {
            _isUploading = true;
            _pickerView.SetInteractable(false);
            if (_updateButton != null) _updateButton.interactable = false;

            bool success;
            try
            {
                success = await _repository.UpdateProfilePictureAsync(
                    _pickerView.SelectedImageBytes, _pickerView.SelectedImageFileName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                success = false;
            }

            if (this == null)
                return;

            if (success)
            {
                ProfilePictureUpdated?.Invoke();
                gameObject.SetActive(false);
            }
            else
            {
                _isUploading = false;
                ShowFeedback("Failed to update profile picture.");
                _pickerView.SetInteractable(true);
                RefreshUpdateButtonState();
            }
        }

        private void RefreshUpdateButtonState()
        {
            if (_updateButton != null)
                _updateButton.interactable = !_isUploading && _pickerView != null && _pickerView.HasSelection;
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
