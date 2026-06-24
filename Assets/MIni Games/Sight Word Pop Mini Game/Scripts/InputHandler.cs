using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles all player input. Listens for taps/clicks anywhere on screen,
/// raycasts against UI elements, finds FloatingObject components, and delegates.
/// 
/// Completely decoupled — knows nothing about scoring or game state.
/// GameManager provides the current target word via SetTargetWord().
/// </summary>
public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    private string _currentTargetWord;
    private bool _isAcceptingInput;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    public void SetTargetWord(string word) => _currentTargetWord = word;

    public void SetInputActive(bool active) => _isAcceptingInput = active;

    // ─────────────────────────────────────────────
    // Input loop
    // ─────────────────────────────────────────────

    private void Update()
    {
        if (!_isAcceptingInput) return;

        // Support both touch and mouse click in one block
        bool tapped = false;
        Vector2 tapPosition = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
            tapPosition = Input.mousePosition;
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapped = true;
            tapPosition = Input.GetTouch(0).position;
        }
#endif

        if (tapped)
            ProcessTap(tapPosition);
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        // Build a pointer event for UI raycasting
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Walk up the hierarchy from each hit until we find a FloatingObject
        foreach (var result in results)
        {
            var floatingObj = result.gameObject.GetComponentInParent<FloatingObject>();
            if (floatingObj != null)
            {
                floatingObj.OnTapped(_currentTargetWord);
                return; // Only handle one tap per frame
            }
        }
    }
}