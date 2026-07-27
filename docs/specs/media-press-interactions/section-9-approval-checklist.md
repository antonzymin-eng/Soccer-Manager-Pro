# Media & Press Interactions #35 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / dependencies + tiered DAG / **§1.4's three verification findings** /
      KD-1..KD-10 / determinism posture.
- [x] §2 FR-ME-001..038, data structures, failure modes F1..F9 and the explicit *"an empty drain is not a
      failure mode"* note.
- [x] §3 FM-ME-01..06 with the queue seam and its managed-club gate, the answer command's refuse/throw
      split **and its check ordering**, expiry with the argued F6 guard, the consequence/drain pair, the
      keyed selection value, the deferred deep draw, and seventeen hand-verifiable worked examples
      **including the away mirror**.
- [x] §4 tiered assembly + reference direction with a CS0104 pre-check, file layout, the
      `MediaTextBoundary` sibling adapter, **all three** #30 touch points, the #46 read seam, save
      composition, neighbour contracts.
- [x] §5 test plan across identity / units / the refuse-throw split / determinism / delivery + save /
      **localization compliance** / fail-loud / structural + the T-phase closed-loop scenario.
- [x] §6 loop classification (world tick + post-round, no hot path), the two scaling terms, cost profile,
      `[GT]` budget ceilings.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-7.
- [x] §8 XC-035-001..022 + the §8.0 prerequisite + the back-prop table + the not-a-back-prop list.
- [x] Appendices A (constants), B (save layout), C (the `MediaIntent` roster + ordinal-band table).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them).
- [x] `[CROSS]` rows name their authority and are consumed read-only — #35 re-declares none of #49's or
      #33's types (T-ME-BOUND-004).
- [x] `[CROSS-PENDING]`: `_RESERVED_0x27_` / `SubsystemOrdinals.Media = 89` pending the **deep-tier**
      promotion (§8.3) — the ERR-040-001 / ERR-030-001 spec-text-first precedent.
- [x] `MEDIA_INTENT_OPTION_BAND_START` is `[FIXED]` and **asserted by test** (T-ME-LOC-002), because it is
      a save-correctness boundary rather than a tunable.
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts only
      shape, identity and direction — never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] #49 FR-LC-002 bars a baked localized string; FR-LC-004 defines
      `Render(in LocalizedTextRequest)`; FR-LC-007 is `draw % variantCount` — **total at `draw = 0`**, the
      fact FR-ME-018's fallback rests on. *(`localization-accessibility/section-2.md`)*
- [x] #49 **FR-LC-012** makes a sim assembly referencing `TacticalDirector.Localization` a **build error**
      — the fact §4.2's adapter placement rests on.
- [x] #49 FR-LC-013/014 + §2.2: producers bind by a **sibling boundary adapter** and carry **disjoint**
      slots — *"#35/#46 carry disjoint slots"* verbatim.
- [x] #49 **§7.3 names `MediaTextBoundary` in advance** — so #35 fits an existing extension point rather
      than extending the core, which is what makes §8.4's "no #49 structural change" claim true.
- [x] #49 **FR-LC-020 binds `SelectionDraw` to `DrawReserved` / the `world.text` reservation** — a MUST on
      the generic seam naming one producer's stream, contradicting §7.3's *"if they draw"*, FR-LC-013/014,
      and FR-LC-005 in the same spec. **The defect ERR-049-001 fixes**, and the premise KD-2 rests on.
- [x] #33 **FR-HS-002**: #33 owns `MoraleState` + `PersonalityProfile`; *"no other assembly writes them."*
- [x] #33 **FR-HS-024**: *"#46 is the only consumer that writes #33 morale … all are deferred."* — the
      fact that makes the plan's morale-write direction **unsatisfiable** and forces KD-3.
