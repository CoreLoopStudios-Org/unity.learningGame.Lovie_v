using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI
{
    /// <summary>
    /// Attach to close buttons inside panels.
    /// Closes the assigned panel with a reverse pop-up animation and destroys it.
    /// </summary>
    public class PanelCloser : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel to Close")]
        [SerializeField] private GameObject panelToClose;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.25f;
        [SerializeField] private float endScale = 0f;
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Options")]
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool findParentPanel = true;

        private bool isClosing = false;

        private void Awake()
        {
            // Auto-find parent panel if not assigned
            if (panelToClose == null && findParentPanel)
            {
                Transform parent = transform.parent;
                while (parent != null)
                {
                    RectTransform parentRect = parent.GetComponent<RectTransform>();
                    if (parentRect != null && parent.gameObject != transform.root.gameObject)
                    {
                        // Found a potential panel - use it
                        panelToClose = parent.gameObject;
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }

        private void Update()
        {
            if (!closeOnEscape || isClosing)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePanel();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanel();
            }
#endif
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ClosePanel();
        }

        /// <summary>
        /// Public method to close the panel (can be called from Button onClick).
        /// </summary>
        public void ClosePanel()
        {
            if (isClosing)
                return;

            if (panelToClose == null)
            {
                Debug.LogWarning("PanelCloser: No panel assigned to close!", this);
                return;
            }

            StartCoroutine(AnimateClose(panelToClose));
        }

        private System.Collections.IEnumerator AnimateClose(GameObject panel)
        {
            isClosing = true;

            // Get or add RectTransform
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogWarning("PanelCloser: Panel has no RectTransform!", panel);
                isClosing = false;
                yield break;
            }

            // Get or add CanvasGroup for fade effect
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            float startScaleValue = rectTransform.localScale.x;

            // Disable raycast during close animation to prevent further clicks
            CanvasGroup tempCanvasGroup = panel.GetComponent<CanvasGroup>();
            if (tempCanvasGroup == null)
            {
                tempCanvasGroup = panel.AddComponent<CanvasGroup>();
            }
            tempCanvasGroup.blocksRaycasts = false;

            while (elapsed < animationDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / animationDuration);
                float easedProgress = EaseInBack(progress);

                // Scale animation: 1 -> 0
                float currentScale = Mathf.Lerp(startScaleValue, endScale, easedProgress);
                rectTransform.localScale = Vector3.one * currentScale;

                // Fade animation: 1 -> 0
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

                yield return null;
            }

            // Final values
            rectTransform.localScale = Vector3.one * endScale;
            canvasGroup.alpha = 0f;

            // Destroy the panel
            Destroy(panel);
            isClosing = false;
        }

        private float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        /// <summary>
        /// Set the panel to close programmatically.
        /// </summary>
        public void SetPanelToClose(GameObject panel)
        {
            panelToClose = panel;
        }

        /// <summary>
        /// Set whether the panel closes on Escape key press.
        /// </summary>
        public void SetCloseOnEscape(bool value)
        {
            closeOnEscape = value;
        }
    }
}
