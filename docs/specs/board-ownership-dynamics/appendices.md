# Board & Ownership Dynamics #45 — Appendices

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #45 has no `[DERIVED]` and no `[EST]` constants, so neither region appears.
`[GT]` values are **illustrative pending the T3 balance pass** (§7.4 R-4) — the spec's contract is their
*shape and identity behaviour*, never their magnitude.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `BOARD_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The sub-blob's own version gate (KD-6). Independent of `SEASON_SAVE_FORMAT_VERSION` **and** of #30's `SEASON_STATE_FORMAT_VERSION` — bumping any one never implies the others (§4.6). |
| `BD_NOT_ADVANCED_SENTINEL` | `uint.MaxValue` | `[FIXED]` | The unadvanced cursor. **Not `0`** — day `0` is a legal world day, and a `0` sentinel silently no-ops a day-0 advance instead of failing loud (#33 FR-HS-008). |
| `BD_CONFIDENCE_NEUTRAL_PERMILLE` | `500` | `[FIXED]` | The neutral standing; the re-centring point in §3.2 and the pivot in §3.3. Fixed because the identity property depends on it equalling `BD_TRACK_NEUTRAL_PERMILLE`. |
| `BD_TRACK_NEUTRAL_PERMILLE` | `500` | `[FIXED]` | The neutral on-track reading. Same constraint. |
| `BD_DIAL_MAX` | `2000` | `[FIXED]` | The upper bound on every dial. **Fixed, not tunable** — it is what bounds the §3.3 product below `int` overflow (`500 · 2000 · 1000 = 1.0 × 10⁹`). Raising it is an arithmetic-safety change, not a balance change. |
| `DRAW_PURPOSE_TAKEOVER` | `0` | `[FIXED]` | The §3.5 purpose ordinal. **APPEND-only** — reordering re-keys every historical draw. |
| `DRAW_PURPOSE_RADIX` | `16` | `[FIXED]` | The §3.5 fixed radix. **Never "the current purpose count"** — a growing radix breaks cross-version replay parity the moment a purpose is appended. |
| `BD_CLUB_STRIDE` | `65536` | `[FIXED]` | The §3.5 club stride; bounds the club-id space the ordinal keeps injective. |
| `BOARD_STREAM_SITE_ID` | `"board.takeover"` | `[FIXED]` | The single subsystem-wide `RegisterStream` site id (FR-BD-022). |
| `BOARD_STREAM_VERSION` | `1` | `[FIXED]` | Bumping it re-keys the stream and changes every future takeover — a deliberate, digest-visible act. |

### A.2 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `BoardModifier`, `BoardModifier.Identity` (`1000`) | #40 `club-finances-economy` §2.2 | Consumed, never re-declared (FR-BD-017). |
| `ClubId` | #27 | The keying identity. |
| `_RESERVED_0x2D_`, `SubsystemOrdinals.BoardOwnership` (95) | #16 §3.4 | `[CROSS-PENDING]` until the T3 promotion (§8.2). |

### A.3 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `BD_CONFIDENCE_DRIFT_STEP_PERMILLE` | `20` | Per-day drift step — ~25 days from neutral to an extreme at identity patience. |
| `BD_MORALE_WEIGHT_PERMILLE` | `0` | **Zero at minimal** — the deep-tier #33 input contributes exactly nothing until T3 (§3.2 identity). |
| `BD_BUDGET_SENSITIVITY_PERMILLE` | `0`, bounded `[0, 1000]` | **Zero at minimal** — this is what makes the `BoardModifier` projection exactly `Identity` for *every* confidence value, not merely at neutral (§3.3 / FR-BD-019). Turning it on is T3's named activation. **The `[0,1000]` bound is load-bearing, not cosmetic:** §3.3's overflow argument is stated at `sensitivity ≤ 1000`, so the bound is what that argument rests on. It is enforced at the consuming seam (F1) like every other dial. |
| `BD_BUDGET_MULT_MIN` / `_MAX` | `700` / `1300` | Clamp on the projected multiplier — ±30% at the extremes. |
| `BD_BAND_CRITICAL_MAX` | `200` | Band edges, half-open ascending (§3.4). |
| `BD_BAND_INSECURE_MAX` | `450` | |
| `BD_BAND_STABLE_MAX` | `750` | |
| `BD_BUDGET_ADVANCE_US` | `5` | §6.3 ceiling for one club's daily advance. A **ceiling, not a measurement** — no certified number exists for #45 (§6.3). |
| `BD_BUDGET_SEASON_PROJECTION_US` | `5` | §6.3 ceiling for one club's boundary projection. Same caveat. |

**Why the two zero-valued `[GT]` dials are `[GT]` and not `[FIXED]`.** They are `0` *today* because the
minimal tier is the identity, and they are exactly the dials T3 turns on — a `[DERIVED]` or `[FIXED]`
tag would assert a designer must never set them, which is the opposite of their purpose. Their being
zero is a **staging decision**, not a physical constraint.

**Consequence, stated plainly:** #45's behaviour-neutrality (KD-8) holds **at these defaults**. A config
that raises `BD_BUDGET_SENSITIVITY_PERMILLE` deliberately leaves the identity tier — that is the
intended deep-tier behaviour, not a violation, and §5's identity tests run at defaults.

## Appendix B — Save sub-blob layout (KD-6)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** —
the outer codec sees a length-prefixed byte block and never parses it (FR-BD-027).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `BOARD_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below is interpreted (F3). |
| 2 | `ClubCount` | `i32` | Length prefix — read through the overflow-safe bound compared against `total − offset` (F5; the `MatchSaveCodec` hardening). |
| 3 | per club × `ClubCount` | — | `ClubId` (`i32`), then `ConfidencePermille` (`i32`), `LastAdvancedWorldDay` (`u32`), then `OwnershipType` (`u8`) + the three dials (`i32` each), then `TakeoverState` (`u32` + `i32`). |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F5). |

