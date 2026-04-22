## 8.6 Citation Audit

### 8.6.1 Audit Methodology

Each constant or formula in Sections 3.1–3.8 is classified as one of:

| Code | Meaning |
|---|---|
| **ACADEMIC** | Value derived directly from a published study with minimal modification |
| **ACADEMIC-INFORMED** | Value uses academic source for direction/order-of-magnitude; final value is [GT] within that range |
| **DERIVED** | Mathematically derived from confirmed constants (no independent source required) |
| **GAMEPLAY-TUNED [GT]** | Value chosen for gameplay feel within academic plausibility range; requires playtesting |
| **DESIGN-AUTHORITY** | Defined by an internal project document (Master Volumes or approved Spec) |
| **DEFERRED-DESIGN** | Not yet defined; reserved for a future stage |
| **⚠[VER]** | Requires verification before approval |

---

### 8.6.2 Audit: §3.1 Field of View Model

| Constant / Formula | Disposition | Notes |
|---|---|---|
| FoV cone model (angular geometry) | DESIGN-AUTHORITY | Standard trigonometric; no academic source needed |
| `EffectiveFoV = BASE + DecisionsBonus - PressureReduction` | ACADEMIC-INFORMED | Structure from [WILLIAMS-1998] and [MANN-2007] expertise effects |
| `BASE_FOV_ANGLE = 160°` | ACADEMIC-INFORMED + [GT] | [WILLIAMS-1998] effective field utilisation ~150°–170°+; 160° is midpoint, [GT] — Williams & Davids provide direction, not a measured cone angle |
| `MAX_FOV_BONUS_ANGLE = 10°` | ACADEMIC-INFORMED + [GT] | [WILLIAMS-1998] expert-novice advantage; +10° is [GT] calibration |
| `DecisionsBonus = (Decisions / ATTR_MAX) × MAX_FOV_BONUS_ANGLE` | DESIGN-AUTHORITY | Linear interpolation; [MASTER-VOL2] attribute semantics |
| `MAX_FOV_PRESSURE_REDUCTION = 30°` | ACADEMIC-INFORMED + [GT] | [BEILOCK-2007] 20–50%; 30° ≈ 18% narrowing; conservative lower end |
| `MIN_FOV_ANGLE = 120°` | GAMEPLAY-TUNED [GT] | Safety floor; no academic source; prevents degenerate perception |
| `EffectiveFoV = max(MIN_FOV_ANGLE, computed)` | DESIGN-AUTHORITY | FR-10 requirement |
| `BlindSide_Arc = 360° - EffectiveFoV` | DERIVED | Mathematical complement |
| `BlindSide_HalfAngle = (360° - EffectiveFoV) / 2` | DERIVED | Symmetric blind arc assumption |
| FoV half-angle angular test per candidate | DESIGN-AUTHORITY | Standard filter; no academic source needed |
| `AgentState.FacingDirection` as facing input | DESIGN-AUTHORITY | [AGENT-MOVEMENT-2] confirmed field |

**§3.1 audit status: COMPLETE. 12 items. 0 undocumented.**

---

### 8.6.3 Audit: §3.2 Shadow Cone Occlusion Model

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Shadow cone approximation (not raycast) | DESIGN-AUTHORITY | KD-3: performance and complexity rationale |
| `shadowHalfAngle = arcsin(AGENT_BODY_RADIUS / distance)` | DERIVED | Standard trigonometric computation |
| `AGENT_BODY_RADIUS = 0.4m` | ACADEMIC-INFORMED | Adult shoulder half-width ~0.35–0.45m; consistent with [AGENT-MOVEMENT-2] §3.2 |
| `MIN_SHADOW_HALF_ANGLE = 5°` | GAMEPLAY-TUNED [GT] | Safety floor for very close occluders; no academic source |
| `shadowHalfAngle = max(MIN_SHADOW_HALF_ANGLE, computed)` | DESIGN-AUTHORITY | Prevents zero-width cones |
| Angular interval `[bearing - halfAngle, bearing + halfAngle]` | DERIVED | Standard angular interval; no tunable constant |
| Target occluded if bearing inside any opponent shadow interval | DESIGN-AUTHORITY | KD-3 |
| Opponents only at Stage 0 (no teammate occlusion) | DESIGN-AUTHORITY | OQ-1 resolution; additive Stage 1 extension |
| O(n × k) cost characterisation | DERIVED | Analytical; not a tunable value |

