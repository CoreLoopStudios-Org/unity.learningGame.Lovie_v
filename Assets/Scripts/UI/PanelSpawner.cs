using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Attach to buttons that should open panels (like profile, settings buttons).
    /// Spawns the assigned panel prefab with a pop-up animation.
    /// </summary>
    public class PanelSpawner : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel to Spawn")]
        [SerializeField] private GameObject panelPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private float startScale = 0f;
        [SerializeField] private float endScale = 1f;
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Options")]
        [SerializeField] private bool spawnAtMousePosition = false;
        [SerializeField] private Transform parentTransform;
        [SerializeField] private bool destroyExisting = true;

        private GameObject currentPanel;

        private void Awake()
        {
            // If no parent assigned, use Canvas
            if (parentTransform == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    parentTransform = canvas.transform;
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SpawnPanel();
        }

        /// <summary>
        /// Public method to spawn the panel (can be called from Button onClick).
        /// </summary>
        public void SpawnPanel()
        {
            if (panelPrefab == null)
            {
                Debug.LogWarning("PanelSpawner: No panel prefab assigned!", this);
                return;
            }

            // Destroy existing panel if option is enabled
            if (destroyExisting && currentPanel != null)
            {
                Destroy(currentPanel);
            }

            // Spawn the panel
            currentPanel = Instantiate(panelPrefab, parentTransform);

            // Set position
            if (spawnAtMousePosition && parentTransform != null)
            {
                Vector2 localPoint;
                RectTransform parentRect = parentTransform as RectTransform;
                if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, Input.mousePosition, null, out localPoint))
                {
                    currentPanel.transform.localPosition = localPoint;
                }
            }

            // Start pop-up animation
            StartCoroutine(AnimatePopup(currentPanel));
        }

        private System.Collections.IEnumerator AnimatePopup(GameObject panel)
        {
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = panel.AddComponent<RectTransform>();
            }

            // Ensure CanvasGroup exists for fade effect
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = elapsed / animationDuration;
                float easedProgress = EaseOutBack(progress);

                // Scale animation
                rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, easedProgress);

                // Fade animation
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

                yield return null;
            }

            rectTransform.localScale = Vector3.one * endScale;
            canvasGroup.alpha = 1f;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Set the panel prefab programmatically.
        /// </summary>
        public void SetPanelPrefab(GameObject prefab)
        {
            panelPrefab = prefab;
        }

        /// <summary>
        /// Get the currently spawned panel (null if none).
        /// </summary>
        public GameObject GetCurrentPanel()
        {
            return currentPanel;
        }
    }
}
