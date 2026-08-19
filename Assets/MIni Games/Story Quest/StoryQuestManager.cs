using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.GameFramework.Content;
using Modules.GameFramework.UI;
using ImagineMe.API;

namespace Modules.Games.StoryQuest
{
    /// <summary>
    /// Top-level controller for the Story Quest mini-game. Loads the level
    /// content, populates the story text, spawns one question card per
    /// question into the scrollable quiz content, tracks how many were
    /// answered correctly, and shows the Complete button only once every
    /// question has been answered.
    /// </summary>
    public class StoryQuestManager : MonoBehaviour
    {
        #region Fields

        [Header("Content")]
        [SerializeField] private string _storyId = "story_001";

        [Header("Reading Panel")]
        [SerializeField] private TMP_Text _storyText;
        [SerializeField] private GameObject _readingPanel;

        [Header("Quiz")]
        [SerializeField] private GameObject _quizPanel;
        [SerializeField] private Transform _questionContentParent;
        [SerializeField] private QuestionCardController _questionCardPrefab;
        [SerializeField] private Button _completeButton;

        [Header("Completion")]
        [SerializeField] private GameObject _completionPanel;
        [SerializeField] private GameCompletionReporter completionReporter;

        private IStoryQuestContentRepository _contentRepository;
        private StoryQuestLevel _level;
        private readonly List<QuestionCardController> _spawnedCards = new List<QuestionCardController>();

        private int _answeredCount;
        private int _correctCount;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _contentRepository = new JsonStoryQuestContentRepository();
        }

        private void Start()
        {
            if (completionReporter == null)
            {
                completionReporter = GetComponent<GameCompletionReporter>();
                if (completionReporter == null)
                {
                    completionReporter = gameObject.AddComponent<GameCompletionReporter>();
                }
            }

            if (_storyId != null && _storyId.StartsWith("rd_"))
            {
                completionReporter.SetGameId("reading_detective");
            }
            else
            {
                completionReporter.SetGameId("story_quest");
            }

            LoadAndDisplayLevel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSpawnedCards();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Total number of questions answered correctly so far. Used by the
        /// completion flow to calculate score and coins earned.
        /// </summary>
        public int CorrectAnswerCount => _correctCount;

        /// <summary>
        /// Total number of questions in the current level.
        /// </summary>
        public int TotalQuestionCount => _level?.questions?.Count ?? 0;

        /// <summary>
        /// Hides the Reading panel and shows the Quiz panel. Bound to the
        /// Next button's OnClick() on the Reading screen.
        /// </summary>
        public void ShowQuizPanel()
        {
            if (_readingPanel != null)
            {
                _readingPanel.SetActive(false);
            }

            if (_quizPanel != null)
            {
                _quizPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Bound to the Complete button's OnClick(), shown only once every
        /// question has been answered. Locks the quiz panel against further
        /// input, logs the final result, and shows the completion panel.
        /// No-ops if called before every question has been answered.
        /// </summary>
        public void CompleteGame()
        {
            if (_answeredCount < TotalQuestionCount)
            {
                return;
            }

            if (_quizPanel != null)
            {
                _quizPanel.SetActive(false);
            }

            Debug.Log($"Story Quest Complete: {CorrectAnswerCount}/{TotalQuestionCount} correct");

            // Report completion to backend
            if (completionReporter != null)
            {
                completionReporter.ReportCompletion(CorrectAnswerCount, TotalQuestionCount);
            }

            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
            }
        }

        #endregion

        #region Private Methods

        private void LoadAndDisplayLevel()
        {
            _level = _contentRepository.LoadLevel(_storyId);

            if (_level == null)
            {
                Debug.LogError("[StoryQuestManager] Failed to load level, aborting setup.");
                return;
            }

            if (_storyText != null)
            {
                _storyText.text = _level.content;
            }

            SpawnQuestionCards();
            SetCompleteButtonVisible(false);
        }

        private void SpawnQuestionCards()
        {
            if (_questionCardPrefab == null || _questionContentParent == null)
            {
                Debug.LogError("[StoryQuestManager] Question card prefab or content parent not assigned.");
                return;
            }

            int totalQuestions = _level.questions.Count;

            for (int i = 0; i < totalQuestions; i++)
            {
                QuestionData questionData = _level.questions[i];

                QuestionCardController card = Instantiate(_questionCardPrefab, _questionContentParent);
                card.Setup(i + 1, totalQuestions, questionData.questionText, questionData.options, questionData.correctOptionIndex);
                card.OnAnswered += HandleQuestionAnswered;

                _spawnedCards.Add(card);
            }
        }

        private void UnsubscribeFromSpawnedCards()
        {
            foreach (QuestionCardController card in _spawnedCards)
            {
                if (card != null)
                {
                    card.OnAnswered -= HandleQuestionAnswered;
                }
            }
        }

        private void SetCompleteButtonVisible(bool isVisible)
        {
            if (_completeButton != null)
            {
                _completeButton.gameObject.SetActive(isVisible);
            }
        }

        #endregion

        #region Events / Callbacks

        private void HandleQuestionAnswered(bool wasCorrect)
        {
            _answeredCount++;

            if (wasCorrect)
            {
                _correctCount++;
            }

            if (_answeredCount >= TotalQuestionCount)
            {
                SetCompleteButtonVisible(true);
            }
        }

        #endregion
    }
}