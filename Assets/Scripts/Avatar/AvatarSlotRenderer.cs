using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Avatar
{
    // Renders body/hair/dress sprites onto fixed UI Image slots from an avatarState JSON
    public static class AvatarSlotRenderer
    {
        public static void Apply(string avatarStateJson, AvatarPartDatabase database, Image bodyImage, Image hairImage, Image dressImage, bool fallbackToDefaults = true)
        {
            if (bodyImage == null && hairImage == null && dressImage == null)
                return;

            if (database == null)
            {
                Debug.LogWarning("[AvatarSlotRenderer] No AvatarPartDatabase assigned — cannot render avatar.");
                return;
            }

            if (!string.IsNullOrEmpty(avatarStateJson))
            {
                Debug.Log($"[AvatarSlotRenderer] avatarState: {avatarStateJson}");

                var state = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(avatarStateJson);
                if (state != null && state.Count > 0)
                {
                    SetSlot(FindPart(state, AvatarPartCategory.BodyColor, database), bodyImage);
                    SetSlot(FindPart(state, AvatarPartCategory.Hair, database), hairImage);
                    SetSlot(FindPart(state, AvatarPartCategory.Dress, database), dressImage);
                    return;
                }
            }

            if (!fallbackToDefaults)
            {
                // No avatar data — hide the slots instead of showing defaults
                SetSlot(null, bodyImage);
                SetSlot(null, hairImage);
                SetSlot(null, dressImage);
                return;
            }

            // No avatar saved yet — fall back to default parts
            SetSlot(database.GetDefaultPartForCategory(AvatarPartCategory.BodyColor), bodyImage);
            SetSlot(database.GetDefaultPartForCategory(AvatarPartCategory.Hair), hairImage);
            SetSlot(database.GetDefaultPartForCategory(AvatarPartCategory.Dress), dressImage);
        }

        private static AvatarPartItem FindPart(Dictionary<string, string> state, AvatarPartCategory category, AvatarPartDatabase database)
        {
            if (state.TryGetValue(category.ToString(), out string itemId))
            {
                var part = database.GetPartById(itemId);
                if (part != null && part.AvatarSprite != null)
                    return part;

                Debug.LogWarning($"[AvatarSlotRenderer] Could not resolve avatar part '{itemId}' for {category} — check the AvatarPartDatabase assignment.");
            }

            return null;
        }

        private static void SetSlot(AvatarPartItem part, Image target)
        {
            if (target == null)
                return;

            if (part != null && part.AvatarSprite != null)
            {
                target.sprite = part.AvatarSprite;
                target.enabled = true;
            }
            else
            {
                target.enabled = false;
            }
        }
    }
}
