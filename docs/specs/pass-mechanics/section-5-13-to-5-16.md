## 5.13 Acceptance Criteria Summary

The following criteria must all be satisfied before Section 5 (and the full specification)
can be marked as passing the quality gate.

### 5.13.1 Unit Test Acceptance

| Criterion | Required | Blocking |
|-----------|----------|---------|
| All PT- tests pass | 8/8 | Yes |
| All PV- tests pass (non-blocked) | 10/12 (2 ERR-007-blocked) | Yes |
| All LA- tests pass | 8/8 | Yes |
| All SV- tests pass | 8/8 | Yes |
| All PE- tests pass | 10/10 | Yes |
| All TR- tests pass | 16/16 | Yes |
| All WF- tests pass (non-blocked) | 0/6 (all ERR-007-blocked) | Blocked by ERR-007 |
| All PSM- tests pass | 6/6 | Yes |
| All EC- tests pass | 8/8 | Yes |

When ERR-007 is resolved, WF- and blocked PV- tests must be completed before approval.

### 5.13.2 Integration Test Acceptance

| Criterion | Required | Blocking |
|-----------|----------|---------|
| All IT- tests pass (non-blocked) | 11/12 (1 ERR-008-blocked) | Yes |
| IT-012 determinism test passes | 1/1 | Yes — CRITICAL |

When ERR-008 is resolved, IT-004 must be completed.

### 5.13.3 Validation Scenario Acceptance

| Criterion | Required | Blocking |
|-----------|----------|---------|
| All VS- expected outputs within tolerance | 6/6 | Yes |
| No VS- output produces NaN or Infinity | 6/6 | Yes |
| Football realism judgement: peer review confirms plausibility | Lead Developer sign-off | Yes |

### 5.13.4 Coverage and Performance Acceptance

| Criterion | Target | Blocking |
|-----------|--------|---------|
| Line coverage on §3.x code | > 85% | Yes |
| `EvaluatePass()` p99 ≤ 0.25ms | Profiler result | Yes |
| Full unit suite runtime | < 15 seconds | No (advisory) |

---

## 5.14 Test Execution Plan

### 5.14.1 Test Ordering

Tests must execute in the following order to enable efficient failure diagnosis. Later
phases depend on earlier phases; a failure in a unit test does not block *execution* of
integration tests for diagnostic purposes, but it is blocking for *sign-off*.

```
Phase 1: EC- (edge cases / robustness)         — validates defensive coding first
Phase 2: PT- (pass type classification)         — validates profile lookup foundation
Phase 3: PV- (pass velocity)                    — validates primary output calculation
Phase 4: LA- (launch angle)                     — depends on correct velocity output
Phase 5: SV- (spin vector)                      — depends on correct velocity output
Phase 6: PE- (pass error)                       — isolated sub-system
Phase 7: TR- (target resolution)                — isolated sub-system
Phase 8: WF- (weak foot)                        — isolated sub-system [ERR-007-BLOCKED]
Phase 9: PSM- (state machine)                   — depends on all §3.x functions
Phase 10: IT- (integration)                     — all unit tests must pass first
Phase 11: VS- (validation scenarios)            — full pipeline must pass first
```

### 5.14.2 Execution Cadence

| Cadence | Tests Run | Trigger |
|---------|-----------|---------|
| On every code change | All unit tests (EC- through PSM-) | Developer — local, pre-commit |
| On pull request | All unit + all IT- tests | CI pipeline |
| On milestone | Full suite including VS- | Manual milestone gate |
| Nightly | IT-012 determinism + performance profiling | Scheduled CI job |
| After any constant tuning | Full unit suite | Manual — document baseline SHA |

### 5.14.3 Failure Response Protocol

**Level 1 — Formula error (PV-, LA-, SV-, PE-):** HALT. Fix before any integration work.
These failures contaminate all downstream tests and invalidate validation scenarios.

