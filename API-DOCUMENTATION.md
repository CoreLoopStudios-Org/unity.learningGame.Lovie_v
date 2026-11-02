# Imagine Me — API Documentation

> Single source of truth for ALL API endpoints. Every contract below was verified against the actual backend source code on 2026-08-18. Consumers: web frontends (finished) and the Unity client (planned — see `UNITY-INTEGRATION-PLAN.md`).

**Version:** 2.0 (consolidates and replaces `API-DOCUMENTATION-UNITY.md` and the previous v1 of this file — both contained contracts that did not match the deployed backend)
**Base URL:** `https://imaginemebylovie.com` (development: `http://localhost:5200`)
**Content type:** `application/json`
**Health check:** `GET /health` (anonymous)

---

## 1. Conventions

- All JSON properties are **camelCase** (`loginStreak`, not `LoginStreak`).
- All enums serialize as **numbers** (see Section 2 for the authoritative values).
- All ids are **GUID strings**. All timestamps are **ISO 8601 UTC strings**.
- JSONB payload fields (`contentPayload`, `questionsPayload`, `avatarState`, `payload`, `metadata`, `additionalData`) are **strings containing JSON**, not nested objects. Serialize before sending, parse after receiving.
- Auth header: `Authorization: Bearer {jwt}`. Tokens live 24 h; there is **no refresh endpoint** — re-login on expiry.
- Role comes from the JWT role claim: `"Admin"`, `"Parent"`, or `"Child"`. Auth responses do NOT contain a top-level `role` field.

### Error responses

Produced by the global exception middleware, always camelCase:

```json
{ "status": 404, "message": "Story with ID ... not found." }
```

| Code | Meaning |
|------|---------|
| 200 / 201 / 204 | Success / Created / No content |
| 400 | Validation failure (FluentValidation) or bad input |
| 401 | Missing/invalid token, wrong credentials |
| 403 | Valid token, wrong role |
| 404 | Resource not found |
| 409 / 400 | Business rule violation (e.g. not enough coins, already purchased) |
| 500 | Unhandled server error |

---

## 2. Enums (authoritative — from `src/ImagineMe.Domain/Enums/`)

```csharp
UserType        { Admin = 1, Parent = 2 }            // no Child member; Child is a JWT role only
ActivityType    { StoryRead = 1, QuizAttempt = 2, GamePlayed = 3, DailyLogin = 4 }
ContentStatus   { None = 0, Draft = 1, Published = 2 }
PurchaseStatus  { None = 0, Completed = 1 }          // purchases complete instantly — no approval flow
AudioType       { Narration = 1, BackgroundMusic = 2, SoundEffect = 3, FullStory = 4 }
```

> WARNING for client authors: older docs claimed `Published = 1`, `Pending/Approved/Rejected` purchase states, and `Parent = 1`. All wrong. Use the values above.

---

## 3. Authentication Endpoints (anonymous)

### 3.1 `POST /api/auth/register` — register Parent or Admin

Request:
```json
{ "email": "parent@example.com", "password": "SecurePass123!", "fullName": "John Doe", "userType": 2 }
```
`userType`: 1 = Admin, 2 = Parent.

Response — `AuthResponse`:
```json
{ "token": "eyJ...", "tokenType": "Bearer", "expiresAt": "2026-08-19T06:00:00Z" }
```

### 3.2 `POST /api/auth/login` — Parent/Admin login

Request: `{ "email": "...", "password": "..." }`
Response: `AuthResponse`.
Note: parents must verify email before login.

### 3.3 `POST /api/auth/child/login` — Child login

Request:
```json
{ "username": "childuser", "password": "childpass123", "parentId": "optional-guid" }
```

Response — `ChildAuthResponse`:
```json
{
  "token": "eyJ...", "tokenType": "Bearer", "expiresAt": "2026-08-19T06:00:00Z",
  "childId": "guid", "fullName": "Child Full Name", "username": "childuser", "coins": 120, "loginStreak": 5
}
```

