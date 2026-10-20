using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;

namespace Api
{
    /// <summary>
    /// Result of a native sign-in flow, delivered from Java/Objective-C as
    /// JSON (field names are the wire contract with AuthBridge.java and
    /// NativeAuth.mm — do not rename).
    /// </summary>
    [Serializable]
    public class NativeAuthResult
    {
        public bool success;
        public string provider;      // "Google" or "Apple"
        public string idToken;       // the JWT sent to the backend
        public string userId;        // provider subject claim
        public string email;         // when the provider reveals it
        public string displayName;
        public string givenName;
        public string familyName;
        public string photoUrl;
        public string error;         // machine-readable: cancelled, config_missing, ...
        public string errorMessage;

        public bool Cancelled => error == "cancelled";
    }

    [Serializable]
    internal class NativeAuthEvent
    {
        public string type;   // "apple_credential_state"
        public string userId;
        public string value;  // authorized | revoked | notFound | transferred | unknown
    }

    // Claims carried inside Google/Apple ID tokens (keys are the wire names).
    [Serializable]
    internal class IdTokenClaims
    {
        [JsonProperty("email")] public string email;
        [JsonProperty("name")] public string name;
        [JsonProperty("picture")] public string picture;
        [JsonProperty("given_name")] public string givenName;
        [JsonProperty("family_name")] public string familyName;
    }

    /// <summary>
    /// Native Google and Apple sign-in without any Unity plugin: Java
    /// (Google Credential Manager) on Android, Objective-C (OAuth 2.0 + PKCE /
    /// ASAuthorizationAppleIDProvider) on iOS. Raw ID tokens are returned; the
    /// backend exchange lives in SocialLoginService.
    /// Call SignInWithGoogle/Apple from the main thread. Results arrive on the
    /// main thread.
    /// </summary>
    public static class NativeAuth
    {
        private const string DispatcherObjectName = "NativeAuthDispatcher";

        public static bool IsGoogleSupported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return !string.IsNullOrEmpty(ApiConfig.Instance.GoogleServerClientId);
#elif UNITY_IOS && !UNITY_EDITOR
                return !string.IsNullOrEmpty(ApiConfig.Instance.GoogleIosClientId);
#else
                return false;
#endif
            }
        }

        public static bool IsAppleSupported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static Awaitable<NativeAuthResult> SignInWithGoogleAsync() => ToAwaitable(SignInWithGoogle, "Google");

        public static Awaitable<NativeAuthResult> SignInWithAppleAsync() => ToAwaitable(SignInWithApple, "Apple");

        /// <summary>Runs the native Google sign-in flow; the callback fires on the main thread.</summary>
        public static void SignInWithGoogle(Action<NativeAuthResult> onResult)
        {
            var dispatcher = NativeAuthDispatcher.Ensure();
            dispatcher.BeginGoogle(onResult);

#if UNITY_ANDROID && !UNITY_EDITOR
            string serverClientId = ApiConfig.Instance.GoogleServerClientId;
            if (string.IsNullOrEmpty(serverClientId))
            {
                dispatcher.CompleteGoogle(Failure("Google", "config_missing",
                    "Google server client ID is not configured on the ApiConfig asset."));
                return;
            }

            try
            {
                using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaClass bridge = new AndroidJavaClass("com.lovie.auth.AuthBridge");
                bridge.CallStatic("signInWithGoogle", activity, serverClientId);
            }
            catch (Exception e)
            {
                dispatcher.CompleteGoogle(Failure("Google", "native_error",
                    $"Failed to start Google Credential Manager: {e.Message}"));
            }
#elif UNITY_IOS && !UNITY_EDITOR
            LovieAuth_GoogleSignIn(ApiConfig.Instance.GoogleIosClientId);
#else
            dispatcher.CompleteGoogle(Failure("Google", "unsupported",
                "Google sign-in is only available on Android and iOS devices."));
#endif
        }

        /// <summary>Runs the native Sign in with Apple flow (iOS only); the callback fires on the main thread.</summary>
        public static void SignInWithApple(Action<NativeAuthResult> onResult)
        {
            var dispatcher = NativeAuthDispatcher.Ensure();
            dispatcher.BeginApple(onResult);

#if UNITY_IOS && !UNITY_EDITOR
            LovieAuth_AppleSignIn();
#else
            dispatcher.CompleteApple(Failure("Apple", "unsupported",
                "Sign in with Apple is only available on iOS devices."));
#endif
        }

        /// <summary>
        /// Checks whether the user's Apple ID is still authorized for this app.
        /// Reports "authorized", "revoked", "notFound", "transferred" or "unknown".
        /// iOS only; unsupported platforms report "unknown".
        /// </summary>
        public static void GetAppleCredentialState(string appleUserId, Action<string> onResult)
        {
            if (string.IsNullOrEmpty(appleUserId))
            {
                onResult?.Invoke("unknown");
                return;
            }

            var dispatcher = NativeAuthDispatcher.Ensure();
            dispatcher.BeginCredentialState(onResult);

#if UNITY_IOS && !UNITY_EDITOR
            LovieAuth_AppleGetCredentialState(appleUserId);
#else
            dispatcher.CompleteCredentialState("unknown");
#endif
        }

        /// <summary>Cancels a running Google flow (dismisses the account sheet / web session).</summary>
        public static void CancelGoogle()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaClass bridge = new AndroidJavaClass("com.lovie.auth.AuthBridge");
                bridge.CallStatic("cancelGoogleSignIn");
            }
            catch (Exception)
            {
                // Nothing to cancel — the flow is not running.
            }
