# Tactical Presets & AI-Manager Selection Specification #26 — Section 4: Architecture and File Layout

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 4.1 Placement (per the supplement's §6 step 6, made explicit here per FR-CS structural-decision discipline)

| File | Assembly | Role |
|---|---|---|
| `src/tactical-instructions/TacticPreset.cs` | `TacticalDirector.TacticalInstructions` | §2.2.1 value type — a pure data layer over that assembly's own types; a new assembly for named bundles would be disproportionate |
| `src/tactical-instructions/TacticPresetLibrary.cs` | `TacticalDirector.TacticalInstructions` | §2.2.2 catalogue (Appendix A.1) |
| `src/tactical-instructions/TacticalPresetsConstants.cs` | `TacticalDirector.TacticalInstructions` | §3.5 constants + Appendix A.2/A.3 `[GT]` tables |
| `src/match-engine/ManagerProfile.cs` | match-engine | §2.2.3 |
| `src/match-engine/ManagerState.cs` | match-engine | §2.2.4 |
| `src/match-engine/ManagerDecisionGate.cs` | match-engine | §3.2 gate (KD-3: a gate, not a clock file) |
| `src/match-engine/ManagerAdaptation.cs` | match-engine | §3.3/§3.4 scoring + ladder + apply calls |

Split rationale: preset **data** sits with the #21 types it composes (no new reference edge);
manager **logic** sits in the composition root because it is the layer that already owns
`SetTeamTactic`/`SetPlayerTactic` and the boot appliers — putting logic in `tactical-instructions`
would force that bottom-layer assembly to reference upward (forbidden by FR-TI-003's direction).

## 4.2 Call-path contract

```
Boot:      TacticPresetLibrary[k] → Project (FM-TP-01) → TeamTacticConfigApplier.Apply / PlayerTacticConfigApplier.Apply
Mid-match: ManagerAdaptation → MatchEngine.SetTeamTactic / SetPlayerTactic  (never the appliers — F3)
Commit:    both paths ride the existing FR-TI-027 stride-boundary commit
```

## 4.3 Test seams

`TestOnly_ManagerState(teamId)` (mode, current ordinal, hold, last tick) at wiring; the existing
`TestOnly_Mentality`/tactic seams already observe the applied values end-to-end.

## 4.4 Interface contracts

None new (FR-TP-017). The disk loader (KD-6) will be a parser producing `TacticPresetLibrary`
contents — a construction-time input swap, not an interface this spec pre-declares.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial architecture; data-with-#21 / logic-with-composition-root split rationale recorded. |
#endregion
