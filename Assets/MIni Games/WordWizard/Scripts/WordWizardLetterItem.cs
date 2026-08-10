using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.Games.WordWizard
{
    /// <summary>
    /// A single tappable letter, shown either in the letter pool or placed
    /// into a slot. Holds no placement logic of its own — on tap it only
    /// reports itself via the callback passed to <see cref="Setup"/>,
    /// leaving WordWizardManager to decide whether it moves into a slot or
    /// back to the pool.
    /// </summary>
    public class WordWizardLetterItem : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TextMeshProUGUI _letterText;
        [SerializeField] private Button _button;

        private Action<WordWizardLetterItem> _onClicked;

        #endregion

        #region Properties

        /// <summary>The letter this item represents. Set once via <see cref="Setup"/>.</summary>
        public char LetterChar { get; private set; }

        #endregion

        #region Unity Lifecycle

        // None beyond the default MonoBehaviour lifecycle.

        #endregion

        #region Public Methods

        /// <summary>
        /// Populates this item with its letter and wires its tap callback.
        /// Call immediately after instantiating the prefab.
        /// </summary>
        /// <param name="letter">The letter this item represents.</param>
        /// <param name="onClicked">Invoked with this item whenever it is tapped.</param>
        public void Setup(char letter, Action<WordWizardLetterItem> onClicked)
        {
            LetterChar = letter;
            _onClicked = onClicked;

            if (_letterText != null)
            {
                _letterText.text = letter.ToString();
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClicked);
            }
        }

        /// <summary>
        /// Enables or disables tap input on this item, e.g. while the round
        /// is transitioning between entries.
        /// </summary>
        /// <param name="isInteractable">Whether this item should currently accept taps.</param>
        public void SetInteractable(bool isInteractable)
        {
            if (_button != null)
            {
                _button.interactable = isInteractable;
            }
        }

        #endregion

        #region Private Methods

        // None.

        #endregion

        #region Events / Callbacks

        private void HandleClicked()
        {
            _onClicked?.Invoke(this);
        }

        #endregion
    }
}
