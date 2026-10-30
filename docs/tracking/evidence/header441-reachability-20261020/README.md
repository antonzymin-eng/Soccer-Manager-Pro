# #441 header reachability: frozen local evidence and independent sanity probe

Created October 20, 2026; packaging and independent probe prepared October 21, 2026.
Cadence contract corrected October 22, 2026 after the invalid October 21 attempt;
accepted corrected local sanity result recorded October 22, 2026.
Purpose: make the corrected local six-seed arrival result and its bounded witness
classification reproducible, then measure a separate pre-Heading proximity denominator.
This is development evidence from the Windows .NET shim, not Unity certification or a
football rate claim. The owning definitions and interpretation are in
`docs/tracking/header-reachability-441-counters.md` §§3.7–3.8 and 6.4.

## What is frozen

- `original-source.patch` is the original, unmodified `git diff --binary` of
  `src/match-engine/MatchEngine.cs` and
  `src/match-engine/tests/HeaderReachabilityDiagnosticTests.cs` against base
  `1a3b4271b5d7a50bbff3a88ef242345c8e49e89b`. It reverse-applied cleanly
  to the measured working files before the subsequent comment date correction.
  No gameplay, schema, RNG, or `[GT]` edit is in it.
- `classification/Program.cs` is byte-for-byte the original bounded replay
  source, SHA-256
  `203522E3272C699086A54A01578432E535F70F67F9FD3786EBBE891D1E6A5368`.
  `classification/classification.txt.xz` losslessly preserves the original
  CRLF trace/output. Its **decompressed** SHA-256 is
  `8BB2492CFE9CB53ADDA31F8D5330C5486B0A380AA91514E9FA064E8E1F209A92`.
  `classification/original-probe.csproj.txt` preserves the original
  ignored `Temp` project bytes; `classification/Probe.csproj` is its runnable
  archive-relative project. The latter does not alter `Program.cs`.
- `raw/header441-arrival-final.trx.xz` and
  `raw/header441-focused-final.trx.xz` are lossless LZMA/XZ copies of the
  original TRX files, not re-rendered extracts. Their **decompressed** SHA-256
  values are respectively
  `03D4A3A85FE29B5F7386EC7F44A1E7EB4CBBE8D56BC9CCD03D87CDC756753F92`
  and
  `DEB23BBED4C76C8F66814F7D7F4F634E37788FD8113F801854489CE5BFC0EC91`.
  The first is the corrected 21-minute six-seed arrival run; the second is the
  focused control capture. The withdrawn earlier TRX is not a valid arrival result.
- At measurement time, `MatchEngine.cs` was SHA-256
  `909E402087177A3F23EB1EFB923CA43644A085C2DE862D03D9F0F10B4B39C77D`
  and `HeaderReachabilityDiagnosticTests.cs` was SHA-256
  `14A9755DDF15597CDC756ED7BC1AB24FB5BA2F19EC9E184976AC987E613FBF0D`.
  The current `MatchEngine.cs` SHA-256 is
  `9D6EDC1AD735590DD76C4A4FA307964AD9B3290F3906302398FDD3F0C28C5CF6`
  solely because version-history row 2.14's date was corrected from October 18
  to October 20 **after** archiving the measured patch. The original hash remains
  historical measurement identity, not the current source hash. This comment
  change cannot alter compiled gameplay IL or snapshot digests.

The source reconstruction was also exercised from the measured base with
that checkout's `.gitattributes`: ordinary `git apply`, without line-ending
normalization or patch rewriting, reproduced the measured
`MatchEngine.cs` and diagnostic-test SHA-256 values above exactly.

## Reconstruct the measured source

In an isolated checkout of base `1a3b4271b5d7a50bbff3a88ef242345c8e49e89b`,
copy this evidence package into the same relative location, then run from the
checkout root:

```text
git apply --check docs/tracking/evidence/header441-reachability-20261020/original-source.patch
git apply docs/tracking/evidence/header441-reachability-20261020/original-source.patch
python tools/dotnet-ci/generate_projects.py
dotnet build docs/tracking/evidence/header441-reachability-20261020/classification/Probe.csproj -p:NuGetAudit=false --ignore-failed-sources
python -c "import pathlib; pathlib.Path('Temp').mkdir(exist_ok=True)"
dotnet run --project docs/tracking/evidence/header441-reachability-20261020/classification/Probe.csproj --no-build -- Temp/header441-replay.txt
```

