# Finance T3b2 — supplementary Linux validation

**Created:** October 13, 2026
**Version:** 1.0
**Purpose:** Retain source identity and executed evidence for #40 §3.7 / §5.10 revenue amounts.

## Scope and source identity

The sole `[club-finances] DeepRevenueEnabled` config gate defaults **false**. Enabled revenue is
integer-only and deterministic: in-season sponsorship plus position-based attendance per completed
home fixture. Break/pre-season days earn zero. All clubs remain staged before publication at slot 11a.
There is no new save field/version, assembly reference, finance cursor or RNG stream/draw.
Stadium capacity remains the approved 20,000 `[CROSS-PENDING]` constant until #53 T0.

`source-tree.txt` pins the complete validated `src` Git tree. `source-hashes.txt` pins the changed source
and metadata files, with paths relative to repository root. The final C# sources were recompiled after
header-only edits; both production and test DLLs are byte-identical to the executed DLLs, whose hashes
are in `binary-hashes.txt`. Binaries are not committed. All files in this directory except `SHA256SUMS`
are covered by that integrity manifest.

## Executed results

| Check | Result |
|---|---|
| ClubFinances assembly | 70 passed / 0 failed |
| SeasonSave assembly | 502 passed / 0 failed; 505 discovered, 3 diagnostic/calibration cases filtered |
| Focused finance lifecycle subset | 40 passed / 0 failed (included in the SeasonSave total) |
| Unique selected total | **572 passed / 0 failed** |
| Fresh-process production config probes | 6 passed |
| C# 9 / netstandard2.1 production compilation | relevant assembly dependency closure compiled, 0 errors |
| Both test assemblies + test shim against NUnit 3.5 | compiled, 0 errors (API compatibility only) |

The result XML and runner logs are retained compressed; `xz -dc season-save.xml.xz` reads the XML.
The normal `run-gate.sh` attempt reached restore and failed in .NET CLI startup at
`System.Diagnostics.Process.GetStat` / process metadata lookup (`standard-gate-unavailable.log`).
The reported passes use direct Roslyn compilation and NUnitLite 3.14 on .NET 8.0.28 with the repository
UnityShim. They are supplementary, **not** the standard Linux policy gate or governing Unity evidence.
The compile log includes three inherited DecisionTree CS0649 warnings and the test-shim netstandard
reference warning. NUnit 3.5 was used for compilation only, not execution.

The six isolated config probes call public `AdvanceToNextFixtureDay`, fixture resolution and
`AdvanceDays(1)`, independently check cash deltas, and verify: empty config is off; explicit true earns
75,360 total across four clubs in the opening round; custom sponsorship/price tuning earns 107,108;
negative tuning stays inert when off; negative sponsorship and invalid stadium fill refuse when on.
`config-probe.cs.txt` preserves the probe source; it is supplementary and not a CI test fixture.

## Repeatable verification

On a normal .NET SDK host, use the repository runner:

```bash
bash tools/dotnet-ci/run-gate.sh --test-filter 'FullyQualifiedName~ClubFinances|FullyQualifiedName~SeasonSave'
```

The fallback compiler is retained as `compile.py.txt`. Run project generation first, supply
`TD_REPO`, `TD_VALIDATION_OUT`, and `TD_DOTNET_ROOT` (SDK 8.0.422 with netstandard2.1 and net8.0.28
reference packs), and put NUnit 3.14's `nunit.framework.dll` in the output directory. It follows
project-reference closures and compiles the two selected test assemblies. Build `runner.cs.txt` as an
executable against net8 references and NUnitLite 3.14; use a net8.0.28 runtimeconfig. Executed selectors:

```text
finance-runner.dll TacticalDirector.ClubFinances.Tests.dll --workers=0
finance-runner.dll TacticalDirector.SeasonSave.Tests.dll --workers=0 --where="cat != Calibration && cat != Diagnostic"
```

## Remaining merge gates

- Final-head GitHub CI, including the repository's Linux functional gate.
- Exact-final-head Unity **6000.4.9f1** compilation on the pinned host under `src/CLAUDE.md` Verification.

The PR remains draft until governing validation is recorded. Enabling or retuning revenue remains a
separate balance-pass/owner decision.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-10-13 | — | T3b2 source identity, direct compile/test evidence and bounded remaining gates. |
#endregion
