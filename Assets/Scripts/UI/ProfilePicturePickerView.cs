using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Media;

namespace Modules.Profile
{
    public sealed class ProfilePicturePickerView : MonoBehaviour
    {
        #region Fields

        [SerializeField] private ImageUploadConfigSO _config;
        [SerializeField] private Image _previewImage;
        [SerializeField] private GameObject _previewMask;
        [SerializeField] private Button _pickButton;

        private IImagePicker _picker;
        private ImageProcessor _imageProcessor;
        private byte[] _selectedImageBytes;
        private string _selectedImageFileName;

        #endregion

        #region Properties

        public bool HasSelection => _selectedImageBytes != null;
        public byte[] SelectedImageBytes => _selectedImageBytes;
        public string SelectedImageFileName => _selectedImageFileName;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _picker = new NativeGalleryImagePicker();
            _imageProcessor = new ImageProcessor(_config);
            SetMaskActive(false);

            if (_pickButton != null)
            {
                _pickButton.onClick.AddListener(HandlePickClicked);
            }
        }

        private void OnDestroy()
        {
            if (_pickButton != null)
            {
                _pickButton.onClick.RemoveListener(HandlePickClicked);
            }
        }

        #endregion

        #region Public Methods

        public void SetCurrentSprite(Sprite sprite)
        {
            _selectedImageBytes = null;
            _selectedImageFileName = null;

            if (_previewImage != null)
            {
                _previewImage.sprite = sprite;
            }

            SetMaskActive(sprite != null);
        }

        public void SetInteractable(bool interactable)
        {
            if (_pickButton != null)
            {
                _pickButton.interactable = interactable;
            }
        }

        #endregion

        #region Private Methods

        private async void HandlePickClicked()
        {
            if (_picker == null || _picker.IsBusy || _config == null)
            {
                return;
            }

            ImagePickResult pickResult = await _picker.PickImageAsync(_config.PickerTitle);

            if (!pickResult.IsPicked)
            {
                if (pickResult.Status == ImagePickStatus.PermissionDenied)
                {
                    SelectionFailed?.Invoke("Permission denied. Please allow photo access to select an image.");
                }

                return;
            }

            await ProcessPickedImageAsync(pickResult.FilePath);
        }

        private async Task ProcessPickedImageAsync(string filePath)
        {
            try
            {
                ProcessedImage processedImage = await _imageProcessor.ProcessAsync(filePath);

                _selectedImageBytes = processedImage.JpegBytes;
                _selectedImageFileName = processedImage.FileName;

                ApplyPreviewTexture(processedImage.PreviewTexture);

                processedImage.Dispose();

                SelectionChanged?.Invoke();
            }
            catch (ImageProcessingException exception)
            {
                SelectionFailed?.Invoke(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SelectionFailed?.Invoke("Something went wrong while processing that photo.");
            }
        }

        private void ApplyPreviewTexture(Texture2D texture)
        {
            if (_previewImage == null || texture == null)
            {
                Debug.LogWarning("[ProfilePicturePickerView] Preview image or processed texture was null.", this);
                return;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _previewImage.sprite = sprite;
            SetMaskActive(true);
        }

        private void SetMaskActive(bool isActive)
        {
            if (_previewMask != null)
            {
                _previewMask.SetActive(isActive);
            }
        }

        #endregion

        #region Events / Callbacks

        public event Action SelectionChanged;
        public event Action<string> SelectionFailed;

        #endregion
    }
}
