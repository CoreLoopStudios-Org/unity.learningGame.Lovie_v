using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class AdminQuizQuestionCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI[] answerTexts = new TextMeshProUGUI[4];

        [Header("Correct Answer Highlight")]
        [SerializeField] private TextMeshProUGUI[] letterTexts = new TextMeshProUGUI[4];
        [SerializeField] private Image[] strokeImages = new Image[4];
        [SerializeField] private Image[] fillImages = new Image[4];
        [SerializeField] private Color correctLetterColor = new Color(0.294f, 0.686f, 0.314f);
        [SerializeField] private Color correctStrokeColor = new Color(0.39607844f, 0.68235296f, 0.42352942f); // 65AE6C
        [SerializeField] private Color correctFillColor = new Color(0.88235295f, 0.9254902f, 0.8745098f); // E1ECDF

        private Color[] defaultLetterColors;
        private Color[] defaultStrokeColors;
        private Color[] defaultFillColors;

        private void Awake()
        {
            defaultLetterColors = CaptureColors(letterTexts);
            defaultStrokeColors = CaptureColors(strokeImages);
            defaultFillColors = CaptureColors(fillImages);
        }

        private static Color[] CaptureColors(TextMeshProUGUI[] texts)
        {
            Color[] colors = new Color[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                colors[i] = texts[i] != null ? texts[i].color : Color.white;
            }
            return colors;
        }

        private static Color[] CaptureColors(Image[] images)
        {
            Color[] colors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                colors[i] = images[i] != null ? images[i].color : Color.white;
            }
            return colors;
        }

        public void Setup(string question, string[] options, int correctIndex)
        {
            if (questionText != null)
                questionText.text = question ?? string.Empty;

            for (int i = 0; i < answerTexts.Length; i++)
            {
                if (answerTexts[i] == null) continue;
                string option = options != null && i < options.Length ? options[i] : string.Empty;
                answerTexts[i].text = option;
            }

            for (int i = 0; i < letterTexts.Length; i++)
            {
                if (letterTexts[i] != null)
                    letterTexts[i].color = i == correctIndex ? correctLetterColor : defaultLetterColors[i];
            }

            for (int i = 0; i < strokeImages.Length; i++)
            {
                if (strokeImages[i] != null)
                    strokeImages[i].color = i == correctIndex ? correctStrokeColor : defaultStrokeColors[i];
            }

            for (int i = 0; i < fillImages.Length; i++)
            {
                if (fillImages[i] != null)
                    fillImages[i].color = i == correctIndex ? correctFillColor : defaultFillColors[i];
            }
        }
    }
}
