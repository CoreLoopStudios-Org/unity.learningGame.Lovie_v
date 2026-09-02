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
  "childId": "guid", "username": "childuser", "coins": 120, "loginStreak": 5
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

---

## 4. Child Endpoints — `[Authorize(Roles = "Child")]`

The child id is always derived from the JWT. Never send it from the client.

### 4.1 `GET /api/child/profile` — `ChildProfileDto`

```json
{
  "id": "guid", "username": "childuser", "coins": 120, "loginStreak": 5,
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

### 4.5 `GET /api/child/stories` — `StoryDto[]` (Published only)

`StoryDto`:
```json
{
  "id": "guid", "title": "The Adventure", "coverImageUrl": "https://...",
  "contentPayload": "[{\"page\":1,\"text\":\"...\"}]", "status": 2,
  "createdAt": "...", "updatedAt": null
}
```

### 4.6 `GET /api/child/stories/{id}` — `StoryDto`

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
    { "childId": "guid", "username": "kid1", "totalCoins": 120, "loginStreak": 5, "lastActivityAt": "..." }
  ]
}
```
Note: the array property is `childSummaries` (not `children`); coins field is `totalCoins`.

### 5.2 `POST /api/parent/children` — create child

Request: `{ "username": "newchild", "password": "SecurePass123!" }` (username 3–50 chars, password 6–100)
Response: `"guid"` (child id)

### 5.3 `GET /api/parent/children` — `ChildSummaryDto[]`

```json
[{ "id": "guid", "username": "kid1", "coins": 120, "loginStreak": 5, "lastActivityAt": "..." }]
```

### 5.4 `GET /api/parent/children/{id}` — `ChildDetailDto`

`ChildSummaryDto` fields plus `"avatarState"`, `"additionalData"`, `"lastLoginDate"`.

### 5.5 `PUT /api/parent/children/{id}` — update child

Request (all optional): `{ "username": "...", "password": "...", "avatarState": "{...}", "additionalData": "{...}" }`
Response: `true`

### 5.6 `DELETE /api/parent/children/{id}` — Response: `true`. Permanent.

### 5.7 `GET /api/parent/children/{id}/activities` — `ChildActivityDto[]`

```json
[{ "id": "guid", "childId": "guid", "activityType": 1, "payload": "{...}", "createdAt": "..." }]
```

---

## 6. Admin Endpoints — `[Authorize(Roles = "Admin")]`

### 6.1 Platform stats

`GET /api/admin/stats` — `AdminStatsDto`

```json
{
  "totalUsers": 42,
  "activeChildren": 30,
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
| GET | `/api/admin/users?page=1&pageSize=10` | — | `PaginatedUsersDto` |
| GET | `/api/admin/users/stats` | — | `UserStatsDto` |
| PATCH | `/api/admin/users/{id}/disable` | `{ "disabled": true }` | 204 No Content |

`PaginatedUsersDto`:
```json
{
  "users": [{ "id": "guid", "email": "...", "fullName": "...", "userType": 2, "createdAt": "...", "emailConfirmed": true }],
  "totalCount": 42, "page": 1, "pageSize": 10, "totalPages": 5
}
```

`UserStatsDto`:
```json
{ "totalUsers": 42, "totalChildren": 30, "adminCount": 2, "parentCount": 40, "recentRegistrations": 5 }
```

### 6.2.1 Children — `/api/admin/children`

Children are **not** rows in the users table (`UserType` has no Child member) — they live in a separate Child table linked to a parent. This endpoint lists them platform-wide; `/api/admin/users` never contains children.

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/admin/children?page=1&pageSize=10` | — | `PaginatedChildrenDto` |

`PaginatedChildrenDto`:
```json
{
  "children": [{
    "id": "guid", "username": "childuser", "parentId": "guid",
    "parentName": "John Doe", "parentEmail": "parent@example.com",
    "coins": 120, "loginStreak": 5,
    "lastActivityAt": "...", "additionalData": "{\"level\":3}"
  }],
  "totalCount": 30, "page": 1, "pageSize": 10, "totalPages": 3
}
```

### 6.3 Stories — `/api/admin/stories`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| POST | `/` | `{ "title", "coverImageUrl", "contentPayload", "status" }` | `"guid"` |
| GET | `/?status=2&titleSearch=adventure&sortBy=newest` | — | `StoryDto[]` |
| GET | `/recent` | — | `StoryDto[]` (top 10) |
| GET | `/{id}` | — | `StoryDto` |
| PUT | `/{id}` | all fields optional | `true` |
| DELETE | `/{id}` | — | `true` |

> ⚠️ As of 2026-09-02 the backend source implements only `GET /` (params: `status`, `titleSearch` — **no `sortBy`**), `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`. `GET /recent` is **not implemented**; requests return 404.

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
| GET | `/?minPrice=50&maxPrice=500` | — | `StoreItemDto[]` |
| GET | `/{id}` | — | `StoreItemDto` |
| PUT | `/{id}` | all fields optional | `true` |
| DELETE | `/{id}` | — | `true` |

> ⚠️ As of 2026-09-02 `POST /story/{storyId}` is **not implemented** in the backend source; requests return 404.

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

> ⚠️ As of 2026-09-02 **no admin profile endpoints exist in the backend source** — requests return 404. Also missing from the backend: `POST /api/admin/media/upload`. Confirm with the backend team before wiring UI to these.

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
