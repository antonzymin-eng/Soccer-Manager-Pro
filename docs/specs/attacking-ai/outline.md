# Attacking AI Specification #15 — Outline

## Purpose
Define chance-creation behaviors that express tactical identity while maintaining transition discipline.

## Scope
Pattern execution, support heuristics, final-third decisions, overload logic, defensive transition, and quality metrics.

## Section Plan
- Section 1 — Chance-creation patterns (overlaps, underlaps, cutbacks, third-man runs).
- Section 2 — Support distance/angle heuristics around the ball carrier.
- Section 3 — Final-third preferences (cross, pass, shoot, recycle).
- Section 4 — Overload and isolation creation; weak-side exploitation.
- Section 5 — Transition-to-defense behavior on possession loss.
- Section 6 — Team-style modifiers and tactical instruction hooks.
- Section 7 — Unit tests for pattern selection and support logic.
- Section 8 — Quality tests for chance volume, quality, and tactical identity.
- Section 9 — Approval checklist.
- Appendices — Chance taxonomy and tuning presets.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #15 status in `SPEC_INDEX.md`: NOT STARTED. Approved upstream:
  Agent Movement #2, Perception #7, Decision Tree #8, Shot Mechanics #6.
- Final-action selection (cross/pass/shoot) is already owned by Decision Tree
  #8 (approved). xG / shot quality is Stage 1+ per Shot Mechanics #6 §7.
- Parameter-based physics rule (CLAUDE.md): no `KickType`/`ShotType`/`PassType`
  enums in the physics layer.

### Findings

1. **[H] Missing metadata header.** Same gap as siblings. Add per Shot
   Mechanics #6 outline header.

2. **[H] Section plan misaligned with CLAUDE.md template.** §6 "team-style
   modifiers" sits in the §6 performance slot per template; §8 "quality
   tests" sits in the §8 references slot. No references section. Re-map.

3. **[H] §3 collides with Decision Tree #8.** "Final-third preferences
   (cross, pass, shoot, recycle)" is exactly the action-selection territory
   already approved in Spec #8. Either Spec #15 *consumes* Decision Tree
   output (and its §3 becomes "support context fed to #8") or it owns
   final-third action selection (and #8 must be amended). Cannot do both.
   This is the highest-risk finding because Spec #8 is APPROVED and any
   redefinition triggers a renumbering-cascade-class change to a frozen spec.

4. **[H] Pattern-template enum risk.** §1 "overlaps, underlaps, cutbacks,
   third-man runs" reads like a discrete enum. CLAUDE.md eliminated
   `KickType`/`ShotType`/`PassType` for this exact pattern. Patterns
   should be parameterized (e.g., support-position offset relative to
   ball carrier, run-line angle, run-line depth, run-line timing) or
   the spec must justify why named patterns are tactical-vocabulary
   only (analogous to Shot Mechanics OI-006 ShotType resolution).

5. **[H] xG-bound acceptance criteria infeasible at Stage 0.** §8 "quality
   tests for chance volume, quality" implicitly requires an xG model.
   Shot Mechanics #6 §7 explicitly defers xG to Stage 1+. Either define a
   Stage-0-feasible surrogate (volume + simple distance/angle heuristic) or
   move §8 acceptance criteria to Stage 1+.

6. **[M] Boundary with Defensive AI #14 §4 unstated.** §5 "transition-to-
   defense behavior on possession loss" overlaps with Spec #14 (defensive
   reset on turnover). Each side must agree on which spec owns the
   handoff trigger.

7. **[M] Boundary with Positioning AI #12 unstated.** "Support distance/
   angle heuristics around the ball carrier" overlaps with Spec #12
   (formation anchors and ball-relative offsets). Either Spec #12 owns
   passive support and Spec #15 owns active off-ball runs, or boundary
   must be drawn elsewhere. Pre-commit.

8. **[M] Determinism plan absent.** Run-selection and pattern-trigger
   evaluation are authoritative AI state per Deterministic Simulation #16.
   Cite #16 §3.2 (deterministic iteration), §4 (RNG ownership), §6 (digest
   scope).

9. **[M] Coordinate convention unmentioned.** "Final third", "weak side",
   "third-man run" geometry depends on corner-origin axes (Ball Physics
   §1.2). Re-affirm.

10. **[M] Tick-rate split unstated.** Pattern decisions are 10 Hz; running
    motion is 60 Hz steering. Declare.

11. **[M] Constant-tag policy not invoked.** §7 unit tests and §6 tactical
    modifiers will introduce constants requiring `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]`
    tags.

12. **[L] No event production declared.** Pattern triggers
    (`OverlapInitiatedEvent`, `RunStartedEvent`) likely needed by
    statistics, telemetry, and Decision Tree #8 perception of run
    availability. Enumerate or defer.

13. **[L] "Tactical identity" is unmeasurable as written.** §8 "quality
    tests for ... tactical identity" needs a measurable definition (e.g.,
    distinct chance-creation distribution between defined team-style
    presets) or the §9 Approval Checklist will fall back on subjective
    sign-off — risking the ERR-005 fabricated-verification pattern.

### Recommended next steps
- Add full metadata header.
- Resolve §3 vs Decision Tree #8 ownership BEFORE re-mapping sections —
  this is the highest-impact decision.
- Decide pattern-template approach (parameterized vs vocabulary) and
  document analogous to Shot #6 KD-3 / OI-006.
- Add Boundary Matrix with #8, #12, #14.
- Define Stage-0-feasible chance-quality surrogate or punt §8 to Stage 1+.
