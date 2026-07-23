# Personalities, Morale & Squad Dynamics #33 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

Tests land at T-phase; this is the acceptance contract.

## 5.1 KD-1 read-contract locks (the headline)

- **T-HS-KD1-001** — `BuildHumanSystemsView` emits **only** `fromPlayerId/toPlayerId/edge` triples; the view
  carries **no** baseline / per-entity field (schema-shape assertion — FR-HS-015).
- **T-HS-KD1-002** — every routed `edge` is finite `∈ [0,1]`; `SetPlayerEdgeMirror` rejects a value outside
  `[0,1]` / NaN (fail-loud, FR-HS-018).
- **T-HS-KD1-003** — after `RouteIntoLivingWorld`, #22's `ApplyEvent` still **throws** on `PlayerEdge`, and a
  §3.1 owned-layer (`Affinity`/`Trust`) update leaves the mirrored `PlayerEdge` bit unchanged
  (`T-LW-U-035`-class — FR-HS-018).
- **T-HS-KD1-004** — wiring #22 phase-2 with an **empty** #33 view changes **no** #22 output byte across a
  full world-tick run (FR-HS-019 / KD-8).
- **T-HS-KD1-005** — one-directionality: #33's assembly references nothing in `living-world` (asmdef-shape
  assertion); no #33 code path reads a #22 memory/arc value (FR-HS-016/028).

## 5.2 Determinism & save/restore

- **T-HS-DET-001** — Save→restore across a **mid-season** boundary: each player's `MoraleState` /
  `PersonalityProfile` and every club-scoped pairwise `StrengthPermille` + mentoring pairing restore
  **field-identical**; resuming `AdvanceHumanSystemsDay` reaches the same state as an uninterrupted run.
- **T-HS-DET-002** — Save→restore across a **mid-`RollToNextSeason()`** boundary: #33 state already committed
  for the season restores field-identical; the FR-SN-029 restartable-transform contract.
- **T-HS-DET-003** — Two-run determinism: a full season's `AdvanceHumanSystemsDay` sequence from one world
  seed produces byte-identical #33 state for every player on both runs.
- **T-HS-DET-004** — **Draw-free lock:** the serialized #33 block contains **no** `RngCursor`/`actionOrdinal`
  field (grep/schema-shape assertion); `AdvanceHumanSystemsDay` reads no RNG (FR-HS-009/013).
- **T-HS-DET-005** — Purity: `AdvanceHumanSystemsDay` called twice with identical inputs and the same
  `worldDay` yields the same first result and a no-op second call (F6/FR-HS-012).

## 5.3 Behaviour-neutral identity (KD-8)

- **T-HS-NEU-001** — A default squad (all `Create()` seeds, neutral pairwise) advances a world day with a
  neutral committed input to **exactly** the same state (morale/traits/pairwise unchanged); `DeriveCliques`
  returns none; the empty view leaves #22 byte-identical (§3.5 worked example).
- **T-HS-NEU-002** — `MoraleState.Create()` = `{500, 500, sentinel}`; `PersonalityProfile.Create()` = all
  `TRAIT_NEUTRAL` — the pre-first-season identity.
- **T-HS-NEU-003** — Registering #33 leaves every existing RNG stream's cursor **byte-identical** across a
  full season run with and without #33 active — trivially true (no #33 stream at minimal), the #40
  `T-FN-NEU-003` test class.

## 5.4 Clique / chemistry derivation (KD-4)

- **T-HS-CLQ-001** — `DeriveCliques` is a pure read: no clique/chemistry field is persisted in the sub-blob
  (schema-shape assertion — FR-HS-020/021); a pairwise edit is reflected in both the derived clique and the
  #22 mirror with no third stored copy.
- **T-HS-CLQ-002** — **Int/float boundary + mutuality lock:** a pairwise at `600` in a direction does **not**
  pass the threshold (`600 > 600` false; `600/1000f > 0.6f` false); `601` passes on both representations
  (`601/1000f > 0.6f` true); and a **one-sided** strong tie (a→b = 700, b→a = 500) forms **no** clique edge
  (mutuality — matches #22's "mutual > 0.6") — the KD-4/L2 cross-representation + mutuality proof.

## 5.5 Ordering, idempotency & lifecycle

- **T-HS-ORD-001** — `AdvanceHumanSystemsDay` runs at #30 slot 3 (before #41's slot 4, before
  `WorldStore.AdvanceDay()`) — a structural/ordering assertion against `RunWorldTickInFixedOrder`.
