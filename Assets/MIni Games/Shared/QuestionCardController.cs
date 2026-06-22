using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Modules.GameFramework.UI
{
    /// <summary>
    /// Controller for a single quiz question card. Populates the question
    /// number, question text, and the four answer options from data, then
    /// listens for any option being selected and orchestrates the correct
    /// and wrong feedback across all four options it holds references to.
    /// Attach to the root of the question card prefab.
    /// </summary>
    public class QuestionCardController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TMP_Text _questionNoText;
        [SerializeField] private TMP_Text _questionTitleText;
        [SerializeField] private List<AnswerOptionView> _options;

        private int _correctOptionIndex;
        private bool _hasAnswered;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToOptions();
        }

        private void OnDisable()
        {
            UnsubscribeFromOptions();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Populates this question card with data. Call immediately after
        /// instantiating the prefab.
        /// </summary>
        /// <param name="questionNumber">Display index, e.g. 1 for "Question 1/3".</param>
        /// <param name="totalQuestions">Total question count for the display label.</param>
        /// <param name="questionText">The question text to show.</param>
        /// <param name="optionTexts">Exactly four option strings, in display order.</param>
        /// <param name="correctOptionIndex">Index (0-3) of the correct option.</param>
        public void Setup(int questionNumber, int totalQuestions, string questionText, IReadOnlyList<string> optionTexts, int correctOptionIndex)
        {
            _hasAnswered = false;
            _correctOptionIndex = correctOptionIndex;

            if (_questionNoText != null)
            {
                _questionNoText.text = $"Question {questionNumber}/{totalQuestions}";
            }

            if (_questionTitleText != null)
            {
                _questionTitleText.text = questionText;
            }

            for (int i = 0; i < _options.Count; i++)
            {
                string optionText = i < optionTexts.Count ? optionTexts[i] : string.Empty;
                _options[i].Setup(i, optionText);
                _options[i].SetInputLocked(false);
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeToOptions()
        {
            if (_options == null)
            {
                return;
            }

            foreach (AnswerOptionView option in _options)
            {
                if (option != null)
                {
                    option.OnOptionSelected += HandleOptionSelected;
                }
            }
        }

        private void UnsubscribeFromOptions()
        {
            if (_options == null)
            {
                return;
            }

            foreach (AnswerOptionView option in _options)
            {
                if (option != null)
                {
                    option.OnOptionSelected -= HandleOptionSelected;
                }
            }
        }

        private void LockAllOptions()
        {
            foreach (AnswerOptionView option in _options)
            {
                option.SetInputLocked(true);
            }
        }

        #endregion

        #region Events / Callbacks

        /// <summary>
        /// Fired once, when the player selects any option for this question.
        /// Reports whether the selected answer was correct so the parent
        /// quiz flow can track score and decide when to advance.
        /// </summary>
        public event Action<bool> OnAnswered;

        private void HandleOptionSelected(AnswerOptionView selectedOption)
        {
            if (_hasAnswered)
            {
                return;
            }

            _hasAnswered = true;
            LockAllOptions();

            bool isCorrect = selectedOption.OptionIndex == _correctOptionIndex;

            if (isCorrect)
            {
                selectedOption.ShowCorrect();
            }
            else
            {
                selectedOption.ShowWrong();
                _options[_correctOptionIndex].ShowCorrect();
            }

            OnAnswered?.Invoke(isCorrect);
        }

        #endregion
    }
}