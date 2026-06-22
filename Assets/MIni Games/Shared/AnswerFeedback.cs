using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameFramework.UI
{
    /// <summary>
    /// Shared animation component for quiz-style answer feedback.
    /// Handles the "pop" tap animation, the correct-answer settle, and the
    /// wrong-answer shake + red color tween + haptic feedback.
    /// Attach this to the same GameObject as <see cref="AnswerOptionView"/>.
    /// This component contains ZERO game-specific logic and must be reused
    /// by every quiz-style mini-game (Story Quest, future quiz games, etc.).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AnswerFeedback : MonoBehaviour
    {
        #region Fields

        [SerializeField] private RectTransform _targetTransform;
        [SerializeField] private Graphic _colorTarget;

        [SerializeField] private float _popDuration = 0.2f;
        [SerializeField] private float _popScale = 1.08f;

        [SerializeField] private float _shakeDuration = 0.35f;
        [SerializeField] private float _shakeStrength = 12f;
        [SerializeField] private int _shakeVibrato = 18;

        [SerializeField] private Color _wrongTintColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float _wrongTintDuration = 0.4f;

        private Color _originalColor;
        private Vector3 _originalScale;
        private Sequence _activeSequence;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_targetTransform == null)
            {
                _targetTransform = GetComponent<RectTransform>();
            }

            _originalScale = _targetTransform.localScale;

            if (_colorTarget != null)
            {
                _originalColor = _colorTarget.color;
            }
        }

        private void OnDisable()
        {
            KillActiveSequence();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays the immediate tap "pop" feedback. Should be called on every
        /// tap regardless of whether the answer is correct or wrong.
        /// </summary>
        public void PlayTapPop(Action onComplete = null)
        {
            KillActiveSequence();

            _targetTransform.localScale = _originalScale;

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(_targetTransform.DOScale(_originalScale * _popScale, _popDuration * 0.5f).SetEase(Ease.OutBack));
            _activeSequence.Append(_targetTransform.DOScale(_originalScale, _popDuration * 0.5f).SetEase(Ease.InOutSine));
            _activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Plays the correct-answer feedback. Sprite swap is handled by the
        /// caller (<see cref="AnswerOptionView"/>); this only handles motion/color.
        /// </summary>
        public void PlayCorrect(Action onComplete = null)
        {
            KillActiveSequence();

            _targetTransform.localScale = _originalScale;

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(_targetTransform.DOScale(_originalScale * _popScale, 0.15f).SetEase(Ease.OutBack));
            _activeSequence.Append(_targetTransform.DOScale(_originalScale, 0.2f).SetEase(Ease.OutCubic));
            _activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Plays the wrong-answer feedback: shake, red color tween, and a
        /// light haptic vibration so the player feels the mistake.
        /// </summary>
        public void PlayWrong(Action onComplete = null)
        {
            KillActiveSequence();

            _targetTransform.localPosition = Vector3.zero;
            TriggerHapticFeedback();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(_targetTransform.DOShakePosition(_shakeDuration, _shakeStrength, _shakeVibrato));

            if (_colorTarget != null)
            {
                _colorTarget.color = _originalColor;
                _activeSequence.Join(_colorTarget.DOColor(_wrongTintColor, _wrongTintDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
            }

            _activeSequence.OnComplete(() =>
            {
                if (_colorTarget != null)
                {
                    _colorTarget.color = _originalColor;
                }

                _targetTransform.localPosition = Vector3.zero;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Immediately resets scale, position, and color to their original
        /// state. Call when a question card is reused/repooled.
        /// </summary>
        public void ResetVisualState()
        {
            KillActiveSequence();

            _targetTransform.localScale = _originalScale;
            _targetTransform.localPosition = Vector3.zero;

            if (_colorTarget != null)
            {
                _colorTarget.color = _originalColor;
            }
        }

        #endregion

        #region Private Methods

        private void TriggerHapticFeedback()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private void KillActiveSequence()
        {
            if (_activeSequence != null && _activeSequence.IsActive())
            {
                _activeSequence.Kill();
            }

            _activeSequence = null;
        }

        #endregion
    }
}