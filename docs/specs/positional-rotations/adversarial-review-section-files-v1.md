# Positional Rotations Specification #25 — Section-Files PASS-1 Adversarial Review

**Created:** July 8, 2026
**Reviewed set:** all 11 section files at v0.1
**Findings:** 1 High / 1 Medium / 3 Low — all resolved in the v0.2 fix pass, same day. PASS-2 run
after the fixes per the pipeline rule (High found ⇒ repeat until no High); PASS-2 re-read of the
fixed surfaces found nothing further at H/M.
**Method:** struct-surface verification against `src/positioning-ai/AgentPositioningData.cs`;
constant verification against `PositioningAIConstants.cs`; hysteresis pseudocode traced against
the FR text; determinism analysis of the restore path.

---

## H-1 — §4.2's "previous-tick composed targets" do not exist, and the proposed restore path breaks byte-identity

§4.2 v0.1 claimed the trigger predicate reads "the previous heartbeat's composed targets (already
stored on `AgentPositioningData` from the last tick)". **The struct has no such field** (verified:
`EntityId`/`SlotIndex`/`Position`/`IsActive`/`Role`/`IsGoalkeeper` only). Worse, §4.2's restore
note ("the restore path re-runs that seeding against restored bindings") meant post-restore
trigger evaluation would run against `SeedFromFormation`'s *baseline* compose instead of the
actual previous tick's contextual targets — an unbroken run and a save/restored run would diverge
at the first post-restore trigger evaluation, failing FR-RO-013's own byte-identity contract and
T-RO-DET-003.

**Resolution (v0.2):** the controller owns a per-agent `LastComposedTarget` cache (`Vector2` ×
roster), written at the end of every #12 tick, **serialized** in the Appendix B block, and
restored verbatim (no re-seed). Boot populates it from `SeedFromFormation`'s initial compose. §4.2,
§2.2, FR-RO-013, Appendix B, F-modes (non-finite cache gate), and T-RO-I-003/T-RO-DET-003 updated.

## M-1 — Phase-exit reset/freeze contradiction

FR-RO-010/§3.4 mandate phase exit *freezes* dwell accumulation — but §3.1 v0.1 folded the phase
term into the predicate, and §3.2 resets dwell to 0 on any predicate miss, so a phase exit RESET
the dwell instead of freezing it. **Resolution:** the phase check is hoisted out of the predicate
into an outer evaluation gate — out of phase, dwell is left unchanged (frozen), the hold countdown
continues (§3.4 unchanged), and the now-pure-geometric predicate is not evaluated. §3.1, §3.2,
FR-RO-004 updated; T-RO-U-003 already tested the correct (freeze) behaviour — the v0.1 test plan
contradicted the v0.1 pseudocode, which is exactly the encode-vs-catch failure mode this project's
AR history warns about, caught here at spec stage.

## L-1 — Displacement inequality strictness

§3.1's guarantee used `<`; the two `≥ advantage` bounds only give `≤`. Corrected.

## L-2 — Pair-state restore gates unspecified

Added F6: `Rotated` byte ∈ {0,1}; `HoldTicksRemaining ≤ ROTATION_HOLD_TICKS` and > 0 only while
`Rotated`; `TriggerDwellTicks ≥ 0` and finite — note it may legitimately exceed
`ROTATION_TRIGGER_DWELL_TICKS` when the per-tick commit cap defers an eligible commit, so no upper
bound is imposed on it. Test added.

## L-3 — Unverified line-dwell claim

"sits well above today's line-dwell constant" was asserted without a value. Verified:
`PositioningAIConstants.LINE_DWELL_TICKS = 5` (`PositioningAIConstants.cs:92`), so
`ROTATION_HOLD_TICKS = 30 ≥ 5` holds with 6× margin; Appendix D now states the verified value.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-07-08 | — | PASS-1 filed and resolved (1H+1M+3L); PASS-2 re-read clean at H/M — v0.2 fix pass same day. |
#endregion
