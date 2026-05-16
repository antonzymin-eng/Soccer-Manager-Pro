# Adversarial Review — Positioning AI #12 Section Files v0.1 (PASS-1)

**Created:** May 15, 2026
**Reviewer:** AI agent (claude/review-positional-ai-specs-v4rmD), self-adversarial PASS-1 against v0.1 section files.
**Scope:** `section-1.md` … `section-9-approval-checklist.md` + `appendices.md` (all v0.1, May 15, 2026), measured against CLAUDE.md, `SPEC_INDEX.md`, the v1.2 outline, and approved upstreams #1, #2, #7, #8, #16, #17, #18, #20.
**Method:** Body-text grep + worked-example math re-execution + #8 §2.2.6 / §3.1.7 cross-reference verification.
**Severity legend:** **H** = blocks `IN REVIEW` advancement (math wrong, semantic inversion, upstream-contract violation); **M** = must resolve in v0.2 fix pass (inconsistency, unsourced/unused constant); **L** = follow-up (prose tidy, glossary mismatch).
**Result:** 7 H / 9 M / 5 L = **21 findings**.

---

## Verified premises (no defect)

- `SPEC_INDEX.md` row 12: NOT STARTED. Section files were authored ahead of formal status update — same posture as #16 on May 2, 2026. Not itself a defect; flagged at L-05 for status reconciliation.
- Upstream specs #1 / #2 / #7 / #8 / #16 / #17 / #18 / #19 / #20 all APPROVED (#16 Tier 2 May 14; #9, #18, #19 May 15). Citation surface stable.
- The v1.0 outline review (`adversarial-review-v1.md`, 13 findings) was closed in `outline-detailed.md` v1.1/v1.2 before section-file drafting. Those findings are not re-litigated below; only NEW defects introduced or surviving in v0.1 section files are scored.

---

## H findings (block `IN REVIEW`)

### AR-S1-01 [H] §3.5 compactness formula is inverted relative to its prose intent — fatigue inversion class

§3.5.1 sets `lateralCompactness = baseLateral[phase] * (1 + SCORE_ATK_GAIN * scoreDiff) * (1 − FATIGUE_LATERAL_RELAX * teamMeanFatigue)`. §3.5.2 then applies `rel.y *= lateralCompactness / baseLateral[phase]`. The scalar acts as a **multiplier on displacement from the centroid**, so values >1 push agents FURTHER from centroid (looser shape), values <1 pull them IN (tighter shape).

Prose says the opposite at every site:
- §3.5.1: "each goal up tightens by 5%" — but with `scoreDiff = +2`, the multiplier becomes `1.10`, which under §3.5.2 LOOSENS the shape.
- §3.5.1: "fully fatigued team relaxes lateral compactness by 15%" — but the formula yields `(1 − 0.15·1) = 0.85`, which under §3.5.2 TIGHTENS the shape.
- §3.5.3 worked example: arrives at `lateralCompactness = 1.034` and prose calls this "net 3.4% tighter lateral shape". With `rel.y *= 1.034 / 1.00 = 1.034`, agents move 3.4% further from centroid — looser, not tighter.

This is exactly the fatigue-inversion bug class CLAUDE.md "Things That Have Gone Wrong Before" flags (FR-02 in Pass Mechanics). Two independent fixes are possible: (a) flip §3.5.2 to `rel.y *= baseLateral[phase] / lateralCompactness`; or (b) rename the scalar to `lateralSpread` and rewrite §3.5.1 gains with opposite signs. Either way, the unit test T-U-060 (which checks `lateralCompactness = 1.034 ± 0.001`) does not detect the inversion because it only validates the scalar value, not its directional effect on shape — see AR-S1-15.

**Fix locus:** §3.5.1, §3.5.2, §3.5.3, T-U-060 (add a directional assertion: leading + fatigued → centroid distance DECREASES vs. base).

### AR-S1-02 [H] §3.3.1 line-partition indices are off-by-one and inconsistent across archetypes

