# Unity API Integration Documentation

This document serves as the comprehensive guide for **Unity Developers** and **AI Agents** working on the `unity.learningGame.Lovie_v` project. It outlines the architecture of the C# API SDK, how to consume it, and the final steps required to complete the UI integration.

---

## 1. Architecture Overview

The API SDK is fully implemented and located in `Assets/Scripts/API/`. It maps directly to the backend contracts defined in `API-DOCUMENTATION.md`.

### Core Components
*   **`ApiClient.cs`**: A wrapper around `UnityWebRequest` that handles authentication headers, JSON serialization (via Newtonsoft), timeout, retry logic, and error parsing. It uses Unity 6's native `Awaitable<T>` for async operations.
*   **`ApiConfig.cs`**: A `ScriptableObject` containing the Base URL (Development/Production). It lives in `Assets/Resources/ApiConfig.asset`.
*   **`SessionManager.cs`**: The source of truth for the user's JWT authentication state. It decodes the JWT to determine the user's Role (`Admin`, `Parent`, `Child`) and automatically handles session expiration/redirection.
*   **`Endpoints/`**: Domain-specific wrappers (`AuthApi.cs`, `ChildApi.cs`, `ParentApi.cs`, `AdminApi.cs`) that provide strongly-typed methods for making backend requests.

---

## 2. Developer Guide: How to Make API Calls

If you are adding new features, here is how you interact with the backend.

### Initializing the Client
Before making any calls, the `ApiClient` must be initialized with the `ApiConfig`. This is typically handled by `SceneBootstrap.cs` on app startup.
```csharp
using Api;

var config = ApiConfig.Instance;
ApiClient.Instance.Initialize(config);
```

### Checking Authentication State
```csharp
if (!SessionManager.Instance.IsAuthenticated)
{
    // Redirect to login
}
bool isChild = SessionManager.Instance.IsChildSession;
```

### Making an Endpoint Call
Instantiate the specific API endpoint class you need and await its methods.
```csharp
using Api.Endpoints;

// Create the endpoint wrapper
var childApi = new ChildApi(ApiClient.Instance);

try 
{
    var stats = await childApi.GetStatsAsync();
    Debug.Log($"Coins: {stats.coins}, Streak: {stats.loginStreak}");
} 
catch (ApiException ex) 
{
    // ApiException contains the HTTP response code and error message
    Debug.LogError($"API Error {ex.responseCode}: {ex.Message}");
}
```

---

## 3. Final Integration: Scene Wiring Instructions

While the C# API logic is complete, the final step is wiring the UI Controllers to Unity Scenes in the Editor. **AI Agents cannot do this automatically**; a human developer must open the Unity Editor to attach the scripts.

### Required Editor Wiring Checklist:
1.  **Bootstrap Scene (`SceneBootstrap.cs`)**: Attach this to an empty GameObject in the initial loading scene. It ensures `ApiClient` is initialized and listens for `OnSessionExpired`.
2.  **Child Login (`ChildLoginController.cs`)**: Attach to the login panel in `Main Game/Children/Login`. Assign the Username input, Password input, and Login button in the Inspector.
3.  **Parent Login (`ParentLoginController.cs`)**: Attach to `Main Game/Parent/Parent Login`.
4.  **Parent Dashboard (`ParentDashboardController.cs`)**: Attach to the parent dashboard scene. Wire up the "Create Child" inputs, "View Activities" button, and text fields.
5.  **Store Scene (`StoreUIController.cs`)**: Attach to the store UI. Link the `itemsContainer` transform, the `itemCardPrefab`, and tab buttons.
6.  **Sight Word Pop**: In the Sight Word Pop scene, select the **Start Button** and ensure its UnityEvent `OnClick()` is linked to `UIManager.OnStartButtonPressed()`.

---

## 4. Final Integration: Fixing Mini-Game Score Tracking

Currently, several mini-games report "fake" perfect scores to the backend regardless of child performance. **Both Developers and AI Agents** can help fix this in the code.

**The Bug**: 
Games are hardcoding `totalRounds` as the `correctCount` when calling the `GameCompletionReporter`.
```csharp
// Currently found in RhymeTimeManager.cs (line 214), WordMatchManager.cs (line 287), etc.
completionReporter.ReportCompletion(totalRounds, totalRounds);
```

**The Fix**:
You must implement a tracking variable (`_correctAnswers`) that only increments when the child succeeds on their first try, and pass that to the reporter.

### Example Fix Workflow for AI/Devs:
1.  Open the target script (e.g., `RhymeTimeManager.cs`).
2.  Add a class variable: `private int _correctAnswers = 0;`
3.  In the method where a child makes a correct match, add `_correctAnswers++;`.
4.  Change the completion call to:
```csharp
completionReporter.ReportCompletion(_correctAnswers, totalRounds);
```
**Files that need this fix:**
*   `RhymeTimeManager.cs`
*   `WordMatchManager.cs`
*   `SentenceBuilderManager.cs`
*   `ListenWordManager.cs`

---

## 5. Offline & Caching Services

The API SDK includes built-in services to handle offline play for children:
*   **`ContentCacheService`**: Automatically caches Stories, Quizzes, and Store Items to local JSON files when fetched. If the API fails due to no internet, it falls back to the cache.
*   **`OfflineActivityQueue`**: If `ChildApi.LogGameActivityAsync` fails due to no network, the activity is dumped into `OfflineActivityQueue`. When `ContentSyncManager` detects network restoration, it pushes the queued activities silently in the background.

No extra work is required to use these; they are integrated into `GameCompletionService` and `StoreService` automatically.
