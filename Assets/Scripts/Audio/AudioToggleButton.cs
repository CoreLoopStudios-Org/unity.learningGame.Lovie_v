using UnityEngine;
using UnityEngine.UI;
using Audio;

namespace Audio.UI
{
    /// <summary>
    /// Universal audio toggle button component.
    /// Place this on any UI toggle button to control audio.
    /// Automatically syncs with AudioManager and updates sprite.
    /// </summary>
    public class AudioToggleButton : MonoBehaviour
    {
        [Header("Audio Type")]
        [SerializeField] private AudioType audioType = AudioType.BGM;

        [Header("Sprite States")]
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        [Header("Optional: Custom Images")]
        [SerializeField] private bool useCustomTarget = false;
        [SerializeField] private Image targetImage;

        private Button button;
        private Image buttonImage;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = targetImage;

            if (!useCustomTarget)
            {
                buttonImage = GetComponent<Image>();
                if (buttonImage == null)
                {
                    buttonImage = GetComponentInChildren<Image>();
                }
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnToggleClicked);
            }

            SubscribeToAudioManager();
            UpdateInitialState();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnToggleClicked);
            }

            UnsubscribeFromAudioManager();
        }

        private void SubscribeToAudioManager()
        {
            if (AudioManager.Instance == null) return;

            switch (audioType)
            {
                case AudioType.BGM:
                    AudioManager.Instance.OnBGMStateChanged += HandleStateChanged;
                    break;
                case AudioType.SFX:
                    AudioManager.Instance.OnSFXStateChanged += HandleStateChanged;
                    break;
                case AudioType.Master:
                    AudioManager.Instance.OnBGMStateChanged += HandleMasterStateChanged;
                    AudioManager.Instance.OnSFXStateChanged += HandleMasterStateChanged;
                    break;
            }
        }

        private void UnsubscribeFromAudioManager()
        {
            if (AudioManager.Instance == null) return;

            switch (audioType)
            {
                case AudioType.BGM:
                    AudioManager.Instance.OnBGMStateChanged -= HandleStateChanged;
                    break;
                case AudioType.SFX:
                    AudioManager.Instance.OnSFXStateChanged -= HandleStateChanged;
                    break;
                case AudioType.Master:
                    AudioManager.Instance.OnBGMStateChanged -= HandleMasterStateChanged;
                    AudioManager.Instance.OnSFXStateChanged -= HandleMasterStateChanged;
                    break;
            }
        }

        private void UpdateInitialState()
        {
            bool isOn = false;

            switch (audioType)
            {
                case AudioType.BGM:
                    isOn = AudioManager.Instance?.IsBGMEnabled ?? true;
                    break;
                case AudioType.SFX:
                    isOn = AudioManager.Instance?.IsSFXEnabled ?? true;
                    break;
                case AudioType.Master:
                    isOn = (AudioManager.Instance?.IsBGMEnabled ?? true) &&
                           (AudioManager.Instance?.IsSFXEnabled ?? true);
                    break;
            }

            UpdateSprite(isOn);
        }

        private void OnToggleClicked()
        {
            if (AudioManager.Instance == null) return;

            switch (audioType)
            {
                case AudioType.BGM:
                    AudioManager.Instance.ToggleBGM();
                    break;
                case AudioType.SFX:
                    AudioManager.Instance.ToggleSFX();
                    break;
                case AudioType.Master:
                    bool newState = !(AudioManager.Instance.IsBGMEnabled && AudioManager.Instance.IsSFXEnabled);
                    AudioManager.Instance.SetBGMEnabled(newState);
                    AudioManager.Instance.SetSFXEnabled(newState);
                    break;
            }
        }

        private void HandleStateChanged(bool isOn)
        {
            UpdateSprite(isOn);
        }

        private void HandleMasterStateChanged(bool isOn)
        {
            if (audioType == AudioType.Master)
            {
                bool bothOn = AudioManager.Instance.IsBGMEnabled && AudioManager.Instance.IsSFXEnabled;
                UpdateSprite(bothOn);
            }
        }

        private void UpdateSprite(bool isOn)
        {
            if (buttonImage == null) return;

            Sprite spriteToUse = isOn ? onSprite : offSprite;

            if (spriteToUse != null)
            {
                buttonImage.sprite = spriteToUse;
            }
        }

        public enum AudioType
        {
            BGM,
            SFX,
            Master
        }
    }
}
