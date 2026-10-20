using System;
using Api.Endpoints;
using Api.Models;
using UnityEngine;

namespace Api
{
    /// <summary>
    /// Device smoke test for native social sign-in (drop into a scene, tick
    /// "Run On Start", build a Development Build). Follows the ApiSmokeTest
    /// pattern: every step logs with Unity's console so it can be read from
    /// adb logcat (-s Unity) or Xcode.
    ///
    /// Expected on Android: Google sheet appears → account chosen → session
    /// stored as Parent → profile fetch proves the JWT works. On iOS Google
    /// needs the iOS client ID in ApiConfig first; Apple runs once the
    /// Sign in with Apple capability is provisioned.
    /// </summary>
    public class SocialAuthSmokeTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private bool testGoogle = true;
        [SerializeField] private bool testApple = false;

        async void Start()
        {
            if (!runOnStart) return;
            await RunTests();
        }

        public async Awaitable RunTests()
        {
            Debug.Log("🧪 Social auth smoke test — supported flags: " +
                      $"Google={SocialLoginService.IsGoogleAvailable} Apple={SocialLoginService.IsAppleAvailable}");

            if (!SocialLoginService.IsGoogleAvailable && !SocialLoginService.IsAppleAvailable)
            {
                Debug.LogWarning("⚠️ No social provider available on this platform/config — " +
                                 "check ApiConfig client IDs. Nothing to test.");
                return;
            }

            if (testGoogle && SocialLoginService.IsGoogleAvailable)
            {
                await TestProvider("Google", SocialLoginService.LoginWithGoogle);
            }

            if (testApple && SocialLoginService.IsAppleAvailable)
            {
                await TestProvider("Apple", SocialLoginService.LoginWithApple);
            }
        }

        private async Awaitable TestProvider(string provider, Func<Awaitable<Models.AuthResponse>> login)
        {
            Debug.Log($"🔑 Testing {provider} sign-in (pick an account on the device)...");

            try
            {
                var auth = await login();

                Debug.Log($"✅ {provider} login succeeded:" +
                          $"\n  - Token: {auth.token.Substring(0, Math.Min(20, auth.token.Length))}..." +
                          $"\n  - Expires: {auth.expiresAt}");
            }
            catch (SocialAuthException ex)
            {
                if (ex.Cancelled)
                {
                    Debug.LogWarning($"⚠️ {provider} sign-in cancelled by user — flow aborted cleanly.");
                    return;
                }
                Debug.LogError($"❌ {provider} native flow failed [{ex.Code}]: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ {provider} backend exchange failed: {ex.Message}");
                return;
            }

            // Session must be stored as a Parent for the bearer header to work.
            if (!SessionManager.Instance.IsAuthenticated)
            {
                Debug.LogError($"❌ {provider}: login returned but SessionManager has no valid session.");
                return;
            }
            Debug.Log($"✅ Session stored — role={SessionManager.Instance.Role} email={SessionManager.Instance.Email}");

            try
            {
                ParentProfile profile = await new ParentApi(ApiClient.Instance).GetProfileAsync();
                Debug.Log($"✅ Authenticated parent profile fetched:" +
                          $"\n  - ID: {profile.id}" +
                          $"\n  - Name: {profile.fullName}" +
                          $"\n  - Email: {profile.email}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Parent profile fetch failed with the social session: {ex.Message}");
            }
        }
    }
}
