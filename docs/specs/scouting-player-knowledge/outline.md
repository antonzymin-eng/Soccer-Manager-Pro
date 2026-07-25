# Scouting & Player Knowledge #32 — Outline

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial, promoted from design supplement v0.3)
**Version:** 0.1
**Status:** APPROVED

---

## Purpose

**Player knowledge as a per-manager VIEW**: scout assignments, attribute masking / fog-of-war (the
manager sees attribute *ranges*, not truths, until a player is scouted), scout reports, and
recommendations, advanced on the world tick and persisted alongside the season/career save. The
governing invariant (roadmap §5): knowledge is a **VIEW over #27's true attributes and NEVER a
mutation of them**. The minimal tier is the **omniscient identity** (fog off — every read is exact,
draw-free, byte-neutral); the Stage-3 deep tier narrows the **same view seam** into ranges that
tighten with scouting effort and #34 scout quality, on **one code path**.

## Section map

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, out-of-scope seams, dependencies, key decisions (KD-1..KD-8) |
| 2 | Functional requirements (FR-SC-001..027), data structures, failure modes (F1..F6) |
| 3 | Core algorithms: band resolution, estimate derivation, keyed-ordinal derivation, assignment lifecycle, ranking |
| 4 | Architecture, assembly/file layout, the view seam, save composition |
| 5 | Test plan (view-not-mutation + identity + invariants + save/determinism + fail-loud) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and T-phase plan (T0–T3) |
| 8 | References and cross-spec cross-references (XC-032-*) |
| 9 | Approval checklist |
| Appendices | Constant catalogue, save-block layout, worked estimate example |

## Governing decisions (see §1)

- **KD-1** — the overlay stores a **knowledge band** per scouted player; per-attribute `[Min,Max]`
  ranges are **derived on read** (band → `[GT]` half-width table; stateless keyed noise re-centre),
  never stored; maximal knowledge collapses to `[truth, truth]` arithmetically. Freshness is the
  **live-form window** semantic (width is the scouted quantity; the centre tracks current truth).
- **KD-2** — the view boundary is read-only **by construction** (`in PlayerRecord` value copies, no
  storage reference, readonly view types); **own-squad omniscience** (managed-club players always
  `BAND_MAX`); identity facts + `WeakFootRating` exact at any band.
- **KD-3** — deep accuracy draws are **position-independent keyed draws** on `(playerId, band,
  attrIdx, purpose)` (fixed-radix ordinal, deliberately NOT `worldDay`); views mutate no RNG state;
  a zero-width estimate makes **no RNG call**, so the minimal tier is draw-free and
  `_RESERVED_0x24_`/86 stays reserved at approval.
- **KD-4** — #34 scout quality scales assignment **speed** (`DaysPerBand`) only, never estimate
  widths; #32 defines `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000`, closing #34's open baseline.
- **KD-5** — recommendation/search is #32's **own pure read-only ranking**; #32 issues no offers —
  the manager acts via #31's `SubmitBid`, unchanged.
- **KD-6** — one `SCOUTING_SAVE_FORMAT_VERSION` season-save sub-blob (**not** the plan's
  `WORLD_STORE_FORMAT_VERSION` bump — an argued revision); knowledge is durable career state; the
  re-key/retirement hygiene rule (drop-on-roster-event, fail-loud on unresolvable ids).
- **KD-7** — `AssignScout`/`CancelAssignment` are manager commands; progress accrues at #30's **new
  tick-order slot 7** (ERR-030-007, reserve-ahead, empty at minimal); managed-manager scope.
- **KD-8** — one code path: `fogEnabled` off ⇒ every read is the `BAND_MAX` row of the same tables
  the deep tier uses; a fog-off season is byte-identical to pre-#32.

## Back-props

- **At approval:** one — the #30 scouting tick-order null-seam slot (ERR-030-007). `0x24`/86 stays
  reserved (draw-free minimal); #34/#31/#27/#38/#16 unchanged.
- **At T-phase (deferred):** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the roster-event
  hygiene hook consumption when #31's roster-commit (ERR-030-005) lands (T3); the #16
  `DOMAIN_TAG_SCOUTING = 0x24` promotion at the first accuracy draw (T3).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial outline, promoted from design supplement v0.3 (AR-converged). Status IN REVIEW. |
#endregion
