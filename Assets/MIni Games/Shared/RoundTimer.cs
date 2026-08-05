using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Modules.GameFramework.UI
{
    /// <summary>
    /// Generic countdown timer for a single round of gameplay. Counts down
    /// from a configured duration, updates an mm:ss display each frame, and
    /// fires <see cref="OnTimerExpired"/> once when it reaches zero.
    /// Contains zero game-specific logic — reusable by any mini-game that
    /// needs a round timer. The owning manager decides what "expired" means
    /// for its own game (advance round, end session, etc.).
    /// </summary>
    public class RoundTimer : MonoBehaviour
    {
        #region Fields

        [SerializeField] private float _durationSeconds = 60f;
        [SerializeField] private TextMeshProUGUI _timerText;

        private const string TimerDisplayFormat = "{0:00}:{1:00}";
        private const int SecondsPerMinute = 60;

        private float _timeRemaining;
        private bool _isRunning;

        #endregion

        #region Properties

        /// <summary>Seconds remaining in the current countdown.</summary>
        public float TimeRemaining => _timeRemaining;

        /// <summary>True while the countdown is actively ticking.</summary>
        public bool IsRunning => _isRunning;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _timeRemaining = _durationSeconds;
            UpdateTimerDisplay();
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            TickCountdown();
        }

        #endregion

        #region Public Methods

        /// <summary>Starts (or resumes) the countdown from its current remaining time.</summary>
        public void StartTimer()
        {
            _isRunning = true;
        }

        /// <summary>Stops the countdown without resetting the remaining time.</summary>
        public void StopTimer()
        {
            _isRunning = false;
        }

        /// <summary>Stops the countdown and resets the remaining time back to the configured duration.</summary>
        public void ResetTimer()
        {
            _isRunning = false;
            _timeRemaining = _durationSeconds;
            UpdateTimerDisplay();
        }

        #endregion

        #region Private Methods

        private void TickCountdown()
        {
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _isRunning = false;
                UpdateTimerDisplay();
                OnTimerExpired?.Invoke();
                return;
            }

            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (_timerText == null)
            {
                return;
            }

            int totalWholeSeconds = Mathf.Max(0, Mathf.FloorToInt(_timeRemaining));
            int minutes = totalWholeSeconds / SecondsPerMinute;
            int seconds = totalWholeSeconds % SecondsPerMinute;
            _timerText.text = string.Format(CultureInfo.InvariantCulture, TimerDisplayFormat, minutes, seconds);
        }

        #endregion

        #region Events / Callbacks

        /// <summary>Fired once, the moment the countdown reaches zero.</summary>
        public event Action OnTimerExpired;

        #endregion
    }
}
