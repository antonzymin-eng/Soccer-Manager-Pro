# Heading Mechanics #10 — `outline-detailed.md` v1.0 Adversarial Review (Pass 1)

**Created:** May 15, 2026
**Reviewer:** Adversarial pass against `outline-detailed.md` v1.0 (May 15, 2026).
**Scope:** Detailed outline only. Section files are stubs and not reviewed here.
**Method:** Read outline-detailed end-to-end; cross-checked KD claims against
APPROVED upstream specs (#1, #2, #3, #4, #6, #16); checked internal
consistency between KDs, §3.1 master constants table, §3 algorithms,
§4 interfaces, §5 tests, and §6 budgets.
**Headline:** Outline is structurally sound and the 16 KDs successfully
close the v0.1 22-finding review. However, **§3 algorithms have not been
reconciled against the §3.1 master constants table**, leaving multiple
constants un-enumerated and at least one constant double-named with
incompatible semantics. A handful of upstream-surface assumptions are
also unbacked. Findings:

| Severity | Count |
|----------|-------|
| HIGH     | 5     |
| MEDIUM   | 9     |
| LOW      | 7     |
| Total    | 21    |

Recommend a v1.1 fix pass before section-file authoring begins. None of
the findings invalidate the §1.3 KD set or the dependency closure;
they're concentrated in §3 internal consistency and §4 / §6 budget
arithmetic.

---

## HIGH

### H-1 — `MAX_TOLERANCE_MS` vs. `MAX_EARLY/LATE_TOLERANCE_MS`: §3.4 contradicts §3.1

§3.1 enumerates `MAX_EARLY_TOLERANCE_MS [GT]` and
`MAX_LATE_TOLERANCE_MS [GT]` as two distinct constants, implying an
asymmetric early/late timing window (which is correct for heading: a
late header is mechanically worse than an early one). §3.4's
`timingQuality` formula, however, references a single
`MAX_TOLERANCE_MS`:

```
timingQuality = 1 - clamp01(|timingOffsetMs| / MAX_TOLERANCE_MS)
```

This is internally inconsistent. Either §3.4 must split into a signed
piecewise form (early branch / late branch using the two §3.1
constants) or §3.1 must collapse to a single symmetric tolerance.
The asymmetric form is the better physics; the formula is the wrong
side.

**Fix:** Rewrite §3.4 `timingQuality` as
`(timingOffsetMs ≤ 0) ? 1 - clamp01(-timingOffsetMs / MAX_EARLY_TOLERANCE_MS) :
1 - clamp01(timingOffsetMs / MAX_LATE_TOLERANCE_MS)` and add an
explicit note that label thresholds (`EARLY_LABEL_THRESHOLD_MS`,
`LATE_LABEL_THRESHOLD_MS`) are independent of the quality-degradation
tolerances.

### H-2 — KD-4 / §3.3 `JumpReach` formula drops the `Heading` term that KD-4 declares

KD-4 says: `JumpReach = f(Strength, Balance, Heading)`. The §3.3
formula is:

```
JumpReach_m = JUMP_REACH_BASE_M
            + JUMP_REACH_K_STRENGTH · Strength_norm
            + JUMP_REACH_K_BALANCE  · Balance_norm
```

`Heading` is missing. §3.1 also lacks `JUMP_REACH_K_HEADING`. This
is a direct KD violation introduced inside the same outline that
declares the KD. Either the formula gains the term and §3.1 gains
the constant, or KD-4 is rewritten to drop `Heading` from the
`JumpReach` signature (which is defensible — `Heading` is mostly
about contact technique, not aerial reach).

Recommend adding the term: `Heading` arguably governs anticipation
(timing the jump apex), which conflates with reach in the absence of
a separate timing-attribute. Until that separation is designed, the
`Heading` term in `JumpReach` is a defensible aggregation.

### H-3 — §3.6 spin transfer requires `headAngularVelocity` from a source that doesn't exist

§3.6 references `headAngularVelocity` as an input. Agent Movement #2
(APPROVED) exposes COM kinematics and player-attribute
`Heading/Strength/Balance` (`section-3-5-part-2.md` §3.5.6) but
**does not publish a head-segment angular velocity**. There are
three viable resolutions, and the outline picks none:

1. Derive `headAngularVelocity` inside #10 from agent yaw/pitch rate
   already published by AM #2 (cheap; less faithful but no upstream
   amendment).
