using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mascot.Tests
{
    /// <summary>
    /// Smoke test suite for the Mascot System.
    /// Can be run from context menu or from a GameObject in Play mode.
    /// </summary>
    public class MascotSmokeTest : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = false;

        private void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run Mascot Smoke Tests")]
        public void RunAllTests()
        {
            Debug.Log("🧪 [MascotSmokeTest] Starting Mascot System Smoke Tests...");

            int passed = 0;
            int total = 0;

            void Run(string testName, Action test)
            {
                total++;
                try
                {
                    test();
                    passed++;
                    Debug.Log($"  ✅ [PASS] {testName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"  ❌ [FAIL] {testName}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            // Save original prefs to restore after tests
            string originalPref = MascotManager.GetSelectedMascotId();

            try
            {
                Run("Database Loads Default Asset", TestDatabaseLoads);
                Run("Database Lookups All 5 Mascots", TestDatabaseLookups);
                Run("Database Normalized Lookups", TestDatabaseNormalizedLookups);
                Run("MascotManager PlayerPrefs Save and Read", TestManagerPersistence);
                Run("MascotManager Clear Selection", TestManagerClear);
                Run("MascotSelectionButton Stroke Toggling", TestSelectionButtonStroke);
                Run("MascotAvatarDisplay Runtime Swapping", TestAvatarDisplay);
                Run("MascotSelectionPanel Full Workflow", TestPanelSelectionFlow);

                Debug.Log($"🏁 [MascotSmokeTest] Finished: {passed}/{total} tests passed.");
            }
            finally
            {
                // Restore original PlayerPrefs state
                if (!string.IsNullOrEmpty(originalPref))
                {
                    MascotManager.SetSelectedMascot(originalPref);
                }
                else
                {
                    MascotManager.ClearSelection();
                }
            }
        }

        private void TestDatabaseLoads()
        {
            var db = MascotManager.GetDatabase();
            Assert(db != null, "Default MascotDatabase should load from Resources/Mascots/MascotDatabase.");
            Assert(db.Count == 5, $"Database should contain 5 mascots, found: {db.Count}");
        }

        private void TestDatabaseLookups()
        {
            var db = MascotManager.GetDatabase();
            string[] expectedIds = { "Boy_1", "Boy_2", "Girl_3", "Girl_4", "Girl_5" };

            foreach (var id in expectedIds)
            {
                var mascot = db.GetById(id);
                Assert(mascot != null, $"Mascot '{id}' should be found in database.");
                Assert(mascot.previewSprite != null, $"Mascot '{id}' must have a preview sprite.");
                Assert(mascot.animatorController != null, $"Mascot '{id}' must have an animator controller.");
            }
        }

        private void TestDatabaseNormalizedLookups()
        {
            var db = MascotManager.GetDatabase();

            Assert(db.GetById("boy_1") != null, "Lookup 'boy_1' lowercase should work.");
            Assert(db.GetById("BOY_1") != null, "Lookup 'BOY_1' uppercase should work.");
            Assert(db.GetById("Boy 1") != null, "Lookup 'Boy 1' with space should work.");
            Assert(db.GetById("boy1") != null, "Lookup 'boy1' without underscore should work.");
            Assert(db.GetById("girl 5") != null, "Lookup 'girl 5' should work.");
        }

        private void TestManagerPersistence()
        {
            MascotManager.ClearSelection();
            Assert(!MascotManager.HasSelectedMascot(), "HasSelectedMascot should be false after clear.");

            bool eventFired = false;
            MascotData eventData = null;
            Action<MascotData> handler = data =>
            {
                eventFired = true;
                eventData = data;
            };

            MascotManager.OnMascotChanged += handler;
            try
            {
                MascotManager.SetSelectedMascot("Boy_2");
                Assert(MascotManager.HasSelectedMascot(), "HasSelectedMascot should be true after setting mascot.");
                Assert(MascotManager.GetSelectedMascotId() == "Boy_2", "GetSelectedMascotId should match 'Boy_2'.");
                Assert(eventFired, "OnMascotChanged event should have fired.");
                Assert(eventData != null && eventData.id == "Boy_2", "Event data should contain Boy_2.");
            }
            finally
            {
                MascotManager.OnMascotChanged -= handler;
            }
        }

        private void TestManagerClear()
        {
            MascotManager.SetSelectedMascot("Girl_3");
            Assert(MascotManager.HasSelectedMascot(), "Precondition failed: mascot should be set.");

            MascotManager.ClearSelection();
            Assert(!MascotManager.HasSelectedMascot(), "HasSelectedMascot should be false after ClearSelection.");
            Assert(string.IsNullOrEmpty(MascotManager.GetSelectedMascotId()), "GetSelectedMascotId should be empty string.");
        }

        private void TestSelectionButtonStroke()
        {
            var btnGo = new GameObject("TestButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var strokeGo = new GameObject("Selected Stroke", typeof(RectTransform), typeof(Image));
            strokeGo.transform.SetParent(btnGo.transform, false);

            var mascotBtn = btnGo.AddComponent<MascotSelectionButton>();
            mascotBtn.MascotId = "Test_Mascot";
            mascotBtn.ResolveReferences();

            Assert(mascotBtn.StrokeObject == strokeGo, "StrokeObject should be automatically resolved.");
            Assert(!strokeGo.activeSelf, "Stroke should initially be inactive.");

            mascotBtn.SetSelected(true);
            Assert(strokeGo.activeSelf, "Stroke should be active after SetSelected(true).");

            mascotBtn.SetSelected(false);
            Assert(!strokeGo.activeSelf, "Stroke should be inactive after SetSelected(false).");

            DestroyImmediate(btnGo);
        }

        private void TestAvatarDisplay()
        {
            var avatarGo = new GameObject("TestAvatar", typeof(RectTransform), typeof(Image), typeof(Animator));
            var display = avatarGo.AddComponent<MascotAvatarDisplay>();
            display.ResolveComponents();

            var db = MascotManager.GetDatabase();
            var boy1 = db.GetById("Boy_1");

            display.ApplyMascot(boy1);

            Assert(display.AvatarAnimator.runtimeAnimatorController == boy1.animatorController,
                "Avatar Animator should have Boy 1 animator controller assigned.");
            Assert(display.AvatarImage.sprite != null, "Avatar Image should have sprite assigned.");

            DestroyImmediate(avatarGo);
        }

        private void TestPanelSelectionFlow()
        {
            // Create simulated panel
            var panelGo = new GameObject("Mascot Selection Panel", typeof(RectTransform));
            var containerGo = new GameObject("Mascot Container", typeof(RectTransform));
            containerGo.transform.SetParent(panelGo.transform, false);

            var b1Go = new GameObject("Boy_1", typeof(RectTransform), typeof(Image), typeof(Button));
            b1Go.transform.SetParent(containerGo.transform, false);
            var stroke1 = new GameObject("Selected Stroke", typeof(RectTransform));
            stroke1.transform.SetParent(b1Go.transform, false);

            var b2Go = new GameObject("Girl_5", typeof(RectTransform), typeof(Image), typeof(Button));
            b2Go.transform.SetParent(containerGo.transform, false);
            var stroke2 = new GameObject("Selected Stroke", typeof(RectTransform));
            stroke2.transform.SetParent(b2Go.transform, false);

            var startBtnGo = new GameObject("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
            startBtnGo.transform.SetParent(panelGo.transform, false);

            // Setup avatar
            var avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Animator));
            var avatarDisplay = avatarGo.AddComponent<MascotAvatarDisplay>();
            avatarDisplay.ResolveComponents();

            // Clear PlayerPrefs to simulate first launch
            MascotManager.ClearSelection();

            var controller = panelGo.AddComponent<MascotSelectionPanelController>();
            controller.AutoDiscoverReferences();

            // Check discovery
            Assert(controller.MascotButtons.Count == 2, $"Should discover 2 mascot buttons, found: {controller.MascotButtons.Count}");
            Assert(!stroke1.activeSelf, "Stroke 1 should be inactive by default.");
            Assert(!stroke2.activeSelf, "Stroke 2 should be inactive by default.");

            // Click Girl_5 button
            var mb2 = b2Go.GetComponent<MascotSelectionButton>();
            mb2.Button.onClick.Invoke();

            Assert(stroke2.activeSelf, "Girl_5 stroke should be active after clicking.");
            Assert(!stroke1.activeSelf, "Boy_1 stroke should be inactive after clicking Girl_5.");
            Assert(controller.StagedMascotId == "Girl_5", "Staged mascot should be Girl_5.");

            // Click Start Button
            var startBtn = startBtnGo.GetComponent<Button>();
            startBtn.onClick.Invoke();

            // Panel should close, PlayerPrefs should be saved, avatar should update
            Assert(!panelGo.activeSelf, "Panel should be hidden after Start Adventure is clicked.");
            Assert(MascotManager.HasSelectedMascot(), "Mascot should now be saved in PlayerPrefs.");
            Assert(MascotManager.GetSelectedMascotId() == "Girl_5", "Saved mascot should be Girl_5.");
            Assert(avatarDisplay.CurrentMascotId == "Girl_5", "Avatar display current mascot should be Girl_5.");

            DestroyImmediate(panelGo);
            DestroyImmediate(avatarGo);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}
