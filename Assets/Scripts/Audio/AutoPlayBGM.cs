using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Place this on your Main Menu scene to auto-play background music.
    /// </summary>
    public class AutoPlayBGM : MonoBehaviour
    {
        [Header("Auto-play Settings")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float delaySeconds = 0.5f;

        private void Start()
        {
            if (playOnStart && AudioManager.Instance != null)
            {
                Invoke(nameof(PlayMusic), delaySeconds);
            }
        }

        private void PlayMusic()
        {
            // This plays the "Main Menu BGM" assigned in AudioManager
            AudioManager.Instance.PlayMainMenuBGM();
        }
    }
}
