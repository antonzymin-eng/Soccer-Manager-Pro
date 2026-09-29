# Static unread serialized/snapshot field sweep

**Date:** 2026-09-13  
**Branch:** `wiring/w12-gate-firing-diagnostic`  
**Authoritative sweep commit:** `b63eb3c68a4ae2b70d776fb6b660b8763bd80a96`  
**Workflow run:** `34803051927`  
**Artifact:** `w12-static-unread-field-sweep-34803051927`  
**Artifact digest:** `sha256:c1e3526987793b8d736fad3f52e983982a8d10d483215a4ebbcb0f44bdef94f6`  
**Tool:** `tools/unread_serialized_field_sweep.py`  
**Regression suite:** `tools/tests/test_unread_serialized_field_sweep.py` — PASS

This is the separate static sweep required before reconciling PR #398. It is intentionally distinct from W12: W12 measures runtime gate firing, while this pass looks for state that is populated/carried but has no production behavioral consumer.

## Semantics

The sweep is a conservative lexical candidate generator, not compiler proof. It scans production C# only and covers:

- Unity `[SerializeField]` fields; Unity serialization counts as a write/activation surface.
- Snapshot/state/save/config carrier fields.
- Cross-file object-initializer and common receiver/member-array writes.
- Same-name fields are bound to the lexical receiver type where it can be resolved, preventing another type's `.HasBall` read from falsely clearing `DefensiveAgentSnapshot.HasBall`.

Test-only reads do not count. Reads inside serialization, capture, restore, save/load, codec, and equivalent transport paths are reported as **transport-only** and deliberately do not clear a finding. That preserves the class of defect where a value is written, persisted, and restored but never influences gameplay.

## Result

The authoritative run reports **105 candidates**:

- **10 high-signal `snapshot/state-carrier` candidates** with writes and **zero behavioral or transport reads**.
- **95 `transport-only` candidates** that are written and carried through persistence/state-transfer paths but for which this lexical pass found no non-transport consumer.
- **0 Unity-serialized candidates** survived the read analysis.

### High-signal zero-read candidates

| Field | Writer evidence | Status |
|---|---|---|
| `DefensiveAgentSnapshot.HasBall` | `MatchEngine.cs:4095` | **Known C10 confirmed** — populated and not consumed by defensive assignment logic. |
| `DefensiveSnapshot.PossessionOwnerEntityId` | `MatchEngine.cs:4034` | New static candidate; triage separately. |
| `RngStreamState.DrawIndex` | `DeterministicRngService.cs:96`, `RngStreamState.cs:59` | Static candidate; likely state/metadata semantics require manual type-aware review before declaring dormant. |
| `RngStreamState.SiteId` | `DeterministicRngService.cs:65` | Static candidate; manual review required. |
| `RngStreamState.StreamVersion` | `DeterministicRngService.cs:68` | Static candidate; manual review required. |
| `RngStreamState.EntityId` | `DeterministicRngService.cs:67` | Static candidate; manual review required. |
| `PositioningPerceptionSnapshot.ActiveOutfieldCount` | `MatchEngine.cs:3923` | New static candidate; triage separately. |
| `PressingAgentSnapshot.HasBall` | `MatchEngine.cs:3997` | New pressing-input candidate; trigger logic currently identifies the carrier through `BallCarrierEntityId`. |
| `PressingSnapshot.BallVelocity` | `MatchEngine.cs:3959` | New pressing-input candidate; triage separately. |
| `PressingSnapshot.PossessionTeamId` | `MatchEngine.cs:3970` | New pressing-input candidate; triage separately. |

The full 105-row report is retained in the run artifact rather than copied into tracking documentation.

## Interpretation / gate effect

This sweep is **diagnostic, not a merge blocker for PR #398 by itself**. It confirms that the repository contains field-level dormancy that method/assembly call-graph checks cannot see, including the already-documented C10 case. The 95 transport-only rows are review leads, not proof of defects; snapshot containers are expected to carry many fields across capture/restore boundaries.

For the current work sequence, the sweep requirement is therefore satisfied once this artifact and triage record are preserved. No sweep finding is being fixed opportunistically before PR #398 reconciliation. New candidates should be filed/handled in their owning wiring or architecture workstream unless the post-#398 W12 comparison directly implicates one.
