# Mini Games — Full Project Audit (Backend Readiness & Completeness)

**Scope note — read this first:** Two things in the session brief don't match the actual repository, checked across every local and remote branch (`main`, `Towhid`, `Devlopment_DevT`, `origin/Development_miniGames`, etc.):

1. **`CLAUDE_CONTEXT_STUDYAPP.md` and `CLAUDE_CONTEXT_SESSION2.md` do not exist anywhere in this repository.** No architecture-rules doc was found to cross-reference, so every rule cited below (repository-pattern requirement, ScriptableObject flag, `IStudyGame` contract, `CoinWallet`) is stated purely from the brief's own description, not verified against a written spec.
2. **Only 7 mini-games exist, not 10.** Confirmed by folder listing, scene listing, and project-wide text search for "prefix", "suffix", "sequenc", "wizard" across `.cs`/`.unity`/`.json` in every branch — zero hits. **Prefix & Suffix, Story Sequencing, and Word Wizard do not exist in any form** (no folder, script, scene, or content file). The 7 that exist: Word Match, Sentence Builder, Word Listen (likely "Sound & Spell"), Story Quest, Rhyme Time, Sight Word Pop, and Reading Detective.

Also confirmed project-wide (via full-text search of all `.cs` files): **no `CoinWallet`, `IStudyGame`, or `GameResult` type exists anywhere in the codebase.** These are treated below as "does not exist yet," not as partially-built.

Everything else in this report is drawn directly from reading the actual `.cs`, `.unity`, `.prefab`, and `.json` files — file paths and line numbers are cited throughout. Where a scene or script could not be found, that is stated explicitly rather than assumed.

---

## 1. Word Match

**Folder:** `Assets/MIni Games/WordMatch/` · **Scene:** `Word Match Mini Game.unity` · **Class:** `CoreLoop.WordMatch.WordMatchManager`

### Completion status
Playable round-to-round: drag-to-connect matching, correct/incorrect line coloring, and round advancement all work. **Dead-ends after the last round** — [`WordMatchManager.cs:243-249`](WordMatch/Scripts/WordMatchManager.cs) `LevelCompleteRoutine()` only does `Debug.Log("Word Match Level Complete!")`. No completion event, no result data, no UI transition — the player is left on a faded-out screen with nothing else happening.

- **Unassigned `[SerializeField]` refs** (`Word Match.prefab`, component id `8564459335457630448`): `timeUpPopup`, `timeUpRestartButton`, `timeUpCountdownText`, `audioSource`, `successSound`, `errorSound` are all `{fileID: 0}` in the **prefab itself**. In the live scene, `timeUpPopup`/`timeUpRestartButton`/`timeUpCountdownText` are re-wired via a per-instance override (scene lines ~623–637) to a nested "Time Up panel" prefab, so the timeout UI works in-game. **`audioSource`, `successSound`, `errorSound` are never overridden — all SFX in this game are silently disabled** (calls are null-guarded, so no crash, just silence).
- No leftover debug comments or duplicate classes found in this game's own scripts.

### Content source
`WordMatchLevelSO` (ScriptableObject, `WordMatch/Scripts/WordMatchLevelSO.cs`), data asset `WordMatch/Level 1.asset`. Schema: `rounds: [{ entries: [{ id, word, image(Sprite), audioClip(AudioClip) }] }]`. **Every entry's `audioClip` is `{fileID: 0}` in `Level 1.asset`** — the per-card "play audio" button (`WordMatchItem.PlayAudio`) has nothing to play for any word in the current level.
**Flag: ScriptableObject content** — per the brief's stated rule, this needs a backend migration path since admin-uploaded content can't modify a baked build asset.
**No repository interface exists.** `WordMatchManager` holds `[SerializeField] private WordMatchLevelSO currentLevel` directly — nothing to plug a Firestore implementation into without adding an interface layer first.

