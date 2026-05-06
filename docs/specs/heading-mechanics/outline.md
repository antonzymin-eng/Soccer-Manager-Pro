# Heading Mechanics Specification #10 — Outline

## Purpose
Define deterministic, skill-informed heading interactions that feel realistic and tactically legible.

## Scope
Covers eligibility, timing quality, outcome generation, contested interactions, and validation criteria for heading actions.

## Section Plan
- Section 1 — Header eligibility rules
  - Proximity thresholds, jump windows, body-orientation requirements.
  - Aerial-state gating and invalid-state rejection.
- Section 2 — Timing and contact-quality model
  - Early / perfect / late contact windows.
  - Outcome shaping multipliers by contact quality.
- Section 3 — Direction and power generation
  - Attribute influences.
  - Momentum and incoming ball-flight integration.
- Section 4 — Contested header model
  - Duel resolution using balance/strength.
  - Collision interactions and disturbance factors.
- Section 5 — Edge-case handling
  - Glancing contact behavior.
  - Complete misses and defensive-clearance variants.
  - Own-goal risk modeling.
- Section 6 — Tuning controls and telemetry
  - Designer-exposed parameters.
  - Runtime counters for contact quality and duel outcomes.
- Section 7 — Unit validation scenarios
  - Eligibility, timing-window, and direction/power assertions.
- Section 8 — Integration match-feel scenarios
  - Crosses, set pieces, and aerial duels mapped to feel criteria.