- [x] #33 §2.2 `HumanSystemsDayInput` is a **transient input struct**, not serialized state — the fact
      that makes ERR-033-003 carry **no** `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump.
      *(`personalities-morale-dynamics/section-2.md`)*
- [x] #33 §3.1 `ComputeMoraleTarget(equilibrium, committedInputs, personality)` consumes
      `BoardObjectiveDeltaPermille` as an additive term — the shape #35's delta copies.
- [x] #33's F6 guard is `worldDay == LastAdvancedWorldDay ⇒ **no-op**` — the fact that **bars** same-day
      delivery (a re-run would silently drop the delta, not apply it), making the one-day contract the
      only shape #33 permits rather than a preference.
- [x] #33 FR-HS-008 pins the unadvanced sentinel at `uint.MaxValue`, **not** `0`.
- [x] #30 §3.4 `AdvanceAndPlayNextRound` ends each fixture with `Table.ApplyResult` then
      `EmitMatchOutcome(result)`, *"producer-only … one per fixture"* — the queue hook.
      *(`season-competition-loop/section-3.md`)*
- [x] #30 §3.3 step 3 is where the per-player `HumanSystemsDayInput` is assembled — the **drain** point,
      and the seam that would otherwise be missed.
- [x] **#30's pinned tick order is currently malformed**: two seams claim step 7 (#42 academy, #32
      scouting — the latter after the live-tick line with a duplicate `# 8. world day`); FR-SN-034's
      enumeration omits #32; and `ERR-030-007` / `ERR-030-009` are each used twice for different changes.
      The §8.0 prerequisite. *(`season-competition-loop/section-2.md`, `section-3.md`)*
- [x] **No reputation state exists anywhere in the approved set** — a tree-wide search returns three hits,
      none of them state, one of which (`youth-academy-intake/section-7.md`) explicitly **disowns** one.
      The fact KD-4 rests on.
- [x] #16 §3.4 already carries *"**Reserved — held for Media & Press Interactions #35 per roadmap §6
      (`SubsystemOrdinals` 89); MUST NOT be reused.**"* — so **no #16 back-prop at approval**.
      *(`deterministic-sim/section-3.md` §3.4 v1.0.13)*
- [x] `SplitMix64` is **not** a shared public primitive in `deterministic-sim` — it lives inside
      `DeterministicRngService.cs`, which is why `FixtureScheduler` and `LeagueBootstrap` each carry a
      local copy. The fact that lets #35's minimal tier reference **nothing**.
      *(`src/deterministic-sim/`, `src/season-save/`)*
