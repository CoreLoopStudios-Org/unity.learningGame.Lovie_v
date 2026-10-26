#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
namespace Api.EditorTools
{
    /// <summary>
    /// Patches the generated Xcode project after an iOS build so native social
    /// sign-in works:
    ///  - registers the Google OAuth redirect URL scheme
    ///    (com.googleusercontent.apps.&lt;iOS client ID&gt;) in Info.plist,
    ///  - adds the Sign in with Apple entitlement and capability to the main
    ///    target (the provisioning profile must include the capability — see
    ///    Assets/Scripts/API/SocialAuth/README.md).
    /// The Google client ID is read from the ApiConfig asset so the build stays
    /// in sync with what the runtime uses.
    /// </summary>
    public static class NativeAuthPostProcess
    {
        private const string GoogleUrlSchemePrefix = "com.googleusercontent.apps.";
        private const string AppleSignInEntitlementKey = "com.apple.developer.applesignin";

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS) return;

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();

            AddAppleSignInCapability(project, projectPath, mainTarget, buildPath);
            AddGoogleUrlScheme(buildPath);

            project.WriteToFile(projectPath);
        }

        private static void AddAppleSignInCapability(PBXProject project, string projectPath, string mainTarget, string buildPath)
        {
            project.AddCapability(mainTarget, PBXCapabilityType.SignInWithApple);

            // AddCapability flips the Xcode capability, but with manual signing
            // the entitlements file is what actually matters — make sure it
            // exists, is linked and holds the key (idempotent).
            string entitlementsRel = project.GetBuildPropertyForAnyConfig(mainTarget, "CODE_SIGN_ENTITLEMENTS");
            if (string.IsNullOrEmpty(entitlementsRel))
            {
                entitlementsRel = "Unity-iPhone/Unity-iPhone.entitlements";
                project.SetBuildProperty(mainTarget, "CODE_SIGN_ENTITLEMENTS", entitlementsRel);
            }

            string entitlementsAbs = Path.Combine(buildPath, entitlementsRel);
            Directory.CreateDirectory(Path.GetDirectoryName(entitlementsAbs) ?? buildPath);

            PlistDocument entitlements = new PlistDocument();
            if (File.Exists(entitlementsAbs)) entitlements.ReadFromFile(entitlementsAbs);

            if (entitlements.root[AppleSignInEntitlementKey] == null)
            {
                entitlements.root.CreateArray(AppleSignInEntitlementKey).AddString("Default");
            }

            entitlements.WriteToFile(entitlementsAbs);
        }

        private static void AddGoogleUrlScheme(string buildPath)
        {
            string clientId = ApiConfig.Instance.GoogleIosClientId;

            if (string.IsNullOrEmpty(clientId))
            {
                Debug.LogWarning(
                    "[NativeAuth] ApiConfig has no Google iOS Client Id — skipping Google redirect scheme. " +
                    "Google sign-in on iOS will not work until it is set.");
                return;
            }

            string scheme = GoogleUrlSchemePrefix + clientId;

            string plistPath = Path.Combine(buildPath, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementArray urlTypes = plist.root["CFBundleURLTypes"] as PlistElementArray;
            if (urlTypes == null) urlTypes = plist.root.CreateArray("CFBundleURLTypes");

            foreach (PlistElement element in urlTypes.values)
            {
                if (element is PlistElementDict dict
                    && dict["CFBundleURLSchemes"] is PlistElementArray schemes
                    && SchemeExists(schemes, scheme))
                {
                    return; // Already registered from a previous build.
                }
            }

            PlistElementDict entry = urlTypes.AddDict();
            entry.SetString("CFBundleURLName", GoogleUrlSchemePrefix + "lovie");
            entry.CreateArray("CFBundleURLSchemes").AddString(scheme);

            plist.WriteToFile(plistPath);
        }

        private static bool SchemeExists(PlistElementArray schemes, string scheme)
        {
            foreach (PlistElement element in schemes.values)
            {
                if (element is PlistElementString s && s.value == scheme) return true;
            }
            return false;
        }
    }
}
#endif
