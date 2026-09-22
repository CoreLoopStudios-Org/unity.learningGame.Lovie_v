using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class ParentActivityCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image activityImage;

        public void Setup(string displayName, string typeLabel, string timeLabel)
        {
            if (nameText != null)
                nameText.text = displayName;

            if (typeText != null)
                typeText.text = typeLabel;

            if (timeText != null)
                timeText.text = timeLabel;
        }

        public void Clear()
        {
            if (nameText != null) nameText.text = string.Empty;
            if (typeText != null) typeText.text = string.Empty;
            if (timeText != null) timeText.text = string.Empty;
        }
    }
}
