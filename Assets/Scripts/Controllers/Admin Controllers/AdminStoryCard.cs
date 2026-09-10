using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Api.Models;

namespace UI
{
    public class AdminStoryCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI uploadedText;

        [Header("Card Menu")]
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject menuPopup;
        [SerializeField] private Button createQuizButton;
        [SerializeField] private Button deleteButton;

        public Story Story { get; private set; }

        // The panel subscribes and performs the API call — the card stays a dumb view.
        public event Action<AdminStoryCard> DeleteClicked;

        private void Awake()
        {
            if (menuPopup != null) menuPopup.SetActive(false);

            if (menuButton != null) menuButton.onClick.AddListener(ToggleMenu);
            if (createQuizButton != null) createQuizButton.onClick.AddListener(OnCreateQuizClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        private void OnDestroy()
        {
            if (menuButton != null) menuButton.onClick.RemoveListener(ToggleMenu);
            if (createQuizButton != null) createQuizButton.onClick.RemoveListener(OnCreateQuizClicked);
            if (deleteButton != null) deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }

        public void Setup(Story story)
        {
            Story = story;

            if (nameText != null)
            {
                nameText.text = story?.title ?? string.Empty;
            }

            if (uploadedText != null)
            {
                uploadedText.text = FormatUploadedDate(story?.updatedAt, story?.createdAt);
            }
        }

        private void ToggleMenu()
        {
            if (menuPopup == null) return;
            menuPopup.SetActive(!menuPopup.activeSelf);
        }

        private void CloseMenu()
        {
            if (menuPopup != null && menuPopup.activeSelf)
                menuPopup.SetActive(false);
        }

        private void Update()
        {
            if (menuPopup == null || !menuPopup.activeSelf) return;

            if (PointerPressedOutsideMenu())
                CloseMenu();
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

        // Only records which story was chosen — opening the quiz panel is
        // handled by the PanelSpawner wired on the Create Quiz button. The
        // popup is intentionally left open: closing it here would deactivate
        // the button and kill PanelSpawner's popup animation coroutine. It
        // closes on the next click outside the menu.
        private void OnCreateQuizClicked()
        {
            AdminQuizContext.SelectedStoryId = Story?.id;
            AdminQuizContext.SelectedStoryTitle = Story?.title;
        }

        private void OnDeleteClicked()
        {
            DeleteClicked?.Invoke(this);
        }

        private static string FormatUploadedDate(string updatedAt, string createdAt)
        {
            string raw = string.IsNullOrWhiteSpace(updatedAt) ? createdAt : updatedAt;
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime date))
            {
                return date.ToLocalTime().ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            }

            return raw;
        }
    }
}
