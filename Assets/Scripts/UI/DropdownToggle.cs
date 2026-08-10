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

        [Header("Animation Settings")]
        [SerializeField] private float spawnAnimationDuration = 0.4f;
        [SerializeField] private float despawnAnimationDuration = 0.25f;
        [SerializeField] private AnimationCurve spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve despawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Events")]
        [SerializeField] private UnityEvent onTurnOn;
        [SerializeField] private UnityEvent onTurnOff;

        private GameObject spawnedInstance;
        private bool isOn = false;

        private void Awake()
        {
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }
            UpdateSprite();
        }

        public void Toggle()
        {
            if (isOn) TurnOff();
            else TurnOn();
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
            Transform parent = spawnParent;
            if (parent == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null) parent = canvas.transform;
            }

            spawnedInstance = Instantiate(prefabToSpawn, parent);

            if (closeOnOutsideClick)
            {
                PanelClickHandler clickHandler = spawnedInstance.AddComponent<PanelClickHandler>();
                clickHandler.Initialize(this);
            }

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
                float t = Mathf.Clamp01(elapsed / spawnAnimationDuration);
                float curvedT = spawnCurve.Evaluate(t);

                rect.localScale = Vector3.one * curvedT;
                canvasGroup.alpha = curvedT;

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
                float t = Mathf.Clamp01(elapsed / despawnAnimationDuration);
                float curvedT = despawnCurve.Evaluate(t);

                rect.localScale = startScale * (1f - curvedT);
                canvasGroup.alpha = 1f - curvedT;

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

        public bool IsOn => isOn;
    }

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
