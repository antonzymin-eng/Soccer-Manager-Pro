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
| `owner-held-red.txt` | Optional owner-held failing acceptance predicates. **Not quarantine.** A comments-only file means no exception is configured; configured rows execute separately and must still fail at their recorded diagnostic baseline. |
| `verify-owner-held-red.py` | Requires one exact test identity, failed outcome, recorded diagnostic tokens, no extra results, and expected runner exit. Unexpected green/drift/ambiguity blocks. |
| `coverage.runsettings` | Coverlet/XPlat coverage configuration used by PR/nightly policy modes. |
| `precommit.runsettings` | NUnit pre-commit selection. Excludes taxonomy prefixes only when they occur at the start of the **method name** (`^int_`, `^sim_`, `^e2e_`), avoiding `FullyQualifiedName` substring over-exclusion. |
| `run-gate.sh` | Lower-level generated-project executor. Accepts explicit arguments only; inherited filter/owner/coverage environment controls are rejected. |
| `check_evidence_manifests.py` | Verifies the evidence-integrity contract registry and canonical SHA-256 manifests under `docs/tracking/evidence/`. |
| `check_branch_ancestry.py` | Local branch-cleanup ancestry guard. Refuses ancestry claims from shallow history; full local history or authoritative remote/API comparison is required. |

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

No owner-held RED is currently configured. On September 20, 2026 the owner retired `sim_match_engine_close_chance` from this ledger after the predicate became green; it now runs in the ordinary blocking sweep. This retirement does not turn owner-held RED into quarantine or remove the generic mechanism.

PR/nightly policy modes, when one or more rows are configured:

1. exclude each configured exact `Name` from the ordinary blocking pass;
2. run the configured owner-held `Name` set separately;
3. parse its TRX;
4. require exactly one matching result per configured row and the recorded diagnostic tokens;
5. fail if a configured row passes, drifts, is missing/ambiguous, returns extra tests, or exits abnormally.

With a comments-only ledger, no exclusion is applied and the dedicated owner-held stage is skipped; the ordinary sweep owns every result.

The diagnostic contract is proven only when the real PR gate executes successfully; a unit fixture proves verifier behavior, not the live test message format.

## Evidence/governance utilities

Two repository-governance utilities live here because their failure modes affect whether retained
evidence can be trusted or deleted safely.

`check_evidence_manifests.py` owns the repository's **evidence-integrity contract registry** under
`docs/tracking/evidence/`. Every tracked top-level evidence directory must be registered as exactly one
of:

- `SHA256SUMS`: a **complete tracked-file manifest**. Every Git-tracked regular file recursively below
  the manifest directory, except the manifest itself, must be listed exactly once and match its
  SHA-256 digest. Ignored/untracked files are intentionally outside this contract, so local
  `.DS_Store`, extracted archives, and other scratch material cannot make required CI red. Symlinks
  are not permitted in a full-manifest directory.
- `artifact-SHA256SUMS`: an **artifact-scoped manifest**. Every listed file must exist and match its
  digest, but unrelated sibling documentation is deliberately outside that manifest's digest claim.
- an explicitly registered **external verifier**. Current examples are W12
  (`check_w12_evidence.py`) and the PR #416 ref archive (`check_pr416_evidence_refs.py`). This
  registry verifies that the named owner still exists; it does not claim those external contracts
  have identical enforcement strength or duplicate their semantics.

A new tracked evidence directory with no registered integrity contract fails closed. A
`*SHA256SUMS*`-style file, case-insensitively, also fails unless it uses a canonical contract name or
is explicitly allowlisted; the current `TRX-SHA256SUMS` exception is owned by
`pr420-evidence.py` because its rows describe members inside the committed archive rather than
filesystem coverage. The current standalone root evidence note is likewise explicitly registered.

This does **not** mean every evidence byte is covered by a SHA-256 manifest. The repository-wide
property is registration of the owning integrity mechanism; digest coverage depends on each
directory's declared contract. The tooling unit suite executes this registry and all canonical
SHA-256 manifests against the committed repository inside required `Spec hygiene checks`.

Complete-manifest scope comes from `git ls-files`. When invoked inside a Git worktree, failure to
obtain the tracked-file set is an error; the checker does not silently fall back to filesystem
scanning. Filesystem scanning is used only for non-Git temporary fixtures.

To regenerate a complete manifest from tracked files, run from the manifest directory:

```bash
git ls-files -z -- . | grep -zv '^SHA256SUMS$' | sort -z | xargs -0 sha256sum > SHA256SUMS
```

The manifest grammar is intentionally strict: lowercase SHA-256, two spaces, then the relative path.

`check_branch_ancestry.py` is the local branch-cleanup guard:

```bash
python3 tools/dotnet-ci/check_branch_ancestry.py --repo . --ancestor <branch-or-tip> --descendant main
```

It checks `git rev-parse --is-shallow-repository` **before** resolving or comparing refs. A shallow
checkout exits **3** with a guard error and makes no merged/unmerged/deletable claim; exit **2** remains
reserved for command-line usage errors. The safe alternatives are to unshallow/obtain complete local
history or to use an authoritative remote/API comparison. Fetching all branch refs without removing
the shallow boundary is not sufficient. This is a procedural guard, not a server-side branch-deletion
control; the owning tracking issue therefore remains NARROWED rather than closed.

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
| Governance addendum | 2026-09-21 | — | Adds the evidence-integrity contract registry/checker and the shallow-history ancestry guard; records tracked-file scope, explicit external-verifier boundaries, fail-closed Git-scope behavior, and ancestry exit-code semantics. |
| Policy addendum (retirement) | 2026-09-20 | — | Owner decision retires the final configured owner-held row, `sim_match_engine_close_chance`, without changing its predicate or bounds. Documents the already-unit-tested empty-ledger terminal state: ordinary sweep unfiltered, dedicated stage skipped. |
| Policy addendum | 2026-09-04 | — | **Testing Strategy pipeline correction.** Makes `tools/run-tests-local.sh` the canonical developer/CI policy entry point; records exact owner-held RED handling, anchored NUnit pre-commit selection, persistent staged-index build cache, coverage settings, and the gated certified-host nightly boundary. This operational correction intentionally does not advance the historical gate-document version key, because live open-issue records cite the Aug-7 v1.2 revision as dated evidence. |
| 1.2 | 2026-08-07 | — | Recorded that the full generated Linux gate can run in the Claude remote Ubuntu environment; still non-certifying. |
| 1.1 | 2026-07-13 | — | Certification-pin citations updated to the Unity 6000.4.9f1 target tuple; gate remained non-certifying. |
| 1.0 | 2026-06-12 | — | Initial gate: shim + generator + runner + quarantine; first full suite execution exposed multiple previously uncompiled defects. |
