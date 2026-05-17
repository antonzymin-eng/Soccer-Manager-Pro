# Adversarial Review — Pressing AI #13 Section Files v0.1 (PASS-1)

**Created:** May 17, 2026
**Reviewer:** AI agent (claude/adversarial-ai-spec-review-zkYuQ), self-adversarial PASS-1 against v0.1 section files.
**Scope:** `section-1.md` … `section-9-approval-checklist.md` + `appendices.md` (all v0.1, May 17, 2026), measured against CLAUDE.md, `SPEC_INDEX.md`, `outline-detailed.md` v1.0, and approved / IN-REVIEW upstreams #1, #2, #4, #5, #7, #8, #11, #12, #16, #17, #18, #19, #20.
**Method:** Body-text grep + worked-example math re-execution + #8 / #5 / #12 / #16 / #18 cross-reference anchor verification.
**Severity legend:** **H** = blocks `NOT STARTED → IN REVIEW` advancement (citation to non-existent anchor, semantic inversion, upstream-contract violation, phantom interface); **M** = must resolve in v0.2 fix pass (internal contradiction, unsourced constant, drift between FR and §3 algorithm); **L** = follow-up (prose tidy, glossary mismatch, redundant duplication).
**Result:** 6 H / 7 M / 4 L = **17 findings**.

---

## Verified premises (no defect)

