using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    // Dropdown-style menu shared by admin cards: the menu button toggles the
    // popup, a press anywhere outside closes it, and card actions call Close().
    public class AdminCardMenu : MonoBehaviour
    {
        [Header("Card Menu")]
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject menuPopup;

        private void Awake()
        {
            if (menuPopup != null) menuPopup.SetActive(false);

            if (menuButton != null) menuButton.onClick.AddListener(ToggleMenu);
        }

        private void OnDestroy()
        {
            if (menuButton != null) menuButton.onClick.RemoveListener(ToggleMenu);
        }

        private void ToggleMenu()
        {
            if (menuPopup == null) return;
            menuPopup.SetActive(!menuPopup.activeSelf);
        }

        public void Close()
        {
            if (menuPopup != null && menuPopup.activeSelf)
                menuPopup.SetActive(false);
        }

        private void Update()
        {
            if (menuPopup == null || !menuPopup.activeSelf) return;

            if (PointerPressedOutsideMenu())
                Close();
        }

        private bool PointerPressedOutsideMenu()
        {
            Vector2 pointerPos = default;
            bool pressed = false;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                pressed = true;
                pointerPos = mouse.position.ReadValue();
            }
            else
            {
                Touchscreen touchscreen = Touchscreen.current;
                if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
                {
                    pressed = true;
                    pointerPos = touchscreen.primaryTouch.position.ReadValue();
                }
            }

            if (!pressed) return false;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return true;

            PointerEventData pointerData = new PointerEventData(eventSystem) { position = pointerPos };
            List<RaycastResult> hits = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, hits);

            foreach (RaycastResult hit in hits)
            {
                if (IsInsideMenu(hit.gameObject)) return false;
            }

            return true;
        }

        private bool IsInsideMenu(GameObject hitObject)
        {
            if (hitObject == null) return false;

            if (menuPopup != null &&
                (hitObject == menuPopup || hitObject.transform.IsChildOf(menuPopup.transform)))
                return true;

            // Treat the menu button as part of the menu so pressing it again
            // doesn't close-and-reopen within the same click.
            if (menuButton != null &&
                (hitObject == menuButton.gameObject || hitObject.transform.IsChildOf(menuButton.transform)))
                return true;

            return false;
        }
    }
}
