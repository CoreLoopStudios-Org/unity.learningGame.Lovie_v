using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace UI
{
    public class TabNavigationController : MonoBehaviour
    {
        [System.Serializable]
        public class TabButton
        {
            public string tabName;
            public UnityEngine.UI.Button button;
            public Image buttonImage;
            public TextMeshProUGUI buttonText;
            public Image buttonIcon;
            public ContentType contentType;
        }

        [System.Serializable]
        public class TabContent
        {
            public ContentType contentType;
            public GameObject contentObject;
            public CanvasGroup canvasGroup;
        }

        [System.Serializable]
        public enum ContentType
        {
            Stories,
            AvatarItems,
            Coins,
            Kids_User,
            Parent_User
        }

        [Header("Tab Buttons")]
        [SerializeField] private TabButton[] tabButtons;

        [Header("Tab Content Panels")]
        [SerializeField] private TabContent[] tabContents;

        [Header("Appearance Settings")]
        [SerializeField] private Color selectedColor = new Color(0.608f, 0.364f, 0.835f);
        [SerializeField] private Color defaultColor = new Color(0.459f, 0.459f, 0.51f);
        [SerializeField] private float selectedStrokeWidth = 4f;
        [SerializeField] private float defaultStrokeWidth = 0f;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float colorChangeDuration = 0.2f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve colorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private ContentType currentTab = ContentType.Stories;
        private bool isTransitioning = false;

        private void Start()
        {
            InitializeNavigation();
            ShowTab(ContentType.Stories, false);
        }

        private void InitializeNavigation()
        {
            foreach (var tabButton in tabButtons)
            {
                if (tabButton.button != null)
                {
                    ContentType contentType = tabButton.contentType;
                    tabButton.button.onClick.AddListener(() => OnTabButtonClicked(contentType));
                }
            }
        }

        private void OnTabButtonClicked(ContentType contentType)
        {
            if (isTransitioning || contentType == currentTab)
                return;

            ShowTab(contentType, true);
        }

        public void ShowTab(ContentType contentType, bool animate = true)
        {
            if (isTransitioning)
                return;

            StartCoroutine(TransitionToTab(contentType, animate));
        }

        private IEnumerator TransitionToTab(ContentType contentType, bool animate)
        {
            isTransitioning = true;

            TabContent currentContent = GetTabContent(currentTab);
            TabContent newContent = GetTabContent(contentType);

            if (currentContent != null && currentContent.contentObject != null)
            {
                if (animate)
                {
                    yield return StartCoroutine(FadeOutContent(currentContent));
                }
                currentContent.contentObject.SetActive(false);
            }

            UpdateButtonAppearances(currentTab, contentType);

            currentTab = contentType;

            if (newContent != null && newContent.contentObject != null)
            {
                newContent.contentObject.SetActive(true);

                if (animate && newContent.canvasGroup != null)
                {
                    yield return StartCoroutine(FadeInContent(newContent));
                }
            }

            isTransitioning = false;
        }

        private IEnumerator FadeOutContent(TabContent content)
        {
            if (content.canvasGroup == null)
            {
                content.canvasGroup = content.contentObject.GetComponent<CanvasGroup>();
                if (content.canvasGroup == null)
                {
                    content.canvasGroup = content.contentObject.AddComponent<CanvasGroup>();
                }
            }

            float elapsed = 0f;
            float startAlpha = content.canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float curvedT = fadeCurve.Evaluate(t);
                content.canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curvedT);
                yield return null;
            }

            content.canvasGroup.alpha = 0f;
        }

        private IEnumerator FadeInContent(TabContent content)
        {
            if (content.canvasGroup == null)
            {
                content.canvasGroup = content.contentObject.GetComponent<CanvasGroup>();
                if (content.canvasGroup == null)
                {
                    content.canvasGroup = content.contentObject.AddComponent<CanvasGroup>();
                }
            }

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float curvedT = fadeCurve.Evaluate(t);
                content.canvasGroup.alpha = Mathf.Lerp(0f, 1f, curvedT);
                yield return null;
            }

            content.canvasGroup.alpha = 1f;
        }

        private void UpdateButtonAppearances(ContentType oldTab, ContentType newTab)
        {
            TabButton oldButton = GetTabButton(oldTab);
            if (oldButton != null)
            {
                SetButtonAppearance(oldButton, false);
            }

            TabButton newButton = GetTabButton(newTab);
            if (newButton != null)
            {
                SetButtonAppearance(newButton, true);
            }
        }

        private void SetButtonAppearance(TabButton tabButton, bool isSelected)
        {
            Color targetColor = isSelected ? selectedColor : defaultColor;
            float targetStroke = isSelected ? selectedStrokeWidth : defaultStrokeWidth;

            if (tabButton.buttonIcon != null)
            {
                StartCoroutine(AnimateColorChange(tabButton.buttonIcon, targetColor));
            }

            if (tabButton.buttonText != null)
            {
                StartCoroutine(AnimateTextColorChange(tabButton.buttonText, targetColor));
            }

            UpdateFigmaStrokeWidth(tabButton.buttonImage, targetStroke);
        }

        private void UpdateFigmaStrokeWidth(Image figmaImage, float strokeWidth)
        {
            if (figmaImage == null)
                return;

            FigmaImageHelper.SetStrokeWidth(figmaImage, strokeWidth);
        }

        private IEnumerator AnimateColorChange(Image image, Color targetColor)
        {
            if (image == null)
                yield break;

            float elapsed = 0f;
            Color startColor = image.color;

            while (elapsed < colorChangeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / colorChangeDuration);
                float curvedT = colorCurve.Evaluate(t);
                image.color = Color.Lerp(startColor, targetColor, curvedT);
                yield return null;
            }

            image.color = targetColor;
        }

        private IEnumerator AnimateTextColorChange(TextMeshProUGUI text, Color targetColor)
        {
            if (text == null)
                yield break;

            float elapsed = 0f;
            Color startColor = text.color;

            while (elapsed < colorChangeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / colorChangeDuration);
                float curvedT = colorCurve.Evaluate(t);
                text.color = Color.Lerp(startColor, targetColor, curvedT);
                yield return null;
            }

            text.color = targetColor;
        }

        private TabButton GetTabButton(ContentType contentType)
        {
            foreach (var tabButton in tabButtons)
            {
                if (tabButton.contentType == contentType)
                    return tabButton;
            }
            return null;
        }

        private TabContent GetTabContent(ContentType contentType)
        {
            foreach (var content in tabContents)
            {
                if (content.contentType == contentType)
                    return content;
            }
            return null;
        }

        public ContentType GetCurrentTab()
        {
            return currentTab;
        }

        public void SetDefaultTab(ContentType contentType)
        {
            currentTab = contentType;
        }
    }
}
