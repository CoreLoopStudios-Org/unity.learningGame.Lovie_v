using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreLoop.WordMatch
{
    [RequireComponent(typeof(Image))]
    public class MatchPoint : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image dotImage;
        
        private WordMatchItem ownerItem;
        private WordMatchManager manager;

        public RectTransform RectTransform => transform as RectTransform;
        public WordMatchItem OwnerItem => ownerItem;

        public void Setup(WordMatchItem owner)
        {
            ownerItem = owner;
            manager = FindFirstObjectByType<WordMatchManager>();
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
