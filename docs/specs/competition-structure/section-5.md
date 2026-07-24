# Competition Structure #43 — Section 5: Test Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file AR PASS-1; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 5.1 Behaviour-neutral identity (KD-8) — the headline

- **T-CP-NEU-001** — a season under the singleton collection advances **byte-identical** to bare
  #30 (same fixtures, table, roll, digests); the (a') point is unvisited; no RNG stream is
  registered and every existing cursor is byte-identical (the #40 `T-FN-NEU-003` class); the
  sub-blob is the version tag + instance-0 binding only (FR-CP-003).
- **T-CP-NEU-002** — #43 holds no #30 object (static/reflection assertion — FR-CP-002); instance-0
  reads route through the root's #30 read surface.

## 5.2 Draw determinism (KD-2/KD-7, deep)

- **T-CP-DET-001** — two-run byte-identical drawn brackets (a full cup season) from one world seed.
- **T-CP-DET-002 (stability)** — the same round drawn after any save→restore and in any call order
  yields identical pairings (keyed, cursor-free — FR-CP-007/024).
- **T-CP-DET-003 (independence)** — with two competitions drawing on the same day, permuting one's
  draw calls leaves the other's bracket byte-identical (distinct `entityId`).
- **T-CP-DET-004 (re-derivation cross-check)** — a persisted bracket equals a from-keys
  re-derivation over the same results (locks FR-CP-010's persist-vs-derive coherence).
- **T-CP-DET-005 (canonical-order lock)** — a shuffled-input entrant set, canonicalized, produces
  the same pairings as the ascending input (FR-CP-005); feeding a non-canonical list to `DrawRound`
  fails loud.
- **T-CP-DET-006 (§3.2 mechanics)** — with an rng stub yielding `(2, 0, 1)`, `DrawRound` over
  `[3, 7, 12, 20]` reproduces `[12, 7, 20, 3]` (the §3.2 worked example's Fisher–Yates mechanics);
  ordinal bound violations (F5) throw.
- **T-CP-DET-007 (instance seeds)** — `DeriveInstanceSeed` is a pure draw-free derivation
  (instrumented rng seam asserts zero stream use); two instances over the same club set produce
  distinct fixture sequences; same inputs ⇒ same seed (FR-CP-006).

## 5.3 Bracket coherence (KD-3, deep)

- **T-CP-BRK-001** — round entrant counts halve; a winner ∉ its pairing fails loud (F4); round-0
  entrant multiset equals the competition's entrant set.
- **T-CP-BRK-002** — a restored bracket is field-identical and **no draw re-rolls on load**
  (instrumented rng seam asserts zero draw computations during decode — FR-CP-025).
- **T-CP-BRK-003 (F2)** — drawing a round before the prior round fully resolves, or on a
  non-knockout instance, fails loud.

## 5.4 Promotion/relegation (KD-4, deep)

- **T-CP-PRO-001** — the §3.4 worked example reproduces the exact membership swap; same standings
  ⇒ same swap (two-run).
- **T-CP-PRO-002** — a one-division world: the transform is a no-op (FR-CP-018).
- **T-CP-PRO-003** — mid-roll save→restore across (a') continues to the same post-roll state
  (the FR-SN-029 restartability contract extended).
- **T-CP-PRO-004** — `ClubId`s unchanged by the transform; #43 mutates membership only (static
  assertion — no #27/#31-style migration dispatch); step (c)'s regenerated fixtures use the
  **post-transform** club sets (FR-CP-017).
- **T-CP-PRO-005 (F2)** — mismatched division tables (a club in neither/both) fail loud.

## 5.5 Merged calendar (KD-5, deep)

- **T-CP-CAL-001** — no club plays twice in a day across the collection; cup rounds land only on
  days their entrants are league-free; the view is a pure function (same mappings ⇒ same schedule).
- **T-CP-CAL-002** — #30's `SeasonCalendar` is byte-unchanged by merged-view queries; the minimal
  path never invokes the view (FR-CP-019).

## 5.6 Save round-trip (KD-6)

- **T-CP-SAV-001** — the sub-blob round-trips field-identical (minimal: binding only; deep:
  registry + brackets + membership) and survives `RollToNextSeason`.
- **T-CP-SAV-002 (F3/F4)** — bad version / out-of-bounds length (overflow-safe `total − offset`) /
  trailing bytes / non-ascending `CompetitionId`s or entrant `ClubId`s / duplicate entrants /
  incoherent brackets all throw at decode.
- **T-CP-SHAPE-001** — the serialized block contains **no** `RngCursor`/`actionOrdinal` field
  (schema-shape assertion, FR-CP-014).
- **T-CP-INT-001** — every field integer; no float in #43 (static/reflection assertion, FR-CP-023).

## 5.7 Fail-loud (F1/F6)

- **T-CP-FAIL-001 (F1)** — an operation naming an unknown `CompetitionId` / an entrant outside the
  #27 club universe throws.
- **T-CP-FAIL-002 (F6)** — a non-league instance with an empty entrant set reaching a consuming
  seam throws; instance 0 (legitimately empty — its entrants live in #30) does not.

## 5.8 Requirement traceability

Every FR-CP-001..025 maps to a T-CP-* test above **or** a recorded §7 deferral. Deep-tier-only
requirements (FR-CP-007/009/010/011/015..019/024) are locked at their minimal identity boundary
now (the singleton-collection equality) and fully at the deep T-phase.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §5 (identity, draw determinism, bracket coherence, promotion/relegation, merged calendar, save, fail-loud, traceability), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1: **L** — T-CP-DET-006 rephrased to a stubbed-rng mechanics lock (the v0.1 phrasing asserted illustrative "say"-values no real seed derives); **M follow-through** — new T-CP-DET-007 locks `DeriveInstanceSeed` (pure, draw-free, distinct-instance independence — FR-CP-006). |
#endregion
