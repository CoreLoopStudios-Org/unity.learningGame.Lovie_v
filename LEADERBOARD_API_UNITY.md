# Leaderboard & Scoring API — Unity Client Guide

> Verified against the backend on **2026-11-05** (feature: score + leaderboard system).
> Companion to `API-DOCUMENTATION.md` (master reference). Endpoint contracts below come
> straight from live responses.
>
> **Base URL:** `https://imaginemebylovie.com` (development: `http://localhost:5200`)
> **Content type:** `application/json` · all properties camelCase · enums as numbers ·
> GUIDs as strings · timestamps ISO 8601 UTC.

---

## 1. What was added

| # | Endpoint | Auth | Purpose |
|---|----------|------|---------|
| 1 | `GET /api/leaderboard/weekly?page=&pageSize=` | **Anonymous** | Current week's top children |
| 2 | `GET /api/leaderboard/alltime?page=&pageSize=` | **Anonymous** | All-time top children |
| 3 | `GET /api/leaderboard/rank/{childId}` | **Anonymous** | One child's score + rank on both boards + streak |
| 4 | `POST /api/child/activities/vocabulary` | **Child JWT** | Report mastered words (earns points) |
| 5 | `GET /api/admin/leaderboard/settings` | **Admin JWT** | Fetch current scoring rules & streak milestones |
| 6 | `PUT /api/admin/leaderboard/settings` | **Admin JWT** | Update scoring rules & streak milestones (full replace) |

**Changed:** every activity-logging response (`/activities/story`, `/activities/quiz`,
`/activities/game`, `/activities/vocabulary`) now also returns `pointsEarned` and
`totalPoints` (additive fields — old builds keep working).

The leaderboard endpoints are reachable **without any token**, from any network — safe to
call before login (e.g. on a public leaderboard screen). All other rules from
`API-DOCUMENTATION.md` (24 h tokens, no refresh, re-login on 401) still apply.

---

## 2. How points are earned (display rules)

Points are computed **server-side only** — the client never sends point amounts.

| Source | Default points | Rule (server-side) |
|--------|---------------|--------------------|
| Story completed | **200** | First read of a given story per UTC day. Re-reading the same story the same day: 0 points (coins behave the same way). |
| Quiz accuracy | **max 100 × accuracy%** | First attempt of a given quiz per UTC day. `points = round(100 × score / 100)` where `score` is the 0–100 value you already send. No negative points. |
| Vocabulary | **10 per word** | Each call reports `wordsMastered` (1–100). |
| Reading streak | **3 days → 10, 7 → 100, 30 → 1000** | Awarded automatically when the streak hits a milestone day exactly, once per streak run. After a break the streak resets to 0 and milestones can be earned again. |

> ⚠️ **All values above are defaults and can be changed by the admin at runtime.** Do NOT
> hardcode them into UI copy ("Read a story, earn 200 points!"). Show the real numbers from
> API responses instead: `+{pointsEarned} points` from the activity response, and the
> `score` fields from the leaderboard. Streak milestone badges (7/14/30 days) can still be
> displayed as day thresholds, but the associated point values should stay generic.

**Weekly reset:** the weekly board resets every **Monday 00:00 UTC**. The response tells you
the exact boundary via `weekStart`.

**Two different streaks — don't mix them up:**
- `loginStreak` (existing, in the child-login response) — daily-login streak, drives the
  weekly coin-reward table. Unchanged.
- `currentStreakDays` (new, in the rank response) — **reading streak**, drives leaderboard
  points. A day counts when the child reads at least one story that day. Managed entirely
  by the server.

---

## 3. Endpoints

### 3.1 `GET /api/leaderboard/weekly?page=1&pageSize=10` — anonymous

Pagination: `page` ≥ 1 (default 1), `pageSize` 1–100 (default 10; values outside are
clamped server-side). Returns children ranked by points earned **in the current week**
(Monday 00:00 UTC → now). Deterministic tie-break: equal scores are ordered by childId —
ranks are stable across pages.

