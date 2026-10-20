# Native Social Sign-In (Google + Apple)

Plugin-free native sign-in wired to the Imagine Me backend:

| Layer | Files |
|---|---|
| Android (Java) | `Assets/Plugins/Android/AuthBridge.java`, `Assets/Plugins/Android/GoogleCredentialActivity.java` |
| Android build | `Assets/Plugins/Android/AndroidManifest.xml`, `Assets/Plugins/Android/mainTemplate.gradle` (both enabled in Player Settings) |
| iOS (Objective-C) | `Assets/Plugins/iOS/NativeAuth.mm` |
| iOS build | `Assets/Editor/iOS/NativeAuthPostProcess.cs` |
| C# runtime | `Assets/Scripts/API/SocialAuth/NativeAuth.cs` (raw native tokens), `SocialLoginService.cs` (token → backend → session) |
| API layer | `AuthApi.GoogleLoginAsync/AppleLoginAsync`, `AuthModels` request DTOs, `ApiConfig` client-ID fields |

Google on Android uses the **Credential Manager API** (`androidx.credentials`, the modern replacement for the deprecated Google Sign-In Android SDK). Google on iOS is a plain **OAuth 2.0 + PKCE** flow inside `ASWebAuthenticationSession` — no Google SDK, no CocoaPods. Apple is native **Sign in with Apple**, iOS only.

## 1. Google Cloud Console (one project, three OAuth clients)

Create all three under *APIs & Services → Credentials* in the same Google Cloud project:

1. **Android client** — package `com.loviev.imagineme` + SHA-1 of the signing key.
   ```bash
   keytool -list -v -keystore Assets/loviev.keystore -alias loviev
   ```
   (Debug builds sign with the Unity debug keystore: `~/.android/debug.keystore`, alias `androiddebugkey`, password `android` — add that SHA-1 too, or debug builds will fail with `caller not found`/`no_credentials`.)
2. **iOS client** — the app's final iOS bundle ID (see step 4).
3. **Web application client** — its client ID is the `serverClientId` Android passes to Credential Manager; Android ID tokens carry it as their audience.

## 2. Backend environment

The backend verifies Google tokens against a **comma-separated audience list**:

```
Authentication__Google__ClientIds=<web-client-id>,<ios-client-id>
Authentication__Apple__ClientIds=<ios-bundle-id>
```

Add the iOS client ID (Android tokens already carry the web client ID). Apple's audience is the app bundle ID.

## 3. Unity ApiConfig asset

Open `Assets/Resources/ApiConfig.asset` and fill:

- **Google Server Client Id** — the *Web application* client ID (used on Android).
- **Google iOS Client Id** — the *iOS* client ID (used by the iOS flow; also read at build time to register the redirect URL scheme in Info.plist).

## 4. iOS specifics

- Set the real bundle ID in *Player Settings* (currently `com.DefaultCompany.Imagine-Me-Child`) — it is the Apple token audience and must match the Apple/backend config.
- The build post-process adds the **Sign in with Apple** entitlement + capability and the `com.googleusercontent.apps.<client-id>` URL scheme automatically.
- Project uses **manual signing**: the App ID and provisioning profile in the Apple Developer portal must have *Sign in with Apple* enabled, or codesigning will fail.

## 5. Calling it (no UI wiring yet — this is the API)

```csharp
try
{
    AuthResponse auth = await SocialLoginService.LoginWithGoogle(); // or LoginWithApple()
    // SessionManager/TokenStore already hold the Parent JWT
}
catch (SocialAuthException e) when (e.Cancelled) { /* user dismissed the sheet */ }
catch (Exception e) { /* e.Message covers backend errors (ApiException) */ }
```

Lower-level access: `NativeAuth.SignInWithGoogleAsync()/SignInWithAppleAsync()` return the raw provider token; `NativeAuth.IsGoogleSupported/IsAppleSupported` gate the UI; `SocialLoginService.CheckAppleCredentialState(userId, s => ...)` reports `"authorized" | "revoked" | "notFound" | "transferred" | "unknown"` on app focus.

## Notes

- Android needs Google Play services ( Credential Manager does not run on HMS/de-Googled devices).
- Error codes carried by `SocialAuthException.Code`: `cancelled`, `config_missing`, `unsupported`, `no_credentials`, `state_mismatch`, `token_exchange_failed`, `network_error`, ...
- The backend issues a 24 h JWT with no refresh: existing session-expiry handling in `SessionManager` applies unchanged.
- Device test matrix: first-ever sign-in (account picker + consent), returning user (one-tap sheet), cancelled flow, no Google account on device.
