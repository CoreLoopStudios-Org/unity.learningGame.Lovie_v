using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mascot
{
    /// <summary>
    /// Component placed on each mascot button in the selection panel.
    /// Manages the visual selection state, including activating/deactivating the child 'stroke' object.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MascotSelectionButton : MonoBehaviour
    {
        [Header("Mascot ID")]
        [Tooltip("The mascot ID this button represents (e.g. 'Boy_1', 'Girl_5'). Defaults to GameObject name if empty.")]
        [SerializeField] private string mascotId;

        [Header("UI References (Auto-detected if unassigned)")]
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;

        [Tooltip("Child GameObject representing the selection outline / stroke. Default is inactive.")]
        [SerializeField] private GameObject strokeObject;

        [Header("State")]
        [SerializeField] private bool isSelected = false;

        public string MascotId
        {
            get => !string.IsNullOrEmpty(mascotId) ? mascotId : gameObject.name;
            set => mascotId = value;
        }

        public bool IsSelected => isSelected;
        public Button Button => button;
        public GameObject StrokeObject => strokeObject;

        public event Action<MascotSelectionButton> OnButtonClicked;

        private void Awake()
        {
            ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            if (strokeObject == null)
            {
                strokeObject = FindStrokeChild();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            // Sync visual stroke with initial isSelected state
            SetSelected(isSelected);
        }

        /// <summary>
        /// Automatically finds a child object named 'stroke', 'selected stroke', etc.
        /// </summary>
        public GameObject FindStrokeChild()
        {
            // 1. Direct match by exact or partial name
            foreach (Transform child in transform)
            {
                string lower = child.name.ToLowerInvariant();
                if (lower.Contains("stroke") || lower.Contains("selected") || lower.Contains("outline"))
                {
                    return child.gameObject;
                }
            }

            // 2. Recursive search in case nested
            var allChildren = GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == transform) continue;
                string lower = child.name.ToLowerInvariant();
                if (lower.Contains("stroke") || lower.Contains("selected"))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private void HandleClick()
        {
            OnButtonClicked?.Invoke(this);
        }

        /// <summary>
        /// Updates the selection state and toggles the child stroke object.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (strokeObject != null)
            {
                strokeObject.SetActive(selected);
            }
        }

        public void SetPreviewSprite(Sprite sprite)
        {
            if (buttonImage != null && sprite != null)
            {
                buttonImage.sprite = sprite;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (strokeObject == null)
            {
                strokeObject = FindStrokeChild();
            }
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }
#endif
    }
}
