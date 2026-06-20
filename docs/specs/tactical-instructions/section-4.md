# Tactical Instructions Specification #21 — Section 4: Architecture & Integration

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 4.1 Assembly placement

`TacticalDirector.TacticalInstructions` is an **infrastructure data assembly** at the bottom of the
reference graph (peer to `project-constants` in role): it references only `project-constants`; the five
consumers reference it. It contains **no game-loop logic** — only enums, structs, factories, and the
constant catalogue. The translation maps (§3.1) and the seam logic (§3.2–§3.5) live in the **consuming**
assemblies, not here. This keeps `Physics ← Mechanics ← AI` acyclic (FR-TI-002/003).

## 4.2 File layout (`src/tactical-instructions/`)

| File | Contents |
|---|---|
| `tactical-instructions.asmdef` | references `TacticalDirector.ProjectConstants` only; autoReferenced true |
| `TacticalInstructionsConstants.cs` | single catalogue (Appendix A) |
| `Mentality.cs` `Tempo.cs` `TacticWidth.cs` `TacticDefWidth.cs` `LineOfEngagement.cs` `TransitionPlan.cs` `GkDistributionPolicy.cs` `FocusPlay.cs` `TacticPassing.cs` `TacticPressing.cs` `TacticTriggerMask.cs` `TacticFormation.cs` `Duty.cs` `PlayerRole.cs` `InstrBias.cs` `SetPieceDutyFlags.cs` | one enum per file (#20 FILE NAMING) |
| `TeamTactic.cs` `PlayerInstructions.cs` `PlayerTactic.cs` | aggregate structs + factories |
| `Tests/` | `EnumOrdinalStabilityTests.cs`, factory-identity tests |

The seam maps are **not** here — they live with their consumers, e.g. `decision-tree/TacticTranslation.cs`,
`pressing-ai/TacticTranslation.cs`, etc. (each references this assembly downward).

## 4.3 Module responsibilities

| Module | Responsibility |
|---|---|
| Enums | the instruction vocabulary; APPEND-only ordinals |
| `TeamTactic`/`PlayerTactic`/`PlayerInstructions` | immutable-per-match input carriers + identity factories |
| `TacticalInstructionsConstants` | all multipliers/scalars/tables ([GT]) |
| consumer-side `TacticTranslation` (×5) | `Tactic*` → subsystem-local enum (translate-once, FR-TI-025) |
| match-engine Phase-D assembly layer | reads tactics, runs translation on change, populates routing fields |

## 4.4 Routing contract (KD-4 / FR-TI-024)

| Consumer | Delivery field(s) | Owner of the field |
|---|---|---|
| #8 Decision Tree | `TacticalContext` — replace the two `bool` stubs with resolved `TeamTactic` + per-agent `PlayerTactic` | #8 (existing struct, extended) |
| #12 Positioning | new width/role/duty fields on `PositioningPerceptionSnapshot` / `ContextModifierInputs` | #12 |
| #13 Pressing | new trigger-mask + line-of-engagement fields on `PressingSnapshot` | #13 |
| #14 Defensive | new man-mark-override + offside-toggle fields on `DefensiveSnapshot` | #14 |
| #15 Attacking | new style/overload/width fields on `AttackingSnapshot` | #15 |
| #11 Goalkeeper | `DistributeIntent` default seed | #11 |

All delivery fields carry **translated local enums** (FR-TI-025). The Phase-D assembly layer is the
single writer; subsystems read their own field types as they do today.

## 4.5 Interface contracts

No new interface is published (FR-TI-029). Delivery is by **plain struct fields** on existing snapshot
types (downward data flow, no abstraction). The only behavioural seam is the consumer-side translation
function + the `UtilityScorer`/`OptionGenerator` multiplier insertion (§3.3). Per the #20 event-vs-
interface tree, same-/downward-layer data flow uses direct fields, not interfaces.

## 4.6 Determinism & safety boundaries

- No RNG, no domain tag (FR-TI-026).
- In-match mutation gated to tactical-stride boundaries (FR-TI-027); the assembly layer holds the
  pending tactic and swaps it on `IsAiStrideTick`.
- Snapshot field set + ordering owned here (Appendix B), `SNAPSHOT_SCHEMA_VERSION` bump at T2 (FR-TI-028).
- All factories produce identity behaviour (FR-TI-031), so the layer is a safe no-op until populated.

## 4.7 Cross-spec validation checks (authored at Stage 1 alongside the seams)

1. `TacticPassing/Pressing/TriggerMask/Formation` map onto a valid subsystem enum (or clamp per F5).
2. `RoleWeightModifiers` default row = all 1.0.
3. `Balanced`/`Default` factories produce digests identical to the no-instruction baseline.
4. Man-mark override never breaches a #14 invariant (cascade re-run).
5. No `Tactic*` enum appears on a Mechanics hot path (grep gate).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Assembly placement, file layout, routing contract, determinism boundaries. |
#endregion
