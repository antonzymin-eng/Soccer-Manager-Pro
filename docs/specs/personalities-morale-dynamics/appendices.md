# Personalities, Morale & Squad Dynamics #33 — Appendices

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Every constant carries exactly one source tag. Magnitudes marked `[GT]` are illustrative pending a Stage-2/3
balance pass (the #21 G2 precedent); the shapes/directions are the reviewed contract.

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` | 1 | [FIXED] | The #33 sub-blob version (KD-7). A season-save sub-blob, independently gated from `WORLD_STORE_FORMAT_VERSION` / `SEASON_SAVE_FORMAT_VERSION` / `PROGRESSION_/TRAINING_/FINANCE_/MEDICAL_SAVE_FORMAT_VERSION`. |
| `PERMILLE_DENOM` | 1000 | [FIXED] | Shared per-mille denominator; keeps every ratio integer, no float (FR-HS-004). |
| `MORALE_NEUTRAL_PERMILLE` | 500 | [GT] | The neutral "content" morale seed (`MoraleState.Create`) and the pre-first-season equilibrium — a design default a balance pass could tune; illustrative pending G2 (tagged `[GT]` alongside the sibling neutral seeds for consistency). |
| `TRAIT_NEUTRAL` | 10 | [GT] | The neutral trait seed (`PersonalityProfile.Create`); the #27 T0 all-neutral seed value, a tunable design default (illustrative pending G2). |
| `HS_NOT_ADVANCED_SENTINEL` | `uint.MaxValue` | [FIXED] | `MoraleState.LastAdvancedWorldDay` unadvanced sentinel — **NOT** `0` (F6 / the #41 `MEDICAL_NOT_ADVANCED_SENTINEL` precedent). |
| `CLIQUE_THRESHOLD_PERMILLE` | 600 | [DERIVED] | `= 0.6 × PERMILLE_DENOM` — the per-mille rescale of vol-2 §2.1's `0.6` clique threshold (consumed by #22 as `[CROSS]` from vol-2, mirrored here at per-mille). A **mutual** pair is a clique edge iff both directions' `StrengthPermille > 600` (§3.2); `600/1000f == 0.6f` exactly, so the rescale introduces no boundary drift. |
| `RELATIONSHIP_NEUTRAL_PERMILLE` | 500 | [GT] | The neutral pairwise seed (strangers/acquaintance baseline); below `CLIQUE_THRESHOLD_PERMILLE`, so a default squad forms no clique; illustrative pending G2. |
| `MORALE_DRIFT_STEP_PERMILLE` | 20 | [GT] | Max per-day morale drift toward target (`DriftPermille`, §3.1). Bounds the daily swing; illustrative pending G2. |
| `RELATIONSHIP_DRIFT_STEP_PERMILLE` | 5 | [GT] | Max per-day pairwise drift toward its co-appearance/result target; illustrative pending G2. |
| `MORALE_TARGET_WIN_DELTA_PERMILLE` | 40 | [GT] | Committed-input nudge to the morale target on a win (a loss subtracts, a benching subtracts); illustrative, direction is the contract. |
| `MORALE_TEMPERAMENT_DAMPEN_MAX_PERMILLE` | 300 | [GT] | The maximum dampening a high `Temperament` applies to the target swing (a steadier player swings less); illustrative. |

**`DOMAIN_TAG_HUMAN_SYSTEMS` / `SubsystemOrdinals.HumanSystems`** — `0x25` / `87` respectively, per
`docs/tracking/personalities-morale-dynamics-design.md` §5 and the roadmap §6 off-pitch reservation.
**RESERVED, NOT promoted** at this spec's approval (present as the `_RESERVED_0x25_` placeholder row in #16
§3.4 — the #40 `_RESERVED_0x29_` / KD-6 precedent) because the minimal tier is draw-free. These are **not**
`[GT]`/`[FIXED]` project constants declared in this catalogue — they are #16's tag-namespace reservation,
cross-cited `[CROSS: #16 §3.4]` once promoted at #33 T3's first stochastic draw.

## Appendix B — Worked example: clique int/float boundary (KD-4 / L2)

The threshold test (applied per direction; a clique edge additionally requires **both** directions to pass —
the mutuality rule, §3.2) agrees exactly across the two representations:

| `StrengthPermille` (one direction) | #33 test (`> 600`) | #22 float test (`permille/1000f > 0.6f`) | Passes threshold? |
|---|---|---|---|
| 599 | false | `0.599f > 0.6f` → false | no (both) |
| 600 | false | `0.600f > 0.6f` → false | no (both) |
| 601 | true | `0.601f > 0.6f` → true | yes (both) |

`600 / 1000f == 0.6f` exactly (600 and 1000 are representable, the quotient is the representable `0.6f`), so
there is no boundary disagreement to leak. A **one-sided** strong tie (a→b = 700, b→a = 500) passes the
threshold in one direction but is **not** a clique edge (mutuality fails), matching #22. Locked by
T-HS-CLQ-002.

## Appendix C — Worked example: save/restore across a mid-season boundary

Seed: club 7, world day 12, after a run of fixtures has drifted state. Player 0:
`MoraleState { MoralePermille: 560, EquilibriumPermille: 500, LastAdvancedWorldDay: 12 }`, traits neutral;
pairwise (0,1) `StrengthPermille: 615` (a clique edge). Save now; restore. All fields restore field-identical.
Continuing `AdvanceHumanSystemsDay` for day 13 with a neutral input reaches the same
`{ MoralePermille: 552, … }` (drift toward 500 by ≤ `MORALE_DRIFT_STEP_PERMILLE`) as an uninterrupted run
(T-HS-DET-001) — `AdvanceHumanSystemsDay` is a pure function of its inputs with no cursor to diverge (draw-free,
KD-6). `DeriveCliques` still reports the (0,1) clique post-restore (derived from the restored pairwise store,
persisted nowhere — T-HS-CLQ-001). If the #22 view were being flowed, `SetPlayerEdgeMirror(0,1, 0.615f)` would
re-establish the mirror identically; at the minimal empty-view setting nothing is routed (KD-8).

## Appendix D — Worked example: behaviour-neutral identity (KD-8)

With all players `Create()`-seeded (morale 500, traits 10), every pairwise at
`RELATIONSHIP_NEUTRAL_PERMILLE = 500`, and a neutral committed input, one world day's advance leaves every
`MoralePermille = 500` (drift toward a 500 target is a no-op) and every pairwise unchanged; `DeriveCliques`
returns none (no pair `> 600`); `BuildHumanSystemsView` at the minimal empty-view setting emits nothing, so a
#22 world tick is **byte-identical** to one without #33 (`T-LW-U-035` green — T-HS-NEU-001). Registering #33
adds **no** RNG stream, so every existing stream's cursor is byte-identical (T-HS-NEU-003) — trivially, since
no #33 stream exists at the minimal tier.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial constant catalogue + worked examples (clique boundary, save/restore, behaviour-neutral identity). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (M/L): `CLIQUE_THRESHOLD_PERMILLE` retagged `[DERIVED]` (= 0.6 × PERMILLE_DENOM); neutral seeds unified to `[GT]`; Appendix B gains the mutuality note. |
#endregion