2. Synthesize it from `ContactPointIntent + headVelocityVector`
   (already in §3.5) — collapse to a derived scalar.
3. Request a back-prop amendment on AM #2 §3.5 (requires APPROVED
   spec amendment; precedent exists via the EntityId no-reuse
   patches in #2 §2.5 v1.1.1).

**Fix:** Add a §3.6 input-derivation paragraph and a KD or KD-amendment
naming the chosen path. This is load-bearing for KD-16 (Spin transfer
is Heading-owned).

### H-4 — §6.1 budget (≤80 µs) is exceeded by §6.3 worst case (~120 µs); no reconciliation

§4.5 / §6.1 set the per-tick budget at ≤80 µs at 22-agent peak.
§6.3 then estimates worst case (3-way duel + tiebreaker RNG) at
~120 µs. That's a 1.5× overrun **at the duel-resolution frame**,
not at idle. Two issues:

1. The 80 µs number isn't justified — it's labelled `[EST]` but no
   derivation from the §6.3 component costs is shown.
2. The outline doesn't say whether 120 µs is acceptable as a p99
   spike against an 80 µs steady-state budget, or whether 80 µs is
   the absolute ceiling that the duel-frame violates.

This will fail a §9 cross-check against #18 §6 ratify-not-override
(KD-2 of #18) since the per-spec §6 must be self-consistent before
#18 ratifies it.

**Fix:** Either raise the budget candidate to ≥120 µs and justify
against #18 §5 baseline, or prove the 80 µs steady-state vs. 120 µs
p99 distinction with explicit tail-budget framing. Cite the
`certification-platform.md` Stage-0 host pin caveat — none of these
numbers are credible until the host platform is pinned.

### H-5 — §3.7 step 2 contains a magic number (`0.01`); violates KD-11

```
duelScore = w_B·Balance + w_S·Strength + w_H·Heading
          + 0.01 · rng.NextFloat(DRAW_SITE_DUEL_TIEBREAK)
```

The `0.01` is unnamed and untagged. KD-11 prohibits magic numbers in
formula code; it would be hypocritical to permit them in the
algorithm pseudocode that drives the formula code. Worse, the
semantics of the term are conflated:

- If the intent is **deterministic tie-breaking** (only break score
  ties), the formula should be `if |scoreA - scoreB| < ε then break
  with RNG`, not "always perturb by 0.01 × U[0,1]".
- If the intent is **outcome noise** (small chance of upset on
  near-ties), `0.01` is way too small to ever flip a meaningful
  duel; weights `w_*` are presumably O(1).

**Fix:** Add `DUEL_TIEBREAK_EPSILON [GT]` and
`DUEL_TIEBREAK_NOISE_AMPLITUDE [GT]` to §3.1; pick one of the two
semantics in §3.7 and document why; rewrite the formula in tagged
constants.

---

## MEDIUM

### M-1 — §3.1 master constants table is missing constants used in §3.4–§3.7

The §3.1 table is presented as the canonical surface for §9.1
constant-tag verification. The following constants appear in §3
algorithm bodies but are **absent** from §3.1:

- `α` (timing/point blend coefficient) — §3.4. Outline says `α =
  [GT]` inline but doesn't list it in §3.1.
- `EARLY_LABEL_THRESHOLD_MS`, `LATE_LABEL_THRESHOLD_MS` — §3.4.
- `MIN_CONTACT_QUALITY` — §3.7 (cutoff below which loser becomes
  a failed attempt).
- `FRAME_MS` — §3.4 (presumably `[DERIVED]` from `TICK_RATE_PHYSICS_HZ`,
  but must be enumerated).
- `spinPreservationFactor`, `reversalTerm` — §3.6 (these may be
  derived from `SPIN_TRANSFER_COEFF` and contact-point geometry,
  but the outline doesn't say).
- `MAX_TOLERANCE_MS` — §3.4 (overlaps with H-1; if H-1 is fixed by
  splitting, this entry is removed; otherwise it must be added).

**Fix:** Inventory every symbol in §3.4–§3.7 pseudocode and either
list it in §3.1 with a tag or mark it `[DERIVED]` with formula.

### M-2 — `IDEAL_CONTACT_FRAME_OFFSET` is per-jump derived, not a constant

§3.1 lists `IDEAL_CONTACT_FRAME_OFFSET [DERIVED] from jump apex (#2)`.
But this is a per-jump-instance derived value (depends on jump
trajectory, ball trajectory, etc.), not a constant. It belongs in
the §3.2 / §3.3 algorithm output structure, not in the master
constants table.

**Fix:** Move it out of §3.1; document it in §3.2 as a per-call
output of the eligibility predicate.

### M-3 — §6.3 / §5.3.1 telemetry expectations off by ~3–4×

- §6.3: "p99 contested duels: estimated ≤3 per match minute" → 270
  per 90-min match. Real-world football has 25–40 headers total per
  match. 3 contested duels per minute is implausible.
- §5.3.1: "10-minute simulation with ~15 headers expected" → 90 per
  match. Same magnitude error.

These numbers will lock into validation gates and waste tuning
cycles.

**Fix:** Recalibrate against published football data (e.g., Opta /
StatsBomb header counts per match: ~28). State source.

### M-4 — `DRAW_SITE_CONTACT_POINT_ERROR` and `DRAW_SITE_TIMING_JITTER` have no caller in §3

§4.4 declares three RNG draw sites: `DRAW_SITE_DUEL_TIEBREAK`,
`DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`. Only
the first appears in any §3 algorithm. The other two are
phantom — if they're intended to model human-error noise on
contact-point and timing, the noise term needs to appear in §3.4
formula.

**Fix:** Either inject the noise terms into §3.4 (and update §3.1
constants for noise σ) or drop the unused draw sites from §4.4.
Phantom draw sites trip determinism-owner sign-off (KD-10) because
the registry must be a complete catalogue.

### M-5 — `attemptCommittedTick` (10 Hz) → contact frame (60 Hz) staleness policy unspecified

`HeaderIntent` carries `attemptCommittedTick: int` from the 10 Hz
tactical loop. Predicted contact frame is computed at 60 Hz, often
3–18 physics frames after commit. Between commit and contact, the
ball trajectory may have shifted (deflection off a defender,
goalkeeper interaction, friction integration). Outline doesn't
specify:

- Whether `HeaderIntent` is re-validated each physics frame.
- What happens if the predicted contact frame moves outside the
  attempt window after commit.
- Whether `targetIntent` is re-evaluated against the ball trajectory
  delta or held fixed (player chose to head it *there* regardless).

This is a load-bearing decision for KD-12 (failed-attempt physics)
because the difference between "ball moved away after commit" and
"jumper mistimed" is exactly what determines `failureCause`.

**Fix:** Add §3.2 subsection on intent-staleness policy or pre-commit
a KD-17.

### M-6 — `XC-010-001` (AM #2 §2.5 EntityId no-reuse) binding has no articulated need

§8.4 allocates `XC-010-001` to AM #2 §2.5 EntityId no-reuse. The
constraint's purpose (per `ERR-016-002` / CLAUDE.md OPEN ISSUES) is
to bind specs that **cache or reference agent IDs across despawn
boundaries**. Heading consumes `agentId` only within a single
contact resolution (one frame), so the binding is unmotivated.
Including it is harmless but dilutes the cross-reference catalog —
and §9.2 will require resolving it back to a concrete need.

**Fix:** Either (a) drop `XC-010-001` and shift the others
up-numbered, or (b) document the actual need (e.g., "duel-loser
agent IDs stored in `HeaderExecutedEvent.contestedDuelId` may
outlive the duel frame") if one exists.

### M-7 — KD-13 (set-piece headers in scope) adds no §3 content; verify this is intentional

KD-13 says set-piece headers ARE in Stage 0 scope, with rationale
that the cross is delivered by #5 and the header is "mechanically
identical." If that's true, KD-13 is a clarification, not a load-
bearing decision — and §3 contains no set-piece-specific logic.
That's fine **if** there's no set-piece-specific physics needed
(e.g., wall presence, defender-pile interactions, in-swinger /
out-swinger spin-on-arrival differences).

Spin-on-arrival ought to be different on a corner cross vs. open-play
cross because the cross-taker spins it deliberately. But §3.6 reads
incoming spin from `BallState` regardless, so this is naturally
handled by the data path. **Action: verify**, then either confirm
KD-13 is purely a clarification or add the missing set-piece logic.

### M-8 — §4.2 cites AM #2 `§3.5.8` which doesn't exist; §3.5.6 is "Configuration Structures"

Outline §4.2 cites: `AgentMovement.GetPlayerAttributes(agentId) →
PlayerAttributes — Spec #2 §3.5.6`. Verified: AM #2 §3.5.6 is
"Configuration Structures", which contains the `Heading` field
declaration but does not define a getter API. The cited section is
adjacent-correct but not the right anchor.

KEY DESIGN DECISIONS block in outline-detailed also references
`§3.5.8 (jump kinematics)` — section `3-5-part-3.md` exists but no
specific subsection is anchored. Citation drift will produce
`TBD-NORMATIVE` flags during section-file authoring.

**Fix:** Pin exact subsection anchors in AM #2 for: (a) attribute
read API, (b) jump apex / aerial-phase exposure, (c) per-frame
`KinematicState` getter. Update §1.4 dependency table and §4.2
input contracts to match.

### M-9 — §5.4.4 duplicates a project-wide gate already enforced by #19 / #20

§5.4.4 "No `System.Random` / `DateTime.Now` usage (CLAUDE.md gate)"
is a project-wide CI gate; it should not be re-implemented in #10's
test plan. Belongs in #19 §3.x or #20 (per #20 already-APPROVED
status). Listing it here suggests #19 / #20 don't cover it (false)
or that #10 owns a duplicate gate (waste). Same logic applies less
strongly to §5.4.1 grep gate for `HeaderType` — that one is #10-
specific (which symbol to grep) and reasonably lives here.

**Fix:** Drop §5.4.4. Reference #19 §3.x or #20 §3.4 instead.

---

## LOW

### L-1 — `Perfect` as label name risks being read as a quality gate

KD-2 is explicit that `Early/Perfect/Late` labels are telemetry-only
and never branch the formula. Naming the central bucket `Perfect`
invites future readers to treat it as a gate. `OnTime`, `Centred`,
or `Nominal` would be safer. (Pass Mechanics #5 used similar naming,
so consistency vs. clarity is the call here.)

### L-2 — §1.2 ambiguous routing for set-piece kick

> Set-piece kick delivery (the kick itself) → Spec #5 (Pass) or
> Stage 1+ set-piece spec.

The "or" is ambiguous. Today: #5. Stage 1+: separate spec. State
that explicitly.

### L-3 — `GLANCING_ANGLE_THRESHOLD_RAD` is a dead constant

Listed in §3.1 with note "telemetry-only; not a gate per KD-1/KD-2"
but referenced nowhere else in §3 or §4. Either wire it into a
telemetry classifier (§2.4) or drop it.

### L-4 — Dependencies bullet says #7 "not a direct dependency, cited for tractability"

If #7 isn't a direct dependency, it shouldn't be in the upstream
dependency bullet list; that list is the load-bearing dependency
declaration for §1.4. Move #7 to a "tractability cite" footnote.

### L-5 — §3.7 "Multi-way (3+) duels: winner-only emits `HeaderExecutedEvent`" — verify §2.3 F-04 alignment

F-04 description says "emits both `HeaderExecutedEvent` (winner) and
`HeaderAttemptFailedEvent` (losers)". §3.7 step 4 phrases it as
"winner-only emits `HeaderExecutedEvent`; all losers emit failed
events." Same intent, but the phrasings should be made identical to
prevent drift between sections during authoring.

### L-6 — §8.3 has only 2 academic anchors (Bull 1985, Auger & Pellegrini 2007); "up to 6"

Pass Mechanics #5 §8 carries materially more references for a
comparable physics scope. Two named + "up to 6" is sparse and risks
§9 sign-off pushback from the physics-owner. Pre-identify 4–6 more
sources during outline phase, not during §9 audit.

### L-7 — `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` semantics

The horizon is a fixed time, but a flat header travels much further
in 1 s than a looping header. A distance horizon (or a
dual horizon: min(time_s, distance_m)) better reflects the "shape"
intent. Worth a sentence in §3.8 / Appendix A.4.

---

## CROSS-CUTTING OBSERVATIONS

### C-1 — `outline-detailed.md` heading line says "Created: May 15, 2026, 11:00 PM PST" but VERSION HISTORY says v1.0 May 15, 2026

Consistent, but the v0.1 → v1.0 jump skips a v0.2 / v0.3
intermediate. Project precedent (Spec #18, #19) favors small
incremental versions. Not a blocker — single-author outline doesn't
need the same versioning rigor as section files.

### C-2 — DOMAIN_TAG allocation pattern: precedent supports the request

DOMAIN_TAG values currently allocated (#16 §3.4, verified May 15,
2026): `0x10`–`0x14` original; `0x15` = `DOMAIN_TAG_EVENT_LEDGER`
(Event System #17 allocation, May 14, 2026). Next free slot is
`0x16`. The outline's `[CROSS-PENDING]` request is well-shaped;
once §3.10 of #16 is amended (pure namespace allocation per #17
precedent — no `DETERMINISM_DIGEST_VERSION` bump), the `[CROSS-
PENDING]` → `[CROSS]` promotion is mechanical. Recommend the
outline cite the #17 precedent explicitly in KD-10 to make the
back-prop ergonomic.

### C-3 — KD set is well-architected and closes the v0.1 review

KD-1…KD-16 do successfully resolve the 22 v0.1 findings. The
mapping table in Appendix E is the right discipline. The findings
above are all about **execution drift** between the KD layer and
the §3 / §4 / §6 layers — not about the KD set itself.

---

## RECOMMENDED V1.1 FIX PASS SCOPE

1. Resolve H-1 by splitting `timingQuality` into asymmetric form.
2. Resolve H-2 by adding `JUMP_REACH_K_HEADING` to §3.1 + §3.3, OR
   amending KD-4 to drop `Heading` from `JumpReach`.
3. Resolve H-3 by picking one of the three `headAngularVelocity`
   sourcing options and pre-committing as KD-17.
4. Resolve H-4 by reconciling §6.1 budget against §6.3 worst case
   (raise budget OR formalize tail-budget framing).
5. Resolve H-5 by tagging `0.01` and disambiguating tiebreak vs.
   noise semantics.
6. Sweep §3.4–§3.7 against §3.1; add every missing constant
   (M-1 inventory).
7. Move `IDEAL_CONTACT_FRAME_OFFSET` out of §3.1 (M-2).
8. Recalibrate §6.3 / §5.3.1 header-count expectations (M-3).
9. Reconcile draw-site catalog with formula callers (M-4).
10. Pre-commit intent-staleness policy as KD-17 or §3.2 subsection
    (M-5).
11. Pin AM #2 subsection anchors (M-8).
12. Drop §5.4.4 duplicate gate (M-9).
13. Quick-pass L-1 through L-7 (each is a single-paragraph or
    single-line edit).

Estimated v1.1 effort: 3–4 hours single-author. No KD set changes
required (KD-17 addition, if chosen, is additive).

---

## VERSION HISTORY

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | May 15, 2026 | adversarial reviewer | Pass-1 review of `outline-detailed.md` v1.0 |
