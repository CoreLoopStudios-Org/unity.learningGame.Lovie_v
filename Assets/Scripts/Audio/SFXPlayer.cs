using UnityEngine;
using Audio;

namespace Audio
{
    /// <summary>
    /// Static helper class for playing sound effects from anywhere in the code.
    /// </summary>
    public static class SFXPlayer
    {
        /// <summary>
        /// Plays a sound effect by name from Resources/Audio/SFX folder.
        /// </summary>
        public static void Play(string sfxName)
        {
            if (AudioManager.Instance == null) return;

            AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{sfxName}");
            if (clip != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
            else
            {
                Debug.LogWarning($"SFX not found: {sfxName}");
            }
        }

        /// <summary>
        /// Plays a sound effect with custom volume scale.
        /// </summary>
        public static void Play(string sfxName, float volumeScale)
        {
            if (AudioManager.Instance == null) return;

            AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{sfxName}");
            if (clip != null)
            {
                AudioManager.Instance.PlaySFX(clip, volumeScale);
            }
            else
            {
                Debug.LogWarning($"SFX not found: {sfxName}");
            }
        }

        /// <summary>
        /// Plays a sound effect from a direct AudioClip reference.
        /// </summary>
        public static void PlayClip(AudioClip clip)
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySFX(clip);
        }

        /// <summary>
        /// Plays a sound effect from a direct AudioClip reference with custom volume.
        /// </summary>
        public static void PlayClip(AudioClip clip, float volumeScale)
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySFX(clip, volumeScale);
        }

        /// <summary>
        /// Plays a BGM by name from Resources/Audio/BGM folder.
        /// </summary>
        public static void PlayBGM(string bgmName)
        {
            if (AudioManager.Instance == null) return;

            AudioClip clip = Resources.Load<AudioClip>($"Audio/BGM/{bgmName}");
            if (clip != null)
            {
                AudioManager.Instance.PlayBGM(clip);
            }
            else
            {
                Debug.LogWarning($"BGM not found: {bgmName}");
            }
        }
    }
}
