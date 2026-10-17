using UnityEngine;

namespace UI
{
    /// <summary>
    /// The screen edge a <see cref="SafeAreaFitter"/> keeps its element clear of.
    /// </summary>
    public enum SafeAreaEdge
    {
        /// <summary>Keep clear of the notch / camera cutout / status bar.</summary>
        Top,

        /// <summary>Keep clear of the home indicator / gesture bar.</summary>
        Bottom
    }

    /// <summary>
    /// Nudges a UI element away from device notches and home indicators WITHOUT
    /// changing its size, anchors or pivot, so the designed layout is never altered.
    /// The element only moves when the device cutout intrudes further than the
    /// design already allows (see <c>_designClearance</c>); on devices without a
    /// cutout it does nothing. Attach to a header (Edge = Top) or a bottom nav bar
    /// (Edge = Bottom). Children move with it because they are parented to it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        #region Fields

        private const float DEFAULT_DESIGN_CLEARANCE = 80f;

        [Tooltip("Which screen edge this element must stay clear of.")]
        [SerializeField] private SafeAreaEdge _edge = SafeAreaEdge.Top;

        [Tooltip("Distance (canvas units) between the screen edge and the nearest content of this " +
                 "element in the design. The element only moves by the amount the device cutout " +
                 "exceeds this value.")]
        [SerializeField] private float _designClearance = DEFAULT_DESIGN_CLEARANCE;

        private RectTransform _rectTransform;
        private Canvas _rootCanvas;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private float _appliedOffset;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            CacheReferences();
            ApplySafeArea();
        }

        private void Update()
        {
            if (HasScreenChanged())
            {
                ApplySafeArea();
            }
        }

        private void OnDisable()
        {
            RemoveOffset();
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            _rectTransform = GetComponent<RectTransform>();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            _rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
        }

        private bool HasScreenChanged()
        {
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (safeArea == _lastSafeArea && screenSize == _lastScreenSize)
            {
                return false;
            }

            return true;
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            float newOffset = Mathf.Max(0f, GetCutoutInsetInCanvasUnits() - _designClearance);
            MoveBy(newOffset - _appliedOffset);
            _appliedOffset = newOffset;
        }

        private float GetCutoutInsetInCanvasUnits()
        {
            float insetPixels = _edge == SafeAreaEdge.Top
                ? Screen.height - Screen.safeArea.yMax
                : Screen.safeArea.yMin;

            float canvasScale = _rootCanvas != null && _rootCanvas.scaleFactor > 0f
                ? _rootCanvas.scaleFactor
                : 1f;

            return Mathf.Max(0f, insetPixels) / canvasScale;
        }

        private void RemoveOffset()
        {
            if (_rectTransform == null || Mathf.Approximately(_appliedOffset, 0f)) return;

            MoveBy(-_appliedOffset);
            _appliedOffset = 0f;
        }

        // Top moves the element down (negative Y); Bottom moves it up (positive Y).
        private void MoveBy(float delta)
        {
            float direction = _edge == SafeAreaEdge.Top ? -1f : 1f;
            Vector2 position = _rectTransform.anchoredPosition;
            position.y += direction * delta;
            _rectTransform.anchoredPosition = position;
        }

        #endregion
    }
}
