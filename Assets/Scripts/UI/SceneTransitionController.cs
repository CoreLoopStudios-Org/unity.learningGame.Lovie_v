using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Loads a scene through SceneTransition so the splash covers the load.
    // Drop it on a Button and the click is wired automatically; leave it on any
    // other object and call LoadScene() from code or a UnityEvent instead.
    public class SceneTransitionController : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        private Button button;

        void Start()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(LoadScene);
        }

        public void LoadScene()
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[SceneTransitionController] No scene name set on '{name}'.", this);
                return;
            }

            SceneTransition.Load(sceneName);
        }

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(LoadScene);
        }
    }
}