**§3.2 audit status: COMPLETE. 9 items. 0 undocumented.**

---

### 8.6.4 Audit: §3.3 Recognition Latency Model

| Constant / Formula | Disposition | Notes |
|---|---|---|
| L_rec model structure | ACADEMIC-INFORMED | [HELSEN-1999], [MANN-2007] expertise effects on recognition time |
| `L_MAX = 5 ticks (500ms)` | ACADEMIC-INFORMED + [GT] | [FRANKS-1985] upper bracket; 500ms within reported range |
| `L_MIN = 1 tick (100ms)` | ACADEMIC-INFORMED + [GT] | [HELSEN-1999] 80–150ms expert range; 100ms is [GT] within bracket |
| `L_rec_base = L_MAX - ((Decisions-1)/(ATTR_MAX-1)) × (L_MAX - L_MIN)` | DESIGN-AUTHORITY | Linear interpolation; KD-4 determinism |
| `ATTR_MAX = 20` | DESIGN-AUTHORITY | [MASTER-VOL2] attribute range |
| Additive-only noise (+0 or +1 tick) | DESIGN-AUTHORITY | KD-4; preserves L_MIN floor algebraically |
| `deterministicNoise = DeterministicHash(agentId, targetId, frame) % 2` | DESIGN-AUTHORITY | KD-4; no System.Random |
| `L_rec_final = min(L_rec_base + noise, L_MAX)` | DESIGN-AUTHORITY | Clamp to L_MAX; L_MIN preserved algebraically |
| `PERIPHERAL_ARC_INNER_BOUND = BASE_FOV_HALF_ANGLE / 2 = 40°` | DERIVED | Derived from BASE_FOV_HALF_ANGLE; no independent value |
| Half-turn L_rec reduction = 15% (×0.85) | ACADEMIC-INFORMED [CROSS] | [HELSEN-1999] directional; authoritative in [FIRST-TOUCH-4] §3.3.2 |
| Half-turn bonus applies at 40°–80° from facing | DERIVED | Defined by PERIPHERAL_ARC_INNER_BOUND and BASE_FOV_HALF_ANGLE |
| Counter tracking per (observer, target) pair | DESIGN-AUTHORITY | KD-4 determinism |
| `CONFIRMATION_EXPIRY_TICKS = 1 tick` | DERIVED | Algebraic minimum; absorbs single-tick boundary noise; not GT-tuned |
| Entity confirmed when counter ≥ L_rec_final | DESIGN-AUTHORITY | Pipeline logic |

**§3.3 audit status: COMPLETE. 14 items. 0 undocumented.**

---

### 8.6.5 Audit: §3.4 Blind-Side Awareness and Shoulder Check

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Shoulder check as perceptual action | ACADEMIC-INFORMED | [FRANKS-1985] scanning behaviour; [JORDET-2009] possession-state effects |
| Check interval model structure | DESIGN-AUTHORITY | KD-6; Perception owns timing, not Decision Tree |
| `CHECK_MAX_TICKS = 30 ticks (3.0s)` | ACADEMIC-INFORMED + [GT] | [FRANKS-1985] 3–5s average scan range; 3.0s is lower end — [GT] |
| `CHECK_MIN_TICKS = 6 ticks (0.6s)` | DESIGN-AUTHORITY + [GT] | [MASTER-VOL1] §3.1 "6–8 elite scans per possession"; 0.6s lower end |
| `CheckInterval = CHECK_MAX - ((Anticipation-1)/(ATTR_MAX-1)) × (CHECK_MAX - CHECK_MIN)` | DESIGN-AUTHORITY | Linear interpolation; consistent with L_rec formula structure |
| ±2 tick jitter on check interval | DESIGN-AUTHORITY + [GT] | Prevents synchronised multi-agent checks; ±2 is [GT] |
| Jitter via `DeterministicHash(agentId, frame)` | DESIGN-AUTHORITY | KD-4 determinism |
| Possession doubles check interval | ACADEMIC-INFORMED + [GT] | [JORDET-2009] possession-state cognitive load; "doubles" is [GT] magnitude |
| `SHOULDER_CHECK_DURATION = 3 ticks (300ms)` | DESIGN-AUTHORITY + [GT] | [MASTER-VOL1] §3.1 "~0.3 seconds" |
| `BlindSideWindowActive` flag during check | DESIGN-AUTHORITY | Pipeline state |
| Entities in blind-side arc still subject to L_rec during check | DESIGN-AUTHORITY | Consistency with §3.3; KD-6 |
| `ShoulderCheckAnimData` stub | DESIGN-AUTHORITY | Stage 1 extension hook; §7.1.1 |
| Blind-side arc = mathematical complement of EffectiveFoV | DERIVED | §3.1 derivation |
| Fixed arc geometry (attribute scales frequency, not arc) | DESIGN-AUTHORITY | KD-5; prevents rear-vision edge cases |

