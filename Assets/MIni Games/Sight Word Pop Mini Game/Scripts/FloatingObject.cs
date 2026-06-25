using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingObject : MonoBehaviour
{
    [Header("References (set on prefab)")]
    [SerializeField] private TextMeshProUGUI _wordLabel;
    [SerializeField] private ShakeController _shakeController;
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Button _button; // ← ADD THIS

    public string Word { get; private set; }
    public FloatingObjectType ObjectType { get; private set; }

    private FloatingObjectConfigSO _config;
    private float _floatSpeed;
    private bool _isPopped;
    private Action<FloatingObject, bool> _onTapCallback;
    private RectTransform _rectTransform;
    private float _topBoundaryY;

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

        // Wire button click
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnButtonClicked);
        
        Debug.Log($"[FloatingObject] Initialized: {word}");
    }

    private void OnButtonClicked()
    {
        Debug.Log($"[FloatingObject] Button clicked: {Word}");
        OnTapped(InputHandler.Instance.CurrentTargetWord);
    }

    private void Update()
    {
        if (_isPopped) return;

        _rectTransform.anchoredPosition += Vector2.up * (_floatSpeed * Time.deltaTime);

        if (_rectTransform.anchoredPosition.y > _topBoundaryY)
            OnMissed();
    }

    public void StartShake() =>
        _shakeController.StartShake(_config.shakeStyle, _config.shakeIntensity, _config.shakeSpeed);

    public void StopShake() =>
        _shakeController.StopShake();

    public void OnTapped(string currentTargetWord)
    {
        if (_isPopped) return;

        Debug.Log($"[FloatingObject] Tapped: {Word}, Target: {currentTargetWord}");

        bool isCorrect = string.Equals(Word, currentTargetWord, StringComparison.OrdinalIgnoreCase);
        _onTapCallback?.Invoke(this, isCorrect);

        if (isCorrect)
            TriggerCorrectPop();
        else
            TriggerWrongFeedback();
    }

    private void TriggerCorrectPop()
    {
        _isPopped = true;
        StopShake();

        if (_config.popParticlePrefab != null)
        {
            var vfx = Instantiate(_config.popParticlePrefab, transform.position, Quaternion.identity);
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = _config.popColor;
            }
            Destroy(vfx, 2f);
        }

        AudioManager.Instance.PlaySFX(_config.correctPopSFX);

        _visualRoot.SetActive(false);
        Invoke(nameof(ReturnToPool), 0.1f);
    }

    private void TriggerWrongFeedback()
    {
        AudioManager.Instance.PlaySFX(_config.wrongTapSFX);
        StopShake();
        _shakeController.StartShake(ShakeStyle.SideSway, 15f, 20f);
        CancelInvoke(nameof(StopWrongShake));
        Invoke(nameof(StopWrongShake), 0.4f);
    }

    private void StopWrongShake() => StopShake();

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