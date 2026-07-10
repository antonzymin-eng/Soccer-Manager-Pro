# Positional Rotations Specification #25 — Section 4: Architecture and File Layout

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Placement (no new assembly)

| File | Assembly | Role |
|---|---|---|
| `src/positioning-ai/RotationController.cs` | `TacticalDirector.PositioningAI` | §3.1–§3.4 controller |
| `src/positioning-ai/RotationPairState.cs` | `TacticalDirector.PositioningAI` | §2.2.2 state |
| `src/positioning-ai/RotationAdjacencyCatalogue.cs` | `TacticalDirector.PositioningAI` | Appendix A `[GT]` tables |
| `src/tactical-instructions/RotationFreedom.cs` | `TacticalDirector.TacticalInstructions` | §2.2.1 enum (with the #21 back-prop) |

## 4.2 Tick position and the target-feedback subtlety

```
phase classification → **RotationController (§3.2)** → SlotComposer (anchor…clamp) → ShapeAnalyzer (lines/lanes)
```

The §3.1 predicate consumes composed targets, but the controller runs *before* `SlotComposer` — so
it uses the **previous heartbeat's** composed targets, held in a controller-owned per-agent
`LastComposedTarget` cache (`Vector2` × roster) written at the end of every #12 tick (PASS-1 H-1:
`AgentPositioningData` carries no composed-target field, so the cache is new state this spec
owns). The one-tick-stale read is deliberate: it avoids a same-tick circular dependency (compose →
maybe swap → recompose), costs at most one heartbeat of trigger latency (absorbed by the 5-tick
dwell anyway), and keeps the pipeline single-pass. Boot populates the cache from
`SeedFromFormation`'s initial compose. **The cache is serialized state** (Appendix B): restore
loads it verbatim — a re-seed on restore would evaluate post-restore triggers against baseline
targets instead of the actual previous tick's contextual targets and break FR-RO-013/T-RO-DET-003
byte-identity.

## 4.3 Routing contract

`SetTeamTactic` stages `RotationFreedom`; the Phase-D writer solely populates
`PositioningPerceptionSnapshot.RotationFreedom`; new `TestOnly_SlotBinding(teamId, agentId)` +
`TestOnly_RotationPairState(teamId, row)` seams at wiring; `MatchEngineTacticTests` routing cases
per team.

## 4.4 #12 contract amendment

`AgentPositioningData.SlotIndex` is today assigned once by `SeedFromFormation`. This spec amends
that contract (ERR back-prop, §2.4): the field remains #12-owned, and the `RotationController` is
its **sole** post-seed writer — an explicit single-writer rule so no future system casually swaps
bindings (the hazard class the supplement's KD-4 flagged).

## 4.5 Interface contracts

None new (FR-RO-018).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial architecture; the previous-tick-target read is the load-bearing design note here. |
| 0.2 | 2026-07-08 | — | PASS-1 H-1: previous-tick targets are a controller-owned SERIALIZED `LastComposedTarget` cache — the v0.1 claim that `AgentPositioningData` stored them was false, and the v0.1 restore re-seed broke byte-identity. |
#endregion