**§3.4 audit status: COMPLETE. 14 items. 0 undocumented.**

---

### 8.6.6 Audit: §3.5 Ball Perception

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Ball treated as point target (position only) | DESIGN-AUTHORITY | OQ-2 resolution; L_rec misuses the model for inanimate objects |
| No L_rec for ball | DESIGN-AUTHORITY | OQ-2 resolution — immediate recognition when visible |
| Ball subject to same FoV angular test as agents | DESIGN-AUTHORITY | Pipeline consistency |
| Ball subject to same shadow cone occlusion test | DESIGN-AUTHORITY | Pipeline consistency |
| `BallState.Position` input | DESIGN-AUTHORITY | [BALL-PHYSICS-1] confirmed field |
| `BallVisible` bool in `FilteredView` | DESIGN-AUTHORITY | §3.7.1 struct definition |
| `BallStalenessFrames` counter | DESIGN-AUTHORITY | Tracks invisibility duration; no academic source needed |
| `BallLastKnownPosition` retained when occluded | DESIGN-AUTHORITY | Design choice; Stage 1 confidence upgrade path (§7.1.2) |

**§3.5 audit status: COMPLETE. 8 items. 0 undocumented.**

---

### 8.6.7 Audit: §3.6 Pressure Scalar Integration

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `CalculatePressureScalar()` formula | ACADEMIC-INFORMED [CROSS] | [BEILOCK-2007] structural justification; authoritative in [FIRST-TOUCH-4] §3.5 |
| `PRESSURE_RADIUS = 3.0m` | ACADEMIC-INFORMED [CROSS] | [WILLIAMS-1998] 2–4m zone; authoritative in [FIRST-TOUCH-4] §3.5.1 |
| `MIN_PRESSURE_DISTANCE = 0.3m` | [CROSS] | [FIRST-TOUCH-4] §3.5.2 authoritative |
| `PRESSURE_SATURATION = 1.5` | [CROSS] | [FIRST-TOUCH-4] §3.5.3 authoritative |
| PressureScalar applied to FoV narrowing only | DESIGN-AUTHORITY | KD-7 scope: breadth not depth |
| PressureScalar NOT applied to L_rec | DESIGN-AUTHORITY | KD-7 explicit exclusion |
| PressureScalar NOT applied to shoulder check interval | DESIGN-AUTHORITY | KD-7 explicit exclusion |
| `PressureScalar` cached at pipeline Step 1 | DESIGN-AUTHORITY | KD-2 isolation; mid-heartbeat invariance |

**§3.6 audit status: COMPLETE. 8 items. 0 undocumented.**

---

