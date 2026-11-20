# #441 ERR-010-005 header-trigger approach condition — receipts

Recorded November 20, 2026. Created to preserve the failing-before/passing-after regression, the
post-fix six-seed census and the pre/post physical-trajectory comparison for `ERR-010-005`
([`spec-error-log.md`](../../spec-error-log.md),
[design §4.2](../../gk-heading-engine-integration-design.md#42-header-trigger),
[counters §6.8](../../header-reachability-441-counters.md#68-post-fix-rerun--err-010-005-approach-condition-november-20-2026)).

Pre-fix source: `420c0730` (PR #558 head; its `src/` tree equals `ceb0384a`). Post-fix source: the
`ERR-010-005` commit on `claude/charming-mccarthy-4drrk8`. Linux x64, .NET SDK 8.0.134, Debug Unity shim.

| File | What it shows |
|---|---|
| `pre-fix-regressions.trx.xz` | The new composed `MatchEngineGkHeadingTests.HeaderTrigger_*` cases on pre-fix source: 4 failed (moving-away and nearer-passer, home and away), 3 passed (the two reversed-flight controls and the existing projection test). |
| `post-fix-regressions.trx.xz` | The same composed cases plus every `NearestHeaderCandidate` lock on post-fix source: 19 passed, 0 failed. |
| `post-fix-match-engine-suite.trx.xz` | Unfiltered `MatchEngine.Tests` on post-fix source: 1,071 passed, 17 skipped (environment-gated diagnostics), 0 failed. |
| `post-fix-strict-corpus.trx.xz` | `HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters` with `TD_HEADER441_DIAGNOSTIC=1` on post-fix source. Pre-fix is `../header441-current-main-20261120/strict-corpus.trx.xz`. |
| `reviewed-head-kd7-regressions.trx.xz` | PR #559 review P2: all composed `HeaderTrigger_*` cases on the reviewed head `e6e2255` with the KD-7 latch tests added: 4 failed (ball reversing in range commits twice; nearer agent's interruption re-arms the first; home and away). |
| `base-kd7-regressions.trx.xz` | The same file on the pre-fix base `420c0730`: 6 failed (the four approach-condition cases plus the nearer-agent interruption, home and away); the reversal case passes there. |
| `post-review-regressions.trx.xz` | All `MatchEngineGkHeadingTests` plus the `NearestHeaderCandidate` locks with the KD-7 correction: 50 passed, 0 failed. |
| `post-review-strict-corpus.trx.xz` | The six-seed census with the KD-7 correction. Aggregates equal `post-fix-strict-corpus`; snapshot digests differ. |
| `PhysicalTrajectoryProbe.cs` | Runs the six frozen seeds to full time and hashes the ball position/velocity and every agent position/velocity after every tick, plus the score. It reads no other serialized state, so it cannot attribute a snapshot-digest difference to particular fields. |
| `physical-trajectory.txt` | The probe's output for pre-fix, the first landing `e6e2255`, and the KD-7 correction. All three agree on every seed. |

## Reproduce

The regression receipts and the census use the commands of
[`../header441-current-main-20261120/README.md`](../header441-current-main-20261120/README.md) with
these filters:

```bash
python3 tools/dotnet-ci/generate_projects.py
dotnet test src/match-engine/tests/match-engine-tests.gen.csproj -p:NuGetAudit=false \
  --filter 'FullyQualifiedName~MatchEngineGkHeadingTests.HeaderTrigger_|FullyQualifiedName~NearestHeaderCandidate'
dotnet test src/match-engine/tests/match-engine-tests.gen.csproj -p:NuGetAudit=false \
  --filter 'FullyQualifiedName~HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters' \
  --environment TD_HEADER441_DIAGNOSTIC=1 --logger 'trx;LogFileName=post-census.trx' --results-directory Temp
```

For the failing-before receipt, check out the pre-fix source, copy the post-fix
`src/match-engine/tests/MatchEngineGkHeadingTests.cs` over it (that file does not call the changed
signature), and run the first filter.

The probe needs a throwaway directory outside the repository holding a copy of
`PhysicalTrajectoryProbe.cs` and this project, which references the generated engine project of the
tree being measured. The SDK compiles the `.cs` file by default; do not add a `Compile` item for it
(that duplicate fails with `NETSDK1022`).

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
  <ItemGroup><ProjectReference Include="<tree>/src/match-engine/match-engine.gen.csproj" /></ItemGroup>
</Project>
```

`dotnet run` prints one `physical=` line per seed. Compare the lines for the two trees.
