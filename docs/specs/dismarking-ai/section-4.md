# Dismarking & Marker-Awareness AI Specification #23 — Section 4: Architecture and File Layout

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 4.1 Placement decision (no new assembly)

Per §1.4 KD-7 and the design supplement's §6 step 8, this spec adds files to **existing**
assemblies; it introduces no `src/dismarking-ai/` tree and no new asmdef:

| File | Assembly | Role |
|---|---|---|
| `src/positioning-ai/MarkingPressureEvaluator.cs` | `TacticalDirector.PositioningAI` | §3.1/§3.2 pure evaluator + dwell update (mirrors `RestDefenseEvaluator.cs`) |
| `src/positioning-ai/MarkingDwellState.cs` | `TacticalDirector.PositioningAI` | §2.2.1 per-agent state struct |
| `src/positioning-ai/SlotComposer.cs` (edit) | `TacticalDirector.PositioningAI` | §3.3 offset stage insertion |
| `src/decision-tree/UtilityScorer.cs` (edit) | `TacticalDirector.DecisionTree` | §3.4 penalty multiplier |
| `src/tactical-instructions/DismarkIntensity.cs` | `TacticalDirector.TacticalInstructions` | §2.2.2 enum (lands with the #21 back-prop) |

New constants join `PositioningAIConstants.cs` (`#region GT`) and #8's `TacticalWeights` per
FR-DM-016.

## 4.2 Pipeline position

`SlotComposer` stage order after this spec (insertion in **bold**):

```
anchor → offset → ContextModifier → [#24 build-up overlay] → spacing → **dismark offset (§3.3)** → pitch clamp → lines → lanes
```

This is the combined order pinned in Build-Up Structures #24 §4.2 (the overlay precedes spacing;
this spec's evasion nudge follows it) — whichever spec implements second cites the first and adds
the shared stage-order test (PASS-1 L-3). The stage is a no-op unless `DismarkIntensity ≠ Off` **and** phase is `InPoss` **and** the agent is
an eligible off-ball outfielder with pressure above the floor (FR-DM-006/007/009).

## 4.3 Routing contract

Identical in shape to the #21 T2/Phase-D pattern:

1. Manager sets `TeamTactic.DismarkIntensity` (staged via `SetTeamTactic`, committed at the AI-stride
   boundary, FR-TI-027).
2. The match-engine Phase-D writer routes the active value into
   `PositioningPerceptionSnapshot.DismarkIntensity` and `TacticalContext.DismarkIntensity` — the sole
   populator (FR-DM-015).
3. Consumers read their local routing field; no consumer references `TeamTactic` directly.
4. New `TestOnly_DismarkIntensity(teamId)` seam + `MatchEngineTacticTests` routing case at wiring,
   mirroring `TestOnly_MarkingOrientation`.

## 4.4 Perception access

`MarkingPressureEvaluator` receives the agent's `FilteredView` by value from the same per-agent
loop that already feeds #8 — it does not call into `PerceptionSystem` itself. This keeps the
evaluator pure (KD-2) and makes FR-DM-001 mechanically auditable (the function signature admits no
other opponent-data source). Per the §3.2 PASS-1 M-1 contract, this pass runs **after** #12 in the
stride order, so the offset stage reads the previous stride's pressure — the match-engine writer
carries it into the next tick's `PositioningPerceptionSnapshot` build, keeping #12 itself free of
any `FilteredView` dependency.

## 4.5 Interface contracts

No new interfaces (FR-DM-018). The marker-side reaction ("marker notices the dismark and
re-tightens") is #14's future concern, deferred in §7.2 — writing a hook for it now would be the
phantom-interface class ERR-001/ERR-004 prohibit.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial architecture: placement table, pipeline position, routing contract. |
| 0.2 | 2026-07-08 | — | PASS-1: combined #23/#24 stage order cross-cited (L-3); §4.4 records the one-stride staleness routing (M-1). |
#endregion
