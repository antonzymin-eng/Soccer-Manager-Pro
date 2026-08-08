# Player Progression & Lifecycle #28 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.3 — ERR-028-005: T-PG-DET-002 reworded to the gap-replay semantic that makes it satisfiable)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.3
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
  separately: `AdvanceDay_FirstCall_AdvancesExactlyOneDay` — the first call on a never-advanced store
  (cursor at the sentinel) advances **exactly one day** and anchors the cursor there, since it cannot
  know how far in the past the career actually began accruing.

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

## 5.8 A `#19 ScenarioRunner` capstone (post-wiring, not gating)

- **T-PG-SIM-001 (optional)** — `multi-season-aging`: build a roster, advance N seasons, assert the
  aged state + a determinism digest — the match-engine capstone precedent. Added once the engine is
  wired (T2), not required at the design stage.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan (T-PG-*): byte-exact restore, two-run determinism, behaviour-neutral identity, CA/PA, regen, retirement/boundary, fail-loud, optional capstone. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-08-08 | — | ERR-028-005: T-PG-DET-002 reworded — the long-gap cursor match holds because `AdvanceDay` replays every intervening day, not because the cursor is gap-independent (only age is); added the separately-locked first-call-anchors-at-one-day semantic. Spec + code, same commit (T1/T2a). |
#endregion