Side effect: login updates the streak and awards the daily login coins (10/day, once per calendar day) server-side.

### 3.4 `POST /api/auth/send-verification`

Request: `{ "email": "..." }` → Response: `{ "message": "..." }`

### 3.5 `POST /api/auth/verify-email`

Request: `{ "email": "...", "otp": "123456" }` → Response: `{ "message": "..." }`

### 3.6 `POST /api/auth/send-reset-otp`

Request: `{ "email": "..." }` → Response: `{ "message": "..." }`

### 3.7 `POST /api/auth/reset-password`

Request: `{ "email": "...", "otp": "654321", "newPassword": "..." }` → Response: `{ "message": "..." }`

### 3.8 `POST /api/auth/google` — Sign in with Google (Parents only)

The client signs the user in with the native Google Sign-In SDK and exchanges the resulting **ID token** here. The backend verifies it against Google's published keys (issuer `accounts.google.com`, audience = configured client IDs, signature, expiry, `email_verified`) and issues the normal `AuthResponse` with a `Parent` role claim.

Request:
```json
{ "idToken": "eyJ..." }
```
Response: `AuthResponse`.

Behaviour:
- Google has already verified the email, so **no OTP email is sent**: a new account is created as `Parent` with `emailConfirmed = true` and no password.
- If a Parent account with the same email already exists (matched case-insensitively), the Google identity is linked to it and the email is marked verified; the existing password keeps working.
- `400` — `idToken` missing, or the provider is not configured on the server.
- `401` — invalid/expired token, unverified Google email, or the email belongs to an Admin account (social sign-in is Parents only).
- `403` — banned account.

### 3.9 `POST /api/auth/apple` — Sign in with Apple (Parents only)

Same exchange with Apple's identity token (issuer `https://appleid.apple.com`, audience = app Bundle ID and/or Services ID). Apple only reveals the user's name during the *first* authorization, so the client forwards it for account creation; it is ignored for existing accounts.

Request:
```json
{ "idToken": "eyJ...", "firstName": "Jane", "lastName": "Doe" }
```
`firstName` / `lastName` are optional. Response and error codes as in 3.8.

Notes for both providers:
- An account created through Google/Apple has no password. `POST /api/auth/login` for it returns `401` with `"This account uses Google or Apple sign-in. Please continue with your provider."` (deliberately without the word "verify", which the parent app treats as the unverified-email flow). Such an account can add a password through the reset flow (3.6 + 3.7).
- Server configuration: `Authentication:Google:ClientIds` and `Authentication:Apple:ClientIds`, comma-separated (env `Authentication__Google__ClientIds`, `Authentication__Apple__ClientIds`). Tokens are only accepted for these audiences.

---

## 4. Child Endpoints — `[Authorize(Roles = "Child")]`

The child id is always derived from the JWT. Never send it from the client.

### 4.1 `GET /api/child/profile` — `ChildProfileDto`

```json
{
  "id": "guid", "fullName": "Child Full Name", "username": "childuser", "coins": 120, "loginStreak": 5,
  "avatarState": "{\"hair\":\"brown\"}", "additionalData": null,
  "lastLoginDate": "2026-08-18T05:00:00Z", "lastActivityAt": "2026-08-18T05:30:00Z"
}
```

### 4.2 `GET /api/child/stats` — `ChildStatsDto`

```json
{
  "coins": 120, "loginStreak": 5, "canClaimDailyReward": false,
  "lastLoginDate": "2026-08-18T05:00:00Z", "lastActivityAt": "2026-08-18T05:30:00Z"
}
```

🔧 Planned additive fields (GAP-3): `storiesRead`, `quizzesTaken`, `gamesPlayed`, `totalCoinsEarned`, `totalCoinsSpent`.

### 4.3 `PUT /api/child/avatar`

Request: `{ "avatarState": "{\"hair\":\"brown\",\"eyes\":\"blue\"}" }` (JSON **string**)
Response: `true`