Clubs are written in **ascending `ClubId` order** so the blob is a function of state, not of insertion
order.

**Deliberately absent — three things, each for its own reason:**

1. **Any `RngStreamState` / cursor** (FR-BD-028). KD-2's keyed ordinal makes a takeover roll a pure
   function of `(worldSeed, clubId, worldDay)`, all of which are already in the world or the blob.
2. **Any copy of #30's `BoardObjective`** (FR-BD-014). Caching it here for convenience is precisely how
   the double truth KD-5 removed would come back — and it would come back *silently*, since the copy
   would only diverge after a restore. **This is the warning at the point of temptation (§7.4 R-2).**
3. **Any copy of #40's budget.** Same reasoning, other neighbour.

**APPEND-only** (FR-BD-030). New fields — including the deep tier's takeover history — go at the **end**
with a `BOARD_SAVE_FORMAT_VERSION` bump; inserting mid-block shifts every subsequent offset.

## Appendix C — Job-security band table (§3.4)

| Band | Confidence range | Reading |
|---|---|---|
| `Critical` | `[0, 200)` | dismissal imminent |
| `Insecure` | `[200, 450)` | under pressure |
| `Stable` | `[450, 750)` | secure in post |
| `Secure` | `[750, 1000]` | strongly backed |

Ranges are **half-open and ascending**, exhaustive over `[0,1000]`, with each boundary belonging to the
**upper** band (`200 → Insecure`). #30 owns what a band *means* for the sacking decision (KD-3); this
table only fixes the mapping, so the two specs cannot disagree about where an edge falls.

**Not tabulated: a worked takeover example.** Takeover outcomes are the output of a SipHash-keyed draw
and are not hand-computable; a table here would be fabricated. They are pinned **relationally** instead
(T-BD-DET-004 position-independence, T-BD-DET-005 ordinal injectivity), which is mechanically checkable
without knowing a single drawn number. If a golden takeover sequence is wanted for regression, it is
captured at T3 from a real run on the pinned host and recorded as evidence — never authored here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial appendices (A.1 Fixed incl. the overflow-bounding `BD_DIAL_MAX` rationale, A.2 Cross, A.3 GT with the why-these-are-GT-not-FIXED note and the identity-at-defaults consequence; B save layout with the three deliberately-absent fields and the R-2 warning at the point of temptation; C the band table + the no-fabricated-takeover rationale). Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | PASS-1 fix (M): `BD_BUDGET_ADVANCE_US` / `BD_BUDGET_SEASON_PROJECTION_US` were declared `[GT]` in §6.3 but **absent from the Appendix A catalogue**, which is meant to be the single catalogue — added to A.3 with their ceiling-not-measurement caveat. |
#endregion
