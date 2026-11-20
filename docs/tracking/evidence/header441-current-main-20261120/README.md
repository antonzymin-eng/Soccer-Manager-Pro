# #441 current-main diagnostic rerun

Recorded November 20, 2026. Measured source: `ceb0384afbe393dcb3d4cf6c8ed89796e9ba95a3`.
Linux x64, .NET SDK 8.0.426, runtime 8.0.32, Debug Unity shim. See
[the counter definitions and results](../../header-reachability-441-counters.md#67-current-main-rerun--november-20-2026)
for the interpretation. The focused NUnit receipt covers the #526 observer and the existing
MatchEngine GK/Heading path. The census and probes are local diagnostic runs.

## Linux pre-commit receipt

The repository's `bash tools/bootstrap-dev.sh` ran the full pre-commit composition on the
staged tree `36573b40c3bf5daeeea5615a38e3bb1f5e3bc6ee`, which matches
`0285f325cae3c01593f749405d01e901bf002e0d^{tree}`. It completed with 4,778 passed,
206 skipped and zero failed across 37 project summaries. MatchEngine accounted for 1,035
passed and one skipped. The bootstrap cold run uses the pre-commit test selection outside
the 60-second commit-hook budget. The hook writes the tested tree ID to a separate state
file, which is archived alongside the console output. [Complete run log](precommit-bootstrap.log.xz),
[hook tree record](precommit-index-tree.txt).

## Reproduce the strict census and archived trace

Generate the shim projects at the measured commit. Store outputs under the ignored `Temp/`
directory.

```bash
python3 tools/dotnet-ci/generate_projects.py
mkdir -p Temp
dotnet test src/match-engine/tests/match-engine-tests.gen.csproj -p:NuGetAudit=false \
  --filter 'FullyQualifiedName~HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters' \
  --environment TD_HEADER441_DIAGNOSTIC=1 \
  --logger 'trx;LogFileName=main-baseline.trx' --results-directory Temp
```

The existing bounded classification probe checks the old seed-1 interval on the measured
engine. The staged replay then reads the October trace and varies only the first frame driven
after the tactical commit. It checks preparation, execution and the published event.

```bash
archive=docs/tracking/evidence/header441-reachability-20261020
receipt=docs/tracking/evidence/header441-current-main-20261120
dotnet run --project "$archive/classification/Probe.csproj" -p:NuGetAudit=false -- \
  Temp/current-main-seed-1-prefix.txt
xz -cd "$archive/classification/classification.txt.xz" > Temp/archived-classification.txt
dotnet run --project "$receipt/ArrivalReplay.csproj" -p:NuGetAudit=false -- \
  Temp/archived-classification.txt
```

## Reproduce the current missed arrival

Run the production baseline and the three experimental starts through normal match ticks.
The experimental runs inject one intent just before Heading and hold the existing latch
at the later production stride. Their assertions cover the jump start, prepared frame,
published header event and terminal failure.

```bash
dotnet build "$receipt/ArrivalReplay.csproj" -p:NuGetAudit=false
dotnet run --project "$receipt/ArrivalReplay.csproj" --no-build -- current
for start in 113398 113399 113400; do
  dotnet run --project "$receipt/ArrivalReplay.csproj" --no-build -- current "$start"
done
```

## Reproduce the broad proximity check

The sanity patch initializes formation through one real tick in each staged mid-match control,
as required on this engine. The full matches still start at tick zero. It replaces the
historical October digest pins with an external comparison against the strict census on the
same commit.

```bash
mkdir -p Temp/header441-sanity
cp "$archive/sanity/Program.cs" Temp/header441-sanity/Program.cs
patch Temp/header441-sanity/Program.cs < "$receipt/current-main-sanity.patch"
sed 's|../../../../../|../../|g' "$archive/sanity/Probe.csproj" > Temp/header441-sanity/Probe.csproj
dotnet run --project Temp/header441-sanity/Probe.csproj -p:NuGetAudit=false -- --self-test
dotnet run --project Temp/header441-sanity/Probe.csproj --no-build -- --corpus Temp/main-sanity.txt &&
  python3 "$receipt/verify-census-digests.py" Temp/main-baseline.trx Temp/main-sanity.txt
```

The verifier requires all six frozen seeds at tick 324000 in both reports and fails on
any digest mismatch. It also accepts the archived `strict-corpus.trx.xz` and `broad-corpus.txt`
directly. The broad probe checks the full-time callback boundary and reconciles its counts
against the frame and passage totals.
