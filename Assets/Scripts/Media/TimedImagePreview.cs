using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Media
{
    [RequireComponent(typeof(RawImage))]
    public sealed class TimedImagePreview : MonoBehaviour, IPointerClickHandler
    {
        #region Fields

        [SerializeField] private RawImage _image;

        private Sequence _sequence;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        #endregion

        #region Public Methods

        public void Show(Texture2D texture, ImageUploadConfigSO config)
        {
            if (_image == null || texture == null || config == null)
                return;

            _sequence?.Kill();
            gameObject.SetActive(true);

            _image.texture = texture;
            _image.uvRect = RawImageCropUtility.CalculateCoverUvRect(
                texture.width, texture.height, _image.rectTransform.rect.size);
            _image.color = new Color(1f, 1f, 1f, 0f);
            transform.localScale = Vector3.one * config.PreviewStartScale;

            float duration = config.PreviewAnimationDuration;
            _sequence = DOTween.Sequence()
                .Append(_image.DOFade(1f, duration).SetEase(Ease.InOutSine))
                .Join(transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack))
                .AppendInterval(config.PreviewVisibleSeconds)
                .Append(_image.DOFade(0f, duration).SetEase(Ease.InOutSine))
                .OnComplete(HideImmediately)
                .SetLink(gameObject);
        }

        public void HideImmediately()
        {
            _sequence?.Kill();
            _sequence = null;

            if (_image != null)
                _image.texture = null;

            gameObject.SetActive(false);
        }

        #endregion

        #region Events / Callbacks

        public void OnPointerClick(PointerEventData eventData)
        {
            HideImmediately();
        }

        #endregion
    }
}