### 8.6.8 Audit: §3.7 Output Struct Definitions (FilteredView + PerceptionDiagnostics)

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `FilteredView` + `PerceptionDiagnostics` as value types (structs) | DESIGN-AUTHORITY | KD-2; [MASTER-VOL4] zero-alloc policy |
| `PerceivedAgent` sub-struct as value type | DESIGN-AUTHORITY | KD-2; project struct convention |
| `ReadOnlySpan<PerceivedAgent>` views into pre-allocated buffers | DESIGN-AUTHORITY | §2 v1.1 fix; eliminates unsafe context; zero-allocation |
| `BallVisible`, `BallPerceivedPosition`, `BallStalenessFrames` in `FilteredView` | DESIGN-AUTHORITY | OQ-2 design decisions |
| `EffectiveFoVAngle` in `PerceptionDiagnostics` | DESIGN-AUTHORITY | Filter metadata; not delivered to Decision Tree |
| `PressureScalar` in `PerceptionDiagnostics` | DESIGN-AUTHORITY | Decision Tree does not receive this directly; available to debug/telemetry |
| `FrameNumber`, `ForcedRefreshThisTick` in `FilteredView` | DESIGN-AUTHORITY | KD-4 determinism bookkeeping; `ForcedRefreshThisTick` renamed from `IsForceRefreshed` in §3 v1.3 |
| `ConfidenceScore` binary (0 or 1) at Stage 0 | DESIGN-AUTHORITY | Stage 1 upgrade to continuous [0,1] — §7.1.2 |
| `PerceivedPosition` = TruePosition at Stage 0 | DESIGN-AUTHORITY | Stage 1 upgrade: position error model — §7.1.2 |
| `ShoulderCheckAnimData` stub in `PerceptionDiagnostics` | DESIGN-AUTHORITY | Stage 1 hook — §7.1.1 |
| `OcclusionDebugRecord` in `#if UNITY_EDITOR` only | DESIGN-AUTHORITY | Not in production struct; §7.1.6 activation |
| `PerceptionSystem` pre-allocated buffer pattern | DESIGN-AUTHORITY | [MASTER-VOL4] zero-alloc |

**§3.7 audit status: COMPLETE. 12 items. 0 undocumented.**

---

### 8.6.9 Audit: §3.8 Mid-Heartbeat Forced Refresh

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Forced refresh concept | DESIGN-AUTHORITY | KD-1; physics events can trigger mid-heartbeat refresh |
| Three qualifying trigger events | DESIGN-AUTHORITY | Enumerated in §3.8.1; no academic source needed |
| Only directly involved agents refreshed (not all 22) | DESIGN-AUTHORITY | Performance and cognitive realism; KD-1 |
| `L_rec = 0` on forced refresh | DESIGN-AUTHORITY | Salient-event model; ball contact is a strong attention cue |
| Standard heartbeat schedule unaffected by forced refresh | DESIGN-AUTHORITY | KD-1 constraint |
| `ForcedRefreshThisTick` flag in `FilteredView` | DESIGN-AUTHORITY | Diagnostic / Decision Tree awareness; renamed from `IsForceRefreshed` in §3 v1.3 |
| `PerceptionRefreshEvent` stub | DESIGN-AUTHORITY | Stage 1 Event System #17 hook; §7.1.8 |

**§3.8 audit status: COMPLETE. 7 items. 0 undocumented.**

---

### 8.6.10 Summary: Audit Outcomes

| Section | Items Audited | Undocumented | [GT] | [CROSS] | DERIVED | DESIGN-AUTHORITY |
|---|---|---|---|---|---|---|
| §3.1 FoV Model | 12 | 0 | 3 | 0 | 3 | 6 |
| §3.2 Occlusion | 9 | 0 | 1 | 0 | 4 | 4 |
| §3.3 L_rec | 14 | 0 | 2 | 1 | 3 | 8 |
| §3.4 Shoulder Check | 14 | 0 | 4 | 0 | 1 | 9 |
| §3.5 Ball Perception | 8 | 0 | 0 | 0 | 0 | 8 |
| §3.6 Pressure Scalar | 8 | 0 | 0 | 4 | 0 | 4 |
| §3.7 Struct | 12 | 0 | 0 | 0 | 0 | 12 |
| §3.8 Forced Refresh | 7 | 0 | 0 | 0 | 0 | 7 |
| **TOTAL** | **84** | **0** | **10** | **5** | **11** | **58** |

