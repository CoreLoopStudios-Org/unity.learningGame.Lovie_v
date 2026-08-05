using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all player input. Listens for taps/clicks anywhere on screen,
/// raycasts against UI elements, finds FloatingObject components, and delegates.
/// Completely decoupled — knows nothing about scoring or game state.
/// GameManager provides the current target word via SetTargetWord().
/// </summary>
public class InputHandler : MonoBehaviour
{
    #region Fields

    public static InputHandler Instance { get; private set; }

    public string CurrentTargetWord => _currentTargetWord;

    private string _currentTargetWord;
    private bool _isAcceptingInput;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!_isAcceptingInput) return;

        bool tapped = false;
        Vector2 tapPosition = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapped = true;
            tapPosition = Mouse.current.position.ReadValue();
        }
#else
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapped = true;
            tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
#endif

        if (tapped)
            ProcessTap(tapPosition);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the current target word that the player should tap.
    /// Called by GameManager each time the audio loop advances.
    /// </summary>
    public void SetTargetWord(string word) => _currentTargetWord = word;

    /// <summary>
    /// Enables or disables tap input. Disabled during round transitions
    /// and when the game is paused.
    /// </summary>
    public void SetInputActive(bool active) => _isAcceptingInput = active;

    #endregion

    #region Private Methods

    private void ProcessTap(Vector2 screenPosition)
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var floatingObj = result.gameObject.GetComponentInParent<FloatingObject>();
            if (floatingObj != null)
            {
                floatingObj.OnTapped(_currentTargetWord);
                return;
            }
        }
    }

    #endregion
}