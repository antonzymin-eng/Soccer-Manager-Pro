# Defensive AI Specification #14 — Section 7: Future Extensions

**Created:** May 17, 2026
**Last Updated:** August 12, 2026 (v0.2 — KD-6 revised (`ERR-014-006`, wiring backlog W2): §7.1 gains a scoped amendment noting §3.6.5's tackle outcome resolution ships as Stage-0 runtime code, independent of the three Stage-1-activation preconditions.)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

This section catalogues all deferred capabilities, stage-gated activations,
and permanent exclusions for Defensive AI #14. Items here are **not** missing
from the spec — they are explicitly out of Stage 0 scope per the Stage-Binding
Statement in §1.8 and KD-16.

---

## 7.1 Stage 1 — Runtime Activation (KD-16)

The entire §3 algorithm and §4 file structure are Stage 0 specification
deliverables. Runtime code activates at Stage 1. Three preconditions gate
Stage 1 activation (FR-DA-037):

**Amended (KD-6 revised — `ERR-014-006`, wiring backlog W2, August 12,
2026):** §3.6.5's tackle outcome resolution (`TackleOutcomeResolver.cs`)
is a scoped exception — it ships as runtime code alongside this revision,
independent of the three preconditions below, because the delegation it
replaces (#8 mediates dispatch to #3) was never satisfiable and gating it
behind the same three preconditions would have kept it permanently dead
for a reason unrelated to any of them. The three preconditions below still
gate the rest of #14's coordinated-assignment runtime (`MarkDirective`,
`MarkAssignment`, hysteresis, offside trap); this amendment does not
re-derive their status.

**(a) ERR-014-001 ratified:** `TacticalContext.MarkDirective?` nullable field
added to #8 §2.2.6 (Decision Tree). Option B selected at spec-draft time;
back-prop to #8 §2.2.6 is pending lead-developer ratification. Until ratified,
`TacticalContext.MarkDirective?` is not a valid field in the shipped struct
and #14's output has no consumer in the #8 action loop.

**(b) #12 Positioning AI reaches APPROVED:** #12 is the sole source of the
`BaselineDefensiveShapeView`, the team phase enum (FR-DA-013 gate), and the
per-agent `LineMembership` used by anti-chaos invariant 1 (FR-DA-025). #14
cannot activate with an `IN REVIEW` #12 because the accessor surface
(`GetBaselineShape`, `GetPhase`, `GetLine`) is not yet ratified as a stable
contract.

**(c) ERR-014-002 and ERR-014-003 #17 channel rows landed:** `MARK_ASSIGNED`
and `LINE_STEPPED` event channels must be registered in #17 §3.10 within
the `0x18–0x1B` block reserved for #14. The channels are not produced at
Stage 0 (no runtime code); they are consumed by the replay / telemetry layer
at Stage 1.

These three preconditions mirror the same activation pattern used by
Pressing AI #13 §1.8.

---

## 7.2 Stage 1+ — Man-Marking Individual Instructions Override

Per-player man-marking instructions from the team tactics screen allow the
human manager (or AI manager in career mode) to designate a specific opponent
as a manual man-mark target for a named defender. This overrides the automatic
threat-score assignment of §3.3.

**Blocked on:** coach-UI and team-instruction system — neither is in the current
20-spec set. At Stage 1 the assignment algorithm operates fully automatically
based on threat scores (§3.5) and displacement cost (§3.4). Manual assignment
layers above the automatic algorithm and is a UI concern deferred to a future
game-infrastructure spec.

**Stage 1 impact:** none — the §3.3 algorithm ignores manual overrides at Stage 1.
A boolean flag `isManuallyAssigned` is reserved in `MarkAssignment` (§2.2.2)
for Stage 2+ use without a breaking schema change.

---

## 7.3 Stage 1+ — MARK_ASSIGNED / LINE_STEPPED Event Channels (KD-15)

Atomic back-prop into #17 §3.10 via ERR-014-002 (`MARK_ASSIGNED`) and
ERR-014-003 (`LINE_STEPPED`). Channel IDs will be allocated within the
`0x18–0x1B` block reserved for #14 in the Event System channel registry.

**MARK_ASSIGNED:** fired once per agent when a `MarkAssignment` changes mode
or target (not every tick — transitions only). Payload: `agentId`, `newMode`,
`newTargetId`, `tick`.

**LINE_STEPPED:** fired once per team per offside-trap step-up event (§3.7.4).
Payload: `team`, `stepUpTargetDepth`, `agentCount`, `tick`.

Both channels are deferred to Stage 1 first commit per KD-15. No channel
emission code at Stage 0.

---

## 7.4 Stage 1+ — #15 Attacking AI Emergency Signal (KD-8)

`MarkDirective.emergencyFlag` (§2.2.1) indicates that the last-man emergency
protocol (§3.8) is active. At Stage 1+, Attacking AI #15 may consume this
signal as a goal-risk indicator, triggering transition-recovery behaviour
(e.g., suppressing attacking runs to reinforce the backline).

**Blocked on:** #15 Attacking AI is NOT STARTED at the time of this draft.
Per CLAUDE.md "Interface Design Principle," no interface or accessor surface
is produced against #15 until #15 reaches `IN REVIEW` (FR-DA-036). The
`emergencyFlag` field exists in `MarkDirective` for internal #14 use and is
available to the orchestrator; the consuming behaviour on #15's side is a
future concern.

---

## 7.5 Stage 2+ — Set-Piece Defensive Wall Formation (KD-7)

When the team is defending a free-kick, #14 selects the wall members, computes
wall depth, and determines agent spacing. This is confirmed as #14's
responsibility per #11 FR-GK-016 (Goalkeeper Mechanics explicitly assigns
defensive wall placement to #14 rather than to the GK system).

**Blocked on:**
- Set-piece event infrastructure (planned Stage 2+): the match engine needs
  a reliable set-piece detection and mode-switching mechanism.
- Binding to #11 §FR-GK-016 GK wall-request surface (the GK communicates
  the desired wall geometry to #14; the surface is defined in #11 at Stage 1+).
- Wall physics parameters (wall-gap enforcement, encroachment handling) depend
  on Collision System #3 §3.3 contact-physics extensions not yet specified.

**Stage 0 / Stage 1 impact:** none — free-kick defensive setup is absent from
the §3.13 per-tick pipeline. Wall formation is a §7 exclusion at Stage 1.

---

## 7.6 Stage 2+ — Tactical Instructions Overlay (Named Defensive Styles)

High-line, low-block, and mid-block as named styles selectable via the team
tactics screen. At Stage 1, `DefensiveLineDepth` (read from #12) is the sole
continuous parameter governing the defensive line position; the offside-trap
constants (`OFFSIDE_MAX_DEPTH_M`, `MIN_BACKLINE_AGENTS`) are uniform across
formations.

Stage 2+ named styles would map to pre-configured constant bundles:

| Style | Effect |
|---|---|
| High-line | Higher `OFFSIDE_MAX_DEPTH_M` (~48.0 m), lower `MIN_BACKLINE_AGENTS` (2), shorter `OFFSIDE_TRAP_DWELL_TICKS` (2) |
| Mid-block | Default Stage 1 values |
| Low-block | Lower `OFFSIDE_MAX_DEPTH_M` (~30.0 m), higher `MIN_BACKLINE_AGENTS` (4), `OFFSIDE_BALL_SPEED_THRESHOLD_M_S` irrelevant (trap rarely fires in deep block) |

**Blocked on:** team-instruction UI infrastructure. At Stage 1 the `[GT]`
constants in §6.1 are static designer values; the override system is a Stage 2+
addition.

---

## 7.7 Stage 2+ — Threat Score Full 2D Euclidean Proximity

The §3.5 `perceivedGoalProximity` formula uses x-axis distance only (longitudinal
distance from goal). This is a deliberate Stage 0 simplification noted in
§3.5.2 ("Using only the longitudinal distance prevents penalising wide attackers").

Stage 2+ enhancement: replace x-axis proximity with full 2D Euclidean distance
from the own goal centre:

```
xDist = opp.position.x − ownGoalCenter.x
yDist = opp.position.y − ownGoalCenter.y
euclidDist = sqrt(xDist² + yDist²)
maxDist = sqrt(PITCH_LENGTH_M² + HALF_PITCH_WIDTH_M²)   // ≈ 63.0 m diagonal
perceivedGoalProximity_2D = 1.0 − (euclidDist / maxDist)
```

The Stage 2+ formula better accounts for central vs. wide positions. A central
striker at (20.0, 34.0) m scores higher proximity than a corner-flag attacker
at (20.0, 0.0) m.

**Stage 1 impact:** none; the formula in §3.5.2 is intentionally x-axis only.
No constant changes needed — the upgrade is a formula change, not a constant
addition.

---

## 7.8 Stage 2+ — Per-Archetype Defensive Profiles

Different formation archetypes naturally call for different default `[GT]`
constant bundles. Examples:

| Formation | Profile |
|---|---|
| 4-3-3 (pressing high) | `OFFSIDE_MAX_DEPTH_M = 48.0`, `MIN_BACKLINE_AGENTS = 3`, `MAX_MAN_MARK_ASSIGNMENTS = 3` |
| 4-4-2 (standard) | Stage 1 defaults (§6.1) |
| 5-4-1 (deep block) | `OFFSIDE_MAX_DEPTH_M = 30.0`, `MIN_BACKLINE_AGENTS = 4`, `MAX_MAN_MARK_ASSIGNMENTS = 4` |
| 3-5-2 (aggressive pressing) | `MIN_BACKLINE_AGENTS = 2` (wing-backs cover), `MAX_MAN_MARK_ASSIGNMENTS = 5` |

**Blocked on:** formation-archetype metadata system (undefined in the current
20-spec set). At Stage 1 all teams use the same `[GT]` constant values
regardless of formation. Per-archetype profiles are a Stage 2+ addition that
requires the archetype metadata to propagate to `DefensiveAITick.Execute`.

---

## 7.9 Stage 2+ — ML-Tuned [GT] Parameter Fitting

Threat-score weights, hysteresis constants, offside-trap thresholds, and
anti-chaos invariant values are `[GT]` designer-estimated starting points.
At Stage 2+, these constants become tunable via simulation-driven parameter
fitting (using the surrogate metrics from §5.7 and a full xG model from #6 §7).

**Stage 1 impact:** none — constants are static in `DefensiveAIConstants.cs`.
ML tuning is a post-Stage-1 concern. The catalogue format is already compatible:
moving from static `[GT]` constants to runtime-configurable `[GT]` constants
requires no spec-text changes, only a change in how `DefensiveAIConstants.cs`
is populated at Stage 1 startup.

---

## 7.10 Stage 5+ — Fixed64 Migration (per #9)

#14's arithmetic uses `float` at Stage 0 and Stage 1. Fixed64 migration follows
Fixed64 Math Library #9's Stage 5+ schedule per CLAUDE.md "When Writing Code."

**Stage 0 / Stage 1 impact:** none. The formulas in §3 are specified in terms
agnostic to the arithmetic type — the algorithm bodies do not change. At Stage 5+:
- All `float` arithmetic in `MarkAssigner.cs`, `ThreatScoreEvaluator.cs`,
  and `OffsideTrapController.cs` is replaced with `Fixed64` equivalents.
- The determinism digest (§4.6) continues to work; Fixed64 values are
  serialisable with the same byte-width as the current `float` representation.
- `DefensiveAIConstants.cs` numeric literals change type annotation only;
  values are unchanged.

---

## 7.11 Stage 5+ — Cross-Platform Bit-Exact Determinism (per #9)

Cross-platform bit-exact parity is a Stage 5 deliverable, not a Stage 0 or
Stage 1 quality gate (CLAUDE.md "When Writing Code"). At Stage 0–Stage 4,
#14 achieves single-machine determinism via state snapshots (#16 §3.2)
rather than deterministic arithmetic.

**Stage 5 effect on #14:** once Fixed64 arithmetic binds and the platform-certification
host is pinned (`docs/tracking/certification-platform.md`), #14's per-tick
digest must produce bit-identical results across all certified platforms.
The existing digest scope declaration (§4.6) is already the correct format
for that verification; no spec-text changes are needed at Stage 5.

---

## 7.12 Permanent Exclusions

The following are permanently out of scope for Defensive AI #14 regardless of
stage. If any of these become requirements, they belong in a new separate spec.

| Exclusion | Reason | Owning spec (if any) |
|---|---|---|
| Offside-rule adjudication (VAR, flag decisions, goal-line calls) | #14 places defenders; adjudication is a referee / rules concern | Future referee spec (not in 20-spec set) |
| Per-player tactical instructions UI | Coach screen is a game-infrastructure concern | Deferred post-Stage-1 game-infrastructure spec |
| Save-game persistence of #14's authoritative state | Save system is a game-infrastructure concern | Future save-system spec |
| Goalkeeper-as-last-man specialised handling | #11 owns all GK positioning behaviour (KD-7) | Goalkeeper Mechanics #11 |
| Outfield agent stamina effects on tackle mode | Fatigue is an input to perception (#7) but does not alter #14's decision logic at Stage 0 | Possible Stage 2+ extension to §3.6 algorithm |
| Per-match injury tracking integration | Substitution and injury are handled by the `isActive` filter from #7 perception; #14 has no injury-specific logic | Health / Injury spec (not in 20-spec set) |
| Cross-platform bit-exact parity at Stage 0–4 | Fixed64 migration is Stage 5+; state-snapshot determinism is sufficient at Stages 0–4 | Fixed64 Math Library #9 |

---

## 7.13 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial draft. §7.1 Stage 1 gate conditions (three preconditions per FR-DA-037). §7.2 man-marking instructions overlay blocked on coach-UI. §7.3 event channels (KD-15 / ERR-014-002..003) deferred to Stage 1 first commit. §7.4 #15 emergency signal declaration (KD-8 / FR-DA-036). §7.5 set-piece defensive wall (KD-7 / #11 FR-GK-016) blocked on Stage 2+ set-piece infrastructure. §7.6 named defensive styles blocked on team-instruction UI. §7.7 2D Euclidean proximity upgrade noted as Stage 2+ enhancement. §7.8 per-archetype profiles blocked on formation-archetype metadata. §7.9 ML tuning deferred to Stage 2+. §7.10–§7.11 Fixed64 / cross-platform binding per #9 Stage 5+. §7.12 permanent exclusions table (7 items). |
| 0.2 | August 12, 2026 | AI agent (wiring backlog W2) | KD-6 revised (`ERR-014-006`): §7.1 gains a scoped amendment — §3.6.5's tackle outcome resolution ships as Stage-0 runtime code (`TackleOutcomeResolver.cs`) independent of the three FR-DA-037 preconditions, because the delegation it replaces (#8 mediates, #3 owns contact) was never satisfiable. The three preconditions are otherwise untouched and still gate the rest of #14's runtime. |
