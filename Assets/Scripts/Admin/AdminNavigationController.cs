using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace UI
{
    public class AdminNavigationController : MonoBehaviour
    {
        [System.Serializable]
        public class NavigationButton
        {
            public Button button;
            public TextMeshProUGUI buttonText;
            public Image buttonIcon;
            public AdminPageType pageType;
        }

        [System.Serializable]
        public class PagePanel
        {
            public AdminPageType pageType;
            public GameObject pageObject;
            public CanvasGroup canvasGroup;
        }

        [Header("Navigation Buttons")]
        [SerializeField] private NavigationButton[] navigationButtons;

        [Header("Page Panels")]
        [SerializeField] private PagePanel[] pagePanels;

        [Header("Settings")]
        [SerializeField] private Color selectedColor = new Color(0.608f, 0.364f, 0.835f); // #9B5DE5
        [SerializeField] private Color defaultColor = new Color(0.459f, 0.459f, 0.51f); // Default gray
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float scaleDuration = 0.2f;
        [SerializeField] private float selectedScale = 1.1f;

        private AdminPageType currentPage = AdminPageType.Home;
        private bool isTransitioning = false;

        private void Start()
        {
            InitializeNavigation();
            ShowPage(AdminPageType.Home, false);
        }

        private void InitializeNavigation()
        {
            foreach (var navButton in navigationButtons)
            {
                if (navButton.button != null)
                {
                    navButton.button.onClick.AddListener(() => OnNavigationButtonClicked(navButton));
                }
            }
        }

        private void OnNavigationButtonClicked(NavigationButton clickedButton)
        {
            if (isTransitioning || clickedButton.pageType == currentPage)
                return;

            ShowPage(clickedButton.pageType, true);
        }

        public void ShowPage(AdminPageType pageType, bool animate = true)
        {
            if (isTransitioning)
                return;

            StartCoroutine(TransitionToPage(pageType, animate));
        }

        private IEnumerator TransitionToPage(AdminPageType pageType, bool animate)
        {
            isTransitioning = true;

            PagePanel currentPagePanel = GetPagePanel(currentPage);
            PagePanel newPagePanel = GetPagePanel(pageType);

            if (currentPagePanel != null && currentPagePanel.pageObject != null)
            {
                if (animate)
                {
                    yield return StartCoroutine(FadeOutPage(currentPagePanel));
                }
                currentPagePanel.pageObject.SetActive(false);
            }

            UpdateButtonStates(currentPage, pageType);

            currentPage = pageType;

            if (newPagePanel != null && newPagePanel.pageObject != null)
            {
                newPagePanel.pageObject.SetActive(true);

                if (animate && newPagePanel.canvasGroup != null)
                {
                    yield return StartCoroutine(FadeInPage(newPagePanel));
                }
            }

            isTransitioning = false;
        }

        private IEnumerator FadeOutPage(PagePanel pagePanel)
        {
            if (pagePanel.canvasGroup == null)
            {
                // Create canvas group if not exists
                pagePanel.canvasGroup = pagePanel.pageObject.GetComponent<CanvasGroup>();
                if (pagePanel.canvasGroup == null)
                {
                    pagePanel.canvasGroup = pagePanel.pageObject.AddComponent<CanvasGroup>();
                }
            }

            float elapsed = 0f;
            float startAlpha = pagePanel.canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeDuration;
                pagePanel.canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }

            pagePanel.canvasGroup.alpha = 0f;
        }

        private IEnumerator FadeInPage(PagePanel pagePanel)
        {
            if (pagePanel.canvasGroup == null)
            {
                pagePanel.canvasGroup = pagePanel.pageObject.GetComponent<CanvasGroup>();
                if (pagePanel.canvasGroup == null)
                {
                    pagePanel.canvasGroup = pagePanel.pageObject.AddComponent<CanvasGroup>();
                }
            }

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeDuration;
                pagePanel.canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                yield return null;
            }

            pagePanel.canvasGroup.alpha = 1f;
        }

        private void UpdateButtonStates(AdminPageType oldPage, AdminPageType newPage)
        {
            NavigationButton oldButton = GetNavigationButton(oldPage);
            if (oldButton != null)
            {
                SetButtonColor(oldButton, defaultColor);
                SetButtonScale(oldButton, 1f);
            }

            NavigationButton newButton = GetNavigationButton(newPage);
            if (newButton != null)
            {
                SetButtonColor(newButton, selectedColor);

                if (scaleDuration > 0)
                {
                    StartCoroutine(AnimateButtonScale(newButton, 1f, selectedScale));
                }
                else
                {
                    SetButtonScale(newButton, selectedScale);
                }
            }
        }

        private void SetButtonColor(NavigationButton navButton, Color color)
        {
            if (navButton.buttonText != null)
            {
                navButton.buttonText.color = color;
            }

            if (navButton.buttonIcon != null)
            {
                navButton.buttonIcon.color = color;
            }
        }

        private void SetButtonScale(NavigationButton navButton, float scale)
        {
            if (navButton.button != null)
            {
                RectTransform rectTransform = navButton.button.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one * scale;
                }
            }
        }

        private IEnumerator AnimateButtonScale(NavigationButton navButton, float startScale, float endScale)
        {
            if (navButton == null || navButton.button == null)
                yield break;

            RectTransform rectTransform = navButton.button.GetComponent<RectTransform>();
            if (rectTransform == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / scaleDuration;
                float easedProgress = EaseOutBack(progress);
                float currentScale = Mathf.Lerp(startScale, endScale, easedProgress);
                rectTransform.localScale = Vector3.one * currentScale;
                yield return null;
            }

            rectTransform.localScale = Vector3.one * endScale;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private NavigationButton GetNavigationButton(AdminPageType pageType)
        {
            foreach (var navButton in navigationButtons)
            {
                if (navButton.pageType == pageType)
                    return navButton;
            }
            return null;
        }

        private PagePanel GetPagePanel(AdminPageType pageType)
        {
            foreach (var page in pagePanels)
            {
                if (page.pageType == pageType)
                    return page;
            }
            return null;
        }

        public AdminPageType GetCurrentPage()
        {
            return currentPage;
        }
    }

    [System.Serializable]
    public enum AdminPageType
    {
        Home,
        Stories,
        Users,
        Profile,
        Progress,
        Settings,
        Games,
        Store,
        Stories_Item,
        Avatar_Item,
        Coins_Item
    }
}