§3.3.1: "stable k=3 partition cuts at indices 3 and 7 of the 10-agent ordering: Defense: indices [0..3) → 4 agents (extended back line); Midfield: [3..7) → 4 agents; Attack: [7..10) → 3 agents".

Half-open interval `[0..3)` contains indices {0, 1, 2} — **3 agents, not 4**. The prose count contradicts the index expression. To get the 4/4/2 of a 4-4-2 (or 4/3/3 of a 4-3-3), the cuts must be at indices 4 and 8 (4-4-2) or 4 and 7 (4-3-3) — and they are necessarily archetype-specific (the next sentence concedes this but still ships the wrong default).

Worse, §3.3.1 quotes a "4/5/1 after grouping AM with midfield" cut for 4-2-3-1 without specifying the index pair, and Appendix B.3 places AM at `longPct = 0.65` (more advanced than DM1/DM2 at 0.40 and slightly behind LM/RM at 0.62) — so a stable sort by `agent.x` would put AM in slot 8 or 9, not in the midfield grouping. The "grouping AM with midfield" is a `[GT]` archetype rule the spec needs to state explicitly (as a per-archetype role→line override, not as an emergent partition).

**Fix locus:** §3.3.1 replace fixed cut indices with a per-archetype `lineCutIndices[]` lookup in `PositioningAIConstants.cs`; add explicit role→`defaultLine` override note for AM in 4-2-3-1; add T-U-035 covering each archetype's line cardinality.

### AR-S1-03 [H] §3.7 step order produces stale line/lane after spacing displacement; T-I-008 cannot pass

§3.7 / §3.11 order:
1. anchor → 2. offset → 3. context modifiers → 4. **resolve line+lane with hysteresis** → 5. hard-spacing displacement → 6. clamp → 7. write.

`ResolveLine`/`ResolveLane` are committed at step 4 based on the pre-displacement slot. Step 5 then mutates `slot` via §3.6.3 displacement. The committed `LineMembership` / `LaneAssignment` in `HysteresisState.lastLine` / `lastLane` therefore reflect the pre-displacement geometry — which is then digested (FR-PA-038). Two consequences:

1. **Tactical bug:** §3.4.3 "Hard (FR-PA-027): at most three agents per lane anywhere. Violation triggers cost-based displacement (§3.6) to evict the third occupant." But §3.6.3 cost is `|slot − anchor|²`, not lane-overload cost; spacing displacement is not lane-aware. T-I-008 ("Lane overload: forcing 4 agents into one lane resolves to ≤3 within 1 tick") cannot pass: nothing in the §3.7 pipeline counts lane occupants and ejects the surplus.

2. **Digest bug:** the `LaneAssignment` written into `HysteresisState` for tick T does not match the agent's actual lane at the slot finally emitted — the digest is internally inconsistent. #14 (Stage 1+) consuming `GetLane(id)` will see a different value than a downstream consumer reading `formationSlot.y` and re-classifying.

**Fix locus:** insert a dedicated "Step 4.5 — lane overload resolution" between line/lane commit and spacing; OR re-resolve line/lane AFTER spacing/clamp and digest the post-displacement values. Update §3.11 pseudocode and T-I-008 to match.

### AR-S1-04 [H] §4.4.3 misuses #8's `Stage0Default` factory — would clobber `PressingInstruction`/`PassingInstruction`/`DefensiveLineDepth` every tick

§4.4.3 step 3: "Orchestrator, per agent, calls `TacticalContext.Stage0Default(PositioningAI.GetFormationSlot(id))` to assemble that agent's per-agent `DecisionContext`."