#elif UNITY_IOS && !UNITY_EDITOR
            LovieAuth_GoogleCancel();
#endif
        }

        private static NativeAuthResult Failure(string provider, string code, string message) => new NativeAuthResult
        {
            success = false,
            provider = provider,
            error = code,
            errorMessage = message
        };

        /// <summary>
        /// Fills email/name/photo from the ID token's own claims when the
        /// platform didn't report them directly: Android Credential Manager
        /// exposes no email, and Apple only reveals name/email on the first
        /// authorization. Display-only — the backend re-derives claims from
        /// the verified token.
        /// </summary>
        private static void FillMissingProfileFromToken(NativeAuthResult result)
        {
            if (!result.success || string.IsNullOrEmpty(result.idToken)) return;

            try
            {
                string[] parts = result.idToken.Split('.');
                if (parts.Length < 2) return;

                string base64 = parts[1].Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

                string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                IdTokenClaims claims = JsonConvert.DeserializeObject<IdTokenClaims>(json);
                if (claims == null) return;

                if (string.IsNullOrEmpty(result.email)) result.email = claims.email;
                if (string.IsNullOrEmpty(result.displayName)) result.displayName = claims.name;
                if (string.IsNullOrEmpty(result.photoUrl)) result.photoUrl = claims.picture;
                if (string.IsNullOrEmpty(result.givenName)) result.givenName = claims.givenName;
                if (string.IsNullOrEmpty(result.familyName)) result.familyName = claims.familyName;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NativeAuth] Could not decode profile claims from token: {e.Message}");
            }
        }

        private static Awaitable<NativeAuthResult> ToAwaitable(Action<Action<NativeAuthResult>> signIn, string provider)
        {
            var completion = new AwaitableCompletionSource<NativeAuthResult>();
            signIn(result => completion.TrySetResult(result ?? Failure(provider, "native_error", "Native sign-in returned no result.")));
            return completion.Awaitable;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void LovieAuth_GoogleSignIn(string clientId);
        [DllImport("__Internal")] private static extern void LovieAuth_GoogleCancel();
        [DllImport("__Internal")] private static extern void LovieAuth_AppleSignIn();
        [DllImport("__Internal")] private static extern void LovieAuth_AppleGetCredentialState(string userId);
#endif

        /// <summary>
        /// Hidden persistent GameObject receiving UnitySendMessage callbacks
        /// from Java (AuthBridge) and Objective-C (NativeAuth.mm) — both
        /// hardcode the name "NativeAuthDispatcher".
        /// </summary>
        private class NativeAuthDispatcher : MonoBehaviour
        {
            private Action<NativeAuthResult> pendingGoogle;
            private Action<NativeAuthResult> pendingApple;
            private Action<string> pendingCredentialState;

            public static NativeAuthDispatcher Ensure()
            {
                var existing = FindFirstObjectByType<NativeAuthDispatcher>(FindObjectsInactive.Exclude);
                if (existing != null) return existing;

                GameObject go = new GameObject(DispatcherObjectName);
                var dispatcher = go.AddComponent<NativeAuthDispatcher>();
                DontDestroyOnLoad(go);
                return dispatcher;
            }

            public void BeginGoogle(Action<NativeAuthResult> onResult)
            {
                pendingGoogle?.Invoke(Failure("Google", "superseded", "Another Google sign-in was started."));
                pendingGoogle = onResult;
            }

            public void BeginApple(Action<NativeAuthResult> onResult)
            {
                pendingApple?.Invoke(Failure("Apple", "superseded", "Another Apple sign-in was started."));
                pendingApple = onResult;
            }

            public void BeginCredentialState(Action<string> onResult)
            {
                pendingCredentialState = onResult;
            }

            public void CompleteGoogle(NativeAuthResult result)
            {
                var callback = pendingGoogle;
                pendingGoogle = null;
                callback?.Invoke(result);
            }

            public void CompleteApple(NativeAuthResult result)
            {
                var callback = pendingApple;
                pendingApple = null;
                callback?.Invoke(result);
            }

            public void CompleteCredentialState(string value)
            {
                var callback = pendingCredentialState;
                pendingCredentialState = null;
                callback?.Invoke(value);
            }

            // Called from Java via UnitySendMessage("NativeAuthDispatcher", "OnNativeAuthResult", json).
            public void OnNativeAuthResult(string json)
            {
                NativeAuthResult result;
                try
                {
                    result = JsonUtility.FromJson<NativeAuthResult>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NativeAuth] Unparseable native result: {e.Message}");
                    result = Failure("Google", "native_error", "Unparseable native sign-in result.");
                }

                if (result == null)
                {
                    result = Failure("Google", "native_error", "Empty native sign-in result.");
                }

                FillMissingProfileFromToken(result);

                if (result.provider == "Apple") CompleteApple(result);
                else CompleteGoogle(result);
            }

            public void OnNativeAuthEvent(string json)
            {
                try
                {
                    var evt = JsonUtility.FromJson<NativeAuthEvent>(json);
                    if (evt?.type == "apple_credential_state")
                        CompleteCredentialState(string.IsNullOrEmpty(evt.value) ? "unknown" : evt.value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NativeAuth] Unparseable native event: {e.Message}");
                    CompleteCredentialState("unknown");
                }
            }
        }
    }
}