- [x] `RegisterStream` appends into a bounded, never-shrinking table; `MaxRngStreams = 64`, no unregister
      — the bound FR-ME-019 avoids contributing to. *(`src/deterministic-sim/DeterministicRngService.cs`,
      #42 §7.4 R-1)*
- [x] **`ERR-030-012`, `-013` and `-014` are all already FILED** — by #30's own T2 implementation on July
      26, the same day this spec's supplement was written. The supplement proposed `-012` and `-013`;
      both collide. Reassigned to **`-022`** and **`-023`** (§9.4.1 M-1). *(`docs/tracking/spec-error-log.md`)*
- [x] `FR-ME-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in `docs/specs/`.
- [x] `ERR-049-001` and `ERR-033-002` / `-003` are free; `ERR-033-001` is deliberately **retired unused**
      in favour of `-003` (§8.2).

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G0** — the §8.0 **prerequisite** (ERR-030-022, #30's tick-order reconciliation) lands, or its numbering is confirmed. | #30 owner / drafter | ✅ **CLOSED** — landed July 27, 2026 with the flip. #30 §3.3.1 records the reconciliation; #32 scouting → step 9, **#35 media expiry → step 10**, `AdvanceDay` → 12. #35 now cites a defensible number |
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-049-001**, **ERR-033-003** (jointly with #46), **ERR-033-002**, **ERR-030-023** atomically with the status flip. | drafter | ✅ **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.47) |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ✅ **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ✅ **CLOSED** — row + Registry-Changes entry landed July 27, 2026 |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the `0x27` promotion
(deep tier, first draw — FR-LW-031 forbids registering it earlier); the outer
`SEASON_SAVE_FORMAT_VERSION` bump (T2); the #45 board-facing signal and the #33 morale read (both deep);
and the T3 `[GT]` balance pass (§A.4).

**G0 is unusual and is stated as a gate rather than a risk on purpose.** It is a defect in **another
spec's** approved text that #35 did not cause and cannot fix unilaterally. Treating it as a #35 risk would
let #35 approve while citing a step number that does not unambiguously exist.

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 8M + 9L, all resolved in the v0.2 fix pass.** The M findings cluster around the two things
this spec is: a **routing** spec (where an unstated seam is invisible) and a **producer** for two other
specs' contracts (where a second producer breaks assumptions written for the first).

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **`ERR-030-012` and `-013` are both already filed.** The supplement proposed them for the tick-order prerequisite and the two media seams; both were taken by **#30's own T2 implementation on the same day the supplement was written** (`-012` the §4.5 cursor-stream contradiction, `-013` the §4.6 unimplementable outcome record, `-014` ERR-030-014 itself). Nothing cross-checks a proposed id against the log, so this would have landed silently and produced two different changes under one id — the exact defect §8.0 exists to record in #30's own history. | Verified against `spec-error-log.md` and reassigned to **`ERR-030-022`** / **`-023`**. Recorded in §9.3 so the verification is re-runnable. `outline.md` / `section-8.md` v0.2. |
| M-2 | M | **The question/option ordinal band had a `[FIXED]` boundary constant but no failure mode.** A catalogue row placing a question intent in the option band (or vice versa) was undetectable — and because the band is the mechanism that lets one enum carry two rosters, a violation silently mis-renders a conference rather than failing. | New **FR-ME-010 / F5**, checked at **both** the authoring boundary and decode; locked by T-ME-LOC-003 and T-ME-FAIL-002. |
| M-3 | M | **§3.2's check ordering was unstated**, and the natural implementation — validate arguments first — is **wrong**: a client holding a stale render may pass an `optionIndex` valid for the previous state, which would throw on what F3 classifies as a legal race. | Ordering pinned (resolution check **before** range check), with the reason; locked by **T-ME-U-015** so a later tidy-up fails a test. |
| M-4 | M | **`SelectionValue` mixed `MEDIA_NO_SUBJECT` as `0xFFFFFFFF`**, making the subject-less case the mix's most *extreme* input and colliding with a maximal `subjectId`. Subject-less questions are the common case at the minimal tier (every result question), so this is not a corner. | The **`subjectId + 1` shift** (§3.5), mapping `-1 → 0`; locked by T-ME-DET-004. |
| M-5 | M | **§3.3's F6 day-gap guard was adopted "verbatim from #33" with no argument** — and #53, written the same week, argues the *opposite* posture (no cursor, gaps legal). Copying either without argument means one of them is wrong. | §3.3 now argues #35's case specifically: a gap would expire conferences **on the wrong day**, stamping consequences with a `RecordedWorldDay` that never happened. Load-bearing here, ceremony there. |
| M-6 | M | **The root's post-sum clamp was unstated.** With #46 as a second producer of the same `ExternalDeltaPermille`, two producers each at their own bound exceed the field's contract — a failure a per-producer field made impossible and the shared field makes possible. The cost of ERR-033-003 was imported without naming it. | §4.4 states where the sum and clamp happen; locked by **T-ME-I-010**; recorded as risk **R-7**. |
| M-7 | M | **`HasCitedEpisode` was "false at every tier" in prose but present in `MediaSlots`.** A present-but-false field is what a later maintainer flips — and flipping it silently re-introduces the `living-world` dependency KD-1 exists to remove. | New **FR-ME-015**; the field is **absent by decision** from `MediaSlots`, with the absence documented in the struct itself. |
| M-8 | M | **The catalogue-coverage test would have been written for questions only.** KD-1's first paragraph reads as one roster; the two-roster requirement is three paragraphs later. A coverage test missing the option half is exactly what pushes an implementer to bake option labels — the one thing #49's coverage-lock exists to prevent. | **T-ME-LOC-001** states *"questions **and** answer options"* with the reason attached, and **FR-ME-012** makes it a requirement rather than a test convention. |
| L-1 | L | KD-10 (the `MediaIntent` ordinal contract) lived inside KD-1, where a reviewer scanning the KD list would miss it — despite being a **save-correctness** property with no version gate behind it. | Promoted to its own key decision. |
| L-2 | L | §1's KD-3 still read `MediaDeltaPermille`; v0.7 recorded the producer-agnostic rename only in the back-prop table. | Carried into §1 inline. |
| L-3 | L | The identity precondition ("no conference **answered**" vs "every consequence `0`") had been corrected three times across the supplement's own AR cycle and re-appeared each time. Prose alone is demonstrably insufficient. | **T-ME-ID-002** asserts the trap as its own test, so a suite built to the weaker precondition **fails**. |
| L-4 | L | No test forbade a decoded **zero** `PendingDelta` row, which FR-ME-033 makes impossible to produce — so accepting one silently breaks the presence invariant. | **T-ME-FAIL-007**. |
| L-5 | L | `MediaTriggerInput`, `MediaSlots` and `MediaCursors` were described in prose only. | Written out in §2.2. |
| L-6 | L | The managed-club gate's *position* (step 1) was not justified, though it is a real performance property: #30 calls the seam ~190× per round. | §3.1 and §6.1 both state it. |
| L-7 | L | Every worked example used the **home** team — the ERR-008-002 defect class, in a spec whose archetype selection is home/away-relative. | Away-mirror examples (b)/(c) added; **T-ME-U-002** locks them. |
| L-8 | L | `MediaTextBoundary`'s placement outside `src/media/` was presented as tidiness. | Restated: under FR-LC-012 it would **not compile**. |
| L-9 | L | The adapter's intent check read as *the* gate, so a reader could implement the FR-LC-015 gate only in the boundary layer — where any other `MediaIntent` consumer bypasses it. | §4.3 marks it **defence in depth**; **T-ME-LOC-007** asserts the gate holds through any #35 surface. |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §7.1 did not state that T2's behavioural neutrality is a property of the
**authored catalogue** rather than of the code — unlike #53 and #45, where it is a code property — so
"identity-preserving T2" would have been read as a stronger guarantee than #35 offers; now stated, and
recorded as risk **R-1**. **L-2:** §6.4's bounded-footprint claim did not name **which three MUSTs** keep
it bounded (clear-on-delivery, drop-on-departure, never-write-zero), any one of whose removal makes the
APPEND-only blob grow without bound across a career. **L-3:** §8.4 did not carry a #45 row, so "#45 needs
nothing at approval" was inferable but unstated in the one table a reviewer checks for omissions.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; no model #35 does not own is duplicated, and the #49 / #33 re-basing is explicit rather than implied. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example — **including an away-team mirror** — and no fabricated verification values (in particular, no test pins a `SelectionValue` output). | ⏳ pending |
| R-03 | Determinism posture is complete: the draw-free minimal tier, the position-independent keyed mix, the absence of any persisted cursor, and the single-registration deep-tier model are each justified rather than asserted. | ⏳ pending |
| R-04 | Persistence is version-gated, opaque, fail-loud, APPEND-only, and bumps no format version it does not own; the undelivered-delta invariant and the three bounding MUSTs are stated. | ⏳ pending |
| R-05 | Cross-spec back-props are enumerated with owners and timing, **every proposed ERR id is verified free against the log**, the #8.0 prerequisite is distinguished from #35's own changes, and the joint #46 filing is recorded on both sides. | ⏳ pending |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-049-001**, **ERR-033-003** (jointly with #46), **ERR-033-002**, **ERR-030-022**, **ERR-030-023** (`spec-error-log.md` v1.47). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #35 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** — G1 closed (PASS-1 0H+8M+9L → AR-2 0H+0M+3L convergence, §9.4.1). G0 and G2–G4 remain open:
G0 is another spec's defect that #35 cannot fix unilaterally, back-props land atomically with the status
flip, sign-off is a human authority, and the registry row is added at promotion.

**What verification did to this spec, restated at the decision point.** #35 arrived with a plan whose KD-1
made it a consumer of #22's text generator with a morale-write direction into #33. Checking both against
source found that **#49 had superseded the first** (and names #35's adapter in advance) and that **an
approved MUST forbids the second**. The result is a spec that is *smaller* than planned — it loses a
`living-world` dependency and gains a boundary adapter — and whose minimal tier references **nothing at
all**.

**#35 also surfaced two defects it did not cause**, and both are the same shape — *a contract written
correctly for one producer, breaking the moment a second arrives*: #49's FR-LC-020 binds a generic seam to
#22's specific stream (ERR-049-001), and #33's per-producer morale field does not survive #46
(ERR-033-003, filed jointly). Neither is a #35 constraint; both outlive #35 and would have been hit by the
next producer regardless.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, the five gates incl. the unusual G0 prerequisite gate, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+8M+9L, all resolved) and the AR-2 convergence sweep (0H+0M+3L). §9.1 completeness updated for FR-ME-010/015 and KD-10; §9.2 gained the band-boundary line; §9.3 gained the verified **`ERR-030-012`/`-013`-are-taken** row (the PASS-1 M-1 correction), the `FR-ME` prefix check, and the `SplitMix64`-is-not-shared row that the "minimal tier references nothing" claim rests on. G0 and G2–G4 remain open. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-049-001**, **ERR-033-003** (jointly with #46), **ERR-033-002**, **ERR-030-022**, **ERR-030-023** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.47). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
