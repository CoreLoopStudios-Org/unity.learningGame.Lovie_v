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

## 2. Environments & Testing (Development vs Production)

The API SDK is configured to support two entirely separate environments: **Development** and **Production**. 

### 🌐 API Base URLs
| Environment | Base URL Domain | Purpose |
| :--- | :--- | :--- |
| **Development** | `https://dev-api.imaginemebylovie.com/api` | Used for Unity Editor testing and Development Builds. Uses volatile mock data. |
| **Production** | `https://api.imaginemebylovie.com/api` | Used for final release builds. Connects to the live PostgreSQL database. |

### How the Environment is Detected
Environment switching is handled automatically in `Assets/Scripts/API/ApiConfig.cs` using Unity's C# Preprocessor Directives.

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    return RemoteDevUrl; // Points to dev-api.imaginemebylovie.com
#else
    return ProductionUrl; // Points to api.imaginemebylovie.com
#endif
```

### 🛠️ How to Test in the Development Environment
When you test the game by clicking **Play in the Unity Editor**, or when you create a **Development Build**, the SDK automatically connects to `https://dev-api.imaginemebylovie.com/api` (or your local backend if configured).
* **The Database:** The Dev API runs on a volatile, **InMemory database**. It is wiped clean every time the server restarts.
* **Mock Data:** It comes pre-seeded with dummy users, mock store items (Dragon Hat, Magic Wand), and sample stories/quizzes.
* **Manual Override:** If you need to force the Unity Editor to test against the live Production database, you can select the `ApiConfig.asset` file in your `Resources` folder and change the **Environment** dropdown in the Inspector from `Auto` to `Production`.

#### Dummy Login Credentials (Development ONLY)
Because the development database requires valid JWT tokens, it is seeded with the following dummy accounts on startup. You MUST use these exact credentials to test the UI endpoints:

| Role | Username / Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@dev.local` | `Admin123!` |
| **Parent** | `parent@dev.local` | `Parent123!` |
| **Parent 2** | `parent2@dev.local` | `Parent123!` |
| **Parent 3** | `parent3@dev.local` | `Parent123!` |
| **Parent 4** | `parent4@dev.local` | `Parent123!` |
| **Child** (Parent User) | `dev_child` | `Child123!` |
| **Child** (Parent 2) | `dev_child2` | `Child123!` |
| **Child** (Parent 2) | `dev_child3` | `Child123!` |
| **Child** (Parent 3) | `dev_child4` | `Child123!` |
| **Child** (Parent 3) | `dev_child5` | `Child123!` |
| **Child** (Parent 3) | `dev_child6` | `Child123!` |
| **Child** (Parent 4) | `dev_child7` | `Child123!` |
| **Child** (Parent 4) | `dev_child8` | `Child123!` |

*(Note: The Parent account already has `EmailConfirmed = true` in the development database so you can log in immediately without needing an OTP).*

### 🚀 How it Implements for Production
When you compile the final game for the App Store, Google Play, or WebGL by unchecking "Development Build" in the Build Settings, Unity automatically strips out the development URLs.
* The game will strictly connect to `https://api.imaginemebylovie.com/api`.
* It interacts with the real **PostgreSQL** database.
* The dev dummy accounts **DO NOT EXIST** in production. 

#### Temporary Production Credentials
For initial live testing, a temporary Super Admin account has been securely injected into the production database:
*   **Email:** `superadmin@imagineme.com`
*   **Password:** `Temporary_Super_Admin`

*(⚠️ Please delete this account via the Admin Dashboard or database once production testing is complete).*

---

## 3. Developer Guide: How to Make API Calls

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