### 4.4 `POST /api/child/daily-reward` — `DailyRewardResultDto`

```json
{ "alreadyClaimed": false, "coinsAwarded": 10, "totalCoins": 130, "loginStreak": 6 }
```

Guarded per calendar day (login may have already claimed it — then `alreadyClaimed: true, coinsAwarded: 0`).

### 4.4.1 `POST /api/child/rewards/claim` — `int` (New Total Coins)

Mission-reward claim from the Unity Rewards panel. Adds `coinsAwarded` to the child's balance, records a `ChildRewardClaim` row plus a `RewardClaim` activity, and returns the new total.

Request:
```json
{ "rewardId": "play_3_games", "coinsAwarded": 25, "dayKey": null, "isDaily": false }
```

- `rewardId` — client catalog id (max 100 chars, required).
- `coinsAwarded` — clamped to `1..1000`, otherwise `400`.
- `dayKey` — `"yyyyMMdd"` for daily-reset rewards; `null` for one-time rewards (stored as `""`).

Uniqueness — unique index on `(ChildId, RewardId, DayKey)`: one-time claims are permanent (empty `dayKey`), daily claims are once per `dayKey`. A repeat claim returns `400 Bad Request` ("Reward already claimed") — the Unity client treats that as already-claimed and resyncs the balance from `GET /api/child/stats`.

### 4.4.2 `POST /api/child/profile/add-coins` — `int` (New Total Coins)

Directly adds a specified amount of coins to the child's balance. Bypasses all server-side economy validation except for a positive value check.

Request:
```json
{ "coins": 100 }
```

- `coins` — must be > 0.
Returns the child's new total coin balance.

### 4.4.3 `POST /api/child/avatar/unlock-all` — `UnlockAllAvatarsDto`

Unlocks **every avatar part** for the logged-in child at a fixed price of **999 coins** (price is owned by the server). No request body. The unlock is recorded durably as `"allAvatarsUnlocked": true` in the child's `additionalData` JSONB (visible in `GET /api/child/profile`) and an audit `ChildActivity` (`AvatarUnlockAll`) is written atomically with the coin deduction.

Response:
```json
{ "coins": 501, "allAvatarsUnlocked": true }
```

Errors:
- `400` — `"Not enough coins. Required: 999, Available: {X}."` when the balance is short (Unity shows this message verbatim).
- `400` — `"All avatars are already unlocked."` on a repeat purchase (no double charge).
- `401` — child not authenticated.

Client note: this replaces the local-only unlock in `UnlockAllPopupController`; after a successful call, refresh the coin wallet with the returned balance and honor the `additionalData.allAvatarsUnlocked` flag when checking part locks so unlocks survive restart.

### 4.5 `GET /api/child/stories` — `StoryDto[]` (Published only)

`StoryDto`:
```json
{
  "id": "guid", "title": "The Adventure", "coverImageUrl": "https://...",
  "contentPayload": "[{\"page\":1,\"text\":\"...\"}]", "status": 2,
  "priceInCoins": 200, "isUnlocked": false,
  "createdAt": "...", "updatedAt": null
}
```
Pricing contract: `priceInCoins >= 0` always (`0 = Free, >0 = Paid`). Free stories come back with `isUnlocked: true`; render Free/Paid from `priceInCoins`, ownership from `isUnlocked`.

### 4.6 `GET /api/child/stories/{id}` — `StoryDto`

### 4.6.1 `POST /api/child/stories/{id}/purchase` — `bool`
Deducts `priceInCoins` from the child's balance and unlocks the story. Returns `400 Bad Request` if the child does not have enough coins, already owns the story, the story is free (`priceInCoins <= 0`), or the story is not `Published`. The database strictly enforces `Coins >= 0`, `PriceInCoins >= 0`, and unique ownership to prevent race condition exploits.

### 4.7 `GET /api/child/quizzes?storyId={optional-guid}` — `QuizDto[]` (Published only)

