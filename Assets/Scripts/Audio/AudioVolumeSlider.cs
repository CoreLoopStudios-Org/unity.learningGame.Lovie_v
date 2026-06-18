using UnityEngine;
using UnityEngine.UI;
using Audio;

namespace Audio.UI
{
    /// <summary>
    /// Connects a UI Slider to AudioManager volume control.
    /// Place this on your BGM and SFX sliders in the settings panel.
    ///
    /// IMPORTANT: Setup your Slider in Unity Inspector:
    /// - Min Value: 0
    /// - Max Value: 1 (for decimal) or 100 (for percentage)
    /// - Whole Numbers: UNCHECKED (for smooth volume control)
    /// </summary>
    public class AudioVolumeSlider : MonoBehaviour
    {
        [Header("Audio Type")]
        [SerializeField] private AudioType audioType = AudioType.BGM;

        [Header("Slider Settings")]
        [SerializeField] private Slider slider;
        [SerializeField] private bool updateOnStart = true;

        [Header("Value Range")]
        [Tooltip("Is slider value 0-100? If false, assumes 0-1")]
        [SerializeField] private bool isPercentage = false;

        [Header("Optional: Volume Text Display")]
        [SerializeField] private TMPro.TextMeshProUGUI volumeText;

        private bool isInternalUpdate = false;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }

        private void OnEnable()
        {
            if (AudioManager.Instance == null) return;

            switch (audioType)
            {
                case AudioType.BGM:
                    AudioManager.Instance.OnBGMVolumeChanged += HandleVolumeChanged;
                    break;
                case AudioType.SFX:
                    AudioManager.Instance.OnSFXVolumeChanged += HandleVolumeChanged;
                    break;
            }

            slider.onValueChanged.AddListener(OnSliderValueChanged);

            if (updateOnStart)
            {
                UpdateSliderValue();
            }
        }

        private void OnDisable()
        {
            if (AudioManager.Instance == null) return;

            switch (audioType)
            {
                case AudioType.BGM:
                    AudioManager.Instance.OnBGMVolumeChanged -= HandleVolumeChanged;
                    break;
                case AudioType.SFX:
                    AudioManager.Instance.OnSFXVolumeChanged -= HandleVolumeChanged;
                    break;
            }

            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        private void OnSliderValueChanged(float value)
        {
            if (isInternalUpdate || AudioManager.Instance == null) return;

            float normalizedValue = isPercentage ? value / 100f : value;

            switch (audioType)
            {
                case AudioType.BGM:
                    AudioManager.Instance.SetBGMVolume(normalizedValue);
                    break;
                case AudioType.SFX:
                    AudioManager.Instance.SetSFXVolume(normalizedValue);
                    break;
            }

            UpdateVolumeText(normalizedValue);
        }

        private void HandleVolumeChanged(float newVolume)
        {
            isInternalUpdate = true;

            float sliderValue = isPercentage ? newVolume * 100f : newVolume;
            slider.value = sliderValue;

            UpdateVolumeText(newVolume);

            isInternalUpdate = false;
        }

        private void UpdateSliderValue()
        {
            isInternalUpdate = true;

            float currentVolume = audioType == AudioType.BGM
                ? AudioManager.Instance.BGMVolume
                : AudioManager.Instance.SFXVolume;

            float sliderValue = isPercentage ? currentVolume * 100f : currentVolume;
            slider.value = sliderValue;

            UpdateVolumeText(currentVolume);

            isInternalUpdate = false;
        }

        private void UpdateVolumeText(float volume)
        {
            if (volumeText != null)
            {
                volumeText.text = Mathf.RoundToInt(volume * 100f) + "%";
            }
        }

        public enum AudioType
        {
            BGM,
            SFX
        }
    }
}
