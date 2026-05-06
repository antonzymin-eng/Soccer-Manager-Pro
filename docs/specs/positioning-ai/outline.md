# Positioning AI Specification #12 — Outline

## Purpose
Define team shape maintenance and context-aware positioning behavior for all phases of play.

## Scope
Anchors, transitions, spacing constraints, context modifiers, and integration boundaries with adjacent AI systems.

## Section Plan
- Section 1 — Role/formation anchors and ball-relative offsets.
- Section 2 — Transition rules (in-possession, out-of-possession, turnovers).
- Section 3 — Spacing, lane occupation, and collision-aware movement limits.
- Section 4 — Context modifiers (score, fatigue, tactical-intensity inputs).
- Section 5 — Interface boundaries with Decision Tree and Perception outputs.
- Section 6 — Tunable parameters and authoring workflow.
- Section 7 — Shape-integrity and compactness unit tests.
- Section 8 — Tactical correctness integration scenarios (coverage and balance).
- Section 9 — Approval checklist.
- Appendices — Formation archetype profiles and debug overlays.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #12 status in `SPEC_INDEX.md`: NOT STARTED. Decision Tree #8 and
  Perception #7 already APPROVED — both are upstream interface sources.
- CLAUDE.md "Interface Design Principle": write interfaces only when both sides
  are specified (anti-pattern that produced ERR-001 / ERR-004).
- Authoritative coordinate system: corner origin (Ball Physics §1.2). 10 Hz
  tactical loop drives positioning; 60 Hz physics loop renders motion.

### Findings

1. **[H] Missing metadata header.** No Created/Updated, version, status,
   dependencies, downstream consumers, or estimated effort. Compare to Shot
   Mechanics #6 outline (header lines 7–14) — that is the project template.

2. **[H] Section plan deviates from CLAUDE.md template.** The mandatory template
   places functional requirements / data structures / failure modes in §2,
   formulas/algorithms in §3, architecture in §4, tests in §5, performance in
   §6, future extensions in §7, references in §8. Current outline puts
   "transition rules" in §2 (formula territory), "interface boundaries" in §5
   (architecture territory), and "tunable parameters and authoring workflow" in
   §6 (which CLAUDE.md reserves for performance budgets). No slot exists for
   references. Re-map.

3. **[H] Boundary with Decision Tree #8, Pressing AI #13, Defensive AI #14, and
   Attacking AI #15 unstated.** "Anchors" and "context-aware positioning" overlap
   with Decision Tree (final-action selection), Pressing AI (press shape),
   Defensive AI (line/zonal assignment), Attacking AI (support distance/angle).
   Without explicit ownership boundaries this spec will collide with at least
   four downstream specs. Pre-commit boundaries before drafting.

4. **[H] Authoring-tool scope creep.** §6 "tunable parameters and authoring
   workflow" implies a tools surface that does not exist at Stage 0 and is not
   in the master development plan for this stage. Either drop the workflow piece
   or label Stage 1+ deferral.

5. **[H] Formation data ownership undefined.** §1 "role/formation anchors"
   requires authoritative formation data (4-3-3, 4-2-3-1, etc.). Source spec
   not named — is it tactical-instruction config, coach UI, or save-game data?
   Without an upstream owner, formation values become magic numbers in this
   spec — exactly the trap CLAUDE.md "Constants Governance" forbids.

6. **[M] No determinism plan.** Positioning updates that read perception state
   and write target positions are authoritative state per Deterministic
   Simulation #16. Outline does not commit to: deterministic iteration order
   (#16 §3.2), `DeterministicRngService` for any stochastic micro-jitter
   (#16 §4), or per-phase digest scope (#16 §6.2). Cite Spec #16.

7. **[M] Coordinate-system convention unstated.** "Ball-relative offsets"
   without committing to the corner-origin axes (Ball Physics §1.2) re-opens
   the "pitch center" trap (CLAUDE.md "Things That Have Gone Wrong Before").

8. **[M] Hysteresis/anti-jitter discussion missing.** Anchor-relative repositioning
   on every tactical tick will oscillate at decision boundaries unless
   hysteresis is built in (Agent Movement §3.1 already establishes this pattern
   for the project). Outline should pre-commit to the same architectural
   pattern rather than re-inventing it.

9. **[M] Tick-rate split unstated.** Positioning targets are computed on the
   10 Hz tactical loop; agents physically move toward them on the 60 Hz physics
   loop via Agent Movement #2 steering. Outline must declare that boundary or
   risk mixing concerns with Agent Movement.

10. **[M] Constant-tag policy not invoked.** §6 "tunable parameters" requires
    `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]` tagging per CLAUDE.md. Pre-commit
    or the Approval Checklist will repeat the ERR-005 fabrication risk.

11. **[L] Fatigue interaction unmentioned.** §4 "context modifiers" lists
    fatigue but does not call out the `0=rested,1=fatigued` convention. Pre-
    commit the convention to neutralise the recurring inversion bug.

12. **[L] No event production / consumption declared.** Positioning likely
    consumes phase-change events (in-possession ↔ out-of-possession turnover)
    and may produce shape-change telemetry events. Enumerate upstream
    subscription targets and downstream emitters or defer explicitly.

13. **[L] Test-pyramid hint missing.** §7 "shape-integrity unit tests" is
    fine, but compare to Shot Mechanics #6 §5.1 which lists target test counts
    by category — that gives a verifiable Approval Checklist target.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template.
- Add Boundary Matrix declaring ownership splits with #8, #13, #14, #15.
- Add Key Design Decisions covering coordinate convention, fatigue convention,
  determinism via #16, formation-data source, hysteresis pattern reuse.
- Convert §6 from "authoring workflow" to "performance budgets" per template.
