using UnityEngine;

namespace Audio
{
    public class AutoPauseBGM : MonoBehaviour
    {
        [Header("Options")]
        [SerializeField] private bool pauseOnEnable = true;
        [SerializeField] private bool resumeOnDisable = true;

        private void OnEnable()
        {
            if (pauseOnEnable && AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseBGM();
            }
        }

        private void OnDisable()
        {
            if (resumeOnDisable && AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeBGM();
            }
        }
    }
}
