using UnityEngine;

namespace UI
{
    /// <summary>
    /// Fits its RectTransform to the device's safe area, keeping UI content
    /// clear of notches, camera cutouts, and rounded corners on mobile screens.
    /// Attach directly to any top-level panel whose content must not be
    /// covered (headers, bottom nav bars, full-screen panels).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        #region Fields

        private RectTransform rectTransform;
        private Rect lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (HasScreenChanged())
            {
                ApplySafeArea();
            }
        }

        #endregion

        #region Private Methods

        private bool HasScreenChanged()
        {
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (safeArea == lastSafeArea &&
                screenSize == lastScreenSize &&
                Screen.orientation == lastOrientation)
            {
                return false;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastOrientation = Screen.orientation;
            return true;
        }

        private void ApplySafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }

        #endregion
    }
}
