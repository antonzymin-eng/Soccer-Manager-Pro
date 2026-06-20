# Tactical Instructions #21 — PASS-1 Adversarial Review (Section Files v0.1)

**Created:** June 20, 2026
**Reviewer:** — (adversarial pass)
**Target:** `docs/specs/tactical-instructions/` section files v0.1
**Result:** 2 H · 4 M · 4 L (10 findings). All resolved in the v0.2 fix pass (same commit).

Method: each load-bearing seam claim was fact-checked against the actual source files it cites
(`UtilityScorer`, `ActionSelector`, `ContextModifierInputs`, `TacticalContext`, `DefensiveSnapshot`,
`StyleProfile`), per the project's recurring failure mode (specs asserting hooks that do not exist).

---

## High

**H-1 — `Tempo` targets a non-existent "decision threshold" (phantom hook).**
§3.4 / FR-TI-015 say `Tempo` "adjusts decision/pass utility thresholds." Verified against
`ActionSelector.cs`: selection is **pure max EffectiveUtility** after composure noise — there is no
threshold to raise or lower. This is the same phantom-hook class as `FocusPlay` (which v0.1 correctly
flagged as new logic) but `Tempo` was mislabelled as resolving into an existing tunable.
**Fix:** reclassify `Tempo` as a **new branch** (§2.3 exception, KD-11) that biases concrete utility
terms (e.g. forward-PASS/SHOOT vs HOLD weighting, and option-generation breadth), not a "threshold";
update §3.4, FR-TI-015, §3.3, and the KD-11 list. *(Resolved v0.2.)*

**H-2 — `StyleProfile` is double-driven by `Mentality` and `TransitionWon/Lost` with no precedence.**
§3.2 maps `Mentality` → a `StyleProfile`; verified that `StyleProfile` carries `TransitionHoldTicks`
(among `DepthMult`/`TimingMult`/`SupportMult`/`MaxRunners`). FR-TI-020 separately routes
`TransitionWon/Lost` → `StyleProfile.TransitionHoldTicks`. Two inputs drive one output; the spec never
says which wins. **Fix:** define composition — `Mentality` selects the **base** `StyleProfile`;
`TransitionWon/Lost` override **only** the transition dimension (`TransitionHoldTicks` + the #13
counter-press gate). State it in §3.2/§3.4 and FR-TI-020. *(Resolved v0.2.)*

## Medium

**M-1 — `Width`/`DefWidth` mislabelled "existing tunable."**
§3.4 column "Target tunable (existing)" routes `TacticWidth` → "#12 `ContextModifierInputs` lateral
scalar." Verified: `ContextModifierInputs` has exactly `ScoreDiff`/`TeamMeanFatigue`/`TacticalIntensity`
— **no width/lateral field**. The compactness *scaling* in `ContextModifier` exists, but Width needs a
**new field** (consistent with §4.4 "new width/role/duty fields," but §3.4/§1 overstate "no new
branch"). **Fix:** relabel as "new field feeding the existing compactness scaling." *(Resolved v0.2.)*

**M-2 — `TeamTactic.DefensiveLine` is a parallel surface (the project's recurring bug class).**
Verified two existing declarations: `TacticalContext.DefensiveLineDepth` (#8) and
`DefensiveSnapshot.DefensiveLineDepth` (#14, sourced from #12 per FR-DA-012). `TeamTactic.DefensiveLine
float[0,1]` (v0.1) is a **third**. **Fix:** state that `DefensiveLine` is the manager-set **input** the
assembly layer writes into the single authoritative `DefensiveLineDepth` (+ `Mentality` line bias),
not a parallel value; #12 remains the depth authority. *(Resolved v0.2.)*

**M-3 — `[Flags]` enums break the uniform ordinal-stability assertion.**
FR-TI-007 + §5.2 (T-TI-U-001..016) assert `(int)Member == N` for all 16 enums, but `TacticTriggerMask`
and `SetPieceDutyFlags` are `[Flags]` (bit positions 1,2,4,8; `byte` caps at 8 flags). The sequential
assertion is wrong for them. **Fix:** carve out flags enums — assert **bit-position** stability + the
8-flag `byte` ceiling; reword FR-TI-007. *(Resolved v0.2.)*

**M-4 — "Every FR traces to a test" overstated; §5.7 has a wrong row.**
§9.1 item 2 and §5 claim every FR traces to ≥1 test, but FR-TI-002/003/029/030 are verified by
asmdef-reference grep / inspection, not executable tests; §5.7 maps FR-002/003 to `EXP-004` (the
schema-order lock — unrelated). **Fix:** add a "verified by inspection/grep" verification class; correct
the FR-002/003 row to the §4.7 grep gate; soften item 2 to "every FR traces to a test **or a named
verification**." *(Resolved v0.2.)*

## Low

**L-1 — Priority 6 is undefined.** SPEC_INDEX priorities are 1–5. `#21` was assigned Priority 6 without
defining it. **Fix:** note "Priority 6 = Stage-1 forward (post Stage-0 set)" in the SPEC_INDEX entry.
*(Resolved v0.2.)*

**L-2 — `RISK_MULT_BALANCED`/`LINE_BIAS_BALANCED` mis-tagged `[DERIVED]`.** `[DERIVED]` requires a
documented formula (FR-CS-021); these are just the identity row. **Fix:** document the formula
(`= MentalityRiskMult[Balanced]` / `MentalityLineBias[Balanced]`) so the tag is valid. *(Resolved v0.2.)*

**L-3 — `outline.md` KD summary truncates at KD-8 while §1.5 has KD-1..KD-12.** **Fix:** outline points
to §1.5 for the full list. *(Resolved v0.2.)*

**L-4 — `TacticFormation(≥3)` vague.** It must currently mirror **exactly** the 3 #12 `FormationFamily`
members for a 1:1 map (else F5-clamp). **Fix:** tighten wording in §2.2.4 / §3.1. *(Resolved v0.2.)*

---

## Disposition

All 10 resolved in the v0.2 fix pass (section files bumped to v0.2). The two H findings were both
phantom-hook / double-drive defects caught only by reading the cited source — exactly the class the
project's "Interface Design Principle" guards against. §9.2 gate **G1 (PASS-1 + fix pass)** is now
satisfied; **G2 (balance pass)** and **G3 (lead-developer sign-off)** remain open.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-06-20 | — | PASS-1: 2H+4M+4L filed and resolved in the v0.2 fix pass. |
#endregion