Response — `LeaderboardPageDto`:
```json
{
  "entries": [
    { "rank": 1, "childId": "33333333-3333-3333-3333-333333333333", "username": "dev_child", "fullName": "Dev Child", "avatarState": "{\"head\":\"cat\",\"color\":\"blue\"}", "score": 250 }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "weekStart": "2026-11-02T00:00:00Z"
}
```

- `entries` — ordered best-first; may be **empty** (early in a week / no scores yet).
- `avatarState` — the child's avatar config as a raw JSON string (same shape
  `GET /api/child/profile` returns). `"{}"` when the child has no customization. Parse it
  and render the avatar exactly like you do on the profile screen. Board pages are cached
  up to 60s, so a just-changed avatar can appear stale for at most a minute.
- `weekStart` — the Monday this board covers. Use it for a "resets in…" countdown.
- An empty page beyond the end returns `entries: []` — treat as "no more pages".

### 3.2 `GET /api/leaderboard/alltime?page=1&pageSize=10` — anonymous

Identical shape, but `score` is the child's **all-time total** and `weekStart` is `null`.

### 3.3 `GET /api/leaderboard/rank/{childId}` — anonymous

`{childId}` = the `childId` you get from `POST /api/auth/child/login` (store it at login).

Response — `ChildRankDto`:
```json
{
  "childId": "33333333-3333-3333-3333-333333333333",
  "username": "dev_child",
  "weeklyScore": 250,
  "weeklyRank": 2,
  "weeklyNextRank": 1,
  "weeklyPointsToNextRank": 50,
  "totalScore": 250,
  "totalRank": 2,
  "totalNextRank": 1,
  "totalPointsToNextRank": 50,
  "currentStreakDays": 1,
  "longestStreakDays": 1
}
```

- `weeklyRank` / `totalRank` are **`null` while the child has 0 points** on that board —
  handle nulls (render "Unranked", not rank 0).
- `weeklyNextRank` / `weeklyPointsToNextRank` (and the `total*` pair) power the
  "150 points to #36" chase line: they are `null` while unranked or already **#1**
  (nothing to chase). Otherwise `pointsToNextRank` is what the child needs to tie the
  lowest score above them, and `nextRank` is the rank they'd hold there (tied children
  share it).
- `404` when the childId doesn't exist.
- Use `currentStreakDays` for the streak flame/counter UI and `longestStreakDays` for the
  profile/best-record UI.

### 3.4 `POST /api/child/activities/vocabulary` — Child JWT

Call this when the child masters word(s) — typically from the WordWizard mini-game or any
vocabulary exercise. You can report one word or a batch:

Request:
```json
{ "wordsMastered": 5 }
```

- `wordsMastered`: 1–100 (required). `0` or `>100` → `400`.
- Fire per mastery event (`1`) or batch per session (`5`) — either is fine. Batching saves
  requests.

Response — `ActivityLoggedDto` (same envelope as story/quiz/game):
```json
{
  "id": "4c86b9bd-8eb0-4a4d-9299-960da8e81c58",
  "activityType": 7,
  "createdAt": "2026-11-05T06:39:28.4219786Z",
  "coinsEarned": 0,
  "totalCoins": 510,
  "message": "+50 points",
  "pointsEarned": 50,
  "totalPoints": 250
}
```

- Vocabulary earns **points only, no coins**.
- `activityType` 7 = `VocabularyMastered` (new enum member; full list in
  `API-DOCUMENTATION.md` §2).
- Show `message` as-is if you already display activity toasts — the server composes it
  (e.g. `"First read of story today: +10 coins | +200 points"`).

### 3.5 `GET /api/admin/leaderboard/settings` — Admin JWT

Fetch current runtime scoring configuration (points per task, streak milestones). Used by the admin dashboard or developer/QA diagnostic tools.

Headers:
- `Authorization: Bearer {adminToken}`

Response — `ScoreSettingsDto`:
```json
{
  "storyCompletionPoints": 200,
  "quizAccuracyMaxPoints": 100,
  "vocabularyPointsPerWord": 10,
  "streakMilestones": [
    { "days": 3, "points": 10 },
    { "days": 7, "points": 100 },
    { "days": 30, "points": 1000 }
  ]
}
```

