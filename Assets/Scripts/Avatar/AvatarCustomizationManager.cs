using UnityEngine;
using System;
using System.Collections.Generic;

namespace Avatar
{
    /// <summary>
    /// Runtime manager for avatar customization state
    /// Handles current selections and coordinate save/load operations
    /// </summary>
    public class AvatarCustomizationManager : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        [Header("Settings")]
        [SerializeField] private bool autoLoadOnStart = true;
        [SerializeField] private bool autoSaveOnSelection = true;
        [SerializeField] private string saveKeyPrefix = "AvatarCustomization";

        // Current selections
        private Dictionary<AvatarPartCategory, string> currentSelections;

        // Events
        public event Action<AvatarPartCategory, AvatarPartItem> OnPartSelected;
        public event Action<AvatarPartCategory> OnCategoryChanged;
        public event Action OnAvatarLoaded;
        public event Action OnAvatarReset;

        #region Initialization

        private void Awake()
        {
            currentSelections = new Dictionary<AvatarPartCategory, string>();
        }

        private void Start()
        {
            if (avatarDatabase != null && autoLoadOnStart)
            {
                LoadAvatar();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Select a part for a category
        /// </summary>
        public void SelectPart(AvatarPartCategory category, AvatarPartItem part)
        {
            if (part == null)
            {
                Debug.LogWarning($"Attempted to select null part for category {category}");
                return;
            }

            if (part.Category != category)
            {
                Debug.LogWarning($"Part category mismatch. Expected {category}, got {part.Category}");
                return;
            }

            currentSelections[category] = part.ItemId;
            OnPartSelected?.Invoke(category, part);
            OnCategoryChanged?.Invoke(category);

            if (autoSaveOnSelection)
            {
                SaveAvatar();
            }
        }

        /// <summary>
        /// Get currently selected part for a category
        /// </summary>
        public AvatarPartItem GetSelectedPart(AvatarPartCategory category)
        {
            if (currentSelections.TryGetValue(category, out string itemId))
            {
                return avatarDatabase?.GetPartById(itemId);
            }

            // Return default if nothing selected
            return avatarDatabase?.GetDefaultPartForCategory(category);
        }

        /// <summary>
        /// Get currently selected part ID (for save/load)
        /// </summary>
        public string GetSelectedPartId(AvatarPartCategory category)
        {
            return currentSelections.TryGetValue(category, out string itemId) ? itemId : null;
        }

        /// <summary>
        /// Get all current selections
        /// </summary>
        public Dictionary<AvatarPartCategory, AvatarPartItem> GetAllSelections()
        {
            var result = new Dictionary<AvatarPartCategory, AvatarPartItem>();
            var categories = Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];

            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None) continue;

                var part = GetSelectedPart(category);
                if (part != null)
                {
                    result[category] = part;
                }
            }

            return result;
        }

        /// <summary>
        /// Reset avatar to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            if (avatarDatabase == null) return;

            currentSelections.Clear();

            var categories = Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];
            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None) continue;

                var defaultPart = avatarDatabase.GetDefaultPartForCategory(category);
                if (defaultPart != null)
                {
                    currentSelections[category] = defaultPart.ItemId;
                }
            }

            OnAvatarReset?.Invoke();
            SaveAvatar();
        }

        /// <summary>
        /// Save current avatar configuration to PlayerPrefs
        /// </summary>
        public void SaveAvatar()
        {
            var categories = Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];

            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None) continue;

                string key = $"{saveKeyPrefix}_{category.GetSaveKey()}";
                string value = GetSelectedPartId(category);

                PlayerPrefs.SetString(key, value ?? string.Empty);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load avatar configuration from PlayerPrefs
        /// </summary>
        public void LoadAvatar()
        {
            currentSelections.Clear();

            var categories = Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];

            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None) continue;

                string key = $"{saveKeyPrefix}_{category.GetSaveKey()}";
                string savedItemId = PlayerPrefs.GetString(key, string.Empty);

                // Try to load saved item
                if (!string.IsNullOrEmpty(savedItemId))
                {
                    var savedPart = avatarDatabase?.GetPartById(savedItemId);
                    if (savedPart != null)
                    {
                        currentSelections[category] = savedItemId;
                        continue;
                    }
                }

                // Fall back to default
                var defaultPart = avatarDatabase?.GetDefaultPartForCategory(category);
                if (defaultPart != null)
                {
                    currentSelections[category] = defaultPart.ItemId;
                }
            }

            OnAvatarLoaded?.Invoke();
        }

        /// <summary>
        /// Clear saved avatar data from PlayerPrefs
        /// </summary>
        public void ClearSavedData()
        {
            var categories = Enum.GetValues(typeof(AvatarPartCategory)) as AvatarPartCategory[];

            foreach (var category in categories)
            {
                if (category == AvatarPartCategory.None) continue;

                string key = $"{saveKeyPrefix}_{category.GetSaveKey()}";
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        #endregion

        #region Database Access

        public AvatarPartDatabase Database => avatarDatabase;
        public List<AvatarPartItem> GetPartsForCategory(AvatarPartCategory category)
        {
            return avatarDatabase?.GetPartsByCategory(category) ?? new List<AvatarPartItem>();
        }

        public List<AvatarPartCategory> GetAvailableCategories()
        {
            return avatarDatabase?.GetAvailableCategories() ?? new List<AvatarPartCategory>();
        }

        #endregion
    }
}
