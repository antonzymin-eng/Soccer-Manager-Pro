# #440 Stage A evidence — observer neutrality and census complete

Created November 10, 2026. This observation-only investigation measures main
`d4b026aa951432d342217314b8c4d49b53a19e7d`; #434 remains an unmerged independent
draft. Measured observer/analysis freeze: `15b110d19b6dcb22f1d35d314858cf3b69987030`.
No gameplay fix, new approved parameter/policy, ERR, RNG draw or save-schema change.
See [design v1.4](../../w-440-ball-loose-design.md#proposed-stage-b-experiment--owner-approval-required).

**Review follow-up: proposal only.** C is a measured experiment with proposed 25%
ceilings for immediate original-tackler recovery and rapid original-carrier return
in frames f+1..f+24. Only pooled results gate; team/seed and the former f+1..f+3
slice are descriptive. A censored time-to-first-regain distribution extends through
f+60. The proposed pooled coverage floor is 30 complete windows per gate, with no
team quota; the fixed sample may end inconclusive. The design fixes all-episode denominators, censor-completion
bounds, minimum coverage and stop/report/no-tuning rules before any new result.
The proposed next experiment is 24 runs on an explicitly selected v29 base containing
#543; none has begun. A static admission/save/off-tick audit makes schema-neutrality
conditional on the fence being zero at every supported save/off-tick boundary; if
it can affect a future consumer, serialize it and bump the next free world schema.
Symmetric lockout is fallback only, requiring a separate owner decision. The Stage A
corpus, definitions, observer and historical results below are unchanged.

## Measured results

All six enabled/disabled pairs pass every raw digest, complete trajectory/BallState,
team/pass-flight witness, geometry/phase and ordered canonical ledger comparison.
All disabled streams also match the independently measured main C0 lane from #434.
There are **60 exact stream checks**, 12 receipted full matches, 324000 scheduled
calls per match (323999 live frames). Every observed raw preimage/previous link is
independently validated, with normal nonempty BuildHash retained in both modes.

The independently replayed journal reconciles **all 37 BallLoose episodes** to the
engine counter and every frame-end possessor. **37/37 regain the original carrier
within the tackle frame through passive pickup; all 37 end that frame with the
carrier.** There are no intervening same-frame teammate/opponent acquisitions.
At the fixed one-second endpoint: **30 loose / 5 original carrier / 2 opponent /
0 teammate / 0 censored**. Across the observed prefixes there are **12 opponent
recoveries and 0 teammate recoveries**. These event-prefix metrics differ from the
endpoint bins by design. All six matches together have **16021 changed-holder edges**
(including releases), **146 shots / 47 goals / 48 fouls**; team attribution follows
the frozen rules, not credited-scorer inference.

| Seed | BallLoose | Same-frame carrier regain | Dispossessions | Horizon loose / carrier / opponent |
| --- | ---: | ---: | ---: | --- |
| `0000000000000001` | 6 | 6 | 3 | 4 / 0 / 2 |
| `000000005A5A5A5A` | 8 | 8 | 6 | 6 / 2 / 0 |
| `0000000099887766` | 7 | 7 | 0 | 7 / 0 / 0 |
| `00000000ABCDEF12` | 6 | 6 | 0 | 4 / 2 / 0 |
| `00000000D1A6D05E` | 7 | 7 | 2 | 6 / 1 / 0 |
| `0F1E2D3C4B5A6978` | 3 | 3 | 1 | 3 / 0 / 0 |


All 37 release edges retain the tackle position and have exactly zero recorded
velocity. `ReleaseBallControl` clears velocity; `RunLooseBallPickup` then selects
the nearest eligible player, including the old carrier. The source-order pickup
markers support H1 on this corpus; H2 first-touch and H3 hidden same-frame other-
player acquisition do not explain any of these 37 regains. Later opponent recovery
does occur, so a tackle-frame conclusion must not be extrapolated to the horizon.
These are mechanical findings on the neutral corpus, not causal scoring estimates.

`analysis/episodes.csv`, `per-match-team.csv`, `audit.json` and `kinematics.json`
retain every episode/seed/team, the independent replay checks and exact copied
kinematics. Every full chronology and all owner transitions remain in the captures.
No definitions, seeds, horizons or favorable cases were selected after measurement.

## Inputs, retention and interruption

`capture-inputs.tar.xz` preserves the exact as-run files/manifest; `design-note-as-run.md.txt`
preserves the preregistered note. `analysis-freeze.json` was committed before the
full run. `prepared.json` pins the source/binary/runtime and local instrument inputs.
The shared trace reader is retained in source control and changes compression only.

The scheduler stopped on workspace capacity after seven exit-0 receipts. One
concurrently completed output had no captured exit receipt; it is retained separately
in `unreceipted-at-interruption.tar.xz` and is **not** an accepted primary run. After
packing successful outputs and removing generated caches, `resume.py` verified the
unchanged pins, reused only the seven receipted successes and ran five jobs: the
unreceipted mode again plus four unstarted jobs. All five exited 0. Original/recovery
commands, timestamps, the outer exception and the unknown exit are retained; the
unknown exit is not fabricated. Total processes started: 13; accepted full runs: 12.
No capture/observer or measurement definition changed to recover storage.

`retained/results/` contains all 12 completed stores and metadata. Every packed
`traces.tar.xz` has an archive SHA and raw per-stream byte counts/hashes. Integer
word-delta/shuffle compression is lossless, including every float bit and event byte;
NumPy is optional and the portable fallback is identical. All raw bytes were checked
before deleting gzip containers; their original container hashes/sizes are retained.
The unreceipted archive preserves its original gzip containers verbatim.

## Reproduction

Use SDK 8.0.426 / CLR 8.0.32, the recorded Debug/development settings and frozen six
seeds. For a new run, prepare from the measured observer SHA and run the ordinary
wrapper in a fresh directory; the recovery wrapper is only for the recorded interrupted
workspace. Restore #434's `retained/control` tree for the independent main cross-check.

```bash
python3 docs/tracking/evidence/ball-loose-stage-a-20261110/prepare.py --repo "$PWD" --work-dir /tmp/w440-capture --head 15b110d19b6dcb22f1d35d314858cf3b69987030 --dotnet /path/to/dotnet --packages /path/to/nuget-cache
python3 docs/tracking/evidence/ball-loose-stage-a-20261110/run.py --work-dir /tmp/w440-capture --main-control /path/to/formation-duty/retained/control
python3 docs/tracking/evidence/ball-loose-stage-a-20261110/analyze.py --work-dir /tmp/w440-capture --output-dir /tmp/w440-analysis
```

For reanalysis of this package, materialize its LFS payloads, copy `PASS.json` to
`retained/PASS.json` if needed and use `retained/` as the analysis work directory.
Both direct gzip and indexed packed stores are supported by the shared reader.

## Validation and pending host

The exact command `bash tools/run-tests-local.sh --pr` passes: all 37 assembly
console summaries total **0 failed / 4487 passed / 225 existing skips**. MatchEngine
reports verbatim **Failed: 0, Passed: 960, Skipped: 16, Total: 976**. Quarantine is
empty. The same three baseline inconclusives (EventSystem boot golden, ShotMechanics
BM007 and MatchEngine connected-tackle sample) are outside console totals; TRX
retains them. **Zero tests are newly skipped, quarantined or disabled.** The new
collector fixture passes 20/20 without environment-gated Ignore/Explicit. Release
build and reflection verify the delegate/callback/copied event/kind are absent
without development/editor/test symbols. The unchanged 60-second hook timed out on
cold work; later commits use a disclosed command-scoped bypass, not a passing hook.

Pinned Unity 6000.4.9f1 validation remains pending. The final PR body names the exact
checkout SHA. Force full compile/reimport and run unfiltered EditMode
`TacticalDirector.MatchEngine.Tests`, especially `BallLooseEpisodeObserverTests` and
existing tackle/possession/restart fixtures. Retain Editor.log, XML, exact source,
counts and the existing skip/inconclusive reasons. This Linux evidence does not
certify Unity or replace the host request. Draft PR only; Stage B waits for the
owner's mechanism, proposed parameter and scoped no-same-frame-regain policy decision.

CSV report containers use LF newlines in Git; their cell values are unchanged from
the analyzer output. Binary trace streams retain their original exact byte hashes.