- Status codes: `200 OK`, `401 Unauthorized`, `403 Forbidden` (non-Admin role).

### 3.6 `PUT /api/admin/leaderboard/settings` — Admin JWT

Full replace of active scoring settings. Updates apply to future score awards only (the ledger is immutable). Automatically evicts the settings cache on write.

Headers:
- `Authorization: Bearer {adminToken}`
- `Content-Type: application/json`

Request Body — `ScoreSettingsDto`:
```json
{
  "storyCompletionPoints": 200,
  "quizAccuracyMaxPoints": 100,
  "vocabularyPointsPerWord": 10,
  "streakMilestones": [
    { "days": 3, "points": 10 },
    { "days": 7, "points": 100 },
    { "days": 30, "points": 1000 }
  ]
}
```

Validation constraints:
- `storyCompletionPoints`: 0–100,000
- `quizAccuracyMaxPoints`: 0–1,000
- `vocabularyPointsPerWord`: 0–1,000
- `streakMilestones`: max 20 milestones, `days` unique between 1 and 3,650, `points` between 0 and 100,000

Response:
- `200 OK` with the updated `ScoreSettingsDto`
- `400 Bad Request` on validation failure
- `401 Unauthorized` / `403 Forbidden` if not an authenticated Admin

---

## 4. Activity responses gained point fields (all activity endpoints)

`ActivityLoggedDto` now ends with:

| Field | Meaning |
|-------|---------|
| `pointsEarned` | Leaderboard points earned **by this activity** (0 when the daily dedup suppresses the award, or a non-scoring activity like games) |
| `totalPoints` | The child's **all-time** point total after this activity |

Use them for the post-activity "+N points" popup and the profile point counter — no need to
refetch the leaderboard immediately (see §5, caching).

Example — story read (first today):
```json
{ "id": "cd37d6a1-...", "activityType": 1, "createdAt": "2026-11-05T06:39:27.0050202Z",
  "coinsEarned": 10, "totalCoins": 510,
  "message": "First read of story today: +10 coins | +200 points",
  "pointsEarned": 200, "totalPoints": 200 }
```

Example — story re-read the same day (dedup):
```json
{ "id": "...", "activityType": 1, "...": "...",
  "coinsEarned": 0, "totalCoins": 510,
  "message": "Story already read today - no coins awarded",
  "pointsEarned": 0, "totalPoints": 250 }
```

**Quiz reminder:** points (like coins) only compute when the quiz payload contains
`quizId` and `score` — keep sending them as you do today:
`{ "quizId": "guid", "payload": "{\"quizId\":\"guid\",\"score\":85}" }`
(`score` = 0–100 accuracy; fractional values are rounded and clamped server-side).

---

## 5. Freshness & caching (important UX rules)

- Leaderboard pages and ranks are **cached server-side for 30–60 seconds**. After a child
  earns points, the board may not include them for up to a minute. **Do not** poll in a
  retry loop after posting an activity — the activity response already carries the exact
  `pointsEarned`/`totalPoints`; refresh the leaderboard on the next natural screen entry.
- Recommended client-side refresh: on screen enter, plus a manual pull-to-refresh. No
  auto-refresh timer needed.

---

## 6. Unity implementation

Unity's built-in `JsonUtility` does **not** handle camelCase, nullable ints, or nested
arrays well — use the Unity **Newtonsoft JSON** package
(`com.unity.nuget.newtonsoft-json`, standard in Unity 2021+).

### DTOs

