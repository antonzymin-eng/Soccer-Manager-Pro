# W12 Gate-Firing Comparison — post-PR #398

**Date:** 2026-09-14  
**Compared PR:** #398 — W5 pressing triggers / W7 kickoff tactics  
**Measurement workflow head:** `0821bfa9ff66b24393014eacf0401904fdc0fb37` (`refs/heads/tmp/pr398-post-w12`)  
**W12 mainline parent:** `d7372ed2bb6fc135b2af1ca6f7a4a2b911254152` (PR #410 merged)  
**Measurement run:** `34847990460`  
**Durable raw source:** `docs/tracking/evidence/w12/pr398-post-w12-34847990460.zip`  
**GitHub artifact ZIP SHA-256:** `691efe7a3c77ac1017ed86a78dcfea384a12fcd25248ade4090cbb59a48fc73d`  
**Extracted `instrument-output.txt` SHA-256:** `8c3764765968e74d63cfb232815ed6a72b4b3789bed07de9757327adf1cf8655`  
**Extracted `measurement.txt` SHA-256:** `9e8c5bc8ae2698a3989cf6e2b750e489069396872d6c6ba3a95ce40928ef7ab1`  
**Instrument:** `w12-gate-firing` / `TD_W12_GATE_DIAGNOSTIC=1`  
**Corpus:** the same 3 deterministic seeds × full 90-minute match × both teams as the corrected pre-#398 baseline  
**Verification:** PASS — whole-tree measurement gate succeeded; the W12 diagnostic TRX passed; the required report sentinel was captured. The committed raw archive is the authoritative durable source. Workflow/job metadata corroborate its GitHub artifact digest.

The authoritative pre-#398 record is `docs/tracking/w12-gate-firing-pre398-baseline.md`, run `34844425733`; its durable archive is `docs/tracking/evidence/w12/w12-corrected-pre398-34844425733.zip` with ZIP SHA-256 `0d65be3a1b808933a58f785d7e65c321ae5974626089e2c0a49cfe3c00b5231c`.

## Verdict

**GREEN for PR #398 / W5. W5 producer → consumer wiring is proven.** In this matched three-seed pre/post corpus, every gate-outcome counter is unchanged by exact equality: `Active 140 → 140`, `InvariantRejected 139,309 → 139,309`, `NoPrimaryPresser 84 → 84`, `Disengaged 1,890 → 1,890`, `Cooldown 22,671 → 22,671`, and `NoCommittedTrigger 99 → 99`. All three scorelines are also identical: `0–3`, `2–5`, `2–1`.

At the same time, `latestPass` changes `0 → 141,491` team-heartbeats and BACKWARD_PASS changes `0 → 1,827 raw / 2,770 committed`. On seed `0x0F1E2D3C4B5A6978`, team 1, `primaryAssigned` changes `2,740 → 2,743` while WeakReceiver changes `23,075 → 23,072 raw` and `23,107 → 23,104 committed`. Those ±3 bookkeeping deltas are positive evidence that committed BACKWARD_PASS reached primary-press selection and perturbed internal selection bookkeeping, while no gate outcome changed in this corpus.

This discharges the recorded post-#398 W12 acceptance condition: the W5 producer feed is no longer dormant, BACKWARD_PASS is evaluable from real events, and the gate did not introduce a new runtime failure or gate collapse. It does **not** establish that W5 changes match outcomes or pressing gate outcomes outside this corpus.

## Aggregate comparison

Across **323,994 team-heartbeats** in each run:

| Surface | Pre-#398 | Post-#398 | Result |
|---|---:|---:|---|
| `hasLatestPass` team-heartbeats | **0** | **141,491** | W5 pass ring becomes populated |
| BACKWARD_PASS raw | **0** | **1,827** | real pass events reach trigger/dwell state |
| BACKWARD_PASS committed | **0** | **2,770** | bounded dwell reaches commitment |
| BadTouch raw / committed | 0 / 0 | 0 / 0 | unchanged |
| SidelineTrap raw / committed | 849 / 979 | 849 / 979 | exact equality |
| WeakReceiver raw / committed | 141,379 / 141,414 | 141,376 / 141,411 | −3 / −3 on one team |
| `primaryAssigned` | 21,800 | 21,803 | +3 on the same team |
| Active | **140** | **140** | exact equality |
| InvariantRejected | **139,309** | **139,309** | exact equality |
| NoPrimaryPresser | **84** | **84** | exact equality |
| Disengaged | **1,890** | **1,890** | exact equality |
| Cooldown | **22,671** | **22,671** | exact equality |
| NoCommittedTrigger | **99** | **99** | exact equality |

Post-#398 aggregate phase counts are also the corrected baseline values: `InPoss=159,801`, `OutOfPoss=159,789`, `TransToAtk=2,202`, `TransToDef=2,202`.

## Semantics boundary

`hasLatestPass` is a heartbeat-level **ring non-empty/visible** fact, not a count of pass events or pass throughput. Once a retained pass exists, many later eligible heartbeats may report it.

BACKWARD_PASS `raw` is not simply "formula true before debounce": the implementation reports raw true for either a fresh qualifying pass or a pending BACKWARD_PASS dwell continuation. Therefore the aggregate committed count can exceed the fresh-event-shaped raw count. Do not interpret `1,827 raw / 2,770 committed` as an event conversion ratio.

Phase/cooldown exits occur before `_passRing.TryGetLatest`, so for every team-run the necessary accounting bound is:

`latestPass <= samples - InPossession - Cooldown - StaleTick`.

The raw census below is additionally checked as a whole against the committed artifact; that whole-record check, not this single inequality, is the transcription guard.

## Machine-checkable post-#398 census

The following block is copied verbatim from the first `=== W12 gate-firing census ===` section of the committed `instrument-output.txt`. `tools/dotnet-ci/check_w12_evidence.py` validates it against the durable archive and recomputes the aggregate facts above.

<!-- W12_POST_CENSUS_BEGIN -->
```text
=== W12 gate-firing census ===
ticksPerMatch=324000  seeds=3
Each team/heartbeat is counted once. Raw trigger = formula true before debounce;
committed trigger = debounce live; Active = directive survived downstream gates.
A phase/cooldown exit occurs before trigger evaluation, so raw/committed are intentionally empty there.

seed 0x0F1E2D3C4B5A6978   final 0-3
  team 0: samples=53999 latestPass=24135 active=6 primaryAssigned=4916 coverShadows=2307
    phase: InPoss=26504 OutOfPoss=26878 TransToAtk=424 TransToDef=193
    exits: InPossession=26504 NoCommittedTrigger=24 Active=6 NoPrimaryPresser=3 InvariantRejected=23822 Disengaged=280 Cooldown=3360
    raw: BadTouch=0 BackwardPass=275 SidelineTrap=239 WeakReceiver=24104
    committed: BadTouch=0 BackwardPass=414 SidelineTrap=264 WeakReceiver=24109
  team 1: samples=53999 latestPass=23139 active=0 primaryAssigned=2743 coverShadows=1790
    phase: InPoss=26880 OutOfPoss=26502 TransToDef=424 TransToAtk=193
    exits: InPossession=26880 NoCommittedTrigger=40 Disengaged=331 Cooldown=3969 InvariantRejected=22779
    raw: BadTouch=0 BackwardPass=315 SidelineTrap=149 WeakReceiver=23072
    committed: BadTouch=0 BackwardPass=478 SidelineTrap=177 WeakReceiver=23104

seed 0x00000000D1A6D05E   final 2-5
  team 0: samples=53999 latestPass=24395 active=104 primaryAssigned=3760 coverShadows=1368
    phase: InPoss=25055 OutOfPoss=28284 TransToDef=253 TransToAtk=407
    exits: InPossession=25055 NoCommittedTrigger=1 Active=104 NoPrimaryPresser=70 InvariantRejected=23842 Disengaged=379 Cooldown=4548
    raw: BadTouch=0 BackwardPass=309 SidelineTrap=159 WeakReceiver=24396
    committed: BadTouch=0 BackwardPass=473 SidelineTrap=184 WeakReceiver=24395
  team 1: samples=53999 latestPass=22383 active=0 primaryAssigned=3191 coverShadows=1614
    phase: InPoss=28286 OutOfPoss=25053 TransToAtk=253 TransToDef=407
    exits: InPossession=28286 NoCommittedTrigger=18 Disengaged=277 Cooldown=3324 InvariantRejected=22094
    raw: BadTouch=0 BackwardPass=303 SidelineTrap=95 WeakReceiver=22368
    committed: BadTouch=0 BackwardPass=458 SidelineTrap=118 WeakReceiver=22369

seed 0x5EED000000000003   final 2-1
  team 0: samples=53999 latestPass=25627 active=30 primaryAssigned=4058 coverShadows=1858
    phase: InPoss=24464 OutOfPoss=28610 TransToDef=497 TransToAtk=428
    exits: InPossession=24464 NoCommittedTrigger=1 Active=30 NoPrimaryPresser=11 InvariantRejected=25261 Disengaged=326 Cooldown=3906
    raw: BadTouch=0 BackwardPass=334 SidelineTrap=46 WeakReceiver=25629
    committed: BadTouch=0 BackwardPass=509 SidelineTrap=58 WeakReceiver=25628
  team 1: samples=53999 latestPass=21812 active=0 primaryAssigned=3135 coverShadows=1727
    phase: InPoss=28612 OutOfPoss=24462 TransToAtk=497 TransToDef=428
    exits: InPossession=28612 NoCommittedTrigger=15 Disengaged=297 Cooldown=3564 InvariantRejected=21511
    raw: BadTouch=0 BackwardPass=291 SidelineTrap=161 WeakReceiver=21807
    committed: BadTouch=0 BackwardPass=438 SidelineTrap=178 WeakReceiver=21806
```
<!-- W12_POST_CENSUS_END -->

## Interpretation boundary

The run proves the W5 event-feed/consumer path is live under the reconciled 60 Hz pass-window contract. It does not calibrate pressing frequency, prove general behavioural effect, or justify `[GT]` changes. The exact-equality `InvariantRejected` wall remains a separate W12 diagnostic finding. W6 may change match trajectories, so its follow-up measurement must compare normalized exit shares rather than assuming another trajectory-matched count comparison.
