using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class ClickOutsideToClose : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float closeDuration = 0.2f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool isClosing = false;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Update()
        {
            if (isClosing) return;

            // Check for left click (Input System)
            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

                // Check if click is outside this panel
                if (!IsClickOverPanel(mousePos))
                {
                    Close();
                }
            }
        }

        private bool IsClickOverPanel(Vector2 screenPos)
        {
            if (rectTransform == null) return false;

            // Get the canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas?.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

            // Convert screen point to local point
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                screenPos,
                camera,
                out localPoint))
            {
                return rectTransform.rect.Contains(localPoint);
            }

            return false;
        }

        public void Close()
        {
            if (isClosing) return;
            StartCoroutine(CloseAnimation());
        }

        private System.Collections.IEnumerator CloseAnimation()
        {
            isClosing = true;

            // Disable interaction
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            float elapsed = 0f;
            Vector3 startScale = rectTransform.localScale;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / closeDuration;

                // Fade and scale
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                }
                rectTransform.localScale = startScale * (1f - t);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
