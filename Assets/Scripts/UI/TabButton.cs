using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace UI
{
    public class TabButton : MonoBehaviour
    {
        [Header("Tab Settings")]
        [SerializeField] private string tabId;
        [SerializeField] private TabNavigationController.ContentType contentType;

        [Header("Visual Elements")]
        [SerializeField] private Image buttonImage; // Figma image with Stroke Width
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Image buttonIcon;

        [Header("Appearance")]
        [SerializeField] private Color selectedColor = new Color(0.608f, 0.364f, 0.835f);
        [SerializeField] private Color defaultColor = new Color(0.459f, 0.459f, 0.51f);
        [SerializeField] private float selectedStrokeWidth = 4f;
        [SerializeField] private float defaultStrokeWidth = 0f;

        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private bool animateChanges = true;

        private bool isSelected = false;

        private void Awake()
        {
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();

            if (buttonText == null)
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Start()
        {
            SetSelected(false, false);
        }

        public void SetSelected(bool selected, bool animate = true)
        {
            isSelected = selected;
            Color targetColor = selected ? selectedColor : defaultColor;
            float targetStroke = selected ? selectedStrokeWidth : defaultStrokeWidth;

            if (animate && animateChanges)
            {
                StartCoroutine(AnimateSelection(targetColor, targetStroke));
            }
            else
            {
                ApplyAppearance(targetColor, targetStroke);
            }
        }

        private IEnumerator AnimateSelection(Color targetColor, float targetStroke)
        {
            float elapsed = 0f;

            Color startIconColor = buttonIcon != null ? buttonIcon.color : Color.white;
            Color startTextColor = buttonText != null ? buttonText.color : Color.white;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;

                if (buttonIcon != null)
                {
                    buttonIcon.color = Color.Lerp(startIconColor, targetColor, progress);
                }

                if (buttonText != null)
                {
                    buttonText.color = Color.Lerp(startTextColor, targetColor, progress);
                }

                yield return null;
            }

            ApplyAppearance(targetColor, targetStroke);
        }

        private void ApplyAppearance(Color color, float strokeWidth)
        {
            if (buttonIcon != null)
            {
                buttonIcon.color = color;
            }

            if (buttonText != null)
            {
                buttonText.color = color;
            }

            ApplyStrokeWidth(strokeWidth);
        }

        private void ApplyStrokeWidth(float strokeWidth)
        {
            if (buttonImage != null)
            {
                FigmaImageHelper.SetStrokeWidth(buttonImage, strokeWidth);
            }
            if (buttonImage != null)
            {
                Outline outline = buttonImage.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectDistance = new Vector2(strokeWidth, strokeWidth);
                }
            }
        }

        public TabNavigationController.ContentType GetContentType()
        {
            return contentType;
        }

        public string GetTabId()
        {
            return tabId;
        }

        public bool IsSelected()
        {
            return isSelected;
    }

        public void SetContentType(TabNavigationController.ContentType type)
        {
            contentType = type;
        }

        public void ToggleSelection()
        {
            SetSelected(!isSelected);
        }
    }
}
