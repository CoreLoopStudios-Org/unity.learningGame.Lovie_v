using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attached to every floating word object (Star, Cloud, Bubble).
/// 
/// Responsibilities:
///   - Float upward
///   - Display the assigned word
///   - Delegate shake to ShakeController (on visual child)
///   - Report tap result to GameManager
///   - Return itself to pool when off-screen or after pop
/// 
/// Does NOT know about: scoring, audio playback, spawning logic.
/// </summary>
public class FloatingObject : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector (set on prefab)
    // ─────────────────────────────────────────────
    [Header("References (set on prefab)")]
    [SerializeField] private TextMeshProUGUI _wordLabel;
    [SerializeField] private ShakeController _shakeController;   // On visual child
    [SerializeField] private GameObject _visualRoot;             // Visual child root

    // ─────────────────────────────────────────────
    // Runtime state (set by SpawnManager via Init)
    // ─────────────────────────────────────────────
    public string Word { get; private set; }
    public FloatingObjectType ObjectType { get; private set; }

    private FloatingObjectConfigSO _config;
    private float _floatSpeed;
    private bool _isPopped;
    private Action<FloatingObject, bool> _onTapCallback; // (self, isCorrect)
    private RectTransform _rectTransform;
    private float _topBoundaryY;

    // ─────────────────────────────────────────────
    // Init — called by SpawnManager instead of constructor
    // ─────────────────────────────────────────────
    public void Init(
        string word,
        FloatingObjectConfigSO config,
        float floatSpeed,
        float topBoundaryY,
        Action<FloatingObject, bool> onTapCallback)
    {
        Word = word;
        ObjectType = config.objectType;
        _config = config;
        _floatSpeed = floatSpeed;
        _topBoundaryY = topBoundaryY;
        _onTapCallback = onTapCallback;
        _isPopped = false;

        _rectTransform = GetComponent<RectTransform>();
        _wordLabel.text = word;
        _visualRoot.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // Float upward every frame
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (_isPopped) return;

        _rectTransform.anchoredPosition += Vector2.up * (_floatSpeed * Time.deltaTime);

        // Missed — floated off screen
        if (_rectTransform.anchoredPosition.y > _topBoundaryY)
        {
            OnMissed();
        }
    }

    // ─────────────────────────────────────────────
    // Shake API — called by GameManager when audio plays
    // ─────────────────────────────────────────────
    public void StartShake()
    {
        _shakeController.StartShake(_config.shakeStyle, _config.shakeIntensity, _config.shakeSpeed);
    }

    public void StopShake()
    {
        _shakeController.StopShake();
    }

    // ─────────────────────────────────────────────
    // Tap handling — called by InputHandler
    // ─────────────────────────────────────────────
    public void OnTapped(string currentTargetWord)
    {
        if (_isPopped) return;

        bool isCorrect = string.Equals(Word, currentTargetWord, StringComparison.OrdinalIgnoreCase);
        _onTapCallback?.Invoke(this, isCorrect);

        if (isCorrect)
            TriggerCorrectPop();
        else
            TriggerWrongFeedback();
    }

    // ─────────────────────────────────────────────
    // Pop VFX/SFX
    // ─────────────────────────────────────────────
    private void TriggerCorrectPop()
    {
        _isPopped = true;
        StopShake();

        // Spawn pop particle at current world position
        if (_config.popParticlePrefab != null)
        {
            var vfx = Instantiate(_config.popParticlePrefab, transform.position, Quaternion.identity);
            // Set particle color if the system supports it
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = _config.popColor;
            }
            Destroy(vfx, 2f);
        }

        // Play correct SFX
        AudioManager.Instance.PlaySFX(_config.correctPopSFX);

        // Hide visual immediately, return to pool after tiny delay
        _visualRoot.SetActive(false);
        Invoke(nameof(ReturnToPool), 0.1f);
    }

    private void TriggerWrongFeedback()
    {
        // Play wrong SFX
        AudioManager.Instance.PlaySFX(_config.wrongTapSFX);

        // Vibrate the object (quick punch, not the word-audio shake)
        StopShake(); // Stop any ongoing word shake
        _shakeController.StartShake(ShakeStyle.SideSway, 15f, 20f); // Fast hard shake

        // Stop wrong shake after short duration, resume word shake if still playing
        CancelInvoke(nameof(StopWrongShake));
        Invoke(nameof(StopWrongShake), 0.4f);
    }

    private void StopWrongShake()
    {
        StopShake();
    }

    private void OnMissed()
    {
        _isPopped = true;
        StopShake();
        _onTapCallback?.Invoke(this, false);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        CancelInvoke();
        SpawnManager.Instance.ReturnToPool(this);
    }

    private void OnDisable()
    {
        CancelInvoke();
        _isPopped = false;
    }
}