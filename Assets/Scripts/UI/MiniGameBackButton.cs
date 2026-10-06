using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Back exit for a minigame scene. Add this to the scene's back Button GameObject —
    /// clicking returns to the menu's Games page without reporting completion
    /// (back = quit the run early).
    /// </summary>
    public class MiniGameBackButton : MonoBehaviour
    {
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

        private void OnDestroy()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(ExitToGamesPage);
            }
        }

        private void ExitToGamesPage()
        {
            MiniGameNavigator.BackToMenu();
        }
    }
}
