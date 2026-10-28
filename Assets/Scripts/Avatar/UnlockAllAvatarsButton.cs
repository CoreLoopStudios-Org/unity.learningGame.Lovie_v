using System;
using UnityEngine;
using UnityEngine.UI;

namespace Avatar
{
    /// <summary>
    /// Makes the "Unlock All Avatars" pill tappable. It only announces the tap through
    /// <see cref="OnUnlockAllTapped"/>; the navigation layer listens and opens the store,
    /// so this button needs no reference to it.
    /// </summary>
    public class UnlockAllAvatarsButton : MonoBehaviour
    {
        #region Fields

        private Button _button;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
            }

            if (_button.targetGraphic == null)
            {
                Graphic graphic = GetComponent<Graphic>();
                if (graphic == null)
                {
                    var hitArea = gameObject.AddComponent<Image>();
                    hitArea.color = Color.clear;
                    graphic = hitArea;
                }

                _button.targetGraphic = graphic;
            }
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }

        #endregion

        #region Events / Callbacks

        /// <summary>Raised when the "Unlock All Avatars" button is tapped.</summary>
        public static event Action OnUnlockAllTapped;

        private void HandleClicked()
        {
            OnUnlockAllTapped?.Invoke();
        }

        #endregion
    }
}
