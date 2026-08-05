using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Pure view layer. Subscribes to GameManager events and updates UI.
/// Never calls into game logic — only reads events and updates displays.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI _coinLabel;
    [SerializeField] private TextMeshProUGUI _feedbackLabel;    // "Great!" / "Try Again!"
    [SerializeField] private GameObject _hudRoot;

    [Header("Panels")]
    [SerializeField] private GameObject _idlePanel;            // Start screen
    [SerializeField] private GameObject _roundCompletePanel;
    [SerializeField] private GameObject _pausePanel;

    [Header("Feedback")]
    [SerializeField] private string _correctFeedbackText = "⭐ Great!";
    [SerializeField] private string _wrongFeedbackText = "Try Again!";
    [SerializeField] private Color _correctColor = new Color(1f, 0.85f, 0f);
    [SerializeField] private Color _wrongColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float _feedbackDuration = 0.8f;

    private Coroutine _feedbackRoutine;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────
    private void Start()
    {
        GameManager.Instance.OnCoinsChanged += UpdateCoins;
        GameManager.Instance.OnStateChanged += UpdatePanels;
        GameManager.Instance.OnTapResult += ShowTapFeedback;

        // Initial state
        UpdateCoins(0);
        ShowPanel(GameManager.GameState.Idle);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged -= UpdateCoins;
            GameManager.Instance.OnStateChanged -= UpdatePanels;
            GameManager.Instance.OnTapResult -= ShowTapFeedback;
        }
    }

    // ─────────────────────────────────────────────
    // Event handlers
    // ─────────────────────────────────────────────

    private void UpdateCoins(int coins)
    {
        if (_coinLabel != null)
            _coinLabel.text = coins.ToString();
    }

    private void UpdatePanels(GameManager.GameState state)
    {
        ShowPanel(state);
    }

    private void ShowTapFeedback(bool isCorrect)
    {
        if (_feedbackLabel == null) return;

        if (_feedbackRoutine != null)
            StopCoroutine(_feedbackRoutine);

        _feedbackRoutine = StartCoroutine(FeedbackRoutine(isCorrect));
    }

    // ─────────────────────────────────────────────
    // Panel visibility
    // ─────────────────────────────────────────────
    private void ShowPanel(GameManager.GameState state)
    {
        SetActive(_idlePanel, state == GameManager.GameState.Idle);
        SetActive(_roundCompletePanel, state == GameManager.GameState.RoundComplete);
        SetActive(_pausePanel, state == GameManager.GameState.Paused);
        SetActive(_hudRoot, state == GameManager.GameState.Playing);
    }

    // ─────────────────────────────────────────────
    // Feedback coroutine
    // ─────────────────────────────────────────────
    private IEnumerator FeedbackRoutine(bool isCorrect)
    {
        _feedbackLabel.text = isCorrect ? _correctFeedbackText : _wrongFeedbackText;
        _feedbackLabel.color = isCorrect ? _correctColor : _wrongColor;
        _feedbackLabel.gameObject.SetActive(true);

        // Animate scale pop
        _feedbackLabel.transform.localScale = Vector3.one * 1.4f;
        float elapsed = 0f;
        while (elapsed < _feedbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _feedbackDuration;
            _feedbackLabel.transform.localScale = Vector3.Lerp(
                Vector3.one * 1.4f, Vector3.one, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        _feedbackLabel.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Button callbacks (wire up in Inspector)
    // ─────────────────────────────────────────────
    public void OnStartButtonPressed() => GameManager.Instance.StartRound();
    public void OnPauseButtonPressed() => GameManager.Instance.PauseGame();
    public void OnResumeButtonPressed() => GameManager.Instance.ResumeGame();
    public void OnBackButtonPressed()
    {
        GameManager.Instance.EndRound();
        // Load previous scene or fire your study app's back navigation here
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}