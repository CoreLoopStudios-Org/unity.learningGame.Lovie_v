using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.GameFramework.UI
{
    /// <summary>
    /// Represents a single answer option in a quiz question (one of typically four).
    /// This is a pure view component: it displays data it is given, detects taps,
    /// and reports the tap upward via <see cref="OnOptionSelected"/>.
    /// It has NO knowledge of sibling options, the correct answer, or scoring —
    /// that orchestration belongs to the question controller that spawns these.
    /// Reused as-is across every quiz-style mini-game.
    /// </summary>
    [RequireComponent(typeof(AnswerFeedback))]
    public class AnswerOptionView : MonoBehaviour, IPointerClickHandler
    {
        #region Fields

        [SerializeField] private Image _optionBackground;
        [SerializeField] private TMP_Text _optionText;

        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _correctSprite;

        private AnswerFeedback _feedback;
        private int _optionIndex;
        private bool _isInputLocked;

        #endregion

        #region Properties

        /// <summary>
        /// The index of this option within its parent question's option list (0-3).
        /// Set by the spawner at instantiation time.
        /// </summary>
        public int OptionIndex => _optionIndex;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _feedback = GetComponent<AnswerFeedback>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Populates this option with display data. Called by the question
        /// controller immediately after instantiation.
        /// </summary>
        /// <param name="optionIndex">This option's index within the question (0-3).</param>
        /// <param name="optionText">The answer text to display.</param>
        public void Setup(int optionIndex, string optionText)
        {
            _optionIndex = optionIndex;
            _isInputLocked = false;

            if (_optionText != null)
            {
                _optionText.text = optionText;
            }

            if (_optionBackground != null && _normalSprite != null)
            {
                _optionBackground.sprite = _normalSprite;
            }

            if (_feedback == null)
            {
                Debug.LogError($"[AnswerOptionView] Missing AnswerFeedback component on GameObject '{gameObject.name}' (full path: {GetFullPath()}). Add the AnswerFeedback component to this exact GameObject.", gameObject);
                return;
            }

            _feedback.ResetVisualState();
        }

        private string GetFullPath()
        {
            string path = gameObject.name;
            Transform current = transform.parent;

            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// Locks or unlocks tap input on this option. The question controller
        /// locks all options once any answer has been selected.
        /// </summary>
        public void SetInputLocked(bool isLocked)
        {
            _isInputLocked = isLocked;
        }

        /// <summary>
        /// Plays the "this option is correct" visual feedback.
        /// Called by the question controller — may be called on an option
        /// the player did not tap, if they tapped a different (wrong) one.
        /// </summary>
        public void ShowCorrect()
        {
            if (_optionBackground != null && _correctSprite != null)
            {
                _optionBackground.sprite = _correctSprite;
            }

            _feedback.PlayCorrect();
        }

        /// <summary>
        /// Plays the "this option is wrong" visual feedback: shake, red tint, haptic.
        /// Sprite stays on normal; only the wrong option that was actually tapped
        /// should call this.
        /// </summary>
        public void ShowWrong()
        {
            _feedback.PlayWrong();
        }

        #endregion

        #region Events / Callbacks

        /// <summary>
        /// Fired when the player taps this option, after the tap-pop animation
        /// has started. The question controller subscribes to this to learn
        /// which option (by index) was selected.
        /// </summary>
        public event Action<AnswerOptionView> OnOptionSelected;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isInputLocked)
            {
                return;
            }

            _isInputLocked = true;
            _feedback.PlayTapPop(() => OnOptionSelected?.Invoke(this));
        }

        #endregion
    }
}