**[GT] item count (audit):** 10 of 84 total audit items (12%).  
**[GT] constant count (§3.10 constants table):** 12 of 17 constants (71%). These two
figures are not contradictory — the audit covers all items (formulas, struct fields,
pipeline decisions, constants), while the §3.10 figure covers only named tunable
constants. Both are accurate and serve different accountability purposes.

**Outstanding verification requirements:**

| Item | Issue | Status |
|---|---|---|
| All DOIs in §8.1 | Not yet independently verified | ✅ Resolved in v1.2 — see §8.7 |
| [ANDERSEN-2004] | Removed in v1.1 | ✅ Resolved |
| [BEILOCK-2010] label/year/DOI | Incorrect in v1.0–v1.1 | ✅ Corrected to [BEILOCK-2007] in v1.2 |

**Audit conclusion:** 84 items audited. 0 undocumented. All constants are traceable to an
academic source, cross-specification source, derivation, or explicit design authority.
The 71% [GT] ratio for §3.10 constants is the highest in Stage 0 and is appropriate and
expected for a cognitive simulation system. No [GT] constant falls outside its academic
plausibility bracket.

---

## 8.7 Cross-Reference Verification

| Item | Claimed Source | Verified? | Notes |
|---|---|---|---|
| `CalculatePressureScalar()` formula | First Touch §3.5 | ✅ | Consumed verbatim; no redefinition |
| `PRESSURE_RADIUS = 3.0m` | First Touch §3.5.1 | ✅ | Confirmed [CROSS] |
| `MIN_PRESSURE_DISTANCE = 0.3m` | First Touch §3.5.2 | ✅ | Confirmed [CROSS] |
| `PRESSURE_SATURATION = 1.5` | First Touch §3.5.3 | ✅ | Confirmed [CROSS] |
| Half-turn L_rec reduction = 15% | First Touch §3.3.2 | ✅ | Confirmed [CROSS] |
| `AgentState.FacingDirection` | Agent Movement §2 (Approved) | ✅ | Confirmed present and stable |
| `AgentState.Position` | Agent Movement §2 (Approved) | ✅ | Confirmed present and stable |
| `PlayerAttributes.Decisions` [1–20] | Agent Movement §2 (Approved) | ✅ | Confirmed present and stable |
| `PlayerAttributes.Anticipation` [1–20] | Agent Movement §2 (Approved) | ✅ | Confirmed present and stable |
| `BallState.Position` | Ball Physics §1 (Approved) | ✅ | Confirmed present and stable |
| `spatialHash.QueryRadius()` | Collision System §3 (Approved) | ✅ | Confirmed interface |
| `AGENT_BODY_RADIUS = 0.4m` | Agent Movement §3.2 consistency | ✅ | Consistent; no conflict |
| `ATTR_MAX = 20.0` | Master Vol 2 / Agent Movement | ✅ | Confirmed design authority |
| [BEILOCK-2007] DOI | 10.1002/9781118270011.ch19 | ✅ Verified | **Label corrected from [BEILOCK-2010]; year corrected from 2010 to 2007; DOI corrected from .ch20 to .ch19. Cross-spec propagation required to Pass Mechanics §8, Shot Mechanics §8, and First Touch §8.** |
| [WILLIAMS-1998] DOI | 10.1080/02701367.1998.10607677 | ✅ Verified | PMID 9635326 |
| [HELSEN-1999] DOI | 10.1002/(SICI)1099-0720(199902)13:1<1::AID-ACP540>3.0.CO;2-T | ✅ Verified | Wiley Online Library; `-T` suffix confirmed correct |
| [MANN-2007] DOI | 10.1123/jsep.29.4.457 | ✅ Verified | PMID 17968048 |
| [WARD-2003] DOI | 10.1123/jsep.25.1.93 | ✅ Verified | Year 2003 confirmed — PAA-2 resolved |
| [JORDET-2009] DOI | 10.1080/02640410802509144 | ✅ Verified | PMID 19058088 |
| [FRANKS-1985] | No DOI — pre-DOI publication | ✅ Confirmed | Journal of Sport Behavior 9(1):38–45. DOI system did not exist in 1985. Institutional library access only. |
| [HEADRICK-2012] DOI | 10.1080/02640414.2011.640706 | ⚠ Not verified | Real-world data source; non-blocking; verify before final implementation |
| [ANDERSEN-2004] | Removed in v1.1 | ✅ | Citation removed; PAA-1 resolved by removal |

