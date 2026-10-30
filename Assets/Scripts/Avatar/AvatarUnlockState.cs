using System;
using UnityEngine;

namespace Avatar
{
    /// <summary>
    /// Session-wide "every avatar part unlocked" flag. Fed from three sources:
    /// the server's additionalData.allAvatarsUnlocked (profile load), the saved
    /// avatarState's isUnlocked field, or a successful unlock-all purchase.
    /// See ECONOMY-INTEGRATION-GUIDE §6.
    /// </summary>
    public static class AvatarUnlockState
    {
        public static bool AllUnlocked { get; private set; }

        /// <summary>Raised when the flag flips. Listeners must unsubscribe on destroy — static event.</summary>
        public static event Action Changed;

        public static void SetAllUnlocked(bool value)
        {
            if (AllUnlocked == value)
                return;

            AllUnlocked = value;
            Changed?.Invoke();
        }

        /// <summary>
        /// Server truth lives in ChildProfile.additionalData ({"allAvatarsUnlocked":true}).
        /// Always overwrites the flag so loading a new child's profile clears a
        /// previous child's unlock.
        /// </summary>
        public static void ReadFromAdditionalData(string additionalData)
        {
            bool unlocked = false;

            if (!string.IsNullOrWhiteSpace(additionalData))
            {
                try
                {
                    var data = JsonUtility.FromJson<AdditionalData>(additionalData);
                    unlocked = data != null && data.allAvatarsUnlocked;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse additionalData for avatar unlock: {e.Message}");
                }
            }

            SetAllUnlocked(unlocked);
        }

        [Serializable]
        private class AdditionalData
        {
            public bool allAvatarsUnlocked;
        }
    }
}
