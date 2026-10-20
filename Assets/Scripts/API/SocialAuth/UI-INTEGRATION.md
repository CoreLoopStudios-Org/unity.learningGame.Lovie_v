# UI Integration Guide — Social Sign-In Buttons

For the Unity developer wiring the login UI. The whole native + backend pipeline
is done — you only call one method per provider. Two buttons on the **Parent
Login** screen:

- **Continue with Google** — Android + iOS device builds
- **Continue with Apple** — iOS device builds only (auto-hidden elsewhere)

Runtime API lives in `Assets/Scripts/API/SocialAuth/` (`Api` namespace):
`SocialLoginService.LoginWithGoogle()` / `LoginWithApple()`. On success the
Parent JWT is already stored in `SessionManager`/`TokenStore` — you only load
the dashboard scene, exactly like the email flow.

## 1. Scene setup (Parent Login scene)

1. Add a **Button** below the login form: *"Continue with Google"*. Google "G"
   icon on white (light theme) / dark (dark theme) button; follow Google's
   branding rules ("Sign in with Google" wording is also fine).
2. Add a second **Button**: *"Continue with Apple"*. Must follow Apple's
   Sign in with Apple button guidelines when it ships (Apple logo + black/white
   style, Touch/Face/Apple ID wording variants allowed).
3. Drop both references onto `ParentLoginController` (new serialized fields
   below). No other scene objects are needed — no manager prefab, the native
   bridge creates its own hidden dispatcher at runtime.

## 2. Controller code — add to `ParentLoginController.cs`

```csharp
[Header("Social Sign-In")]
[SerializeField] private Button googleSignInButton;
[SerializeField] private Button appleSignInButton;

private bool socialFlowRunning;

// --- add inside Start() ---
if (googleSignInButton != null)
{
    googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
    // Hidden in the Editor too (unsupported there) — toggle it manually in the
    // scene view while designing layout.
    googleSignInButton.gameObject.SetActive(SocialLoginService.IsGoogleAvailable);
}
if (appleSignInButton != null)
{
    appleSignInButton.onClick.AddListener(OnAppleSignInClicked);
    // iOS only — auto-hidden on Android, Editor, and until iOS config exists.
    appleSignInButton.gameObject.SetActive(SocialLoginService.IsAppleAvailable);
}

// --- new handlers ---
async void OnGoogleSignInClicked()
{
    if (socialFlowRunning) return;
    socialFlowRunning = true;
    ShowLoading();
    ClearError();

    try
    {
        await SocialLoginService.LoginWithGoogle();  // sheet → backend → session stored
        SceneTransition.Load(dashboardScene);
    }
    catch (SocialAuthException ex)
    {
        if (!ex.Cancelled)  // user closed the sheet — no error spam, just stay put
        {
            ShowError(ex.Code == "config_missing"
                ? "Google sign-in is not set up on this build."
                : ex.Message);
        }
    }
    catch (ApiException ex)
    {
        HandleApiError(ex);  // same handler as email login (backend messages)
    }
    catch (Exception ex)
    {
        Debug.LogError($"Google sign-in error: {ex.Message}");
        ShowError("Something went wrong. Please try again.");
    }
    finally
    {
        socialFlowRunning = false;
        HideLoading();
    }
}

async void OnAppleSignInClicked()
{
    // Same shape; Apple names arrive only on the very first authorization and
    // are forwarded by the service automatically.
    if (socialFlowRunning) return;
    socialFlowRunning = true;
    ShowLoading();
    ClearError();

    try
    {
        await SocialLoginService.LoginWithApple();
        SceneTransition.Load(dashboardScene);
    }
    catch (SocialAuthException ex)
    {
        if (!ex.Cancelled) ShowError(ex.Message);
    }
    catch (ApiException ex)
    {
        HandleApiError(ex);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Apple sign-in error: {ex.Message}");
        ShowError("Something went wrong. Please try again.");
    }
    finally
    {
        socialFlowRunning = false;
        HideLoading();
    }
}

// --- add to OnDestroy() ---
if (googleSignInButton != null) googleSignInButton.onClick.RemoveListener(OnGoogleSignInClicked);
if (appleSignInButton != null) appleSignInButton.onClick.RemoveListener(OnAppleSignInClicked);
```

## 3. Behavior notes

- Everything is main-thread; `async void` handlers match the existing style.
- During the native flow an OS sheet overlays the app — keep the loading
  indicator until the await resolves; the `socialFlowRunning` guard prevents
  double-launching (a second tap while a sheet is up would otherwise restart
  the flow).
- `SocialAuthException.Code` reference: `cancelled` (stay silent),
  `config_missing` (client IDs missing in ApiConfig), `no_credentials`
  (no matching account / SHA‑1 mismatch on device), `unsupported`,
  `state_mismatch`, `token_exchange_failed`, `network_error`.
- Backend rejections surface as `ApiException` with the server's message —
  e.g. *"This email is already linked to a different Google/Apple account."*
  `HandleApiError` already displays these correctly.
- Do **not** create `AuthApi`/`ApiClient` for these calls — the service owns
  that; reusing the controller's `authApi` instance is unnecessary.

## 4. Testing checklist

- Editor: both buttons hidden (expected — native flows don't run in Editor).
- Android device build: Google button visible → tap → account sheet → success
  lands on Parent Dashboard; cancel path returns quietly to the form.
- `SocialAuthSmokeTest` (same folder) remains available for pipeline testing
  without any UI.
- iOS device build: Google appears only after the iOS client ID is added to
  ApiConfig; Apple button requires the capability/provisioning (see README).