```csharp
using Newtonsoft.Json;

namespace ImagineMe.Leaderboard
{
    [System.Serializable]
    public class LeaderboardEntryDto
    {
        [JsonProperty("rank")] public int Rank;
        [JsonProperty("childId")] public string ChildId;
        [JsonProperty("username")] public string Username;
        [JsonProperty("fullName")] public string FullName;
        [JsonProperty("avatarState")] public string AvatarState; // raw JSON config, parse to render
        [JsonProperty("score")] public int Score;
    }

    [System.Serializable]
    public class LeaderboardPageDto
    {
        [JsonProperty("entries")] public LeaderboardEntryDto[] Entries = System.Array.Empty<LeaderboardEntryDto>();
        [JsonProperty("totalCount")] public int TotalCount;
        [JsonProperty("page")] public int Page;
        [JsonProperty("pageSize")] public int PageSize;
        [JsonProperty("totalPages")] public int TotalPages;
        [JsonProperty("weekStart")] public System.DateTime? WeekStart; // null on all-time board
    }

    [System.Serializable]
    public class ChildRankDto
    {
        [JsonProperty("childId")] public string ChildId;
        [JsonProperty("username")] public string Username;
        [JsonProperty("weeklyScore")] public int WeeklyScore;
        [JsonProperty("weeklyRank")] public int? WeeklyRank;   // null = unranked (0 points)
        [JsonProperty("weeklyNextRank")] public int? WeeklyNextRank;             // null = unranked or #1
        [JsonProperty("weeklyPointsToNextRank")] public int? WeeklyPointsToNextRank; // null = unranked or #1
        [JsonProperty("totalScore")] public int TotalScore;
        [JsonProperty("totalRank")] public int? TotalRank;     // null = unranked
        [JsonProperty("totalNextRank")] public int? TotalNextRank;               // null = unranked or #1
        [JsonProperty("totalPointsToNextRank")] public int? TotalPointsToNextRank;   // null = unranked or #1
        [JsonProperty("currentStreakDays")] public int CurrentStreakDays;
        [JsonProperty("longestStreakDays")] public int LongestStreakDays;
    }

    [System.Serializable]
    public class VocabularyRequest
    {
        [JsonProperty("wordsMastered")] public int WordsMastered;
    }

    [System.Serializable]
    public class StreakMilestoneDto
    {
        [JsonProperty("days")] public int Days;
        [JsonProperty("points")] public int Points;
    }

    [System.Serializable]
    public class ScoreSettingsDto
    {
        [JsonProperty("storyCompletionPoints")] public int StoryCompletionPoints;
        [JsonProperty("quizAccuracyMaxPoints")] public int QuizAccuracyMaxPoints;
        [JsonProperty("vocabularyPointsPerWord")] public int VocabularyPointsPerWord;
        [JsonProperty("streakMilestones")] public System.Collections.Generic.List<StreakMilestoneDto> StreakMilestones = new();
    }

    // ActivityLoggedDto — extend your existing class with:
    //   [JsonProperty("pointsEarned")] public int PointsEarned;
    //   [JsonProperty("totalPoints")]  public int TotalPoints;
}
```

### Service (UnityWebRequest)

