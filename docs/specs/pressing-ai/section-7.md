# Pressing AI Specification #13 — Section 7: Future Extensions

**Created:** May 17, 2026
**Last Updated:** May 17, 2026
**Version:** 0.3
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

All Stage 0 deferrals enumerated below per KD-12. Each subsection
states the deferred scope, the stage gate, and the named binding
slot reserved by Stage 0 work (CLAUDE.md "Interface Design
Principle" — no interface written against unspecified consumers).

## 7.1 Stage 1 — Runtime Activation (KD-12)

Implementation lands once **all three** preconditions clear
(FR-PR-044):

1. #8 ratifies the `ERR-013-001` amendment text (mechanism per
   OI-001 — accessor vs `TacticalContext.PressDirective` field).
2. #12 Positioning AI reaches `APPROVED` (currently `IN REVIEW`).
3. #17 channel rows for `PRESS_TRIGGERED` / `PRESS_DISENGAGED`
   land via `ERR-013-002` / `ERR-013-003`.

Stage 1 first-commit deliverables (deferred from Stage 0):

- `src/PressingAI/*.cs` per §4.2 file structure.
- `[HotPathAllocExempt]` annotations on every hot-path method.
- Channel-registry rows in #18 Appendix F.0 schema for the two
  #17 channels.

## 7.2 Stage 1+ — Named Press Styles

