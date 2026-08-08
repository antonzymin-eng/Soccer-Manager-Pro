# Code Standards & Style Guide Specification #20 — Section 7: Future Extensions

**File:** `docs/specs/code-standards/section-7.md`
**Purpose:** Records Stage 1 tooling deliverables and CI gates that activate when coding
begins, Stage 5+ rule extensions, permanent exclusions (style debates this spec refuses to
relitigate), and the deferred-decisions tracker (D1–D5).
**Created:** May 8, 2026
**Version:** 1.0.1
**Status:** DRAFT
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 7; `outline-mid.md` v1.2, §7.1–§7.5

---

## Table of Contents

- [7.1 Stage 1 Deliverables](#71-stage-1-deliverables)
- [7.2 Stage 1 CI Gates](#72-stage-1-ci-gates)
- [7.3 Stage 5+ Extensions](#73-stage-5-extensions)
- [7.4 Permanent Exclusions](#74-permanent-exclusions)
- [7.5 Deferred Decisions Tracker](#75-deferred-decisions-tracker)
- [7.6 Version History](#76-version-history)

---

## 7.1 Stage 1 Deliverables

Five tooling artifacts MUST be produced at Stage 1 project setup, before the first source
file is committed to `src/`. Each deliverable cites Spec #20 as its normative source.

| # | Deliverable | Trigger to activate | Acceptance criterion |
|---|---|---|---|
| D1-artifact | Numeric lint thresholds (line-length cap, method-length cap, nesting-depth limit; resolves Deferral D1 in §7.5) | `docs/tracking/certification-platform.md` fully pinned (OS, Unity LTS revision, C# version) AND first Stage 1 module profiled. Note: this triggers a *value selection*, not the activation of FR-CS-008 itself — FR-CS-008 (language-version pin) activates when the platform document resolves; threshold values follow once profiler baselines exist. | Threshold values committed to `.editorconfig` and cited in `src/CLAUDE.md` |
| D2-artifact | Roslyn analyzer ruleset (`.ruleset` or `.editorconfig` `[*.cs]` severity block) | First `src/` file committed | All Spec #20 Error-level FRs produce `error`-severity Roslyn diagnostics; `dotnet build` fails on any violation; Stage 1 analyzer IDs from Appendix D §D.2–§D.5 are populated |
| D3-artifact | `BannedSymbols.txt` populated | First `src/` file committed | File contains every symbol in Appendix D categories `det-banned` (§D.1) and `alloc-hot-path` (§D.2); `Microsoft.CodeAnalysis.BannedApiAnalyzers` package referenced in all game-loop `.csproj` files |
| D4-artifact | `.editorconfig` finalised | First `src/` file committed | Covers: indent style (4 spaces), brace style (Allman), `using` directive placement, `var` policy, namespace style (file-scoped), `sealed`-by-default suggestion; committed at repo root alongside `BannedSymbols.txt` |
| D5-artifact | First `src/CLAUDE.md` drafted | All 20 Stage 0 specs approved | Document covers: exact `src/` subdirectory paths, `.asmdef` GUIDs, build commands (`dotnet build`, `dotnet test`, Unity batch-mode), IDE configuration, constant catalogue concrete file paths; cites Spec #20 as normative source for every convention it concretises |

**Relationship to deferred decisions:** D1-artifact depends on D1 (numeric thresholds,
§7.5). D5-artifact is authorized by the root CLAUDE.md "Deferred: `src/CLAUDE.md`" gate —
it MUST NOT be created until all 20 specs are approved.

**Appendix D and Roslyn IDs:** The Appendix D tables in `appendices.md` include a
"Stage 1 analyzer ID (placeholder)" column. Populating those placeholders with real
diagnostic IDs is part of D2-artifact and D3-artifact. At Stage 0 the column values are
`TBD-ROSLYN-###`; they are replaced at Stage 1 once the analyzer packages are chosen.

---

## 7.2 Stage 1 CI Gates

Three gates enforce Spec #20 compliance in the CI pipeline. Each gate runs a specific
command; failure at that gate blocks the corresponding action.

| Gate | Trigger | Command | Failure behaviour |
|---|---|---|---|
| Pre-commit | Every `git commit` on a `src/` file | `dotnet format --verify-no-changes` | Commit rejected; developer runs `dotnet format` to auto-fix whitespace/indent violations before retrying |
| PR | Every pull-request open or push-to-PR | `dotnet build /p:TreatWarningsAsErrors=true` with Roslyn analyzer ruleset active | PR blocked; all Error-level Spec #20 diagnostics must be resolved before merge is permitted |
| Merge | Merge to `main` | Zero-allocation profiler test on game-loop assemblies (Unity batch-mode, managed-heap snapshot) | Merge blocked; any non-zero allocation in the 60 Hz physics path (FR-CS-066) must be eliminated before merge |

**Stage 0 status:** None of these gates can be activated at Stage 0 — `src/` is empty and
the toolchain is not configured. The commands above are normative targets; they are
recorded here so the Stage 1 setup author has a concrete checklist.

**Pre-commit hook setup note:** The pre-commit gate requires a Git pre-commit hook or
Husky configuration pointing at `dotnet format --verify-no-changes`. The exact hook
installation command is deferred to `src/CLAUDE.md` (D5-artifact, §7.1).

**Merge gate dependency:** The zero-allocation merge gate depends on the host platform
being pinned in `docs/tracking/certification-platform.md`. Until that document is fully
populated (see CLAUDE.md Open Issues — "Stage 0 host platform pin"), the merge gate cannot
produce a reproducible baseline. The gate MUST NOT be marked active until the platform pin
resolves (see also FR-CS-008 — INACTIVE status in §2.2.1).

---

## 7.3 Stage 5+ Extensions

The following rules are intentionally deferred beyond Stage 0 and Stage 1. They are listed
here so future maintainers understand why they are absent from the current rule set.

### Fixed64 Enforcement (Stage 5 trigger: Spec #9 ships)

CLAUDE.md (Open Issues — "Fixed64 stage scope decision") establishes that Fixed64
migration is a **Stage 5+** concern. At Stage 5, when cross-platform multiplayer is added:

- FR-CS-072 (`double` prohibition) will be reviewed; if Fixed64 replaces `float` on the
  physics path, the rule expands to cover `double` and `float` in game-state assemblies.
- Spec #9 (the Fixed64 library spec) will publish the authoritative `Fixed64` type. Spec
  #20 §3.7 will gain a cross-reference to Spec #9 at that point.
- Appendix D `det-banned` category will expand to include `System.Single` and
  `System.Double` for game-state assemblies (with a parallel `det-required` entry for
  the Fixed64 arithmetic type).
- The Roslyn analyzer ruleset (D2-artifact) will add diagnostics for floating-point
  literals in game-state namespaces.

### Cross-Platform Bit-Exact Parity Rules (Stage 5 trigger: multiplayer milestone)

Single-machine determinism (replay, save/load, debug rewind) is achieved at Stage 0 via
state snapshots, not deterministic arithmetic — per CLAUDE.md "When Writing Code." At
Stage 5, cross-platform bit-exact parity becomes a hard requirement. Additions at that
point:

- FMA and denormals-are-zero compiler flags locked per-platform (currently `_TBD_` in
  `certification-platform.md`). FR-CS-040 itself is active at Stage 0 (the default-ban
  applies); what unblocks here is FR-CS-040's *override pathway* — the platform-pin
  precondition that, together with lead-developer sign-off, allows FMA opt-in.
- Platform-specific known-answer test (KAT) suites added to the merge gate.

### `unsafe` and SIMD Intrinsic Policy Revisit (Stage 2+ trigger: performance profiling)

The current Stage 0 rule prohibits `unsafe` code without lead-developer sign-off
(FR-CS-010). SIMD intrinsics (`System.Runtime.Intrinsics`) are not addressed. At Stage 2,
after profiler baselines exist, the policy SHOULD be revisited:

- If a hot-path system cannot meet its microsecond budget without SIMD, an exception
  process (matching the format in §2.2 Exception block) is added for that system.
- Any `unsafe` expansion requires a dedicated security and memory-safety review.

---

## 7.4 Permanent Exclusions

The following topics are **permanently excluded** from Spec #20 and from any future
amendment. They represent decisions that were made once and are not open for
re-specification.

### Style Debates Already Decided

| Topic | Decision | Where decided | Rationale for closure |
|---|---|---|---|
| Indentation: tabs vs spaces | 4 spaces | §3.1.4 | Matches C# community standard and `.editorconfig` default; reopening would break all existing diffs |
| Brace style | Allman (opening brace on new line) | §3.1.4 | Matches Microsoft C# Coding Conventions and the project's existing spec-file conventions |
| Line-ending style | LF (Unix) | §3.1.4 / `.editorconfig` | Git `autocrlf` handles Windows dev machines; re-debating yields no value |

These rules are frozen. A PR that reopens indentation or brace style will be rejected
without review. The goal is to eliminate bikeshedding permanently, not to invite yearly
revisits.

### Frameworks and Libraries Not Mandated

Spec #20 deliberately avoids mandating:

- **IoC / dependency injection container:** The Stage 0 physics layer uses struct-based
  composition with no service locator or DI framework. If a DI framework is introduced at
  Stage 3+ (UI layer), that choice belongs to the Stage 3 spec, not Spec #20.
- **Logging framework:** Unity's `Debug.Log` family is the Stage 0 baseline. A structured
  logging framework (Serilog, NLog, etc.) is a Stage 3+ concern. Spec #20 governs
  comment and documentation conventions, not runtime logging.
- **Serialization library:** JSON / binary serialization is out of scope for the physics
  layer. Spec #16 (Deterministic Simulation) owns the canonical serialization contract.

These exclusions prevent Spec #20 from accumulating framework-specific rules that would
need to change whenever a library is swapped.

---

## 7.5 Deferred Decisions Tracker

Five decisions are explicitly deferred from Stage 0. Each entry records the deferral
statement, the trigger that allows (or requires) the decision to be made, and the owner.

| ID | Decision deferred | Deferral statement | Trigger to revisit | Owner |
|---|---|---|---|---|
| D1 | Numeric lint thresholds (line-length cap, method-length cap, nesting-depth limit) | Thresholds are deferred per KD-5 (§1.3): no source code exists at Stage 0, so empirical baselines cannot be established. Resolution is gated on (a) FR-CS-008 activation — the C# language version pinned in `certification-platform.md` — and (b) the first Stage 1 module reaching a profiled baseline. No placeholder values are inserted — a wrong threshold is worse than no threshold. | `certification-platform.md` fully pinned (C# version, Unity LTS, compiler flags) AND first Stage 1 module profiled per §5.3 | Lead developer + Stage 1 setup author |
| D2 | Test framework choice | Spec #19 (Testing Strategy) owns test-framework selection. Spec #20 §3.9.4 (test-fixture carve-out) is intentionally framework-agnostic to avoid a circular dependency. | Spec #19 reaches `IN REVIEW` status in `SPEC_INDEX.md` | Spec #19 author |
| D3 | Build commands, IDE setup, assembly GUIDs | These are concrete implementation details that depend on the Unity LTS version and project directory structure chosen at Stage 1. `src/CLAUDE.md` (D5-artifact) is the home for this information; it MUST NOT be created until all 20 specs are approved. | All 20 Stage 0 specs approved | Stage 1 setup author |
| D4 | Fixed64 enforcement rules | Stage 0 uses `float`. Fixed64 migration is Stage 5+. Spec #9 will define the Fixed64 library; Spec #20 §3.7 will gain a cross-reference at that point. See §7.3 for detail. | Spec #9 reaches `APPROVED` status | Spec #9 author → Spec #20 amendment author |
| D5 | Concrete C# language version pin | FR-CS-008 is gated on `certification-platform.md`. The C# language version determines which features are permissible (e.g., `required` members, primary constructors). Spec #20 rules are written to be forward-compatible with C# 10–12; the pin specifies the exact floor. | `docs/tracking/certification-platform.md` row "C# language version" is non-`_TBD_` | Lead developer |

**Resolution protocol:** When a deferred decision is resolved, the owner MUST:
1. Update this table row (change "deferred" entry to a resolution summary with date).
2. Activate or amend any INACTIVE FRs that depended on the decision (see §2.2.1 FR-CS-008
   and its override conditions).
3. Append a version history entry to every section file changed.
4. Update `docs/tracking/PROGRESS.md` with the milestone.

---

## 7.6 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 8, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 7 and `outline-mid.md` v1.2 §7.1–§7.5. | — |
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fixes (audit finding H-02): corrected three stale FR-CS-### identifiers — §7.3 `double` cite FR-CS-039 → FR-CS-072; §7.3 `unsafe` cite FR-CS-042 → FR-CS-010; §7.3 FMA paragraph clarified that FR-CS-040 is active at Stage 0 and only its override pathway is gated on the platform pin. §7.1 D1-artifact and §7.5 D1 rewordings: D1 deferral is governed by KD-5 (no Stage 0 code to baseline against), with FR-CS-008 activation as a precondition, not the source of the threshold values themselves. | — |

---

*End of Section 7 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