- Section 9 — Approval checklist
- Appendices
  - Parameter ranges and exemplar tuning profiles.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md` project rules, the 9-section template, and
> adjacent approved/in-progress specs. Findings recorded for tracking;
> resolution required before promoting outline to a Section 1 draft.
>
> Severity legend: **H** = blocks draft start; **M** = must resolve during
> draft to pass approval gate; **L** = follow-up worth tracking but not
> draft-blocking.

### Verified premises (cross-checked against repo)
- `SPEC_INDEX.md`: Spec #10 status NOT STARTED. Approved upstream:
  Ball Physics #1, Agent Movement #2, Collision System #3, First Touch #4,
  Shot Mechanics #6, Perception #7, Decision Tree #8. Pass Mechanics #5
  is **SUSPENDED** — direct relevance because crosses (the dominant
  source of headers) live there.
- Shot Mechanics #6 §1.2 line 186 + KD-6 (line 344, OI-002): "All ball
  contacts where contact body part is the **head**, regardless of ball
  height or agent posture" route to Heading Mechanics #10. The 0.5 m
  contact-height threshold from First Touch #4 explicitly does NOT apply.
  Spec #10 must own this rule definitively.
- Agent Movement #2 §3.5.6 (`section-3-5-part-2.md` line 347–348):
  `Heading` attribute already declared and marked "Used by Spec #10".
  `Strength` (line 58) and `Balance` (line 311) also exist. **No
  `Jumping` / `JumpReach` / `Aerial` attribute is declared in the
  approved `PlayerAttributes` struct.**
- Project conventions (`CLAUDE.md`): coordinate origin at pitch corner
  (Ball Physics §1.2); fatigue `0=rested, 1=fatigued`; parameter-based
  physics (no type enums in physics layer); 10 Hz tactical / 60 Hz
  physics tick split; constant tagging
  `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]`.

### Findings

1. **[H] Missing metadata header.** Outline omits Created/Updated dates,
   version, status, spec-of-20 marker, estimated effort, dependency list,
   and downstream consumers. Compare to Shot Mechanics #6 outline header
   (lines 7–14) which is the de-facto project standard. Without this,
   status reconciliation drift (already an open project issue) will recur.

2. **[H] Section plan misaligned with CLAUDE.md template.** The mandatory
   template places: §2 = functional requirements / data structures /
   failure modes; §3 = formulas/algorithms; §4 = architecture / file
   layout / interface contracts; §5 = test plan; §6 = performance analysis
   and budgets; §7 = future extensions / Stage 1+ deferrals; §8 =
   references / citations / DOI verification. This outline routes
   "tuning controls and telemetry" to §6 (performance slot), unit
   validation to §7 (future-extensions slot), and integration scenarios
   to §8 (references slot), leaving no slot for references or for an
   architecture / interface section. Re-map before drafting.

3. **[H] Contact-window enum risk (early/perfect/late).** §2 names three
   discrete contact windows. CLAUDE.md "Parameter-Based Physics (No Type
   Enums)" eliminated `KickType`/`ShotType`/`PassType` for the same shape
   of trap. Contact timing should be a continuous offset (e.g., signed
   ms relative to ideal-contact frame, or normalized phase ∈ [-1, +1])
   that produces a continuous quality scalar. Discrete windows are
   acceptable only as telemetry labels emitted from the continuous
   computation — they MUST NOT gate the physics formula.

4. **[H] Glancing-vs-power distinction risks the same enum trap.** §5
   "glancing contact behavior" alongside §3 "direction and power
   generation" reads as two discrete header types. Per Shot Mechanics
   #6 OI-006 (ShotType eliminated) and Pass Mechanics analogue, the
   header outcome should be emergent from a `ContactZone`-equivalent
   parameter (e.g., contact point on the head: forehead-centre /
   forehead-edge / temple) plus head-velocity vector and incoming
   ball-velocity vector. Decision Tree #8 supplies *intent*; physics
   produces vectors; named outcomes are downstream telemetry only.

5. **[H] Missing `Jumping` attribute in approved Agent Movement #2.**
   §1 "jump windows" and §4 "duel resolution" require a vertical-reach
   capability. Agent Movement #2 §3.5.6 declares `Heading`, `Strength`,
   `Balance` — but **no `Jumping`/`JumpReach`/`Aerial` attribute**.
   Either (a) Spec #10 derives jump reach from existing attributes
   (e.g., `JumpReach = f(Strength, Balance)` with a `[DERIVED]` tag),
   or (b) Agent Movement #2 needs a minor revision to add the
   attribute — touching an APPROVED spec requires lead-developer
   authorization (CLAUDE.md). Decide and document before drafting.

6. **[H] Upstream dependencies undeclared.** Outline never names:
   Ball Physics #1 (incoming `BallState` velocity / spin / Magnus —
   the physical input to every head contact), Agent Movement #2
   (jump kinematics, attributes `Heading` / `Strength` / `Balance`,
   `AgentPhysicalProperties`), Collision System #3 (head-ball contact
   resolution and contested-duel collision), First Touch #4 (analogous
   model for foot/body contact below head height; boundary established
   by Shot #6 KD-6), Pass Mechanics #5 (cross delivery — SUSPENDED
   risk), Shot Mechanics #6 (analogous output interface, scope-boundary
   authority), Decision Tree #8 (intent parameters: header target,
   power intent, contact-point intent), Deterministic Simulation #16
   (RNG governance for duel ties). Each must be enumerated with
   section-level citations as Shot Mechanics #6 outline §2.5 / §2.6 do.

7. **[H] Output interface to Ball Physics undeclared.** Shot Mechanics
   #6 §4 emits `Ball.ApplyKick(velocity, spin, agentId, matchTime)`
   and publishes `ShotExecutedEvent`. Pass Mechanics is parallel.
   Heading Mechanics presumably emits the same `Ball.ApplyKick` call
   plus a `HeaderExecutedEvent` for downstream consumers (Goalkeeper
   #11, Event System #17, statistics). Outline does not declare the
   output surface — this is the single most important interface this
   spec produces.

8. **[H] Pass Mechanics #5 SUSPENDED-status risk unacknowledged.**
   Crosses are the canonical header source. Cross delivery semantics
   (target trajectory, hang time, swerve) live in Pass Mechanics #5,
   currently SUSPENDED awaiting re-review. Spec #10 must either gate
   draft completion on #5 re-approval or define a stable subset
   interface (e.g., consume only `KickVelocity`/`KickSpin` from the
   incoming `BallState`, not Pass-specific labels) that survives any
   #5 amendment.

9. **[H] Own-goal handling is out-of-scope as written.** §5 "own-goal
   risk modeling" — own-goal *detection* is a referee/match-rules
   concern (see analogous goal-detection deferral in Shot Mechanics
   #6 §1.2: "Goal detection and match state update" → Match Referee /
   Event System #17). Heading Mechanics produces a velocity vector;
   whether the ensuing trajectory crosses the defender's own goal line
   is downstream. Outline must either rephrase as "header outcomes
   that may produce own-goal-shaped trajectories — flagged in
   `HeaderExecutedEvent` for Event System #17 to adjudicate" or drop
   the bullet.

10. **[M] Determinism plan absent.** Contested duels (§4) and direction
    perturbation in glancing contact (§5) imply stochastic-feeling
    outcomes. All authoritative randomness must route through
    `DeterministicRngService` per Deterministic Simulation #16 §4.1
    with registered draw-site IDs (#16 §4.5). Iteration over duel
    participants must be deterministic per #16 §3.2. Cite explicitly.

11. **[M] Coordinate-system convention unstated.** §3 "momentum and
    incoming ball-flight integration" relies on the corner-origin axes
    (Ball Physics §1.2). A one-line reaffirmation preempts the recurring
    "pitch center" trap (CLAUDE.md "Things That Have Gone Wrong Before").

12. **[M] Fatigue convention not pre-committed.** Header power and jump
    height are obvious fatigue consumers. Per CLAUDE.md the convention
    `0.0 = rested, 1.0 = fatigued` has been inverted before (Pass
    Mechanics FR-02 ERR-class). Outline should pre-commit.

13. **[M] Tick-rate split unstated.** Header intent (target, power) is
    selected by Decision Tree #8 on the 10 Hz tactical loop; jump
    kinematics, contact resolution, and ball-velocity emission live on
    the 60 Hz physics loop. Boundary must be declared.

14. **[M] Constant-tag policy not invoked.** §6 "designer-exposed
    parameters" (proximity thresholds, jump-window widths, contact
    multipliers, glancing thresholds, own-goal-shape thresholds)
    requires `[GT]/[EST]/[FIXED]/[DERIVED]/[CROSS]` tagging per
    CLAUDE.md. Without pre-commit the §9 Approval Checklist risks the
    ERR-005 (Pass Mechanics) fabricated-checklist trap.

15. **[M] Boundary with First Touch #4 reaffirmation needed.** Shot
    Mechanics #6 KD-6 establishes "contact body part is the sole
    discriminator" but the 0.5 m height threshold lives in First Touch
    #4. Heading Mechanics must explicitly state "head contact below
    0.5 m (e.g., diving headers, bicycle-kick headers, head-on-ground
    contact) routes to Heading Mechanics #10, NOT First Touch #4" — or
    First Touch's 0.5 m rule will silently swallow low-height head
    contacts.

16. **[M] Boundary with Goalkeeper Mechanics #11 unstated.** GK punches,
    palm-clears, and goal-line headed clearances are an edge case: GK
    head contact follows GK rules (#11) or general heading rules (#10)?
    Both specs are NOT STARTED — pre-commit ownership now to avoid
    re-litigation when both are drafted.

17. **[M] Contested-duel resolution overlaps with Collision System #3.**
    §4 "duel resolution using balance/strength" and "collision
    interactions" — is the duel a Collision-System contact event with
    a Heading-specific resolution, or a Heading-System computation that
    consults Collision-System contact data? Spec #3 is APPROVED;
    Heading Mechanics must consume its interface, not redefine it.

18. **[L] No mention of weak-side / non-dominant-aerial penalty.**
    Pass and Shot have `WeakFootRating` (Agent Movement §3.5.6).
    Some players are markedly stronger jumping or heading off one
    side. Either declare an analogous weak-aerial-side modifier or
    explicitly defer to Stage 1+.

19. **[L] "Complete misses" (§5) physics output undefined.** When the
    header attempt fails contact entirely (jump mistimed, ball passes
    through head zone), what is the physics output? Most likely "no
    `Ball.ApplyKick` call; ball trajectory unchanged; agent emits
    failed-attempt telemetry event". State explicitly.

20. **[L] Set-piece scope demarcation missing.** §8 "crosses, set
    pieces, and aerial duels" — set pieces (corners, free kicks)
    are deferred to Stage 1+ per Shot Mechanics #6 §1.2 "Set piece
    shots". Headers from set-piece deliveries are mechanically
    identical to open-play headers (the cross is just a Pass), so
    Heading Mechanics #10 *can* cover them at Stage 0. State this
    explicitly to avoid scope drift.

21. **[L] Concussion / injury modeling unmentioned.** Modern football
    games face scrutiny on this. Stage 0 surely defers to a future
    medical/injury system (no such spec exists in the 20-spec set —
    presumably Stage 1+). State the deferral so it is traceable.

22. **[L] Spin transfer to ball undeclared.** A header on a spinning
    incoming ball can preserve, reverse, or null the spin depending
    on contact. Outline does not address whether `Ball.ApplyKick`
    spin parameter is computed by Heading Mechanics or punted to
    Ball Physics. Decide.

### Recommended next steps (not changes — to be made when promoting outline to draft)
- Add full metadata header (cf. Shot Mechanics #6 outline lines 7–14).
- Re-map Section Plan to CLAUDE.md 9-section template (§2 FRs/data/
  failure modes; §4 architecture/interface; §6 performance budgets;
  §7 Stage 1+ deferrals; §8 references).
- Enumerate upstream/downstream dependency tables (cf. Shot #6 §2.5 / §2.6),
  including the Pass Mechanics #5 SUSPENDED-status risk note.
- Insert Key Design Decisions list pre-committing:
  - parameter-based contact model (no header-type enum)
  - continuous contact-quality scalar (not early/perfect/late enum)
  - body-part discriminator inheritance from Shot #6 KD-6 / First Touch #4
  - `Jumping` attribute resolution (derive vs add to AM #2)
  - own-goal handling routed to Event System #17, not adjudicated here
  - RNG governance via Deterministic Simulation #16
  - fatigue convention `0=rested, 1=fatigued`
  - coordinate convention corner-origin
- Add Open Items Tracker for: GK boundary (#11), Pass #5 cross-delivery
  semantics, Jumping-attribute decision.
- Define `HeaderExecutedEvent` struct mirroring `ShotExecutedEvent`
  (Shot #6 §4.5 / `section-4-4-to-4-10.md` lines 96–146).
