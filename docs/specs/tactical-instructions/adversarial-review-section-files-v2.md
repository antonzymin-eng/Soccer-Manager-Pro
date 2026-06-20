# Tactical Instructions #21 — PASS-2 Adversarial Review (Section Files v0.2)

**Created:** June 20, 2026
**Reviewer:** — (adversarial pass, fresh-eyes sweep over the v0.2 surface)
**Target:** `docs/specs/tactical-instructions/` section files v0.2 (post PASS-1)
**Result:** 1 H · 4 M · 1 L (6 findings). All resolved in the v0.3 fix pass (same commit).

Method: re-checked the v0.2 fixes for contradictions they introduced, and fact-checked the determinism
and perf claims against `src/match-engine/MatchEngine.cs`.

---

## High

**H-1 — FR-TI-031 / DET-002 (digest identity) contradict FR-TI-028 (tactics are digest-load-bearing).**
Verified `MatchEngine.cs` serializes ball + agent state but **no** tactics today. FR-TI-028 makes
`TeamTactic`/`PlayerTactic` part of the canonical snapshot field set; adding that block necessarily
changes the payload digest versus a pre-tactics baseline. So FR-TI-031 ("reproduce the no-instruction
baseline **exactly**") and DET-002 ("default-tactic digest == no-instruction baseline digest") cannot
both hold once tactics are serialized — two MUST clauses in direct conflict.
**Fix:** FR-TI-031 reworded to **behavioural / world-state** identity (realised trajectories + ball
state + pre-tactics fields are bit-identical); DET-002 compares the **world-state-subset** digest
(excluding the tactics block), and explicitly states the full payload digest differs by design.
Appendix B carries a matching note. *(Resolved v0.3.)*

## Medium

**M-1 — `DefensiveLine` double-serialization / divergence-on-restore.** PASS-1 M-2 said `DefensiveLine`
is "input only, written into the single authoritative `DefensiveLineDepth`," but Appendix B still
serializes `DefensiveLine` as a `TeamTactic` field while `DefensiveLineDepth` is also serialized by
#8/#12 — two depth surfaces that must agree (`resolved == dial + bias`) and could diverge on restore if
the bias function changes. **Fix:** pin that only the **input dial** is in this layer's snapshot block;
the resolved `DefensiveLineDepth` is **recomputed every tick** from dial + mentality and is never an
independently-restorable second surface (§3.4 + Appendix B notes). *(Resolved v0.3.)*

**M-2 — `Tempo`'s utility half is absent from the §3.3 product.** PASS-1 reclassified `Tempo` as raising
"PASS/SHOOT relative to HOLD" — a per-action utility weighting — but the §3.3 `utility' = clamp(…)`
formula listed only four factors (role × mentality × duty × instruction); `Tempo` had nowhere to act on
utility, only on option breadth. **Fix:** added `tempoActionBias[tempo, opt.Type]` as the **fifth**
factor (Standard = identity); §3.3/§3.4/FR-TI-015 and Appendix A.3 (`TempoActionBias[5][7]` +
`TempoBreadthScalar[5]`) updated; the breadth effect is documented as the second, separate Tempo action.
*(Resolved v0.3.)*

**M-3 — §6 perf ignores the new option-generation branches.** §6.3 charged only "four scalar multiplies"
to #8, but the PASS-1 reclassification made `Tempo` and `FocusPlay` **new `OptionGenerator` branches**
that run per tick. **Fix:** §6.3 now accounts for five multiplies + the two bounded per-agent option-gen
passes; estimate widened to < 0.02 ms (still charged to #8, still zero-alloc). *(Resolved v0.3.)*

**M-4 — FR-TI-016 wording lagged the PASS-1 M-1 fix.** It read "feed `ContextModifierInputs` lateral/
vertical compactness" without saying the struct has **no width field** today (verified) and needs a new
one. **Fix:** reworded to "via a **new field** on `ContextModifierInputs`; the scaling code is reused."
*(Resolved v0.3.)*

## Low

**L-1 — §5.7 mis-mapped constant-tag FRs to enum tests.** FR-TI-008/009/010 (constant tags) traced to
`U-001..016` (enum-ordinal tests), which don't verify tags. **Fix:** retargeted to `verify: §9 item 6`
(constant-tag inspection). *(Resolved v0.3.)*

---

## Disposition

All 6 resolved in the v0.3 fix pass (section files → v0.3). H-1 was the load-bearing one: a determinism-
contract contradiction surfaced only by checking what the match engine actually serializes today — the
same "verify the claim against source" discipline that caught the PASS-1 phantom hooks. §9.2 gate G1
remains satisfied (now through PASS-2); G2 (balance pass) and G3 (sign-off) remain open.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-06-20 | — | PASS-2: 1H+4M+1L filed and resolved in the v0.3 fix pass. |
#endregion