**Level 2 — Classification or target error (PT-, TR-):** HALT. These are gateway functions;
errors here propagate through every pass execution.

**Level 3 — State machine error (PSM-):** Fix before integration testing. PSM-006
(double ApplyKick) is Level 1 severity despite being in the PSM category.

**Level 4 — Integration failure (IT-):** May run non-dependent IT- tests for diagnosis.
Must resolve before milestone gate. IT-012 determinism failure escalates immediately to
Level 1.

**Level 5 — Validation scenario deviation (VS-):**
- Within 2× tolerance: investigate formula calibration.
- Within 10× tolerance: investigate player archetype attribute values in fixtures.
- Exceeds 10× tolerance: treat as Level 1 formula error.

**IT-012 determinism failure is a CRITICAL halt.** Do not proceed with any further
development until root cause is identified and resolved. Possible causes: floating-point
operations with non-deterministic ordering, unintentional mutable state sharing between
evaluations, platform-specific math library differences. See `DeterministicTestHarness.cs`
for diagnostic procedures.

### 5.14.4 Regression Strategy

All 82 unit tests constitute the regression suite. Before any constant or formula tuning:

1. Record baseline pass metrics (commit SHA, test results, VS- outputs).
2. Make tuning changes.
3. Run full unit suite — confirm no regressions.
4. Re-run VS- scenarios — document output delta.
5. Record new baseline with tuning rationale in `PassConstants.cs` comments.

Never tune a constant without running the full suite afterward. Constants are not
isolated; changing `BASE_ERROR(Ground)` affects PE-001, PE-002, PE-003, VS-001, and VS-006.

---

## 5.15 Section Summary

Section 5 defines **100 tests** across 11 categories covering the full Pass Mechanics
system:

- **82 unit tests** verify individual formula correctness, boundary conditions, and edge
  case handling in isolation.
- **12 integration tests** verify correct cross-system data flow across all five interface
  contracts defined in Section 4, including the CRITICAL IT-012 determinism regression test.
- **6 validation scenarios** verify that the complete pipeline produces football-realistic
  outputs for canonical player archetypes, serving as the ground-truth regression suite
  for all future formula and constant tuning.

**Current blockers:**
- 8 tests are `[ERR-007-BLOCKED]` (WF-001–WF-006, PV-006, WF-005) pending `KickPower`,
  `WeakFootRating`, `Crossing` attribute confirmation from Agent Movement spec amendment.
- 1 test is `[ERR-008-BLOCKED]` (IT-004) pending `PossessingAgentId` design resolution.

These blockers do not prevent the remaining 91 tests from being written and executed.
Resolution of ERR-007 and ERR-008 before §3.2 and §3.7 finalisation is the recommended
path to clearing all blocks before implementation begins.

---

## 5.16 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 20, 2026, 11:30 PM PST | Claude (AI) / Anton | Initial draft. 100 tests across 11 categories. ERR-007 and ERR-008 blocks carried forward. All VS- scenarios include football realism narrative check. |
| 1.1 | February 21, 2026 | Claude (AI) / Anton | 6 test corrections from Appendix B numerical verification. LA-006 expectation inverted (CRITICAL — physics was correct, test was wrong). PV-004, PV-005, PV-010 conditions corrected to align with AM-003-001 V_OFFSET formula. PE-001 uses explicit ELITE_ERROR constant instead of undefined MIN_ERROR. WF-003 corrected from Rating=20 to Rating=5 (WeakFootRating is [1,5] scale). WF-004 step values corrected to match [1,5] range. LA-001 bounds corrected from [0°,3°] to [2°,5°]. |
| 1.2 | March 25, 2026 | Claude (AI) / Anton | Post-audit fix: §5.1.5 performance targets aligned with §6 — replaced impossible mean<0.15ms with p95<0.05ms, p99<0.10ms, peak-frame<0.25ms (Min-02). |

---

*End of Section 5 — Pass Mechanics Specification #5*

*Next: Section 6 — Performance Analysis*
