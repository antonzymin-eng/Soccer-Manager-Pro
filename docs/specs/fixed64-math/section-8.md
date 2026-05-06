# Fixed64 Math Library Specification #9 — Section 8: Integration, API Surface, and Rollout Guidance

## 8.1 Ownership and Mandatory Usage (Stage-Gated)

Fixed64 adoption is **gated by development stage** per `master-development-plan.md` and the April 26, 2026 Stage-5+ scope decision recorded in CLAUDE.md OPEN ISSUES.

- **Stage 0 — Stage 4 (Pre-multiplayer).** Simulation-critical paths (gameplay, physics, collision, AI, replay) MUST use `float`. Fixed64 MUST NOT be required in these paths. Single-machine determinism (replay, save/load, debug rewind) is achieved via state snapshots, not deterministic arithmetic. The currently-approved physics specs (#1 Ball Physics, #2 Agent Movement, #3 Collision System, #4 First Touch, #6 Shot Mechanics, #7 Perception System, #8 Decision Tree) all conform to this rule.
- **Stage 5+ (Cross-platform multiplayer).** Fixed64 MUST be mandatory in simulation-critical gameplay, physics, collision, AI, and replay paths; this is the cutover point where cross-platform bit-exact parity becomes a release-blocking quality gate. The migration playbook in §8.3 governs that transition.
- **All stages.** Fixed64 MAY be used opportunistically for code that already benefits from its semantics (e.g., serialization keys, replay event payloads), provided it does not perturb approved float-based subsystems.

This staging is the single normative source for "where Fixed64 is required". Subsystem specs MUST NOT impose Fixed64 requirements that contradict the table above without an approved spec amendment.

Cross-references: `XC-009-001` (this clause) is consumed by Deterministic Simulation #16 §3.x and by `master-development-plan.md` §2.3 / §8 risk mitigation. See §8.7 for the full XC table.

## 8.2 API Naming and Compatibility
- Public families: `Checked*`, `Saturating*`, `Unchecked*` — see §2.8 for operator binding.
- Compatibility policy: semantic versioning + deprecation window of two minor releases.
- Major-version bumps MUST cite the breaking change in the Fixed64 changelog and propagate to consumers via the migration playbook in §8.3.

## 8.3 Migration Playbook (Stage 4 → Stage 5)
- Recommended rollout phases: shadow mode (run float and Fixed64 paths in parallel, log divergence), dual-path assertions (gate on bounded divergence per subsystem), phased cutover (subsystem by subsystem, replay-corpus-validated), cleanup (remove float paths after a stabilization window).
- Deferred dependencies for Fixed64 v1.0 final approval: `#9` itself (this spec), `#16` Deterministic Simulation (`IN PROGRESS`), `#17` Event System (`NOT STARTED`), `#18` Performance Optimization Strategy (`NOT STARTED`), `#19` Testing Strategy (`NOT STARTED`).

## 8.4 Interop Contracts (Typed)
- Integration contracts MUST define shared rounding, serialization, and error-handling expectations per subsystem.
- Per-subsystem typed cross-references:
  - `XC-009-002` ↔ Ball Physics #1: rounding mode for ball-state snapshots is nearest-even Q32.32.
  - `XC-009-003` ↔ Agent Movement #2: position/velocity serialization uses raw int64 little-endian per §1.6.
  - `XC-009-004` ↔ Collision System #3: contact-impulse arithmetic uses `CheckedMulNearestEven` per §2.3.1.
  - `XC-009-005` ↔ Decision Tree #8: deterministic RNG seed material is encoded as int64 raw, never as canonical-text float.
  - `XC-009-006` ↔ Deterministic Simulation #16: digest layout per Appendix E binds this spec to #16 §3.2.5; reciprocal `XC-016-NNN` filed in #16's pending revision (see CLAUDE.md OPEN ISSUE on `ERR-016-002`).
  - `ERR-009-001` (this spec, filed against Pass-1 review): mul/div pseudocode bit-exactness — closed by §2.3 v0.3.

## 8.5 Maturity Gates
- Reference implementation stages: draft -> beta -> production-ready; each gate requires vector/determinism/perf sign-off.
- v1.0 release-ready criteria are enumerated in §9 (Approval Checklist).

## 8.6 Documentation and Onboarding
- Downstream teams MUST receive API docs, migration examples, and failure-mode matrix references before adoption.
- Onboarding material MUST cite the Stage 0 / Stage 5+ split from §8.1 prominently.

## 8.7 Cross-Reference Index (Normative)

| ID | Direction | Target | Subject |
|---|---|---|---|
| `XC-009-001` | this spec → consumers | `master-development-plan.md`, #16 | Stage-gated mandatory usage |
| `XC-009-002` | this spec → #1 Ball Physics | rounding for snapshots |
| `XC-009-003` | this spec → #2 Agent Movement | int64 LE serialization |
| `XC-009-004` | this spec → #3 Collision System | nearest-even mul for impulses |
| `XC-009-005` | this spec → #8 Decision Tree | RNG seed encoding |
| `XC-009-006` | this spec ↔ #16 Deterministic Sim | digest layout reciprocal |
| `FM-009-001` | this spec § | nearest-even rounding formula (§2.3) |
| `FM-009-002` | this spec § | sqrt paired-bit algorithm (§3.1) |
| `FM-009-003` | this spec § | CORDIC sin/cos/atan2 (§3.2) |
| `EC-009-001` | this spec § | `CheckedNegate(FIXED64_MIN)` overflow |
| `EC-009-002` | this spec § | `CheckedDiv(_, 0)` precedence (§2.2) |
| `EC-009-003` | this spec § | `Float -> Fixed` non-finite (§4.2) |
| `ERR-009-001` | resolved | mul/div pseudocode negative-operand bug (closed §2.3 v0.3) |

## 8.8 Version History
- v0.2 (2026-05-06): Made §8.1 stage-gated (Stage 0–4 float / Stage 5+ Fixed64); added typed cross-reference index in §8.7; named deferred dependencies in §8.3.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 8.