- **T-HS-ORD-002** — Advancing the same `worldDay` twice is a no-op; a `worldDay` gap fails loud (F6).
- **T-HS-LIFE-001** — A #28 regen inserts a neutral `MoraleState.Create()`/`PersonalityProfile.Create()` for
  the fresh `PlayerId` and drops the id from prior teammates' pairwise sets; a retirement removes the
  retiree's per-player + pairwise entries — no unbounded leak across seasons (FR-HS-027).

## 5.6 Integer posture & fail-loud

- **T-HS-INT-001** — Every `MoraleState`/`PersonalityProfile`/`PairwiseRelationship` field is an integer; no
  projection introduces a float except the single `StrengthPermille / 1000f` at the #22 boundary
  (static/reflection assertion — FR-HS-004).
- **T-HS-FAIL-001** — Bad `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` → fail loud (F3).
- **T-HS-FAIL-002** — Out-of-bounds length prefix / trailing bytes → fail loud (F5).
- **T-HS-FAIL-003** — `default(PersonalityProfile)` (traits `0 ∉ [1,20]`) reaching a consuming seam → fail
  loud (F4); a default-constructed per-player record is rejected here. (`default(MoraleState)` alone is
  field-in-contract and never used unpaired — FR-HS-005 — so it is not independently asserted.)
- **T-HS-FAIL-004** — An out-of-range morale/trait/relationship value, or a pairwise `PlayerId` outside the
  club universe, reaching a consuming seam → fail loud (F1/F2/F7).

## 5.7 FR traceability

| FR | Covering test(s) |
|---|---|
| FR-HS-001 | T-HS-ORD-001, T-HS-DET-003 |
| FR-HS-002 | T-HS-DET-001 |
| FR-HS-003 | T-HS-DET-001, T-HS-CLQ-001 |
| FR-HS-004 | T-HS-INT-001 |
| FR-HS-005 | T-HS-NEU-002, T-HS-FAIL-003 |
| FR-HS-006 | T-HS-NEU-001, T-HS-NEU-002 |
| FR-HS-007 | T-HS-KD1-001 (equilibrium not in the routed view) |
| FR-HS-008 | T-HS-NEU-002, T-HS-ORD-002 |
| FR-HS-009 | T-HS-DET-004 |
| FR-HS-010 | T-HS-NEU-001 (neutral drift), §3.1 worked example |
| FR-HS-011 | T-HS-NEU-001, T-HS-CLQ-002 |
| FR-HS-012 | T-HS-DET-005, T-HS-ORD-002 |
| FR-HS-013 | T-HS-DET-004, T-HS-NEU-003 |
| FR-HS-014 | (deep-tier — recorded in §7; keyed-draw contract) |
| FR-HS-015 | T-HS-KD1-001 |
| FR-HS-016 | T-HS-KD1-005 |
| FR-HS-017 | T-HS-KD1-001, T-HS-KD1-005 |
| FR-HS-018 | T-HS-KD1-002, T-HS-KD1-003 |
| FR-HS-019 | T-HS-KD1-004 |
| FR-HS-020 | T-HS-CLQ-001, T-HS-CLQ-002 |
| FR-HS-021 | T-HS-CLQ-001 |
| FR-HS-022 | T-HS-NEU-001 (empty mentoring), (deep-tier #34 seam — §7) |
| FR-HS-023 | (deferred consumption — recorded in §7) |
| FR-HS-024 | (deferred consumption — recorded in §7) |
| FR-HS-025 | T-HS-KD1-005 (no write-back path) |
| FR-HS-026 | T-HS-DET-001, T-HS-FAIL-001, T-HS-FAIL-002 |
| FR-HS-027 | T-HS-LIFE-001 |
| FR-HS-028 | T-HS-KD1-005 |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial test plan (T-HS-*) + full FR-HS-001..028 traceability. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (M): T-HS-CLQ-002 gains the mutuality case; T-HS-FAIL-003 scoped to `PersonalityProfile`. |
#endregion
