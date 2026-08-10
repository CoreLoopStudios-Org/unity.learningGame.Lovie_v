using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Avatar
{
    /// <summary>
    /// Database containing all avatar parts organized by category
    /// </summary>
    [CreateAssetMenu(fileName = "AvatarDatabase", menuName = "Avatar/Avatar Part Database")]
    public class AvatarPartDatabase : ScriptableObject
    {
        [Header("Avatar Parts Collection")]
        [SerializeField]
        private List<AvatarPartItem> allAvatarParts;

        // Runtime cache
        private Dictionary<AvatarPartCategory, List<AvatarPartItem>> categorizedParts;
        private Dictionary<string, AvatarPartItem> partsById;

        #region Initialization

        private void OnEnable()
        {
            BuildCache();
        }

        /// <summary>
        /// Build runtime cache for quick lookups
        /// </summary>
        public void BuildCache()
        {
            categorizedParts = new Dictionary<AvatarPartCategory, List<AvatarPartItem>>();
            partsById = new Dictionary<string, AvatarPartItem>();

            if (allAvatarParts == null) return;

            foreach (var part in allAvatarParts)
            {
                if (part == null) continue;

                // Add to ID lookup
                if (!partsById.ContainsKey(part.ItemId))
                {
                    partsById.Add(part.ItemId, part);
                }

                // Add to category lookup
                if (!categorizedParts.ContainsKey(part.Category))
                {
                    categorizedParts[part.Category] = new List<AvatarPartItem>();
                }
                categorizedParts[part.Category].Add(part);
            }

            // Sort each category by SortOrder
            foreach (var category in categorizedParts.Keys)
            {
                categorizedParts[category].Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get all parts for a specific category
        /// </summary>
        public List<AvatarPartItem> GetPartsByCategory(AvatarPartCategory category)
        {
            if (categorizedParts == null) BuildCache();

            if (categorizedParts.TryGetValue(category, out var parts))
            {
                return new List<AvatarPartItem>(parts);
            }
            return new List<AvatarPartItem>();
        }

        /// <summary>
        /// Get a part by its ID
        /// </summary>
        public AvatarPartItem GetPartById(string itemId)
        {
            if (partsById == null) BuildCache();

            return partsById.TryGetValue(itemId, out var part) ? part : null;
        }

        /// <summary>
        /// Get all categories that have at least one part
        /// </summary>
        public List<AvatarPartCategory> GetAvailableCategories()
        {
            if (categorizedParts == null) BuildCache();

            return categorizedParts.Keys
                .Where(cat => cat != AvatarPartCategory.None)
                .OrderBy(cat => (int)cat)
                .ToList();
        }

        /// <summary>
        /// Get the default item for a category
        /// </summary>
        public AvatarPartItem GetDefaultPartForCategory(AvatarPartCategory category)
        {
            var parts = GetPartsByCategory(category);
            return parts.FirstOrDefault(p => p.IsDefault) ?? parts.FirstOrDefault();
        }

        /// <summary>
        /// Get all parts
        /// </summary>
        public List<AvatarPartItem> GetAllParts()
        {
            return new List<AvatarPartItem>(allAvatarParts);
        }

        #endregion

        #region Editor Helpers

        /// <summary>
        /// Find parts by name (useful for debugging)
        /// </summary>
        public AvatarPartItem FindPartByName(string name)
        {
            return allAvatarParts.FirstOrDefault(p => p != null && p.DisplayName == name);
        }

        /// <summary>
        /// Get total count of all parts
        /// </summary>
        public int GetTotalPartCount()
        {
            return allAvatarParts?.Count ?? 0;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Add a part to the database (editor only)
        /// </summary>
        public void AddPart(AvatarPartItem part)
        {
            if (part == null) return;
            if (allAvatarParts == null) allAvatarParts = new List<AvatarPartItem>();
            if (allAvatarParts.Contains(part)) return;

            allAvatarParts.Add(part);
            BuildCache();
        }

        /// <summary>
        /// Remove a part from the database (editor only)
        /// </summary>
        public void RemovePart(AvatarPartItem part)
        {
            if (part == null || allAvatarParts == null) return;
            allAvatarParts.Remove(part);
            BuildCache();
        }

        /// <summary>
        /// Clear all parts from the database (editor only)
        /// </summary>
        public void ClearAllParts()
        {
            allAvatarParts?.Clear();
            BuildCache();
        }

        /// <summary>
        /// Get the internal list (editor only)
        /// </summary>
        public List<AvatarPartItem> GetPartsList()
        {
            return allAvatarParts;
        }

        /// <summary>
        /// Set the internal list (editor only)
        /// </summary>
        public void SetPartsList(List<AvatarPartItem> parts)
        {
            allAvatarParts = parts;
            BuildCache();
        }
        #endif

        #endregion
    }
}
