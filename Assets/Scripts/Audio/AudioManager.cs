using UnityEngine;
using System;

namespace Audio
{
    /// <summary>
    /// Singleton AudioManager for handling background music and sound effects.
    /// Settings are persisted using PlayerPrefs.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Background Music Clips")]
        [SerializeField] private AudioClip mainMenuBGM;
        [SerializeField] private AudioClip gameBGM;
        [SerializeField] private AudioClip[] additionalBGM;

        [Header("Sound Effect Clips")]
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip buttonHoverSFX;
        [SerializeField] private AudioClip panelOpenSFX;
        [SerializeField] private AudioClip panelCloseSFX;
        [SerializeField] private AudioClip correctAnswerSFX;
        [SerializeField] private AudioClip wrongAnswerSFX;
        [SerializeField] private AudioClip starCollectSFX;
        [SerializeField] private AudioClip[] additionalSFX;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultBGMVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float defaultSFXVolume = 1f;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 1f;

        // PlayerPrefs Keys
        private const string BGM_ENABLED_KEY = "BGM_Enabled";
        private const string SFX_ENABLED_KEY = "SFX_Enabled";
        private const string BGM_VOLUME_KEY = "BGM_Volume";
        private const string SFX_VOLUME_KEY = "SFX_Volume";

        // Runtime state
        private bool isBGMEnabled = true;
        private bool isSFXEnabled = true;
        private float bgmVolume = 0.7f;
        private float sfxVolume = 1f;
        private AudioClip currentBGM;

