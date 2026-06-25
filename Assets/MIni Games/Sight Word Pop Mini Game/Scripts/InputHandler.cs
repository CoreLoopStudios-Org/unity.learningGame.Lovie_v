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
    public string CurrentTargetWord => _currentTargetWord; 

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
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"[InputHandler] Tap at {screenPosition}, hit {results.Count} objects");
        
        foreach (var result in results)
        {
            Debug.Log($"[InputHandler] Hit: {result.gameObject.name}");
            
            var floatingObj = result.gameObject.GetComponentInParent<FloatingObject>();
            if (floatingObj != null)
            {
                Debug.Log($"[InputHandler] Found FloatingObject: {floatingObj.Word}");
                floatingObj.OnTapped(_currentTargetWord);
                return;
            }
        }
    }
}