using UnityEngine;
using UnityEngine.SceneManagement;
using Api;

namespace UI
{
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Scene Routes")]
        [SerializeField] private string childLoginScene = "Main Game/Children/Login";
        [SerializeField] private string childMainMenuScene = "Main Game/Children/Main Menu";
        [SerializeField] private string parentLoginScene = "Main Game/Parent/Parent Login";
        [SerializeField] private string parentDashboardScene = "Main Game/Parent/Parent Dashboard";
        [SerializeField] private string adminLoginScene = "Main Game/Admin/Admin Login";
        [SerializeField] private string adminDashboardScene = "Main Game/Admin/Admin Dashbaord";

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            CheckSessionAndRoute();
        }

        void CheckSessionAndRoute()
        {
            if (SessionManager.Instance == null)
            {
                var sessionGo = new GameObject("SessionManager");
                sessionGo.AddComponent<SessionManager>();
            }

            if (SessionManager.Instance.IsAuthenticated && SessionManager.Instance.IsValidToken())
            {
                RouteByRole();
            }
            else
            {
                RouteToLogin();
            }
        }

        void RouteByRole()
        {
            string role = SessionManager.Instance.Role;

            switch (role)
            {
                case "Child":
                    SceneManager.LoadScene(childMainMenuScene);
                    break;
                case "Parent":
                    SceneManager.LoadScene(parentDashboardScene);
                    break;
                case "Admin":
                    SceneManager.LoadScene(adminDashboardScene);
                    break;
                default:
                    Debug.LogWarning($"Unknown role: {role}");
                    SessionManager.Instance.ClearSession();
                    RouteToLogin();
                    break;
            }

            Destroy(gameObject);
        }

        void RouteToLogin()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene.Contains("Parent"))
            {
                SceneManager.LoadScene(parentLoginScene);
            }
            else if (currentScene.Contains("Admin"))
            {
                SceneManager.LoadScene(adminLoginScene);
            }
            else
            {
                SceneManager.LoadScene(childLoginScene);
            }

            Destroy(gameObject);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && SessionManager.Instance != null)
            {
                SessionManager.Instance.CheckSessionExpiry();
            }
        }
    }
}
