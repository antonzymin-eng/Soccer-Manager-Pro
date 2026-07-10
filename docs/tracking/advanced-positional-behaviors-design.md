# Advanced Positional Behaviors — Design Supplement

> **Created:** July 7, 2026
> **Last Updated:** July 8, 2026 (v0.4 — **PROMOTED**: all three candidates authored as section
> files at `IN REVIEW` — `docs/specs/dismarking-ai/` (#23, FR-DM), `docs/specs/build-up-structures/`
> (#24, FR-BU), `docs/specs/positional-rotations/` (#25, FR-RO) — completing §6 steps 1–3 for all
> three (`SPEC_INDEX.md` registry rows added; RESERVED entries retired). The promoted specs
> supersede this note where they deviate (notably #24 §1 KD-3 refines this note's KD-3
> `TransitionWon` gating into an opt-in dial + post-regain suppression window, with rationale).
> Also: §6's "Specification Before Code" citation corrected root `CLAUDE.md` → `README.md`
> (the heading lives in README.md; verified by grep).)
> **Prior:** v0.3 — new §6 Implementation Plan: the per-candidate
> spec-promotion pipeline (outline → section files → PASS-1/2 → sign-off → `APPROVED` → T0 code
> → match-engine wiring → implementation AR cycle) plus a recommended #23→#24→#25 sequencing.)
> **Prior:** v0.2 — AR-1 fix: KD-3's gating-dial citation corrected from a generic
> `TransitionPlan` reference to the specific `TeamTactic.TransitionWon` field, verified against
> `src/tactical-instructions/TeamTactic.cs`.
>
> **Status:** DESIGN SUPPLEMENT (forward-looking; **NOT** a formal approved spec, **NOT** yet
> implemented, **NOT** yet section-file drafted). Parallel in status to the pre-promotion
> `tactical-instruction-layer-design.md` (→ Spec #21) and `living-world-system-design.md`
> (→ Spec #22). No code is authored from this note until it is reviewed, split, and promoted.
> **Author:** —
> **Purpose:** Scope three related new-behavior-class items surfaced by the July 7, 2026
> tactical-theory cross-reference (see root `CLAUDE.md` OPEN ISSUES, "four cheap-item tactical
> additions LANDED") that were explicitly assessed there as **too large for a routing-seam
> reuse**: dismarking, scripted build-up structures, and positional rotations. All three need
> new per-agent decision logic — either marker-awareness state or dynamic slot reassignment —
> not just a new tactic dial consumed by an existing seam. This note is a **joint feasibility
> and architecture pass** across the three; each is a candidate for its own numbered spec (see
> §5) once reviewed, not a single spec.

---

## 0. Why these three are grouped here

The July 7, 2026 tactical-theory cross-reference landed four cheap items (`MarkingOrientation`,
rest-defense coverage, half-spaces PASS bonus, blind-side press bias) by routing an existing
per-agent value into an existing scorer/selector — the `TacticTranslation` + routing-field
pattern established by Spec #21. That same pass identified four items that do **not** fit the
pattern and were deliberately left out:

| Item | Why it doesn't fit the cheap-seam pattern |
|---|---|
| Dismarking | Needs a new **marker-awareness** signal — no existing field carries "how tightly am I being marked" to the marked player's own decision loop |
| Scripted build-up structures | Needs a new **phase-gated structural overlay** on formation positioning, not a scalar multiplier on an existing action |
| Positional rotations | Needs **dynamic slot reassignment** — today's `FormationSlotRecord` binding is fixed per agent for the match; a rotation swaps it |
| Game model / AI-manager tactics | (covered in the sibling note `game-model-ai-manager-design.md` — different substrate, preset layer + manager decision logic, not a positional behavior) |

The first three share a dependency shape (all extend #7 Perception System / #8 Decision Tree /
#12 Positioning AI) and a common architectural hazard (see §2 KD-5), so they are scoped together
here. They are **not** proposed as one spec — §5 reserves three separate candidate numbers,
matching the project's "each would need its own spec pass" assessment.

---

## 1. What already exists vs. what each item adds

| Concern | Existing construct | What's missing |
|---|---|---|
| Opponent proximity awareness | `Perception System #7` `FilteredView.PerceivedAgent[]` (position, team, recognition latency) | A per-agent **derived marking-pressure signal** (closest tracked opponent's dwell/proximity trend) feeding a new evasive-movement adjustment |
| Off-ball positioning | `Positioning AI #12` `SlotComposer` (anchor → offset → modifiers → spacing → clamp → lines → lanes) | A **dismarking offset stage** inserted into the pipeline, gated on the new marking-pressure signal |
| Possession-phase structure | `Positioning AI #12` `Phase {InPoss/OutOfPoss/TransToAtk/TransToDef}` + formation pull-factor table (static per phase) | A **build-up sub-phase table** keyed on ball progression zone (own third / middle / final third) with its own anchor offsets, gated by `TacticalInstructions #21` `TeamTactic.TransitionWon` |
| Formation slot identity | `Positioning AI #12` `FormationSlotRecord` — one fixed slot per agent for the whole match (`AgentPositioningData.SlotIndex`) | A **dynamic slot-reassignment controller** that swaps two agents' `SlotIndex` bindings under a triggering condition, with hysteresis against thrashing |
| Marker's own targeting | `Defensive AI #14` `MarkAssignment.TargetEntityId` (opponent-internal state) | **Not** consumed here — see KD-5. The marked player must react to what it *perceives*, not to the opponent team's internal directive struct |

---

## 2. Architectural decisions (candidate KDs — subject to revision at spec-authoring time)

**KD-1 — Joint scoping, separate specs.** This note analyzes shared substrate and shared
hazards across all three items so the eventual three specs do not duplicate that analysis or
diverge on the perception-boundary rule (KD-5). Each item is promoted to its own
`docs/specs/<folder>/` at its own pace, in any order; none blocks another.

**KD-2 — Dismarking is a perception-derived signal, not a directive-read.** A marked player must
not read `MarkAssignment.TargetEntityId` from the opposing team's `Defensive AI #14` internal
state — that is the marker's private tactical output, and reading it would give the attacker
omniscient knowledge of a defender's intent the real perception model (#7) has no channel for.
Instead, dismarking consumes only what `PerceptionSystem.FilteredView` already exposes for the
nearest opponent(s): position, recognition latency, closing trend. A new derived signal —
tentatively `MarkingPressure` — computes a bounded scalar from proximity + dwell time (frames the
nearest opponent has stayed within a marking radius), analogous in shape to the existing
`RestDefenseEvaluator` (#12 §7.13) pattern: a pure evaluator consuming an existing snapshot,
producing one new routing field, gated behind a tactic dial default-off.

**KD-3 — Build-up structures are a formation-table extension, not a new action type.** The
Decision Tree's parameter-based-physics / no-type-enum rule (root `CLAUDE.md` "Parameter-Based
Physics") extends by analogy: build-up structure should not introduce a new `ActionType`. It
extends `Positioning AI #12`'s existing anchor/offset pipeline with a build-up sub-table indexed
by ball-progression zone, consumed as an additional offset stage — the same shape as
`ContextModifier`'s existing lateral/vertical compactness stage. Gated by `TacticalInstructions
#21 TeamTactic.TransitionWon` (§3.2/FR-TI-020 — the "plan on winning the ball" dial; the field is
already serialized into the world-state snapshot via `MatchEngine.WriteTeamTactic`, per the source
grep run for this note, but has **no AI-side consumer yet** anywhere in Positioning AI, Decision
Tree, or the other Mechanics AIs — it is a genuinely free, already-declared seam for this item to
land on, not a new field).

**KD-4 — Rotations are the highest-risk item; scope narrowly.** Swapping two agents'
`FormationSlotRecord` bindings mid-match is a genuinely new state mutation — nothing in the
current architecture reassigns `SlotIndex` after `SeedFromFormation`. A rotation controller needs:
(a) a triggering condition (proximity/phase-based, evaluated per formation-adjacent slot pair,
not all-pairs — O(n²) is out of budget at 10 Hz for 11 agents but the adjacency set is small and
static per formation); (b) a hysteresis dwell lock (parallel to `AgentHysteresisState`) so a
rotation, once taken, holds for a minimum number of ticks before reverting or re-triggering;
(c) an explicit interaction contract with the existing `ShapeAnalyzer` line/lane re-sort (a
rotation must not fight the line-assignment dwell logic that already exists for a different
purpose). This item alone likely justifies the largest of the three specs.

**KD-5 — Perception-boundary invariant (applies to all three).** No new subsystem in this group
may read another team's internal AI directive struct (`MarkAssignment`, `PressDirective`,
`AttackDirective`) directly. All opponent-derived signals must route through `Perception System
#7`'s `FilteredView`, preserving the same-invariant the project already relies on for #8's
decision-making (an agent decides off what it *perceives*, never off omniscient state). This is
the first candidate KD each of the eventual three specs' §1 should cite verbatim.

**KD-6 — Default-neutral until a tactic opts in.** Following the #21/#12/#13/#14 cheap-item
precedent (root `CLAUDE.md` OPEN ISSUES, July 7, 2026 entry), all three items must default to
today's exact behaviour — no dismarking offset, no build-up sub-table activation, no rotation
trigger — until a manager sets a non-default tactic dial. None of the three should require a new
`SNAPSHOT_SCHEMA_VERSION` bump to reach this default-neutral state (any new per-agent hysteresis
state does bump it once wired, same as every prior AI extension in this project).

---

## 3. Rough scope estimate (for spec-authoring sequencing, not a commitment)

| Candidate | Extends | New per-agent state | Estimated FR count (illustrative) | Relative size |
|---|---|---|---|---|
| Dismarking & Marker-Awareness | #7, #8, #12 | `MarkingPressure` scalar + dwell counter | ~12–18 | Small–Medium |
| Scripted Build-Up Structures | #12, #21 | build-up sub-phase index per agent | ~15–20 | Medium |
| Positional Rotations | #12 | rotation hysteresis state per adjacent slot pair | ~20–28 | Medium–Large |

These are rough-order estimates for sequencing discussion only — not FR-numbered, not
approval-gating, and not binding on the eventual spec authors.

---

## 4. Open questions (to resolve before section-file drafting begins)

1. Does `MarkingPressure` need its own RNG draw site, or is it a pure deterministic function of
   already-perceived state? (Current lean: pure function — no new draw site, matching
   `RestDefenseEvaluator`.)
2. Should the build-up sub-table be a fixed small catalogue (own-third / middle-third /
   final-third, 3 rows) or a continuous function of `x`-position? Precedent
   (`PositioningAIConstants` formation tables) favors a small discrete catalogue.
3. For rotations: is the adjacency set (which slot pairs are eligible to rotate) a static
   per-`FormationFamily` table, or does it need to be tactic-configurable? Static table is the
   cheaper Stage-1 answer; tactic-configurable is a larger surface.
4. Do any of the three need a new domain tag / `SubsystemOrdinals` allocation in
   Deterministic Simulation #16 §3.4, or do they route through the existing Mechanics-layer
   ordinals (Positioning AI = 20)? Likely the latter — these are refinements within #12's existing
   ordinal, not new subsystems.

---

## 5. Candidate spec numbers (reserved, not yet promoted)

Per `SPEC_INDEX.md` "Before creating a new spec folder, add the entry here first" — these numbers
are **reserved** here to prevent a future renumbering collision (the project's most recurring bug
class per root `CLAUDE.md` "KNOWN HAZARD — Spec Renumbering Cascades") but are **not** added to
the `SPEC_INDEX.md` registry table until each is promoted to section files, matching the #21/#22
precedent (design note → promoted spec, registry row added at promotion, not before).

| Candidate # | Working title | Folder (reserved, not yet created) |
|---|---|---|
| 23 | Dismarking & Marker-Awareness AI | `dismarking-ai/` |
| 24 | Scripted Build-Up Structures | `build-up-structures/` |
| 25 | Positional Rotations | `positional-rotations/` |

See the sibling note `game-model-ai-manager-design.md` for candidate #26 (Tactical Presets &
AI-Manager Selection), reserved separately since it extends a different substrate
(`TeamTacticConfig`/`PlayerTacticConfig`, not the on-pitch AI pipeline).

---

## 6. Implementation plan

Per `README.md` "Specification Before Code" and the project's own recurring precedent (every
one of #1–#22 went spec-first), **no `src/` code is written from this note directly.** The plan
below is the promotion pipeline each of #23/#24/#25 must pass through — identical in shape to how
`tactical-instruction-layer-design.md` became Spec #21 and `living-world-system-design.md` became
Spec #22 — followed by the T-phase code-landing sequence once a candidate is `APPROVED`.

**Recommended sequencing: #23 (Dismarking) → #24 (Build-Up Structures) → #25 (Positional
Rotations), in that order.** Rationale: §3's size table ranks them smallest-to-largest; #23 also
has the cleanest existing precedent to imitate end-to-end (`RestDefenseEvaluator` — a pure
evaluator over an existing snapshot, one new routing field, one match-engine writer, one
`TestOnly_*` seam, one `MatchEngineTacticTests` case), so building it first de-risks the KD-5
perception-boundary pattern before #24/#25 lean on it. Nothing *requires* this order — each is
independently promotable — but it minimizes wasted rework if the KD-5 pattern needs adjustment
after the first implementation.

**Per-candidate promotion pipeline (repeat for each of #23/#24/#25):**

1. **Outline.** Author `docs/specs/<folder>/outline.md` (or `outline-detailed.md` directly, per
   the #22 precedent of skipping straight to a detailed outline when the design supplement
   already carries enough KD-level detail) from this note's relevant §1–§4 content. Assign the
   real FR-ID prefix (e.g. `FR-DM-*` for Dismarking, `FR-BU-*` for Build-Up, `FR-RO-*` for
   Rotations — verified by grepping `docs/specs/**/*.md` for existing `FR-[A-Z]+-` prefixes
   that `FR-PR-*` is already Pressing AI's (#13) prefix and would collide, while `DM`/`BU`/`RO`
   are unused; re-grep before final assignment in case a prefix was claimed since this note).
2. **Promote `SPEC_INDEX.md`.** Move the candidate's row from the "RESERVED" section (added in
   this pass) into the main registry table at status `NOT STARTED` or `IN PROGRESS`, per the
   `SPEC_INDEX.md` "Before creating a new spec folder, add the entry here first" rule.
3. **Section files.** Author the full 9-section template (§1 Introduction/scope/KDs, §2 FRs +
   data structures + failure modes, §3 formulas/algorithms, §4 architecture/file layout/interface
   contracts, §5 test plan, §6 performance budget, §7 future extensions, §8 references, §9
   approval checklist) + appendices, per `CLAUDE.md` "SPEC FILE CONVENTIONS". §1 MUST cite this
   note's KD-5 (perception-boundary invariant) verbatim, per §2 KD-5 above.
4. **PASS-1 adversarial review** of the section files (fresh-eyes, whole-document sweep — the
   project's typical first-pass yield for a spec this size is in the 1H–3H / 3M–7M / 3L–5L range
   per the #13/#14/#15/#21 precedents). Fix pass in the same or next commit.
5. **PASS-2 (if PASS-1 found High-severity issues) or proceed to sign-off (if PASS-1 was
   clean/Low-only).** Repeat until a pass yields no High findings — this note's own AR-1/AR-2
   cycle (§ above) is the pattern to follow at the section-file level too.
6. **Cross-spec back-props.** File `ERR-<spec>-NNN` entries for any amendment this candidate
   needs in an already-`APPROVED` spec's own text (by analogy: #21 needed `TacticalContext`
   nullable-field amendments in #8; #23 will likely need a new `TacticalContext` or
   `PositioningPerceptionSnapshot` field the same way). Land the back-prop patch in the target
   spec atomically with this candidate's own `APPROVED` transition, per the established
   freeze-then-amend pattern.
7. **Lead-developer R-01..R-05 sign-off → `APPROVED`.** Update `SPEC_INDEX.md`, `PROGRESS.md`,
   `README.md` per the standard closing-a-spec checklist every prior spec followed.
8. **T0 code scaffolding.** Per KD-2/KD-3 above, none of the three candidates obviously needs a
   brand-new assembly the way Tactical Instructions (#21) or Living World (#22) did — they are
   small-to-medium extensions of already-existing assemblies (#7/#8/#12). Default plan: land new
   files directly inside the extended assembly (e.g. a new `positioning-ai/MarkingPressureEvaluator.cs`
   for #23, mirroring `RestDefenseEvaluator.cs`'s placement) rather than a new `src/<folder>/`
   tree — confirm this placement decision explicitly in each candidate's own §4 (Architecture)
   before authoring code, since it is exactly the kind of structural decision `CLAUDE.md`
   "KNOWN HAZARD" table warns gets made informally and then drifts.
9. **Match-engine wiring.** One new routing field + one `TestOnly_*` seam + one
   `MatchEngineTacticTests` case, mirroring the `MarkingOrientation`/`RestDefenseSufficient`/
   `AgentLane` cheap-item precedent (root `CLAUDE.md` OPEN ISSUES, July 7, 2026 entry) — every one
   of those defaulted to identity behaviour until a manager opts in, and so must every item here
   (KD-6).
10. **Implementation-level AR cycle.** Adversarially review the landed code (AR-1, AR-2, …) until
    a pass converges clean, per the project's universal post-landing convention.

**Definition of done for this plan's own scope:** this section is satisfied once all three
candidates have reached step 2 (promoted out of `SPEC_INDEX.md` "RESERVED") for at least the
first candidate in the recommended sequence — the remaining steps are the substance of each
candidate's own eventual spec and implementation work, not further design-supplement content.

---

## VERSION HISTORY

| Version | Date | Notes |
|---|---|---|
| 0.4 | 2026-07-08 | PROMOTED — #23/#24/#25 section files authored at `IN REVIEW` (§6 steps 1–3 complete for all three); `SPEC_INDEX.md` registry rows added, RESERVED entries retired. Promoted specs supersede this note on deviation (#24 KD-3 gating refinement recorded there). §6 citation fix: "Specification Before Code" is a `README.md` heading, not root `CLAUDE.md`. |
| 0.3 | 2026-07-07 | Added §6 Implementation Plan — the per-candidate spec-promotion pipeline + recommended #23→#24→#25 sequencing rationale. Self-review of the plan caught the proposed `FR-PR-*` prefix for Rotations colliding with Pressing AI's (#13) existing prefix (grep-verified against `docs/specs/**/*.md`); corrected to `FR-RO-*`. |
| 0.2 | 2026-07-07 | AR-1 fix (0H+0M+1L): KD-3 (and the parallel §1 table cell) cited a generic `TeamTactic.TransitionPlan` field as the build-up gating dial; the struct actually has no such field, only `TransitionWon`/`TransitionLost` (both backed by the `TransitionPlan` enum type). Corrected both citations to `TransitionWon` specifically, with the no-AI-consumer-yet claim re-verified by grep. |
| 0.1 | 2026-07-07 | Initial creation — joint scoping note for dismarking, scripted build-up structures, and positional rotations, per the July 7, 2026 tactical-theory cross-reference's "considered but NOT implemented" carve-out. |
