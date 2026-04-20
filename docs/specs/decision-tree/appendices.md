# Decision Tree Specification #8 — Appendices

**File:** `appendices.md`  
**Purpose:** Provide derivations, verification tables, and sensitivity notes supporting Section 3 formulas and Section 6 performance claims.  
**Created:** April 20, 2026  
**Version:** 1.0  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

---

## Table of Contents

- [Appendix A — Utility Formula Derivation Notes](#appendix-a--utility-formula-derivation-notes)
- [Appendix B — Worked Verification Examples](#appendix-b--worked-verification-examples)
- [Appendix C — Sensitivity Analysis Notes](#appendix-c--sensitivity-analysis-notes)
- [Appendix D — Determinism Trace Format](#appendix-d--determinism-trace-format)
- [Appendix E — Version History](#appendix-e--version-history)

---

## Appendix A — Utility Formula Derivation Notes

### A.1 Generic utility shape

`U = U_base × M_attr × M_context × M_tactical × (1 − R)`

Where:
- `U` unitless utility score (`[0, +∞)` before final clamps).
- `U_base` unitless base action weight.
- `M_*` unitless multipliers.
- `R` unitless risk (`[0, 1]`).

**Range constraints:**
- `R` must remain within `[0, 1]`.
- final score clamp enforced in scoring stage as defined in §3.2.

### A.2 PRESS urgency derivation example

Given:
- `PressingMode = HIGH`, so base PRESS tactical modifier = `1.4` [GT],
- opponent possession urgency factor = `1.2` [GT].

Effective tactical modifier:
`M_tactical_press = 1.4 × 1.2 = 1.68`.

This is unitless and valid for scoring multiplication.

---

## Appendix B — Worked Verification Examples

### B.1 Per-agent budget verification (Section 6)

Given:
- DT batch budget `B_batch = 4.0 ms` [GT],
- `N = 22` agents.

Derived target:
`B_agent = 4.0 / 22 = 0.1818 ms` [DERIVED].

### B.2 Option count bound verification

Section 3.1 candidate composition:
- PASS up to 10,
- SHOOT 1,
- DRIBBLE 1,
- HOLD 1,
- MOVE 1,
- PRESS 1,
- INTERCEPT 1.

Baseline composition total:
`10 + 1 + 1 + 1 + 1 + 1 + 1 = 16`.

Section 3.1 invariant **INV-GEN-09** sets the hard cap to **17** because the option buffer reserves one additional slot for safety/fallback handling. Tests must verify both constraints simultaneously:\n+- baseline generated composition ≤ 16 for normal branch logic,\n+- absolute buffer/invariant cap ≤ 17 under all conditions.

---

## Appendix C — Sensitivity Analysis Notes

### C.1 Composure noise sensitivity

Inputs:
- `Composure` normalized range `[0,1]`.
- noise bound constants from §3.3.

Expected behavior:
- lowering composure increases action-ranking volatility,
- elite composure narrows volatility but does not force zero noise if suppression < 1.

### C.2 Tactical multiplier sensitivity

`PRESS_URGENCY_FACTOR` [GT] range recommendation: `[1.0, 1.5]`.

- At 1.0: no extra urgency effect.
- At 1.5: aggressive press prevalence rises sharply; balance tests required.

---

## Appendix D — Determinism Trace Format

Recommended trace fields per agent per heartbeat:
- `FrameNumber`
- `AgentId`
- option list snapshot (type + scored utility)
- selected action
- deterministic hash input tuple (`seed`, `agentId`, `tick`, `actionTypeOrdinal`)

This trace format supports deterministic replay diffing when two runs diverge.

---

## Appendix E — Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | April 20, 2026 | Claude (AI) / Anton | Initial appendices draft with derivation, worked examples, sensitivity notes, and determinism trace format. |

---

*End of Appendices — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
