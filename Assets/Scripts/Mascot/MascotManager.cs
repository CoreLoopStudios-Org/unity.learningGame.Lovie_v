using System;
using UnityEngine;

namespace Mascot
{
    /// <summary>
    /// Central manager for mascot state, PlayerPrefs persistence, and change events.
    /// Follows the requirement: stored on PlayerPrefs so reinstall resets state.
    /// </summary>
    public static class MascotManager
    {
        public const string PlayerPrefsKey = "SELECTED_MASCOT_ID";
        public const string DefaultDatabaseResourcePath = "Mascots/MascotDatabase";

        public static event Action<MascotData> OnMascotChanged;
        public static event Action<string> OnMascotIdChanged;

        private static MascotDatabase cachedDatabase;

        /// <summary>
        /// Checks whether a mascot has already been selected and saved in PlayerPrefs.
        /// Returns false on first launch after fresh install or reinstall.
        /// </summary>
        public static bool HasSelectedMascot()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return false;
            string id = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            return !string.IsNullOrEmpty(id);
        }

        /// <summary>
        /// Gets the raw selected mascot ID saved in PlayerPrefs.
        /// </summary>
        public static string GetSelectedMascotId()
        {
            return PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        }

        /// <summary>
        /// Saves the selected mascot ID into PlayerPrefs, saves to disk, and notifies listeners.
        /// </summary>
        public static void SetSelectedMascot(string mascotId, MascotDatabase db = null)
        {
            if (string.IsNullOrEmpty(mascotId))
            {
                Debug.LogWarning("[MascotManager] Attempted to set an empty mascot ID.");
                return;
            }

            PlayerPrefs.SetString(PlayerPrefsKey, mascotId);
            PlayerPrefs.Save();

            Debug.Log($"[MascotManager] Saved mascot selection to PlayerPrefs: '{mascotId}'");

            var database = db ?? GetDatabase();
            MascotData mascotData = database != null ? database.GetById(mascotId) : null;

            OnMascotIdChanged?.Invoke(mascotId);
            OnMascotChanged?.Invoke(mascotData);
        }

        /// <summary>
        /// Resolves and returns the currently selected MascotData from PlayerPrefs.
        /// </summary>
        public static MascotData GetSelectedMascot(MascotDatabase db = null)
        {
            string id = GetSelectedMascotId();
            if (string.IsNullOrEmpty(id)) return null;

            var database = db ?? GetDatabase();
            if (database != null)
            {
                return database.GetById(id);
            }

            return null;
        }

        /// <summary>
        /// Clears the mascot selection in PlayerPrefs (useful for debug testing or reinstall simulation).
        /// </summary>
        public static void ClearSelection()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
                PlayerPrefs.Save();
                Debug.Log("[MascotManager] Cleared mascot selection in PlayerPrefs.");
            }

            OnMascotIdChanged?.Invoke(null);
            OnMascotChanged?.Invoke(null);
        }

        /// <summary>
        /// Gets or lazily loads the default MascotDatabase from Resources.
        /// </summary>
        public static MascotDatabase GetDatabase()
        {
            if (cachedDatabase != null) return cachedDatabase;
            cachedDatabase = Resources.Load<MascotDatabase>(DefaultDatabaseResourcePath);
            return cachedDatabase;
        }

        /// <summary>
        /// Registers a database reference for runtime lookup.
        /// </summary>
        public static void RegisterDatabase(MascotDatabase db)
        {
            if (db != null)
            {
                cachedDatabase = db;
            }
        }
    }
}
