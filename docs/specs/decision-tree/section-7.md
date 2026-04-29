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

---

*End of Section 7 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
