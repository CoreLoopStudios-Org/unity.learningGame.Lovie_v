using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mascot
{
    /// <summary>
    /// Runtime bootstrapper that automatically hooks the Mascot components into the Main Menu scene
    /// if they are not already manually serialized in the scene hierarchy.
    /// Ensures 100% out-of-the-box functionality.
    /// </summary>
    internal static class MascotSceneAutoHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            HookScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HookScene(scene);
        }

        public static void HookScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            // Only hook in scenes that contain Mascot Selection Panel or Main Menu
            var rootObjects = scene.GetRootGameObjects();
            Transform mascotPanelTransform = null;
            Transform avatarTransform = null;

            foreach (var root in rootObjects)
            {
                if (mascotPanelTransform == null)
                {
                    mascotPanelTransform = FindDeepChild(root.transform, "Mascot Selection Panel");
                }
                if (avatarTransform == null)
                {
                    avatarTransform = FindDeepChild(root.transform, "Avatar");
                }
            }

            // 1. Hook Avatar
            if (avatarTransform != null)
            {
                var display = avatarTransform.GetComponent<MascotAvatarDisplay>();
                if (display == null)
                {
                    display = avatarTransform.gameObject.AddComponent<MascotAvatarDisplay>();
                }
                display.ResolveComponents();
            }

            // 2. Hook Mascot Selection Panel
            if (mascotPanelTransform != null)
            {
                var panelController = mascotPanelTransform.GetComponent<MascotSelectionPanelController>();
                if (panelController == null)
                {
                    panelController = mascotPanelTransform.gameObject.AddComponent<MascotSelectionPanelController>();
                }
                panelController.AutoDiscoverReferences();
            }
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindDeepChild(parent.GetChild(i), name);
                if (result != null) return result;
            }

            return null;
        }
    }
}
