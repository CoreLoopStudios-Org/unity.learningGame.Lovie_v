using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.GameFramework.UI;

namespace Modules.Games.PrefixSuffix
{
    /// <summary>
    /// Top-level controller for the Prefix &amp; Suffix mini-game. Loads a batch
    /// of entries from the content repository, steps through them one at a
    /// time, handles option selection and answer checking, tracks score, and
    /// delegates round countdown to a shared <see cref="RoundTimer"/>.
    /// </summary>
    public class PrefixSuffixManager : MonoBehaviour
    {
        #region Fields

        [Header("Content")]
        [Tooltip("How many entries to pull from the repository for this session.")]
        [SerializeField] private int _entriesPerBatch = 10;

        [Header("Prompt")]
        [Tooltip("Displays the current entry's root word, live-updated to preview " +
                 "the combined word as the player selects an option and a mode.")]
        [SerializeField] private TextMeshProUGUI _rootWordText;
        [Tooltip("Displays \"Round X/Y\" for the current entry. Updated on every LoadEntry() call.")]
        [SerializeField] private TextMeshProUGUI _roundCounterText;

        [Header("Mode Toggle")]
        // Both Toggle components must have the same ToggleGroup assigned to their
        // "Group" field in the Unity Inspector — that is what gives them mutual
        // exclusivity. This script does not create or assign that group.
        [Tooltip("Selects Prefix mode for the current entry. Part of a ToggleGroup " +
                 "with _suffixToggle for mutual exclusivity, assigned in the Inspector.")]
        [SerializeField] private Toggle _prefixToggle;
        [Tooltip("Selects Suffix mode for the current entry. Part of a ToggleGroup " +
                 "with _prefixToggle for mutual exclusivity, assigned in the Inspector.")]
        [SerializeField] private Toggle _suffixToggle;

        [Header("Options")]
        [Tooltip("Fixed row of option buttons. Entries with fewer options than " +
                 "buttons will have the extra buttons hidden for that round.")]
        [SerializeField] private List<Button> _optionButtons;
        [Tooltip("Label text paired 1:1 with _optionButtons, in the same order.")]
        [SerializeField] private List<TextMeshProUGUI> _optionLabels;
        [Tooltip("Color applied to an option button's Image when not selected.")]
        [SerializeField] private Color _normalOptionColor = Color.white;
        [Tooltip("Color applied to an option button's Image while it is the current selection.")]
        [SerializeField] private Color _selectedOptionColor = Color.yellow;

        [Header("Buttons & Text")]
        [Tooltip("Confirms the current selection and evaluates it against CorrectOptionIndex.")]
        [SerializeField] private Button _checkAnswerButton;
        [Tooltip("Consumes one hint and reveals the correct option when clicked.")]
        [SerializeField] private Button _hintButton;
        [Tooltip("Displays the remaining hint count, e.g. \"Hint (3)\".")]
        [SerializeField] private TextMeshProUGUI _hintText;
        [Tooltip("Displays the correct/incorrect feedback message after Check Answer.")]
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Header("Feedback")]
        [SerializeField] private string _correctFeedbackMessage = "Correct!";
        [SerializeField] private string _incorrectFeedbackMessage = "Not quite — try the next one!";
        [Tooltip("Seconds the feedback message stays on screen before the next round loads.")]
        [SerializeField] private float _feedbackDisplayDuration = 1.5f;

        [Header("Hints")]
        [SerializeField] private int _initialHints = 3;

        [Header("Round Timer")]
        [Tooltip("Shared countdown component. Timer expiry is treated the same as an incorrect answer.")]
        [SerializeField] private RoundTimer _roundTimer;

        [Header("Completion")]
        [SerializeField] private GameObject _completionPanel;

        private const int NoSelection = -1;

        private IPrefixSuffixContentRepository _contentRepository;
        private List<PrefixSuffixEntry> _roundEntries;
        private List<Image> _optionButtonImages;