The fresh replay output goes to ignored `Temp/`, leaving the frozen trace
untouched. Decompress `classification/classification.txt.xz` to a separate
ignored `Temp/` path, then compare the `witness`, `classification`,
`summary`, and `endDigest` lines. Assembly SHA lines in a fresh
build may differ with compiler/runtime details; the archived output records
the measured assemblies. The run should close the one passage at frame
125468, find two witness pairs at starts 125455/125456, and end at tick
125500 with digest
`CF64C07C29EDDE36ED456865DCA3CDE20CD6148D576C59EAC57D3FF8E2808E00`.

After creating `Temp/` as above, the compressed original TRX can be
extracted there without altering the archive:

```text
python -c "import lzma,pathlib; p=pathlib.Path('docs/tracking/evidence/header441-reachability-20261020/raw/header441-arrival-final.trx.xz'); pathlib.Path('Temp/header441-arrival-final.trx').write_bytes(lzma.decompress(p.read_bytes()))"
```

## Independent proximity sanity counter: frozen definitions

`sanity/Program.cs` is a separate standalone console. It attaches only to
`MatchEngine.TestOnly_HeaderArrivalObserver`, which fires immediately before
`Heading.Update` in the real 60 Hz Physics phase. It does not use
`ArrivalCensus`, hypothetical jump starts, `HeadingJumpKinematics`, commit
events, or launch labels. Each of the six frozen seeds is run for exactly
324,000 attempted and completed `RunTick` calls with `EnableGkHeading`.
The expected observed callback frames are **1–323999 inclusive**, exactly
323,999 samples. `RunTick` advances the clock before Input; at tick 324,000,
Input sets `MatchEnded` and Physics returns before the callback. The snapshot
still occurs at tick 324,000, where the final digest must match §6.4 for every
seed and `MatchEnded` must be true. Every attempted tick is validated: one
callback at that same tick while live, zero at full-time. Halftime, restarts,
and possession permit no additional gaps, duplicates, or tolerance.

The primary pair is one active, non-goalkeeper, non-sent-off outfielder in
one callback frame when possession is `NO_POSSESSION`, ball-centre Z is
in **[1.5, 2.6] m inclusive**, and ball/player XY distance squared is
**≤1.0 m²**. `GROUNDED` and `STUMBLING` are included. This is a broad
spatial sanity condition, not a reachable head contact.

The report distinguishes:

- `attemptedRunTicks` and `completedRunTicks`, each 324,000 per seed,
  separately from `callbackFrames` and `sampledFrames` (the latter is the count of unique
  callbacks accepted under strict increasing frame order; they are equal),
  `looseFrames`, and
  `heightBandLooseFrames`, each with the previous predicates applied;
- `allFramesOutfielderObservations`, all active outfielder/frame
  observations, and `outfielderObservations`, that count only on
  `heightBandLooseFrames`;
- `anyNearFrames`, unique height-band loose frames with at least one
  primary pair, and `nearAgentFrames`, the number of such agent/frame pairs;
  per-team and movement-state splits use `nearAgentFrames` as denominator;
- the nearest active outfielder's XY distance on **every**
  `heightBandLooseFrame`, before the 1 m cutoff, in mutually exclusive
  bins `≤0.18`, `(0.18,0.5]`, `(0.5,1]`, `(1,2]`, `>2` m, plus
  `noActive` when no eligible outfielder exists;
- ball vertical velocity `>0`, `<0`, or `==0`, once per
  `heightBandLooseFrame` and once per `anyNearFrame` separately.

A passage belongs to one player and consists of consecutive frames satisfying
the complete primary pair. Any predicate failure, observation gap, or end of
run closes it. Deliberate synthetic gaps test passage splitting; an unexpected
real-engine gap fails cadence validation and invalidates the run. Each CSV row has seed, agent, team, inclusive start/end,
duration in observed frames, minimum XY distance, and ball X/Y/Z/Vz at that
minimum. Exact duration counts are printed. Required reconciliations:
`sum(duration)=nearAgentFrames`; team and state pair sums equal
`nearAgentFrames`; nearest bins including `noActive` and height Vz
classes each sum to `heightBandLooseFrames`; near Vz classes sum to
`anyNearFrames`.

