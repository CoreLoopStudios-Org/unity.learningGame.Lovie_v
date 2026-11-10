using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mascot
{
    /// <summary>
    /// Coordinates the Mascot Selection Panel.
    /// Manages button selection states, toggling stroke outlines, saving choice to PlayerPrefs,
    /// and updating the Home Page Avatar with the selected mascot's animator and sprite.
    /// </summary>
    public class MascotSelectionPanelController : MonoBehaviour
    {
        [Header("Database")]
        [Tooltip("Optional database asset. If not assigned, loads from Resources/Mascots/MascotDatabase.")]
        [SerializeField] private MascotDatabase database;

        [Header("UI Hierarchy (Auto-detected if null)")]
        [Tooltip("The root GameObject of the selection panel. Defaults to this GameObject.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("Container holding the mascot buttons (e.g. 'Mascot Container').")]
        [SerializeField] private Transform mascotContainer;

        [Tooltip("The mascot buttons. Auto-populated from mascotContainer if empty.")]
        [SerializeField] private List<MascotSelectionButton> mascotButtons = new List<MascotSelectionButton>();

        [Tooltip("The Start Adventure button.")]
        [SerializeField] private Button startAdventureButton;

        [Header("Target Avatar (Optional)")]
        [Tooltip("Home page avatar display to update immediately upon selection. Auto-found if null.")]
        [SerializeField] private MascotAvatarDisplay targetAvatarDisplay;

        [Header("Behaviour Options")]
        [Tooltip("If true, panel automatically hides on launch when a mascot is already saved in PlayerPrefs.")]
        [SerializeField] private bool hideIfAlreadySelected = true;

        [Tooltip("If true, Start Adventure button is disabled until a mascot is picked.")]
        [SerializeField] private bool requireSelectionToStart = true;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip selectionSound;
        [SerializeField] private AudioClip startSound;

        [Header("Debug State")]
        [SerializeField] private string stagedMascotId;

        public string StagedMascotId => stagedMascotId;
        public IReadOnlyList<MascotSelectionButton> MascotButtons => mascotButtons;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (database != null)
            {
                MascotManager.RegisterDatabase(database);
            }

            AutoDiscoverReferences();
        }

        private void Start()
        {
            InitializeFlow();
        }

        /// <summary>
        /// Automatically discovers hierarchy references if not manually assigned in inspector.
        /// </summary>
        public void AutoDiscoverReferences()
        {
            // 1. Panel root
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            // 2. Mascot Container
            if (mascotContainer == null)
            {
                var container = transform.Find("Mascot Container");
                if (container != null)
                {
                    mascotContainer = container;
                }
            }

            // 3. Mascot Buttons
            if (mascotButtons == null || mascotButtons.Count == 0)
            {
                mascotButtons = new List<MascotSelectionButton>();
                Transform searchRoot = mascotContainer != null ? mascotContainer : transform;

                foreach (Transform child in searchRoot)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        var mascotBtn = child.GetComponent<MascotSelectionButton>();
                        if (mascotBtn == null)
                        {
                            mascotBtn = child.gameObject.AddComponent<MascotSelectionButton>();
                        }
                        mascotBtn.ResolveReferences();
                        mascotButtons.Add(mascotBtn);
                    }
                }
            }

            // 4. Start Button
            if (startAdventureButton == null)
            {
                var startTransform = transform.Find("Start Button");
                if (startTransform != null)
                {
                    startAdventureButton = startTransform.GetComponent<Button>();
                    if (startAdventureButton == null)
                    {
                        startAdventureButton = startTransform.gameObject.AddComponent<Button>();
                    }
                }
            }

            // 5. Target Avatar Display
            if (targetAvatarDisplay == null)
            {
                targetAvatarDisplay = FindFirstObjectByType<MascotAvatarDisplay>(FindObjectsInactive.Include);
            }
        }

        private void InitializeFlow()
        {
            // Check if already selected on a previous session
            if (hideIfAlreadySelected && MascotManager.HasSelectedMascot())
            {
                panelRoot.SetActive(false);

                // Ensure Home Page Avatar displays the saved mascot
                if (targetAvatarDisplay != null)
                {
                    targetAvatarDisplay.Refresh();
                }
                return;
            }

            // First launch (or selection cleared) -> Show selection panel!
            panelRoot.SetActive(true);
            stagedMascotId = null;

            // Wire up each mascot button
            foreach (var btn in mascotButtons)
            {
                if (btn == null) continue;
                btn.ResolveReferences();
                btn.SetSelected(false); // Stroke default active as false per requirement
                btn.OnButtonClicked -= HandleMascotButtonClicked;
                btn.OnButtonClicked += HandleMascotButtonClicked;
            }

            // Wire Start Button
            if (startAdventureButton != null)
            {
                startAdventureButton.onClick.RemoveListener(HandleStartAdventureClicked);
                startAdventureButton.onClick.AddListener(HandleStartAdventureClicked);

                if (requireSelectionToStart)
                {
                    startAdventureButton.interactable = false;
                }
            }
        }

        private void HandleMascotButtonClicked(MascotSelectionButton clicked)
        {
            if (clicked == null) return;

            stagedMascotId = clicked.MascotId;

            // Activate clicked button's stroke; deactivate all others
            foreach (var btn in mascotButtons)
            {
                if (btn != null)
                {
                    btn.SetSelected(btn == clicked);
                }
            }

            if (requireSelectionToStart && startAdventureButton != null)
            {
                startAdventureButton.interactable = true;
            }

            PlaySound(selectionSound);
        }

        private void HandleStartAdventureClicked()
        {
            if (string.IsNullOrEmpty(stagedMascotId))
            {
                Debug.LogWarning("[MascotSelectionPanel] Cannot start adventure: no mascot has been selected.");
                return;
            }

            PlaySound(startSound);

            // 1. Store in PlayerPrefs & fire change event
            MascotManager.SetSelectedMascot(stagedMascotId, database);

            // 2. Immediately update target Avatar on Home Page
            if (targetAvatarDisplay != null)
            {
                targetAvatarDisplay.Refresh();
            }

            // 3. Close panel
            panelRoot.SetActive(false);
        }

        /// <summary>
        /// Opens the panel manually (e.g. for re-selection from settings).
        /// </summary>
        public void OpenPanel()
        {
            panelRoot.SetActive(true);
            string currentId = MascotManager.GetSelectedMascotId();
            stagedMascotId = currentId;

            foreach (var btn in mascotButtons)
            {
                if (btn != null)
                {
                    bool isCur = !string.IsNullOrEmpty(currentId) &&
                                 string.Equals(btn.MascotId, currentId, System.StringComparison.OrdinalIgnoreCase);
                    btn.SetSelected(isCur);
                }
            }

            if (startAdventureButton != null)
            {
                startAdventureButton.interactable = !requireSelectionToStart || !string.IsNullOrEmpty(stagedMascotId);
            }
        }

        public void ClosePanel()
        {
            
            panelRoot.SetActive(false);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Reset Selection (Clear PlayerPrefs)")]
        public void ResetSelectionForTesting()
        {
            MascotManager.ClearSelection();
            InitializeFlow();
        }

        [ContextMenu("Auto-Discover Hierarchy References")]
        private void EditorAutoDiscover()
        {
            AutoDiscoverReferences();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
