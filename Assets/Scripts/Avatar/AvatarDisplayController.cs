using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Avatar
{
    /// <summary>
    /// Controls the visual display of the avatar
    /// Renders selected parts with proper layering and transforms
    /// </summary>
    public class AvatarDisplayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarCustomizationManager customizationManager;

        [Header("Avatar Display")]
        [SerializeField] private Transform avatarRoot;
        [SerializeField] private Image bodyBaseImage;              // Base body (always visible)

        [Header("Part Renderers (Optional)")]
        [SerializeField] private Image hairRenderer;
        [SerializeField] private Image dressRenderer;
        [SerializeField] private Image shoesRenderer;
        [SerializeField] private Image accessoriesRenderer;
        [SerializeField] private Image cosmeticsRenderer;

        [Header("Dynamic Rendering")]
        [SerializeField] private bool useDynamicRendering = true;
        [SerializeField] private GameObject partRendererPrefab;    // Prefab with Image component
        [SerializeField] private Transform partsContainer;          // Parent for dynamically created renderers

        [Header("Layer Settings")]
        [SerializeField] private int baseSortingOrder = 10;
        [SerializeField] private int layerIncrement = 1;

        // Runtime state
        private Dictionary<AvatarPartCategory, Image> partRenderers;
        private bool isInitialized = false;

        #region Initialization

        private void Awake()
        {
            partRenderers = new Dictionary<AvatarPartCategory, Image>();
        }

        private void Start()
        {
            Initialize();
            SubscribeToEvents();
            RefreshAvatarDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Initialize()
        {
            if (useDynamicRendering && partRendererPrefab != null && partsContainer != null)
            {
                SetupDynamicRenderers();
            }
            else
            {
                SetupStaticRenderers();
            }

            isInitialized = true;
        }

        private void SetupStaticRenderers()
        {
            // Map existing image components to categories
            if (hairRenderer != null)
                partRenderers[AvatarPartCategory.Hair] = hairRenderer;

            if (dressRenderer != null)
                partRenderers[AvatarPartCategory.Dress] = dressRenderer;

            if (shoesRenderer != null)
                partRenderers[AvatarPartCategory.Shoes] = shoesRenderer;

            if (accessoriesRenderer != null)
                partRenderers[AvatarPartCategory.Accessories] = accessoriesRenderer;

            if (cosmeticsRenderer != null)
                partRenderers[AvatarPartCategory.Cosmetics] = cosmeticsRenderer;

            // Body color is special - it tints the base body
            if (bodyBaseImage != null)
            {
                partRenderers[AvatarPartCategory.BodyColor] = bodyBaseImage;
            }
        }

        private void SetupDynamicRenderers()
        {
            // Create renderer for each category
            var categories = Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];

            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None) continue;

                GameObject rendererObj = Instantiate(partRendererPrefab, partsContainer);
                rendererObj.name = $"{category}_Renderer";

                Image rendererImage = rendererObj.GetComponent<Image>();
                if (rendererImage != null)
                {
                    int sortingOrder = baseSortingOrder + ((int)category * layerIncrement);
                    SetSortingOrder(rendererImage, sortingOrder);

                    partRenderers[category] = rendererImage;

                    // Body color category uses the base body
                    if (category == AvatarPartCategory.BodyColor && bodyBaseImage != null)
                    {
                        partRenderers[category] = bodyBaseImage;
                        Destroy(rendererObj);
                    }
                }
            }
        }

        private void SetSortingOrder(Image image, int sortingOrder)
        {
            // Set sorting order for proper layering
            // This works with Canvas sorting or custom layers
            if (image.transform is RectTransform rectTransform)
            {
                rectTransform.SetSiblingIndex(sortingOrder);
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            if (customizationManager != null)
            {
                customizationManager.OnPartSelected += OnPartSelected;
                customizationManager.OnAvatarLoaded += OnAvatarLoaded;
                customizationManager.OnAvatarReset += OnAvatarReset;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (customizationManager != null)
            {
                customizationManager.OnPartSelected -= OnPartSelected;
                customizationManager.OnAvatarLoaded -= OnAvatarLoaded;
                customizationManager.OnAvatarReset -= OnAvatarReset;
            }
        }

        #endregion

        #region Event Handlers

        private void OnPartSelected(AvatarPartCategory category, AvatarPartItem part)
        {
            UpdatePartDisplay(category, part);
        }

        private void OnAvatarLoaded()
        {
            RefreshAvatarDisplay();
        }

        private void OnAvatarReset()
        {
            RefreshAvatarDisplay();
        }

        #endregion

        #region Display Updates

        /// <summary>
        /// Refresh the entire avatar display with current selections
        /// </summary>
        public void RefreshAvatarDisplay()
        {
            if (!isInitialized) return;

            var selections = customizationManager?.GetAllSelections();
            if (selections == null) return;

            foreach (var kvp in selections)
            {
                UpdatePartDisplay(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Update display for a specific category
        /// </summary>
        private void UpdatePartDisplay(AvatarPartCategory category, AvatarPartItem part)
        {
            if (part == null) return;

            if (!partRenderers.TryGetValue(category, out Image renderer))
            {
                Debug.LogWarning($"No renderer found for category: {category}");
                return;
            }

            if (renderer == null) return;

            // Handle body color differently (tint instead of sprite change)
            if (category == AvatarPartCategory.BodyColor && renderer == bodyBaseImage)
            {
                // If body color part has a sprite, use it
                // Otherwise, you might want to tint the existing sprite
                if (part.AvatarSprite != null)
                {
                    renderer.sprite = part.AvatarSprite;
                }
                return;
            }

            // Update sprite for other categories
            if (part.AvatarSprite != null)
            {
                renderer.sprite = part.AvatarSprite;
                renderer.enabled = true;
            }
            else
            {
                // Hide renderer if no sprite
                renderer.enabled = false;
            }

            // Apply custom layer if specified
            if (!string.IsNullOrEmpty(part.CustomLayerName))
            {
                renderer.gameObject.layer = LayerMask.NameToLayer(part.CustomLayerName);
            }

            // Adjust sorting order if offset is specified
            if (part.SortingOrderOffset != 0)
            {
                SetSortingOrder(renderer, baseSortingOrder + ((int)category * layerIncrement) + part.SortingOrderOffset);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force refresh avatar display
        /// </summary>
        [ContextMenu("Refresh Avatar Display")]
        public void ForceRefresh()
        {
            RefreshAvatarDisplay();
        }

        /// <summary>
        /// Get the renderer for a specific category
        /// </summary>
        public Image GetRendererForCategory(AvatarPartCategory category)
        {
            return partRenderers.TryGetValue(category, out Image renderer) ? renderer : null;
        }

        /// <summary>
        /// Set the customization manager (useful for runtime assignment)
        /// </summary>
        public void SetCustomizationManager(AvatarCustomizationManager manager)
        {
            UnsubscribeFromEvents();
            customizationManager = manager;
            SubscribeToEvents();
            RefreshAvatarDisplay();
        }

        #endregion

        #region Snapshots

        /// <summary>
        /// Capture current avatar state as a texture
        /// </summary>
        public Texture2D CaptureAvatarSnapshot(int width = 512, int height = 512)
        {
            if (avatarRoot == null) return null;

            // This is a basic implementation
            // For production, consider using RenderTexture with a separate camera
            // or a UI-to-Texture capture solution

            return null; // TODO: Implement snapshot functionality
        }

        #endregion
    }
}
