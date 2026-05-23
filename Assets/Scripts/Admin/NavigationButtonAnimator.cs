using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Admin
{
    /// <summary>
    /// Handles hover and click animations for navigation buttons.
    /// Attach this to each navigation button GameObject.
    /// </summary>
    public class NavigationButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float clickScale = 0.95f;
        [SerializeField] private float animationDuration = 0.15f;

        [Header("Color Settings")]
        [SerializeField] private bool useColorTransition = true;
        [SerializeField] private Color hoverColor = new Color(0.708f, 0.5f, 0.9f); // Lighter purple
        [SerializeField] private Color normalColor = new Color(0.459f, 0.459f, 0.51f); // Default gray
        [SerializeField] private Color selectedColor = new Color(0.608f, 0.364f, 0.835f); // #9B5DE5

        [Header("References")]
        [SerializeField] private Image targetImage;
        [SerializeField] private bool isSelected = false;

        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Coroutine currentAnimation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = transform.parent?.GetComponent<RectTransform>();
            }

            originalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void Start()
        {
            if (isSelected && targetImage != null && useColorTransition)
            {
                targetImage.color = selectedColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected)
            {
                AnimateScale(hoverScale);
                if (useColorTransition && targetImage != null)
                {
                    CrossFadeColor(hoverColor);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected)
            {
                AnimateScale(1f);
                if (useColorTransition && targetImage != null)
                {
                    CrossFadeColor(normalColor);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isSelected)
            {
                AnimateScale(clickScale);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isSelected)
            {
                AnimateScale(hoverScale);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (useColorTransition && targetImage != null)
            {
                CrossFadeColor(selected ? selectedColor : normalColor);
            }

            AnimateScale(selected ? 1.1f : 1f);
        }

        private void AnimateScale(float targetScale)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(ScaleAnimation(targetScale));
        }

        private System.Collections.IEnumerator ScaleAnimation(float targetScale)
        {
            if (rectTransform == null)
                yield break;

            float elapsed = 0f;
            Vector3 startScale = rectTransform.localScale;
            Vector3 endScale = originalScale * targetScale;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float easedT = EaseOutQuad(t);
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                yield return null;
            }

            rectTransform.localScale = endScale;
        }

        private void CrossFadeColor(Color targetColor)
        {
            if (targetImage == null)
                return;

#if UNITY_EDITOR
            targetImage.CrossFadeColor(targetColor, animationDuration, true, true);
#else
            StartCoroutine(ColorFadeAnimation(targetColor));
#endif
        }

        private System.Collections.IEnumerator ColorFadeAnimation(Color targetColor)
        {
            if (targetImage == null)
                yield break;

            float elapsed = 0f;
            Color startColor = targetImage.color;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                targetImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            targetImage.color = targetColor;
        }

        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
