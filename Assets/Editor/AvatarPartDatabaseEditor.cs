using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Avatar
{
    /// <summary>
    /// Custom editor for AvatarPartDatabase
    /// Provides tools for managing avatar parts
    /// </summary>
    [CustomEditor(typeof(AvatarPartDatabase))]
    public class AvatarPartDatabaseEditor : UnityEditor.Editor
    {
        private AvatarPartDatabase database;
        private Vector2 scrollPosition;
        private AvatarPartCategory filterCategory = AvatarPartCategory.None;
        private string searchFilter = string.Empty;
        private bool showCreatePanel = false;
        private string newPartName = string.Empty;
        private AvatarPartCategory newPartCategory = AvatarPartCategory.Hair;

        #region Styles

        private static class Styles
        {
            public static readonly GUIContent categoryLabel = new GUIContent("Category");
            public static readonly GUIContent createButton = new GUIContent("Create New Part");
            public static readonly GUIContent refreshButton = new GUIContent("Refresh Cache");
            public static readonly GUIContent validateButton = new GUIContent("Validate All");
            public static readonly GUIContent filterLabel = new GUIContent("Filter");
            public static readonly GUIContent searchLabel = new GUIContent("Search");
            public static readonly GUIContent exportLabel = new GUIContent("Export Report");
            public static readonly GUIContent autoPopulateButton = new GUIContent("🔍 Auto-Populate from Project");
        }

        #endregion

        private void OnEnable()
        {
            database = (AvatarPartDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Header
            EditorGUILayout.LabelField("Avatar Part Database", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This database contains all avatar parts for customization. Use this editor to manage and organize your parts.", MessageType.Info);

            EditorGUILayout.Space();

            // Toolbar
            DrawToolbar();

            EditorGUILayout.Space();

            // Statistics
            DrawStatistics();

            EditorGUILayout.Space();

            // Default inspector for manual list editing
            EditorGUILayout.LabelField("Manual Editing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use the list below to manually add/remove avatar parts. Or click '🔍 Auto-Populate' above to automatically find all parts.", MessageType.None);
            serializedObject.Update();
            SerializedProperty partsList = serializedObject.FindProperty("allAvatarParts");
            EditorGUILayout.PropertyField(partsList, new GUIContent("All Avatar Parts"), true);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            // Parts list (custom view)
            DrawPartsList();

            EditorGUILayout.Space();

            // Create panel
            if (showCreatePanel)
            {
                DrawCreatePanel();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(database);
            }
        }

        #region UI Sections

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Auto-populate button (prominent position)
            if (GUILayout.Button(Styles.autoPopulateButton, EditorStyles.toolbarButton, GUILayout.Width(180)))
            {
                AutoPopulateDatabase();
            }

            GUILayout.Space(10);

            // Category filter
            filterCategory = (AvatarPartCategory)EditorGUILayout.EnumPopup(
                Styles.filterLabel,
                filterCategory,
                GUILayout.Width(150)
            );

            // Search
            searchFilter = EditorGUILayout.TextField(
                Styles.searchLabel,
                searchFilter,
                EditorStyles.toolbarTextField,
                GUILayout.Width(200)
            );

            GUILayout.FlexibleSpace();

            // Refresh button
            if (GUILayout.Button(Styles.refreshButton, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                database.BuildCache();
                Debug.Log("Avatar database cache refreshed.");
            }

            // Validate button
            if (GUILayout.Button(Styles.validateButton, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ValidateAllParts();
            }

            // Export button
            if (GUILayout.Button(Styles.exportLabel, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ExportDatabaseReport();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatistics()
        {
            var allParts = database.GetAllParts();
            var categories = database.GetAvailableCategories();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Parts:", GUILayout.Width(100));
            EditorGUILayout.LabelField(allParts.Count.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Categories:", GUILayout.Width(100));
            EditorGUILayout.LabelField(categories.Count.ToString());
            EditorGUILayout.EndHorizontal();

            // Category breakdown
            foreach (var category in categories)
            {
                var parts = database.GetPartsByCategory(category);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {category.GetDisplayName()}:", GUILayout.Width(100));
                EditorGUILayout.LabelField(parts.Count.ToString());
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPartsList()
        {
            var allParts = database.GetAllParts();

            // Apply filters
            var filteredParts = allParts.Where(p =>
            {
                if (p == null) return false;

                // Category filter
                if (filterCategory != AvatarPartCategory.None && p.Category != filterCategory)
                    return false;

                // Search filter
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    string searchLower = searchFilter.ToLower();
                    return p.DisplayName.ToLower().Contains(searchLower) ||
                           p.ItemId.ToLower().Contains(searchLower);
                }

                return true;
            }).ToList();

            // Draw list
            EditorGUILayout.LabelField($"Parts ({filteredParts.Count})", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            foreach (var part in filteredParts)
            {
                DrawPartItem(part);
            }

            EditorGUILayout.EndScrollView();

            // Create button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(showCreatePanel ? "▲ Create New Part" : "▼ Create New Part", GUILayout.Width(150)))
            {
                showCreatePanel = !showCreatePanel;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPartItem(AvatarPartItem part)
        {
            if (part == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Icon preview
            if (part.IconSprite != null && part.IconSprite.texture != null)
            {
                Texture2D texture = part.IconSprite.texture;
                float aspect = (float)part.IconSprite.rect.width / part.IconSprite.rect.height;
                float height = 50;
                float width = height * aspect;

                GUILayout.Label(texture, GUILayout.Width(width), GUILayout.Height(height));
            }

            // Info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(part.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {part.ItemId}");
            EditorGUILayout.LabelField($"Category: {part.Category.GetDisplayName()}");
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Select button
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = part;
            }

            EditorGUILayout.EndHorizontal();

            // Sprite info
            if (part.IconSprite != null || part.AvatarSprite != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (part.IconSprite != null)
                {
                    EditorGUILayout.LabelField($"Icon: {part.IconSprite.name}");
                }
                if (part.AvatarSprite != null)
                {
                    EditorGUILayout.LabelField($"Avatar: {part.AvatarSprite.name}");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCreatePanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Create New Avatar Part", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Name input
            newPartName = EditorGUILayout.TextField("Display Name:", newPartName);

            // Category selection
            newPartCategory = (AvatarPartCategory)EditorGUILayout.EnumPopup(
                "Category:",
                newPartCategory
            );

            // Create button
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(100)))
            {
                CreateNewPart();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                showCreatePanel = false;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Operations

        private void AutoPopulateDatabase()
        {
            // Find all AvatarPartItem assets in the project
            string[] guids = AssetDatabase.FindAssets("t:AvatarPartItem");
            List<AvatarPartItem> foundParts = new List<AvatarPartItem>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AvatarPartItem part = AssetDatabase.LoadAssetAtPath<AvatarPartItem>(path);
                if (part != null)
                {
                    foundParts.Add(part);
                }
            }

            if (foundParts.Count == 0)
            {
                EditorUtility.DisplayDialog("No Parts Found", "No AvatarPartItem assets found in the project.", "OK");
                return;
            }

            // Get current parts
            var currentParts = database.GetPartsList();
            if (currentParts == null)
            {
                currentParts = new List<AvatarPartItem>();
                database.SetPartsList(currentParts);
            }

            int addedCount = 0;
            int skippedCount = 0;

            // Add parts to database
            foreach (var part in foundParts)
            {
                // Skip if already in database
                if (currentParts.Contains(part))
                {
                    skippedCount++;
                    continue;
                }

                database.AddPart(part);
                addedCount++;
            }

            // Mark database as dirty
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            // Build cache with new parts
            database.BuildCache();

            // Show result
            string message = $"Successfully added {addedCount} parts to the database.";
            if (skippedCount > 0)
            {
                message += $"\n\nSkipped {skippedCount} parts that were already in the database.";
            }

            message += $"\n\nTotal parts in database: {database.GetTotalPartCount()}";

            EditorUtility.DisplayDialog("Database Updated", message, "OK");

            Debug.Log($"[Avatar Database] Auto-populated with {foundParts.Count} parts:");
            Debug.Log($"  - Hair: {foundParts.Count(p => p.Category == AvatarPartCategory.Hair)} items");
            Debug.Log($"  - Dress: {foundParts.Count(p => p.Category == AvatarPartCategory.Dress)} items");
            Debug.Log($"  - Body: {foundParts.Count(p => p.Category == AvatarPartCategory.BodyColor)} items");
            Debug.Log($"  - Accessories: {foundParts.Count(p => p.Category == AvatarPartCategory.Accessories)} items");
            Debug.Log($"  - Shoes: {foundParts.Count(p => p.Category == AvatarPartCategory.Shoes)} items");
        }

        private void CreateNewPart()
        {
            if (string.IsNullOrEmpty(newPartName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a name for the part.", "OK");
                return;
            }

            // Create asset
            var part = ScriptableObject.CreateInstance<AvatarPartItem>();
            part.name = newPartName.Replace(" ", "");

            // Set basic properties
            part.SetDisplayName(newPartName);
            part.SetCategory(newPartCategory);

            // Mark as dirty so Unity saves the changes
            EditorUtility.SetDirty(part);

            // Save asset
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Avatar Part",
                part.name,
                "asset",
                "Choose where to save the avatar part"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(part, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = part;

                showCreatePanel = false;
                newPartName = string.Empty;
            }
        }

        private void ValidateAllParts()
        {
            var allParts = database.GetAllParts();
            int invalidCount = 0;

            foreach (var part in allParts)
            {
                if (part == null) continue;

                if (!part.IsValid())
                {
                    invalidCount++;
                    Debug.LogWarning($"Invalid avatar part: {part.DisplayName} ({part.ItemId})");
                }
            }

            if (invalidCount == 0)
            {
                EditorUtility.DisplayDialog("Validation", $"All {allParts.Count} parts are valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Validation",
                    $"Found {invalidCount} invalid parts out of {allParts.Count} total. Check console for details.",
                    "OK"
                );
            }
        }

        private void ExportDatabaseReport()
        {
            var allParts = database.GetAllParts();
            string report = "Avatar Part Database Report\n";
            report += "============================\n\n";
            report += $"Total Parts: {allParts.Count}\n";
            report += $"Generated: {System.DateTime.Now}\n\n";

            var categories = database.GetAvailableCategories();
            foreach (var category in categories)
            {
                var parts = database.GetPartsByCategory(category);
                report += $"\n{category.GetDisplayName()} ({parts.Count} items):\n";

                foreach (var part in parts)
                {
                    string lockStatus = part.IsLocked ? "🔒" : "✓";
                    string defaultStatus = part.IsDefault ? "[DEFAULT]" : "";
                    report += $"  {lockStatus} {part.DisplayName} {defaultStatus}\n";
                }
            }

            // Save to file
            string path = EditorUtility.SaveFilePanel(
                "Save Report",
                "",
                "AvatarDatabaseReport.txt",
                "txt"
            );

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                EditorUtility.RevealInFinder(path);
            }
        }

        #endregion
    }
}