### New Admin API Features (Added Aug 2026)
The `AdminApi` class has been expanded to support extended dashboard features:
*   **Sorting & Recent Stories:** Use `adminApi.GetStoriesAsync("newest")` or `"alphabetical"`. Fetch top 10 recent stories via `adminApi.GetRecentStoriesAsync()`.
*   **Advanced Stats:** `AdminStats` now includes lists for `mostWatchedStories` and `mostPlayedGames` (containing name, category, and thumbnailUrl), and `totalEarnings`.
*   **Updated Stats Contract (Sept 2026):** `AdminStats` no longer has `activeChildren` — use `totalChildren` instead (all created children: banned counted, only hard-deleted excluded). `AdminHomePanelController` binds its kids tile to `stats.totalChildren`. The `UserStatsDto` from `GET /api/admin/users/stats` renamed `parentCount`→`totalParents` and `adminCount`→`totalAdmins`.
*   **Profile Management:** Admins can view their profile (`GetProfileAsync`) and update their email/password securely (`UpdateCredentialsAsync`).
*   **Store Integration:** Convert any story into a store item instantly using `adminApi.AddStoryToStoreAsync(storyId)`.
*   **Media Upload:** Upload a `.png` or `.jpg` directly to the server using `await adminApi.UploadMediaAsync(fileBytes, "image.png")`. It returns the URL string which can be saved to a Story or Store Item.
*   **Story Pricing (Sept 2026):** `priceInCoins` on `StoryDto`/`StoreItemDto` is always `>= 0` (`0 = Free, >0 = Paid`); creating/updating with a negative value returns `400 Bad Request`. Admin story responses always carry `isUnlocked: true` (admins bypass pricing) — drive any Free/Paid badge in the admin panel from `priceInCoins > 0`, never from `isUnlocked`.

---

## 4. Final Integration: Scene Wiring Instructions

While the C# API logic is complete, the final step is wiring the UI Controllers to Unity Scenes in the Editor. **AI Agents cannot do this automatically**; a human developer must open the Unity Editor to attach the scripts.

### Required Editor Wiring Checklist:
1.  **Bootstrap Scene (`SceneBootstrap.cs`)**: Attach this to an empty GameObject in the initial loading scene. It ensures `ApiClient` is initialized and listens for `OnSessionExpired`.
2.  **Child Login (`ChildLoginController.cs`)**: Attach to the login panel in `Main Game/Children/Login`. Assign the Username input, Password input, and Login button in the Inspector.
3.  **Parent Login (`ParentLoginController.cs`)**: Attach to `Main Game/Parent/Parent Login`.
4.  **Parent Dashboard (`ParentDashboardController.cs`)**: Attach to the parent dashboard scene. Wire up the "Create Child" inputs, "View Activities" button, and text fields.
5.  **Store Scene (`StoreUIController.cs`)**: Attach to the store UI. Link the `itemsContainer` transform, the `itemCardPrefab`, and tab buttons.
6.  **Sight Word Pop**: In the Sight Word Pop scene, select the **Start Button** and ensure its UnityEvent `OnClick()` is linked to `UIManager.OnStartButtonPressed()`.

---

## 5. Final Integration: Fixing Mini-Game Score Tracking

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

## 6. Offline & Caching Services

The API SDK includes built-in services to handle offline play for children:
*   **`ContentCacheService`**: Automatically caches Stories, Quizzes, and Store Items to local JSON files when fetched. If the API fails due to no internet, it falls back to the cache.
*   **`OfflineActivityQueue`**: If `ChildApi.LogGameActivityAsync` fails due to no network, the activity is dumped into `OfflineActivityQueue`. When `ContentSyncManager` detects network restoration, it pushes the queued activities silently in the background.

No extra work is required to use these; they are integrated into `GameCompletionService` and `StoreService` automatically.

## 7. Economy & IAP Security Constraints (Backend Integrity)

The backend has been hardened to prevent exploits, meaning the Unity Client must be prepared to handle `400 Bad Request` or `500 Internal Server Error` responses gracefully if a child triggers concurrent economy actions.

*   **Negative Balance Prevention:** The PostgreSQL database strictly enforces `Coins >= 0`. If the Unity client allows a child to rapidly purchase two items simultaneously and they don't have enough coins for both, the database will forcefully abort the second transaction. The client should catch the resulting error and refresh the coin balance from the server.
*   **Duplicate IAP Receipts:** `POST /api/child/store/iap/process` enforces a strictly unique `TransactionId`. If the Unity client retries sending the same Apple/Google receipt twice during a network hiccup, the database will block the duplicate. Unity should catch the error and treat the transaction as "Already Processed" rather than a hard failure.
*   **Double-Purchasing Stories:** A composite primary key ensures a child cannot own the same story twice. If the user spam-clicks the "Buy Story" button, the subsequent requests will be rejected by the backend.
*   **Price Validation (Sept 2026):** The database enforces `PriceInCoins >= 0` on `Stories` and `StoreItems`. A `POST /api/child/stories/{id}/purchase` also returns `400 Bad Request` if the story is free (`priceInCoins <= 0`) or not `Published` — surface those errors as a friendly message instead of treating them as network failures.
