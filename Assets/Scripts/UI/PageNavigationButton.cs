using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Attach to any button that should navigate to a specific page.
    /// Works with AdminNavigationController to change pages and update nav bar colors.
    /// </summary>
    public class PageNavigationButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Navigation Settings")]
        [SerializeField] private AdminPageType targetPage;
        [SerializeField] private AdminNavigationController navigationController;

        [Header("Options")]
        [SerializeField] private bool findNavControllerAutomatically = true;

        private void Awake()
        {
            EnsureRaycastTarget();

            // Auto-find AdminNavigationController if not assigned
            if (navigationController == null && findNavControllerAutomatically)
            {
                navigationController = FindObjectOfType<AdminNavigationController>();
            }
        }

        // A click handler only receives taps on a Graphic that raycasts. A button made of just
        // text/icons would only react on the glyphs, so add an invisible full-area Image.
        private void EnsureRaycastTarget()
        {
            if (GetComponent<Graphic>() != null)
            {
                return;
            }

            var hitArea = gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            NavigateToPage();
        }

        /// <summary>
        /// Public method that can be called from Button onClick event.
        /// </summary>
        public void NavigateToPage()
        {
            if (navigationController == null)
            {
                Debug.LogWarning("PageNavigationButton: No AdminNavigationController assigned or found!", this);
                return;
            }

            navigationController.ShowPage(targetPage);
        }

        /// <summary>
        /// Set the target page type programmatically.
        /// </summary>
        public void SetTargetPage(AdminPageType pageType)
        {
            targetPage = pageType;
        }

        /// <summary>
        /// Set the AdminNavigationController reference programmatically.
        /// </summary>
        public void SetNavigationController(AdminNavigationController controller)
        {
            navigationController = controller;
        }
    }
}