```csharp
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ImagineMe.Leaderboard
{
    public static class LeaderboardApi
    {
        // Point this at the environment: dev http://localhost:5200, prod https://imaginemebylovie.com
        public const string BaseUrl = "http://localhost:5200";

        // Leaderboard reads are anonymous - no Authorization header needed.
        public static IEnumerator GetWeekly(Action<LeaderboardPageDto> onOk, Action<string> onError, int page = 1, int pageSize = 10)
            => Get($"{BaseUrl}/api/leaderboard/weekly?page={page}&pageSize={pageSize}", onOk, onError);

        public static IEnumerator GetAllTime(Action<LeaderboardPageDto> onOk, Action<string> onError, int page = 1, int pageSize = 10)
            => Get($"{BaseUrl}/api/leaderboard/alltime?page={page}&pageSize={pageSize}", onOk, onError);

        public static IEnumerator GetRank(string childId, Action<ChildRankDto> onOk, Action<string> onError)
            => Get($"{BaseUrl}/api/leaderboard/rank/{childId}", onOk, onError);

        // Needs the child's JWT (from POST /api/auth/child/login).
        public static IEnumerator LogVocabulary(string childToken, int wordsMastered, Action<ActivityLoggedDto> onOk, Action<string> onError)
        {
            var body = JsonConvert.SerializeObject(new VocabularyRequest { WordsMastered = wordsMastered });
            using var req = new UnityWebRequest($"{BaseUrl}/api/child/activities/vocabulary", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {childToken}");
            yield return Send(req, body => JsonConvert.DeserializeObject<ActivityLoggedDto>(body), onOk, onError);
        }

        // Admin endpoints (require Admin JWT)
        public static IEnumerator GetSettings(string adminToken, Action<ScoreSettingsDto> onOk, Action<string> onError)
        {
            using var req = UnityWebRequest.Get($"{BaseUrl}/api/admin/leaderboard/settings");
            req.timeout = 15;
            req.SetRequestHeader("Authorization", $"Bearer {adminToken}");
            yield return Send(req, JsonConvert.DeserializeObject<ScoreSettingsDto>, onOk, onError);
        }

        public static IEnumerator UpdateSettings(string adminToken, ScoreSettingsDto settings, Action<ScoreSettingsDto> onOk, Action<string> onError)
        {
            var body = JsonConvert.SerializeObject(settings);
            using var req = new UnityWebRequest($"{BaseUrl}/api/admin/leaderboard/settings", "PUT")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {adminToken}");
            yield return Send(req, JsonConvert.DeserializeObject<ScoreSettingsDto>, onOk, onError);
        }

        private static IEnumerator Get<T>(string url, Action<T> onOk, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 15;
            yield return Send(req, JsonConvert.DeserializeObject<T>, onOk, onError);
        }

        private static IEnumerator Send<T>(UnityWebRequest req, Func<string, T> parse, Action<T> onOk, Action<string> onError)
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                onOk?.Invoke(parse(req.downloadHandler.text));
            else
                onError?.Invoke($"{(int)req.responseCode} {req.error}: {req.downloadHandler?.text}");
        }
    }
}
```

### Screen wiring (typical leaderboard screen)

```csharp
// On screen enter: board + the child's own banner in parallel
StartCoroutine(LeaderboardApi.GetWeekly(page => RenderRows(page.Entries), err => Debug.LogError(err)));
StartCoroutine(LeaderboardApi.GetRank(ChildSession.ChildId, rank =>
{
    streakText.text = $"{rank.CurrentStreakDays} day streak";
    myRankText.text = rank.WeeklyRank.HasValue ? $"#{rank.WeeklyRank.Value}" : "Unranked";
}, err => { /* 404 => child deleted; fall back to board-only UI */ }));
```

Pagination: show `Next` while `page < totalPages`; request the next page on demand.

---

## 7. Errors

Standard error envelope (see `API-DOCUMENTATION.md` §1):

```json
{ "status": 400, "message": "..." }
```

| Case | Code |
|------|------|
| Vocabulary `wordsMastered` outside 1–100 | 400 |
| Vocabulary without a child token / expired token | 401 (re-login, then retry once) |
| `rank/{childId}` for a deleted/unknown child | 404 |
| Admin settings PUT validation failure (negative points, duplicate milestone days, >20 milestones) | 400 |
| Admin settings GET/PUT without a valid admin token | 401 (unauthorized) / 403 (forbidden for non-Admin) |
| Public leaderboard endpoints (`weekly`, `alltime`, `rank`) | never 401/403 — fully public |

If a leaderboard request fails entirely (network down), keep the last rendered board and
show a retry affordance — the endpoints are cache-backed and cheap; retrying is fine.

---

## 8. Checklist for the Unity build

- [ ] Update `ActivityLoggedDto` with `pointsEarned` / `totalPoints`; surface `+N points` after story/quiz/vocabulary.
- [ ] Call `POST /api/child/activities/vocabulary` on word mastery (WordWizard etc.).
- [ ] Leaderboard screen: weekly + all-time tabs, paginated, anonymous access.
- [ ] "Your rank" banner from `rank/{childId}`; handle `null` ranks ("Unranked").
- [ ] Reading-streak UI from `currentStreakDays` (do **not** reuse `loginStreak`).
- [ ] No hardcoded point values in UI text.
- [ ] No polling faster than once per screen entry (server cache is 30–60 s).
