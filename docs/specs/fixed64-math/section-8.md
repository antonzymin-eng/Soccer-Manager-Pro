# Fixed64 Math Library Specification #9 — Section 8: Integration, API Surface, and Rollout Guidance

## 8.1 Ownership and Mandatory Usage
- Fixed64 MUST be mandatory in simulation-critical gameplay, physics, collision, and replay paths.

## 8.2 API Naming and Compatibility
- Public families: `Checked*`, `Saturating*`, `Unchecked*`.
- Compatibility policy: semantic versioning + deprecation window of two minor releases.

## 8.3 Migration Playbook
- Recommended rollout phases: shadow mode, dual-path assertions, phased cutover, cleanup.

## 8.4 Interop Contracts
- Integration contracts MUST define shared rounding, serialization, and error-handling expectations per subsystem.

## 8.5 Maturity Gates
- Reference implementation stages: draft -> beta -> production-ready; each gate requires vector/determinism/perf sign-off.

## 8.6 Documentation and Onboarding
- Downstream teams MUST receive API docs, migration examples, and failure-mode matrix references before adoption.

## 8.7 Version History
- v0.1 (2026-05-01): Initial draft aligned to outline Section 8.
