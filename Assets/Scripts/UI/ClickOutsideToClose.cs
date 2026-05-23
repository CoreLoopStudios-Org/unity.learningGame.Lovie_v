using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class ClickOutsideToClose : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private GameObject panelToClose;

        [Header("Settings")]
        [SerializeField] private bool closeOnBackgroundClick = true;

        private void Awake()
        {
            // If not assigned, use this GameObject
            if (panelToClose == null)
            {
                panelToClose = gameObject;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!closeOnBackgroundClick)
                return;

            // Check if click was outside the panel content
            RectTransform panelRect = panelToClose.transform as RectTransform;
            if (panelRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelRect, eventData.pressPosition, eventData.pressEventCamera, out localPoint))
                {
                    // If click is outside the rect, close the panel
                    if (!panelRect.rect.Contains(localPoint))
                    {
                        ClosePanel();
                    }
                }
            }
        }

        public void ClosePanel()
        {
            PanelCloser closer = GetComponent<PanelCloser>();
            if (closer != null)
            {
                closer.ClosePanel();
            }
            else
            {
                Destroy(panelToClose);
            }
        }
    }
}
