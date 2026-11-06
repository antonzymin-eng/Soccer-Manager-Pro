# Release fixture-hook inspection

This scratch-only .NET 8 metadata probe reads the compiled MatchEngine assembly without loading it.
It checks the two PR #542 fixture fields and counts direct calls at their production call sites.
Development must contain both fields and each call once; release must contain neither field nor call.
The probe is not Unity gameplay or a test assembly source file.

From the repository root, copy the two adjacent `.txt` files to an ignored scratch directory as
`Program.cs` and `Probe.csproj`. Generate the repository .NET projects first, then build:

```sh
dotnet build src/match-engine/tests/match-engine-tests.gen.csproj
dotnet build src/match-engine/match-engine.gen.csproj --no-restore -m:1 -c Release -p:DefineConstants=TRACE
dotnet run --project work/w8c/release-hook-probe/Probe.csproj -- \
  src/match-engine/bin/Debug/netstandard2.1/TacticalDirector.MatchEngine.dll \
  src/match-engine/bin/Release/netstandard2.1/TacticalDirector.MatchEngine.dll
```

The `TRACE` global property replaces the generator's development define for the release proof.
Receipts are `validation/release-build.log.xz` and `validation/release-hook-inspection.log.xz`.
