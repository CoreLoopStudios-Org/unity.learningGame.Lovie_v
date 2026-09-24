using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;

namespace UI
{
    public class ParentProfilePanelController : MonoBehaviour
    {
        private const string MaskedPassword = "••••••••";

        [Header("Profile Fields")]
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private TextMeshProUGUI emailText;
        [SerializeField] private TextMeshProUGUI passwordText;

        [Header("Logout")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private string loginScene = "Parent Login";

        private void Awake()
        {
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void OnDestroy()
        {
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        private void OnEnable()
        {
            Refresh();
        }

        // Parents are plain Users in the backend (no parent profile endpoint) —
        // id + email come from the session token's JWT claims.
        public void Refresh()
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[ParentProfilePanelController] No valid session, skipping profile load.");
                return;
            }

            if (userIdText != null)
                userIdText.text = string.IsNullOrEmpty(SessionManager.Instance.UserId)
                    ? "-"
                    : SessionManager.Instance.UserId;

            if (emailText != null)
                emailText.text = string.IsNullOrEmpty(SessionManager.Instance.Email)
                    ? "-"
                    : SessionManager.Instance.Email;

            // The Users table has fullName, but no endpoint exposes the parent's
            // own record yet — show "-" until the backend adds one.
            if (nameText != null)
                nameText.text = "-";

            // The API never returns the password, so it is always shown masked.
            if (passwordText != null)
                passwordText.text = MaskedPassword;

            // No API source for the parent's image yet — the designer
            // placeholder on profileImage stays.
        }

        private void OnLogoutClicked()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.ClearSession();

            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
        }
    }
}
