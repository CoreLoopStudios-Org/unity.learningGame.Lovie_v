using System;
using Api.Endpoints;
using Api.Models;
using UnityEngine;

namespace Api
{
    /// <summary>
    /// Thrown when a native Google/Apple sign-in flow itself fails or is
    /// cancelled before a token reaches the backend. Backend-side failures
    /// surface as ApiException like every other endpoint call.
    /// </summary>
    public class SocialAuthException : Exception
    {
        /// <summary>Machine-readable cause: cancelled, config_missing, unsupported, ...</summary>
        public string Code { get; }

        public bool Cancelled => Code == "cancelled";

        public SocialAuthException(string code, string message) : base(message)
        {
            Code = code;
        }
    }

    /// <summary>
    /// End-to-end social login: native Google/Apple token → POST
    /// /api/auth/google or /api/auth/apple → session stored through
    /// SessionManager. Social logins always produce a Parent session on the
    /// backend. UI hook-up example:
    /// <code>
    /// try {
    ///     AuthResponse auth = await SocialLoginService.LoginWithGoogle();
    ///     // success — SessionManager already holds the token
    /// } catch (SocialAuthException e) when (e.Cancelled) {
    ///     // user dismissed the sheet — stay on the login screen
    /// } catch (Exception e) {
    ///     // show e.Message (ApiException for server errors)
    /// }
    /// </code>
    /// </summary>
    public static class SocialLoginService
    {
        private static AuthApi authApi;

        /// <summary>True when the current platform + ApiConfig can sign in with Google.</summary>
        public static bool IsGoogleAvailable => NativeAuth.IsGoogleSupported;

        /// <summary>True on iOS devices (Sign in with Apple is iOS-only by design).</summary>
        public static bool IsAppleAvailable => NativeAuth.IsAppleSupported;

        public static async Awaitable<AuthResponse> LoginWithGoogle()
        {
            NativeAuthResult native = await NativeAuth.SignInWithGoogleAsync();
            if (!native.success || string.IsNullOrEmpty(native.idToken))
            {
                throw new SocialAuthException(native.error ?? "google_failed",
                    native.errorMessage ?? "Google sign-in did not return an ID token.");
            }

            AuthResponse response = await EnsureApi().GoogleLoginAsync(native.idToken);
            SessionManager.Instance.SetSession(response.token, response.expiresAt, "Parent");
            return response;
        }

        public static async Awaitable<AuthResponse> LoginWithApple()
        {
            NativeAuthResult native = await NativeAuth.SignInWithAppleAsync();
            if (!native.success || string.IsNullOrEmpty(native.idToken))
            {
                throw new SocialAuthException(native.error ?? "apple_failed",
                    native.errorMessage ?? "Apple sign-in did not return an identity token.");
            }

            // Apple reveals the name only on the very first authorization; the
            // backend uses it then (and only then) to create the account.
            string firstName = string.IsNullOrEmpty(native.givenName) ? null : native.givenName;
            string lastName = string.IsNullOrEmpty(native.familyName) ? null : native.familyName;

            AuthResponse response = await EnsureApi().AppleLoginAsync(native.idToken, firstName, lastName);
            SessionManager.Instance.SetSession(response.token, response.expiresAt, "Parent");
            return response;
        }

        /// <summary>
        /// True when the stored Apple ID is still authorized. On revoke the
        /// session should be treated as expired (the backend has no Apple
        /// re-validation). Unsupported platforms report "unknown".
        /// </summary>
        public static void CheckAppleCredentialState(string appleUserId, Action<string> onResult)
        {
            NativeAuth.GetAppleCredentialState(appleUserId, onResult);
        }

        private static AuthApi EnsureApi()
        {
            return authApi ??= new AuthApi(ApiClient.Instance);
        }
    }
}
