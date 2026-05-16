# Positioning AI Specification #12 — Appendices

**Created:** May 15, 2026
**Last Updated:** May 16, 2026 (v0.2 — PASS-1 adversarial fix pass)
**Version:** 0.2
**Status:** DRAFT

---

## Appendix A — Derivations

This appendix accumulates derivation entries that promote
outline-stage `[EST]` constants to `[GT]` per CLAUDE.md "When
Writing or Editing Specs" and KD-12. Each entry must justify the
chosen value with worked reasoning, citation, or sensitivity
analysis. All entries are PENDING at v0.1 and gate the §9.3 (c)
precondition.

### A.1 `ANCHOR_DWELL_TICKS = 5` (PENDING)
Rationale to be derived: 5 ticks at 10 Hz = 500 ms — the typical
visual-stability window for a soccer match-engine observer. To
be confirmed against #2 §3.1 dwell-time analysis.

### A.2 `LINE_HYSTERESIS_M = 3.0 m` (PENDING)
Rationale to be derived: line boundary placement varies by ±2 m
under normal back-line shifts; 3 m dead zone exceeds normal noise
without admitting deliberate line-breaks.

### A.3 `LINE_DWELL_TICKS = 5` (PENDING)
Same 500 ms rationale as A.1, applied to line-membership commits.

### A.4 `LANE_HYSTERESIS_M = 2.0 m` (PENDING)
Rationale: lane width is 13.6 m; 2 m dead zone is ≈15% of lane
width, the standard hysteresis ratio used by #2 §3.1.

### A.5 `PHASE_HYSTERESIS_TICKS = 3` (PENDING)
Rationale: 300 ms — long enough to filter possession bobbles in
contested midfield, short enough to feel responsive in transition.

### A.6 `PHASE_LOOSE_VELOCITY_THRESHOLD = 4.0 m/s` (PENDING)
Rationale: a hopeful clearance leaves the boot at 15+ m/s; a
tactical short pass at 6–8 m/s; a controlled short layoff at 2–4
m/s. The 4 m/s threshold separates intentional layoffs from
direction-of-travel transitions.

### A.7 `OFFSET_RANGE_X_M = 12.0 m` (PENDING)
Rationale: maximum role displacement under full ball-relative pull
should not exceed half the lane width × 1.5 — i.e., 12 m on the
longitudinal axis. Pending sensitivity test against §3.5 worked
example.

### A.8 `OFFSET_RANGE_Y_M = 8.0 m` (PENDING)
Rationale: maximum lateral pull is constrained by lane integrity
(must not displace an agent across more than one lane).

### A.9 `SPACING_EPSILON_M2 = 1e-4 m²` (KD-16 — `[FIXED]`, derivation here)
At the 1.5 m hard-spacing boundary, a `float` ULP is on the order
of `1e-7` m. An epsilon of `1e-4 m²` corresponds to `1 cm` at the
boundary — three orders of magnitude above ULP noise — providing
stable comparisons across CLAUDE.md-permitted `float` arithmetic
variation on the pinned Stage 0 host. Sensitivity analysis in
Appendix D.

## Appendix B — Formation Archetype Profiles

Each archetype is an 11-row table indexed by `RoleId`, plus the
per-archetype `lineCutIndices = (firstMid, firstAtk)` pair feeding
§3.3.1 (AR-S1-02). All `longPct` and `lateralPct` values are
own-attacking-orientation (orchestrator mirrors for the defending
side). Values are representative; final values gate on lead-
developer ratification under R-01.

Planning-doc citation: `docs/planning/master-development-plan.md`
§3.2 lines 441–449 ("FormationSystem.cs, Month 3–4 deliverable")
enumerates the ten Stage 1 named variants; Stage 0 ships three
structural families (4-4-2 / 4-3-3 / 4-2-3-1) covering two-striker
/ front-three / single-striker-with-AM patterns. See §7.6 and
KD-7.

| Archetype | `lineCutIndices` | Defense / Midfield / Attack |
|---|---|---|
| `FAMILY_4_4_2` | (4, 8) | 4 / 4 / 2 |
| `FAMILY_4_3_3` | (4, 7) | 4 / 3 / 3 |
| `FAMILY_4_2_3_1` | (4, 9) | 4 / 5 / 1 (AM in Midfield via `defaultLine` override) |

