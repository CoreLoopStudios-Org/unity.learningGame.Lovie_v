using UnityEngine;

namespace Media
{
    [CreateAssetMenu(fileName = "SO_ImageUploadConfig", menuName = "Lovie/Media/Image Upload Config")]
    public sealed class ImageUploadConfigSO : ScriptableObject
    {
        #region Fields

        [Header("Output")]
        [Tooltip("Final image width in pixels. The picked photo is centre-cropped to Output Width : Output Height.")]
        [SerializeField, Range(64, 4096)] private int _outputWidth = 1080;

        [Tooltip("Final image height in pixels. Story backgrounds are 1080 x 2160 (1:2 portrait).")]
        [SerializeField, Range(64, 4096)] private int _outputHeight = 2160;

        [Tooltip("JPEG quality (1-100). 80 is the usual size/quality sweet spot.")]
        [SerializeField, Range(1, 100)] private int _jpegQuality = 80;

        [Tooltip("Processed images larger than this are rejected before upload.")]
        [SerializeField, Min(1)] private int _maxUploadSizeMegabytes = 5;

        [Tooltip("Decode cap used when the photo's dimensions can't be read in advance (Editor).")]
        [SerializeField, Range(1024, 8192)] private int _fallbackDecodeLimit = 4096;

        [Header("Picker")]
        [Tooltip("Title shown on the Android picker.")]
        [SerializeField] private string _pickerTitle = "Select story background";

        [Tooltip("Prefix of the uploaded file name. A unique id and .jpg are appended.")]
        [SerializeField] private string _uploadFileNamePrefix = "story_background";

        [Header("Preview")]
        [Tooltip("Seconds the preview stays fully visible after a pick.")]
        [SerializeField, Min(0f)] private float _previewVisibleSeconds = 3f;

        [Tooltip("Preview pop-in / fade-out duration (UI pop-in standard: 0.15-0.25s).")]
        [SerializeField, Range(0f, 1f)] private float _previewAnimationDuration = 0.25f;

        [Tooltip("Scale the preview starts from before popping to full size.")]
        [SerializeField, Range(0.5f, 1f)] private float _previewStartScale = 0.92f;

        #endregion

        #region Properties

        public int OutputWidth => _outputWidth;

        public int OutputHeight => _outputHeight;

        public float OutputAspect => (float)_outputWidth / _outputHeight;

        public int JpegQuality => _jpegQuality;

        public int MaxUploadSizeMegabytes => _maxUploadSizeMegabytes;

        public int MaxUploadBytes => _maxUploadSizeMegabytes * MediaConstants.BYTES_PER_MEGABYTE;

        public int FallbackDecodeLimit => _fallbackDecodeLimit;

        public string PickerTitle => _pickerTitle;

        public string UploadFileNamePrefix => _uploadFileNamePrefix;

        public float PreviewVisibleSeconds => _previewVisibleSeconds;

        public float PreviewAnimationDuration => _previewAnimationDuration;

        public float PreviewStartScale => _previewStartScale;

        #endregion
    }
}