`QuizDto`:
```json
{
  "id": "guid", "title": "Story Quiz", "storyId": "guid-or-null",
  "questionsPayload": "[{\"questionText\":\"...\",\"options\":[...],\"correctAnswer\":1}]",
  "status": 2, "createdAt": "...", "updatedAt": null
}
```
Note: questions are inside the `questionsPayload` JSON **string** — there is no `questions` array property.

### 4.8 `GET /api/child/quizzes/{id}` — `QuizDto`

### 4.9–4.11 Activity logging

| Endpoint | Request body |
|----------|--------------|
| `POST /api/child/activities/story` | `{ "storyId": "guid", "payload": "{\"pagesRead\":10,\"timeSpent\":300}" }` |
| `POST /api/child/activities/quiz` | `{ "quizId": "guid", "payload": "{\"score\":85,\"timeSpent\":120}" }` |
| `POST /api/child/activities/game` | `{ "payload": "{\"gameId\":\"rhyme_time\",\"score\":500,\"durationSeconds\":90}" }` |

`payload` is an opaque JSON string owned by the client.

Response — `ActivityLoggedDto`:
```json
{ "id": "guid", "activityType": 3, "createdAt": "2026-08-18T06:00:00Z" }
```

🔧 Planned additive behavior + fields (GAP-2): server-side coin awards; response gains `coinsEarned`, `totalCoins`, `message`. Coins are computed server-side — client-sent amounts are never trusted.

### 4.12 `GET /api/child/store/items` — `StoreItemDto[]` (ordered by price)

`StoreItemDto`:
```json
{
  "id": "guid", "name": "Golden Avatar", "priceInCoins": 100,
  "assetUrl": "https://...", "metadata": "{\"rarity\":\"legendary\"}",
  "createdAt": "...", "updatedAt": null
}
```

### 4.13 `POST /api/child/store/purchase`

Request: `{ "storeItemId": "guid" }`

Behavior: validates coins, **deducts immediately**, creates a `Completed` purchase in one transaction. Errors: not enough coins, already purchased. There is **no pending-approval flow**.

Response — `PurchaseDto`:
```json
{
  "id": "guid", "childId": "guid", "childUsername": "childuser",
  "storeItemId": "guid", "storeItemName": "Golden Avatar", "storeItemAssetUrl": "https://...",
  "priceInCoins": 100, "status": 1, "requestedAt": "...", "completedAt": "...",
  "rejectionReason": null
}
```

### 4.14 `GET /api/child/store/my-items` — `PurchaseDto[]`

### 4.14.1 `POST /api/child/store/iap/process` — `int` (New Total Coins)
Request: `{ "tierId": "guid", "transactionId": "string" }`
Behavior: Validates transaction ID uniqueness, retrieves tier, adds equivalent coins to child's balance, and records the IAP transaction in one database transaction. Returns the new total coins amount. Returns `400 Bad Request` if the `transactionId` has already been processed (enforced by a strict unique database index).

### 4.15 `GET /api/child/minigames?gameType={optional-string}` — `MiniGameContentDto[]` (Published only)

`gameType` is a free string, e.g. `"RhymeTime"`, `"WordWizard"`, `"PrefixSuffix"`, `"StorySequencing"`, `"StoryQuest"`, `"ReadingDetective"`.

`MiniGameContentDto`:
```json
{ "id": "guid", "gameType": "RhymeTime", "title": "Pairs", "status": 2, "createdAt": "...", "updatedAt": null }
```

### 4.16 `GET /api/child/minigames/{id}` — `MiniGameContentDetailDto` (404 if not Published)

Same as above plus `"contentPayload": "<exact JSON the Unity game parses>"`.

🔧 Planned (GAP-5): `GET /api/child/minigames/content/{gameType}?key={optional}` returning the raw `contentPayload` as `application/json` so Unity repositories can consume it exactly like their local `Resources` files. Payload shapes in Appendix A.

---

## 5. Parent Endpoints — `[Authorize(Roles = "Parent")]`

