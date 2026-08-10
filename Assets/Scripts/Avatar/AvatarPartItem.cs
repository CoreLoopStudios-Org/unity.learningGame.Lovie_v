using UnityEngine;

namespace Avatar
{
    /// <summary>
    /// Represents a single customizable part for the avatar system
    /// </summary>
    [CreateAssetMenu(fileName = "NewAvatarPart", menuName = "Avatar/Avatar Part Item")]
    public class AvatarPartItem : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;

        [Header("Category")]
        [SerializeField] private AvatarPartCategory category;

        [Header("Sprites")]
        [SerializeField] private Sprite iconSprite;        // For UI selection preview
        [SerializeField] private Sprite avatarSprite;      // For actual avatar display

        [Header("Sorting")]
        [SerializeField] private int sortOrder;            // For display order in UI
        [SerializeField] private bool isDefault = false;    // Is this the default item for its category?

        [Header("Optional: Layer Settings")]
        [SerializeField] private int sortingOrderOffset;     // For layering multiple parts
        [SerializeField] private string customLayerName;     // Optional custom layer

        [Header("Unlock Requirements (Optional)")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private int requiredLevel = 0;
        [SerializeField] private int requiredCoins = 0;

        // Runtime cache
        private bool isUnlockedCache;

        #region Properties

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public AvatarPartCategory Category => category;
        public Sprite IconSprite => iconSprite;
        public Sprite AvatarSprite => avatarSprite;
        public int SortOrder => sortOrder;
        public bool IsDefault => isDefault;
        public int SortingOrderOffset => sortingOrderOffset;
        public string CustomLayerName => customLayerName;
        public bool IsLocked => isLocked;
        public int RequiredLevel => requiredLevel;
        public int RequiredCoins => requiredCoins;

        #endregion

        #region Runtime Modification

        /// <summary>
        /// Set the display name (for editor/runtime use)
        /// </summary>
        public void SetDisplayName(string name)
        {
            displayName = name;
        }

        /// <summary>
        /// Set the category (for editor/runtime use)
        /// </summary>
        public void SetCategory(AvatarPartCategory cat)
        {
            category = cat;
        }

        /// <summary>
        /// Set the icon sprite (for editor/runtime use)
        /// </summary>
        public void SetIconSprite(Sprite sprite)
        {
            iconSprite = sprite;
        }

        /// <summary>
        /// Set the avatar sprite (for editor/runtime use)
        /// </summary>
        public void SetAvatarSprite(Sprite sprite)
        {
            avatarSprite = sprite;
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = System.Guid.NewGuid().ToString().Substring(0, 8);
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(itemId) &&
                   iconSprite != null &&
                   avatarSprite != null &&
                   category != AvatarPartCategory.None;
        }

        #endregion

        #region Unlock Logic

        /// <summary>
        /// Check if this item is unlocked based on game state
        /// </summary>
        public bool CheckUnlocked(int playerLevel, int playerCoins)
        {
            if (!isLocked) return true;
            return playerLevel >= requiredLevel && playerCoins >= requiredCoins;
        }

        /// <summary>
        /// Check if this item is unlocked (simplified version)
        /// </summary>
        public bool CheckUnlocked()
        {
            return !isLocked;
        }

        #endregion
    }
}
