# #434 formation/Duty evidence

Created November 9, 2026; closeout November 10. This package compares main
`d4b026aa951432d342217314b8c4d49b53a19e7d` with runtime implementation
`23f2fedb140d47b22be36b2d495190b0b1685251`. Later commits add tests, tooling and
tracking only; the production-source identity receipt records that relationship.
The approved contract is [the preflight](../../w-434-formation-duty-preflight.md).

## Scope and interpretation

Formation locks before physics frame 1 and remains fixed. Complete tactic requests
still commit at tactical strides; requested/effective formation are separate.
Duty moves each outfield player's canonical anchor using the existing −3/0/+3 m
catalogue. No RNG, tuning, lineup selection or in-match formation switching changes.

The F442-selected lineup remains mapped by roster index. In particular LM becomes
DM/DM1, RM becomes LW/CAM, and one striker becomes RAM in F4231. The owner accepted
this limitation. Neutral-attribute captures characterize geometry and composition;
they do not measure specialist suitability. Real-squad identity/restore fixtures
cover the unchanged selected IDs. Formation-aware selection and runtime switching
remain explicit follow-ups.

C0 requires exact covered-byte identity. C1–C9 have hard correctness/integrity stops
and descriptive football metrics; no numeric realism acceptance bands were approved.
All cases, seeds and both teams are reported without tuning or favorable selection.
There is no full-match non-default-formation/non-Support combination; unit replay
covers that interaction, as agreed before capture.

## Capture and acceptance

| Check | Result |
| --- | --- |
| Historical frozen B reproduction | All 12 full runs reproduce the six historical terminal fingerprints; original analysis passes. |
| Base reuse eligibility | Pinned source audit + all 120 checks in 36 short probes pass. |
| C0 observer off/on and base/head comparison | All 90 exact stream comparisons pass over six seeds; 24 full runs. |
| Full matrix | 90 unique primary observed matches audited; 29,160,000 primary calls. |
| Geometry/state integrity | Finite full trajectories, valid ownership and existing #12 active-outfield slot bounds pass. |
| Attempts outside primary 90 | 12 extra plain C0 runs, 12 historical runs, 36 probes; preliminary v1.0 attempts retained separately. |

World-state v29 necessarily changes raw digest preimages. The comparator validates
literal schemas, initialized/effective metadata and row counts, then includes every
common byte exactly once. Default overhead is 18 bytes; one/two F4231 teams add
27/36 bytes. Independently reconstructed raw snapshot chains and each arm's own
nonempty BuildHash remain intact. No shipping hash or restore identity is bypassed.

The mutation proof uses a real event-containing snapshot: every one-bit perturbation
at every base/head payload byte must change common output or reject excluded
metadata. This is additional to synthetic checks, not a claim based on field names.

## Characterization

The primary scoring result is per **changed team**, not combined match goals.
Each entry compares that team with itself in its paired base arm. In particular,
Duty base arms already contain that Duty's existing non-positioning effects; do
not replace them with C0's Support baseline.

| Change | Changed home team: six-match goals base → head | Changed away team: six-match goals base → head |
| --- | ---: | ---: |
| 4-3-3 | 20 → 53 | 27 → 64 |
| 4-2-3-1 | 20 → 71 | 27 → 39 |
| All-Attack Duty | 34 → 68 | 26 → 57 |
| All-Defend Duty | 24 → 5 | 31 → 9 |

