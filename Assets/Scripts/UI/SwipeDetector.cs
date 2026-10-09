using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    /// <summary>
    /// Detects horizontal swipes (touch or mouse) anywhere on screen while alive.
    /// Attach to the object whose lifetime should own the detection (e.g. the book
    /// reading panel). SwipeRight = finger moved left-to-right, SwipeLeft = right-to-left.
    /// </summary>
    public class SwipeDetector : MonoBehaviour
    {
        [SerializeField, Min(20)] private float minSwipeDistance = 80f;

        public event Action SwipeRight;
        public event Action SwipeLeft;

        private Vector2 startPoint;
        private bool tracking;

        private void Update()
        {
            if (!tracking)
            {
                if (PointerPressed())
                {
                    startPoint = PointerPosition();
                    tracking = true;
                }
                return;
            }

            if (!PointerReleased())
                return;

            tracking = false;
            Vector2 delta = PointerPosition() - startPoint;

            // Horizontal swipes only — must clear the distance threshold and be more
            // horizontal than vertical so page scrolling never flips pages.
            if (Mathf.Abs(delta.x) < minSwipeDistance || Mathf.Abs(delta.x) <= Mathf.Abs(delta.y))
                return;

            if (delta.x < 0)
            {
                SwipeLeft?.Invoke();
            }
            else
            {
                SwipeRight?.Invoke();
            }
        }

        private static bool PointerPressed()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private static bool PointerReleased()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                return true;

            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        }

        private static Vector2 PointerPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }
    }
}
