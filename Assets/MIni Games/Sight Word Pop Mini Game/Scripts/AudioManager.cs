using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central audio hub. Two responsibilities:
///   1. Play the current target word's audio clip (one at a time)
///   2. Play any SFX (pop, wrong tap) from a pooled source
/// 
/// FUTURE SWAP: Replace PlayWord's AudioClip parameter with
/// an async URL fetch when backend audio is ready.
/// Everything else stays the same.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource _wordSource;     // Dedicated to word pronunciation
    [SerializeField] private int _sfxPoolSize = 5;

    private readonly List<AudioSource> _sfxPool = new List<AudioSource>();

    // ─────────────────────────────────────────────
    // Events — GameManager listens to know when audio ends
    // ─────────────────────────────────────────────
    public event System.Action OnWordAudioStarted;
    public event System.Action OnWordAudioFinished;

    private Coroutine _wordFinishRoutine;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildSFXPool();
    }

    private void BuildSFXPool()
    {
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxPool.Add(source);
        }
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Play a word's audio clip. Fires OnWordAudioStarted immediately
    /// and OnWordAudioFinished when the clip ends.
    /// </summary>
    public void PlayWord(AudioClip clip)
    {
        if (_wordFinishRoutine != null)
            StopCoroutine(_wordFinishRoutine);

        _wordSource.Stop();

        if (clip == null)
        {
            // No audio yet — just wait 2 seconds then fire finished
            OnWordAudioStarted?.Invoke();
            _wordFinishRoutine = StartCoroutine(WaitForWordEnd(2f));
            return;
        }

        _wordSource.clip = clip;
        _wordSource.Play();
        OnWordAudioStarted?.Invoke();
        _wordFinishRoutine = StartCoroutine(WaitForWordEnd(clip.length));
    }

    /// <summary>Stops current word audio immediately (e.g. on game over).</summary>
    public void StopWord()
    {
        if (_wordFinishRoutine != null)
        {
            StopCoroutine(_wordFinishRoutine);
            _wordFinishRoutine = null;
        }
        _wordSource.Stop();
    }

    /// <summary>Play a one-shot SFX from the pool. Fire-and-forget.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        var source = GetAvailableSFXSource();
        if (source == null)
        {
            Debug.LogWarning("[AudioManager] SFX pool exhausted. Increase pool size.");
            return;
        }

        source.clip = clip;
        source.Play();
    }

    // ─────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────
    private System.Collections.IEnumerator WaitForWordEnd(float duration)
    {
        yield return new UnityEngine.WaitForSeconds(duration);
        OnWordAudioFinished?.Invoke();
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var src in _sfxPool)
            if (!src.isPlaying) return src;
        return null;
    }
}