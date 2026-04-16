# Agent Movement Specification - Section 3.1: Movement State Machine

**Created:** February 9, 2026, 12:30 AM PST  
**Version:** 1.2
**Status:** Draft (Revised)
**Dependencies:** Ball Physics Spec #1 (state machine pattern reference), Section 3.2 (Locomotion)

**Updated:** March 4, 2026, 12:00 AM PST

---

## v1.1 Changelog

**CRITICAL FIX:**

1. **Piecewise fatigue model adopted (Appendix A.6.3 resolution):** Replaced linear
   aerobic modifier formula with piecewise model from Section 3.2.4. Changed
   `AEROBIC_MODIFIER_FLOOR` from 0.4 to 0.70, added `AEROBIC_MODIFIER_THRESHOLD` = 0.5.

---


## v1.2 Changelog (March 4, 2026)

**CRITICAL FIXES:**

1. **STUMBLE_SPEED_THRESHOLD corrected from 5.5 to 2.2 m/s:** Section 3.4 (authoritative for stumble mechanics) activates stumble risk above JOG_ENTER (2.2 m/s). Section 3.5 v1.2 correctly updated this value, but Section 3.1 was never updated from its original 5.5f. The comment "Set to SPRINT_EXIT" was incorrect per Section 3.4 design: stumble risk should activate during jogging, not only during sprinting.

2. **STUMBLE_TURN_ANGLE deprecated:** Section 3.4 uses a fraction-based safe zone model (SAFE_FRACTION_MIN/MAX), not an absolute angle threshold. The 60-degree constant was vestigial from an earlier design. Replaced with reference to Section 3.4.4 authoritative stumble trigger model. Constant retained at 60.0f for backward compatibility but marked deprecated with redirect.

---


