# Tactical Presets & AI-Manager Selection Specification #26 — Section 1: Introduction, Scope, Dependencies

**Created:** July 8, 2026
**Last Updated:** July 11, 2026 (v0.3 — §1.6 T2/T4 engine-substrate gates closed)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/game-model-ai-manager-design.md` v0.4

---

## 1.1 Purpose and scope

**In scope:** the `TacticPreset` value type and `TacticPresetLibrary` in-code catalogue; preset →
`TeamTacticConfig`/`PlayerTacticConfig` projection; the manager-decision cadence gate; the kickoff
selection scoring function; the in-match adaptation ladder (score/time-driven preset re-selection);
`ManagerProfile` parameters; serialization of manager-AI state. **Out of scope (explicit
deferrals, §7):** opponent-tactic-aware adaptation (KD-5), the on-disk preset file format (KD-6),
event-triggered re-evaluation (KD-3 — no producers exist), and any UI/scouting exposure of an
opponent's preset. This spec is **additive on top of Spec #21** — it composes already-pinned #21
values and redesigns nothing beneath them (hard dependency, cited per the supplement's §6 step 3
requirement).

## 1.2 Convention inheritance

Project conventions verbatim. The manager layer touches no geometry; its cadence derives from
`MatchClock` tick counts (60 Hz frames / 10 Hz strides), never a third clock rate.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| Tactical Instructions #21 | this → it | **hard dependency**: `TeamTactic`/`PlayerTactic` value types; a preset is a named point in #21's parameter space (KD-7) |
| Match Engine (design note) | it ↔ this | hosts the decision gate + adaptation caller; provides `SetTeamTactic`/`SetPlayerTactic` (mid-match) and the boot appliers (kickoff) |
| Deterministic Sim #16 | it → this | manager-AI state serialization; `SNAPSHOT_SCHEMA_VERSION` bump at wiring |
| Code Standards #20 | governs | placement, tags, zero-alloc |

No dependency on #7/#8/#12–#15: the manager reads match-observable aggregates (score, clock), not
AI internals.

## 1.4 Key decisions

- **KD-1 — Preset = purely additive data; boot vs mid-match callers are distinct** (supplement
  KD-1, AR-1-corrected form): kickoff application projects the preset into
  `TeamTacticConfig`/`PlayerTacticConfig` and calls the *existing*
  `TeamTacticConfigApplier.Apply`/`PlayerTacticConfigApplier.Apply` (pre-kickoff-only per their own
  doc comments); mid-match adaptation calls `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly.
  No new match-engine writer seam. A preset **name** is authoring metadata and never enters the
  digest — only the applied tactic values do (the #21 v9/v10 serialization already covers them).
- **KD-2 — Coarse deterministic cadence** (supplement KD-2, AR-2-corrected form): decision points
  are kickoff, half-time, and every `MANAGER_DECISION_INTERVAL_TICKS` thereafter — all pure
  `MatchClock` tick-count derivations evaluated at the AI-stride boundary (so applied changes ride
  the existing FR-TI-027 stride commit). Event-triggered re-evaluation (goal, red card,
  substitution) is **deferred entirely**: `GoalAwardedEvent`/`CardIssuedEvent`/`SubstitutionEvent`
  have no producers (verified July 7, 2026 — only `PossessionChangedEvent` is wired), and building
  a consumer for them now is the phantom-interface class FR-LW-031/CLAUDE.md forbid.
- **KD-3 — Decision gate, not a clock** (design-note open question 2 resolved to the lighter
  option): no `ManagerDecisionClock` file; a per-team tick-count gate inside the match-engine
  composition root, mirroring the `IsAiStrideTick` pattern.
- **KD-4 — Default-neutral at the subsystem level** (supplement KD-3): the manager AI is
  per-team opt-in (`ManagerMode.Human = 0`, the zero-value identity — no selection, no adaptation,
  no calls). A default match is byte-identical to today.
- **KD-5 — Own-state-only scoring** (supplement KD-4): the scoring function consumes own score
  differential, time remaining, own current preset, and own `ManagerProfile` — never the
  opponent's `TeamTactic`/`PlayerTactic` or AI internals. This is the team-management analogue of
  the positional-behaviors perception-boundary invariant. Opponent-aware adaptation is a §7
  deferral (supplement open question 3 resolved: defer explicitly).
- **KD-6 — On-disk preset format deferred** (supplement KD-5): Stage 0+1 authors the catalogue in
  code; the disk loader is a pure parser swap producing the same `TacticPresetLibrary` (the
  `TeamTacticFileLoader`/`ScenarioIndex` D1 precedent).
- **KD-7 — Presets compose pinned values only** (design-note open question 1 resolved to the
  lean): a preset is a *named point* in #21's already-balance-passed parameter space; no new
  tunable surface, so §9 needs a shape/reference review of the catalogue, not a numeric-value
  sign-off beyond the `ManagerProfile`/threshold `[GT]`s this spec itself introduces.
- **KD-8 — No RNG** : selection and adaptation are pure functions of (profile, score, clock,
  current preset) with deterministic tiebreak (lowest preset ordinal wins); no draw site, no
  domain tag — same posture as #21 KD-6. Cross-manager variety comes from `ManagerProfile`
  parameters, not noise.

## 1.5 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Tactic value semantics | composes | #21 owns |
| 2 | Tactic application/commit | calls existing seams | match engine owns `SetTeamTactic`/stride commit |
| 3 | `[GT]` magnitudes inside presets | reuses pinned values | #21 G2 balance pass owns |
| 4 | Decision cadence | owns the gate predicate | `MatchClock` owns tick counts |
| 5 | Snapshot framing | owns manager-state block | #16 owns codec |
| 6 | Opponent modeling | none (deferred) | future spec |
| 7 | RNG | none | #16 |

## 1.6 T-phase staging (mirrors #21; supplement §3 table carried forward)

| Phase | Scope |
|---|---|
| T0 | `TacticPreset` + `TacticPresetLibrary` (in-code catalogue) |
| T1 | Preset → config projection + boot wiring via existing appliers |
| T2 | Decision gate in the composition root (KD-3). **Prerequisite gate (PASS-1 M-1):** the half-time trigger and `MATCH_TICKS_TOTAL` require the engine to model halves/match length — until then the gate fires kickoff + fixed interval only. **Gate CLOSED 2026-07-11** — the engine match-length model landed (`MatchEngineConstants.MATCH_TICKS_TOTAL`/`HALF_TIME_BOUNDARY_TICK`); the half-time trigger is active |
| T3 | Kickoff selection scoring (own-state-only) |
| T4 | In-match adaptation ladder via `SetTeamTactic`/`SetPlayerTactic`. **Prerequisite gate (PASS-1 M-1):** every `goalDiff ≠ 0` path requires engine score state, which does not exist until a goal-detection producer lands (§7.2's named first candidate); until then the ladder is exercised via test seams only. **Gate CLOSED 2026-07-11** — the Resolve-phase goal producer landed (v14 score state); the ladder runs on live inputs |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial section; supplement open questions 1–4 resolved (KD-3/KD-5/KD-7 + §7 UI deferral). |
| 0.2 | 2026-07-08 | — | PASS-1 M-1: §1.6 T2/T4 rows gain explicit engine-substrate prerequisite gates (score state; halves/match-length model). |
| 0.3 | 2026-07-11 | — | §1.6 T2/T4 prerequisite gates recorded CLOSED — the engine substrate landed (goal detection + match-length model); half-time trigger + live ladder inputs active. |
#endregion
