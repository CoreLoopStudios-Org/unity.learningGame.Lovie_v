using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Api;

namespace UI
{
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Scene Routes")]
        [SerializeField] private string childMainMenuScene = "Main Menu";
        [SerializeField] private string parentDashboardScene = "Parent Dashboard";
        [SerializeField] private string adminDashboardScene = "Admin Dashboard";

        [Header("Launch Cover")]
        [SerializeField] private Color coverColor = new Color32(0x33, 0x2C, 0x66, 0xFF);
        [SerializeField] private string coverSpritePath = "SplashCover";

        private static SceneBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            new GameObject("SceneBootstrap").AddComponent<SceneBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateLaunchCover();
        }

        private void CreateLaunchCover()
        {
            // Covers the launch scene so a routed user never sees the login scene
            // flash before their role scene loads.
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            Sprite splash = Resources.Load<Sprite>(coverSpritePath);
            Image cover = AddStretchedImage("Cover", splash != null ? Color.white : coverColor);
            if (splash != null)
                cover.sprite = splash;
        }

        private Image AddStretchedImage(string name, Color color)
        {
            Image image = new GameObject(name).AddComponent<Image>();
            image.transform.SetParent(transform, false);
            image.color = color;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.sizeDelta = Vector2.zero;
            return image;
        }

        private async void Start()
        {
            if (SessionManager.Instance == null)
            {
                var sessionGo = new GameObject("SessionManager");
                sessionGo.AddComponent<SessionManager>();
            }

            // Wire API 401 errors to session expiry handling
            ApiClient.Instance.OnSessionExpired += SessionManager.Instance.HandleSessionExpired;

            if (SessionManager.Instance.IsAuthenticated)
            {
                RouteByRole();
            }
            else
            {
                Debug.Log($"[SceneBootstrap] No valid session (token={!string.IsNullOrEmpty(SessionManager.Instance.Token)}, expiry={TokenStore.GetExpiresAt() ?? "none"}) — staying on login scene.");
            }

            // Hold the cover through the frame the queued scene switch takes
            // effect, then reveal (dashboard or login).
            await Awaitable.NextFrameAsync();
            Destroy(gameObject);
        }

        private void RouteByRole()
        {
            string role = SessionManager.Instance.Role;

            switch (role)
            {
                case "Child":
                    SceneManager.LoadScene(childMainMenuScene);
                    EnsureChildSessionInitializer();
                    break;
                case "Parent":
                    SceneManager.LoadScene(parentDashboardScene);
                    break;
                case "Admin":
                    SceneManager.LoadScene(adminDashboardScene);
                    break;
                default:
                    Debug.LogWarning($"[SceneBootstrap] Unknown role '{role}' — clearing session.");
                    SessionManager.Instance.ClearSession();
                    break;
            }
        }

        // Wallet/avatar/daily-reward boot for the child lives in ChildSessionInitializer,
        // which is scene-wired — auto-create it so resumed sessions initialize too.
        private static void EnsureChildSessionInitializer()
        {
            if (FindFirstObjectByType<ChildSessionInitializer>() != null)
                return;

            var go = new GameObject("ChildSessionInitializer");
            go.AddComponent<ChildSessionInitializer>();
            DontDestroyOnLoad(go);
        }
    }
}
