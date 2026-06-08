using UnityEngine;

namespace UI
{
    /// <summary>
    /// Controls book image sequence animation triggered by button clicks.
    /// Attach to the "Next page Button" in Book reading Panel prefab.
    /// </summary>
    public class BookAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator bookAnimator;

        [Header("Animation Settings")]
        [SerializeField] private string animationStateName = "Page swapping anim";

        /// <summary>
        /// Call this method from the Next button's onClick event.
        /// </summary>
        public void TriggerNextPageAnimation()
        {
            if (bookAnimator != null)
            {
                bookAnimator.Play(animationStateName, 0, 0f);
            }
        }
    }
}
