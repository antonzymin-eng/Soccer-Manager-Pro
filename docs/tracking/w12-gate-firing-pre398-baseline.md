# W12 Gate-Firing Baseline — pre-PR #398

**Date:** 2026-09-13  
**Main baseline:** `561cf899e3008796daaee57af5b69b762a94c157` (PR #409 merged)  
**Instrumented commit:** `fab74021216819308d468b42604e72c2b3f58994`  
**Measurement run:** `34801691641`  
**Artifact:** `measurement-34801691641` (`sha256:362f7d7e6c01588e681a4865ace78e329fb5f7ac4ef6e92986fb61abe68e2d7e`)  
**Instrument:** `w12-gate-firing` / `TD_W12_GATE_DIAGNOSTIC=1`  
**Corpus:** 3 deterministic seeds × full 90-minute match × both teams  
**Verification:** canonical measurement lane PASS; passing TRX + required W12 report sentinel.

This is the required **pre-#398** runtime baseline. PR #398 is deliberately absent. W12 is observation-only: its state is not serialized and no gameplay decision reads it.

## Aggregate census

Across **323,994 team-heartbeats**:

| Surface | Count |
|---|---:|
| Latest pass visible to Pressing AI | **0** |
| Active press directive | **224** |
| Primary presser assigned at the diagnostic exit | 21,800 |
| Cover shadows selected at the diagnostic exit | 10,666 |
| Phase: `InPoss` | 159,801 |
| Phase: `OutOfPoss` | 159,789 |
| Phase: `TransToAtk` | 2,202 |
| Phase: `TransToDef` | 2,202 |
| Exit: `InPossession` | 159,801 |
| Exit: `Cooldown` | 22,671 |
| Exit: `Disengaged` | 1,890 |
| Exit: `NoCommittedTrigger` | 99 |
| Exit: `InvariantRejected` | **139,309** |
| Exit: `Active` | **224** |

Trigger counts:

| Trigger | Raw | Committed |
|---|---:|---:|
| `BadTouch` | **0** | **0** |
| `BackwardPass` | **0** | **0** |
| `SidelineTrap` | 849 | 979 |
| `WeakReceiver` | 141,379 | 141,414 |

## Per-seed evidence

### Seed `0x0F1E2D3C4B5A6978` — final 0–3

- Team 0: samples 53,999; latestPass 0; active 9; primaryAssigned 4,916; coverShadows 2,307. Phase: InPoss 26,504; OutOfPoss 26,878; TransToAtk 424; TransToDef 193. Exits: InPossession 26,504; NoCommittedTrigger 24; Active 9; InvariantRejected 23,822; Disengaged 280; Cooldown 3,360. Raw: BadTouch 0; BackwardPass 0; SidelineTrap 239; WeakReceiver 24,104. Committed: BadTouch 0; BackwardPass 0; SidelineTrap 264; WeakReceiver 24,109.
- Team 1: samples 53,999; latestPass 0; active 0; primaryAssigned 2,740; coverShadows 1,790. Phase: InPoss 26,880; OutOfPoss 26,502; TransToDef 424; TransToAtk 193. Exits: InPossession 26,880; NoCommittedTrigger 40; Disengaged 331; Cooldown 3,969; InvariantRejected 22,779. Raw: BadTouch 0; BackwardPass 0; SidelineTrap 149; WeakReceiver 23,075. Committed: BadTouch 0; BackwardPass 0; SidelineTrap 177; WeakReceiver 23,107.

### Seed `0x00000000D1A6D05E` — final 2–5

- Team 0: samples 53,999; latestPass 0; active 174; primaryAssigned 3,760; coverShadows 1,370. Phase: InPoss 25,055; OutOfPoss 28,284; TransToDef 253; TransToAtk 407. Exits: InPossession 25,055; NoCommittedTrigger 1; Active 174; InvariantRejected 23,842; Disengaged 379; Cooldown 4,548. Raw: BadTouch 0; BackwardPass 0; SidelineTrap 159; WeakReceiver 24,396. Committed: BadTouch 0; BackwardPass 0; SidelineTrap 184; WeakReceiver 24,395.
- Team 1: samples 53,999; latestPass 0; active 0; primaryAssigned 3,191; coverShadows 1,614. Phase: InPoss 28,286; OutOfPoss 25,053; TransToAtk 253; TransToDef 407. Exits: InPossession 28,286; NoCommittedTrigger 18; Disengaged 277; Cooldown 3,324; InvariantRejected 22,094. Raw: BadTouch 0; BackwardPass 0; SidelineTrap 95; WeakReceiver 22,368. Committed: BadTouch 0; BackwardPass 0; SidelineTrap 118; WeakReceiver 22,369.

### Seed `0x5EED000000000003` — final 2–1

- Team 0: samples 53,999; latestPass 0; active 41; primaryAssigned 4,058; coverShadows 1,858. Phase: InPoss 24,464; OutOfPoss 28,610; TransToDef 497; TransToAtk 428. Exits: InPossession 24,464; NoCommittedTrigger 1; Active 41; InvariantRejected 25,261; Disengaged 326; Cooldown 3,906. Raw: BadTouch 0; BackwardPass 0; SidelineTrap 46; WeakReceiver 25,629. Committed: BadTouch 0; BackwardPass 0; SidelineTrap 58; WeakReceiver 25,628.
- Team 1: samples 53,999; latestPass 0; active 0; primaryAssigned 3,135; coverShadows 1,727. Phase: InPoss 28,612; OutOfPoss 24,462; TransToAtk 497; TransToDef 428. Exits: InPossession 28,612; NoCommittedTrigger 15; Disengaged 297; Cooldown 3,564; InvariantRejected 21,511. Raw: BadTouch 0; BackwardPass 0; SidelineTrap 161; WeakReceiver 21,807. Committed: BadTouch 0; BackwardPass 0; SidelineTrap 178; WeakReceiver 21,806.

## Baseline findings

1. **The pre-#398 pass producer path is completely dark at Pressing AI.** `latestPass=0` for every sampled heartbeat, so `BackwardPass=0` raw/committed is upstream dormancy, not evidence that its formula is too strict. This is the principal pre/post W5 comparison target.
2. **`BadTouch` is independently dormant in the representative corpus.** It is zero raw and committed even though it does not depend on the missing pass-event feed. W5/PR #398 is not expected to repair that signal.
3. **The dominant suppression point is downstream of trigger commitment.** `InvariantRejected=139,309` versus only `Active=224`; the WeakReceiver trigger is live on ~141k heartbeats. This is a W12 finding, not a calibration instruction.
4. **Active pressing is strongly side-asymmetric in this corpus.** Team 0 produces all 224 active directives; Team 1 produces zero across all three seeds. This is recorded as a follow-up finding; do not infer a root cause from the census alone.

## Post-#398 comparison contract

After PR #398 is reconciled onto the W12/static-sweep mainline, rerun this exact instrument and corpus. The comparison is green for the W5 portion only if the pass feed becomes observably live (`latestPass > 0`) and the backward-pass trigger can be evaluated from real events without a new runtime failure or pathological gate collapse. Match outcomes and absolute trigger counts are allowed to move because #398 intentionally changes live pressing/kickoff behaviour. The independent `BadTouch=0`, large invariant-rejection population, and team-side asymmetry remain tracked findings unless the post-run supplies direct evidence explaining them.
