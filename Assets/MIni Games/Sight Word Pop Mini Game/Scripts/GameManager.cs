using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The brain of the game. Owns the state machine and orchestrates
/// all other systems. This is the ONLY place game rules live.
/// 
/// State machine:
///   Idle → Playing → (RoundComplete | GameOver) → Idle
/// 
/// Round loop:
///   1. Build word list from LevelData
///   2. Tell SpawnManager to start spawning
///   3. Cycle through target words — play audio, wait, advance
///   4. Track correct taps and misses
///   5. End round when all words processed
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────
    public enum GameState { Idle, Playing, Paused, RoundComplete, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Idle;

    // ─────────────────────────────────────────────
    // Config
    // ─────────────────────────────────────────────
    [Header("Level")]
    [SerializeField] private LevelDataSO _levelData;

    [Header("Score")]
    [SerializeField] private int _coinsPerCorrectTap = 10;
    [SerializeField] private int _coinPenaltyPerMiss = 5;

    [Header("Completion")]
    [SerializeField] private Api.GameCompletionReporter completionReporter;

    // ─────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────
    private List<WordEntry> _roundWords;      // Words for this round
    private int _currentTargetIndex;          // Which word is being played now
    private WordEntry CurrentTarget => _roundWords[_currentTargetIndex];

    private int _coins;
    private int _correctTaps;
    private int _missedWords;

    private Coroutine _audioLoopRoutine;

    private IWordProvider _wordProvider;

    // ─────────────────────────────────────────────
    // Events (UIManager subscribes)
    // ─────────────────────────────────────────────
    public event System.Action<int> OnCoinsChanged;
    public event System.Action<GameState> OnStateChanged;
    public event System.Action<bool> OnTapResult;   // true = correct, false = wrong

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _wordProvider = Api.ApiConfig.Instance.UseRemoteContent
            ? (IWordProvider)new ApiWordProvider(_levelData)
            : new ScriptableObjectWordProvider(_levelData);
    }

    private void Start()
    {
        if (completionReporter == null)
        {
            completionReporter = GetComponent<Api.GameCompletionReporter>();
            if (completionReporter == null)
            {
                completionReporter = gameObject.AddComponent<Api.GameCompletionReporter>();
            }
        }
        completionReporter.SetGameId("sight_word_pop");

        // Ensure AudioManager exists
        if (AudioManager.Instance == null)
        {
            var audioGo = new GameObject("AudioManager");
            audioGo.AddComponent<AudioManager>();
        }

        // Ensure UIManager HUD exists if not in scene
        if (FindObjectOfType<UIManager>() == null)
        {
            var uiGo = new GameObject("UIManager");
            uiGo.AddComponent<UIManager>();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnWordAudioStarted += HandleWordAudioStarted;
            AudioManager.Instance.OnWordAudioFinished += HandleWordAudioFinished;
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnWordAudioStarted -= HandleWordAudioStarted;
            AudioManager.Instance.OnWordAudioFinished -= HandleWordAudioFinished;
        }
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    public void StartRound()
    {
        if (CurrentState == GameState.Playing) return;

        _correctTaps = 0;
        _missedWords = 0;
        _currentTargetIndex = 0;

        int wordsCount = _levelData != null ? _levelData.wordsPerRound : 8;
        _roundWords = _wordProvider != null ? _wordProvider.GetWords(wordsCount) : (_levelData != null ? _levelData.GetShuffledWords(wordsCount) : new List<WordEntry>());

        SetState(GameState.Playing);

        // Tell SpawnManager to start
        if (SpawnManager.Instance != null && _levelData != null)
        {
            SpawnManager.Instance.StartSpawning(
                ExtractWordStrings(_roundWords),
                _levelData,
                HandleObjectTapped
            );
        }

        // Tell InputHandler to accept taps
        if (InputHandler.Instance != null)
        {
            InputHandler.Instance.SetInputActive(true);
        }

        // Start the audio loop
        _audioLoopRoutine = StartCoroutine(AudioLoop());
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void EndRound()
    {
        StopAllGameRoutines();
        SpawnManager.Instance.StopSpawning();
        InputHandler.Instance.SetInputActive(false);
        AudioManager.Instance.StopWord();
        SetState(GameState.RoundComplete);

        Debug.Log($"[GameManager] Round complete. Correct: {_correctTaps} | Missed: {_missedWords}");

        if (completionReporter != null)
        {
            int total = _correctTaps + _missedWords;
            int score = Mathf.Max(0, (_correctTaps * _coinsPerCorrectTap) - (_missedWords * _coinPenaltyPerMiss));
            completionReporter.ReportCompletionWithScore(score, _correctTaps, total > 0 ? total : 1);
        }
    }

    // ─────────────────────────────────────────────
    // Audio loop — cycles through target words
    // ─────────────────────────────────────────────

    /// <summary>
    /// Plays each target word's audio in sequence.
    /// Waits for the audio to finish + a gap before advancing.
    /// Loops back to the start until the round ends.
    /// </summary>
    private IEnumerator AudioLoop()
    {
        while (CurrentState == GameState.Playing)
        {
            if (_roundWords == null || _roundWords.Count == 0) yield break;

            // Announce current target to InputHandler
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.SetTargetWord(CurrentTarget.word);
            }

            // Play the word audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWord(CurrentTarget);
            }

            // Wait for audio to finish (AudioManager fires OnWordAudioFinished)
            // We poll state here; the shake start/stop is event-driven
            float waitTime = (CurrentTarget.audioClip != null ? CurrentTarget.audioClip.length : 1.5f)
                             + (_levelData != null ? _levelData.audioPlayInterval : 3f);
            yield return new WaitForSeconds(waitTime);

            // Advance to next word (loop through the list)
            _currentTargetIndex = (_currentTargetIndex + 1) % _roundWords.Count;
        }
    }

    // ─────────────────────────────────────────────
    // Event handlers
    // ─────────────────────────────────────────────

    private void HandleWordAudioStarted()
    {
        SpawnManager.Instance.NotifyWordAudioStarted(CurrentTarget.word);
    }

    private void HandleWordAudioFinished()
    {
        SpawnManager.Instance.NotifyWordAudioStopped(CurrentTarget.word);
    }

    private void HandleObjectTapped(FloatingObject obj, bool isCorrect)
    {
        OnTapResult?.Invoke(isCorrect);

        if (isCorrect)
        {
            _correctTaps++;
            AddCoins(_coinsPerCorrectTap);
        }
        else
        {
            // Wrong tap or missed word
            if (!obj.gameObject.activeSelf) // It was a miss (floated off)
                _missedWords++;
            else
                AddCoins(-_coinPenaltyPerMiss);
        }

        // Check round end condition
        CheckRoundEndCondition();
    }

    private void CheckRoundEndCondition()
    {
        // Round ends when all spawned objects have been interacted with
        // This is a simple version; expand for lives/time-based endings
        int totalInteractions = _correctTaps + _missedWords;
        if (totalInteractions >= _roundWords.Count)
            EndRound();
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void AddCoins(int amount)
    {
        _coins = Mathf.Max(0, _coins + amount);
        OnCoinsChanged?.Invoke(_coins);
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    private void StopAllGameRoutines()
    {
        if (_audioLoopRoutine != null)
        {
            StopCoroutine(_audioLoopRoutine);
            _audioLoopRoutine = null;
        }
    }

    private List<string> ExtractWordStrings(List<WordEntry> entries)
    {
        var result = new List<string>(entries.Count);
        foreach (var e in entries)
            result.Add(e.word);
        return result;
    }
}