### Completion / result data
No coins, no score, no time-played, no completion timestamp — none are tracked. `IStudyGame` is not implemented (the type doesn't exist project-wide).

---

## 2. Sentence Builder

**Folder:** `Assets/MIni Games/Sentence Builder/` · **Scene:** `Sentence Builder Mini Game.unity` · **Class:** `CoreLoop.SentenceBuilder.SentenceBuilderManager`

### Completion status
Playable through all sentences (word-pool → drag-into-slot → check). On the last sentence, [`SentenceBuilderManager.cs:508-511`](Sentence%20Builder/Scripts/SentenceBuilderManager.cs) does `_timerRunning = false; Debug.Log("Sentence Builder Mini-Game Complete!");` — same pattern as Word Match: **no completion event, no result, no UI transition.**

- **Unassigned refs** (scene): `audioSource`, `successSound`, `wordClickSound`, `errorSound` are all `{fileID: 0}` — every sound effect in this game is silent. `timeUpPopup`/`timeUpRestartButton`/`timeUpCountdownText` **are** properly wired (non-zero fileIDs), so the time-up flow itself works.
- No dead code or leftover debug comments found. `FlowLayoutGroup.cs` (custom wrapping layout) is self-contained and has no issues.

### Content source
`SentenceBuilderLevelSO` (ScriptableObject), data asset `Sentence Builder/Level1.asset`. Schema: `sentences: [{ image(Sprite), sentence(string, split on spaces at runtime), decoyWords[] }]`.
**Flag: ScriptableObject content** — same backend-migration flag as Word Match.
**No repository interface exists** — direct `[SerializeField] private SentenceBuilderLevelSO levelData` reference.

### Completion / result data
No coins, no score exposed, no timestamp. `IStudyGame` not implemented.

---

## 3. Word Listen ("Sound & Spell")

**Folder:** `Assets/MIni Games/Word Listen/` · **Scene:** `Listen Word Mini Game.unity` · **Class:** `CoreLoop.ListenWord.ListenWordManager`

*(Named "Word Listen" in the Assets folder and class namespace, "Listen Word" in the scene file name — presumed to be the "Sound & Spell" game from the brief; no other game matches that description.)*

### Completion status
Playable through all words (letter-pool → drag-into-slot → check, with audio playback of the target word). On the last word, [`ListenWordManager.cs:453-456`](Word%20Listen/Scripts/ListenWordManager.cs) does `_timerRunning = false; Debug.Log("Listen Word Mini-Game Complete!");` — **same dead-end pattern as the two games above.**

- **Unassigned refs** (scene): `audioSource`, `successSound`, `errorSound`, `letterClickSound` all `{fileID: 0}` — all SFX silent. `timeUpPopup`/`timeUpRestartButton`/`timeUpCountdownText` **are** wired correctly.
- No dead code found.

### Content source
`ListenWordLevelSO` (ScriptableObject), data asset `Word Listen/Level 1.asset`. Schema: `wordsToSpell: [{ image(Sprite), audioClip(AudioClip), targetWord(string), decoyLetters(string) }]`.
**Flag: ScriptableObject content** — same backend-migration flag.
**No repository interface exists.**

### Completion / result data
No coins, no score, no timestamp. `IStudyGame` not implemented.

### Duplicate-logic note (see also Section "Duplicate Games Found")
`ListenWordManager.cs` is structurally a near-line-for-line duplicate of `SentenceBuilderManager.cs` (identical timer/time-up block, identical hint-system logic, identical drag-to-slot animation coroutines) adapted from words to individual letters. Not a shared Unity prefab, but clearly the same hand-written pattern copied into two separately-maintained classes.

---

## 4. Story Quest

**Folder:** `Assets/MIni Games/Story Quest/` · **Scene:** `Story Quest Mini Game.unity` · **Class:** `Modules.Games.StoryQuest.StoryQuestManager`

### Completion status
**Not completable.** Reading panel → Quiz panel flow works; each question can be answered once via the shared `QuestionCardController`/`AnswerOptionView` components; `_answeredCount`/`_correctCount` are tracked correctly and the "CompleteBtn" is made visible once all questions are answered ([`StoryQuestManager.cs:160-173`](Shared/../Story%20Quest/StoryQuestManager.cs)). **But the Complete button's `OnClick` has zero persistent listeners wired in the scene** (`Story Quest Mini Game.unity`, component `195909159`: `m_OnClick: m_PersistentCalls: m_Calls: []`) — pressing it does nothing at all. There is also no method on `StoryQuestManager` a designer could even wire it to that would finish the game (no scene transition, no result event).

- **Content data bug:** `Shared/Resources/Stories/sq_story_001.json` question 1 asks *"What did Luna find in the cave?"* — the story text is about "Binu the Brave Bird" and never mentions Luna or a cave. Looks like a leftover/copy-pasted question from unrelated content.

### Content source
JSON via a genuine repository interface: [`IStoryQuestContentRepository` / `JsonStoryQuestContentRepository`](Shared/StoryQuestContentRepository.cs), loading `Shared/Resources/Stories/{storyId}.json`. Schema: `{ storyId, title, storyType, content, questions: [{ questionText, options: string[], correctOptionIndex }] }`.
**This is the one genuine backend-ready seam in the project** — a Firestore-backed class implementing the same interface would need zero changes to `StoryQuestManager`.

### Shared UI
Uses `Shared/AnswerFeedback.cs`, `Shared/AnswerOptionView.cs`, `Shared/QuestionCardController.cs` — genuinely shared, reusable, well-documented components (currently consumed by Story Quest and Reading Detective only, as expected since they're the only two quiz-format games).

### Completion / result data
`CorrectAnswerCount` / `TotalQuestionCount` are public properties on `StoryQuestManager`, with a doc-comment stating they're "used by the completion flow to calculate score and coins earned" — **but nothing anywhere in the codebase reads them.** This is a documented-but-unbuilt hook, not a working feature. No coins, no timestamp tracking. `IStudyGame` not implemented.

---

## 5. Reading Detective

**Scene:** `Reading Detective Mini Game.unity` · **Class:** same `Modules.Games.StoryQuest.StoryQuestManager` (no dedicated script folder exists for this game)

### Completion status
Identical situation to Story Quest — **not completable**, same empty `OnClick` on the Complete button (verified directly in `Reading Detective Mini Game.unity`, component `195909159`, same `m_Calls: []`).

- The scene's `_storyId` is set to `rd_story_001` (vs. Story Quest's `sq_story_001`) — confirmed this is the exact "shares StoryQuestManager" duplication the brief anticipated.
- **Content data bug:** `Shared/Resources/Stories/rd_story_001.json`'s internal `"storyId"` field is `"rd_story_002"` — mismatched against its own filename. Harmless functionally (the repository loads by filename, not by the internal field) but stale/incorrect data that could collide with a real `rd_story_002.json` later.

### Content source
Same `IStoryQuestContentRepository` seam as Story Quest.

### Completion / result data
Same as Story Quest: no coins, no score persisted, no timestamp, not completable, `IStudyGame` not implemented.

---

## 6. Rhyme Time

**Folder:** `Assets/MIni Games/RhymeTime/` · **Scene:** `Rhyme Time Mini Game.unity` · **Class:** `CoreLoop.WordMatch.RhymeTimeManager`

### Completion status
**Not completable, and the time-up flow is broken as currently wired in the shipped scene.**
- Round-to-round matching works (`Submit()`/`AllMatchedCorrectly()`). When the pair pool runs out, [`RhymeTimeManager.cs:194-199`](RhymeTime/Scripts/RhymeTimeManager.cs) just logs `"[RhymeTimeManager] Pair pool exhausted for this session."` and returns — no completion event, no end screen; the board simply stops responding.
- **Confirmed via scene YAML:** `Rhyme Time Mini Game.unity` is built by taking the **Word Match prefab** (`Word Match.prefab`, guid `e523e4cf228c8034e82fde27f112cde5`), **removing** its `WordMatchManager` component and **adding** a new `RhymeTimeManager` component in its place, wired to the same `leftColumn`/`rightColumn`/`lineContainer`/`columnsCanvasGroup`/`roundText`/`timerText` hierarchy (scene lines ~660–770). The new `RhymeTimeManager` component's `timeUpPopup`, `timeUpRestartButton`, `timeUpCountdownText`, `audioSource`, `successSound`, and `errorSound` are **all unassigned (`{fileID: 0}`)** — the scene's leftover per-instance overrides for those same property names still target the *removed* `WordMatchManager` component, so they have no effect on the new one. **Net effect: when the round timer hits 0, no popup shows, no countdown text updates, no sound plays, and after `timeUpDisplayDuration` (2s) the session silently auto-restarts** — a real, currently-shipping gameplay bug, not a hypothetical one.

### Content source
Genuine repository pattern: [`IRhymeTimePairRepository` / `JsonRhymeTimePairRepository`](RhymeTime/Scripts/JsonRhymeTimePairRepository.cs), loading `Shared/Resources/RhymeTime/Pairs.json`. Schema: `[{ pairId, wordA, wordB }, ...]` — 24 pairs currently authored.
**This is the second genuine backend-ready seam in the project**, parallel to Story Quest's.

### Completion / result data
No coins, no score, no timestamp, `IStudyGame` not implemented.

---

## 7. Sight Word Pop

**Folder:** `Assets/MIni Games/Sight Word Pop Mini Game/` · **Scene:** `Sight Word Pop Mini Game.unity` · **Class:** `GameManager` (+ `SpawnManager`, `InputHandler`, `AudioManager`, `UIManager`)

### Completion status
The most mechanically complete gameplay loop in the project (object-pooled spawn → tap detection → correct/miss scoring → round-end state machine) — **but the scene itself has no HUD wired at all.** `UIManager.cs`'s script GUID (`7577c446f5089d744be031a032843e31`) appears **nowhere** in `Sight Word Pop Mini Game.unity` — confirmed by a full-file search, not a partial one. The scene's only GameObjects are `ElementSpawnZone`, `Managers` (holding `GameManager`/`SpawnManager`/`InputHandler`/`AudioManager`), `PoolRoot`, `EventSystem`, `Main Camera`, `Canvas`. **No coin label, no start/pause/round-complete panels, no tap-feedback text exist in the built scene** — every one of `GameManager`'s events (`OnCoinsChanged`, `OnStateChanged`, `OnTapResult`) currently has zero subscribers at runtime.

- **Leftover debug artifacts** (exactly the pattern the brief asked to flag):
  - [`GameManager.cs:73`](Sight%20Word%20Pop%20Mini%20Game/Scripts/GameManager.cs) — `StartRound(); // ← ADD THIS LINE TEMPORARILY` — forces the round to start immediately on scene load, bypassing any intended Idle/start-button flow (which can't be reached anyway since no start panel is wired).
  - [`FloatingObject.cs:12`](Sight%20Word%20Pop%20Mini%20Game/Scripts/FloatingObject.cs) — `[SerializeField] private Button _button; // ← ADD THIS` — leftover comment marking a recently bolted-on field. The field itself **is** correctly wired in all three prefabs (`StarPrefab`, `CloudPrefab`, `BubblePrefab` all reference a valid Button), so this is cosmetic, not a functional gap.
  - `Debug.Log` calls left in on every init/click/tap: `FloatingObject.cs:47, 52, 76`.

### Content source
`LevelDataSO` (ScriptableObject, word list + baked `AudioClip` refs, `ScriptableObjects/Level_01.asset`) plus three `FloatingObjectConfigSO` assets (`StarConfig`/`CloudConfig`/`BubbleConfig` — per-type visuals, shake style, pop SFX, spawn weight).
**Flag: ScriptableObject content** — the class's own doc-comment already anticipates this ("FUTURE BACKEND SWAP: Replace this with a `LevelDataProvider : IWordProvider`...") but **no such interface exists yet** — it's a documented intention, not a built seam.

### Completion / result data
This is the **only one of the 7 games with any coin logic at all**, and it's entirely local:
- `GameManager.cs:36-37`: `_coinsPerCorrectTap = 10`, `_coinPenaltyPerMiss = 5`.
- Formula ([`GameManager.cs:187-207`](Sight%20Word%20Pop%20Mini%20Game/Scripts/GameManager.cs)): **+10 coins** per correct tap; **-5 coins** only when the player taps the *wrong* floating object while it is still active (tapping nothing, or letting a target word float off-screen unclaimed, only increments a separate `_missedWords` counter with **no coin penalty**). Running total is floor-clamped at 0 (`Mathf.Max(0, ...)`), never persisted, never exposed outside this scene.
- `EndRound()` sets state to `RoundComplete` and logs correct/missed counts to the console — **no structured result object leaves `GameManager`; no coins/score/time payload is ever surfaced for a backend to consume.**
- No completion timestamp or time-played tracking. `IStudyGame` not implemented.

---

## Duplicate Games Found

### 1. Word Match ↔ Rhyme Time — shared prefab, swapped manager component
Same root UI prefab (`Word Match.prefab`: left/right columns, line container, round text, timer, Time-Up panel) and the same drag-line-to-connect interaction family (`MatchPoint`/`UILineConnector` reused as-is by `RhymeTimeMatchPoint`). Rhyme Time's scene was built by **removing** the `WordMatchManager` component from an instance of that prefab and **adding** `RhymeTimeManager` in its place (content source and match-comparison logic differ: ScriptableObject + match-by-object-reference vs. JSON repository + match-by-`PairId`). This reuse is what produced the timer/audio wiring gap documented in Section 6 — it reads as an intentional code-reuse decision that was never followed through with re-wiring the new component's Inspector fields.

### 2. Story Quest ↔ Reading Detective — shared manager class, swapped content id
Literally the same `StoryQuestManager` component and the same shared quiz UI kit (`QuestionCardController`/`AnswerOptionView`/`AnswerFeedback`), differing only in the `_storyId` field (`sq_story_001` vs `rd_story_001`) and which JSON file that id resolves to. This is the cleanest, most intentional reuse in the project — and both games inherit the identical "Complete button not wired" gap as a result.

### 3. Sentence Builder ↔ Word Listen — duplicated hand-written logic, not a shared prefab
Not a shared Unity prefab (each has its own scene/prefab), but `SentenceBuilderManager` and `ListenWordManager` implement essentially the same drag-item-into-slot mechanic, the same timer/time-up/restart block (near-verbatim), and the same hint system — one operating on whole words, the other on individual letters. This looks like one class was copy-pasted from the other and adapted, rather than factored into a shared base. Not urgent, but worth consolidating if more slot-based games are planned, to avoid maintaining two diverging copies of the same logic.

---

## Backend Handoff Summary

### Distinct content data models needed (deduplicated across all 7 games)

| Model | Game(s) | Current source | Firestore-readiness |
|---|---|---|---|
| `RhymeTimePairData { pairId, wordA, wordB }` | Rhyme Time | JSON (`Shared/Resources/RhymeTime/Pairs.json`) | **Ready as-is** — already behind `IRhymeTimePairRepository` |
| `StoryQuestLevel { storyId, title, storyType, content, questions[{questionText, options[], correctOptionIndex}] }` | Story Quest, Reading Detective | JSON (`Shared/Resources/Stories/*.json`) | **Ready as-is** — already behind `IStoryQuestContentRepository` |
| `WordMatchEntry { id, word, imageRef, audioClipRef }` (grouped into rounds) | Word Match | ScriptableObject (`WordMatch/Level 1.asset`) | Needs new repository interface + asset-reference resolution strategy |
| `SentenceData { imageRef, sentence, decoyWords[] }` | Sentence Builder | ScriptableObject (`Sentence Builder/Level1.asset`) | Same — needs interface + asset resolution |
| `ListenWordData { imageRef, audioClipRef, targetWord, decoyLetters }` | Word Listen | ScriptableObject (`Word Listen/Level 1.asset`) | Same — needs interface + asset resolution |
| `WordEntry { word, audioClipRef }` inside `LevelDataSO { levelName, wordsPerRound, targetWordCount, spawnIntervalMin/Max, floatSpeedMin/Max, audioPlayInterval, allWords[] }` | Sight Word Pop | ScriptableObject (`ScriptableObjects/Level_01.asset`) | Same — needs interface (doc-comment already names it `IWordProvider` but it doesn't exist) |
| `FloatingObjectConfigSO { objectType, prefabRef, poolSize, shakeStyle, shakeIntensity, shakeSpeed, popParticlePrefabRef, popColor, correctPopSFXRef, wrongTapSFXRef, spawnWeight }` | Sight Word Pop | ScriptableObject (3 assets: Star/Cloud/Bubble) | Mostly client-side visual config; `spawnWeight` is the only field plausibly backend-tunable |

**Important caveat for the 4 ScriptableObject-based games:** their content bakes in direct `Sprite`/`AudioClip` references. A Firestore document can't hold a Unity asset reference — migrating these needs both a repository interface *and* a decision on how audio/image assets are resolved at runtime (Addressables, a CDN URL field, or similar) before any backend content can actually render. This is a materially bigger lift than Story Quest/Rhyme Time, which are already plain JSON with no embedded asset references.

### Player progress fields needed (currently entirely unbuilt — no game persists any of this)
- Completion state (not started / in progress / completed) — **not tracked by any of the 7 games.**
- Score / correct-count / total-questions — computed transiently in a few games (Story Quest's `CorrectAnswerCount`, Sight Word Pop's `_correctTaps`/`_missedWords`) but **never persisted or surfaced outside the running scene.**
- Coins earned per session — **only Sight Word Pop computes coins at all**, and only as a private, non-persisted `int`.
- Best score / attempts / completion timestamp — **tracked in zero games.**

### Moderation-relevant fields
None of the 7 games support user-uploaded or user-generated content — all content is developer-authored JSON or ScriptableObject data. No moderation surface is needed for the current feature set.

### Special backend logic implied
- Rhyme Time and Story Quest/Reading Detective already fit a clean "one Firestore document per content id" model matching their existing JSON shapes — lowest-effort migration path, since the repository interfaces already exist.
- The 4 ScriptableObject-based games need the interface layer built from scratch, plus an asset-resolution strategy, before any backend-driven content can work — this is the larger, unstarted half of the migration.
- No game currently has a weighted-spawn config that lives outside client-side ScriptableObjects except Sight Word Pop's `spawnWeight` per object type — a candidate for backend tuning if that's ever desired, but not required for launch.

---

## Cross-Game Shared Systems Check

- **`CoinWallet` / shared currency system: does not exist.** Only Sight Word Pop has any coin logic, and it's a private `int` on that game's own `GameManager` — not shared, not centralized. There is nothing to migrate *from* the other 6 games, because they never had coins to begin with.
- **Progress tracking (per-game/per-profile completion): not implemented anywhere.** No save/load, no `PlayerPrefs` usage, no persistence layer of any kind was found in any of the 7 games.
- **`IStudyGame` contract: does not exist as a type anywhere in the codebase.** All 7 games use informal, per-game logic — none implements a shared `Initialize`/`OnGameCompleted`/`GameResult` contract because that contract hasn't been created yet.
- **Shared UI feedback reuse:** `AnswerFeedback`/`AnswerOptionView`/`QuestionCardController` (`Shared/`) genuinely **are** shared and correctly reused — but currently only by Story Quest and Reading Detective (the only two quiz-format games, as expected). `UILineConnector` is likewise genuinely reused between Word Match and Rhyme Time. **No game reimplements a shared component where one already existed for its mechanic type** — the gap is that the two slot-based games (Sentence Builder, Word Listen) never had a shared component to reuse in the first place (see Duplicate Games #3), not that either one ignored an existing one.
