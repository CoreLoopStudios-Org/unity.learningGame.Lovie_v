using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    // Full-screen splash overlay shown during scene switches. Scene loads go through
    // SceneTransition.Load instead of SceneManager.LoadScene so the activation hitch
    // happens behind the splash instead of freezing the current frame on screen.
    public class SceneTransition : MonoBehaviour
    {
        private const string SplashPrefabPath = "LoadingSplash";
        private const float MinDurationSeconds = 0.35f;

        private static SceneTransition instance;

        [SerializeField] private Color fallbackColor = new Color32(0x33, 0x2C, 0x66, 0xFF);

        private Canvas canvas;
        private bool transitioning;

        public static void Load(string sceneName)
        {
            EnsureInstance();
            instance.TransitionAsync(sceneName);
        }

        private static void EnsureInstance()
        {
            if (instance != null)
                return;

            var go = new GameObject("SceneTransition");
            instance = go.AddComponent<SceneTransition>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvas.enabled = false;
        }

        private async void TransitionAsync(string sceneName)
        {
            if (transitioning)
            {
                // A splash is already covering the screen; switch under it.
                SceneManager.LoadScene(sceneName);
                return;
            }

            transitioning = true;
            canvas.enabled = true;
            Transform splash = CreateSplashContent();

            float start = Time.unscaledTime;
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);

            if (load != null)
                await load;

            float remaining = MinDurationSeconds - (Time.unscaledTime - start);
            if (remaining > 0f)
                await Awaitable.WaitForSecondsAsync(remaining);

            await Awaitable.NextFrameAsync();

            if (splash != null)
                Destroy(splash.gameObject);
            canvas.enabled = false;
            transitioning = false;
        }

        private Transform CreateSplashContent()
        {
            GameObject prefab = Resources.Load<GameObject>(SplashPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SceneTransition] No prefab at Assets/Resources/{SplashPrefabPath} — using a plain panel. Set its Texture/Prefab up to get the real loading art.");
                Image panel = new GameObject("LoadingPanel").AddComponent<Image>();
                panel.transform.SetParent(transform, false);
                panel.color = fallbackColor;
                panel.rectTransform.anchorMin = Vector2.zero;
                panel.rectTransform.anchorMax = Vector2.one;
                panel.rectTransform.sizeDelta = Vector2.zero;
                return panel.transform;
            }

            GameObject content = Instantiate(prefab, transform);
            RectTransform rt = content.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            return content.transform;
        }
    }
}
