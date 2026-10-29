using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.Games.StorySequencing
{
    /// <summary>
    /// A single draggable event card in the Story Sequencing mini-game.
    /// Displays its event text and follows the pointer while dragged.
    /// Holds no reordering logic of its own — on drop it only reports
    /// itself and the drop screen position via <see cref="OnCardDropped"/>,
    /// leaving StorySequencingManager to decide where it ends up in the
    /// list.
    /// </summary>
    public class DraggableEventCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Fields

        [SerializeField] private TextMeshProUGUI _eventText;
        [Tooltip("The number badge shown on the card (e.g. \"1\", \"2\"), displaying " +
                 "its current position in the list. This component does not track " +
                 "its own position — it must be refreshed externally after any reorder.")]
        [SerializeField] private TextMeshProUGUI _positionNumberText;

        [Header("Result Marking")]
        [Tooltip("The card background that gets tinted green/red after Check Answer. " +
                 "Left empty, the first Image on this card is used.")]
        [SerializeField] private Graphic _resultGraphic;
        [SerializeField] private Color _correctColor = new Color(0.62f, 0.87f, 0.62f, 1f);
        [SerializeField] private Color _wrongColor = new Color(1f, 0.62f, 0.62f, 1f);
        [SerializeField] private float _resultTweenDuration = 0.25f;
        [SerializeField] private float _resultPunchStrength = 0.08f;

        private const float RESULT_PUNCH_DURATION = 0.35f;

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private Transform _originalParent;
        private Color _defaultResultColor = Color.white;
        private Vector3 _originalScale = Vector3.one;
        private Sequence _resultSequence;
        private bool _isDragEnabled = true;
        private bool _isDragging;

        #endregion

        #region Properties

        /// <summary>
        /// The event id this card represents. Set once via
        /// <see cref="Initialize"/> when the card is spawned; read-only
        /// from outside this class.
        /// </summary>
        public string CurrentEventId { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _parentCanvas = GetComponentInParent<Canvas>();
            _originalScale = transform.localScale;

            if (_resultGraphic == null)
            {
                _resultGraphic = GetComponentInChildren<Image>();
            }

            if (_resultGraphic != null)
            {
                _defaultResultColor = _resultGraphic.color;
            }
        }

        private void OnDestroy()
        {
            _resultSequence?.Kill();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Populates this card with its event id and display text. Call
        /// immediately after instantiating the prefab.
        /// </summary>
        /// <param name="eventId">The event id this card represents.</param>
        /// <param name="eventText">The sentence/event text to display.</param>
        public void Initialize(string eventId, string eventText)
        {
            CurrentEventId = eventId;

            if (_eventText != null)
            {
                _eventText.text = eventText;
            }
        }

        /// <summary>
        /// Updates the position number badge shown on this card. Called by
        /// StorySequencingManager after every reorder, since this component
        /// has no way to know its own list position on its own.
        /// </summary>
        /// <param name="displayNumber">The 1-indexed position to display.</param>
        public void SetPositionNumber(int displayNumber)
        {
            if (_positionNumberText != null)
            {
                _positionNumberText.text = displayNumber.ToString();
            }
        }

        /// <summary>
        /// Tints this card green (right place) or red (wrong place) with a small pop, so the
        /// player can see which sentences still need to move.
        /// </summary>
        /// <param name="isCorrect">True when the card sits in its correct position.</param>
        /// <param name="delay">Seconds to wait before the animation starts (used to stagger cards).</param>
        public void ShowResult(bool isCorrect, float delay = 0f)
        {
            if (_resultGraphic == null)
            {
                return;
            }

            KillResultSequence();

            Color target = isCorrect ? _correctColor : _wrongColor;

            _resultSequence = DOTween.Sequence().SetDelay(delay);
            _resultSequence.Append(_resultGraphic.DOColor(target, _resultTweenDuration).SetEase(Ease.InOutSine));
            _resultSequence.Join(transform.DOPunchScale(Vector3.one * _resultPunchStrength, RESULT_PUNCH_DURATION));
            _resultSequence.OnComplete(() => transform.localScale = _originalScale);
        }

        /// <summary>Removes the green/red marking and returns the card to its normal look.</summary>
        public void ClearResult()
        {
            KillResultSequence();

            if (_resultGraphic != null)
            {
                _resultGraphic.color = _defaultResultColor;
            }
        }

        /// <summary>Turns dragging on or off, e.g. once the correct order has been revealed.</summary>
        /// <param name="isEnabled">Whether the player may drag this card.</param>
        public void SetDragEnabled(bool isEnabled)
        {
            _isDragEnabled = isEnabled;
        }

        #endregion

        #region Private Methods

        private void KillResultSequence()
        {
            if (_resultSequence != null)
            {
                _resultSequence.Kill();
                _resultSequence = null;
            }

            transform.localScale = _originalScale;
        }

        private void FollowPointer(PointerEventData eventData)
        {
            if (_rectTransform == null || _parentCanvas == null)
            {
                return;
            }

            Camera eventCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _parentCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _parentCanvas.transform as RectTransform, eventData.position, eventCamera, out Vector3 worldPoint))
            {
                _rectTransform.position = worldPoint;
            }
        }

        #endregion

        #region Events / Callbacks

        /// <summary>
        /// Fired when the player releases this card after dragging it.
        /// Passes this card and the pointer's screen position at drop time
        /// so the manager can resolve the resulting list position.
        /// </summary>
        public event Action<DraggableEventCard, Vector2> OnCardDropped;

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isDragEnabled)
            {
                return;
            }

            _isDragging = true;
            _originalParent = transform.parent;

            // Reparent to the canvas root while dragging so the container's
            // Vertical Layout Group does not fight this component's manual
            // position updates in OnDrag(). This does not decide list order —
            // the card returns to _originalParent before OnCardDropped fires,
            // so the manager still resolves sibling order purely via
            // Transform.SetSiblingIndex().
            if (_parentCanvas != null)
            {
                transform.SetParent(_parentCanvas.transform, worldPositionStays: true);
                transform.SetAsLastSibling();
            }
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            FollowPointer(eventData);
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, worldPositionStays: true);
            }

            OnCardDropped?.Invoke(this, eventData.position);
        }

        #endregion
    }
}
