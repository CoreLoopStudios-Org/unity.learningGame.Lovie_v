using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Audio;

namespace Audio
{
    /// <summary>
    /// Global handler that automatically plays SFX for ALL button clicks in the scene.
    /// Works with regular buttons, prefab buttons, and dynamically spawned buttons.
    /// Place this on your AudioManager GameObject.
    /// </summary>
    public class GlobalButtonSFXHandler : MonoBehaviour
    {
        [Header("SFX Settings")]
        [SerializeField] private bool enableButtonSFX = true;

        [Header("Custom SFX (Optional)")]
        [SerializeField] private AudioClip customClickSFX;

        [Header("Exclude Components")]
        [Tooltip("Buttons with these components won't play auto SFX")]
        [SerializeField] private string[] excludeComponentNames = new string[] { "AudioToggleButton" };

        // Track subscribed buttons to avoid duplicates
        private HashSet<Button> subscribedButtons = new HashSet<Button>();

        private float checkInterval = 1f;
        private float nextCheckTime;

        private void OnEnable()
        {
            // Clear any previous tracking
            subscribedButtons.Clear();

            // Subscribe to all existing buttons
            SubscribeToAllButtons();
        }

        private void OnDisable()
        {
            // Unsubscribe from all tracked buttons
            foreach (Button button in subscribedButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(OnAnyButtonClicked);
                }
            }
            subscribedButtons.Clear();
        }

        private void Update()
        {
            // Periodically check for new buttons (for dynamically spawned prefabs)
            if (Time.unscaledTime >= nextCheckTime)
            {
                SubscribeToNewButtons();
                nextCheckTime = Time.unscaledTime + checkInterval;
            }
        }

        private void SubscribeToAllButtons()
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);

            foreach (Button button in allButtons)
            {
                TrySubscribeToButton(button);
            }
        }

        private void SubscribeToNewButtons()
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);

            foreach (Button button in allButtons)
            {
                // Only subscribe if we haven't already
                if (!subscribedButtons.Contains(button))
                {
                    TrySubscribeToButton(button);
                }
            }

            // Clean up null/destroyed buttons from the set
            subscribedButtons.RemoveWhere(b => b == null);
        }

        private void TrySubscribeToButton(Button button)
        {
            if (button == null) return;

            // Skip if already subscribed
            if (subscribedButtons.Contains(button)) return;

            // Check if button has excluded components
            if (ShouldExclude(button)) return;

            // Subscribe to button click
            button.onClick.AddListener(OnAnyButtonClicked);
            subscribedButtons.Add(button);
        }

        private bool ShouldExclude(Button button)
        {
            // Check if button has excluded components
            foreach (string componentName in excludeComponentNames)
            {
                if (string.IsNullOrEmpty(componentName)) continue;

                var component = button.GetComponent(componentName);
                if (component != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnAnyButtonClicked()
        {
            if (!enableButtonSFX) return;
            if (AudioManager.Instance == null) return;

            if (customClickSFX != null)
            {
                AudioManager.Instance.PlaySFX(customClickSFX);
            }
            else
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }
}
