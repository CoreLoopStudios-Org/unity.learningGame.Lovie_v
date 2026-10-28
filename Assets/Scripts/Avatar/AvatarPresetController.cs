using System.Collections.Generic;
using Api;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UI;

namespace Avatar
{
    /// <summary>
    /// Applies outfit presets (dress + matching shoes) from item IDs.
    /// Add this to the Avatar Edit Panel, assign each preset button and
    /// its item IDs from the AvatarPartDatabase.
    /// </summary>
    public class AvatarPresetController : MonoBehaviour
    {
        [System.Serializable]
        public class PresetEntry
        {
            public Button button;
            [Tooltip("AvatarPartDatabase ItemId of the Body Color part")]
            public string bodyItemId;
            [Tooltip("AvatarPartDatabase ItemId of the Hair part")]
            public string hairItemId;
            [Tooltip("AvatarPartDatabase ItemId of the Dress part")]
            public string dressItemId;
            [Tooltip("AvatarPartDatabase ItemId of the Shoes part")]
            public string shoesItemId;
        }

        [Header("References")]
        [SerializeField] private AvatarCustomizationManager customizationManager;

        [Header("Presets")]
        [SerializeField] private PresetEntry[] presets;

        private readonly Dictionary<Button, UnityAction> wiredListeners = new Dictionary<Button, UnityAction>();

        private void OnEnable()
        {
            if (presets == null)
                return;

            foreach (var preset in presets)
            {
                if (preset == null || preset.button == null)
                    continue;

                if (wiredListeners.ContainsKey(preset.button))
                    continue;

                UnityAction listener = () => ApplyPreset(preset);
                wiredListeners[preset.button] = listener;
                preset.button.onClick.AddListener(listener);

                if (preset.button.GetComponent<SelectionMark>() == null)
                {
                    Debug.LogWarning($"[AvatarPresetController] No SelectionMark on preset button '{preset.button.name}' — its selection indicator won't update");
                }
            }

            if (customizationManager != null)
            {
                customizationManager.OnPartSelected += OnPartChanged;
                customizationManager.OnAvatarLoaded += OnStateChanged;
                customizationManager.OnAvatarReset += OnStateChanged;
            }

            StoreService.Instance.OnPurchasesRefreshed += OnPurchasesRefreshed;
            AvatarPartItem.OnAnyLockStateChanged += OnPurchasesRefreshed;

            RefreshPresetStates();
        }

        private void OnDisable()
        {
            foreach (var kvp in wiredListeners)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.onClick.RemoveListener(kvp.Value);
                }
            }

            wiredListeners.Clear();

            if (customizationManager != null)
            {
                customizationManager.OnPartSelected -= OnPartChanged;
                customizationManager.OnAvatarLoaded -= OnStateChanged;
                customizationManager.OnAvatarReset -= OnStateChanged;
            }

            StoreService.Instance.OnPurchasesRefreshed -= OnPurchasesRefreshed;
            AvatarPartItem.OnAnyLockStateChanged -= OnPurchasesRefreshed;
        }

        private void OnPurchasesRefreshed()
        {
            RefreshPresetStates();
        }

        private void ApplyPreset(PresetEntry preset)
        {
            if (customizationManager == null)
            {
                Debug.LogWarning("[AvatarPresetController] Customization Manager is not assigned");
                return;
            }

            var database = customizationManager.Database;
            if (database == null)
            {
                Debug.LogWarning("[AvatarPresetController] Customization Manager has no database assigned");
                return;
            }

            if (!IsPresetUnlocked(preset))
            {
                Debug.LogWarning($"[AvatarPresetController] Preset '{preset.button.name}' contains locked parts — apply blocked");
                return;
            }

            var body = ResolvePart(database, preset.bodyItemId, "Body Color");
            var hair = ResolvePart(database, preset.hairItemId, "Hair");
            var dress = ResolvePart(database, preset.dressItemId, "Dress");
            var shoes = ResolvePart(database, preset.shoesItemId, "Shoes");

            if (body == null && hair == null && dress == null && shoes == null)
                return;

            customizationManager.ApplyParts(body, hair, dress, shoes);
        }

        private static AvatarPartItem ResolvePart(AvatarPartDatabase database, string itemId, string label)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            var part = database.GetPartById(itemId);
            if (part == null)
            {
                Debug.LogWarning($"[AvatarPresetController] {label} item '{itemId}' not found in the avatar database");
            }

            return part;
        }

        private void OnPartChanged(AvatarPartCategory category, AvatarPartItem part)
        {
            RefreshPresetStates();
        }

        private void OnStateChanged()
        {
            RefreshPresetStates();
        }

        private void RefreshPresetStates()
        {
            if (presets == null)
                return;

            foreach (var preset in presets)
            {
                if (preset?.button == null)
                    continue;

                bool unlocked = IsPresetUnlocked(preset);
                preset.button.interactable = unlocked;

                var mark = preset.button.GetComponent<SelectionMark>();
                if (mark != null)
                {
                    mark.SetSelected(unlocked && IsPresetActive(preset));
                }
            }
        }

        private bool IsPresetUnlocked(PresetEntry preset)
        {
            return PartUnlocked(preset.bodyItemId) &&
                   PartUnlocked(preset.hairItemId) &&
                   PartUnlocked(preset.dressItemId) &&
                   PartUnlocked(preset.shoesItemId);
        }

        private bool PartUnlocked(string presetItemId)
        {
            if (string.IsNullOrEmpty(presetItemId))
                return true;

            if (customizationManager == null || customizationManager.Database == null)
                return false;

            var part = customizationManager.Database.GetPartById(presetItemId);
            return part != null && part.CheckUnlocked();
        }

        private bool IsPresetActive(PresetEntry preset)
        {
            bool anyDefined = false;
            bool matches = true;

            matches &= PartMatches(preset.bodyItemId, AvatarPartCategory.BodyColor, ref anyDefined);
            matches &= PartMatches(preset.hairItemId, AvatarPartCategory.Hair, ref anyDefined);
            matches &= PartMatches(preset.dressItemId, AvatarPartCategory.Dress, ref anyDefined);
            matches &= PartMatches(preset.shoesItemId, AvatarPartCategory.Shoes, ref anyDefined);

            return anyDefined && matches;
        }

        private bool PartMatches(string presetItemId, AvatarPartCategory category, ref bool anyDefined)
        {
            if (string.IsNullOrEmpty(presetItemId))
                return true;

            anyDefined = true;

            return customizationManager != null &&
                   customizationManager.GetSelectedPartId(category) == presetItemId;
        }
    }
}