- `SPEC_INDEX.md` row 13: `NOT STARTED`. Section files were authored ahead of the formal status flip — same pattern as #12 on May 15, 2026 and #16 on May 2, 2026. §9.7 explicitly states the status flip is gated on PASS-1 review, which is correct posture; not itself a defect.
- The Stage-binding clarification (§1.8) correctly anchors #13 runtime to Stage 1, which dissolves the May-6 outline H-4 finding about Pass Mechanics #5 SUSPENDED-status risk (#5 was re-approved May 6, 2026, and #13 runtime is Stage 1 anyway).
- `ERR-013-005` `DOMAIN_TAG_PRESSING_AI = 0x19` inheritance from the ERR-012-001 shifted block (`0x17…0x1C` post-May-16 after #10 took `0x16`) is internally coherent given the published #11 / #12 ordering (#11 → `0x17`, #12 → `0x18`, #13 → `0x19`). Verified that `deterministic-sim/section-3.md` currently allocates through `0x16` only, so the slot is still nominally available.
- `ERR-013-004` (stale "Fatigue System #13" reference at `decision-tree/section-3-1.md` L753) is real and verified at L753 — one-token patch correctly characterised.

---

## H findings (block `NOT STARTED → IN REVIEW`)

### AR-S1-H1 [H] Citation to non-existent anchor `#8 §1.4.21` — appears 11+ times across spec text

§1.1 closing sentence: "declare the integration surface with Decision Tree #8 — where the runtime activation lands at **Stage 1** per #8 §1.4.21." §1.8: "Authoritative basis: Decision Tree #8 §1.4.21 (\"No coordinated pressing... Stage 1 — Pressing AI #13 introduces coordinated press triggers\")". `outline-detailed.md` v1.0 carries the same anchor in the METADATA HEADER, KD-3, §1.8, and §8.1 row.

**Decision Tree #8 §1.4.21 does not exist.** `decision-tree/section-1.md` §1.4 ("Key Design Decisions") subsections enumerate (verified by `grep -n "^### 1\."`): no §1.4.21. The actual home of the "No coordinated pressing" deferral is `decision-tree/section-1.md` **§1.3.2** (Features Deferred to Stage 1+) — body text "Multi-agent coordination... coordinated pressing triggers are Stage 1+" at L231–232 — and the table row at L426 carrying the quoted "Stage 1 — Pressing AI #13 introduces coordinated press triggers" prose. The "Pressing AI #13 (Stage 1) — Coordinated press state — DT will consult before scoring PRESS" row #13 §1.8 attributes to "#8 §1.5" is actually at L467 of `decision-tree/section-1.md`, which is inside **§1.7.2 (Soft Dependencies — Forward References)** — NOT §1.5 (which is the "Stage 0 Action Set"). Both anchors are wrong; the same drift propagates to §8.1 (`XC-013-013` and `XC-013-014`) and KD-12.

This is exactly the stale-spec-number / wrong-anchor hazard class CLAUDE.md "Things That Have Gone Wrong Before" calls out. Because §1.8 / FR-PR-044 / KD-3 / KD-12 are the authoritative load-bearing citations for the entire Stage-binding decision, advancing the spec to `IN REVIEW` with a hallucinated anchor would propagate the error.

**Fix locus:** §1.1, §1.8, §8.1 rows (`XC-013-013`, `XC-013-014`), KD-3, KD-12, FR-PR-044 source column. Replace `#8 §1.4.21` with `#8 §1.3.2` (deferral prose) and the §1.4 "Key Design Decisions" row at L426. Replace `#8 §1.5` (which is "Stage 0 Action Set") with `#8 §1.7.2` (soft-dependency row at L467). Same fixes propagate back into `outline-detailed.md` v1.0 — record those amendments in the v0.2 fix-pass note.

### AR-S1-H2 [H] FR-PR-010 / §3.1.2 / Appendix B.2 read `passVelocity` from `PassAttemptEvent`, but #5's documented payload does not contain a velocity vector

§3.1.2 trigger formula: `dot(normalize(e.passVelocity.xy), attackingDirection) < BACKWARD_PASS_THRESHOLD`. §4.4.2: "`PassEventRing` is a per-tick read of `PassAttemptEvent` instances... Each event carries the kick velocity vector." Appendix B.2 row "Inputs: `passVelocity ∈ ℝ³` (kick)". §8.5 grep claim: "verified at `pass-mechanics/section-1.md` L274 and `pass-mechanics/section-2.md` L330 (FR-08 publish-at-`CONTACT`). No dedicated 'directional' or 'backward' event; #13 dots the kick-velocity payload locally."

Verified at `pass-mechanics/section-2.md` FR-10 §349–352: the documented `PassAttemptEvent` payload is "**`AgentID`, `PassType`, `TargetPosition`, `FrameNumber`**". There is no `passVelocity`, `kickVelocity`, or any velocity-bearing field on the published event. `TargetPosition` is a Vector position, not a velocity vector. Computing `dot(passVelocity, attackingDirection)` from this payload is not possible without either: (a) #13 deriving the velocity from `Ball.ApplyKick` side-effects (which #5 §3 owns and does not expose as a public surface), or (b) #5 amending FR-10 to add a velocity field.

Compounding: #13 cites the wrong FR number for the publishing requirement. #5 §2.1 FR table shows **FR-08 = "Weak Foot Penalty"** (L59); **FR-10 = "Event Publishing"** (L61, L328–352). #13 §1.3 / FR-PR-010 / Appendix B.2 / §8.1 row all cite "#5 §2 FR-08" when they mean FR-10.

Either (a) file a back-prop ERR-013-006 against #5 FR-10 to add a velocity field to the `PassAttemptEvent` payload (and re-cite as FR-10), or (b) re-source `BACKWARD_PASS` from a different surface (e.g., compute pass direction at #13 from `passer.position → TargetPosition` and dot that vector against `attackingDirection`). Option (b) preserves #5's frozen payload and is consistent with KD-1 "cite-not-redefine".

**Fix locus:** §3.1.2 formula re-derivation; §4.4.2 surface description; Appendix B.2 Inputs/Threshold rows; §8.1 row for #5; §8.5 grep claim; FR-PR-010 wording; OI-005 re-opens.

### AR-S1-H3 [H] §3.3 eligibility line "Stamina ≤ `PRESS_FATIGUE_CEILING [GT]`" inverts the fatigue/stamina semantics — fatigue-inversion class

§3.3 enumerates eligibility constraints:
> 1. Stamina ≥ `PRESS_STAMINA_MINIMUM` per #8 §3.1.8.1 (cite-not-redefine).
> 2. Stamina ≤ `PRESS_FATIGUE_CEILING [GT]` (FR-PR-029; #13-added).

CLAUDE.md fatigue convention: `0.0 = fully rested, 1.0 = fully fatigued`. `PRESS_FATIGUE_CEILING = 0.85` is a *fatigue* ceiling (high fatigue → exclude). The constraint should read **"Fatigue ≤ `PRESS_FATIGUE_CEILING`"** (i.e., an agent with fatigue 0.86 is excluded). Written as "Stamina ≤ 0.85" the rule excludes agents with high stamina — the exact opposite. This is *the* canonical fatigue-inversion bug class CLAUDE.md "Things That Have Gone Wrong Before" calls out (FR-02 Pass Mechanics precedent).

FR-PR-029 itself is correctly worded ("An agent with fatigue ≥ `PRESS_FATIGUE_CEILING [GT]`... is excluded"), so the inversion is localised to the §3.3 prose — but §3.3 is what an implementer would copy into `PrimaryPressSelector.cs`.

Compounding (cite-not-redefine violation): §3.7 prose declares "stamina is the complement of fatigue; see CLAUDE.md fatigue convention". **CLAUDE.md does not define this relationship.** It declares the fatigue convention only. #8 §3.1.8.1 uses "stamina ≥ 0.20" gating without specifying the stamina↔fatigue mapping. #13 silently introduces the invariant `stamina = 1 − fatigue` — a redefinition of #8's surface, violating KD-1. If #13 needs to equate the two scales, it must file an ERR back-prop into #8 §3.1.8.1 to publish the relationship normatively, then cite it; not assert it locally.

**Fix locus:** §3.3 eligibility line 2 (rewrite to use Fatigue not Stamina, OR remove the redundant #13-added ceiling and rely on #8's gate alone); §3.7 closing paragraph (delete the "stamina is the complement of fatigue" sentence OR back-prop the relationship into #8 §3.1.8.1 via a new ERR-013-006 and cite it `[CROSS]`); T-U-051 directional assertion (currently asserts "agent at fatigue 0.85 is excluded" — pass under either reading, so it does not catch this inversion; add a directional test "fully-fatigued agent excluded; fully-rested agent eligible").

### AR-S1-H4 [H] §4.4.3 / §3.9 cite `PositioningAI.GetLine(EntityId)` and `PositioningAI.GetPhase(TeamId)` as Stage-0 surfaces, but #12 explicitly does NOT expose `GetLine` at Stage 0 (and `GetPhase` is not a published accessor at all)

§4.4.3 reads:
```
PositioningAI.GetFormationSlot(EntityId id)    // baseline slot
PositioningAI.GetPhase(TeamId team)            // local phase enum
PositioningAI.GetLine(EntityId id)             // Defense | Midfield | Attack
PositioningAI.IsSentinel(Vector2 slot)         // F6 detection
```

Verified against `positioning-ai/section-4.md` §4.4.3 (L131–145) — the explicit, normative text reads: "The `formationSlot` accessor described in §4.4.3 is the sole Stage 0 accessor of `PositioningAI`. Stage 1+ extensions for `LineMembership` and `LaneAssignment`... `LineMembership PositioningAI.GetLine(EntityId id); // Stage 1+`... **These accessors are NOT exposed at Stage 0 (CLAUDE.md \"Interface Design Principle\")**." `GetPhase(TeamId)` is not in the published #12 §4.4 accessor list at all (phase is computed per §3.0 but its only documented Stage-0 surface is internal — flowing into the slot computation, not exposed to consumers).

The FR-PR-019 backline-floor invariant (§3.9 invariant (2)) — `backlineCount = count(a where #12.line[a] == Defense ...)` — depends entirely on `GetLine`. The KD-11 phase-gate (FR-PR-033) depends on `GetPhase`. Both are foundational to the spec, yet both currently call into surfaces #12 either declares Stage-1-only or doesn't publish at all.

This is the **phantom-interface** trap (ERR-001 / ERR-004 / CLAUDE.md "Interface Design Principle"). Because #13 runtime is itself Stage 1 (KD-12 / §1.8), the calls would land *after* #12's Stage 1+ accessor surfaces are real — so the contract is *eventually* recoverable — but #13 must either: (a) declare these as Stage-1-only #12-side back-prop requests with explicit ERR IDs (mirroring the #14 / #15 deferral pattern KD-5 / KD-6 use), or (b) restate the input as "TacticalContext fields populated by the orchestrator using values #12 internally maintains", with a back-prop into #12 §4 to publish the accessor at Stage 1.

**Fix locus:** §4.4.3 (mark `GetPhase` / `GetLine` as Stage-1+ back-prop requests, allocate new `ERR-013-007` and `ERR-013-008`), §3.9 invariant (2), FR-PR-019 source citation, FR-PR-033 source citation, §8.1 rows for #12, §1.6 Boundary Matrix #12 row mechanism column. Add to OI list.

### AR-S1-H5 [H] §3.4 / FR-PR-023 contradict each other on cover-shadow target selection (cost vs. threat) and on disjointness with the primary press

FR-PR-023: "Cover-shadow target is the **highest-cost** candidate-receiver `EntityId` **not already pressed by the primary**."

§3.4 algorithm: "Assign cover shadows **greedily** in order of **descending threat-score** on `r`... For each `r` in descending threat order, up to `MAX_COVER_SHADOWS [GT]` slots: pick the lowest-`coverCost` eligible defender not already assigned."

Three contradictions:

1. **Cost vs. threat:** FR says "highest-cost candidate-receiver" (i.e., the receiver hardest to cover, by `coverCost`). §3.4 actually picks the highest-threat receiver via `threatScore(r)` — an entirely different scalar combining progression gain, openness, and skill. These are not synonyms and will frequently disagree.

2. **"Not already pressed by the primary":** the primary press targets the **ball-carrier**, not a candidate **receiver**. Candidate receivers are by §3.4's definition opponents *other than* the ball-carrier (and excluding the GK per KD-13). There is no scenario where a candidate receiver could be "pressed by the primary" — the FR clause is a category error.

3. **Tie-break drift:** FR-PR-025 declares "EntityId terminal tie-break"; §3.4 says "EntityId ascending as tie-break". These are consistent but §3.3's tie-break uses `SPACING_EPSILON_M2 = 1e-4 m²` (a metric squared tolerance). §3.4 has no such tolerance, so cover-shadow tie-break behaviour under near-equal float `coverCost` is undefined and would silently break determinism regression T-D-002 (EntityId-permuted input).

**Fix locus:** FR-PR-023 rewrite (e.g., "Cover-shadow targets are the top-`MAX_COVER_SHADOWS` candidate-receivers by descending `threatScore` (§3.4)"); strike the "not already pressed by the primary" clause; either define `threatScore` in §3.4 as a `[GT]` formula (it currently introduces `THREAT_PROGRESSION_W` / `THREAT_OPEN_W` / `THREAT_SKILL_W` constants — see AR-S1-H6 — but no input grounding in §2.3 for `receiverProgressionGain`); add an explicit `SPACING_EPSILON_M2` (or new `THREAT_EPSILON`) tie-break tolerance to §3.4 mirroring §3.3.

### AR-S1-H6 [H] Threat-score formula in §3.4 references `receiverProgressionGain` and `r.perceivedPressure` and `r.attribute.FirstTouch`, but mixes opponent-side and own-side perception fields without crossing #7's visibility filter

§3.4 threat-score formula:
```
threatScore(r) =
    receiverProgressionGain(r) * THREAT_PROGRESSION_W
  + (1 - r.perceivedPressure)  * THREAT_OPEN_W
  + (r.attribute.FirstTouch /20) * THREAT_SKILL_W
```

`r` is an opponent (a candidate receiver, from the defending #13's POV — see §3.1.4 prose). Two cross-spec problems:

1. **`r.perceivedPressure`:** Perception #7 §3.10 publishes `perceivedPressure` as the *observing* agent's perception of their *own* local pressure — i.e., a self-attribute. There is no published "opponent's perceivedPressure" — that would require #13 to read into the opponent's perceptual state, which #7 does not expose. The defending team can observe the *geometric* pressure on `r` (count of own teammates within radius), but that is a different scalar.

2. **`r.attribute.FirstTouch`:** Perception #7 §3.7–§3.10 publishes attribute lookups under perception's own visibility / familiarity gating. Reading an opponent's `FirstTouch` attribute as a clean value silently assumes perfect-knowledge attribute access — but #7's attribute model is supposed to gate this with scouting / familiarity error at Stage 1+. The `WEAK_RECEIVER` trigger (§3.1.4) makes the same assumption and is at least flagged via Q2-style perception-propagation note (§2.3); §3.4 reuses the same assumption silently and amplifies it (now used as a weighted score, not a binary threshold).

3. **`receiverProgressionGain(r)`:** undefined. Prose says "the forward component of `(r.pos − ballCarrier.pos)` along `attackingDirection`, clamped to `[0, 1]` after normalising by half the pitch length." Half-pitch = 52.5 m; the normalisation is ambiguous (clamp before or after divide; signed or absolute). Worked example absent. FR-PR-034 violation.

**Fix locus:** §3.4 — re-derive `r.perceivedPressure` as "geometric pressure on `r` computed by #13 from defender positions" (cite-not-redefine compliant); §3.4 explicit normalisation formula for `receiverProgressionGain` with worked example; Appendix B.4 add `WEAK_RECEIVER` attribute-access caveat. Or: file ERR back-prop into #7 §3.10 to publish "observable pressure on an opponent" as a defensive-side scalar.

---

## M findings (resolve in v0.2)

### AR-S1-M1 [M] FR-PR-031 zone-disengage clause cites `PRESS_ELIGIBLE_ZONE` "polygon" but §3.8(b) implements only a rectangular `X` range — and `PRESS_ZONE_X_MAX = 105.0 m` is the opponent's goal line, not the upper press boundary

§3.8(b) implements `if (ballX < PRESS_ZONE_X_MIN || ballX > PRESS_ZONE_X_MAX)` with `PRESS_ZONE_X_MIN = 35.0 m`, `PRESS_ZONE_X_MAX = 105.0 m`. This is a 1D X-range, not a polygon. FR-PR-031 calls it `PRESS_ELIGIBLE_ZONE` "polygon"; Appendix F glossary says "Rectangular X-range (Stage 0)". The terminology drift (polygon ↔ range ↔ rectangle) is minor but should be reconciled.

More substantively: `PRESS_ZONE_X_MAX = 105.0 m` is the **opponent's goal line** (pitch X ∈ [0, 105]). For a team attacking toward `+X`, ballX > 105.0 means the ball is **off** the pitch beyond the goal line — which can never occur within a live match because Ball Physics #1 would have flagged a goal or goal-kick. So the upper-bound clause is dead code. Either (a) the cap is intentional as a "trivially-true upper bound" — say so explicitly in §3.8 prose; or (b) the intended cap is something like "halfway line" (52.5 m) for a low-block style or "opponent's defensive third" for a mid-block — in which case the value is wrong.

Also: §3.8 says "high-press default eligible-zone" but `PRESS_ZONE_X_MIN = 35.0 m` for a team attacking `+X` means presses fire when the ball is past x = 35 m in own attacking direction — which is a **mid-block** geometry (ball already in middle third), not a high press (which would press in opponent's defensive third, x > 70).

**Fix locus:** §3.8(b) constants and prose; FR-PR-031 polygon→range terminology; Appendix F glossary; Appendix D `PRESS_ZONE_X_MIN` row should be added (currently absent from sensitivity table).

### AR-S1-M2 [M] Domain-tag back-prop is filed as `ERR-013-005` but `outline-detailed.md` KD-10 / §8.3 originally specified `ERR-013-001`

Outline KD-10: "Any stochastic micro-jitter... uses `DeterministicRngService` with domain tag `DOMAIN_TAG_PRESSING_AI` — value `[CROSS-PENDING]` until lead-developer ratifies the Phase B/C block per ERR-012-001 (proposed `0x19` in the #12 outline's KD-9 table; this spec inherits that proposal and **files ERR-013-001 as the back-prop request** if the block has not yet been allocated when section-file draft begins)."

Outline §8.3: "`ERR-013-001` — back-prop to #8 §3.1.8.2 (or §2.2.6 if mechanism chosen at section-file draft is a `TacticalContext` extension)..."

The same outline assigns `ERR-013-001` to two distinct things: the #8 back-prop AND the #16 domain-tag back-prop. Section files resolve this by reassigning the domain-tag back-prop to `ERR-013-005` (§1.3.3, §8.4, §6.1.1 source column) — sensible, but the resolution should be explicitly called out as a renumbering decision in the v0.2 fix pass (mirroring how #12 documented its `ERR-012-001` shift on May 16). Also: `ERR-013-005` is never filed in `docs/tracking/spec-error-log.md` — only the section-file claim exists. Same for `ERR-013-001`..`004`. Filing rows missing.

**Fix locus:** Record the renumbering explicitly in §1.3.3 / §8.4 (e.g., a footnote "Outline KD-10 originally allocated this back-prop as ERR-013-001; section-file draft moved it to ERR-013-005 to avoid collision with the #8 amendment back-prop"); add `ERR-013-001`..`005` rows to `docs/tracking/spec-error-log.md` per the precedent for ERR-010-001, ERR-012-001, ERR-017-001.

### AR-S1-M3 [M] No RNG draw site is declared — yet §6.1.1 reserves `DOMAIN_TAG_PRESSING_AI` and §4.6 says "Stage 0 §3 currently has no stochastic step"

§4.6 RNG row: "`DOMAIN_TAG_PRESSING_AI = 0x19` `[CROSS-PENDING]` (`ERR-013-005`; inherits ERR-012-001 block proposal). **Stage 0 §3 currently has no stochastic step** — the tag is declared so Stage 1+ extensions inherit without re-litigation."

But: §3.3 / §3.4 explicitly invoke "EntityId ascending as terminal tie-break" — which is a **deterministic** tie-break, not a stochastic one. So there is no current draw site, consistent with §4.6's claim. **However:** the Decision Tree #8 §3.1.8 PRESS utility (which #13 advises) does use deterministic scoring with no RNG, and the outline KD-10 anticipates "stochastic micro-jitter (e.g., tie-breaking when two cover-shadow candidates have equal cost) uses `DeterministicRngService`". Section files have silently deleted this micro-jitter design (replaced it with EntityId tie-break) without recording the design change.

If RNG is truly not used, the `DOMAIN_TAG_PRESSING_AI` `[CROSS-PENDING]` reservation is **speculative future-proofing**, which is exactly what CLAUDE.md "Interface Design Principle" forbids ("Write interfaces only when both sides are specified"). Either: (a) drop the domain-tag reservation entirely until a Stage-1+ stochastic step is added — close ERR-013-005 as "premature"; or (b) document why the reservation is now-needed despite no current draw site (e.g., to lock the slot before another Phase-C spec claims it). The #16 §4.5 single-purpose-per-site rule means a slot reserved with no site doesn't violate anything, but the reservation has no consumer to align against.

**Fix locus:** §4.6 RNG row prose (explain why a no-current-draw-site tag is reserved, citing the block-collision-avoidance motive); OR remove the row and `ERR-013-005`.

### AR-S1-M4 [M] §3.1.4 prose contradicts itself on which team `candidates = perception.visibleOpponents` represents

§3.1.4 inline prose mid-formula: "For each candidate receiver `r` in the carrier's pass range (visible per #7, **opponent of the ball-carrier's team** — wait, candidate receivers are **teammates** of the ball-carrier; #13 is the *defending* team scanning the *attacker's* options)". The "— wait..." correction-in-text remains literally in the section file (line 92–94).

Then the code block: `candidates = perception.visibleOpponents             // from #13's POV`. From #13's POV (#13 is the defending team), `visibleOpponents` = opponents of #13 = teammates of the ball-carrier ✓. Conceptually correct, but the inline correction is unedited reviewer-thought, not finished prose. Reads as "the author noticed the error mid-write and never went back".

**Fix locus:** §3.1.4 — delete the "— wait... attacker's options" mid-sentence aside; replace with a clean statement: "candidate receivers are teammates of the ball-carrier, scanned from the defending team's POV; `perception.visibleOpponents` from #13's POV resolves to that set."

### AR-S1-M5 [M] FR-PR-040 `PressAssignment` typo: `PRessAssignment` (capitalisation)

FR-PR-040 row text: "**F6 — #12 baseline slot unavailable** (e.g., #12 emits `SENTINEL_NO_SLOT` for the agent): no `PRessAssignment` override is emitted..."

Mid-word capital `R`. Other section files use `PressAssignment` consistently. Trivial fix.

**Fix locus:** §2.1 FR-PR-040.

### AR-S1-M6 [M] §3.9 demotion priority order leaves the iteration-bound undefined under three simultaneous violations; comment says "≤ 3 iterations" but no proof and the algorithm can starve

§3.9 prose: "After at most `MAX_COVER_SHADOWS + 1 = 3` iterations the set is clean (because each demotion strictly reduces the violating count)."

This claim is true for invariants (1) and (3) (each rejects/demotes a single COVER_SHADOW per pass), but NOT for invariant (2) "Backline floor" — the rule there is a **promotion block**, not a demotion. There is no "demote the offending agent to HOLD_SHAPE" path for the backline-floor invariant; you cannot demote a Defense-line agent from PRIMARY_PRESS back to HOLD_SHAPE without re-running primary-press selection (which §3.9 does not do). If the backline floor is breached after §3.3 commits a primary press, §3.9 has no recovery move except F5 (full fallback) — but the prose suggests demotion is the path. Inconsistent.

Also: §3.9 says "If a primary-press demotion is required to satisfy an invariant, the entire directive falls back to all-`HOLD_SHAPE` per F5 / FR-PR-039." That's consistent with invariant (2), but the "≤ 3 iterations" bound only counts cover-shadow demotions — if F5 fires immediately on a backline-floor breach, the iteration count is 1, not 3. The §6.2 hot-path table row "`EnforceInvariants` | ≤ 3 iterations" is therefore correct as an upper bound but does not flag that backline-floor violations terminate in 1.

**Fix locus:** §3.9 — add explicit handling for invariant (2): "if violation, primary-press is demoted and §3.3 re-runs excluding the violating agent; if no eligible primary remains, F5 fires"; OR explicitly state "invariant (2) triggers F5 immediately on violation (no re-run)"; update §6.2 hot-path complexity claim; add T-C-007 covering the backline-breach + F5 path distinct from the cascading-cover-shadow path covered by T-C-004 / T-C-005.

### AR-S1-M7 [M] §6.6 profiling-plan row "Hot-path channel registry... Stage 1+ automated; **Stage 1 first-commit deliverable**" with gate "FR-PO-070 / FR-PR-006" — but FR-PR-006 is a *spec-internal* zero-alloc requirement, not a #18-defined gate

§6.6 row: "Hot-path channel registry | #18 trace pipeline (Appendix F.0 schema) | Stage 1+ automated; Stage 1 first-commit deliverable | FR-PO-070 / FR-PR-006".

`FR-PO-070` is a Performance Optimization #18 functional requirement (verified — Stage 0 manual / Stage 0+1 automated split per #18 v0.2 fix pass). `FR-PR-006` is #13's own "No heap allocation on the per-tick hot path." These are two different gates serving two different audit hooks. Pairing them in a single gate column conflates the channel-registry conformance audit (FR-PO-070) with the per-tick allocation audit (FR-PR-006).

Also: the §6.6 row uses "Stage 1+ automated; Stage 1 first-commit deliverable" — same ambiguity that #12 AR-S1-16 flagged for the `SHAPE_TRANSITION` channel ("Stage 0+1" vs Stage 1+ inside a §7 Stage 1+ section). The phrase "Stage 1+ automated; Stage 1 first-commit deliverable" parses as "automation lands Stage 1+, but the channel row itself ships at the Stage 1 first commit" — which is right but worded badly.

**Fix locus:** §6.6 split into two rows (one for channel registry / FR-PO-070; one for hot-path allocation tracker / FR-PR-006); clarify the Stage 1 vs Stage 1+ split.

---

## L findings (follow-up)

### AR-S1-L1 [L] §3.1.5 "One-Tick Latency by Design" claim "Perception filtering already enforces this for opponent-side events; #13 inherits the latency without adding its own" — but the citation back to Perception #7 is missing

§3.1.5 asserts a one-tick latency invariant inherited from #7's perception filtering. No #7 section anchor cited. The outline KD-7 closing prose makes the same claim without citation. Either inline a `#7 §X.Y` anchor here or convert to a §1.3 dependency row note. Not load-bearing for any FR.

**Fix locus:** §3.1.5; if perception's snapshot semantics are at `#7 §3.7` or §3.10, cite the specific subsection.

### AR-S1-L2 [L] Appendix B "Reference Cards" duplicate §3.1.1–§3.1.4 trigger surfaces verbatim — drift risk

Appendix B repeats trigger threshold, debounce, worked example, and test for each of the four triggers. The same content is in §3.1.1–§3.1.4. If a constant is tuned in §6.1 (e.g., `BAD_TOUCH_THRESHOLD` flipped 0.40 → 0.35), three places need to update (§3.1.x prose, Appendix B card, §6.1 row). Other approved specs use Appendix-as-summary-card too (Heading #10 Appendix B has the same pattern) — not a unique sin — but a v0.2 cross-check should add a comment "values mirrored from §3.1 / §6.1; single source of truth is `PressingAIConstants.cs` at Stage 1+".

**Fix locus:** Appendix B preamble.

### AR-S1-L3 [L] §6.3 reference-host pin cites "Unity Editor playmode profiler (not a Player build — matches #12 AR-S1-20 disambiguation)" — but #12 AR-S1-20 is itself a v0.1 finding now resolved in v0.2

Citing the *finding ID* (AR-S1-20) of an upstream review as a normative source for one's own perf-host description is brittle — if #12's v0.2 fix pass renumbered or restated the disambiguation, the link breaks. Should cite the resolved #12 §6.3 text directly, not the finding ID.

**Fix locus:** §6.3 caveat paragraph; cite `#12 §6.3` (the resolved location), not "#12 AR-S1-20".

### AR-S1-L4 [L] §5 / §3 test ID prefixes are not from the Testing Strategy #19 taxonomy

#13 uses `T-U-NNN`, `T-I-NNN`, `T-D-NNN`, `T-P-NNN`, `T-C-NNN`, `T-X-NNN`. #19 §3 publishes the canonical test-ID taxonomy. The #13 IDs may or may not match — section-file draft did not grep #19. The §9.3 (e) precondition acknowledges grep gaps for #5 but not for #19. Two of the prefixes (`T-C-` "anti-chaos", `T-X-` "exploit") are spec-local inventions that #19 probably doesn't enumerate.

Not blocking — but the v0.2 fix pass should grep #19 §3 / §4 to confirm prefix conformance, and either bind these to the closest canonical category or file a one-line back-prop into #19 §3 reserving the new prefixes.

**Fix locus:** §5 preamble; §8.5 grep list; §9.3 (e) extended.

---

## Cross-cutting observations (not scored)

- **Math review depth.** I re-executed §3.1.2 (BACKWARD_PASS dot product `-0.894` ✓), §3.1.3 (SIDELINE_TRAP facing dot `0.87` ✓), §3.3 (cost(A)=9.41, cost(B)=5.21 ✓), §3.5 (shadow lane `(68.25, 35.5)` ✓), Appendix C.1 (`50, 31` ✓), C.2 ratio `9.915 : 8.112 ≈ 55 : 45` ✓, C.3 (`56.5, 30` ✓), §3.8 disengage timeline (T+12 disengage / T+24 cooldown clear ✓ — actually T+25 per inclusive count). All worked-example arithmetic in §3 and Appendix C is correct as written.

- **Outline-vs-section drift counts.** Outline declares 44 FRs (§2.1 provisional table) — section file §2.1 publishes 44 FRs ✓. Outline declares 17 KDs — §1.5 maps all 17 ✓. Outline declares 4 triggers, 3 roles, 3 anti-chaos invariants, 4-exploit corpus — §3 / §5 match. The drift is in *content fidelity* (AR-S1-H1 / H4 / H5 / H6) rather than enumeration count.

- **Phantom-interface watch.** No #14 / #15 type or accessor surfaces are produced (KD-5 / KD-6 discipline holds). The phantom-interface findings (AR-S1-H4) are against #12, not #14 / #15.

- **Channel-registry rows.** §6.6 / §7.5 correctly defer the `PRESS_TRIGGERED` / `PRESS_DISENGAGED` channels to Stage 1 first commit using the #18 Appendix F.0 13-field schema (verified at `performance-optimization/appendices.md` L231–253). Conformance pattern matches Heading #10 / Goalkeeper #11.

- **EntityId no-reuse, tick-rate split, corner-origin, parameter-based physics.** All four CLAUDE.md invariants are correctly cited (§1.7 / §8.2) without redefinition.

- **Fatigue convention.** Cited correctly in §1.7 / FR-PR-008 — but inverted in the §3.3 implementation prose (AR-S1-H3). The convention discipline is uneven: declared correctly at the spec-level, then violated at the algorithm-level. Recommend the v0.2 fix pass add a §3.0 preamble paragraph "All fatigue references in §3.x use the CLAUDE.md convention `0 = rested, 1 = fatigued`. The Decision Tree #8 §3.1.8.1 'stamina' is a separate surface; #13 does NOT redefine stamina as a function of fatigue."

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 17, 2026 | AI agent (claude/adversarial-ai-spec-review-zkYuQ) | PASS-1 adversarial review of `section-1.md` … `section-9-approval-checklist.md` + `appendices.md` v0.1. 17 findings (6 H / 7 M / 4 L). Gate: `SPEC_INDEX.md` row 13 remains `NOT STARTED` pending v0.2 fix pass. |