### B.1 4-4-2 Family (`FAMILY_4_4_2`)

| Role | longPct | lateralPct | defaultLine | defaultLane |
|---|---|---|---|---|
| GK | 0.05 | 0.500 | (excluded) | C |
| LB | 0.20 | 0.150 | Defense | LH |
| CB1 | 0.20 | 0.380 | Defense | C |
| CB2 | 0.20 | 0.620 | Defense | C |
| RB | 0.20 | 0.850 | Defense | RH |
| LM | 0.50 | 0.100 | Midfield | LW |
| CM1 | 0.50 | 0.380 | Midfield | C |
| CM2 | 0.50 | 0.620 | Midfield | C |
| RM | 0.50 | 0.900 | Midfield | RW |
| ST1 | 0.78 | 0.420 | Attack | C |
| ST2 | 0.78 | 0.580 | Attack | C |

### B.2 4-3-3 Family (`FAMILY_4_3_3`)

| Role | longPct | lateralPct | defaultLine | defaultLane |
|---|---|---|---|---|
| GK | 0.05 | 0.500 | (excluded) | C |
| LB | 0.22 | 0.150 | Defense | LH |
| CB1 | 0.22 | 0.380 | Defense | C |
| CB2 | 0.22 | 0.620 | Defense | C |
| RB | 0.22 | 0.850 | Defense | RH |
| DM | 0.42 | 0.500 | Midfield | C |
| CM1 | 0.55 | 0.350 | Midfield | C |
| CM2 | 0.55 | 0.650 | Midfield | C |
| LW | 0.74 | 0.100 | Attack | LW |
| ST | 0.80 | 0.500 | Attack | C |
| RW | 0.74 | 0.900 | Attack | RW |

