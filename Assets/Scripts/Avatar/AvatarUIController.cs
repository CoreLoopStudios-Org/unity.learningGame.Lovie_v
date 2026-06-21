using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Avatar
{
    /// <summary>
    /// Controls the avatar customization UI
    /// Handles category buttons, item spawning, and selection feedback
    /// </summary>
    public class AvatarUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarCustomizationManager customizationManager;
        [SerializeField] private AvatarDisplayController avatarDisplay;

        [Header("Category Buttons Container")]
        [SerializeField] private Transform categoryButtonsContainer;

        [Header("Item Container")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemButtonPrefab;

        [Header("Item Button Visual Elements")]
        [SerializeField] private Color selectedColor = new Color(0.6f, 0.8f, 1f);
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float selectedScale = 1.1f;

        [Header("Loading Settings")]
        [SerializeField] private bool spawnItemsOnStart = false;

        // Runtime state
        private Dictionary<AvatarPartCategory, Button> categoryButtons;
        private List<GameObject> spawnedItemButtons;
        private AvatarPartCategory currentCategory;

        #region Initialization

        private void Awake()
        {
            categoryButtons = new Dictionary<AvatarPartCategory, Button>();
            spawnedItemButtons = new List<GameObject>();
        }

        private void Start()
        {
            SetupCategoryButtons();
            SubscribeToEvents();

            if (spawnItemsOnStart)
            {
                var firstCategory = customizationManager?.GetAvailableCategories()?.FirstOrDefault();
                if (firstCategory.HasValue)
                {
                    ShowCategoryItems(firstCategory.Value);
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Setup

        private void SetupCategoryButtons()
        {
            if (categoryButtonsContainer == null) return;

            // Clear existing buttons
            foreach (Transform child in categoryButtonsContainer)
            {
                Destroy(child.gameObject);
            }
            categoryButtons.Clear();

            // Create category buttons
            var categories = customizationManager?.GetAvailableCategories();
            if (categories == null) return;

            foreach (var category in categories)
            {
                CreateCategoryButton(category);
            }
        }

        private void CreateCategoryButton(AvatarPartCategory category)
        {
            // Find or create button from container
            // Assumes buttons are already set up in the prefab
            Button button = null;

            // Try to find existing button by name
            foreach (Transform child in categoryButtonsContainer)
            {
                if (child.name.Contains(category.GetDisplayName()) ||
                    child.name.Contains(category.ToString()))
                {
                    button = child.GetComponent<Button>();
                    break;
                }
            }

            if (button != null)
            {
                categoryButtons[category] = button;
                int categoryIndex = (int)category;
                button.onClick.AddListener(() => OnCategoryButtonClicked(category));
            }
        }

        private void SubscribeToEvents()
        {
            if (customizationManager != null)
            {
                customizationManager.OnPartSelected += OnPartSelected;
                customizationManager.OnAvatarLoaded += OnAvatarLoaded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (customizationManager != null)
            {
                customizationManager.OnPartSelected -= OnPartSelected;
                customizationManager.OnAvatarLoaded -= OnAvatarLoaded;
            }
        }

        #endregion

        #region Category Selection

        public void ShowCategoryItems(AvatarPartCategory category)
        {
            currentCategory = category;
            ClearItemContainer();
            SpawnItemButtons(category);
            UpdateCategoryButtonVisuals(category);
        }

        private void OnCategoryButtonClicked(AvatarPartCategory category)
        {
            ShowCategoryItems(category);
        }

        private void UpdateCategoryButtonVisuals(AvatarPartCategory selectedCategory)
        {
            foreach (var kvp in categoryButtons)
            {
                bool isSelected = kvp.Key == selectedCategory;
                // Update button visuals here if needed
                // Could use TabButton component or custom visual updates
            }
        }

        #endregion

        #region Item Spawning

        private void SpawnItemButtons(AvatarPartCategory category)
        {
            var parts = customizationManager?.GetPartsForCategory(category);
            if (parts == null || parts.Count == 0) return;

            foreach (var part in parts)
            {
                CreateItemButton(part);
            }
        }

        private void CreateItemButton(AvatarPartItem part)
        {
            if (itemButtonPrefab == null || itemContainer == null) return;

            GameObject itemButton = Instantiate(itemButtonPrefab, itemContainer);
            spawnedItemButtons.Add(itemButton);

            // Try to use AvatarItemButton component if available
            AvatarItemButton avatarButton = itemButton.GetComponent<AvatarItemButton>();
            if (avatarButton != null)
            {
                bool isSelected = customizationManager?.GetSelectedPartId(currentCategory) == part.ItemId;
                avatarButton.Setup(part, isSelected);

                // Setup button click through the AvatarItemButton's Button component
                Button button = itemButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnItemButtonClicked(part));
                }
            }
            else
            {
                // Fallback to manual setup
                Button button = itemButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnItemButtonClicked(part));
                }

                // Setup icon
                Image iconImage = itemButton.GetComponentInChildren<Image>();
                if (iconImage != null && part.IconSprite != null)
                {
                    iconImage.sprite = part.IconSprite;
                }

                // Check if currently selected
                UpdateItemButtonSelection(itemButton, part);
            }
        }

        private void OnItemButtonClicked(AvatarPartItem part)
        {
            if (customizationManager != null)
            {
                customizationManager.SelectPart(part.Category, part);
            }

            // Update button visuals
            UpdateAllItemButtonVisuals();
        }

        private void UpdateItemButtonSelection(GameObject itemButton, AvatarPartItem selectedPart)
        {
            if (itemButton == null) return;

            AvatarItemButton avatarButton = itemButton.GetComponent<AvatarItemButton>();
            if (avatarButton != null)
            {
                // AvatarItemButton handles its own visuals
                return;
            }

            bool isSelected = customizationManager?.GetSelectedPartId(currentCategory) == selectedPart.ItemId;
            UpdateItemButtonVisual(itemButton, isSelected);
        }

        private void UpdateAllItemButtonVisuals()
        {
            var parts = customizationManager?.GetPartsForCategory(currentCategory);
            if (parts == null) return;

            for (int i = 0; i < spawnedItemButtons.Count && i < parts.Count; i++)
            {
                AvatarItemButton avatarButton = spawnedItemButtons[i].GetComponent<AvatarItemButton>();
                if (avatarButton != null)
                {
                    bool isSelected = customizationManager?.GetSelectedPartId(currentCategory) == parts[i].ItemId;
                    avatarButton.SetSelected(isSelected);
                }
                else
                {
                    bool isSelected = customizationManager?.GetSelectedPartId(currentCategory) == parts[i].ItemId;
                    UpdateItemButtonVisual(spawnedItemButtons[i], isSelected);
                }
            }
        }

        private void UpdateItemButtonVisual(GameObject button, bool isSelected)
        {
            if (button == null) return;

            AvatarItemButton avatarButton = button.GetComponent<AvatarItemButton>();
            if (avatarButton != null)
            {
                avatarButton.SetSelected(isSelected);
                return;
            }

            Transform imageTransform = button.GetComponentInChildren<Image>()?.transform;
            if (imageTransform != null)
            {
                imageTransform.localScale = isSelected ? Vector3.one * selectedScale : Vector3.one;
            }

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isSelected ? selectedColor : defaultColor;
            }
        }

        private void ClearItemContainer()
        {
            foreach (var button in spawnedItemButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            spawnedItemButtons.Clear();
        }

        #endregion

        #region Event Handlers

        private void OnPartSelected(AvatarPartCategory category, AvatarPartItem part)
        {
            // Update item button visuals if the changed category is currently shown
            if (category == currentCategory)
            {
                UpdateAllItemButtonVisuals();
            }
        }

        private void OnAvatarLoaded()
        {
            // Refresh current category display
            if (currentCategory != AvatarPartCategory.None)
            {
                ShowCategoryItems(currentCategory);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current category
        /// </summary>
        public AvatarPartCategory CurrentCategory => currentCategory;

        /// <summary>
        /// Show category by category index (useful for Unity Events)
        /// </summary>
        public void ShowCategoryByIndex(int categoryIndex)
        {
            if (Enum.IsDefined(typeof(AvatarPartCategory), categoryIndex))
            {
                ShowCategoryItems((AvatarPartCategory)categoryIndex);
            }
        }

        #endregion
    }
}
