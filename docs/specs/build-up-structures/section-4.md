# Scripted Build-Up Structures Specification #24 — Section 4: Architecture and File Layout

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 4.1 Placement (no new assembly)

| File | Assembly | Role |
|---|---|---|
| `src/positioning-ai/BuildUpZoneClassifier.cs` | `TacticalDirector.PositioningAI` | §3.1 classifier + hysteresis |
| `src/positioning-ai/BuildUpZoneState.cs` | `TacticalDirector.PositioningAI` | §2.2.2 per-team state |
| `src/positioning-ai/BuildUpOverlayCatalogue.cs` | `TacticalDirector.PositioningAI` | Appendix A tables as `[GT]` data |
| `src/positioning-ai/SlotComposer.cs` (edit) | `TacticalDirector.PositioningAI` | §3.2 overlay stage |
| `src/tactical-instructions/BuildUpStructure.cs` | `TacticalDirector.TacticalInstructions` | §2.2.1 enum (with the #21 back-prop) |

## 4.2 Pipeline position (coordinated with #23)

```
anchor → offset → ContextModifier → **build-up overlay (§3.2)** → spacing → [#23 dismark offset] → pitch clamp → lines → lanes
```

The overlay precedes spacing (structure proposes a shape; spacing resolves conflicts inside it),
while #23's dismark offset follows spacing (an individual evasion nudge). If both specs land, this
combined order is the pinned contract; whichever implements second cites the first's Appendix and
adds a shared stage-order test.

## 4.3 Routing contract

Same shape as #21 T2/Phase-D and #23 §4.3: `SetTeamTactic` stages the dial; the Phase-D writer is
the sole populator of `PositioningPerceptionSnapshot.BuildUpStructure`; the possession-changed
consumer that arms the §3.3 window lives in the match-engine composition root (where the existing
possession-changed producer/consumer pair already runs) and writes `SuppressTicksRemaining` into
the per-team state before the #12 tick. New `TestOnly_BuildUpStructure(teamId)` +
`TestOnly_BuildUpSuppressTicks(teamId)` seams at wiring.

## 4.4 Interface contracts

None new (FR-BU-016). The classifier is internal to #12's tick; the catalogue is data.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial architecture; combined #23/#24 stage order pinned. |
#endregion
