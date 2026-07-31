---
name: dotnet-gate
description: >-
  Run the non-certifying Linux compile/test gate over the whole src/ tree and report the result in the
  project's established form, including per-suite counts and the quarantine state. Use this skill
  before committing any code change, whenever the user asks to "run the tests", "run the gate",
  "check it builds", or "is it green", when a landing entry needs its gate line, and when triaging a
  CI failure on the dotnet-compile-test job. Trigger it even for a change that looks doc-only if any
  .cs, .asmdef, or tools/dotnet-ci file moved — this gate is the only thing that compiles the tree,
  and surfaces that were never compiled are a documented recurring defect class here.
---

# Run the Dotnet Gate

`tools/dotnet-ci/run-gate.sh` generates plain .NET projects from the Unity asmdefs, builds the
**entire** `src/` tree, and runs every NUnit suite. Before it existed, six consecutive spec test
suites — and one production assembly — had never compiled, so every "the suite enforces X" claim in
the project was unverifiable. Treat a green gate as the minimum bar for any code claim.

## Run it

```bash
bash tools/dotnet-ci/run-gate.sh
```

If the SDK is missing, install it first (past sessions used `apt`, landing on SDK 8.0.x). The script
is `set -euo pipefail` and ends with a `── Gate PASSED ──` line; anything else is a failure.

The stages, so you can read a failure quickly:

1. `generate_projects.py` — asmdef → csproj/sln. A failure here usually means a new or edited
   `.asmdef` (a missing reference, or an assembly name that does not match).
2. `dotnet restore`, then `dotnet build … -clp:ErrorsOnly` over the whole tree. **Any compile error
   anywhere fails the gate** — including in test assemblies.
3. `dotnet test`, excluding quarantined tests from `tools/dotnet-ci/known-failures.txt`.

## The quarantine

`known-failures.txt` is currently **empty** (comments only), which means the full suite is enforced
and any new failure fails CI. It is **shrinking-only**: do not add a test to it to get green. If a
test genuinely encodes an obsolete contract, fix or re-anchor the test with its intent preserved and
say so — that is the documented "tests encoded the old contract" class, and it is a normal outcome of
a correctness fix, not a reason to quarantine.

## Reading failures

A handful of causes account for most gate breaks here, and they are cheap to check first:

- **Unity-vs-netstandard2.1 surface.** Production targets `netstandard2.1` / C# 9 to match Unity's
  BCL. APIs outside it fail here even though they look fine (`File.Move(overwrite:)` was one).
- **NUnit visibility.** `[Test]` methods must be `public` — a suite of 51 internal test methods
  compiled fine and could never run.
- **Static initialization order.** `static readonly` fields initialised before their source read zero
  at runtime. This has bitten three times (`EventRegistry`, `PerceptionConstants`, `EventOrdinalCache`);
  the fix is an expression-bodied property or an explicit `EnsureInitialized()`.
- **Ambiguous type names.** Several assemblies each define a `TacticTranslation`; match-engine sees
  five. Fully-qualify from the first line rather than adding a `using` alias later.
- **Env-gated diagnostics.** Instruments `Assert.Ignore` unless their `TD_*_DIAGNOSTIC` variable is
  set, so "skipped" is the correct result for them, not a problem.

## Report it

Quote real numbers, in the project's established form:

> **Full dotnet gate: PASSED, 0 failures** (whole tree green; match-engine 360 → 366).

Per-suite before → after counts matter because they make a silently-skipped or silently-dropped suite
visible. Never restate a previous landing's gate result as if it were this run's.

If the gate cannot run in this environment, say that plainly and say what was done instead
(exhaustive manual review, CI verification on push) — the project's history has honest entries of
exactly that shape, and they are what makes the green ones credible.

## What this gate is not

It is explicitly **non-certifying**. Determinism certification runs on the pinned Windows / Unity /
Mono tuple in `docs/tracking/certification-platform.md`, via `docs/tracking/cert-run-runbook.md`. A
green Linux gate says the tree compiles and the suites pass; it says nothing about bit-exact
determinism or the certified per-tick performance number.