`#8 §2.2.6` (verified, `decision-tree/section-2-1-to-2-2.md` L711–721) defines `Stage0Default(Vector2 formationSlot)` as a **match-initialisation factory** whose XML doc says: "Called by orchestrator at match initialisation." The factory body sets `PressingInstruction = MEDIUM`, `PassingInstruction = MIXED`, `DefensiveLineDepth = 0.5f` as hardcoded defaults. Calling it per agent per 10 Hz tick would overwrite any Stage 1+ writer's value on those three fields 10 times per second — exactly the kind of cross-spec contract violation the boundary matrix (§1.6) is supposed to prevent.

The correct Stage 0 pattern is: orchestrator writes `ctx.FormationSlot = positioningAI.GetFormationSlot(id);` directly (the field is `public` per `decision-tree/section-2-1-to-2-2.md` L695) — Stage0Default fires once at match init only.

**Fix locus:** §1.1, §4.4.3 step 3, §4.4.3 prose, FR-PA-002 wording. Replace "calls `Stage0Default(slot)`" with "writes `FormationSlot` field". Also flag #14/#15 reservation note: the same orchestrator path will need defined ordering vs. their writers in Stage 1+.

### AR-S1-05 [H] §3.11 pseudocode contradicts §3.5.2 — context modifiers operate on the wrong subject

§3.5.2 formula:
```
rel = anchor[agent] − centroid
rel.y *= lateralCompactness / baseLateral[phase]
rel.x *= verticalCompactness / baseVertical[phase]
anchor[agent] = centroid + rel
```
Compactness scales the **anchor** relative to centroid, BEFORE ball-relative offset is added.

§3.11 pseudocode:
```
Vector2 anchor   = ComputeAnchor(archetype, id);
Vector2 offset   = ComputeBallRelativeOffset(...);
Vector2 baseSlot = anchor + offset;
baseSlot = ApplyContextModifiers(baseSlot, centroid, modifiers, phase);
```
This applies compactness to `anchor + offset`, AFTER the ball-relative offset. The two are not equivalent — under `lateralCompactness ≠ baseLateral`, the offset contribution is scaled along with the anchor, which means the ball-pull magnitude changes with score/fatigue/intensity (silently). §3.5's three worked examples are derived under the §3.5.2 algebra (anchor-only), so test §3.5.3 doesn't reproduce the §3.11 implementation.

**Fix locus:** pick one. Either (a) update §3.11 pseudocode to scale the anchor independently and then add offset, or (b) rewrite §3.5.2 to operate on `baseSlot − centroid` and re-derive the §3.5.3 worked example.

### AR-S1-06 [H] §3.6 single-pass spacing cannot satisfy "resolves within 1 tick" under three-agent collisions

§3.6.1 iterates pairs `(i, j)` with `i.entityId < j.entityId` once. §3.6.3 displaces one agent of the pair by `sqrt(2.25) − sqrt(distSq) + 0.01 m` along the inter-agent unit vector. Three or more agents within 1.5 m of a common point (corner-kick crowd, set-piece ruck, all three CBs after a centroid pull) cannot resolve in a single pass: displacing the cheapest agent of pair (A,B) may push it into C; the (A,C) and (B,C) pairs are evaluated against pre-displacement positions if iteration order hits them before (A,B).

§3.11 calls `EnforceHardSpacing(outSlots)` once. T-I-008 asserts resolution within 1 tick. T-U-040 only tests a 2-agent collision. T-U-034 ("a 4th agent in a lane is displaced (§3.6 path)") presumes §3.6 is lane-aware — see AR-S1-03.

**Fix locus:** §3.6 must specify either (a) iterate to fixed point with a documented max-iteration cap (e.g. `SPACING_MAX_PASSES = 4`) plus a fallback when the cap is reached; or (b) prove via §6 worst-case analysis that single-pass converges on the 22-agent / 1.5 m / pitch-bounded configuration space (this is non-trivial and likely false).

### AR-S1-07 [H] §2.4 NaN sentinel for substituted agents collides with FR-PA-044 NaN guard — agent ghosts back into shape

§2.4 prose (end of section): "Substituted and red-carded agents (FR-PA-036) … their `formationSlot` is written as `(NaN, NaN)` to the orchestrator's output buffer, which the orchestrator interprets as 'no slot this tick'."

