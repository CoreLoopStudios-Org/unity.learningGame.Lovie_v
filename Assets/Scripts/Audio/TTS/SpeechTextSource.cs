using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Audio
{
    /// <summary>
    /// Marks a text field as the source of spoken text. Attach to the same
    /// GameObject as the text whose content should be read aloud. Whatever the
    /// text currently displays (e.g. the visible story page) is what gets spoken.
    /// </summary>
    public class SpeechTextSource : MonoBehaviour
    {
        [SerializeField] private TMP_Text tmpText;
        [SerializeField] private Text legacyText;

        private void Awake()
        {
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
            if (tmpText == null && legacyText == null) legacyText = GetComponent<Text>();
        }

        public string GetSpeechText()
        {
            if (tmpText != null) return tmpText.text;
            if (legacyText != null) return legacyText.text;
            return string.Empty;
        }
    }
}
