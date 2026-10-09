using UnityEngine;

namespace UI
{
    /// <summary>
    /// Plays a book page-turn animator state, forwards or backwards.
    /// Unity Animators can't rewind a clip via negative speed, so reverse playback
    /// freezes the animator and scrubs normalizedTime frame by frame.
    /// </summary>
    public static class BookPageTurnPlayer
    {
        public static async Awaitable PlayAsync(Animator animator, string stateName, bool reverse)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                await Awaitable.WaitForSecondsAsync(0.2f);
                return;
            }

            int stateHash = Animator.StringToHash(stateName);

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            float duration = current.shortNameHash == stateHash
                ? Mathf.Max(0.05f, current.length)
                : 0.3f;

            animator.speed = 0f;
            animator.Play(stateHash, 0, reverse ? 1f : 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                await Awaitable.NextFrameAsync();
                if (animator == null) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                animator.Play(stateHash, 0, reverse ? 1f - t : t);
            }

            // Park on the clip's final frame BEFORE restoring speed — restoring while
            // a reverse scrub sits at frame 0 would replay the clip forwards.
            animator.Play(stateHash, 0, 1f);
            animator.speed = 1f;
        }

        /// <summary>
        /// Passive wait for an animator-driven state (e.g. the panel-open swap the
        /// prefab plays on spawn) to run through. Timeout guards against a disabled
        /// animator so the text always shows.
        /// </summary>
        public static async Awaitable WaitAsync(Animator animator, string stateName)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                await Awaitable.WaitForSecondsAsync(0.2f);
                return;
            }

            int stateHash = Animator.StringToHash(stateName);

            // Wait for the animation to (re)start — the state info is one frame stale after Play.
            for (float elapsed = 0f; elapsed < 3f; elapsed += Time.deltaTime)
            {
                if (animator == null) return;

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash == stateHash && state.normalizedTime < 0.9f) break;

                await Awaitable.NextFrameAsync();
            }

            for (float elapsed = 0f; elapsed < 3f; elapsed += Time.deltaTime)
            {
                if (animator == null) return;

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash != stateHash || state.normalizedTime >= 1f) return;

                await Awaitable.NextFrameAsync();
            }
        }
    }
}
