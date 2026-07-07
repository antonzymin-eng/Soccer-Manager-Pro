# Decision Tree Specification #8 — Section 7: Future Extensions and Deferrals

**File:** `section-7.md`  
**Purpose:** Define Stage 1+ Decision Tree extensions, explicit Stage 0 deferrals, and permanently excluded architecture patterns.  
**Created:** April 20, 2026  
**Version:** 1.0  
**Status:** ✅ APPROVED — Lead developer signed off April 27, 2026 (draft-level quality gate; see §9 approval checklist)  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

---

## Table of Contents

- [7.1 Stage 1 Extensions](#71-stage-1-extensions)
- [7.2 Stage 2 Extensions](#72-stage-2-extensions)
- [7.3 Stage 3+ Extensions](#73-stage-3-extensions)
- [7.4 Fixed64 Migration Path](#74-fixed64-migration-path)
- [7.5 Permanently Excluded Approaches](#75-permanently-excluded-approaches)
- [7.6 Version History](#76-version-history)

---

## 7.1 Stage 1 Extensions

1. **Formation-driven TacticalContext population** (replaces Stage 0 defaults).
2. **Press assignment coordination** with Positioning/Pressing AI.
3. **Heading decision path** integrated with Heading Mechanics (#10).
4. **Body-part intent enrichment** for pass/shot request payloads where execution specs expose hooks.

---

## 7.2 Stage 2 Extensions

1. Set-piece decision logic (corners, free kicks, throw-ins).
2. Team-level transition behaviors (counter-press, rest defense rules).
3. Enhanced off-ball run intent categories for attacking pattern variation.

---

## 7.3 Stage 3+ Extensions

1. Psychology-informed decision modulation beyond Composure noise.
2. Multi-agent coordination for pressing traps and overloads.
3. Match-context strategic adaptation (scoreline/time/game-state policy layers).

---

## 7.4 Fixed64 Migration Path

Stage 0 uses `float` for utility and selection calculations. Migration target is Fixed64 Spec #9.

Migration constraints:
- preserve deterministic ranking behavior,
- preserve monotonic ordering for all utility formulas,
- document any quantization-induced tie-rate increase in updated Section 5 tests.

No Fixed64 implementation details are defined here; this section records only compatibility requirements.

---

## 7.5 Permanently Excluded Approaches

The following are permanently excluded and require architectural amendment to adopt:

1. **Scripted tactical if/else sequences** replacing utility-based selection.
2. **Direct world-state omniscient reads** bypassing `PerceptionSnapshot` and `MatchContext`.
3. **Execution-system tactical reasoning** (Pass/Shot/Movement must execute, not decide).
4. **Non-deterministic decision randomness** in core loop.

---

## 7.6 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | April 20, 2026 | Claude (AI) / Anton | Initial draft of Section 7 future extensions and exclusions. |
| 1.1 | 2026-07-07 | AI agent | Cheap-item additions: new §7.7 (rest-defense risk dampener) + §7.8 (half-spaces PASS bonus) appended below — both LANDED, not deferrals. |

---

## 7.7 Rest-Defense Risk Dampener — LANDED (cheap-item addition, July 7, 2026)

Motivated by a tactical-theory cross-reference pass: "rest defence" is the defensive structure a team
leaves behind while attacking. Positioning AI #12's new `RestDefenseEvaluator` (its own §3.5/§7.13)
judges per-tick whether enough outfield agents sit behind a coverage line while `IN_POSSESSION`. The
result is routed into a new `TacticalContext.RestDefenseSufficient` field (`Stage0Default` seeds
`true` = identity — zero-value `bool` default is `false`, NOT identity, same trap class as
`Mentality`/`Pressing`). `UtilityScorer.ComputeUtility` multiplies PASS/SHOOT/DRIBBLE (only) by
`TacticalWeights.RestDefenseRiskMult` (`[GT]` 0.85) when insufficient; HOLD/MOVE/PRESS/INTERCEPT are
unaffected. Sufficient coverage applies no dampening — byte-identical to pre-addition.

## 7.8 Half-Spaces PASS Bonus — LANDED (cheap-item addition, July 7, 2026)

Motivated by the same cross-reference pass: modern positional-play theory treats the half-spaces
(the lateral corridor between the touchline and the central channel) as the pitch's highest-value
combination-play zone. Positioning AI #12 already classifies every agent's lateral position into one
of five `LaneId` lanes (LW/LH/C/RH/RW, each 13.6 m) for formation-slot purposes — already team-relative
since #12 operates in the per-team canonical attack-toward-+X frame, so no new axis-mirroring risk was
introduced. A new `TacticalContext.AgentLane` field routes each scoring agent's current lane
(`Stage0Default` seeds `LaneId.C`, the semantically-correct identity); `decision-tree.asmdef` gains the
`TacticalDirector.PositioningAI` reference (the first AI-layer → Mechanics-layer reference beyond
`TacticalInstructions`, permitted by the Physics ← Mechanics ← AI ← UI direction). `UtilityScorer.
ScorePass` multiplies by `TacticalWeights.LaneMult[(int)AgentLane]` — half-space lanes (LH/RH) carry
a bonus; central (C) and wide (LW/RW) lanes stay ×1.0.

---

*End of Section 7 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
