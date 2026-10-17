# W8 B measurement merge-gate closeout

**Created:** October 17, 2026
**Purpose:** Account for PR #511's 348 → 126 distribution change and observe every post-release
restricted spell, independently of the zero-sanction census.

## Lane and source control

This rerun uses the **Linux .NET Debug lane**, Unity shim, SDK 8.0.422 / CLR 8.0.28 and
NUnitLite 3.14. It uses the **exact frozen W8 instrument blob
`520ba31c71e06ed408fd5c282b5fd5611cf74879`** from the step-6 anchor, unchanged. A supplemental
fixture instantiates its Census and invokes its existing Observe/AfterTick/Finish methods.
The frozen source is already retained in `../../pre-b-anchor-3974b78-20261013/`.

Both arms use the six frozen seeds, normal constructor, `EnableGkHeading()`, no squad override,
shipping SlowDown and 324,000 ticks per match. Pre-law source is `e54335b0`; post-law source is
`511c235f`, with the same production behavior as the earlier `751b1143` capture. The only temporary
production overlay prints release/clearing observations; `observer-only.patch.xz` retains it, and it
is **not applied to shipping src**. One seed runs per process, with six independent processes in
parallel and NUnit workers disabled. The engine/EventBus is never shared across processes.

All twelve invocations pass. **All twelve terminal digests and distribution totals reproduce the
stored rehandling/per-match.csv exactly**, including the six post-law fingerprints. Every one of
the pre-law frozen report's **9,140 episode/transition/summary/count/exposure/discipline rows** also
matches the archived corrected-B common report in order. All 126 post-law hand-end identities
join their corresponding release/restriction starts by seed, keeper and frame. `control-agreement.json` records this.
These full-corpus equalities establish observer neutrality for the measured matches and agreement
with the earlier runtime captures, despite their runtime patch differences. This is direct NUnit,
not a new canonical VSTest invocation, Unity comparison or performance certification; final-head
GitHub CI remains a separate gate. The SDK/framework/assembly hashes and as-run compile sets are retained.

## Hand claims and count reconciliation

| Seed | Pre-law hand claims / distributions | Post-law hand claims / distributions | Pre-law same-keeper terminal hand returns | Other-claim net change |
|---|---:|---:|---:|---:|
| `0F1E2D3C4B5A6978` | 83 / 83 | 7 / 7 | 42 | -34 |
| `00000000D1A6D05E` | 70 / 70 | 20 / 20 | 29 | -21 |
| `0000000000000001` | 66 / 66 | 15 / 15 | 32 | -19 |
| `00000000ABCDEF12` | 50 / 50 | 22 / 22 | 23 | -5 |
| `0000000099887766` | 42 / 42 | 23 / 23 | 16 | -3 |
| `000000005A5A5A5A` | 37 / 37 | 39 / 39 | 13 | +15 |
| **Total** | **348 / 348** | **126 / 126** | **155** | **−67** |

Every observed hand claim reaches one canonical distribution CONTACT in each arm. The count drop
therefore is not an uncompleted-request or lost-distribution gap in this corpus.

The **222** fewer distributions can be partitioned descriptively into the disappearance of the
**155** tracked same-keeper terminal hand-return bucket and a **67** net reduction in all other
hand claims: `(348 − 155) − 126 = 67`. The earlier ~193 estimate is exactly the pre-law
non-return remainder `348 − 155`, not a predicted post-law count. This is bookkeeping across
changed matches, **not a causal estimate** that exactly 155 illegal claims were prevented and
exactly 67 events were caused by divergence. The old flight observer does not adjudicate every
incidental touch, and the post-law path changes later state and conditional RNG consumption.
No new RNG site, policy, trajectory or calibration value is introduced here.

For `0F1E2D3C4B5A6978`, the analogous reconciliation is **83 = 42 + 41**, versus **7 = 0 + 7**:
76 fewer claims, with a 42-return bucket difference and a 34-claim net remainder. The first hand
episode is identical: keeper 0 claims at frame 28,302 and distributes at 28,536. Pre-law, the same
keeper claims again at **28,542**, only six frames after release. Post-law, the restriction remains
until teammate 3 actually touches the ball at **28,736**, and there is no keeper re-hand claim in
that spell. The later trajectories diverge; the next post-law hand claim is keeper 11 at 124,506.
All seven post-law claims distribute, and all seven spells discharge on another player's touch
(six teammates, one opponent). The frozen observer's 90-minute exposure denominator does not
subtract hand-restricted frames and is not used to prove hand eligibility. This supports changed opportunity/loop exposure, rather than B silently failing
to execute the other 76 requests: those requests/hand claims do not occur in the post-law match.
The complete frozen traces retain the later claim times; no same-frame pairing is invented after
the arms have diverged.

## Restricted spells and opponent-touch aftermath