The explicit commands are:

```text
python tools/dotnet-ci/generate_projects.py
dotnet build docs/tracking/evidence/header441-reachability-20261020/sanity/Probe.csproj -p:NuGetAudit=false --ignore-failed-sources -m:1 -nodeReuse:false -p:UseSharedCompilation=false --verbosity minimal
python -c "import pathlib; pathlib.Path('Temp').mkdir(exist_ok=True)"
dotnet run --project docs/tracking/evidence/header441-reachability-20261020/sanity/Probe.csproj --no-build -- --self-test
dotnet run --project docs/tracking/evidence/header441-reachability-20261020/sanity/Probe.csproj --no-build -- --corpus Temp/header441-sanity.txt
```

After compilation, the same entry point can be run directly without project
evaluation (use the absolute or checkout-relative DLL path):

```text
dotnet docs/tracking/evidence/header441-reachability-20261020/sanity/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll --self-test
dotnet docs/tracking/evidence/header441-reachability-20261020/sanity/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll --corpus Temp/header441-sanity.txt
```

On October 21 the one-node Windows .NET 8 shim build of `sanity/Probe.csproj`
passed with **0 warnings / 0 errors** using
`-p:NuGetAudit=false --ignore-failed-sources -m:1 -nodeReuse:false
-p:UseSharedCompilation=false --verbosity minimal`. The explicit
`--self-test` returned exit 0:
`SELF_TEST,PASS,syntheticCases=6,realRunTickCallback=1`.
This proves the self-test controls and real pre-Heading hook ran; it does not
substitute for the six-seed measurement or Unity validation.

`--self-test` checks exact one-frame multiplicity, contiguous and gap-split
passages, inclusive boundaries and just-outside values, possession/keeper/
sent-off exclusions, grounded/stumbling inclusion, mirrored teams, and a
staged positive observed through the real `RunTick` pre-Heading callback.
The October 22 cadence controls use the same per-attempt validator as the
corpus. A fresh enabled engine's first two ticks must produce callbacks
`[1,2]`, one each, with the match still live. A separate fresh engine starts
at `TestOnly_SetPhysicsFrame(323997)` and takes four ticks: callbacks
`[323998,323999]`, per-tick deltas `[1,1,0,0]`, engine ticks
`[323998,323999,324000,324001]`, and ended flags `[false,false,true,true]`.
Rejection controls cover a skipped live callback, duplicate callback,
callback at full-time, and callback carrying the wrong tick.
Cadence failures report seed, attempted/completed calls, expected/actual
callback delta, current/expected tick, first/last sample and ended state,
then flush the output before throwing. Console progress every 32,400
attempts reports seed, attempts, actual callbacks and current tick; this
does not affect the counters.
`--corpus` is explicit because it runs six full matches; output goes to
ignored `Temp/` until reviewed and archived. No six-seed sanity result has
been asserted in this frozen definition.

The October 21 first corpus attempt is **invalid**: the old source required
a callback at the full-time tick and aborted after the first complete match
(about 31 minutes on this host). It emitted no accepted seed summary or
digest comparison, so its partial rows support no census inference.
The failed source (`66D8AD8934C24BB2F3B04D1A6644CF460A8F05281830A28418615D75DADF0366`),
console, and partial output are preserved losslessly in
`raw/header441-sanity-failed-source-66D8AD89-20261021.cs.xz`,
`raw/header441-sanity-failed-console-20261021.log.xz`, and
`raw/header441-sanity-failed-partial-20261021.txt.xz`. The October 20
definition freeze remains historical; the October 22 correction changes
only the cadence contract and diagnostic controls. The accepted corrected
six-seed result is recorded below. Run 3's strict census and narrow
delayed-takeoff candidate are unchanged.

## Accepted corrected sanity result — October 22, 2026

The literal one-node build command above succeeded on the Windows .NET 8
shim with **0 warnings / 0 errors** in 21.95 s. The direct DLL `--self-test`
returned exit 0 with `syntheticCases=6`, `realRunTickCallback=1`,
`realCadenceCases=2`, and `rejectionCases=4`. The direct DLL `--corpus`
also returned exit 0, from 12:46:05 to 13:10:13 UTC on October 22
(05:46:05–06:10:13 PDT, about 24 m 08 s from timestamps). That wall time
is not a certified performance measurement. The fresh read-only Sol AR-4
review was repeated and reported no High/Medium/Low finding. Astra was
quota blocked; no Opus review is claimed.