FR-PA-044 / F3: "any NaN intermediate (anchor, offset, or composed slot) is replaced with the raw role anchor." The NaN guard in §3.11 (after `ApplyContextModifiers`) fires before output is written. A substituted agent's slot — deliberately set to `NaN` by §3.5 / §2.4 — will be replaced with the raw role anchor by F3 and ghost back into the formation shape, contributing to spacing, centroid, and digest.

FR-PA-036 says the agent is "filtered out of compactness computation at §3.5 input preparation and contributes no slot output", but §3.11 pseudocode shows no such filter (no `isActive` check), and the centroid in §3.5.2 is computed over `ownTeamOutfield` unconditionally.

**Fix locus:** add explicit `if (!agent.isActive) { outSlots[id] = SENTINEL_NO_SLOT; continue; }` in §3.11 pseudocode BEFORE the NaN guard. Define a distinct sentinel (e.g. `Vector2.NegativeInfinity`) that F3 specifically does not rewrite. Update FR-PA-044 to exempt the sentinel; update §3.5 centroid to filter on `isActive`.

---

## M findings (resolve in v0.2)

### AR-S1-08 [M] `FATIGUE_LATERAL_RELAX_M = 4.0 m` is defined but never used in any formula

§6.1 catalogues `FATIGUE_LATERAL_RELAX_M = 4.0 m` `[GT]` with prose "absolute lateral spread cap added by full fatigue". The §3.5.1 formula uses only the dimensionless `FATIGUE_LATERAL_RELAX = 0.15`. No formula in §3.0–§3.9 references the `_M` constant.

Either §3.5 is missing the cap formula (e.g., a clamp `|rel.y| ≤ baseSpread + FATIGUE_LATERAL_RELAX_M · teamMeanFatigue`), or the constant is dead. Dead constants violate CLAUDE.md "constants live in their designated catalogues — no magic numbers in formula code" (the converse is also true: catalogued constants must have a referenced formula). KD-12 also implies an unused tagged constant fails approval.

**Fix locus:** decide and resolve. If the cap is intended, write the formula in §3.5.1 and the worked example; otherwise delete the constant from §6.1.

### AR-S1-09 [M] §3.0.4 phase-hysteresis worked example is off-by-one

§3.0.3 rule: "candidate transition commits only if it persists for `PHASE_HYSTERESIS_TICKS = 3` consecutive ticks."

§3.0.4: "Tick T: candidate = `TransToDef`. If `lastPhase = InPoss` and `phaseDwellTicks ∈ {1, 2}`: output remains `InPoss`. At tick T+3 with the same candidate sustained: output flips to `TransToDef`."

If tick T is the first candidate tick, then T → dwell=1, T+1 → dwell=2, T+2 → dwell=3, commit at T+2 not T+3. The worked example is one tick late. T-U-022 ("stays in `lastPhase` for at least `PHASE_HYSTERESIS_TICKS = 3` ticks") inherits the same ambiguity — does the third tick commit, or does the commit require a fourth tick?

**Fix locus:** §3.0.3 specify "commits on the Nth consecutive candidate tick" or "after N consecutive candidate ticks" unambiguously; correct §3.0.4 to match; tighten T-U-022 wording.

### AR-S1-10 [M] §3.2.2 worked example references an "8 m" pull that the formula cannot produce

§3.2.2 closing prose: "AM anchor pulls back ≈4.5 m … Full pull of 8 m would require ball at x = 0."

At `ball.x = 0`, `basisX(0) = −1`, so `offset.x = 0.60 · −1 · OFFSET_RANGE_X_M = 0.60 · −1 · 12.0 = −7.2 m`. The maximum pull at the AM pullFactor (0.60) and `OFFSET_RANGE_X_M = 12.0 m` is **7.2 m, not 8 m**. The "8 m" appears to be a stale reference to a deprecated outline value (or a confusion with `OFFSET_RANGE_Y_M = 8.0 m` on the wrong axis).