Parents can only access children where `parentId` matches their JWT.

### 5.1 `GET /api/parent/dashboard` — `ParentDashboardDto`

```json
{
  "totalChildren": 2, "activeChildren": 1,
  "childSummaries": [
    { "childId": "guid", "username": "kid1", "totalCoins": 120, "loginStreak": 5, "lastActivityAt": "...", "password": "plaintextpassword" }
  ]
}
```
Note: the array property is `childSummaries` (not `children`); coins field is `totalCoins`. The password is provided in plaintext.

### 5.2 `POST /api/parent/children` — create child

Request: `{ "fullName": "New Child", "username": "newchild", "password": "SecurePass123!" }` (fullName up to 256 chars, username 3–50 chars, password 6–100)
Response: `"guid"` (child id)

### 5.3 `GET /api/parent/children` — `ChildSummaryDto[]`

```json
[{ "id": "guid", "fullName": "Kid One", "username": "kid1", "coins": 120, "loginStreak": 5, "lastActivityAt": "...", "avatarState": "{}", "password": "plaintextpassword" }]
```

*(Note: the `password` field returns the child's password in plaintext so the parent can view it).*

### 5.4 `GET /api/parent/children/{id}` — `ChildDetailDto`

`ChildSummaryDto` fields plus `"avatarState"`, `"additionalData"`, `"lastLoginDate"`.

### 5.5 `PUT /api/parent/children/{id}` — update child

Request (all optional): `{ "fullName": "...", "username": "...", "password": "...", "avatarState": "{...}", "additionalData": "{...}" }`
Response: `true`

### 5.6 `DELETE /api/parent/children/{id}` — Response: `true`. Permanent.

### 5.7 `GET /api/parent/children/{id}/activities` — `ChildActivityDto[]`

```json
[{ "id": "guid", "childId": "guid", "activityType": 1, "payload": "{...}", "createdAt": "..." }]
```

### 5.8 `GET /api/parent/profile` — `ParentProfileDto`

```json
{ "id": "guid", "fullName": "John Doe", "email": "parent@example.com", "profilePictureUrl": "https://..." }
```

### 5.9 `PUT /api/parent/profile` — Update Profile

Request:
```json
{ "fullName": "John Doe", "email": "new@example.com", "profilePictureUrl": "...", "currentPassword": "old", "newPassword": "new" }
```
Response: `true` (boolean)

### 5.10 `POST /api/parent/media/upload` — Upload Profile Picture

*Note: This endpoint accepts standard multipart/form-data. It is an alias of the `api/admin/media` upload endpoint but accepts the `Parent` role.*
Request: `multipart/form-data` with a file field named `file` (PNG, JPG, GIF).
Response:
```json
{ "url": "https://.../uploads/xyz.png" }
```

---

## 6. Admin Endpoints — `[Authorize(Roles = "Admin")]`

### 6.1 Platform stats

`GET /api/admin/stats` — `AdminStatsDto`

```json
{
  "totalParents": 40,
  "totalChildren": 42,
  "totalStories": 15,
  "mostWatchedStories": [
    { "name": "Story 1", "category": "Adventure", "thumbnailUrl": "..." }
  ],
  "mostPlayedGames": [
    { "name": "Rhyme Time", "category": "RhymeTime", "thumbnailUrl": "..." }
  ],
  "totalEarnings": 1500
}
```

### 6.2 Users

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/admin/users?page=1&pageSize=10&searchTerm=test&sortBy=name&sortDescending=false` | — | `PaginatedUsersDto` |
| GET | `/api/admin/users/stats` | — | `UserStatsDto` |
| PATCH | `/api/admin/users/{id}/disable` | `{ "disabled": true }` | 204 No Content |
| DELETE | `/api/admin/users/{id}` | — | 204 No Content |

`PaginatedUsersDto`:
```json
{
  "users": [{ "id": "guid", "email": "...", "fullName": "...", "userType": 2, "createdAt": "...", "emailConfirmed": true, "disabledAt": null }],
  "totalCount": 42, "page": 1, "pageSize": 10, "totalPages": 5
}
```

`UserStatsDto`:
```json
{ "totalUsers": 42, "totalChildren": 30, "totalAdmins": 2, "totalParents": 40, "recentRegistrations": 5 }
```

### 6.2.1 Children — `/api/admin/children`

Children are **not** rows in the users table (`UserType` has no Child member) — they live in a separate Child table linked to a parent. This endpoint lists them platform-wide; `/api/admin/users` never contains children.

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/admin/children?page=1&pageSize=10&searchTerm=test&sortBy=name&sortDescending=false` | — | `PaginatedChildrenDto` |
| PATCH | `/api/admin/children/{id}/disable` | `{ "disabled": true }` | 204 No Content |
| DELETE | `/api/admin/children/{id}` | — | 204 No Content |

`PaginatedChildrenDto`:
```json
{
  "children": [{
    "id": "guid", "fullName": "Child Full Name", "username": "childuser", 
    "coins": 120, "loginStreak": 5,
    "lastActivityAt": "...", "disabledAt": null, "additionalData": "{\"level\":3}",
    "avatarState": "{\"BodyColor\":\"body-1\",\"Hair\":\"hair-2\",\"Dress\":\"dress-1\"}"
  }],
  "totalCount": 30, "page": 1, "pageSize": 10, "totalPages": 3
}
```

> **Note for Admin UI:** The admin dashboard UI will use the `id`, `fullName`, and `avatarState` directly from the `children` array to render the children list. No secondary API calls are required to fetch a child's avatar or full name.


### 6.3 Stories — `/api/admin/stories`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| POST | `/` | `{ "title", "coverImageUrl", "contentPayload", "status" }` | `"guid"` |
| GET | `/?status=2&titleSearch=adventure&sortBy=newest` | — | `StoryDto[]` |
| GET | `/recent` | — | `StoryDto[]` (top 10) |
| GET | `/{id}` | — | `StoryDto` |
| PUT | `/{id}` | all fields optional | `true` |
| DELETE | `/{id}` | — | `true` |

Pricing contract: `priceInCoins` is `0 = Free, >0 = Paid`. Negative values are rejected with `400 Bad Request`, and the database enforces `PriceInCoins >= 0` on `Stories` and `StoreItems`. Admin story endpoints always return `isUnlocked: true` (admins bypass pricing); free/paid rendering must use `priceInCoins`, not `isUnlocked`.

### 6.4 Quizzes — `/api/admin/quizzes`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| POST | `/` | `{ "title", "storyId"?, "questionsPayload", "status" }` | `"guid"` |
| GET | `/?status=2&storyId={guid}` | — | `QuizDto[]` |
| GET | `/{id}` | — | `QuizDto` |
| PUT | `/{id}` | all fields optional | `true` |
| DELETE | `/{id}` | — | `true` |

`questionsPayload` is a JSON **string** (there is no `questions` object array in the API contract).

### 6.5 Store items — `/api/admin/store-items`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| POST | `/` | `{ "name", "priceInCoins", "assetUrl", "metadata"? }` | `"guid"` |
| POST | `/story/{storyId}?priceInCoins=100` | — | `"guid"` |

`priceInCoins` must be `>= 0` (0 = free item); negative values are rejected with `400 Bad Request`.
| GET | `/?minPrice=50&maxPrice=500` | — | `StoreItemDto[]` |
| GET | `/{id}` | — | `StoreItemDto` |
| PUT | `/{id}` | all fields optional | `true` |
| DELETE | `/{id}` | — | `true` |

### 6.6 Mini-games — `/api/admin/minigames`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/?gameType=RhymeTime&status=2` | — | `MiniGameContentDto[]` |
| GET | `/{id}` | — | `MiniGameContentDetailDto` |
| POST | `/` | `{ "gameType", "title", "contentPayload" }` | `"guid"` (201) |
| PUT | `/{id}` | `{ "title"?, "status"?, "contentPayload"? }` | `{ "success": true }` |
| DELETE | `/{id}` | — | `{ "success": true }` |

🔧 Planned additive fields (GAP-4): `description`, `thumbnailUrl`, `assets` on the entity and all mini-game DTOs.

### 6.7 Story audio — `/api/admin/storyaudio`

| Method | Route | Response |
|--------|-------|----------|
| GET | `/story/{storyId}` | `StoryAudio[]` |
| GET | `/story/{storyId}/page/{pageNumber}` | `StoryAudio[]` |
| GET | `/story/{storyId}/language/{language}` | `StoryAudio[]` |
| GET | `/{id}` | `StoryAudio` |
| POST | `/` | created `StoryAudio` (201) |
| PUT | `/{id}` | updated `StoryAudio` |
| DELETE | `/{id}` | 200 OK (soft delete: `isActive = false`) |

Create request:
```json
{
  "storyId": "guid", "audioUrl": "https://...", "mimeType": "audio/mpeg",
  "type": 1, "startTime": 0, "endTime": 120, "language": "en", "durationSeconds": 120
}
```
Update request: same fields, all optional, plus `"isActive"`.

`StoryAudio` response shape:
```json
{
  "id": "guid", "storyId": "guid", "pageNumber": 1, "audioUrl": "https://...",
  "mimeType": "audio/mpeg", "type": 1, "startTime": 0, "endTime": 120,
  "language": "en", "durationSeconds": 120, "isActive": true,
  "createdAt": "...", "updatedAt": null
}
```

🔧 Planned (GAP-8, optional): child read access `GET /api/child/storyaudio/story/{storyId}` for narration playback.

### 6.8 Admin Profile — `/api/admin/profile`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/` | — | `AdminProfileDto` |
| PUT | `/` | `{ "email"?, "fullName"?, "currentPassword"?, "newPassword"? }` | `true` |

`AdminProfileDto`:
```json
{
  "id": "guid",
  "email": "admin@example.com",
  "fullName": "Admin Name"
}
```

---

## Appendix A — Mini-game `contentPayload` shapes (Unity contract)

The backend stores and returns these payloads as **opaque strings — byte-for-byte what the Unity repositories parse with `JsonUtility`**. Field casing is exact; StorySequencing is PascalCase.

| gameType | Payload root shape |
|----------|--------------------|
| `StoryQuest`, `ReadingDetective` | `{ "storyId", "title", "storyType", "content", "questions": [{ "questionText", "options": [], "correctOptionIndex" }] }` |
| `StorySequencing` | `{ "StoryId", "Title", "StoryText", "Events": [{ "Id", "Text", "CorrectPosition" }] }` ← PascalCase |
| `RhymeTime` | array of `{ "pairId", "wordA", "wordB" }` (wrapped per Unity `JsonHelper`: `{ "items": [...] }`) |
| `PrefixSuffix` | array of `{ "id", "rootWord", "mode", "options": [], "correctOptionIndex" }` |
| `WordWizard` | array of `{ "id", "targetWord", "decoyLetters" }` |

Rule for admins/agents authoring content: never reformat or re-serialize these payloads server-side.

## Appendix B — Consumer notes

- **Web frontends (finished):** contract above is live. Any backend change must be additive (new fields/endpoints only) — see guardrails in `BACKEND-GAPS.md`.
- **Unity (planned):** SDK will be generated against this document. See `UNITY-INTEGRATION-PLAN.md`. Items marked 🔧 require the corresponding `BACKEND-GAPS.md` task to be completed first.
- Rate limits (nginx): `/api/` 10 r/s, `/api/auth/` 5 r/s per IP.

### 6.9 IAP Tiers — `/api/admin/iap-tiers/{id}`
`PUT /api/admin/iap-tiers/{id}`
Request: `{ "id": "guid", "name": "Tier 1", "storeProductId": "string", "coinWeight": 100 }`
Response: `bool`
