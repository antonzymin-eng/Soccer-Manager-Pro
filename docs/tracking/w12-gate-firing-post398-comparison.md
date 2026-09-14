# W12 Gate-Firing Comparison — post-PR #398

**Date:** 2026-09-14  
**Compared PR:** #398 — W5 pressing triggers / W7 kickoff tactics  
**Measured commit:** `ed4ce66d40a75d844bb6b4924ce71750df718c23`  
**W12 mainline parent:** `d7372ed2bb6fc135b2af1ca6f7a4a2b911254152` (PR #410 merged)  
**Measurement run:** `34847990460`  
**Artifact:** `pr398-post-w12-34847990460` (`sha256:691efe7a3c77ac1017ed86a78dcfea384a12fcd25248ade4090cbb59a48fc73d`)  
**Instrument:** `w12-gate-firing` / `TD_W12_GATE_DIAGNOSTIC=1`  
**Corpus:** the same 3 deterministic seeds × full 90-minute match × both teams as the corrected pre-#398 baseline  
**Verification:** PASS — the measurement job's whole-tree gate completed successfully with 0 build errors / 0 warnings, the diagnostic TRX recorded a passing W12 test, and the required report sentinel was captured.

The authoritative pre-#398 record is `docs/tracking/w12-gate-firing-pre398-baseline.md` (run `34844425733`, artifact digest `sha256:0d65be3a1b808933a58f785d7e65c321ae5974626089e2c0a49cfe3c00b5231c`).

## Verdict

**GREEN for PR #398 / W5.** The exact condition that was dark before W5 is now observably live on every seed and both teams. The pass-event feed is no longer dormant, BACKWARD_PASS is evaluated from real events under the reconciled 60 Hz `[N-AI_PHASE_STRIDE,N)` contract, and the full gate completed without a new runtime failure.

This discharges the recorded post-#398 W12 merge gate. W7 is covered by the same reconciled build/test gate; it does not alter the W12 pass-feed acceptance condition. Per the wiring backlog sequence, **W6 is next after #398 merges.**

## Aggregate comparison

Across **323,994 team-heartbeats** in both runs:

| Surface | Pre-#398 | Post-#398 | Result |
|---|---:|---:|---|
| Latest pass visible to Pressing AI | **0** | **158,912** | W5 pass feed live |
| BACKWARD_PASS raw | **0** | **1,113** | real pass events reach the raw gate |
| BACKWARD_PASS committed | **0** | **1,926** | bounded discrete-event dwell reaches commitment |
| BadTouch raw / committed | 0 / 0 | 0 / 0 | independent pre-existing dark signal; unchanged by W5 |
| SidelineTrap raw / committed | 849 / 979 | 2,183 / 2,861 | live |
| WeakReceiver raw / committed | 141,379 / 141,414 | 135,334 / 135,294 | live |
| Active press directive | 140 | 5,698 | live; absolute count allowed to move because #398 changes pressing behaviour |
| InvariantRejected | 139,309 | 127,622 | still large; remains a W12 follow-up finding, not a W5 blocker |
| NoPrimaryPresser | 84 | 482 | remains a W12 follow-up finding, not a W5 blocker |

Post-#398 aggregate phase counts: `InPoss=160,162`, `OutOfPoss=160,150`, `TransToAtk=1,841`, `TransToDef=1,841`. Exit counts: `InPossession=160,162`, `Cooldown=27,282`, `Disengaged=1,670`, `NoCommittedTrigger=1,078`, `NoPrimaryPresser=482`, `InvariantRejected=127,622`, `Active=5,698`.

## Per-seed evidence

### Seed `0x0F1E2D3C4B5A6978` — final 0–1

- Team 0: samples 53,999; latestPass 35,574; active 0; primaryAssigned 1,475; coverShadows 782. Phase: InPoss 29,176; OutOfPoss 24,262; TransToAtk 204; TransToDef 357. Exits: InPossession 29,176; NoCommittedTrigger 253; Active 0; NoPrimaryPresser 4; InvariantRejected 20,403; Disengaged 358; Cooldown 3,805. Raw: BadTouch 0; BackwardPass 278; SidelineTrap 119; WeakReceiver 20,721. Committed: BadTouch 0; BackwardPass 254; SidelineTrap 212; WeakReceiver 20,711.
- Team 1: samples 53,999; latestPass 16,449; active 0; primaryAssigned 618; coverShadows 185. Phase: InPoss 24,264; OutOfPoss 29,174; TransToDef 204; TransToAtk 357. Exits: InPossession 24,264; NoCommittedTrigger 642; Active 0; NoPrimaryPresser 19; InvariantRejected 24,168; Disengaged 449; Cooldown 4,457. Raw: BadTouch 0; BackwardPass 24; SidelineTrap 328; WeakReceiver 24,440. Committed: BadTouch 0; BackwardPass 152; SidelineTrap 569; WeakReceiver 24,434.

### Seed `0x00000000D1A6D05E` — final 5–4

- Team 0: samples 53,999; latestPass 28,033; active 5,659; primaryAssigned 12,251; coverShadows 6,349. Phase: InPoss 27,458; OutOfPoss 25,841; TransToDef 335; TransToAtk 365. Exits: InPossession 27,458; NoCommittedTrigger 15; Active 5,659; NoPrimaryPresser 6; InvariantRejected 16,059; Disengaged 178; Cooldown 4,624. Raw: BadTouch 0; BackwardPass 77; SidelineTrap 663; WeakReceiver 21,922. Committed: BadTouch 0; BackwardPass 496; SidelineTrap 745; WeakReceiver 21,916.
- Team 1: samples 53,999; latestPass 25,413; active 0; primaryAssigned 2,436; coverShadows 923. Phase: InPoss 25,843; OutOfPoss 27,456; TransToAtk 335; TransToDef 365. Exits: InPossession 25,843; NoCommittedTrigger 14; Active 0; NoPrimaryPresser 73; InvariantRejected 23,358; Disengaged 243; Cooldown 4,468. Raw: BadTouch 0; BackwardPass 503; SidelineTrap 247; WeakReceiver 23,721. Committed: BadTouch 0; BackwardPass 581; SidelineTrap 274; WeakReceiver 23,713.

### Seed `0x5EED000000000003` — final 0–2

- Team 0: samples 53,999; latestPass 26,952; active 4; primaryAssigned 5,064; coverShadows 2,574. Phase: InPoss 26,890; OutOfPoss 26,529; TransToAtk 241; TransToDef 339. Exits: InPossession 26,890; NoCommittedTrigger 138; Active 4; NoPrimaryPresser 72; InvariantRejected 21,227; Disengaged 234; Cooldown 5,434. Raw: BadTouch 0; BackwardPass 206; SidelineTrap 545; WeakReceiver 22,068. Committed: BadTouch 0; BackwardPass 161; SidelineTrap 769; WeakReceiver 22,063.
- Team 1: samples 53,999; latestPass 26,491; active 35; primaryAssigned 4,930; coverShadows 3,617. Phase: InPoss 26,531; OutOfPoss 26,888; TransToDef 241; TransToAtk 339. Exits: InPossession 26,531; NoCommittedTrigger 16; Active 35; NoPrimaryPresser 308; InvariantRejected 22,407; Disengaged 208; Cooldown 4,494. Raw: BadTouch 0; BackwardPass 25; SidelineTrap 281; WeakReceiver 22,462. Committed: BadTouch 0; BackwardPass 282; SidelineTrap 292; WeakReceiver 22,457.

## Interpretation boundary

This run proves W5's event feed and gate are live; it does **not** calibrate pressing frequency. `BadTouch=0`, the large invariant-rejection population, the enlarged no-primary population, and remaining team/seed asymmetries stay diagnostic findings for later investigation. They must not be converted into `[GT]` changes while KD-W1 remains in force.
