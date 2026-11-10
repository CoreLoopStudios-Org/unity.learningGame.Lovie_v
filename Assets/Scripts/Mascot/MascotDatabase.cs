using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mascot
{
    /// <summary>
    /// ScriptableObject database containing all mascots available in the application.
    /// Easily editable via Unity Inspector and reusable in any scene or feature.
    /// </summary>
    [CreateAssetMenu(fileName = "MascotDatabase", menuName = "Lovie/Mascot/Mascot Database")]
    public class MascotDatabase : ScriptableObject
    {
        [Tooltip("List of all mascots available for selection.")]
        [SerializeField] private List<MascotData> mascots = new List<MascotData>();

        private Dictionary<string, MascotData> cacheById;

        public IReadOnlyList<MascotData> Mascots => mascots;
        public int Count => mascots != null ? mascots.Count : 0;

        private void OnEnable()
        {
            BuildCache();
        }

        public void BuildCache()
        {
            cacheById = new Dictionary<string, MascotData>(StringComparer.OrdinalIgnoreCase);
            if (mascots == null) return;

            foreach (var m in mascots)
            {
                if (m != null && !string.IsNullOrEmpty(m.id))
                {
                    if (!cacheById.ContainsKey(m.id))
                    {
                        cacheById.Add(m.id, m);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a mascot by its ID, supporting case-insensitive and normalized formatting (e.g. 'Boy_1' == 'boy 1').
        /// </summary>
        public MascotData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (cacheById == null || cacheById.Count != mascots.Count)
            {
                BuildCache();
            }

            if (cacheById.TryGetValue(id, out var mascot))
            {
                return mascot;
            }

            // Fallback: search by normalized string
            string normalized = Normalize(id);
            if (mascots != null)
            {
                for (int i = 0; i < mascots.Count; i++)
                {
                    var m = mascots[i];
                    if (m != null && (Normalize(m.id) == normalized || Normalize(m.displayName) == normalized))
                    {
                        return m;
                    }
                }
            }

            return null;
        }

        public MascotData GetByIndex(int index)
        {
            if (mascots == null || index < 0 || index >= mascots.Count) return null;
            return mascots[index];
        }

        public bool HasMascot(string id)
        {
            return GetById(id) != null;
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace(" ", "").Replace("_", "").ToLowerInvariant();
        }

#if UNITY_EDITOR
        public void EditorSetMascots(List<MascotData> newMascots)
        {
            mascots = newMascots;
            BuildCache();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
