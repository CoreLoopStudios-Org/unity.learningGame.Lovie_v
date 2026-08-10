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
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float endScale = 0f;
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Options")]
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool findParentPanel = true;

        private bool isClosing = false;

        private void Awake()
        {
            if (panelToClose == null && findParentPanel)
            {
                Transform parent = transform.parent;
                while (parent != null)
                {
                    RectTransform parentRect = parent.GetComponent<RectTransform>();
                    if (parentRect != null && parent.gameObject != transform.root.gameObject)
                    {
                        panelToClose = parent.gameObject;
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }

        private void Update()
        {
            if (!closeOnEscape || isClosing) return;

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

        public void ClosePanel()
        {
            if (isClosing) return;
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

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogWarning("PanelCloser: Panel has no RectTransform!", panel);
                isClosing = false;
                yield break;
            }

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startScaleValue = rectTransform.localScale.x;

            while (elapsed < animationDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float curvedT = animationCurve.Evaluate(t);

                float currentScale = Mathf.Lerp(startScaleValue, endScale, curvedT);
                rectTransform.localScale = Vector3.one * currentScale;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }

            rectTransform.localScale = Vector3.one * endScale;
            canvasGroup.alpha = 0f;

            Destroy(panel);
            isClosing = false;
        }

        public void SetPanelToClose(GameObject panel)
        {
            panelToClose = panel;
        }

        public void SetCloseOnEscape(bool value)
        {
            closeOnEscape = value;
        }
    }
}