**Fix locus:** rewrite §3.2.2 closing prose with the actual 7.2 m max, or raise `OFFSET_RANGE_X_M` to 13.33 m if 8 m is the intended max for AM, then update Appendix A.7 derivation.

### AR-S1-11 [M] §3.3.3 `GK_DEPTH_M = 5.5 m` is `[GT]` but §1.2.1 says #11 Goalkeeper Mechanics owns GK behavior; this is a forward dependency on a NOT STARTED spec

`SPEC_INDEX.md` row 11: Goalkeeper Mechanics NOT STARTED. §3.3.3 publishes GK slot constants (`GK_DEPTH_M`, `GK_ADVANCE_FACTOR`, `GK_LATERAL_FACTOR`) and says "Detailed GK behavior is specified in #11 Goalkeeper Mechanics; #12 produces only the resting baseline."

Two CLAUDE.md hazards trip here:
1. **Interface Design Principle:** publishing a GK-specific formula whose downstream owner (#11) has not yet specified its consumer-side contract is exactly the phantom-interface trap (ERR-001 / ERR-004). #11's eventual spec may demand a different baseline or different parameterisation; #12 will have shipped `[GT]` values its consumer cannot honour.
2. **Constant tag policy (KD-12):** the GK constants would more properly be `[CROSS-PENDING]` against #11 once #11 declares the GK baseline contract — they should not enter `PositioningAIConstants.cs` as `[GT]` ahead of that contract.

**Fix locus:** either demote GK constants to `[EST]` with a §6.1 note "owner #11 will ratify or supersede at #11 `IN REVIEW`"; or remove the GK slot formula entirely from §3.3.3 and add a §7.x deferral "GK baseline produced by #11 directly" — and have #12 emit no GK slot at all.

### AR-S1-12 [M] §3.4.1 lane bin widths don't sum to 68.0 m if treated as `floor` bins; boundary semantics unspecified

§3.4.1 table:
| Lane | Y range (m) |
| LW | [0.0, 13.6) |
| LH | [13.6, 27.2) |
| C  | [27.2, 40.8) |
| RH | [40.8, 54.4) |
| RW | [54.4, 68.0] |

The first four bins are half-open; RW is closed on both ends. Y = 68.0 exactly is RW; Y = 54.4 exactly is RW (per the half-open `[54.4, 68.0]` interpretation, which is ambiguous — is the left endpoint inclusive or exclusive when the upper bin's left edge equals the lower bin's right edge?). Floating-point `Y = 27.2 − 1 ULP` falls in LH; `Y = 27.2` falls in C. Combined with §3.4.2 hysteresis (`LANE_HYSTERESIS_M = 2.0 m`), boundary equality cases should be specified.

Also: `68.0 / 5 = 13.6` exactly in decimal but `13.6` is not exactly representable in binary `float`. Repeated boundary computation as `(i + 1) * 13.6f` is not bit-identical to writing `13.6f` literal in the table. This bites the float-determinism KD-16.

**Fix locus:** rewrite §3.4.1 with explicit `LaneEdge` array `[0.0f, 13.6f, 27.2f, 40.8f, 54.4f, 68.0f]` literal (one `static readonly` array, no computation); specify inclusive-left exclusive-right for all bins with explicit clamp at `Y = 68.0f → RW`; add T-U-035 boundary test at each edge.

### AR-S1-13 [M] §3.5.2 centroid is computed but never specified — over which agents, with what weights, when?

§3.5.2 references `centroid` and §3.11 calls `ComputeCentroid(perception, archetype)`, but no formula appears. Is it the mean of own-team outfield positions? Mean of own-team anchors? Mean of own-team slots? Weighted by role? Centroid choice is decisive for compactness magnitude: anchor-centroid is fixed per archetype; position-centroid drifts with the game. T-T-005 ("centroid `y > 36 m`") depends on which centroid is meant.

**Fix locus:** add §3.5.2.0 "Centroid Definition" — define the centroid explicitly (likely: mean of own-team-outfield current `agent.position`, GK excluded, inactive filtered), and include the formula in §3.10 catalogue.

### AR-S1-14 [M] §3.6.4 worked example doesn't recompute lane after displacement

§3.6.4: A at `(50.0, 30.0)` and B at `(50.8, 30.6)`. A is displaced (smaller cost). Displacement magnitude: `1.5 − 1.0 + 0.01 = 0.51 m` along `(slot[A] − slot[B]) / ||..||`. Direction from B to A is `((50.0 − 50.8), (30.0 − 30.6)) = (−0.8, −0.6)`, magnitude 1.0, so unit vector `(−0.8, −0.6)`. A's new slot: `(50.0 + 0.51·−0.8, 30.0 + 0.51·−0.6) = (49.59, 29.69)`. Distance to B: `sqrt((50.8 − 49.59)² + (30.6 − 29.69)²) = sqrt(1.4641 + 0.8281) = sqrt(2.292) = 1.514 m` — just above 1.5 m, OK. But A's new `(49.59, 29.69)`: A was previously in lane C (`27.2 ≤ 30.0 < 40.8`) and now in lane C still (`27.2 ≤ 29.69 < 40.8`). No lane change. But §3.7's step 4 committed A's lane BEFORE the displacement (AR-S1-03). The worked example is silent on what `lastLane[A]` records.

**Fix locus:** the worked example should explicitly state the line/lane state pre- and post-displacement, both to anchor the AR-S1-03 fix and to give T-U-041 / T-U-042 a concrete expected `HysteresisState` to assert.

### AR-S1-15 [M] §5.2.7 T-U-060 does not actually detect the AR-S1-01 inversion

T-U-060: `scoreDiff = +2`, `fatigue = 0.4`, `InPoss` → `lateralCompactness = 1.034 ± 0.001`. This asserts the SCALAR is correct. It does NOT assert that "shape is tighter than at `scoreDiff = 0, fatigue = 0`". As-written, the test passes whether §3.5.2 multiplies `rel.y` by `lateralCompactness / baseLateral` (looser under +2 lead) or by `baseLateral / lateralCompactness` (tighter). The inversion in AR-S1-01 slides past CI.

**Fix locus:** add T-U-063 — same inputs, assert `mean(|rel.y|)` over own outfield is STRICTLY LESS THAN under baseline `(scoreDiff = 0, fatigue = 0, intensity = base)`. Reciprocal directional tests for vertical compactness via `INTENSITY_VERTICAL_GAIN`.

### AR-S1-16 [M] §7.8 channel reservation says "Stage 0+1" but §7 is by definition Stage 1+ deferrals

§7 preamble: "All Stage 0 deferrals enumerated below per KD-11." §7.8 table:
| `SHAPE_TRANSITION` | Phase transitions for debug overlay (Appendix C) | **0+1** |

A channel labelled "Stage 0+1" inside a Stage 1+ deferral section is self-contradictory — either it ships at Stage 0 (in which case it must be #17-back-propagated NOW, like `ERR-017-001` was for #16) or it ships at Stage 1+ (then drop the "0+" and align with §7 header). KD-10 says "no #17 channels at Stage 0", so the channel cannot ship at Stage 0 — the "0+1" label is the error.

**Fix locus:** §7.8 retag both channels as "Stage 1+", and remove the implicit promise that `SHAPE_TRANSITION` is Stage-0-deliverable.

---

## L findings (follow-up)

### AR-S1-17 [L] Appendix F glossary says "Compositor … six sequential steps" but §3.7 lists seven steps

Appendix F: "Compositor — The Stage 0 simplified slot-composition pipeline (§3.7) — six sequential steps producing the per-agent `formationSlot`." §3.7 lists steps 1–7 (anchor / offset / context / hysteresis / spacing / clamp / write). Either drop "write" as bookkeeping (six) and update §3.7, or fix glossary to "seven steps".

### AR-S1-18 [L] `XC-012-NNN` IDs allocated without `spec-error-log.md` evidence anchor

§8.3 allocates `XC-012-001..009`. None are visible in `docs/tracking/spec-error-log.md`. The #9 / #16 / #17 / #19 precedent is that `XC-NNN-NNN` allocation gets a (typically short) acknowledgement row in the error log so cross-spec readers can find them. Not a defect per se — `XC` is documentary, not erratum — but it breaks searchability.

### AR-S1-19 [L] FR-PA-034 "DELETED" row should be either renumbered out or footnoted with original wording

§2.1 currently shows: "FR-PA-034 | *(DELETED — `StableHash` field dropped per v1.2 outline resolution.)*". 47 active FRs across IDs 001..048 minus 034 (a sparse table). CLAUDE.md doesn't forbid sparse FR IDs, but other specs (#18, #19) tend to renumber. The current row is fine; consider adding a footnote with the deleted FR's original wording for archaeology.

### AR-S1-20 [L] §6.3 "Editor Profile, Release configuration" is mutually contradictory

Unity profile terminology: "Editor" mode runs the editor; "Release" is a Player build configuration. The two cannot coexist. KD-15 reference-host pin should specify either "Editor playmode profiler" (with explicit caveat that perf differs from Player) or "Standalone Player, IL2CPP, Release". Since §6.3 separately says "Engine: Unity 2022.3 LTS, Mono backend", the intended pin is likely "Editor playmode" — say so.

### AR-S1-21 [L] `SPEC_INDEX.md` row 12 status not reconciled with section-file presence

`SPEC_INDEX.md` row 12: NOT STARTED. v0.1 section files exist. Same pattern as #16 on May 2, 2026 (which got an explicit status-reconciliation OPEN ISSUE entry on May 2 before formal IN PROGRESS flip). #12 needs the equivalent: either flip to `IN PROGRESS` in `SPEC_INDEX.md` + CLAUDE.md OPEN ISSUES, or note the discrepancy in `PROGRESS.md`. Filing this as L not M because no cross-reference depends on the row's value yet — `ERR-012-001` / `ERR-012-002` are filed against #16 / #8 respectively, neither against the #12 row directly.

---

## Cross-cutting observations (not scored)

- **Math review depth:** I re-executed §3.0.4, §3.1.2, §3.2.2, §3.5.3, §3.6.4, §6.5 worked examples. Three of six contained arithmetic or directional errors (§3.5.3, §3.2.2, §3.0.4). Recommend the v0.2 fix pass replay every worked example against the published constants.
- **#8 cross-reference depth:** I verified `Stage0Default` against `decision-tree/section-2-1-to-2-2.md` L711–721 and `MOVE_TO_POSITION` against `section-3-1.md` L688–725. AR-S1-04 turned on the XML doc comment "Called by orchestrator at match initialisation", which is enforcement context that body-text grep alone does not surface — recommend the §8.5 grep list add an XML-doc-comment scan against the cited consumer methods.
- **Test coverage of directional invariants:** AR-S1-01 / AR-S1-15 expose a gap — most unit tests assert numerical equalities, not directional inequalities. Suggest a §5.2.9 "Directional Invariant" subsection that for every monotone formula in §3 asserts the sign of the partial derivative w.r.t. its driving variable.
- **`master-development-plan.md` grep for archetype count (Outstanding Question #1 in AR-V1-07):** the v1.2 outline claims to have closed this; the section files cite `master-development-plan.md §3.2` but do not quote the line range. Recommend Appendix B preamble add the exact line citation, same pattern as §7.6's `lines 441–449`.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 15, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial review of `section-1..section-9 + appendices` v0.1. 21 findings (7 H / 9 M / 5 L). |
