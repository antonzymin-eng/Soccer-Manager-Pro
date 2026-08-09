# Player Progression & Lifecycle #28 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** August 9, 2026 (v0.5 — ERR-028-014: §5.1's `AdvanceDay_FirstCall_*` passage corrected from the retired sentinel-anchored behaviour to the shipped seed-day-replay behaviour, with the inversion noted; new §5.9/§5.10 allocate T-PG-BLOCK-001..007 / T-PG-BATCH-001 / T-PG-CODEC-001..007 and §5.7 gains T-PG-SAVE-007/008 for ~17 mutation-audit-proven locks that landed across commits `043ccd0`/`1d19bc8`/`9392839` with no prior test-plan id)
**Last Updated (prior):** August 8, 2026 (v0.4 — ERR-028-006/007/008/009: new locks for the signed anchor, the cross-blob cursor rule, the roster-overwrite refusal, and the F8 sentinel guard)
**Last Updated (prior):** August 8, 2026 (v0.3 — ERR-028-005: T-PG-DET-002 reworded to the gap-replay semantic that makes it satisfiable)
**Version:** 0.5
**Status:** APPROVED

---

Test IDs `T-PG-*`. The aging half is draw-free (FR-PG-002), so its determinism is a pure-projection
lock; only regen generation draws.

## 5.1 Byte-exact restore (KD-1 / FR-PG-006) — the keystone

- **T-PG-DET-001** — Save on **any day** → restore → advance to a later day == an uninterrupted
  advance, byte-for-byte (attributes + `GrowthCursor` + `BirthWorldDay`). Because age is derived (no
  discrete rollover step) and every mutation is integer, a save on the day an attribute-point is spent,
  the day before, and the day after all restore to the identical continuation — nothing is
  double-counted across the save boundary.
- **T-PG-DET-002** — A single `AdvanceDay` call spanning a far-future gap equals a day-by-day advance
  over the same span, for **both** derived age **and** the accumulated `GrowthCursor` — not just age.
  Age matches trivially, being gap-independent (a pure function of `(worldDay, BirthWorldDay)`,
  §3.1.1); the cursor matches only because `AdvanceDay` **replays every intervening day** internally
  (§3.1's `LastAdvancedWorldDay` walk) rather than accruing once for the whole gap — a naive single call
  to the per-player projection would bank one day's `dailyPts` and lose the rest (ERR-028-005). Locked
  separately: `AdvanceDay_FirstCall_ReplaysFromTheSeedDay` — **renamed and its assertion INVERTED at
  ERR-028-014** from `AdvanceDay_FirstCall_AdvancesExactlyOneDay`, which had asserted that the first
  call on a never-advanced store (cursor at the sentinel) advances **exactly one day** and anchors the
  cursor there, "since it cannot know how far in the past the career actually began accruing." That
  reasoning is exactly what ERR-028-014 found false: `ProgressionEngine.SeedFrom` **is** handed the
  seed day (`newGameWorldDay`), so the store always knows where the career's lived history starts, and
  the old test was locking the silent-data defect as intended behaviour — a whole-league age jump with
  only one day of cursor accrual behind it. As shipped, the first call **replays from the seed day like
  any other gap** (the cursor is anchored there by `SeedFrom`, not at the sentinel), so there is no
  first-call special case left at all: a first advance 300 days after the seed day accrues 300 days,
  not one. The sentinel itself is no longer a legal #28 store state (`FromBlocks` refuses a carried
  sentinel; `AdvanceDay` has no never-advanced branch) — see T-PG-BLOCK-007 in §5.9.
- **T-PG-DET-004** — A store seeded at world day **0** (`newGameDay = 0`, the day a real new game
  actually starts) preserves every player's generated age through the first daily step. This is the
  ERR-028-006 regression lock: a `BirthWorldDay` clamped to 0 instead of held negative reports the
  derived age as `worldDay / DAYS_PER_YEAR`, which reads the **entire league** as age 0 the moment
  `AdvanceDay` runs once — this lock fails immediately under the clamped implementation and passes only
  when the anchor is genuinely negative for a non-zero generated age.