        private int _currentEntryIndex;
        private int _selectedOptionIndex = NoSelection;
        private PrefixSuffixMode? _selectedMode = null;
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
            _contentRepository = new JsonPrefixSuffixContentRepository();
            _optionButtonImages = new List<Image>();
        }

        private void Start()
        {
            CacheOptionButtonImages();
            WireButtonListeners();

            _remainingHints = _initialHints;
            UpdateHintUI();

            if (_roundTimer != null)
            {
                _roundTimer.OnTimerExpired += HandleTimerExpired;
            }

            LoadAndBeginSession();
        }

        private void OnDestroy()
        {
            if (_roundTimer != null)
            {
                _roundTimer.OnTimerExpired -= HandleTimerExpired;
            }
        }

        #endregion

        #region Public Methods

        // None — all interaction is wired to private handlers via AddListener
        // in Start(), following Sentence Builder's and Word Listen's convention
        // (hintButton.onClick.AddListener(UseHint) etc.) rather than
        // Inspector-bound OnClick() calls.

        #endregion

        #region Private Methods

        private void LoadAndBeginSession()
        {
            _contentRepository.Initialize();
            _roundEntries = _contentRepository.GetNextBatch(_entriesPerBatch);

            if (_roundEntries.Count == 0)
            {
                Debug.LogError("[PrefixSuffixManager] No entries were loaded — check Resources/PrefixSuffix/Entries.json.");
                return;
            }

            _currentEntryIndex = 0;
            LoadEntry(_currentEntryIndex);
        }

        private void LoadEntry(int index)
        {
            PrefixSuffixEntry entry = _roundEntries[index];

            _hasAnsweredCurrentEntry = false;
            _selectedOptionIndex = NoSelection;
            _selectedMode = null;

            // Setting isOn = false here fires onValueChanged(false) for whichever
            // toggle was previously selected — that is fine, the listeners wired in
            // WireButtonListeners() ignore the false callback and only react to true.
            if (_prefixToggle != null)
            {
                _prefixToggle.isOn = false;
            }

            if (_suffixToggle != null)
            {
                _suffixToggle.isOn = false;
            }

            if (_feedbackText != null)
            {
                _feedbackText.text = string.Empty;
            }

            if (_roundCounterText != null)
            {
                _roundCounterText.text = $"Round {index + 1}/{_roundEntries.Count}";
            }

            PopulateOptionButtons(entry);
            UpdateWordPreview();
            SetInteractionEnabled(true);

            if (_roundTimer != null)
            {
                _roundTimer.ResetTimer();
                _roundTimer.StartTimer();
            }
        }

        private void PopulateOptionButtons(PrefixSuffixEntry entry)
        {
            for (int i = 0; i < _optionButtons.Count; i++)
            {
                bool hasOption = i < entry.Options.Length;

                if (_optionButtons[i] != null)
                {
                    _optionButtons[i].gameObject.SetActive(hasOption);
                }

                if (!hasOption)
                {
                    continue;
                }

                if (i < _optionLabels.Count && _optionLabels[i] != null)
                {
                    _optionLabels[i].text = entry.Options[i];
                }

                SetOptionButtonColor(i, _normalOptionColor);
            }
        }

        private void CacheOptionButtonImages()
        {
            foreach (Button optionButton in _optionButtons)
            {
                _optionButtonImages.Add(optionButton != null ? optionButton.GetComponent<Image>() : null);
            }
        }

        private void WireButtonListeners()
        {
            for (int i = 0; i < _optionButtons.Count; i++)
            {
                if (_optionButtons[i] == null)
                {
                    continue;
                }

                int capturedIndex = i;
                _optionButtons[i].onClick.AddListener(() => SelectOption(capturedIndex));
            }

            if (_checkAnswerButton != null)
            {
                _checkAnswerButton.onClick.AddListener(OnCheckAnswerClicked);
            }

            if (_hintButton != null)
            {
                _hintButton.onClick.AddListener(UseHint);
            }

            if (_prefixToggle != null)
            {
                // ToggleGroup fires onValueChanged for both the toggle becoming
                // selected and the one becoming deselected — only react to true.
                _prefixToggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        SelectMode(PrefixSuffixMode.Prefix);
                    }
                });
            }

            if (_suffixToggle != null)
            {
                _suffixToggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        SelectMode(PrefixSuffixMode.Suffix);
                    }
                });
            }
        }

        private void SelectOption(int index)
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            if (_selectedOptionIndex != NoSelection)
            {
                SetOptionButtonColor(_selectedOptionIndex, _normalOptionColor);
            }

            _selectedOptionIndex = index;
            SetOptionButtonColor(_selectedOptionIndex, _selectedOptionColor);
            UpdateWordPreview();
        }

        private void SetOptionButtonColor(int index, Color color)
        {
            if (index < 0 || index >= _optionButtonImages.Count)
            {
                return;
            }

            Image image = _optionButtonImages[index];
            if (image != null)
            {
                image.color = color;
            }
        }

        private void SelectMode(PrefixSuffixMode mode)
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            _selectedMode = mode;
            UpdateWordPreview();
        }

        private void UpdateWordPreview()
        {
            if (_rootWordText == null)
            {
                return;
            }

            PrefixSuffixEntry entry = _roundEntries[_currentEntryIndex];

            if (!_selectedMode.HasValue || _selectedOptionIndex == NoSelection)
            {
                _rootWordText.text = entry.RootWord;
                return;
            }

            string selectedOptionText = entry.Options[_selectedOptionIndex];

            _rootWordText.text = _selectedMode.Value == PrefixSuffixMode.Prefix
                ? selectedOptionText + entry.RootWord
                : entry.RootWord + selectedOptionText;
        }

        private void OnCheckAnswerClicked()
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            if (_selectedOptionIndex == NoSelection)
            {
                Debug.Log("[PrefixSuffixManager] Select an option before checking your answer.");
                return;
            }

            if (_selectedMode == null)
            {
                Debug.Log("[PrefixSuffixManager] Select Prefix or Suffix before checking your answer.");
                return;
            }

            PrefixSuffixEntry entry = _roundEntries[_currentEntryIndex];
            bool isCorrect = _selectedOptionIndex == entry.CorrectOptionIndex && _selectedMode.Value == entry.Mode;
            EvaluateAnswer(isCorrect);
        }

        private void UseHint()
        {
            if (_isTransitioning || _hasAnsweredCurrentEntry)
            {
                return;
            }

            if (_remainingHints <= 0)
            {
                Debug.Log("[PrefixSuffixManager] No hints remaining.");
                return;
            }

            _remainingHints--;
            UpdateHintUI();

            PrefixSuffixEntry entry = _roundEntries[_currentEntryIndex];
            SelectOption(entry.CorrectOptionIndex);

            // Setting isOn = true fires onValueChanged(true), which calls
            // SelectMode() for us via the listener wired in WireButtonListeners() —
            // calling SelectMode() directly here as well would invoke it twice.
            if (entry.Mode == PrefixSuffixMode.Prefix)
            {
                if (_prefixToggle != null)
                {
                    _prefixToggle.isOn = true;
                }
            }
            else
            {
                if (_suffixToggle != null)
                {
                    _suffixToggle.isOn = true;
                }
            }
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

        private void EvaluateAnswer(bool isCorrect)
        {
            _hasAnsweredCurrentEntry = true;
            _answeredCount++;

            if (isCorrect)
            {
                _correctCount++;
            }

            if (_roundTimer != null)
            {
                _roundTimer.StopTimer();
            }

            ShowFeedback(isCorrect);
            SetInteractionEnabled(false);
            StartCoroutine(AdvanceAfterDelayRoutine());
        }

        private void ShowFeedback(bool isCorrect)
        {
            if (_feedbackText == null)
            {
                return;
            }

            _feedbackText.text = isCorrect ? _correctFeedbackMessage : _incorrectFeedbackMessage;
        }

        private void SetInteractionEnabled(bool isEnabled)
        {
            _isTransitioning = !isEnabled;

            foreach (Button optionButton in _optionButtons)
            {
                if (optionButton != null)
                {
                    optionButton.interactable = isEnabled;
                }
            }

            if (_checkAnswerButton != null)
            {
                _checkAnswerButton.interactable = isEnabled;
            }

            if (_hintButton != null)
            {
                _hintButton.interactable = isEnabled && _remainingHints > 0;
            }

            if (_prefixToggle != null)
            {
                _prefixToggle.interactable = isEnabled;
            }

            if (_suffixToggle != null)
            {
                _suffixToggle.interactable = isEnabled;
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
                // Interaction is already disabled here — EvaluateAnswer() always
                // calls SetInteractionEnabled(false) before this coroutine is
                // started, and nothing re-enables it since LoadEntry() (the only
                // caller of SetInteractionEnabled(true)) does not run again once
                // the session is complete. No further call needed.
                Debug.Log($"[PrefixSuffixManager] Prefix & Suffix Complete: {CorrectAnswerCount}/{TotalQuestionCount} correct");

                if (_completionPanel != null)
                {
                    _completionPanel.SetActive(true);
                }
            }
        }

        #endregion

        #region Events / Callbacks

        private void HandleTimerExpired()
        {
            if (_hasAnsweredCurrentEntry)
            {
                return;
            }

            EvaluateAnswer(false);
        }

        #endregion
    }
}
