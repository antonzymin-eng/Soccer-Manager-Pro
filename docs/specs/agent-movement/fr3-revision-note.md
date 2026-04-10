# Agent Movement Specification â€” FR-3 Revision Note

**Purpose:** Documents the revision to Functional Requirement FR-3 (Deceleration) in Agent Movement Spec Sections 1 & 2. Apply these changes to `Agent_Movement_Spec_Sections_1_2_v1_0.md` when advancing to v1.1.

**Created:** February 9, 2026, 4:15 PM PST  
**Triggered by:** Section 3.2 development revealed deceleration forces in original FR-3 were biomechanically implausible (27â€“40 m/sÂ² = 2.7â€“4.1g).

---

## Change Summary

**Original FR-3:**
> - Controlled deceleration SHALL bring agent to stop within 2.0â€“3.0 meters from sprint speed
> - Emergency deceleration SHALL bring agent to stop within 1.0â€“1.5 meters but SHALL incur stumble risk if speed was above sprint threshold

**Revised FR-3:**
> - Controlled deceleration SHALL bring agent to stop within **3.0â€“5.0 meters** from sprint speed
> - Emergency deceleration SHALL bring agent to stop within **2.5â€“3.5 meters** but SHALL incur stumble risk if speed was above sprint threshold

---

## Justification

At typical sprint speed (~9 m/s), the original stopping distances required:

| Mode | Original Dist | Force Required | G-Force |
|---|---|---|---|
| Controlled (Agility 20) | 2.0m | 20.25 m/sÂ² | 2.1g |
| Controlled (Agility 1) | 3.0m | 13.5 m/sÂ² | 1.4g |
| Emergency (Agility 20) | 1.0m | 40.5 m/sÂ² | 4.1g |
| Emergency (Agility 1) | 1.5m | 27.0 m/sÂ² | 2.8g |

Sports science literature (Harper & Kiely, 2018; Dos'Santos et al., 2020) reports maximal deceleration for trained team-sport athletes at **5â€“12 m/sÂ²** for intentional stops and **10â€“20 m/sÂ²** for absolute peak single-step plants. The original emergency values (27â€“40 m/sÂ²) exceeded any documented human capability.

The revised values produce:

| Mode | Revised Dist | Force Required | G-Force | Headroom to ~20 m/sÂ² ceiling |
|---|---|---|---|---|
| Controlled (Agility 20) | 3.0m | 13.5 m/sÂ² | 1.4g | 6.5 m/sÂ² |
| Controlled (Agility 1) | 5.0m | 8.1 m/sÂ² | 0.8g | 11.9 m/sÂ² |
| Emergency (Agility 20) | 2.5m | 16.2 m/sÂ² | 1.65g | 3.8 m/sÂ² |
| Emergency (Agility 1) | 3.5m | 11.57 m/sÂ² | 1.18g | 8.4 m/sÂ² |

All values now fall comfortably within documented human capability with meaningful headroom for future tuning.

---

## Downstream Impact

- **Section 3.2 (Locomotion):** Already uses revised values in v1.0.
- **Section 3.1 (State Machine):** No change required â€” state transitions are speed-based, not distance-based.
- **Spec #3 (Collision System):** May reference stopping distances for foul detection momentum thresholds. Verify when Spec #3 is drafted.
- **Spec #12 (Positioning AI):** Stopping distance affects AI path planning â€” agents need to begin deceleration earlier with longer stopping distances. This is a design consideration, not a breaking change.
- **Section 2.4 (Failure Modes):** Runaway velocity detection at MAX_SPEED (12.0 m/s) unchanged.

---

## Gameplay Impact Assessment

Longer stopping distances mean:
- Agents commit more when sprinting â€” they can't stop on a dime
- Defenders must anticipate rather than react at the last moment
- Agility attribute becomes more differentiated and valuable (controlled: 3.0m vs 5.0m is 40% difference; emergency: 2.5m vs 3.5m is 29% difference)
- Emergency stops feel riskier â€” the agent covers more ground while braking
- AI path planning must account for realistic braking zones
- Emergency decel has ~4 m/sÂ² headroom to the documented human ceiling â€” if playtesting demands shorter stops, values can be tightened without breaking realism

This is a net positive for realism and tactical depth. The original values would have made agents feel unnaturally responsive at high speed.

---

**Status:** Pending â€” apply to Sections 1-2 when advancing to v1.1.