(Worked example §3.1.2: LW row `(longPct: 0.743, lateralPct:
0.100)` rounds to the table's 0.74 / 0.10 to ±0.005 m.)

### B.3 4-2-3-1 Family (`FAMILY_4_2_3_1`)

| Role | longPct | lateralPct | defaultLine | defaultLane |
|---|---|---|---|---|
| GK | 0.05 | 0.500 | (excluded) | C |
| LB | 0.22 | 0.150 | Defense | LH |
| CB1 | 0.22 | 0.380 | Defense | C |
| CB2 | 0.22 | 0.620 | Defense | C |
| RB | 0.22 | 0.850 | Defense | RH |
| DM1 | 0.40 | 0.380 | Midfield | C |
| DM2 | 0.40 | 0.620 | Midfield | C |
| LM | 0.62 | 0.150 | Midfield | LW |
| AM | 0.65 | 0.500 | Midfield | C |
| RM | 0.62 | 0.850 | Midfield | RW |
| ST | 0.82 | 0.500 | Attack | C |

## Appendix C — Debug Overlays (Stage 0+1 Deferred)

Pre-committed conventions for the Stage 1+ debug overlay surface
(authoring tools and coach UI per §7.1). NOT a Stage 0
deliverable.

| Overlay | Renders | Source |
|---|---|---|
| Anchor markers | 11 dots per side at `anchor[role]` | §3.1 |
| Slot vectors | line from anchor to current `formationSlot` | §3.7 |
| Line bands | translucent horizontal bands at line cuts | §3.3 |
| Lane bands | translucent vertical bands at lane cuts | §3.4 |
| Phase indicator | corner badge with current `Phase` enum | §3.0 |
| Spacing violations | red bond between violating pair | §3.6 |

## Appendix D — Sensitivity Analysis

Each row records the model output's sensitivity to a 1-σ
perturbation of one constant.

| Constant | Perturbation | Affected output | Expected sensitivity |
|---|---|---|---|
| `ANCHOR_DWELL_TICKS` | ±2 ticks | line/lane oscillation rate | low — already filtered by `LINE_HYSTERESIS_M` |
| `LINE_HYSTERESIS_M` | ±1 m | line membership stability | medium |
| `LANE_HYSTERESIS_M` | ±0.5 m | lane membership stability | medium |
| `PHASE_HYSTERESIS_TICKS` | ±1 tick | phase oscillation count | medium |
| `OFFSET_RANGE_X_M` | ±2 m | shape compactness in transitions | high |
| `MIN_AGENT_SEPARATION_M` | (FIXED — do not perturb) | spacing-violation count | n/a |
| `SPACING_EPSILON_M2` | × 10 | boundary-comparison stability | very low; well above ULP |
| `SCORE_ATK_GAIN` | ±0.02 | compactness when leading | low |
| `FATIGUE_LATERAL_RELAX` | ±0.05 | end-of-match shape | medium |

Concrete numerical sensitivity tables to be filled in during the
v0.2 fix pass against measured output from `T-T-001..T-T-006`.

## Appendix E — Worked Examples (Per-Tick Walk-Throughs)

### E.1 4-4-2 InPoss, Ball at Center Circle

Input: ball `(52.5, 34.0)`, possession own, `scoreDiff = 0`,
`fatigue = 0.0`, `tacticalIntensity = 0.5`.
Phase: `InPoss`. `basisX = 0`, `basisY = 0` → all offsets are 0.
All slots equal anchors from Appendix B.1. No spacing violations.

### E.2 4-3-3 OutOfPoss, Ball in Own Defensive Third

Input: ball `(20.0, 34.0)`, possession opponent, `scoreDiff = -1`,
`fatigue = 0.4`, `tacticalIntensity = 0.7`.
Phase: `OutOfPoss`. `basisX = -0.619`, `basisY = 0`. AM `pullFactor
[AM, OutOfPoss] = (0.60, 0.10)` → AM offset `(-4.46, 0)`. AM
shifts from `anchor.x = 0.65 × 105 = 68.25` to `63.79`. Full team
retreat by ≈4–5 m on average.

### E.3 4-2-3-1 TransToAtk, Ball Loose Mid-Field

Input: ball `(60.0, 40.0)` loose, `ball.vx_filtered = +6 m/s`,
`scoreDiff = 0`, `fatigue = 0.2`, `tacticalIntensity = 0.6`.
Candidate phase: `TransToAtk` after `PHASE_HYSTERESIS_TICKS = 3`
sustained ticks. ST advances; AM advances; defenders hold.
Vertical compactness loosens (`INTENSITY_VERTICAL_GAIN = 0.20`
applied).

## Appendix F — Glossary

| Term | Definition |
|---|---|
| **Anchor** | Per-role pitch-relative target position from the formation archetype lookup. |
| **Archetype** | One of three Stage 0 formation families: 4-4-2, 4-3-3, 4-2-3-1. |
| **Compositor** | The Stage 0 simplified slot-composition pipeline (§3.7) — seven sequential steps (anchor / offset / context / spacing / clamp / line+lane resolve / write) producing the per-agent `formationSlot`. (AR-S1-17 step-count alignment.) |
| **Compactness** | Pair of scalars (lateral, vertical) per phase that scale anchor spread around the centroid. |
| **Dwell** | Tick counter that gates a hysteretic transition. |
| **`formationSlot`** | The per-agent `Vector2` output of #12, copied into each agent's `TacticalContext.FormationSlot` at #8 Step 2. |
| **Lane** | Lateral 5-bin classification of pitch Y-coordinate. |
| **Line** | Longitudinal 3-class partition of outfield agents by X-coordinate. |
| **Phase** | Local 4-value enum classifying ball/possession state for #12-internal use. |
| **Pull factor** | `[GT]` per-role-per-phase scalar for ball-relative offset. |
| **Reference host** | The named developer-workstation pin under KD-15 used for §6.3 budget verification until `certification-platform.md` is filled. |
| **`SENTINEL_NO_SLOT`** | `Vector2.NegativeInfinity` value emitted in place of a slot for substituted / red-carded agents. Distinct from `NaN` so the F3 NaN guard does not rewrite it; the orchestrator skips field writes for sentinel values (AR-S1-07). |

## Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial appendices draft. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. AR-S1-17 glossary Compositor "six → seven sequential steps"; AR-S1-02 Appendix B preamble adds per-archetype `lineCutIndices` table + planning-doc citation; AR-S1-07 `SENTINEL_NO_SLOT` added to glossary. |