The changed home side scores **19 goals** in C1, seed `0000000099887766`.
These are large aggregate advantages for formation/Attack changes and sharp
suppression for Defend, not proof of strict dominance. For example, the changed
away side in C8, seed `000000005A5A5A5A`, goes from winning 6–4 to losing 4–7.
The 71 versus 39 F4231 home/away totals are descriptive: six paired matches per
case do not establish a systematic home/away effect or identify its cause.
No tuning or acceptance bands were changed. [The named blocking issue](../../open-issues.md#formation-duty-player-control-blocker)
blocks player-facing formation/Duty controls until marking movement is fixed and
validated. Preset/API reachability remains an expressly accepted integration risk;
this documentation does not implement a runtime feature gate.

**Diagnosis hold:** no new positional-exposure, shot-slot attribution or causal
trace analysis is run here. Wait for Opus's diagnosis design and then use its single
frozen definition set, actual positions and open-play possession, separating formation
and Duty cases. Distance greater than 3 m must not be called “unmarked”. The scoring
table above is a summary of the existing CSV, not that deferred diagnosis.

Full combined shot/goal/foul totals remain in `analysis/aggregate.json` as supporting
data, rather than the headline. See `analysis/per-match-team.csv` for every seed, arm and team, including explicitly
labeled reused C0 references, controlled/loose and pass-flight possession frames,
four AI phases and outfield slot extents/minimum pair distance. `paired-deltas.csv`
retains all paired team differences. `audit.json` records stream hashes and hard
correctness checks. Goals use ScoringTeam, shots TeamId and fouls the offender team.
Scheduled and live denominators remain distinct.

The instrument retains full BallState and ownership on every frame. No independent
numeric stalled-play definition was frozen in the capture protocol; stationary-ball
occupancy must not be called a stall. This package does not claim such a metric.

## Reproduction and retained bytes

Use SDK 8.0.426 / CLR 8.0.32, Debug, unchanged JIT defaults and the recorded generated
shim settings. The exact as-run tools and their original manifest are preserved in
`capture-inputs.tar.xz` (extract before inspecting/replaying); `prepared.json` pins their hashes, source archives and binaries.
The commands/exits are in each stage's `runs.json`, with runner logs and summaries
in `retained/`. Source commits can be rebuilt with `prepare.py` in a new workspace.
Historical reproduction has its own immutable source/instrument freeze.

```bash
python3 docs/tracking/evidence/formation-duty-20261109/prepare.py --repo "$PWD" --work-dir /tmp/formation-duty --head 23f2fedb140d47b22be36b2d495190b0b1685251 --dotnet /path/to/dotnet --packages /path/to/nuget-cache
python3 docs/tracking/evidence/formation-duty-20261109/run.py --work-dir /tmp/formation-duty --stage probe
python3 docs/tracking/evidence/formation-duty-20261109/run.py --work-dir /tmp/formation-duty --stage control
python3 docs/tracking/evidence/formation-duty-20261109/run.py --work-dir /tmp/formation-duty --stage matrix
python3 docs/tracking/evidence/formation-duty-20261109/analyze.py --work-dir /tmp/formation-duty --output-dir /tmp/formation-duty-analysis
```

For reanalysis of retained captures, materialize this directory's LFS payloads and
use `retained/` as `--work-dir` (stage receipts are retained alongside the cases).
`tools/dotnet-ci/retained_trace_store.py` reads both as-run gzip and the packed stores.
Every archive has a SHA-256 index; every decoded stream has its own raw byte count
and SHA-256. Integer word-delta/shuffle compression preserves all IEEE float bits,
records and event order. NumPy is optional; its portable fallback decodes identical
bytes. Original gzip container bytes are not retained; their hashes/sizes are in
the index. All decoded per-frame bytes are retained and were verified before deletion
of the gzip containers. The three storage tests cover bit patterns, block boundaries,
empty streams and refusal to delete originals after corruption.

The preliminary v1.0 control was stopped uniformly before comparing acceptance
results because the instrument omitted the per-frame team/pass-flight witness and
exhaustive real-payload mutations. Its attempts remain separately archived, including
incomplete streams and recorded exits. They are not completed validation runs. The
original and revised inputs are separately identifiable; historical evidence is untouched.

## Requested-value observation contract

`GetRequestedFormation`, `MatchFormationState.HomeRequested/AwayRequested`, and the
corresponding frame/HTTP fields expose the **raw requested enum value**, including
unsupported values such as 255. This is an audit value, not the applied formation.
`GetEffectiveFormation` and the effective frame/HTTP fields expose the validated
kickoff selection; an unknown live request falls back to F442. Do not display raw
255 as an applied formation or silently normalize away the diagnostic request.
Both teams' invalid-live-input tests now assert the raw/effective distinction.
Saved effective metadata remains strict and rejects unknown families.

## First-tick allocation coverage

The new six mirrored family/team cases exercise cold `InitializeFormation` selection
and seeding with a strict **0-byte** oracle. Unity uses its native GC allocation
constraint and a positive control; the Linux counter also has a positive control.
The test additionally executes the first actual `RunTick` on fresh engines and
compares its allocation exactly with the default first-tick baseline on .NET.

An attempted absolute zero assertion for the whole first tick found **536 bytes**
for every family, including F442. A standalone same-lane microprobe reproduces 536
on unchanged main `d4b026aa` and the measured runtime `23f2fedb`, even with existing
lazy crypto primed outside the measurement. The new selection itself allocates 0;
new full first ticks match the existing cost. The global cold-first-tick zero claim
is therefore not established, and no production fix or relaxed existing budget is
introduced. The discovery/probe/test receipts are in `validation/review-followup/`.
The native Unity version of the new test remains part of the pending host request.

### Startup versus later frames — follow-up

The [open allocation issue](../../open-issues.md#engine-frame-allocation) and
[bounded 1,200-tick probe](validation/allocation-followup/README.md) now record that
536 B occurs on tick 1 only in eight repeated samples on both unchanged main and
the measured runtime. Most later ticks are zero, but three reproducible later
logging-associated allocations remain; zero per-frame allocation is not claimed.
This follow-up changes documentation/evidence only; runtime/tests and all Unity
inputs remain identical to reviewed head `75d62186`. Its Unity request can proceed.

## Validation and host request

**Current review follow-up:** the expanded fixture passes **42/42**, including all
six allocation cases and both raw-invalid-request cases. All **354 tooling tests**,
evidence manifests, documentation consistency and Unity meta integrity pass.
The full `bash tools/run-tests-local.sh --pr` sweep and exact-head CI at
`75d62186` have completed: **4,521 passed / 225 existing console skips / 0 failed**,
37 assemblies; MatchEngine **982 / 16**, PositioningAI **143 / 13**. The local TRX
skipped/inconclusive inventory matches the retained baseline exactly; three
pre-existing inconclusives remain separate. Quarantine and owner-held RED ledgers
are empty. [CI receipt](validation/allocation-followup/ci543-summary.json) and
[local inventory](validation/allocation-followup/local-gate-summary.json) retain details.
The full-gate receipts described next belong to the earlier implementation closeout.
Pinned Unity remains owner-run and pending at the new exact head.

Both Linux full-gate attempts and the correction are retained. The successful run reports,
verbatim: **Failed: 0, Passed: 973, Skipped: 16, Total: 989** for MatchEngine;
**143 passed / 13 skipped** for PositioningAI. Across all 37 assemblies the console
summaries total **0 failed / 4,512 passed / 225 skipped**. `Gate PASSED`; quarantine empty.
This full run discovered 33 cases in the new fixture. The final 35-case fixture,
including two subsequently added real-squad cases, separately passes 35/35.

The unchanged adapter console omits three existing inconclusives from its totals:
EventSystem's unpinned boot golden, ShotMechanics' BM007 inability to reach its
stumble setup, and MatchEngine's insufficient connected-tackle sample. TRX retains
all three as NotExecuted. The tackle fixture was also reproduced unchanged on main
under this pinned toolchain. All 225 ignored/opt-in skips are existing; **zero new
skips, quarantines or disabled tests**. The per-assembly TRX outcome/reason inventory
is in `validation/434-gate-summary.json`.

The exact full local command is `bash tools/run-tests-local.sh --pr`. The first
attempt exposed two second-half restart fixtures that jumped the clock before the
new kickoff lock. They now run the normal first tick before that test-only jump;
all original outcome assertions remain. No production fix was needed, and the
narrow restart suite passes 19/19. Both full attempts are retained.

The normal commit hook was attempted and exceeded its unchanged 60-second cold-build
budget. Subsequent commits used a command-scoped bypass, with independent validation;
this is not a passing-hook claim. No test/hook policy was weakened.

Pinned Unity 6000.4.9f1 validation remains pending. The final PR body names the exact
checkout SHA. Force a full compile/reimport and retain Editor.log; run unfiltered
EditMode assemblies `TacticalDirector.MatchEngine.Tests`, `TacticalDirector.PositioningAI.Tests`,
`TacticalDirector.MatchClientCore.Tests` and `TacticalDirector.MatchViewer.Tests`. Expected fixtures include
`MatchEngineFormationDutyTests`, `MatchEngineRestartTests`, `TacticTranslationTests`,
`SlotComposerStageTests`, `TickStampedCommandReplayTests`, `RotationControllerTests`,
`MatchEngineSnapshotSchemaTests` and `LiveMatchFrameCueTests`.
Retain NUnit XML, pass/fail/skip/inconclusive counts and source identity. Linux
shim evidence and an absent-license CI skip do not satisfy this host request.

CSV report containers use LF newlines in Git; their cell values are unchanged from
the analyzer output. Binary trace streams retain their original exact byte hashes.

**Review follow-up:** the original source/test identity receipt describes the earlier
closeout. The new `review-followup-source-identity.json` records the sole additional
source-path change (tests); all production files still match the measured runtime.
See [the host request](HOST-REQUEST.md) for the current exact-head procedure.
