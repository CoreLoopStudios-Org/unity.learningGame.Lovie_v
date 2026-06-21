using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Avatar
{
    /// <summary>
    /// Component for avatar item selection buttons
    /// Handles display of icon, name, and selection state
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AvatarItemButton : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionOutline;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI lockText;

        [Header("Appearance")]
        [SerializeField] private Color selectedColor = new Color(0.6f, 0.8f, 1f);
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float selectedScale = 1.1f;

        [Header("Animation")]
        [SerializeField] private bool animateSelection = true;
        [SerializeField] private float animationDuration = 0.2f;

        private AvatarPartItem linkedItem;
        private Button button;
        private bool isSelected = false;
        private bool isLocked = false;

        #region Initialization

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        #endregion

        #region Setup

        /// <summary>
        /// Initialize the button with an avatar part item
        /// </summary>
        public void Setup(AvatarPartItem item, bool initiallySelected = false)
        {
            linkedItem = item;

            if (item == null) return;

            // Set icon
            if (iconImage != null && item.IconSprite != null)
            {
                iconImage.sprite = item.IconSprite;
            }

            // Set name
            if (nameText != null && !string.IsNullOrEmpty(item.DisplayName))
            {
                nameText.text = item.DisplayName;
            }

            // Check lock status
            CheckLockStatus();

            // Set initial selection state
            SetSelected(initiallySelected, false);

            // Setup button click
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClick);
            }
        }

        private void CheckLockStatus()
        {
            if (linkedItem == null) return;

            isLocked = linkedItem.IsLocked;

            if (isLocked)
            {
                if (lockText != null)
                {
                    lockText.text = GetLockText();
                    lockText.gameObject.SetActive(true);
                }

                if (button != null)
                {
                    button.interactable = false;
                }

                UpdateLockVisuals();
            }
            else
            {
                if (lockText != null)
                {
                    lockText.gameObject.SetActive(false);
                }

                if (button != null)
                {
                    button.interactable = true;
                }
            }
        }

        private string GetLockText()
        {
            if (linkedItem.RequiredCoins > 0)
            {
                return $"🔒 {linkedItem.RequiredCoins}";
            }
            else if (linkedItem.RequiredLevel > 0)
            {
                return $"🔒 Lv.{linkedItem.RequiredLevel}";
            }
            return "🔒";
        }

        #endregion

        #region Selection

        /// <summary>
        /// Set the selected state of this button
        /// </summary>
        public void SetSelected(bool selected, bool animate = true)
        {
            isSelected = selected;

            if (animate && animateSelection)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateSelectionChange(selected));
            }
            else
            {
                ApplySelectionState(selected);
            }
        }

        private System.Collections.IEnumerator AnimateSelectionChange(bool selected)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = selected ? Vector3.one * selectedScale : Vector3.one;

            Color startBgColor = backgroundImage != null ? backgroundImage.color : defaultColor;
            Color targetBgColor = selected ? selectedColor : (isLocked ? lockedColor : defaultColor);

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);

                transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(startBgColor, targetBgColor, t);
                }

                yield return null;
            }

            ApplySelectionState(selected);
        }

        private void ApplySelectionState(bool selected)
        {
            transform.localScale = selected ? Vector3.one * selectedScale : Vector3.one;

            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : (isLocked ? lockedColor : defaultColor);
            }

            if (selectionOutline != null)
            {
                selectionOutline.enabled = selected;
            }
        }

        private void UpdateLockVisuals()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = lockedColor;
            }

            if (iconImage != null)
            {
                iconImage.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }

        #endregion

        #region Interaction

        private void OnButtonClick()
        {
            if (isLocked) return;

            // Event will be handled by UI Controller
            // This button just provides visual feedback
        }

        #endregion

        #region Public API

        public AvatarPartItem LinkedItem => linkedItem;
        public bool IsSelected => isSelected;
        public bool IsLocked => isLocked;

        /// <summary>
        /// Refresh lock status (call when player level/coins change)
        /// </summary>
        public void RefreshLockStatus()
        {
            CheckLockStatus();
        }

        #endregion
    }
}
