using UnityEngine;
using UnityEngine.UI;

namespace Mascot
{
    /// <summary>
    /// Displays a mascot on an Image GameObject with an Animator.
    /// Handles dynamically swapping the Animator Controller and/or Sprite,
    /// rebinding animation states, and listening for mascot selection updates.
    /// Reusable for the Home Page Avatar or any other avatar view.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class MascotAvatarDisplay : MonoBehaviour
    {
        [Header("Components (Auto-detected if null)")]
        [Tooltip("The Image component that displays the mascot sprite / animation frames.")]
        [SerializeField] private Image avatarImage;

        [Tooltip("The Animator component that plays the mascot animation.")]
        [SerializeField] private Animator avatarAnimator;

        [Header("Database (Optional)")]
        [Tooltip("Optional database reference. If unassigned, MascotManager loads default from Resources.")]
        [SerializeField] private MascotDatabase database;

        [Header("Display Options")]
        [Tooltip("Automatically refresh on Awake/OnEnable.")]
        [SerializeField] private bool refreshOnEnable = true;

        [Tooltip("Update the Image sprite to the mascot's initial/preview sprite when assigned.")]
        [SerializeField] private bool updateSprite = true;

        [Tooltip("Update the Animator controller to the mascot's animation controller.")]
        [SerializeField] private bool updateAnimator = true;

        [Tooltip("Reset and rebind animator immediately when a new controller is assigned.")]
        [SerializeField] private bool rebindAnimatorOnAssign = true;

        [Header("Current Mascot")]
        [SerializeField] private string currentMascotId;

        public Image AvatarImage => avatarImage;
        public Animator AvatarAnimator => avatarAnimator;
        public string CurrentMascotId => currentMascotId;

        private void Awake()
        {
            ResolveComponents();
            if (database != null)
            {
                MascotManager.RegisterDatabase(database);
            }
        }

        private void OnEnable()
        {
            MascotManager.OnMascotChanged += HandleMascotChanged;

            if (refreshOnEnable)
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            MascotManager.OnMascotChanged -= HandleMascotChanged;
        }

        public void ResolveComponents()
        {
            if (avatarImage == null)
            {
                avatarImage = GetComponent<Image>();
            }

            if (avatarAnimator == null)
            {
                avatarAnimator = GetComponent<Animator>();
            }
        }

        /// <summary>
        /// Reads the selected mascot from MascotManager and applies it.
        /// </summary>
        public void Refresh()
        {
            ResolveComponents();
            var mascot = MascotManager.GetSelectedMascot(database);
            if (mascot != null)
            {
                ApplyMascot(mascot);
            }
        }

        /// <summary>
        /// Applies the specified mascot configuration to this avatar view.
        /// </summary>
        public void ApplyMascot(MascotData mascot)
        {
            if (mascot == null) return;

            ResolveComponents();
            currentMascotId = mascot.id;

            // 1. Update sprite
            if (updateSprite && avatarImage != null)
            {
                Sprite spriteToUse = mascot.initialFrame != null ? mascot.initialFrame : mascot.previewSprite;
                if (spriteToUse != null)
                {
                    avatarImage.sprite = spriteToUse;
                }
            }

            // 2. Update animator controller
            if (updateAnimator && avatarAnimator != null && mascot.animatorController != null)
            {
                avatarAnimator.runtimeAnimatorController = mascot.animatorController;
                avatarAnimator.enabled = true;

                if (rebindAnimatorOnAssign)
                {
                    avatarAnimator.Rebind();
                    avatarAnimator.Update(0f);
                }
            }
        }

        /// <summary>
        /// Dynamically loads and applies a mascot by its ID string.
        /// </summary>
        public void ApplyMascotById(string id)
        {
            var db = database != null ? database : MascotManager.GetDatabase();
            if (db != null)
            {
                var mascot = db.GetById(id);
                if (mascot != null)
                {
                    ApplyMascot(mascot);
                    return;
                }
            }

            Debug.LogWarning($"[MascotAvatarDisplay] Mascot with ID '{id}' not found in database.");
        }

        private void HandleMascotChanged(MascotData mascot)
        {
            if (mascot != null)
            {
                ApplyMascot(mascot);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveComponents();
        }
#endif
    }
}
