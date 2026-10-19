using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    /// <summary>
    /// Speaks the text of a SpeechTextSource when the button is clicked
    /// (tapping again restarts the current text). Assign the source in the
    /// inspector; otherwise the nearest one in parents is used, then any
    /// active one in the scene.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SpeechButton : MonoBehaviour
    {
        [SerializeField] private SpeechTextSource source;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (source == null) source = GetComponentInParent<SpeechTextSource>();
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(HandleClicked);
        }

        private void HandleClicked()
        {
            if (source == null)
            {
                source = FindFirstObjectByType<SpeechTextSource>();
                if (source == null)
                {
                    Debug.LogWarning("SpeechButton: no SpeechTextSource found — add one to the text field it should read.", this);
                    return;
                }
            }

            string text = source.GetSpeechText();
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("SpeechButton: the linked text field is empty, nothing to speak.", this);
                return;
            }

            MobileTTS.Instance?.Speak(text);
        }
    }
}
