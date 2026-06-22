using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.GameFramework.Content;
using Modules.GameFramework.UI;

namespace Modules.Games.StoryQuest
{
    /// <summary>
    /// Top-level controller for the Story Quest mini-game. Loads the level
    /// content, populates the story text, spawns one question card per
    /// question into the scrollable quiz content, tracks how many were
    /// answered correctly, and enables the Complete button only once every
    /// question has been answered.
    /// </summary>
    public class StoryQuestManager : MonoBehaviour
    {
        #region Fields

        [Header("Content")]
        [SerializeField] private string _storyId = "story_001";

        [Header("Reading Panel")]
        [SerializeField] private TMP_Text _storyText;

        [Header("Quiz")]
        [SerializeField] private Transform _questionContentParent;
        [SerializeField] private QuestionCardController _questionCardPrefab;
        [SerializeField] private Button _completeButton;

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
            SetCompleteButtonInteractable(false);
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

        private void SetCompleteButtonInteractable(bool isInteractable)
        {
            if (_completeButton != null)
            {
                _completeButton.interactable = isInteractable;
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
                SetCompleteButtonInteractable(true);
            }
        }

        #endregion
    }
}