- **T-PG-DET-005** — A negative `BirthWorldDay` (the day-0-bootstrap case) survives the save codec
  round-trip byte-for-byte — `Encode` then `Decode` reproduces the identical signed value, not a
  wrapped or truncated one (ERR-028-006's §3.5 `i64` widening).

**Fixture hazard recorded (ERR-028-006).** Both of #28's existing fixtures used `BaseDay = 100000`
specifically — the comment on them read *"large enough that `BirthWorldDay` stays non-negative"* — which
meant no test in the suite ever exercised the day-0 path a real new game actually starts on, and the
whole-league-reads-as-age-0 defect shipped with every existing test green. A fixture chosen to keep a
value on the safe side of a defect is how that defect ships: T-PG-DET-004/005 above exist specifically
to run at `BaseDay = 0`, not at a value picked to avoid the failure mode being tested for.

## 5.2 Two-run determinism

- **T-PG-DET-003** — The same seed drives a **multi-season** aging projection (build roster → advance
  K seasons through `AdvanceDay` + `RunSeasonBoundary`) to a **byte-identical** final career-state
  block across two independent runs (the end-to-end lock; aging is draw-free, regen is stream-deterministic).

## 5.3 Behaviour-neutral identity (KD-8 / FR-PG-007)

- **T-PG-ID-001** — `curveEnabled` **off** reproduces the literal §4.3 step exactly: a Growth-band
  player gains +1 on exactly one attribute per `DAYS_PER_YEAR` days; a Decline-band player loses 1;
  a Stable-band player is unchanged over the year — the deep-curve-off run == the literal-step run,
  byte-for-byte.
- **T-PG-ID-002 (KD-2 seam neutrality)** — The daily step with `TrainingInput.Neutral` == the daily
  step with no training input, byte-for-byte (the #29 seam adds nothing until #29 writes a non-neutral
  value).

## 5.4 CA/PA model (FR-PG-003 / §3.2)

- **T-PG-CA-001** — `CurrentAbility` recomputed from the restored `[1,20]` attributes equals the
  serialized CA cache (recompute-equals-stored — a corrupt CA can never diverge).
- **T-PG-CA-002** — A growth spend that would push CA past `PotentialAbility` is a no-op at the ceiling
  (F1); the attribute stays, the cursor is not consumed past the ceiling.
- **T-PG-CA-003** — The weighted spend order raises a position's signature attributes first and breaks
  ties by ascending `AttrIdx` (deterministic, no draw).

## 5.5 Regen (KD-3 / FR-PG-010..012)

- **T-PG-REG-001** — Same seed + same club → same newgen `PlayerRecord` (the `RosterGeneratorTests`
  posture: exact `PROGRESSION_REGEN_FIELDS` draw budget, bounds, position/attributes/PA).
- **T-PG-REG-002** — A regen gets a **fresh monotonic `PlayerId`** (≠ the retiree's); after a
  retirement+regen cycle the block has no stale lifecycle entry keyed by the retired id (FR-PG-011).
- **T-PG-REG-003** — A regen's `[1,20]` attributes are generated below its drawn PA (room to grow).

## 5.6 Retirement + season boundary (KD-5 / KD-6 / FR-PG-013..015 / 024)

- **T-PG-RET-001** — A player crossing `RETIREMENT_AGE` mid-season is **flagged** and stays selectable;
  no `Squad` mutation lands mid-fixture.
- **T-PG-RET-002** — `RunSeasonBoundary` emits the retirees + a 1:1 regen per vacancy; the block entry
  count is unchanged (FR-PG-019, no unbounded growth).
- **T-PG-RET-003 (F6 idempotency)** — `RunSeasonBoundary` invoked twice for one boundary is a no-op
  the second time; a save mid-roll → restore → re-run does not double-apply (the retirees/regens are
  identical).
- **T-PG-RET-004** — `RunSeasonBoundary` does **not** re-bank growth (a Stable-band player's attributes
  are unchanged by the boundary step — growth was banked daily, KD-6).

## 5.7 Persistence fail-loud (FR-PG-016..019)

- **T-PG-SAVE-001** — Full-block save→restore round-trip is field-identical (records + overlays +
  `NextPlayerId` + boundary marker).
- **T-PG-SAVE-002** — Fail-loud on a bad `PROGRESSION_SAVE_FORMAT_VERSION` (F3), an out-of-bounds
  entry-count prefix (F5, overflow-safe), and trailing bytes (F5).
- **T-PG-SAVE-003 (composed)** — The block round-trips through the season save (world + season +
  progression + optional match), reusing the `SeasonSaveManagerTests` posture — the world/match blobs
  stay byte-untouched (FR-PG-017).
- **T-PG-SAVE-004 (F8)** — The never-advanced sentinel is refused as a `worldDay` argument to
  `AdvanceDay` (ERR-028-009); the guard fires before any validation or mutation, so a rejected call
  leaves the store's cursor untouched.
- **T-PG-SAVE-005** — The progression cursor (`LastAdvancedWorldDay`) is refused when it is **ahead**
  of the world clock, and when it is **more than one day behind** it, at each of the three boundaries
  independently: `SeasonSaveManager.Save`, `SeasonSaveManager.Load`, and `SeasonLoop` composition
  (ERR-028-007). A lag of exactly one day — the normal state between a day step and the clock's own
  increment — is accepted at all three.
- **T-PG-SAVE-006** — A zero-club progression block is refused when saving it would overwrite a
  destination file that already carries a populated one (ERR-028-008); an empty store may still create
  a new file or overwrite an already-empty one, and an unreadable or foreign destination is overwritten
  as before.
- **T-PG-SAVE-007** — `SeasonSaveManager.Save` refuses a `null` progression argument
  (`ArgumentNullException`, `ParamName == "progression"`) on the same terms as `trainingClubs` /
  `medicalClubs` / `appearanceClubs` — null is not the empty set, and since KD-4 this block carries the
  roster itself, so "this season tracks no careers" must be said with an empty `ProgressionEngine`,
  never with null (mutation-audit lock: deleting the guard left the whole suite green).
- **T-PG-SAVE-008** — `SeasonSaveManager.Load` restores its returned rosters from the **file's**
  progression block, not from the caller-supplied bootstrap provider, whenever the file carries a
  populated block (`rosterSource = progression.ClubCount > 0 ? new ProgressionSquads(progression) :
  squads`). Discriminated by continuing the determinism digest chain past the restore: the fixture
  seeds a day-0 bootstrap roster and a file whose progression block has banked real #28 growth, so only
  the file-sourced branch reproduces the boot-time simulation (mutation-audit lock: forcing the
  unconditional `squads` branch left the whole suite green, because every other in-progress-match
  round-trip test hands the *same* roster to both paths and cannot tell them apart).

## 5.8 A `#19 ScenarioRunner` capstone (post-wiring, not gating)

- **T-PG-SIM-001 (optional)** — `multi-season-aging`: build a roster, advance N seasons, assert the
  aged state + a determinism digest — the match-engine capstone precedent. Added once the engine is
  wired (T2), not required at the design stage.

## 5.9 Block integrity — `FromBlocks`/`ToBlocks`/batch guards (mutation-audit locks, 2026-08-09)

Landed across commits `043ccd0`, `1d19bc8` and `9392839` with no prior `T-PG-*` id. Each was found by a
**mutation audit**: the guard was deleted, the whole suite (including every existing `T-PG-*` lock)
stayed green, and the new test above is the one that then failed — proof that, before this pass, no
test failed if the guard were reverted.

- **T-PG-BLOCK-001** — `FromBlocks` refuses a block whose club ids do not strictly ascend
  (`FromBlocks_NonAscendingClubIds_IsRefused`) — every lookup `FromBlocks` builds is a binary search
  over that invariant.
- **T-PG-BLOCK-002** — `FromBlocks` refuses a club whose player ids do not strictly ascend within it
  (`FromBlocks_NonAscendingPlayerIdsWithinAClub_IsRefused`) — an unordered block makes a carried player
  un-findable, and the miss then reads as "new".
- **T-PG-BLOCK-003** — `FromBlocks` refuses an id-allocation cursor (`nextPlayerId`) at or behind the
  highest player id the block already carries
  (`FromBlocks_IdCursorAtOrBelowHighestCarriedPlayerId_IsRefused`) — otherwise the next regen allocation
  could collide with a live player (FR-PG-011).
- **T-PG-BLOCK-004** — `FromBlocks` **copies** its input `records`/`lifecycles` arrays rather than
  borrowing them (`FromBlocks_CopiesTheStateArrays_NotBorrowsThem`) — mutating the caller's arrays after
  construction must not reach the running career (the #29/#41 AR pass-3 finding, closed on the save
  route and left open on this one until this lock).
- **T-PG-BLOCK-005** — `FromBlocks` refuses a player id shared across two clubs
  (`FromBlocks_ACrossClubDuplicatePlayerId_IsRefused`) — `SeedFrom`'s twin of this guard was already
  locked; this is the *other* route in (ERR-041-019 / ERR-027-004).
- **T-PG-BLOCK-006** — `ToBlocks` hands out **copies**, not the store's live arrays
  (`ToBlocks_ReturnsCopies_NotTheStoresLiveArrays`) — mutating a returned block must not reach the
  store; the store is the single writer (FR-PG-022).
- **T-PG-BLOCK-007** — `FromBlocks` refuses a lifecycle carrying the never-advanced sentinel
  (`FromBlocks_ANeverAdvancedSentinelCursor_IsRefused`, **ERR-028-014**) — the sentinel is a refused
  `worldDay` argument to `AdvanceDay` (F8), never a legal *stored* cursor: a career's lived history must
  start somewhere the world clock can be checked against, or the cursor-vs-clock gate (§2.3 F8 of #30)
  waves it through at any clock while the first advance banks a single day for the whole span. See the
  §5.1 correction above — this is the lock on the fix, not on the retired behaviour.
- **T-PG-BATCH-001** — `AdvanceDay`'s batch validation catches a `ClubId` mismatch **at an index**
  even when the player ids and count at that same index still agree
  (`AdvanceDay_BatchClubIdMismatchAtAnIndex_IsRefused_EvenWhenPlayerIdsAndLengthsAgree`) — isolates the
  positional club-id check from the downstream player-id/length checks by swapping two clubs' batch
  entries while leaving each entry's player ids and count genuinely correct for its new index, so a
  club-id-only guard is the only thing that can catch the drift.

## 5.10 Codec corruption gates (mutation-audit locks, 2026-08-09)

Also landed across `043ccd0`/`1d19bc8`/`9392839`, same audit method as §5.9 (each guard proven
previously unlocked by deletion).

- **T-PG-CODEC-001** — `Encode` refuses a player id shared across two clubs
  (`Encode_CrossClubDuplicatePlayerId_FailsLoud`) — a career requires globally unique player ids
  (ERR-041-019 / ERR-027-004); `CanonicalOrder` alone cannot see across clubs.
- **T-PG-CODEC-002** — `Decode` refuses a cross-club duplicate player id that reaches it despite
  `Encode`'s own refusal (`Decode_CrossClubDuplicatePlayerId_FailsLoud`) — the only way such bytes exist
  is a corrupt/hand-edited file (two internally-valid single-club blobs spliced under one shared
  header); the cross-club global-uniqueness gate at `Decode` is the only thing that can catch a splice.
- **T-PG-CODEC-003** — `Decode` refuses an attribute outside `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]`
  (`Decode_AttributeOutOfRange_FailsLoud`) — this block is the roster now (KD-4), so a corrupt attribute
  would flow straight into `PlayerAttributeProjection` and the match engine.
- **T-PG-CODEC-004** — `Decode` refuses a weak-foot rating outside
  `[WEAK_FOOT_MIN, WEAK_FOOT_MAX]` (`Decode_WeakFootOutOfRange_FailsLoud`) — a different scale from the
  `[1,20]` attributes, gated separately.
- **T-PG-CODEC-005** — `Decode` refuses a negative decoded age (`Decode_NegativeAge_FailsLoud`) — not
  representable in the model; the authoritative anchor is `BirthWorldDay`, which MAY legitimately be
  negative (ERR-028-006), but the derived age cache may not.
- **T-PG-CODEC-006** — `Decode` refuses a string length prefix that overruns the remaining blob
  (`Decode_GuardedStringLengthOverrunsBlob_FailsLoud`) — `ReadGuardedString`'s own bound, ahead of
  `CanonicalSerializer.ReadString`'s unchecked index; must throw, never over-read past the buffer.
- **T-PG-CODEC-007** — `Decode` refuses a `PotentialAbility` outside `[PA_MIN, ABILITY_MAX]`
  (`Decode_PotentialAbilityOutOfRange_FailsLoud`) — PA is the F1 growth ceiling, and a corrupt value
  below `PA_MIN` would silently freeze a player's growth forever
  (`AbilityModel.TrySpendOnePoint` returns false once `CurrentAbility >= PotentialAbility`).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan (T-PG-*): byte-exact restore, two-run determinism, behaviour-neutral identity, CA/PA, regen, retirement/boundary, fail-loud, optional capstone. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-08-08 | — | ERR-028-005: T-PG-DET-002 reworded — the long-gap cursor match holds because `AdvanceDay` replays every intervening day, not because the cursor is gap-independent (only age is); added the separately-locked first-call-anchors-at-one-day semantic. Spec + code, same commit (T1/T2a). |
| 0.4 | 2026-08-08 | — | Added T-PG-DET-004/005 (ERR-028-006 day-0 age-preservation regression lock + negative-anchor round-trip) with the §5.1 fixture-hazard note (`BaseDay = 100000` kept both fixtures off the one day-0 path the product starts on); added T-PG-SAVE-004 (F8 sentinel refusal, ERR-028-009), T-PG-SAVE-005 (cross-blob cursor-vs-clock refusal at all three boundaries, ERR-028-007), T-PG-SAVE-006 (populated-roster overwrite refusal, ERR-028-008). Spec-only, locks for the AR-over-T1/T2a landing. |
| 0.5 | 2026-08-09 | — | **ERR-028-014**: §5.1's `AdvanceDay_FirstCall_AdvancesExactlyOneDay` passage corrected — the test was renamed `AdvanceDay_FirstCall_ReplaysFromTheSeedDay` and its assertion INVERTED, because the quoted reasoning ("cannot know how far in the past the career actually began accruing") was itself the defect: `SeedFrom` **is** handed the seed day. New **§5.9** (`T-PG-BLOCK-001..007`, `T-PG-BATCH-001`) and **§5.10** (`T-PG-CODEC-001..007`) allocate ids for ~15 `FromBlocks`/`ToBlocks`/batch/codec guards that landed across commits `043ccd0`, `1d19bc8` and `9392839` with no prior test-plan id, each proven previously unlocked by a mutation audit (delete the guard, confirm the whole suite including every existing `T-PG-*` lock stays green, restore it). §5.7 gains **T-PG-SAVE-007/008** for two `SeasonSaveManagerTests` mutation-audit locks (`Save` null-progression refusal; `Load` restoring from the file's roster, not the caller's bootstrap provider). Spec-only, no code change. |
#endregion
