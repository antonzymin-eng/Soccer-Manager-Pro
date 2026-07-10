# Tactical Presets & AI-Manager Selection Specification #26 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## 2.1 Functional Requirements

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-TP-001 | A `TacticPreset` is an immutable named bundle: `Name` + one `TeamTactic` + optional `PlayerTactic[]` (roster-indexed). Preset names are authoring metadata: never serialized into the world-state digest, never read by any AI tick. | MUST | KD-1 |
| FR-TP-002 | `TacticPresetLibrary` is a static in-code catalogue (Appendix A); Stage 0+1 has no disk format (parser-swap deferral). | MUST | KD-6 |
| FR-TP-003 | Every preset composes only #21 value types; a preset introduces no new tunable magnitude beyond selecting existing enum members / pinned dial values. | MUST | KD-7 |
| FR-TP-004 | Kickoff application: preset → `TeamTacticConfig`/`PlayerTacticConfig` projection → the existing `TeamTacticConfigApplier.Apply`/`PlayerTacticConfigApplier.Apply`, pre-kickoff only. | MUST | KD-1 |
| FR-TP-005 | Mid-match application: `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly; the boot appliers MUST NOT be called after kickoff. | MUST | KD-1 |
| FR-TP-006 | All manager-AI decisions evaluate only at decision points: kickoff, half-time, and every `MANAGER_DECISION_INTERVAL_TICKS` — derived from `MatchClock` tick counts at the AI-stride boundary; never per-tick, never event-triggered (deferral, KD-2). The half-time trigger is gated on the engine modelling halves (PASS-1 M-1) — the gate ships kickoff + interval first. | MUST | KD-2 / KD-3 |
| FR-TP-007 | `ManagerMode.Human = 0` (zero-value identity): no selection, no adaptation, no engine calls; a default match is byte-identical to pre-#26. `ManagerMode.AI = 1` opts a team in. | MUST | KD-4 |
| FR-TP-008 | The selection/adaptation scoring function consumes exactly: own score differential, time-remaining fraction, own current preset ordinal, own `ManagerProfile`. It MUST NOT read the opponent's `TeamTactic`, `PlayerTactic`, or any opposing AI internal state. | MUST | KD-5 |
| FR-TP-009 | Selection is deterministic: pure function + lowest-preset-ordinal tiebreak; no RNG draw site, no domain tag. | MUST | KD-8 |
| FR-TP-010 | Kickoff selection (T3) applies via the FR-TP-004 boot path; adaptation (T4) via the FR-TP-005 path; both changes commit at the stride boundary (existing FR-TI-027 machinery). | MUST | §3.3/§3.4 |
| FR-TP-011 | Adaptation hysteresis: a switched preset holds for ≥ `MANAGER_SWITCH_HOLD_INTERVALS` decision intervals before another switch is evaluable (no half-time exemption; the ladder saturates rather than churns). | MUST | §3.4 |
| FR-TP-012 | Per-team manager state (mode, profile ref, current preset ordinal, hold-intervals remaining, last-decision tick) is serialized with a `SNAPSHOT_SCHEMA_VERSION` bump at wiring; field order pinned in Appendix C. | MUST | #16 |
| FR-TP-013 | `ManagerMode` and preset ordinals are `byte`-backed, APPEND-only, ordinal-stability-tested; the preset catalogue is APPEND-only (a removed preset would dangle serialized ordinals). | MUST | #16 precedent |
| FR-TP-014 | `PlayerTactic[]` in a preset, when present, must be full-roster-length and validated at library construction (fail loud on length/null mismatch). | MUST | §2.2 |
| FR-TP-015 | All constants in a `TacticalPresetsConstants` catalogue (placement §4.1), one tag each. | MUST | #20 |
| FR-TP-016 | Every §3 formula has units, ranges, and a worked example. | MUST | CLAUDE.md |
| FR-TP-017 | No phantom interfaces: no opponent-model hook, no event-trigger subscription, no disk-loader interface until their prerequisites exist. | MUST | KD-2/KD-5/KD-6 |
| FR-TP-018 | The decision gate runs before the AI-stride tactic commit within the same tick, so a decision made at tick N is staged at N and committed at the same stride boundary the existing machinery uses. | MUST | §3.2 |
| FR-TP-019 | Half-time detection derives from engine-owned match-phase tick counts **once the engine models halves** (it does not today — PASS-1 M-1 gate); this spec adds no clock state of its own beyond `LastDecisionTick`. | MUST | KD-3 |
| FR-TP-020 | `ManagerProfile` `[GT]` parameters ship with named archetypes (Appendix A.2); archetype rows are data, validated by shape tests (monotone urgency, bounded thresholds). | MUST | §3.3 |

## 2.2 Data structures

### 2.2.1 `TacticPreset`

| Field | Type | Notes |
|---|---|---|
| Name | `string` | authoring metadata only (FR-TP-001) |
| Team | `TeamTactic` | #21 value type, as-is |
| Players | `PlayerTactic[]?` | optional; full roster length when present (FR-TP-014) |

### 2.2.2 `TacticPresetLibrary`

Static, ordered, APPEND-only catalogue; ordinal = array index = serialized identity + tiebreak
order. Stage-0+1 contents in Appendix A.1: `Balanced(0)`, `Possession(1)`, `Gegenpress(2)`,
`CounterAttack(3)`, `ParkTheBus(4)`.

### 2.2.3 `ManagerProfile` (per AI-managed team; `[GT]` archetype rows in Appendix A.2)

| Field | Type | Notes |
|---|---|---|
| Aggression | `float [0,1]` | scales the deficit-urgency term (§3.3) |
| Caution | `float [0,1]` | scales the lead-protection term |
| PatienceIntervals | `int ≥ 1` | multiplies `MANAGER_SWITCH_HOLD_INTERVALS` |

### 2.2.4 `ManagerState` (per team, persistent)

`Mode` (byte), `ProfileOrdinal` (byte), `CurrentPresetOrdinal` (byte), `HoldIntervalsRemaining`
(int), `LastDecisionTick` (int). Zero-init = Human mode = inert (KD-4 discipline).

## 2.3 Cross-spec back-props

None to approved specs at T0–T3 (this spec composes #21 without amending it). T4 serialization
(FR-TP-012) amends only the match-engine design note (not a numbered spec) plus the schema bump.

## 2.4 Failure modes

| F | Mode | Handling |
|---|---|---|
| F1 | Preset `Players` length ≠ roster size / null entry | fail loud at library construction (FR-TP-014) |
| F2 | Serialized preset/profile ordinal ≥ catalogue length | fail loud at restore seam |
| F3 | Boot applier invoked post-kickoff by the manager layer | guarded: adaptation path never references the appliers (mechanically auditable — no call site) |
| F4 | Non-finite profile float | refused at profile construction (NaN-gate) |
| F5 | Decision gate fires off-stride | impossible by construction (gate is evaluated inside the stride branch); locked by test |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FR set (20), data model, failure modes. |
| 0.2 | 2026-07-08 | — | PASS-1 M-1: FR-TP-006/019 carry the engine-substrate gates (halves model; score state). |
#endregion