The accepted source and assembly identities are:

| Item | SHA-256 |
|---|---|
| `sanity/Program.cs` | `438D1F5D5B3372CDA5E80889ECEA4234DC67A873959DF6DA67F6F3FDC6474E10` |
| sanity probe DLL | `FE569E86472BE20D7C6FC4F7D58F15CC4CA8D26A6D7D3A7D51E9854DFCC95142` |
| current `MatchEngine.cs` | `9D6EDC1AD735590DD76C4A4FA307964AD9B3290F3906302398FDD3F0C28C5CF6` |
| match-engine DLL | `37089C9E59EC248BA8CFDF906E051FF4EE0B9E0BED11FEFD111CFE49388B5BCA` |

The original Run 3 `MatchEngine.cs` hash above remains the identity of that
earlier measurement; its later comment-date correction is not a gameplay
change. The accepted raw files are byte-preserving `.xz` archives. These
SHA-256 values apply to their **decompressed** bytes and were round-trip
verified:

| Archive | Decompressed SHA-256 |
|---|---|
| `raw/header441-sanity-corrected-20261022.txt.xz` | `7FCA13F8F3FBEDC5ACA380804FD240FA168304EECBCAA86241E9A4F468A85D02` |
| `raw/header441-sanity-corrected-console-20261022.log.xz` | `01D7F13A35020EC86A46F9B05B334C906A2A27BC6442A8362BB32D7B4669032F` |
| `raw/header441-sanity-selftest-20261022.txt.xz` | `3CF3C1AA3F2F74CEBFF293E1324D19B8E3CFCEEE7DE2BFFA043F6ED4B762C4F6` |

All six seeds completed 324,000 attempted/completed ticks with exactly
323,999 actual callbacks/samples at frames 1–323999, `MatchEnded=true`
and the same tick-324000 digests pinned by Run 3. The aggregate report is
1,944,000 attempted/completed calls, 1,943,994 callbacks/samples,
1,762,740 loose callbacks, 35,417 height-band loose frames, 703,506
active-outfielder observations on those frames, 355 unique frames and
355 player-frame pairs within 1 m, and 39 contiguous passages whose
durations sum to 355. The all-callback active-outfielder observation count
is 38,569,934. The full 39 passage rows, six seed summaries/end lines,
nearest-distance and vertical-velocity splits, and exact duration
distribution are in the archived raw report; the compact per-seed table
and interpretation are in the [counter record](../../header-reachability-441-counters.md).

This broad spatial count is neither a head-contact count nor a football
frequency, and it does not fully validate the stricter §3.7 arrival census.
The observed sparse height/player overlap motivates investigating delivery
and positioning, without choosing a causal fix or distinguishing kicker
departure from receiver arrival. A one- or two-frame delayed take-off
remains an unimplemented #10 timing-contract candidate without composed
contact proof. #441 and W9 remain open; landing waits for W8 C measurement,
and #434/#440 retain D-05's after-B→C placement. This Windows shim run is
not Linux CI or Unity/Mono certification.

Near-zero primary pairs would establish only scarcity under this broad
height/proximity definition on these trajectories. Many primary pairs would
localize the next question to spatial/timing conversion; neither outcome by
itself proves the arrival census wrong or supplies a football rate.
Delayed takeoff remains a candidate to investigate with a composed pre-fix
fixture, not a change implemented by this package. KD-W1 still forbids
tuning an unwired subsystem.

## Integrity-checker boundary

The repository-wide evidence-integrity checker is not green on the
unchanged `c95e27ba` baseline: it reports 179 SHA mismatches across 177
unique paths. The manifest scan accounted for all of them as CRLF bytes in
committed files versus LF bytes named by their manifests; none was
unexplained. This package does not change those older archives or claim a
green full-tree check. Its own raw CRLF trace and TRX files are stored as
`.xz` byte-preserving archives, and the new plain-text files use LF, so
the manifest for this package can name their actual committed bytes.