`spells.csv` retains **all 126 spells**, with release frame, keeper/team, exact clearing frame,
clearing reason and toucher identity/team. **114** end on teammate touch and **12** on opponent
touch. None ends on restart, substitution or run-end censoring; none remains stuck. Duration is
**0 / 127 / 507 frames** (min / median / max), or **0 / 2.117 / 8.45 seconds** at 60 Hz.
The two zero-frame spells have an actual opponent touch later in the release frame. This is a
**post-release hand restriction**, not hand-possession duration or C's eight-second clock.

For each opponent-touch end, count both teams' ShotExecutedEvent and GoalAwardedEvent ledger rows
from that frame through `touchFrame + 600`, inclusive. A same-frame shot is retained; this is a
frame-resolution temporal association, not a causal claim about the restriction. All twelve windows
have the full 600-frame follow-up. They contain **eight shots and six goals**, also eight/six unique
ledger events across their union (no double counting across overlapping windows). A goal need not
have a shot inside its window: an earlier shot or other ball contact can precede the clearing touch.
The restriction is already cleared by the opponent touch; these are not goals while hands remain
prohibited, nor evidence of a sanction restart.

| Seed | Keeper | Release frame | Opponent-touch frame | Restricted duration (frames) | Shots / goals in next 600 frames |
|---|---:|---:|---:|---:|---:|
| `0F1E2D3C4B5A6978` | 0 | 147726 | 147862 | 136 | 1 / 0 |
| `00000000D1A6D05E` | 0 | 150552 | 150701 | 149 | 1 / 1 |
| `00000000D1A6D05E` | 11 | 166242 | 166300 | 58 | 1 / 0 |
| `00000000ABCDEF12` | 11 | 24126 | 24126 | 0 | 0 / 0 |
| `00000000ABCDEF12` | 11 | 24378 | 24378 | 0 | 0 / 0 |
| `00000000ABCDEF12` | 11 | 24630 | 24763 | 133 | 0 / 1 |
| `00000000ABCDEF12` | 0 | 124920 | 125058 | 138 | 1 / 1 |
| `0000000099887766` | 0 | 19254 | 19724 | 470 | 0 / 0 |
| `0000000099887766` | 0 | 29184 | 29270 | 86 | 1 / 1 |
| `0000000099887766` | 11 | 145482 | 145662 | 180 | 1 / 1 |
| `0000000099887766` | 0 | 197814 | 198057 | 243 | 1 / 0 |
| `000000005A5A5A5A` | 11 | 124110 | 124236 | 126 | 1 / 1 |

Zero sanctions only means no sanction-restart follow-up population. It does **not** mean these
restricted-spell/opponent-touch windows are empty. No football-balance or safety verdict is inferred
from twelve selected windows; D-03 recovery tuning and general outside-area D-07 remain separate.

## Merge-gate disposition

The two accounting questions are answered for the frozen corpus. No hand-claim-to-execution loss,
unreleased restriction or new baseline mismatch is observed. The first divergence witness is the
intended own-release restriction, and all subsequent claim/request and touch-clear identities
reconcile. This evidence does not prove the absence of every possible goalkeeper defect or certify
balance. The post-law fingerprints remain the B→C candidate on this Linux lane, with adoption gated
by final documentation-head CI and PR #511 landing. Governing Unity evidence remains the separate
`e58577de` host run; no Unity rerun is claimed for this documentation-only closeout.

## Reproduction and inventory

`MeasurementTests.cs.txt` is the supplemental fixture. Restore the pinned arm sources/dependencies,
copy the exact frozen observer into the test compile set, apply the after-arm observation patch in
a scratch tree only, and build Debug without optimization. `prepare.py`, the four `.rsp.xz` files,
`run.py` and `provenance.json` retain the as-run paths, references and commands; adapt their scratch
paths on a different host. Run PairedFrozenCorpus with `TD_MEASURE_SEED=<16-digit seed>`, one seed
per process, then `python3 analyze.py` in this archive regenerates the CSVs and analysis JSON from
the twelve compressed NUnit XML reports and the earlier per-match.csv. XML test-case output is
authoritative; one console log ends before discovery output, while its complete XML passes and
retains every frozen observation. Keep both rather than inventing missing console output.

The two initial serial attempts were superseded after their loaded binaries were rebuilt. The
observed exit 139 is a diagnostic-harness error; none of their data enters this result. The twelve
accepted invocations use separately built immutable binaries and all exit 0.

Files include twelve raw console logs and twelve raw XML results (all xz-compressed), four compile
response files, fixture/scripts, observation patch, run/assembly/source provenance, build log,
claim and spell tables, analysis JSON and control-agreement JSON. Complete SHA-256 coverage belongs
to the parent W8 B evidence directory; no new integrity-registry entry is required.
