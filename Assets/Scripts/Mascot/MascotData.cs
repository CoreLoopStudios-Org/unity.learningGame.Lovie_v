using System;
using UnityEngine;

namespace Mascot
{
    /// <summary>
    /// Represents all configuration and assets for an individual mascot character.
    /// </summary>
    [Serializable]
    public class MascotData
    {
        [Tooltip("Unique ID for this mascot (e.g. 'Boy_1', 'Boy_2', 'Girl_3', 'Girl_4', 'Girl_5').")]
        public string id;

        [Tooltip("Human-friendly display name (e.g. 'Boy 1', 'Boy 2', etc.).")]
        public string displayName;

        [Tooltip("Preview / UI selection sprite (e.g. the icon used on the selection button).")]
        public Sprite previewSprite;

        [Tooltip("First or default animation frame (optional static fallback).")]
        public Sprite initialFrame;

        [Tooltip("The Animator Controller responsible for this mascot's idle and speech animations.")]
        public RuntimeAnimatorController animatorController;

        public bool IsValid => !string.IsNullOrEmpty(id) && (animatorController != null || previewSprite != null);

        public MascotData() { }

        public MascotData(string id, string displayName, Sprite previewSprite, Sprite initialFrame, RuntimeAnimatorController animatorController)
        {
            this.id = id;
            this.displayName = displayName;
            this.previewSprite = previewSprite;
            this.initialFrame = initialFrame;
            this.animatorController = animatorController;
        }

        public override string ToString()
        {
            return $"MascotData({id}, displayName='{displayName}')";
        }
    }
}