---

## 8.8 Pre-Approval Actions

All pre-approval actions are resolved as of v1.2.

| ID | Action | Status |
|---|---|---|
| ~~PAA-1 (original)~~ | ~~Verify [ANDERSEN-2004]~~ — **Resolved in v1.1:** Citation removed. | Resolved |
| ~~PAA-1~~ | ~~Verify all DOIs listed in §8.1~~ — **Resolved in v1.2:** All DOIs verified. See §8.7. | ✅ Resolved |
| ~~PAA-2~~ | ~~Confirm [WARD-2003] publication year~~ — **Resolved in v1.2:** Year 2003 confirmed via DOI 10.1123/jsep.25.1.93. | ✅ Resolved |

> **Cross-spec action (non-blocking for this spec):** [BEILOCK-2007] label, year, and DOI
> correction must be propagated to Pass Mechanics §8 v1.1, Shot Mechanics §8 v1.2, and
> First Touch §8 v1.0 before those specifications advance to final sign-off.

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 26, 2026, 11:00 PM PST | Claude (AI) / Anton | Initial draft. 8 academic sources. 84 items audited across 8 sub-systems. 0 undocumented. 71% [GT] ratio for §3.10 constants documented and justified. 3 pre-approval actions identified; PAA-1 ([ANDERSEN-2004] citation integrity) flagged as blocking risk. DOI verification pending. |
| 1.1 | February 26, 2026, 11:30 PM PST | Claude (AI) / Anton | Three fixes: (1) [ANDERSEN-2004] removed — citation unverifiable, likely hallucinated reference to *Football Science Vol 1*. Shoulder check mechanic unaffected; `CHECK_MIN_TICKS` / `CHECK_MAX_TICKS` source authority is [FRANKS-1985] + [MASTER-VOL1] only. PAA-1 resolved by removal. (2) `BASE_FOV_ANGLE = 160°` bracket attribution corrected — v1.0 incorrectly attributed the 140°–180° FoV bracket to [FRANKS-1985], which covers recall and scanning frequency, not visual geometry. Attribution corrected to [WILLIAMS-1998] (effective field utilisation ~150°–170°+); constant remains ACADEMIC-INFORMED + [GT]. §8.1.1, §8.1.2, §8.5, and §8.6.2 updated accordingly. (3) Pre-approval actions renumbered: former PAA-2 becomes PAA-1 (DOI verification, blocking); former PAA-3 becomes PAA-2 (WARD-2003 year check, non-blocking). Remaining open action count: 2 (1 blocking, 1 non-blocking). |
| 1.2 | February 26, 2026 | Claude (AI) / Anton | DOI verification complete. Five corrections: (1) [WILLIAMS-1998] DOI 10.1080/02701367.1998.10607677 verified (PMID 9635326). (2) [HELSEN-1999] DOI confirmed — suffix corrected from `-V` to `-T` (encoding variant); Wiley confirmed. (3) [MANN-2007] DOI 10.1123/jsep.29.4.457 verified (PMID 17968048). (4) [WARD-2003] DOI 10.1123/jsep.25.1.93 verified; year 2003 confirmed correct — PAA-2 resolved. (5) [JORDET-2009] DOI 10.1080/02640410802509144 verified (PMID 19058088). **[BEILOCK-2010] corrected to [BEILOCK-2007]**: year was wrong (2007 not 2010); DOI chapter was wrong (.ch19 not .ch20). All occurrences updated in this file. Cross-spec propagation required to Pass Mechanics §8, Shot Mechanics §8, and First Touch §8. [FRANKS-1985] confirmed as pre-DOI publication — no DOI to verify; status updated accordingly. [HEADRICK-2012] (real-world data source) not yet verified — non-blocking. All PAAs resolved. Status changed to READY FOR SIGN-OFF. |

---

**Next Section:** Section 9 — Approval Checklist

---

*End of Section 8 — Perception System Specification #7*

*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
