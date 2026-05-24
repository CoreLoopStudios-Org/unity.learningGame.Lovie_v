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
        [SerializeField] private float animationDuration = 0.4f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
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

        public void SpawnPanel()
        {
            if (panelPrefab == null)
            {
                Debug.LogWarning("PanelSpawner: No panel prefab assigned!", this);
                return;
            }

            if (destroyExisting && currentPanel != null)
            {
                Destroy(currentPanel);
            }

            currentPanel = Instantiate(panelPrefab, parentTransform);

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

            StartCoroutine(AnimatePopup(currentPanel));
        }

        private System.Collections.IEnumerator AnimatePopup(GameObject panel)
        {
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = panel.AddComponent<RectTransform>();
            }

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float curvedT = animationCurve.Evaluate(t);

                rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, curvedT);
                canvasGroup.alpha = curvedT;

                yield return null;
            }

            rectTransform.localScale = Vector3.one * endScale;
            canvasGroup.alpha = 1f;
        }

        public void SetPanelPrefab(GameObject prefab)
        {
            panelPrefab = prefab;
        }

        public GameObject GetCurrentPanel()
        {
            return currentPanel;
        }
    }
}
