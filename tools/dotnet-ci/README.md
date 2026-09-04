# tools/dotnet-ci — Non-Certifying Linux Compile/Test Gate

> **Created:** June 12, 2026  
> **Purpose:** Compile the host-free `src/` tree and execute NUnit suites under plain .NET on Linux.  
> **Policy boundary:** `tools/dotnet-ci/run-gate.sh` is the **lower-level executor**. Normal developer and CI policy entry points are versioned in `tools/run-tests-local.sh`.

## Why this exists

Before this gate, multiple specs shipped suites or production surfaces that had never been compiled by any repository check. The Linux shim gate closes that structural gap by generating .NET projects from Unity asmdefs and running NUnit outside Unity.

It is deliberately **non-certifying**. Determinism certification remains owned by the pinned Windows/Unity environment in `docs/tracking/certification-platform.md` and Spec #16. Linux results are regression/compile evidence only.

## Current layout

| Path | Role |
|---|---|
| `generate_projects.py` | Maps `src/**/*.asmdef` to generated `.gen.csproj` files and `TacticalDirector.gen.sln`. Asmdefs remain source of truth. |
| `UnityShim/` | Minimal Unity API shim needed by host-free code. |
| `UnityShim.TestTools/` | Test-framework shims used by generated projects. |
| `known-failures.txt` | Functional flake quarantine ledger. Shrinking-only; currently comments-only. |
| `owner-held-red.txt` | Owner-held failing acceptance predicates. **Not quarantine.** Each is executed separately and must still fail at the recorded diagnostic baseline. |
| `verify-owner-held-red.py` | Requires one exact test identity, failed outcome, recorded diagnostic tokens, no extra results, and expected runner exit. Unexpected green/drift/ambiguity blocks. |
| `coverage.runsettings` | Coverlet/XPlat coverage configuration used by PR/nightly policy modes. |
| `precommit.runsettings` | NUnit pre-commit selection. Excludes taxonomy prefixes only when they occur at the start of the **method name** (`^int_`, `^sim_`, `^e2e_`), avoiding `FullyQualifiedName` substring over-exclusion. |
| `run-gate.sh` | Lower-level generated-project executor. Accepts explicit arguments only; inherited filter/owner/coverage environment controls are rejected. |

## Normal developer commands

Run bootstrap once per clone:

```bash
bash tools/bootstrap-dev.sh
```

Bootstrap installs/verifies the versioned staged-index hook and performs the one-time cold preparation of its persistent build snapshot under `.git/testing-strategy/`.

Use the policy runner after that:

```bash
# Same unit/property-compatible composition used by the git hook.
bash tools/run-tests-local.sh --pre-commit

# PR-equivalent local composition.
bash tools/run-tests-local.sh --pr

# Non-certifying Linux nightly functional/simulation/soak composition.
bash tools/run-tests-local.sh --nightly
```

Do **not** use a bare `bash tools/dotnet-ci/run-gate.sh` result as proof that the repository PR policy composition ran. The low-level command remains useful for executor debugging and targeted investigation, but it bypasses the Spec #19 auditor/owner-held/coverage composition decisions owned by `tools/run-tests-local.sh`.

## Pre-commit performance design

The versioned hook tests the staged Git index rather than the unstaged worktree, but it does **not** create a fresh zero-cache directory on every commit. Its snapshot lives under `.git/testing-strategy/precommit-snapshot`:

- tracked source/document files are overwritten from the current Git index before each run;
- tracked files removed from the index are removed from the snapshot;
- untracked generated projects, `bin/`, and `obj/` remain available for incremental reuse;
- bootstrap performs the cold cache preparation once outside the normal acceptance measurement;
- the normal pre-commit composition remains hard-bounded to 60 seconds.

This design removes the prior cold-restore/34-sequential-project construction defect. It still does **not** prove the ≤60-second requirement: that requires a successful measured run on the certified developer host.

## Owner-held RED policy

`sim_match_engine_close_chance` is currently owner-held RED by explicit project decision. It is not placed in `known-failures.txt` and is not treated as a flake.

PR/nightly policy modes:

1. exclude that exact `Name` from the ordinary blocking pass;
2. run the exact owner-held `Name` separately;
3. parse its TRX;
4. require exactly one matching result and the recorded diagnostic tokens;
5. fail if it passes, drifts, is missing/ambiguous, returns extra tests, or exits abnormally.

The diagnostic contract is proven only when the real PR gate executes successfully; a unit fixture proves verifier behavior, not the live test message format.

## Certified-host boundary

The scheduled Linux job is non-certifying. `.github/workflows/nightly.yml` also defines the authoritative Windows/Unity Spec #16 job, but it is disabled until repository variable `DETERMINISM_CERTIFIED_RUNNER_ENABLED=true` is set after a matching self-hosted runner is actually registered/configured. Until a successful certified-host run exists, FR-TS-075's determinism leg remains operationally open.

## Running in remote Linux authoring environments

Where .NET 8 is already available, the policy runner can execute normally. Historical remote-container measurements established that Ubuntu-hosted .NET can run the generated gate, but those measurements remain non-certifying and do not substitute for the current PR/certified-host evidence.

## Shim fidelity rules

- Shim members replicate Unity semantics only where this codebase depends on them; never add a fake member merely to make broken code compile.
- The shim must stay Unity-shaped. A compile error that Unity would also produce is a valid gate failure.
- When .NET and Unity's supported BCL surface disagree, the production-compatible surface wins.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.3 | 2026-09-04 | — | **Testing Strategy pipeline correction.** Makes `tools/run-tests-local.sh` the canonical developer/CI policy entry point; records exact owner-held RED handling, anchored NUnit pre-commit selection, persistent staged-index build cache, coverage settings, and the gated certified-host nightly boundary. Direct `run-gate.sh` use is now explicitly lower-level/debug only. |
| 1.2 | 2026-08-07 | — | Recorded that the full generated Linux gate can run in the Claude remote Ubuntu environment; still non-certifying. |
| 1.1 | 2026-07-13 | — | Certification-pin citations updated to the Unity 6000.4.9f1 target tuple; gate remained non-certifying. |
| 1.0 | 2026-06-12 | — | Initial gate: shim + generator + runner + quarantine; first full suite execution exposed multiple previously uncompiled defects. |
