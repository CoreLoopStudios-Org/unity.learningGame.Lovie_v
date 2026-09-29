using System;
using System.Threading.Tasks;
using Media;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class StoryBackgroundImageSelector : MonoBehaviour
    {
        #region Fields

        private const string PERMISSION_DENIED_MESSAGE = "Photo access is off. Allow it in the device Settings to choose an image.";
        private const string UNEXPECTED_ERROR_MESSAGE = "Something went wrong while loading the image. Please try again.";

        [SerializeField] private ImageUploadConfigSO _config;
        [SerializeField] private Button _button;
        [SerializeField] private TimedImagePreview _preview;

        private IImagePicker _picker;
        private ImageProcessor _processor;
        private ProcessedImage _selectedImage;
        private bool _isBusy;
        private bool _isInteractable = true;

        #endregion

        #region Properties

        public bool HasImage => _selectedImage != null;

        public byte[] ImageBytes => _selectedImage?.JpegBytes;

        public string FileName => _selectedImage?.FileName;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _button == null || _preview == null)
            {
                Debug.LogError($"[{nameof(StoryBackgroundImageSelector)}] Config, Button and Preview must be assigned.", this);
                enabled = false;
                return;
            }

            _picker = new NativeGalleryImagePicker();
            _processor = new ImageProcessor(_config);
            _button.onClick.AddListener(HandleButtonClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleButtonClicked);

            ReleaseSelectedImage();
        }

        #endregion

        #region Public Methods

        public void Clear()
        {
            if (_preview != null)
                _preview.HideImmediately();

            ReleaseSelectedImage();
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
            RefreshButtonState();
        }

        #endregion

        #region Private Methods

        private void HandleButtonClicked()
        {
            if (!enabled || _isBusy || _picker.IsBusy)
                return;

            _ = SelectImageAsync();
        }

        private async Task SelectImageAsync()
        {
            SetBusy(true);

            try
            {
                ImagePickResult pickResult = await _picker.PickImageAsync(_config.PickerTitle);
                if (this == null || !HandlePickResult(pickResult))
                    return;

                ProcessedImage processedImage = await _processor.ProcessAsync(pickResult.FilePath);
                if (this == null)
                {
                    processedImage.Dispose();
                    return;
                }

                ApplySelection(processedImage);
            }
            catch (ImageProcessingException exception)
            {
                RaiseSelectionFailed(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                RaiseSelectionFailed(UNEXPECTED_ERROR_MESSAGE);
            }
            finally
            {
                if (this != null)
                    SetBusy(false);
            }
        }

        private bool HandlePickResult(ImagePickResult pickResult)
        {
            if (pickResult.Status == ImagePickStatus.PermissionDenied)
                RaiseSelectionFailed(PERMISSION_DENIED_MESSAGE);

            return pickResult.IsPicked;
        }

        private void ApplySelection(ProcessedImage processedImage)
        {
            _preview.HideImmediately();
            ReleaseSelectedImage();
            _selectedImage = processedImage;
            _preview.Show(processedImage.PreviewTexture, _config);
            OnImageSelected?.Invoke();
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            RefreshButtonState();
        }

        private void RefreshButtonState()
        {
            if (_button != null)
                _button.interactable = _isInteractable && !_isBusy;
        }

        private void ReleaseSelectedImage()
        {
            _selectedImage?.Dispose();
            _selectedImage = null;
        }

        private void RaiseSelectionFailed(string message)
        {
            if (this != null)
                OnSelectionFailed?.Invoke(message);
        }

        #endregion

        #region Events / Callbacks

        public event Action OnImageSelected;

        public event Action<string> OnSelectionFailed;

        #endregion
    }
}
