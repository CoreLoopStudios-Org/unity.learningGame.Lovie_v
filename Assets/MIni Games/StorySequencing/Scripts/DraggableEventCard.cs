using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private Transform _originalParent;

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

        #endregion

        #region Private Methods

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
            FollowPointer(eventData);
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, worldPositionStays: true);
            }

            OnCardDropped?.Invoke(this, eventData.position);
        }

        #endregion
    }
}
