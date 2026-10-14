using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI
{
    /// <summary>
    /// Back exit for a minigame scene. Add this to the scene's back Button GameObject —
    /// clicking, or pressing the Android hardware back button, returns to the menu's
    /// Games page without reporting completion (back = quit the run early). If a popup
    /// handled by <see cref="PanelCloser"/> is open, that popup's own back/escape
    /// handling takes priority instead of exiting the game.
    /// </summary>
    public class MiniGameBackButton : MonoBehaviour
    {
        #region Unity Lifecycle

        private void Awake()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(ExitToGamesPage);
            }
            else
            {
                Debug.LogWarning($"[MiniGameBackButton] No Button component on '{name}' — add this component to the back Button object.", this);
            }
        }

        private void Update()
        {
            if (BackWasPressedThisFrame() && !IsPopupOpen())
            {
                ExitToGamesPage();
            }
        }

        private void OnDestroy()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(ExitToGamesPage);
            }
        }

        #endregion

        #region Private Methods

        private bool BackWasPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // A PanelCloser only exists while its popup is open, so its presence means
        // the popup should consume the back press instead of us exiting the game.
        private bool IsPopupOpen()
        {
            return FindAnyObjectByType<PanelCloser>() != null;
        }

        private void ExitToGamesPage()
        {
            MiniGameNavigator.BackToMenu();
        }

        #endregion
    }
}
