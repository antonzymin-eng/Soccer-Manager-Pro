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

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> as a planning document, measured against `CLAUDE.md` project rules, the 9-section
> spec template, and adjacent approved/in-flight specs. Findings recorded here for
> tracking; resolution required before promoting outline to a Section 1 draft.
>
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft
> to pass approval gate; **L** = follow-up worth tracking but not draft-blocking.

### Verified premises (cross-checked against repo)
- `ShotExecutedEvent` already promised by Shot Mechanics #6 §4.5 (`section-4-4-to-4-10.md`
  lines 96–146) as the sole GK interface surface — Spec #11 is the named consumer.
- Spec #11 status in `SPEC_INDEX.md`: NOT STARTED. Approved upstream specs available:
  Ball Physics #1, Agent Movement #2, Collision System #3, First Touch #4, Shot
  Mechanics #6, Perception #7, Decision Tree #8.
- Project conventions (`CLAUDE.md`): coordinate origin at pitch corner; fatigue
  `0=rested,1=fatigued`; parameter-based physics (no type enums); 10 Hz tactical
  loop / 60 Hz physics loop; constant tags `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]`.

### Findings

1. **[H] Missing metadata header.** Outline omits Created/Updated dates, version,
   status, spec-of-20 marker, estimated effort, dependency list, and downstream
   consumers. Compare to Shot Mechanics #6 outline header (lines 7–14) which is
   the de-facto project standard. Without this, status reconciliation drift
   (already an open issue project-wide) will recur.

2. **[H] Section plan misaligned with CLAUDE.md template.** CLAUDE.md mandates:
   §2 = functional requirements / data structures / failure modes; §6 = performance
   analysis & budgets; §7 = future extensions / Stage 1+ deferrals; §8 = references
   / citations / DOI verification. This outline routes failure modes into §7 and
   integration scenarios into §8, leaving no slot for references and conflating
   future extensions with robustness. Re-map sections before drafting.

3. **[H] Save-outcome enum risk (catch/parry/deflect/spill).** §3 is phrased as
   discrete outcomes. Project rule (CLAUDE.md "Parameter-Based Physics") eliminated
   `KickType`/`ShotType`/`PassType` for the same shape of trap. Outcomes are
   acceptable as telemetry labels but MUST NOT gate physics: contact location,
   incoming `KickVelocity`/`KickSpin` (from `ShotExecutedEvent`), hand-stiffness,
   and reach geometry should yield ball post-contact velocity emergently. Outline
   must state this explicitly to avoid repeating ERR-class enum-coupling bugs.

4. **[H] Upstream dependencies undeclared.** Outline never names: Shot Mechanics
   #6 (`ShotExecutedEvent`), Perception #7 (reaction timing / trajectory read),
   Decision Tree #8 (intent parameters for save vs. claim vs. rush), Agent Movement
   #2 (dive kinematics, body-shape penalties), Collision System #3 (hand-on-ball
   contact, post-contact rebound), Ball Physics #1 (`Ball.ApplyKick`-equivalent
   for parries/punches). Each must be enumerated with section-level citations as
   Shot Mechanics #6 outline §2.5 does.

5. **[H] Reaction-time model has no source spec.** §2 "reaction delay" is gameplay-
   critical and must be derived from Perception #7 visibility-cone latency plus a
   GK-specific attribute (Reflexes / Reactions). Without citing Perception #7 the
   reaction delay risks being independently re-invented and drifting from the
   approved perception model — exactly the failure pattern that produced ERR-001
   (phantom interfaces).

6. **[M] Positional-heuristic boundary with Positioning AI #12 unresolved.** §4
   "near/far-post positioning heuristics" overlaps with Spec #12. Either Spec #11
   owns GK positioning end-to-end (and Spec #12 declares it out-of-scope) or
   Spec #12 owns formation/anchor positioning and Spec #11 owns only fine-grain
   shot-relative micro-adjustments. Decision must be recorded before draft.

7. **[M] Distribution-choice RNG ungoverned.** §5 "throw/roll/kick with risk model"
   implies stochastic-feeling outcomes. All authoritative randomness must route
   through `DeterministicRngService` per Deterministic Simulation #16 §4.1; the
   draw site must be registered (§4.5 of Spec #16). Cite explicitly.

8. **[M] Fatigue convention unmentioned.** §1 covers a state machine that will
   inevitably read fatigue (recover state, late-match dives). Per CLAUDE.md the
   convention `0.0 = rested, 1.0 = fatigued` has been inverted before (Pass
   Mechanics FR-02). Outline should pre-commit to the convention.

9. **[M] Constant-tag policy not invoked.** Every tunable in §3 (catch radius,
   parry vector floor, dive launch impulse) requires a `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]`
   tag. Outline does not promise this; the Approval Checklist §9 risks the
   ERR-005 fabrication trap (Pass Mechanics) if constants are added without tags.

10. **[M] No Stage-0 vs Stage-1+ scope demarcation.** Cross spec / set-piece
    saves, distribution-tactics integration with team possession phases, and
    advanced 1v1 narrowing-the-angle behavior should be classed as Stage 1+
    explicitly so Section 7 (per the corrected template) has concrete deferrals
    rather than the current §7 "failure modes" misallocation.

11. **[L] Coordinate-system reminder absent.** §4 "near/far-post" relies on the
    corner-origin axis convention (Ball Physics §1.2). A one-line reaffirmation
    in the outline preempts the recurring "pitch center" trap (ERR-class entry
    in CLAUDE.md "Things That Have Gone Wrong Before").

12. **[L] Tick-rate context missing.** GK perception and reactions live on the
    10 Hz tactical loop, but dive kinematics and ball-contact resolution live
    on the 60 Hz physics loop. The boundary must be stated to avoid the
    "do not conflate" trap from CLAUDE.md.

13. **[L] No event production declared.** GK actions (catch / save / punch /
    distribute) presumably need their own events for Event System #17 and
    statistics consumers. Outline does not enumerate any produced events —
    parallel to Shot #6 publishing `ShotExecutedEvent`, Spec #11 likely owns
    `SaveAttemptedEvent` / `BallClaimedEvent` etc. Declare or defer.

### Recommended next steps (not changes — to be made when promoting outline to draft)
- Add full metadata header to match Shot Mechanics #6 outline.
- Re-map Section Plan to CLAUDE.md template (§2 FRs/data/failure modes; §7 future;
  §8 references).
- Enumerate upstream/downstream dependency tables (cf. Shot #6 §2.5 / §2.6).
- Insert Key Design Decisions list pre-committing: parameter-based contact model,
  fatigue convention, RNG via Spec #16, positional-boundary with Spec #12.
- Add Open Items Tracker for the Spec-#12 boundary, the reaction-model citation,
  and the Stage-0/Stage-1+ split.
