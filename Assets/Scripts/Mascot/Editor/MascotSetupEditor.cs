#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Mascot.Editor
{
    public static class MascotSetupEditor
    {
        private const string DatabaseFolder = "Assets/Resources/Mascots";
        private const string DatabasePath = "Assets/Resources/Mascots/MascotDatabase.asset";
        private const string MainMenuScenePath = "Assets/Scenes/Main Game/Children/Main Menu.unity";

        [MenuItem("Tools/Lovie/Mascot/Create or Refresh Default Mascot Database", false, 10)]
        public static MascotDatabase CreateOrRefreshDatabase()
        {
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
                AssetDatabase.Refresh();
            }

            var db = AssetDatabase.LoadAssetAtPath<MascotDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MascotDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            // Define the 5 mascots
            var mascots = new List<MascotData>
            {
                CreateMascot(
                    id: "Boy_1",
                    displayName: "Boy 1",
                    previewSpritePath: "Assets/Sprites/Dashboard/Mascot Selection/Boy_1.png",
                    initialFramePath: "Assets/Sprites/Avatar/Main Menu Avatars/Boy 1/boy 1-animation_00.png",
                    controllerPath: "Assets/Sprites/Avatar/Main Menu Avatars/Boy 1/Boy 1.controller"
                ),
                CreateMascot(
                    id: "Boy_2",
                    displayName: "Boy 2",
                    previewSpritePath: "Assets/Sprites/Dashboard/Mascot Selection/Boy_2.png",
                    initialFramePath: "Assets/Sprites/Avatar/Main Menu Avatars/Boy 2/boy 2-animation_00.png",
                    controllerPath: "Assets/Sprites/Avatar/Main Menu Avatars/Boy 2/Boy 2.controller"
                ),
                CreateMascot(
                    id: "Girl_3",
                    displayName: "Girl 3",
                    previewSpritePath: "Assets/Sprites/Dashboard/Mascot Selection/Girl_3.png",
                    initialFramePath: "Assets/Sprites/Avatar/Main Menu Avatars/Girl 3/girl 3-animation_00.png",
                    controllerPath: "Assets/Sprites/Avatar/Main Menu Avatars/Girl 3/Girl 3.controller"
                ),
                CreateMascot(
                    id: "Girl_4",
                    displayName: "Girl 4",
                    previewSpritePath: "Assets/Sprites/Dashboard/Mascot Selection/Girl_4.png",
                    initialFramePath: "Assets/Sprites/Avatar/Main Menu Avatars/Girl 4/girl 4-animation_00.png",
                    controllerPath: "Assets/Sprites/Avatar/Main Menu Avatars/Girl 4/Girl 4.controller"
                ),
                CreateMascot(
                    id: "Girl_5",
                    displayName: "Girl 5",
                    previewSpritePath: "Assets/Sprites/Dashboard/Mascot Selection/Girl_5.png",
                    initialFramePath: "Assets/Sprites/Avatar/Main Menu Avatars/Girl 5/girl 5-animation_00.png",
                    controllerPath: "Assets/Sprites/Avatar/Main Menu Avatars/Girl 5/Girls 5.controller"
                )
            };

            db.EditorSetMascots(mascots);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MascotSetupEditor] Mascot Database successfully populated with {mascots.Count} mascots at '{DatabasePath}'.");
            return db;
        }

        private static MascotData CreateMascot(string id, string displayName, string previewSpritePath, string initialFramePath, string controllerPath)
        {
            Sprite preview = AssetDatabase.LoadAssetAtPath<Sprite>(previewSpritePath);
            Sprite initial = AssetDatabase.LoadAssetAtPath<Sprite>(initialFramePath);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

            if (preview == null) Debug.LogWarning($"[MascotSetupEditor] Could not load preview sprite at '{previewSpritePath}'");
            if (controller == null) Debug.LogWarning($"[MascotSetupEditor] Could not load animator controller at '{controllerPath}'");

            return new MascotData(id, displayName, preview, initial, controller);
        }

        [MenuItem("Tools/Lovie/Mascot/Setup Main Menu Mascot System", false, 20)]
        public static void SetupMainMenuScene()
        {
            var db = CreateOrRefreshDatabase();

            // Check if Main Menu is currently open, otherwise open it
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != MainMenuScenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    activeScene = EditorSceneManager.OpenScene(MainMenuScenePath);
                }
                else
                {
                    return;
                }
            }

            // 1. Setup Mascot Selection Panel
            var panelObj = GameObject.Find("Mascot Selection Panel");
            if (panelObj == null)
            {
                Debug.LogError("[MascotSetupEditor] 'Mascot Selection Panel' not found in Main Menu scene!");
                return;
            }

            var panelController = panelObj.GetComponent<MascotSelectionPanelController>();
            if (panelController == null)
            {
                panelController = Undo.AddComponent<MascotSelectionPanelController>(panelObj);
            }

            // Ensure Start Button has a Button component
            var startBtnTransform = panelObj.transform.Find("Start Button");
            if (startBtnTransform != null && startBtnTransform.GetComponent<Button>() == null)
            {
                Undo.AddComponent<Button>(startBtnTransform.gameObject);
            }

            // Ensure each Mascot button has MascotSelectionButton
            var containerTransform = panelObj.transform.Find("Mascot Container");
            if (containerTransform != null)
            {
                foreach (Transform child in containerTransform)
                {
                    if (child.GetComponent<Button>() != null)
                    {
                        var mascotBtn = child.GetComponent<MascotSelectionButton>();
                        if (mascotBtn == null)
                        {
                            mascotBtn = Undo.AddComponent<MascotSelectionButton>(child.gameObject);
                        }
                        mascotBtn.ResolveReferences();
                    }
                }
            }

            // 2. Setup Avatar on Home Page
            var homePage = GameObject.Find("Home Page");
            Transform avatarTransform = null;
            if (homePage != null)
            {
                avatarTransform = homePage.transform.Find("BG/Avatar");
            }
            if (avatarTransform == null)
            {
                var avatarObj = GameObject.Find("Avatar");
                if (avatarObj != null) avatarTransform = avatarObj.transform;
            }

            if (avatarTransform != null)
            {
                var display = avatarTransform.GetComponent<MascotAvatarDisplay>();
                if (display == null)
                {
                    display = Undo.AddComponent<MascotAvatarDisplay>(avatarTransform.gameObject);
                }
                display.ResolveComponents();
            }
            else
            {
                Debug.LogWarning("[MascotSetupEditor] 'Avatar' GameObject not found on Home Page!");
            }

            panelController.AutoDiscoverReferences();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[MascotSetupEditor] Successfully configured Mascot Selection Panel and Avatar display in Main Menu scene!");
            EditorUtility.DisplayDialog("Mascot System Setup", "Main Menu Mascot System configured successfully!\n\n- Database linked (5 mascots)\n- Selection buttons & strokes wired\n- Start button configured\n- Home Page Avatar display configured", "OK");
        }

        [MenuItem("Tools/Lovie/Mascot/Reset Mascot Selection (Clear PlayerPrefs)", false, 30)]
        public static void ResetPlayerPrefs()
        {
            MascotManager.ClearSelection();
            Debug.Log("[MascotSetupEditor] Mascot PlayerPrefs cleared. Next launch will prompt for mascot selection.");
            EditorUtility.DisplayDialog("Mascot Selection Cleared", "Selected mascot cleared from PlayerPrefs.\n\nNext app launch will prompt the user to select their mascot.", "OK");
        }

        [MenuItem("Tools/Lovie/Mascot/Run Mascot Smoke Tests", false, 40)]
        public static void RunSmokeTests()
        {
            var go = new GameObject("~MascotSmokeTestTemp");
            var test = go.AddComponent<Mascot.Tests.MascotSmokeTest>();
            test.RunAllTests();
            Object.DestroyImmediate(go);
            EditorUtility.DisplayDialog("Mascot Smoke Tests", "Smoke tests executed! Check Unity Console for detailed PASS/FAIL output.", "OK");
        }
    }
}
#endif
