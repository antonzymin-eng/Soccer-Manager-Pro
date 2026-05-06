# Pressing AI Specification #13 — Outline

## Purpose
Define readable, coordinated pressing behaviors that are effective without devolving into chaos.

## Scope
Press triggers, coordinated execution, lane denial, stamina/discipline costs, reset logic, and exploit-resistance tests.

## Section Plan
- Section 1 — Trigger catalog (bad touch, backward pass, sideline trap, weak receiver).
- Section 2 — Individual versus coordinated press roles and responsibilities.
- Section 3 — Cover shadows, passing-lane denial, and trap timing.
- Section 4 — Stamina/discipline costs and anti-chaos guardrails.
- Section 5 — Disengage and reset logic after failed press.
- Section 6 — Parameterization and tactical-style variants.
- Section 7 — Unit tests for trigger detection and role behavior.
- Section 8 — Integration tests for exploit resistance and tactical readability.
- Section 9 — Approval checklist.
- Appendices — Trigger telemetry and troubleshooting playbook.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #13 status in `SPEC_INDEX.md`: NOT STARTED. Pass Mechanics #5 is
  **SUSPENDED** — consuming its events introduces a coupling risk.
- Approved upstream: First Touch #4, Perception #7, Decision Tree #8.
- CLAUDE.md fatigue convention: `0=rested, 1=fatigued`. Inverted before in Pass
  Mechanics FR-02.

### Findings

1. **[H] Missing metadata header.** No Created/Updated, version, status,
   dependencies, downstream consumers, or estimated effort. Compare Shot Mechanics
   #6 outline header.

2. **[H] Section plan misaligned with CLAUDE.md template.** Failure modes,
   data structures, and FRs belong in §2; formulas in §3; architecture in §4;
   tests in §5; performance in §6; future extensions in §7; references in §8.
   Current outline routes parameterization to §6 (perf slot) and integration
   tests to §8 (references slot), leaving no references section. Re-map.

3. **[H] Trigger-detection upstream sources undeclared.**
   - "bad touch" → First Touch Mechanics #4 quality output.
   - "backward pass" → Pass Mechanics #5 directional event (currently SUSPENDED;
     this is a real risk to flag).
   - "sideline trap" → ball-state plus pitch geometry from Ball Physics #1.
   - "weak receiver" → Perception #7 visibility/attribute lookup.
   None are cross-referenced. Without this, triggers will be redefined locally
   and drift from canonical event semantics.

4. **[H] Pass Mechanics #5 SUSPENDED-status risk unacknowledged.** Backward-pass
   detection is a primary trigger; the spec it depends on is currently suspended
   awaiting re-review. Outline must either gate Spec #13 draft completion on
   #5 re-approval or define a stable subset interface that does not depend on
   suspended sections.

5. **[H] Cover-shadow / passing-lane denial requires Perception #7.** §3 names
   visibility cones implicitly. Without citing Perception #7, the lane-denial
   geometry will be re-derived locally, repeating the ERR-004 phantom-interface
   pattern.

6. **[M] Determinism plan absent.** Pressing decisions are authoritative AI
   state. Per Deterministic Simulation #16, every press-trigger evaluation is
   a draw site that must use `DeterministicRngService` with a registered ID
   (#16 §4.5), and parallel evaluations must commit at deterministic merge
   barriers (#16 §3.3). Outline does not invoke #16.

7. **[M] Stamina/fatigue convention not pre-committed.** §4 "stamina costs"
   must explicitly use `0=rested, 1=fatigued`. The convention has been
   inverted before; a pre-commit prevents the bug class from reappearing.

8. **[M] Boundary with Defensive AI #14 and Positioning AI #12 unstated.**
   "Coordinated press roles" overlaps with Spec #14 (zonal/man assignments)
   and Spec #12 (formation anchors). Spec #14 outline already plans a §4
   "handoff rules between defensive and pressing systems" — this outline
   must mirror that handoff or one side will go undefined.

9. **[M] Tick-rate split unstated.** Triggers and role assignments live on
   the 10 Hz tactical loop; physical pursuit is steering on the 60 Hz physics
   loop (Agent Movement #2). Outline must declare the boundary.

10. **[M] "Anti-chaos guardrails" undefined.** §4 promises guardrails but
    gives no definitional anchor. Without acceptance criteria the §9 Approval
    Checklist risks ERR-005-style fabricated verification (Pass Mechanics
    history). Define measurable invariants (e.g., max simultaneous pressers
    per ball-side third, max distance-from-anchor, minimum-shape constraint).

11. **[M] "Exploit resistance" tests undefined.** §8 names exploit resistance
    but does not enumerate exploits. Mandatory: list at least the canonical
    pressing-AI exploit set (long-ball-over-pressers, switch-of-play to
    isolated zone, drag-and-bounce one-twos, goalkeeper-as-pivot) so
    Approval can verify coverage.

12. **[L] Constant-tag policy not invoked.** §6 "parameterization" must use
    `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]` tags per CLAUDE.md.

13. **[L] No event production declared.** Pressing likely emits
    `PressTriggeredEvent` / `PressDisengagedEvent` for telemetry and Defensive
    AI #14 handoff. Enumerate or defer.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template.
- Add Upstream Dependencies table naming First Touch #4, Pass Mechanics #5
  (with SUSPENDED-risk note), Perception #7, Decision Tree #8, Agent Movement
  #2, Deterministic Simulation #16.
- Define "anti-chaos" invariants with measurable thresholds.
- Enumerate canonical exploit corpus for §8.
