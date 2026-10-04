using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityFigmaBridge.Runtime.UI;

namespace UI
{
    /// <summary>
    /// One answer option row: figma background, numbering badge, answer/letter texts.
    /// Owns the option's Button and applies the correct/wrong highlight colors,
    /// restoring its prefab defaults between questions.
    /// </summary>
    public class QuizOptionView : MonoBehaviour
    {
        [Serializable]
        public class Theme
        {
            [Header("Correct")]
            public Color correctStroke = new Color(0.39607844f, 0.68235296f, 0.42352942f); // 65AE6C
            public Color correctFill = new Color(0.88235295f, 0.9254902f, 0.8745098f);     // E1ECDF
            public Color correctLetter = new Color(0.294f, 0.686f, 0.314f);
            public Color correctBadgeStroke = new Color(0.39607844f, 0.68235296f, 0.42352942f);
            public Color correctBadgeFill = new Color(0.88235295f, 0.9254902f, 0.8745098f);

            [Header("Wrong")]
            public Color wrongStroke = new Color(0.7529412f, 0.2235294f, 0.1686275f);      // C0392B
            public Color wrongFill = new Color(0.9921569f, 0.9254902f, 0.9176471f);        // FDECEA
            public Color wrongLetter = new Color(0.7529412f, 0.2235294f, 0.1686275f);
            public Color wrongBadgeStroke = new Color(0.7529412f, 0.2235294f, 0.1686275f);
            public Color wrongBadgeFill = new Color(0.9921569f, 0.9254902f, 0.9176471f);
        }

        [Header("References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image numberBadgeImage;
        [SerializeField] private TMP_Text answerText;
        [SerializeField] private TMP_Text numberText;

        private Theme theme;
        private Button button;

        private Color defaultStroke;
        private Color defaultFill;
        private Color defaultBadgeStroke;
        private Color defaultBadgeFill;
        private Color defaultLetter;

        public event Action Clicked;

        private void Awake()
        {
            defaultStroke = GetStrokeColor(backgroundImage);
            defaultFill = GetFillColor(backgroundImage);
            defaultBadgeStroke = GetStrokeColor(numberBadgeImage);
            defaultBadgeFill = GetFillColor(numberBadgeImage);
            defaultLetter = numberText != null ? numberText.color : Color.white;

            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

            // The Figma shader renders fill/stroke itself — Unity's tint would fight it.
            button.transition = Selectable.Transition.None;
            button.targetGraphic = null;
            button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Configure(Theme value)
        {
            theme = value;
        }

        public void SetAnswer(string text)
        {
            if (answerText != null)
            {
                answerText.text = text ?? string.Empty;
            }
        }

        public void ShowCorrect()
        {
            Apply(theme.correctStroke, theme.correctFill, theme.correctBadgeStroke, theme.correctBadgeFill, theme.correctLetter);
        }

        public void ShowWrong()
        {
            Apply(theme.wrongStroke, theme.wrongFill, theme.wrongBadgeStroke, theme.wrongBadgeFill, theme.wrongLetter);
        }

        public void ResetVisual()
        {
            Apply(defaultStroke, defaultFill, defaultBadgeStroke, defaultBadgeFill, defaultLetter);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private void HandleClicked()
        {
            Clicked?.Invoke();
        }

        private void Apply(Color stroke, Color fill, Color badgeStroke, Color badgeFill, Color letter)
        {
            SetColors(backgroundImage, stroke, fill);
            SetColors(numberBadgeImage, badgeStroke, badgeFill);

            if (numberText != null)
            {
                numberText.color = letter;
            }
        }

        private static void SetColors(Image image, Color stroke, Color fill)
        {
            if (image == null) return;

            if (image is FigmaImage figmaImage)
            {
                figmaImage.StrokeColor = stroke;
                figmaImage.FillColor = fill;
            }
            else
            {
                image.color = fill;
            }
        }

        private static Color GetStrokeColor(Image image)
        {
            if (image == null) return Color.white;
            return image is FigmaImage figmaImage ? figmaImage.StrokeColor : image.color;
        }

        private static Color GetFillColor(Image image)
        {
            if (image == null) return Color.white;
            return image is FigmaImage figmaImage ? figmaImage.FillColor : image.color;
        }
    }
}