`PressStyle` enum (high / mid / low block) selectable per match
via team-instruction infrastructure (planning doc Month 5–6 "Team
Instructions"). Each style remaps the `PRESS_ZONE_X_MIN` /
`PRESS_ZONE_X_MAX` window and may also re-tune
`MAX_PRESSERS_BALL_THIRD`.

**Reserved name:** `PressStyle` (§2.2.7). NOT implemented at Stage 0.

## 7.3 Stage 1+ — Custom Trap-Zone Authoring

`TrapZonePolygon` selectable per opponent for scouted weaknesses
(e.g., funnel weak-foot side toward the corner). Polygon mesh
supersedes the rectangular `PRESS_ELIGIBLE_ZONE` at Stage 1+.

**Reserved name:** `TrapZonePolygon` (§2.2.7). NOT implemented at
Stage 0.

## 7.4 Stage 1+ — #14 Defensive AI Handoff

Mark/cover hand-off rule between `HOLD_SHAPE` agents (owned by
#14) and pressing agents (owned by #13). KD-5 declares the
disjoint partition; the runtime composition is Stage 1+.

Per CLAUDE.md "Interface Design Principle", no interface is
published against #14 until #14 reaches `IN REVIEW`.

## 7.5 Stage 1+ — `PRESS_TRIGGERED` / `PRESS_DISENGAGED` Channels via #17

Two debug-overlay and #14-handoff telemetry channels are
deferred to Stage 1+, each requiring an atomic back-propagation
patch into #17 §3.10 (same pattern as `ERR-017-001`):

| Channel (proposed name) | Purpose | Stage |
|---|---|---|
| `PRESS_TRIGGERED` | A press directive becomes non-empty (ERR-013-002) | 1+ |
| `PRESS_DISENGAGED` | A press directive returns to all-`HOLD_SHAPE` (ERR-013-003) | 1+ |

Both channels are Stage 1+ per KD-11 ("no #17 channels at Stage
0"). Channel-registry-schema rows land in #18 Appendix F.0 at the
Stage 1 first commit per #18 Appendix F.0 / §7.2 (mirroring
Heading Mechanics #10 / Goalkeeper #11 conventions).

## 7.6 Stage 1+ — Stamina-Fatigue Model Integration

The planning document references a "Fatigue System" as a Stage 1
deliverable. The current `decision-tree/section-3-1.md` L753
contains a stale reference labelling that system as "#13" — but
#13 is **Pressing AI**, not the Fatigue System (`ERR-013-004`
filed at section-file draft as a one-token patch request).

Once the Fatigue System spec is allocated and approved, #13's
`STAMINA_COST_PRIMARY_PER_TICK` / `STAMINA_COST_SHADOW_PER_TICK` /
`PRESS_FATIGUE_CEILING` parameters bind to it as `[CROSS]` rather
than `[GT]`.

## 7.7 Stage 2+ — ML-Tuned `[GT]` Parameter Fitting

Trigger thresholds, role caps, hysteresis counts, threat-score
weights, and stamina costs are hand-tuned at Stage 0 and Stage 1.
Stage 2+ may apply offline ML to fit the `[GT]` table from
match-event data. Constant tags remain `[GT]` either way — only
the source of the values changes.

## 7.8 Stage 2+ — Per-Archetype Press Preferences

E.g., 4-3-3 default high-press, 5-3-2 default mid-block. Bound to
#12 archetype enum once Stage 2+ team-instruction infrastructure
lands.

## 7.9 Stage 5+ — Fixed64 Migration per #9

`float` arithmetic at Stage 0 / Stage 1 (per CLAUDE.md "When
Writing Code" and #9 §8.1). Stage 5+ binds the Fixed64 library
when cross-platform multiplayer becomes a requirement. The
`SPACING_EPSILON_M2` constant cited from #12 §3.6.1 will be
re-derived in fixed-point ULPs at that migration.

## 7.10 Stage 5+ — Cross-Platform Determinism

Single-machine determinism is achieved at Stage 1 via state
snapshots (`RoleHysteresisState` + `PressTrigger` are digested
per §4.6). Cross-platform bit-exact parity is deferred to Stage
5+ when Fixed64 lands.

## 7.11 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. `ERR-013-004` filed: "Fatigue System #13" stale reference verified present at `decision-tree/section-3-1.md` L753. |
| 0.2 | 2026-07-07 | AI agent | Cheap-item addition: new §7.12 (curving-press blind-side bias) appended below — LANDED, not a deferral. |
| 0.3 | 2026-07-07 | AI agent | **Redesigned after user review** — see the updated §7.12 below. The original "blind-side approach" (bias toward the position opposite the ball carrier's facing) was the wrong mechanic: curving runs are for adjusting COVER SHADOW to deny a passing option during pursuit, not for approaching the carrier from an unseen angle, and the effect must be gated by the presser's own attributes rather than a flat bonus. |

## 7.12 Cover-Shadow Curving — LANDED, redesigned (cheap-item addition, July 7, 2026)

**Original framing (superseded same day):** the first version of this addition biased the primary
presser's approach target toward the position opposite the ball carrier's facing direction (a
"blind-side approach" — reduced reaction time from an unseen angle). User review corrected this: that
is not what a curving press run is for, and applying it as a flat, attribute-independent bias was
architecturally wrong (it manufactured a tactical bonus no player attribute earned).

**Corrected model:** a curving press run adjusts the presser's **cover shadow** — bending the pursuit
path so the presser simultaneously closes down the ball carrier AND denies a nearby passing option,
rather than running a straight line that leaves an easy outlet pass open. `CoverShadowCurve.ApplyCurve`
(pure static, `src/pressing-ai/CoverShadowCurve.cs`) finds the nearest eligible opponent receiver to
the ball carrier (within `CoverShadowCandidateRadiusM`, the same radius `CoverShadowSelector` uses),
computes the shadow-lane point between carrier and receiver (`CoverShadowLaneFraction`, the same lane
concept `CoverShadowSelector` uses), and blends the presser's raw interception target toward that lane
point. The blend weight is `CoverCurveBlendWeightMax` (`[GT]` 0.35, capped below 1.0 so even the best
presser never abandons the direct press) scaled by `CoverShadowCurve.ComputeCurveEffectiveness` — the
presser's own attribute average (`DefensivePositioningAttribute`, `PhysicalEffortAttribute` — the
average of work rate / pace / stamina, `MentalSharpnessAttribute` — the average of decisions /
anticipation — all new `PressingAgentSnapshot` fields, raw 1–20 scale, sourced by `MatchEngine` from
the same `_dtAttrs` the Decision Tree already reads) normalised by `ATTRIBUTE_SCALE_MAX`. A poor,
unfit, low-effort defender (low attributes) curves almost none — effectively a straight line, matching
the intuition that such a player "will not be trying his hardest to pursue the ball and use curving
run correctly." Applied in `PressingAITick` Step 3 **after** `PrimaryPressSelector.Select` returns —
who is selected as primary presser is unaffected; only their movement target curves. No carrier, an
inactive carrier, or no eligible nearby receiver leaves the target unchanged.
