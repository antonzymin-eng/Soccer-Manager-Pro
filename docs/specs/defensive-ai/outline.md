# Defensive AI Specification #14 — Outline

## Purpose
Define a robust defensive framework that preserves shape while minimizing high-quality chances conceded.

## Scope
Defensive principles, assignment logic, line coordination, handoff behavior, emergency actions, and validation scenarios.

## Section Plan
- Section 1 — Core principles: mark, contain, delay, intercept, tackle.
- Section 2 — Zonal/man-hybrid assignments and phase-change updates.
- Section 3 — Line depth, compactness, and offside-line coordination.
- Section 4 — Handoff rules between defensive and pressing systems.
- Section 5 — Emergency behaviors (clearances, last-man risk management).
- Section 6 — Tuning surfaces by team style and game state.
- Section 7 — Shape-preservation and assignment-stability tests.
- Section 8 — Chance-prevention integration outcomes.
- Section 9 — Approval checklist.
- Appendices — Defensive KPI definitions and debug visualizations.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #14 status in `SPEC_INDEX.md`: NOT STARTED. Approved: Agent Movement #2,
  Collision System #3, Perception #7, Decision Tree #8.
- "Tackle" mechanics live in Collision System #3 (already approved).
- Project authoritative coordinate system: corner origin (Ball Physics §1.2);
  "line depth" depends on x-axis convention.

### Findings

1. **[H] Missing metadata header.** No Created/Updated, version, status,
   dependencies, downstream consumers, or estimated effort. Compare to Shot
   Mechanics #6 outline header.

2. **[H] Section plan misaligned with CLAUDE.md template.** Per template,
   §2 is FRs/data/failure modes, §6 is performance, §7 is future extensions,
   §8 is references. Current outline puts emergency behaviors in §5 (still
   inside formula territory §3 by template), tuning surfaces in §6 (perf slot),
   integration outcomes in §8 (references slot). No references section
   exists. Re-map.

3. **[H] Tackle ownership ambiguity.** §1 "Mark, contain, delay, intercept,
   tackle" — but tackle resolution is owned by Collision System #3. This
   outline must declare that Spec #14 selects *intent to tackle* (timing,
   target, commit-vs-jockey) and Spec #3 owns the *physics of contact*. Not
   stating this re-opens the Decision Tree / Physics overlap that produced
   the project-wide "no type enums" rule.

4. **[H] Offside-line ownership undefined.** §3 "offside-line coordination"
   conflates two responsibilities: (a) defenders aligning to step up (this
   spec) and (b) offside-rule adjudication for goal validity (a referee /
   match-rules system not in the 20-spec set). Without separating these,
   the spec will accumulate adjudication logic that belongs to Event System
   #17 / a future referee spec.

5. **[H] Boundary with Pressing AI #13 and Positioning AI #12 unstated.**
   §4 "handoff rules between defensive and pressing systems" presumes a
   handoff protocol that Spec #13 outline has not pre-committed to. Each
   side must declare the same handoff or the systems will diverge. Zonal/
   man assignments (§2) further overlap with Positioning AI formation
   anchors. Define ownership boundaries before drafting.

6. **[H] "Last-man" computation undefined.** §5 "last-man risk management"
   is meaningful only with a deterministic definition of "last man" (which
   coordinate axis, which ball-relative cone, accounting for goalkeeper).
   Without a formula, this becomes a magic-state predicate.

7. **[M] Determinism plan absent.** Defensive role assignments and switching
   are authoritative state per Deterministic Simulation #16. Iteration order
   over defenders MUST be deterministic (#16 §3.2); any stochastic
   tie-breaking MUST go through `DeterministicRngService` (#16 §4). Cite #16.

8. **[M] Coordinate convention unmentioned.** "Line depth" presumes
   corner-origin x-axis (Ball Physics §1.2). Re-affirm to avoid the recurring
   "pitch center" trap.

9. **[M] Tick-rate split unstated.** Assignment updates run on the 10 Hz
   tactical loop; physical pursuit on 60 Hz via Agent Movement. Declare.

10. **[M] Fatigue convention not pre-committed.** §6 "tuning by team style and
    game state" will read fatigue. Pre-commit `0=rested, 1=fatigued` per
    CLAUDE.md.

11. **[M] No event production / consumption declared.** Defensive AI must
    consume turnover events and likely emits `MarkAssignedEvent` /
    `LineSteppedEvent` for telemetry. Enumerate or defer.

12. **[M] Constant-tag policy not invoked.** §6 tunables require
    `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]` tags.

13. **[L] "Chance-prevention integration outcomes" (§8) lacks acceptance
    criteria.** Without quantitative xG-against / shot-quality targets, §9
    Approval risks the ERR-005 fabricated-checklist trap. xG modeling itself
    is Stage 1+ (per Shot Mechanics #6 §7) — define a Stage-0-feasible
    surrogate metric.

14. **[L] No interaction model with Goalkeeper Mechanics #11.** Last-man
    decisions, retreats covering for an out-of-position GK, and clearance
    targets all interact with #11. Boundary-decision pending #11 outline
    metadata.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template.
- Add Boundary Matrix declaring splits with #3 (tackle physics), #11 (GK),
  #12 (anchors), #13 (press), and a future referee/rules system (offside
  adjudication).
- Define "last man" deterministic predicate.
- Pre-commit determinism, coordinate, and fatigue conventions.

