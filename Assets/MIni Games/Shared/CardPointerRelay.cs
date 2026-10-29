using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modules.GameFramework.UI
{
    /// <summary>
    /// Lets a whole card act as a big touch target for a drag that really belongs to a
    /// smaller element (for example the tiny connection dot on a matching card).
    /// It holds no game logic: it only forwards pointer down, drag and up events to the
    /// callbacks it was bound to, so any game can reuse it.
    /// </summary>
    public class CardPointerRelay : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        #region Fields

        private Action<PointerEventData> _onDown;
        private Action<PointerEventData> _onDrag;
        private Action<PointerEventData> _onUp;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the callbacks that receive this card's pointer events. Call again to replace them.
        /// </summary>
        /// <param name="onDown">Called when a touch or click starts on the card.</param>
        /// <param name="onDrag">Called while the pointer moves with the touch held.</param>
        /// <param name="onUp">Called when the touch or click is released.</param>
        public void Bind(Action<PointerEventData> onDown, Action<PointerEventData> onDrag, Action<PointerEventData> onUp)
        {
            _onDown = onDown;
            _onDrag = onDrag;
            _onUp = onUp;
        }

        #endregion

        #region Events / Callbacks

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData)
        {
            _onDown?.Invoke(eventData);
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            _onDrag?.Invoke(eventData);
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData)
        {
            _onUp?.Invoke(eventData);
        }

        #endregion
    }
}
