using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UI
{
    /// <summary>
    /// Modular toggle button with optional prefab spawning and sprite switching.
    /// Works as a simple toggle (sprite only) or dropdown (spawn prefab on open).
    /// </summary>
    public class DropdownToggle : MonoBehaviour
    {
        [Header("Toggle Settings")]
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;

        [Header("Prefab Spawning (Optional)")]
        [SerializeField] private bool spawnPrefab = false;
        [SerializeField] private GameObject prefabToSpawn;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private bool closeOnOutsideClick = true;

        [Header("Animation")]
        [SerializeField] private float spawnAnimationDuration = 0.3f;
        [SerializeField] private float despawnAnimationDuration = 0.2f;

        [Header("Events")]
        [SerializeField] private UnityEvent onTurnOn;
        [SerializeField] private UnityEvent onTurnOff;

        private GameObject spawnedInstance;
        private bool isOn = false;

        private void Awake()
        {
            // Get Image component if not assigned
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            // Set initial sprite
            UpdateSprite();
        }

        /// <summary>
        /// Call this from Button onClick event or directly.
        /// </summary>
        public void Toggle()
        {
            if (isOn)
            {
                TurnOff();
            }
            else
            {
                TurnOn();
            }
        }

        public void TurnOn()
        {
            if (isOn) return;

            isOn = true;
            UpdateSprite();
            onTurnOn?.Invoke();

            if (spawnPrefab && prefabToSpawn != null)
            {
                SpawnPrefab();
            }
        }

        public void TurnOff()
        {
            if (!isOn) return;

            isOn = false;
            UpdateSprite();
            onTurnOff?.Invoke();

            if (spawnPrefab && spawnedInstance != null)
            {
                StartCoroutine(DespawnWithAnimation());
            }
        }

        private void SpawnPrefab()
        {
            // Find spawn parent if not assigned
            Transform parent = spawnParent;
            if (parent == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    parent = canvas.transform;
                }
            }

            spawnedInstance = Instantiate(prefabToSpawn, parent);

            // Add despawn callback for outside clicks
            if (closeOnOutsideClick)
            {
                PanelClickHandler clickHandler = spawnedInstance.AddComponent<PanelClickHandler>();
                clickHandler.Initialize(this);
            }

            // Play spawn animation
            StartCoroutine(SpawnAnimation());
        }

        private System.Collections.IEnumerator SpawnAnimation()
        {
            if (spawnedInstance == null) yield break;

            RectTransform rect = spawnedInstance.GetComponent<RectTransform>();
            if (rect == null) yield break;

            CanvasGroup canvasGroup = spawnedInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = spawnedInstance.AddComponent<CanvasGroup>();
            }

            rect.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;

            while (elapsed < spawnAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / spawnAnimationDuration;
                float eased = EaseOutBack(t);

                rect.localScale = Vector3.one * eased;
                canvasGroup.alpha = t;

                yield return null;
            }

            rect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator DespawnWithAnimation()
        {
            if (spawnedInstance == null) yield break;

            RectTransform rect = spawnedInstance.GetComponent<RectTransform>();
            if (rect == null) yield break;

            CanvasGroup canvasGroup = spawnedInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = spawnedInstance.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            Vector3 startScale = rect.localScale;

            while (elapsed < despawnAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / despawnAnimationDuration;

                rect.localScale = startScale * (1f - t);
                canvasGroup.alpha = 1f - t;

                yield return null;
            }

            Destroy(spawnedInstance);
            spawnedInstance = null;
        }

        private void UpdateSprite()
        {
            if (buttonImage != null)
            {
                buttonImage.sprite = isOn ? onSprite : offSprite;
            }
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Internal handler for outside clicks.
        /// </summary>
        internal void HandleOutsideClick()
        {
            if (isOn && spawnPrefab && closeOnOutsideClick)
            {
                TurnOff();
            }
        }

        private void OnDestroy()
        {
            if (spawnedInstance != null)
            {
                Destroy(spawnedInstance);
            }
        }

        /// <summary>
        /// Get current toggle state.
        /// </summary>
        public bool IsOn => isOn;
    }

    /// <summary>
    /// Attached to spawned prefab to detect outside clicks.
    /// </summary>
    internal class PanelClickHandler : MonoBehaviour
    {
        private DropdownToggle dropdown;
        private RectTransform rectTransform;

        public void Initialize(DropdownToggle owner)
        {
            dropdown = owner;
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (dropdown == null || !dropdown.IsOn) return;

            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

                if (!IsClickInsidePanel(mousePos))
                {
                    dropdown.HandleOutsideClick();
                }
            }
        }

        private bool IsClickInsidePanel(Vector2 screenPos)
        {
            if (rectTransform == null) return false;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas?.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPos, camera, out localPoint))
            {
                return rectTransform.rect.Contains(localPoint);
            }
            return false;
        }
    }
}