        // Events for UI binding
        public event Action<bool> OnBGMStateChanged;
        public event Action<bool> OnSFXStateChanged;
        public event Action<float> OnBGMVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        public bool IsBGMEnabled => isBGMEnabled;
        public bool IsSFXEnabled => isSFXEnabled;
        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadSettings();
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        private void LoadSettings()
        {
            isBGMEnabled = PlayerPrefs.GetInt(BGM_ENABLED_KEY, 1) == 1;
            isSFXEnabled = PlayerPrefs.GetInt(SFX_ENABLED_KEY, 1) == 1;
            bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, defaultBGMVolume);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);

            bgmSource.volume = isBGMEnabled ? bgmVolume : 0f;
            sfxSource.volume = isSFXEnabled ? sfxVolume : 0f;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(BGM_ENABLED_KEY, isBGMEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SFX_ENABLED_KEY, isSFXEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.Save();
        }

        #region BGM Control

        /// <summary>
        /// Plays a specific background music clip with optional fade-in.
        /// </summary>
        public void PlayBGM(AudioClip clip, bool fadeIn = true)
        {
            if (clip == null) return;

            currentBGM = clip;

            if (!isBGMEnabled)
            {
                bgmSource.clip = clip;
                return;
            }

            if (fadeIn)
            {
                StartCoroutine(FadeInBGM(clip));
            }
            else
            {
                bgmSource.clip = clip;
                bgmSource.Play();
            }
        }

        /// <summary>
        /// Plays the main menu background music.
        /// </summary>
        public void PlayMainMenuBGM()
        {
            PlayBGM(mainMenuBGM);
        }

        /// <summary>
        /// Plays the game background music.
        /// </summary>
        public void PlayGameBGM()
        {
            PlayBGM(gameBGM);
        }

        /// <summary>
        /// Stops the current background music with optional fade-out.
        /// </summary>
        public void StopBGM(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutBGM());
            }
            else
            {
                bgmSource.Stop();
            }
        }

        /// <summary>
        /// Pauses the current background music.
        /// </summary>
        public void PauseBGM()
        {
            bgmSource.Pause();
        }

        /// <summary>
        /// Resumes the paused background music.
        /// </summary>
        public void ResumeBGM()
        {
            if (isBGMEnabled)
            {
                bgmSource.UnPause();
            }
        }

        /// <summary>
        /// Toggles BGM on/off and returns the new state.
        /// </summary>
        public bool ToggleBGM()
        {
            isBGMEnabled = !isBGMEnabled;
            SaveSettings();
            OnBGMStateChanged?.Invoke(isBGMEnabled);

            if (isBGMEnabled)
            {
                if (currentBGM != null && !bgmSource.isPlaying)
                {
                    StartCoroutine(FadeInBGM(currentBGM));
                }
                else
                {
                    StartCoroutine(FadeInVolume());
                }
            }
            else
            {
                StartCoroutine(FadeOutVolume());
            }

            return isBGMEnabled;
        }

        /// <summary>
        /// Sets BGM enabled state directly (useful for initial setup).
        /// </summary>
        public void SetBGMEnabled(bool enabled)
        {
            if (isBGMEnabled == enabled) return;

            isBGMEnabled = enabled;
            SaveSettings();
            OnBGMStateChanged?.Invoke(isBGMEnabled);

            if (isBGMEnabled)
            {
                if (currentBGM != null && !bgmSource.isPlaying)
                {
                    bgmSource.clip = currentBGM;
                    bgmSource.Play();
                }
                bgmSource.volume = bgmVolume;
            }
            else
            {
                bgmSource.volume = 0f;
            }
        }

        /// <summary>
        /// Sets the BGM volume (0-1).
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnBGMVolumeChanged?.Invoke(bgmVolume);

            if (isBGMEnabled)
            {
                bgmSource.volume = bgmVolume;
            }
        }

        #endregion

        #region SFX Control

        /// <summary>
        /// Plays a one-shot sound effect.
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || !isSFXEnabled) return;

            sfxSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Plays a sound effect with custom volume.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale)
        {
            if (clip == null || !isSFXEnabled) return;

            sfxSource.PlayOneShot(clip, volumeScale);
        }

        /// <summary>
        /// Toggles SFX on/off and returns the new state.
        /// </summary>
        public bool ToggleSFX()
        {
            isSFXEnabled = !isSFXEnabled;
            SaveSettings();
            OnSFXStateChanged?.Invoke(isSFXEnabled);

            sfxSource.volume = isSFXEnabled ? sfxVolume : 0f;

            return isSFXEnabled;
        }

        /// <summary>
        /// Sets SFX enabled state directly.
        /// </summary>
        public void SetSFXEnabled(bool enabled)
        {
            if (isSFXEnabled == enabled) return;

            isSFXEnabled = enabled;
            SaveSettings();
            OnSFXStateChanged?.Invoke(isSFXEnabled);

            sfxSource.volume = isSFXEnabled ? sfxVolume : 0f;
        }

        /// <summary>
        /// Sets the SFX volume (0-1).
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnSFXVolumeChanged?.Invoke(sfxVolume);

            if (isSFXEnabled)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        // Named SFX helpers for commonly used sounds
        public void PlayButtonClick() => PlaySFX(buttonClickSFX);
        public void PlayButtonHover() => PlaySFX(buttonHoverSFX);
        public void PlayPanelOpen() => PlaySFX(panelOpenSFX);
        public void PlayPanelClose() => PlaySFX(panelCloseSFX);
        public void PlayCorrectAnswer() => PlaySFX(correctAnswerSFX);
        public void PlayWrongAnswer() => PlaySFX(wrongAnswerSFX);
        public void PlayStarCollect() => PlaySFX(starCollectSFX);

        #endregion

        #region Fade Coroutines

        private System.Collections.IEnumerator FadeInBGM(AudioClip clip)
        {
            bgmSource.clip = clip;
            bgmSource.volume = 0f;
            bgmSource.Play();

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                bgmSource.volume = Mathf.Lerp(0f, isBGMEnabled ? bgmVolume : 0f, t);
                yield return null;
            }

            bgmSource.volume = isBGMEnabled ? bgmVolume : 0f;
        }

        private System.Collections.IEnumerator FadeOutBGM()
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = isBGMEnabled ? bgmVolume : 0f;
        }

        private System.Collections.IEnumerator FadeInVolume()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t);
                yield return null;
            }

            bgmSource.volume = bgmVolume;
        }

        private System.Collections.IEnumerator FadeOutVolume()
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            bgmSource.volume = 0f;
        }

        #endregion

        #region Runtime Adding of Clips

        /// <summary>
        /// Adds a BGM clip at runtime and optionally plays it.
        /// </summary>
        public void AddAndPlayBGM(AudioClip clip, bool playImmediately = true)
        {
            if (clip == null) return;

            if (playImmediately)
            {
                PlayBGM(clip);
            }
            else
            {
                currentBGM = clip;
            }
        }

        #endregion
    }
}
