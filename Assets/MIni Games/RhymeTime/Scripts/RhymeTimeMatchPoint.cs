using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreLoop.WordMatch
{
    [RequireComponent(typeof(Image))]
    public class RhymeTimeMatchPoint : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image dotImage;

        private RhymeTimeItem ownerItem;
        private RhymeTimeManager manager;

        public RectTransform RectTransform => transform as RectTransform;
        public RhymeTimeItem OwnerItem => ownerItem;

        public void Setup(RhymeTimeItem owner)
        {
            ownerItem = owner;
            manager = FindFirstObjectByType<RhymeTimeManager>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            manager.OnMatchPointDown(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            manager.OnMatchPointDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            manager.OnMatchPointUp(this, eventData);
        }

        public void SetConnected(bool connected)
        {
            if (dotImage != null)
                dotImage.color = connected ? Color.green : Color.white;
        }
    }
}