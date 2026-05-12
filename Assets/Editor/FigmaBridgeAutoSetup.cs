using UnityEditor;
using UnityEngine;
using UnityFigmaBridge.Editor.Settings;
using System.Threading.Tasks;

[InitializeOnLoad]
public class FigmaBridgeAutoSetup
{
    private static bool isTriggered = false;

    static FigmaBridgeAutoSetup()
    {
        EditorApplication.delayCall += RunSetup;
    }

    [MenuItem("Figma Bridge/Run Auto Setup")]
    public static void RunSetup()
    {
        if (isTriggered && !EditorApplication.isPlaying) return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunSetup;
            return;
        }

        isTriggered = true;
        Debug.Log("[FigmaBridgeAutoSetup] Configuring UnityFigmaBridge...");

        string token = "figd_092vsdlQxSzDBKdQOgKxVmFo3XIBgiF6sSs4O4Z0";
        string url = "https://www.figma.com/design/G21JUmEaMCQk9l58fy6HEg/lovie_v-%7C-reading-app?node-id=181-7123&t=kWta7EwyyHmUd93n-0";

        // Set Personal Access Token
        PlayerPrefs.SetString("FIGMA_PERSONAL_ACCESS_TOKEN", token);
        PlayerPrefs.Save();

        // Create or Update Settings
        string[] guids = AssetDatabase.FindAssets("t:UnityFigmaBridgeSettings");
        UnityFigmaBridgeSettings settings = null;
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<UnityFigmaBridgeSettings>(path);
            settings.DocumentUrl = url;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        else
        {
            settings = ScriptableObject.CreateInstance<UnityFigmaBridgeSettings>();
            settings.DocumentUrl = url;
            
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
            
            AssetDatabase.CreateAsset(settings, "Assets/Settings/UnityFigmaBridgeSettings.asset");
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[FigmaBridgeAutoSetup] Setup complete. You can now run 'Figma Bridge > Sync Document' or 'Figma Bridge > Run Auto Setup' manually.");
        
        // Let's try to trigger sync automatically after a small delay
        Task.Delay(2000).ContinueWith(t => 
        {
            EditorApplication.delayCall += () =>
            {
                Debug.Log("[FigmaBridgeAutoSetup] Attempting to trigger sync...");
                EditorApplication.ExecuteMenuItem("Figma Bridge/Sync Document");
            };
        });
    }
}