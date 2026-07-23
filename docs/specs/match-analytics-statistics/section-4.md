# Match Analytics & Statistics #37 — Section 4: Architecture

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 4.1 Assembly placement

New presentation-layer assembly **`TacticalDirector.MatchAnalytics`** (`src/match-analytics/`).

**References (the `match-viewer` reference set):**
- `TacticalDirector.EventSystem` — the #17 Tier A event structs (read-only payload types).
- `TacticalDirector.MatchEngine` — the observation surface (`BallView`/`AgentView`/`AgentTeamId`/
  `PossessingAgentId`/`CurrentTick`) + the new read-only ledger tap (§4.3).

**Referenced by:** the UI layer (#38) only. **No sim assembly may reference
`TacticalDirector.MatchAnalytics`** (KD-4 — the presentation-clean, one-directional invariant; the
build enforces it by the absence of the reference, exactly as `match-viewer` is unreferenced by sim).

## 4.2 File layout (proposed)

```
src/match-analytics/
├── match-analytics.asmdef
├── MatchStatline.cs             // KD-4 per-team view model
├── AdvancedStatline.cs          // territorial % + xG-availability + heatmap bins
├── StatPoint.cs                 // (team,x,y) map entry
├── MatchAnalyticsResult.cs      // the full per-match result
├── XgLocationModel.cs           // KD-2 pure model (shape)
├── MatchAnalyticsAggregator.cs  // KD-3 core
├── MatchAnalyticsConstants.cs   // [GT]/[FIXED] catalogue (Appendix A)
└── Tests/
    ├── match-analytics-tests.asmdef
    ├── XgLocationModelTests.cs
    ├── MatchAnalyticsAggregatorTests.cs
    └── MatchAnalyticsObserverNeutralityTests.cs
```

## 4.3 The read-only per-tick ledger tap (KD-7)

`MatchEngine` gains one read-only observation accessor exposing the **current tick's** Tier A records
as read-only copies — the same *kind* of surface `match-viewer` added at `MatchEngine` v1.24
(`BallView`/`AgentView`). Contract:
- read-only: it returns value-type copies; it cannot mutate the ledger, the ring buffer, or the digest;
- current-tick scoped: it exposes only the records the engine drained this tick (the tap is pulled
  after `DrainTick`, before the next tick overwrites the ring — the `match-viewer` per-tick sample
  cadence);
- neither an event nor a producer — so the "no new engine event or producer" bar holds.

This is the one small `MatchEngine` addition #37 lands; like `BallView`, it carries no behaviour change
(a match not being observed is byte-identical to one being observed — the observer-neutrality lock,
FR-AN-017). Its exact signature is fixed at T1 against the then-current `EventLedger` drain seam; the
spec pins only the read-only / current-tick / copy-out contract.

## 4.4 Consumption modes (one core, two callers)

The `MatchAnalyticsAggregator` core (KD-3) is caller-agnostic, but its **event consumption is every-tick
and lossless** (§3.5 — a dropped tick loses that tick's records); only the positional sample may stride:
- **Live HUD** (`match-viewer`-style): `ObserveTick` is pumped once per engine tick (lossless events),
  and the HUD calls `Build()` per render frame (idempotent, §3.5). This is why #37 uses a per-tick pump,
  not the wall-clock frame-dropping sample `match-viewer`'s *visual* replay tolerates.
- **End-of-match report** (#38): the same aggregator pumped every tick across the match, `Build()` once
  at full-time.

Both are the same code path over the same live taps — there is no separate "post-match reader" (there
is no ledger reader to build one on, §1.4).

## 4.5 Determinism & naming (KD-5)

#37 registers **no** RNG stream, **no** `DOMAIN_TAG_*`, **no** `SubsystemOrdinal` — it draws nothing and
appears nowhere in the #16 §3.4 catalogue (the `match-viewer` class). This is a positive property, not a
deferred allocation: there is nothing to reserve and no `_RESERVED_` placeholder is warranted.

## 4.6 CS0104 hazard (name collision)

`MatchStatline`/`AdvancedStatline` are new names; a grep of `docs/specs/**` + `src/**` at T0 MUST
confirm no existing type shares them before the assembly is wired (the `TacticTranslation`/
`PlayerAttributes` CS0104 precedent — five `TacticTranslation` types once coexisted in match-engine
scope). If a future spec brings a same-named type into a shared scope, fully-qualify from line one
(the KD-P6 discipline).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial architecture: assembly placement + reference direction, the KD-7 read-only ledger tap contract, consumption modes, no-RNG/tag/ordinal, CS0104 note. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
