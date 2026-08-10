using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.Games.WordWizard
{
    /// <summary>
    /// Top-level controller for the Word Wizard mini-game. Loads a batch of
    /// entries from the content repository, steps through them one at a
    /// time, spawns a shuffled letter pool (target letters + decoys), and
    /// handles tap-to-place/tap-to-undo spelling via a single growing text
    /// display, checking, hints, and score tracking.
    /// </summary>
    public class WordWizardManager : MonoBehaviour
    {
        #region Fields

        [Header("Content")]
        [Tooltip("How many entries to pull from the repository for this session.")]
        [SerializeField] private int _entriesPerBatch = 10;

        [Header("Board")]
        [SerializeField] private WordWizardLetterItem _letterItemPrefab;
        [SerializeField] private Transform _letterPoolContainer;
        [Tooltip("Displays the word as it is built — one character per placed " +
                 "letter, in placement order. Rebuilt from scratch on every push/pop.")]
        [SerializeField] private TextMeshProUGUI _typedWordText;

        [Header("Round")]
        [Tooltip("Displays \"Round X/Y\" for the current entry. Updated on every LoadEntry() call.")]
        [SerializeField] private TextMeshProUGUI _roundCounterText;

        [Header("Buttons & Text")]
        [SerializeField] private Button _checkAnswerButton;
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Header("Feedback")]
        [SerializeField] private string _incompleteFeedbackMessage = "Fill in every letter first!";
        [SerializeField] private string _correctFeedbackMessage = "Correct!";
        [SerializeField] private string _incorrectFeedbackMessage = "Not quite, try again!";
        [Tooltip("Seconds the correct-answer feedback stays on screen before the next round loads.")]
        [SerializeField] private float _feedbackDisplayDuration = 1.5f;

        [Header("Hints")]
        [SerializeField] private int _initialHints = 3;
        [SerializeField] private Button _hintButton;
        [Tooltip("Displays the remaining hint count, e.g. \"Hint (3)\".")]
        [SerializeField] private TextMeshProUGUI _hintText;

        [Header("Audio")]
        [Tooltip("No AudioClip field/resolution strategy exists yet for this content " +
                 "model — PlayAudio() safely no-ops until one is added.")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Button _playAudioButton;

        [Header("Completion")]
        [SerializeField] private GameObject _completionPanel;

        private IWordWizardContentRepository _contentRepository;
        private List<WordWizardEntry> _roundEntries;
        private readonly List<WordWizardLetterItem> _activeLetterItems = new List<WordWizardLetterItem>();
        private readonly List<WordWizardLetterItem> _placedLetterStack = new List<WordWizardLetterItem>();

        private int _currentEntryIndex;
        private int _remainingHints;
        private int _answeredCount;
        private int _correctCount;
        private bool _hasAnsweredCurrentEntry;
        private bool _isTransitioning;

        #endregion

        #region Properties

        /// <summary>
        /// Total number of entries answered correctly so far this session.
        /// </summary>
        public int CorrectAnswerCount => _correctCount;

        /// <summary>
        /// Total number of entries in the current session's batch.
        /// </summary>
        public int TotalQuestionCount => _roundEntries?.Count ?? 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _contentRepository = new JsonWordWizardContentRepository();
        }

        private void Start()
        {
            WireButtonListeners();

            _remainingHints = _initialHints;
            UpdateHintUI();

            LoadAndBeginSession();
        }

        #endregion

        #region Public Methods

        // None — all interaction is wired to private handlers via AddListener
        // in Start(), following Prefix & Suffix's and Word Listen's convention
        // rather than Inspector-bound OnClick() calls.

        #endregion

        #region Private Methods

        private void LoadAndBeginSession()
        {
            _contentRepository.Initialize();
            _roundEntries = _contentRepository.GetNextBatch(_entriesPerBatch);

            if (_roundEntries.Count == 0)
            {
                Debug.LogError("[WordWizardManager] No entries were loaded — check Resources/WordWizard/Entries.json.");
                return;
            }

            _currentEntryIndex = 0;
            LoadEntry(_currentEntryIndex);
        }

        private void LoadEntry(int index)
        {
            WordWizardEntry entry = _roundEntries[index];

            _hasAnsweredCurrentEntry = false;

            if (_feedbackText != null)
            {
                _feedbackText.text = string.Empty;
            }

            if (_roundCounterText != null)
            {
                _roundCounterText.text = $"Round {index + 1}/{_roundEntries.Count}";
            }

            SpawnLetterPool(entry);
            SetInteractionEnabled(true);
        }

        private void SpawnLetterPool(WordWizardEntry entry)
        {
            ClearBoard();

            char[] targetLetters = entry.TargetWord.ToCharArray();
            var lettersToSpawn = new List<char>(targetLetters);

            if (!string.IsNullOrEmpty(entry.DecoyLetters))
            {
                lettersToSpawn.AddRange(entry.DecoyLetters.ToCharArray());
            }

            ShuffleLetterList(lettersToSpawn);

            foreach (char letter in lettersToSpawn)
            {
                WordWizardLetterItem item = Instantiate(_letterItemPrefab, _letterPoolContainer);
                item.Setup(letter, HandleLetterItemClicked);
                _activeLetterItems.Add(item);
            }
        }

        private void ClearBoard()
        {
            foreach (WordWizardLetterItem item in _activeLetterItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _activeLetterItems.Clear();
            _placedLetterStack.Clear();
        }

        private void WireButtonListeners()
        {
            if (_checkAnswerButton != null)
            {
                _checkAnswerButton.onClick.AddListener(OnCheckAnswerClicked);
            }

            if (_hintButton != null)
            {
                _hintButton.onClick.AddListener(UseHint);
            }

            if (_playAudioButton != null)
            {
                _playAudioButton.onClick.AddListener(PlayAudio);
            }
        }

        private void PlaceLetter(WordWizardLetterItem item)
        {
            _placedLetterStack.Add(item);
            RefreshPlacedItemInteractivity();
            RebuildTypedWordDisplay();
        }

        private void UndoLastPlacement()
        {
            if (_placedLetterStack.Count == 0)
            {
                return;
            }

            WordWizardLetterItem item = _placedLetterStack[_placedLetterStack.Count - 1];
            _placedLetterStack.RemoveAt(_placedLetterStack.Count - 1);

            item.SetInteractable(true);
            RefreshPlacedItemInteractivity();
            RebuildTypedWordDisplay();
        }

        // Only the current top of the stack may be tapped again to undo —
        // every other placed letter is locked out until it becomes the top.
        private void RefreshPlacedItemInteractivity()
        {
            for (int i = 0; i < _placedLetterStack.Count; i++)
            {
                bool isTop = i == _placedLetterStack.Count - 1;
                _placedLetterStack[i].SetInteractable(isTop);
            }
        }

        private string BuildTypedWordString()
        {
            string typedWord = string.Empty;

            foreach (WordWizardLetterItem item in _placedLetterStack)
            {
                typedWord += item.LetterChar;
            }

            return typedWord;
        }

        private void RebuildTypedWordDisplay()
        {
            if (_typedWordText != null)
            {
                _typedWordText.text = BuildTypedWordString();
            }
        }

        private WordWizardLetterItem FindUnplacedMatchingItem(char expectedLetterUpper)
        {
            foreach (WordWizardLetterItem item in _activeLetterItems)
            {
                if (_placedLetterStack.Contains(item))
                {
                    continue;
                }

                if (char.ToUpperInvariant(item.LetterChar) == expectedLetterUpper)
                {
                    return item;
                }
            }

            return null;
        }

        private void ShuffleLetterList(List<char> letters)
        {
            for (int i = letters.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (letters[i], letters[swapIndex]) = (letters[swapIndex], letters[i]);
            }
        }

        private void OnCheckAnswerClicked()
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            WordWizardEntry entry = _roundEntries[_currentEntryIndex];

            if (_placedLetterStack.Count < entry.TargetWord.Length)
            {
                ShowFeedback(_incompleteFeedbackMessage);
                return;
            }

            if (IsSpellingCorrect(entry.TargetWord))
            {
                EvaluateCorrectAnswer();
            }
            else
            {
                EvaluateIncorrectAnswer();
            }
        }

        private bool IsSpellingCorrect(string targetWord)
        {
            return string.Equals(BuildTypedWordString(), targetWord, System.StringComparison.OrdinalIgnoreCase);
        }

        private void EvaluateCorrectAnswer()
        {
            _hasAnsweredCurrentEntry = true;
            _answeredCount++;
            _correctCount++;

            ShowFeedback(_correctFeedbackMessage);
            SetInteractionEnabled(false);
            StartCoroutine(AdvanceAfterDelayRoutine());
        }

        private void EvaluateIncorrectAnswer()
        {
            // Unlike a correct answer, this does not end the round — the
            // player keeps the same entry and can retry immediately once the
            // board is cleared below.
            ShowFeedback(_incorrectFeedbackMessage);
            ClearPlacedLettersToPool();
        }

        private void ClearPlacedLettersToPool()
        {
            foreach (WordWizardLetterItem item in _placedLetterStack)
            {
                item.SetInteractable(true);
            }

            _placedLetterStack.Clear();
            RebuildTypedWordDisplay();
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
            }
        }

        private void UseHint()
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            if (_remainingHints <= 0)
            {
                Debug.Log("[WordWizardManager] No hints remaining.");
                return;
            }

            WordWizardEntry entry = _roundEntries[_currentEntryIndex];
            char[] targetLetters = entry.TargetWord.ToCharArray();

            if (_placedLetterStack.Count >= targetLetters.Length)
            {
                return;
            }

            // The next position to fill is simply the current stack length —
            // this is push-only from the top, not a slot-targeted placement,
            // so the hint continues the word correctly regardless of what
            // else may already be sitting (incorrectly) in the pool.
            char nextLetter = char.ToUpperInvariant(targetLetters[_placedLetterStack.Count]);
            WordWizardLetterItem matchingItem = FindUnplacedMatchingItem(nextLetter);

            if (matchingItem == null)
            {
                return;
            }

            _remainingHints--;
            UpdateHintUI();
            PlaceLetter(matchingItem);
        }

        private void UpdateHintUI()
        {
            if (_hintText != null)
            {
                _hintText.text = $"Hint ({_remainingHints})";
            }

            if (_hintButton != null)
            {
                _hintButton.interactable = _remainingHints > 0;
            }
        }

        private void PlayAudio()
        {
            // No AudioClip field/resolution strategy exists yet for this
            // content model — this intentionally no-ops until one is added.
            // Kept as its own method (rather than inlined into the button
            // listener) so a future clip lookup can slot in here without
            // touching how the button is wired.
            if (_audioSource == null)
            {
                return;
            }
        }

        private void SetInteractionEnabled(bool isEnabled)
        {
            _isTransitioning = !isEnabled;

            if (_checkAnswerButton != null)
            {
                _checkAnswerButton.interactable = isEnabled;
            }

            if (_hintButton != null)
            {
                _hintButton.interactable = isEnabled && _remainingHints > 0;
            }

            foreach (WordWizardLetterItem item in _activeLetterItems)
            {
                if (item != null)
                {
                    item.SetInteractable(isEnabled);
                }
            }
        }

        private IEnumerator AdvanceAfterDelayRoutine()
        {
            yield return new WaitForSeconds(_feedbackDisplayDuration);

            int nextIndex = _currentEntryIndex + 1;

            if (nextIndex < _roundEntries.Count)
            {
                _currentEntryIndex = nextIndex;
                LoadEntry(_currentEntryIndex);
            }
            else
            {
                Debug.Log($"[WordWizardManager] Word Wizard Complete: {CorrectAnswerCount}/{TotalQuestionCount} correct");

                if (_completionPanel != null)
                {
                    _completionPanel.SetActive(true);
                }
            }
        }

        #endregion

        #region Events / Callbacks

        private void HandleLetterItemClicked(WordWizardLetterItem item)
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            bool isPlaced = _placedLetterStack.Contains(item);

            if (isPlaced)
            {
                bool isTopOfStack = _placedLetterStack[_placedLetterStack.Count - 1] == item;

                if (isTopOfStack)
                {
                    UndoLastPlacement();
                }

                // Already placed but not the current top — do nothing. Only
                // the most recent placement can be undone.
                return;
            }

            PlaceLetter(item);
        }

        #endregion
    }
}
