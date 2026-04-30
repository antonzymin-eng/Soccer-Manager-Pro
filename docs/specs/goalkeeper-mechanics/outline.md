# Goalkeeper Mechanics Specification #11 — Outline

## Purpose
Define deterministic goalkeeper behavior that balances realism, responsiveness, and tactical coherence.

## Scope
State machine, shot reactions, save outcomes, positional heuristics, distribution logic, and failure/performance validation.

## Section Plan
- Section 1 — Goalkeeper state machine (set, shuffle, rush, dive, recover, distribute).
- Section 2 — Shot reaction logic (trajectory read, reaction delay, reachability checks).
- Section 3 — Save outcomes (catch/parry/deflect/spill) and rebound placement rules.
- Section 4 — 1v1 behavior, cross claims, near/far-post positioning heuristics.
- Section 5 — Post-save distribution choices (throw/roll/kick) with risk model.
- Section 6 — Performance budgets and allocation constraints.
- Section 7 — Failure modes and robustness matrix.
- Section 8 — Integration scenarios for match-feel and tactical consistency.
- Section 9 — Approval checklist.
- Appendices — Tuning tables and diagnostic telemetry definitions.
