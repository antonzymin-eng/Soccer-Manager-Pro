# Defensive AI Specification #14 — Adversarial Review: Section Files v1

**Created:** May 17, 2026
**Review Pass:** PASS 1
**Reviewer:** AI agent (adversarial review mode)
**Scope:** All section files authored in v0.1 (section-1.md through section-9-approval-checklist.md, appendices.md)
**Target branch:** `claude/adversarial-review-ai-specs-JDICJ`

---

## Review Summary

| Severity | Count | Status |
|----------|-------|--------|
| High (H) | 6 | All resolved in v0.2 |
| Medium (M) | 7 | All resolved in v0.2 |
| Low (L) | 4 | All resolved in v0.2 |
| **Total** | **17** | **All resolved** |

---

## High-Severity Findings

### AR-S1-H1 — §3.3.3 Hysteresis Pre-Check Fires on ZONAL Agents

**File:** `section-3.md` §3.3.3 Step 1
**Finding:** The hysteresis pre-check `if hysteresis[agent].dwellCounter > 0` was guarding
all agents regardless of current mode. For an agent in `ZONAL` mode, a stale `dwellCounter`
(leftover from a previous non-ZONAL assignment) would suppress re-evaluation, permanently
locking the agent in ZONAL and preventing it from picking up a new MAN_MARK or INTERCEPT_RUNNER
target. The intent is to prevent thrash on non-ZONAL transitions only — ZONAL re-evaluation
must always run to let agents respond to newly threatening opponents.

**Impact:** Logic defect — agents transitioning out of emergency overrides could be stuck
in ZONAL for up to `MARK_DWELL_TICKS` extra ticks.

**Resolution (v0.2):** Added `assignments[agent].mode != MarkMode.ZONAL AND` guard to Step 1.
The `ResetHysteresis` call in §3.8.3 and §3.9.2 clears the counter on emergency overrides, but
the guard is the canonical safety for any path that doesn't call `ResetHysteresis`.
Test reference: T-DA-054.

---

### AR-S1-H2 — §2.2.2 `MarkAssignment` Missing `overriddenThisTick` and `isManuallyAssigned` Fields

**File:** `section-2.md` §2.2.2
**Finding:** The struct `MarkAssignment` was defined with only four fields (`mode`, `targetEntityId`,
`targetPosition`, `dwellTicksRemaining`). However, `overriddenThisTick` is used in:
- §3.8 Step 3 (set after emergency override)
- §3.9.2 (set after GK-cover override)
- §3.3.3 Step 1 (read to skip overridden agents)
- §3.10.3 Invariant 1 and Invariant 3 (read to protect overridden assignments)
- §3.13 main-loop pseudocode (read in Step 5)

And `isManuallyAssigned` is used in §7.2 (reserved for Stage 2+ man-marking instructions).
Both fields were present throughout §3 algorithms but absent from the struct definition —
a critical inconsistency that would prevent a correct implementation.

**Impact:** Specification defect — implementation would have no source of truth for these fields.

**Resolution (v0.2):** Added both fields to `MarkAssignment` struct in §2.2.2 with correct
types and doc-comments. `targetEntityId` comment also corrected to state "null for ZONAL and
COVER_GK_ZONE" (was incorrectly implied to be always non-null).

---

### AR-S1-H3 — §3.10.3 Invariant 1 Uses `>` Instead of `>=`

**File:** `section-3.md` §3.10.3
**Finding:** The Invariant 1 condition read:
```
if defenseLineInZonal < MIN_BACKLINE_AGENTS AND defenseLineTotal > MIN_BACKLINE_AGENTS:
```
The `>` check is wrong: if `defenseLineTotal == MIN_BACKLINE_AGENTS` exactly (e.g., 3 DEFENSE
agents total with all 3 in ZONAL), the condition body would be unreachable even though the
invariant is satisfied — causing a spurious F4 fallback when the demotion pool is empty.
The correct semantics is `>=`: if there are at least `MIN_BACKLINE_AGENTS` agents on the
DEFENSE line total, a demotion attempt is warranted.

**Impact:** Logic defect — legitimate team shapes with exactly `MIN_BACKLINE_AGENTS`
DEFENSE-line agents could trigger the F4 hard fallback unnecessarily.

**Resolution (v0.2):** Changed `>` → `>=` in the Invariant 1 condition.

---

### AR-S1-H4 — §2.2.4 `MarkHysteresisState` Missing Three Fields

**File:** `section-2.md` §2.2.4
**Finding:** `MarkHysteresisState` in v0.1 contained only `currentMode` and `dwellCounter`.
But §3.11.2 specifies four fields: `dwellCounter`, `candidateMode`, `candidateTargetId`,
`holdTicks`. The fields `candidateMode`, `candidateTargetId`, and `holdTicks` are required
for the `ApplyHysteresisGate` algorithm (§3.11.3) to track which candidate is being
evaluated and for how many consecutive ticks it has been preferred. Without these, the
hysteresis gate could not be implemented. Additionally, the v0.1 field `currentMode`
does not appear in §3.11.2 or anywhere in §3 — it appears to be a leftover from an
earlier design.

**Impact:** Specification defect — inconsistency between §2.2.4 and §3.11.2; implementation
would have no source of truth.

**Resolution (v0.2):** Replaced `MarkHysteresisState` definition in §2.2.4 with the
four-field definition from §3.11.2: `dwellCounter`, `candidateMode`, `candidateTargetId`,
`holdTicks`.

---

### AR-S1-H5 — §3.10.3 Invariant 1 Demotion Candidate Null-Ref on COVER_GK_ZONE

**File:** `section-3.md` §3.10.3
**Finding:** The Invariant 1 demotion candidate selector was:
```
demoteCandidate = argmin over {a : lineMembership[a] == DEFENSE
                                  AND assignments[a].mode != ZONAL}:
    ThreatScore(perception.GetAgent(assignments[a].targetEntityId))
```
`COVER_GK_ZONE` assignments always have `targetEntityId = null` (§2.2.2, §3.9.2).
Calling `ThreatScore(perception.GetAgent(null))` would produce a null-reference error
at runtime. Additionally, emergency-overridden agents (last-man intercept, GK-cover)
should not be demoted — that would undo critical safety assignments.

**Impact:** Runtime null-reference defect; emergency assignments silently violated.

**Resolution (v0.2):** Added `AND assignments[a].targetEntityId != null AND NOT
assignments[a].overriddenThisTick` to the argmin filter.

---

### AR-S1-H6 — §3.7.4 Offside Trap Overwrites Emergency Override Assignments

**File:** `section-3.md` §3.7.4
**Finding:** The offside trap execution loop assigned ZONAL positions to all DEFENSE-line
agents unconditionally:
```
for each agent a with LineMembership == DEFENSE in holdShapePool:
    assignments[a] = MarkAssignment { mode = ZONAL, ... }
```
The offside trap runs at Step 7 in the §3.13 main loop, AFTER the emergency override steps
(Steps 4/4a). An agent with `overriddenThisTick = true` (last-man INTERCEPT_RUNNER or
GK-cover COVER_GK_ZONE) on the DEFENSE line would have its emergency assignment silently
overwritten by a ZONAL position, removing the safety protection at the worst possible moment.

**Impact:** Critical safety defect — last-man emergency protocol disabled by offside trap
in scenarios where both conditions fire simultaneously.

**Resolution (v0.2):** Added `if assignments[a].overriddenThisTick: continue` guard
at the top of the offside trap loop.

---

## Medium-Severity Findings

### AR-S1-M1 — §6.1 Header and §9.1 / §9.4 Reference "27-entry Catalogue" (Actual: 26)

**File:** `section-6.md` §6.7; `section-9-approval-checklist.md` §9.1 item 4, §9.4 item 12
**Finding:** Version history rows and checklist evidence text claimed "27-entry constant
catalogue." Counting the actual §6.1 entries: 22 `[GT]` constants + 4 `[CROSS]`/`[CROSS-PENDING]`
constants = 26 entries. The off-by-one was introduced at draft time and propagated to the
approval checklist evidence.

**Resolution (v0.2):** Corrected "27-entry" → "26-entry" in §6.7 v0.1 history row,
§9.1 item 4 evidence, and §9.4 item 12.

---

### AR-S1-M2 — §2.2.5 `OffsideLineState` Missing `coverGkZoneActiveTicks` Field

**File:** `section-2.md` §2.2.5
**Finding:** `OffsideLineState` was defined with three fields: `currentLineDepth`,
`stepUpDwellCounter`, `cooldownTicksRemaining`. But §3.9.2 reads and writes
`offsideState.coverGkZoneActiveTicks` to track consecutive ticks that COVER_GK_ZONE
is active. This field is also referenced in §3.13 (main loop), §6.5 (memory footprint
calculation), and Appendix F (glossary). Its absence from §2.2.5 is a struct-definition
inconsistency.

**Resolution (v0.2):** Added `coverGkZoneActiveTicks` (int) field to `OffsideLineState`
in §2.2.5 with appropriate doc-comment.

---

### AR-S1-M3 — §2.3 Inputs Table: "Per-Agent FirstTouch" Should Be "Per-Opponent FirstTouch"

**File:** `section-2.md` §2.3
**Finding:** The inputs table listed "Per-agent `FirstTouch`, `Tackling`, `Anticipation`"
under the #7 Perception row. `FirstTouch` is used in the threat score formula (§3.5) to
evaluate *opponents* — #14 reads the perceived attribute of the opposing player being
assessed, not its own-team agents. Describing it as "per-agent" (which typically means
own-team agents in this spec's terminology) is misleading.

**Resolution (v0.2):** Changed "Per-agent" → "Per-opponent" for the `FirstTouch` /
`Tackling` attribute row.

---

### AR-S1-M4 — §3.3.3 Step 7 `ApplyHysteresisGate` Missing `targetPosition`

**File:** `section-3.md` §3.3.3 Step 7
**Finding:** The `ApplyHysteresisGate` call in Step 7 constructed:
```
MarkAssignment { mode = bestMode, targetEntityId = bestTarget }
```
This omits `targetPosition`. Per §2.2.2, `targetPosition` is non-null for all non-ZONAL
assignments (it holds the position to move toward). Since `bestTarget` is an EntityId of
a known opponent in the perception snapshot, the correct `targetPosition` is
`perception.GetAgent(bestTarget).position`. Without it, the committed assignment would
have `targetPosition = null` for MAN_MARK and INTERCEPT_RUNNER modes, causing the §3.10.3
Invariant 3 displacement check and the §3.6 tackle-intent `dist` calculation to operate
on a null vector.

**Resolution (v0.2):** Added `targetPosition = perception.GetAgent(bestTarget).position`
to the `ApplyHysteresisGate` argument construction in Step 7.

---

### AR-S1-M5 — §3.10.3 Invariant 3 Does Not Skip `overriddenThisTick` Agents

**File:** `section-3.md` §3.10.3
**Finding:** The Invariant 3 displacement check iterated all non-ZONAL agents:
```
for each a in holdShapePool:
    if assignments[a].mode != ZONAL AND assignments[a].targetPosition != null:
        displacement = distance(...)
        if displacement > MAX_MARK_DISPLACEMENT_M:
```
Emergency override agents (last-man INTERCEPT_RUNNER at 18 m from goal, GK-cover
COVER_GK_ZONE with `abandonedZoneCenter` potentially 7.5 m from own goal) may legally
produce displacements exceeding `MAX_MARK_DISPLACEMENT_M = 20.0 m` — by design.
Demoting them would undo safety protocols.

**Resolution (v0.2):** Added `AND NOT assignments[a].overriddenThisTick` to the Invariant 3
violation condition.

---

### AR-S1-M6 — `Anticipation` Listed as Consumed Attribute in §2.3 and XC-014-005

**File:** `section-2.md` §2.3; `section-8.md` XC-014-005
**Finding:** Both §2.3 inputs table and XC-014-005 "what is consumed" column listed
`Anticipation` as a consumed opponent attribute alongside `FirstTouch` and `Tackling`.
However, no formula or algorithm in §3 references `Anticipation`. §3.5 (threat score)
uses only `FirstTouch`; §3.6 (tackle intent) mentions `Tackling` as declared for future
use but does not consume it. Listing `Anticipation` implies it influences #14's output,
which is incorrect and would mislead the #7 implementation team about what to expose.

**Resolution (v0.2):** Removed `Anticipation` from §2.3 inputs row and XC-014-005 consumed
list. Added annotation on `Tackling`: "declared for future tackle-quality use; not consumed
by §3.6 algorithm at Stage 0."

---

### AR-S1-M7 — Block Layout for `DOMAIN_TAG_DEFENSIVE_AI` Incorrectly States `0x18` = #11 Without Race Caveat

**File:** `section-1.md` §1.3.3; `section-4.md` §4.6; `section-6.md` §6.1; `section-8.md` XC-014-022 and ERR-014-004
**Finding:** All five sites described the Phase B/C block as `0x17` = #12, `0x18` = #11,
`0x19` = #13, `0x1A` = #14, `0x1B` = #15. Per CLAUDE.md OPEN ISSUES (Goalkeeper Mechanics
#11 and Positioning AI #12 both IN REVIEW), the ERR-011-001/ERR-012-001 race means that if
#12 reaches `APPROVED` first, #11 shifts to `0x1D` rather than `0x18`. The block layout was
stated as fact when it depends on an unresolved race condition.

**Impact:** Documentation inaccuracy — overstates certainty about #11's slot.

**Resolution (v0.2):** All five sites updated to: "0x18 or 0x1D = #11 (ERR-011-001/ERR-012-001
race — if #12 reaches APPROVED first, #11 shifts to 0x1D); #14's 0x1A slot is stable
regardless of that race outcome."

---

## Low-Severity Findings

### AR-S1-L1 — §5.9 Version History States "59 named unit and integration tests"

**File:** `section-5.md` §5.9
**Finding:** The v0.1 version history row stated "59 named unit and integration tests
(T-DA-001..T-DA-071 contiguous)." The range T-DA-001..T-DA-071 contains 71 tests, not 59.
The count and the range contradicted each other.

**Resolution (v0.2):** Corrected "59 named" → "71 named".

---

### AR-S1-L2 — §9.1 Item 32 Evidence States "13-entry table" for Appendix F Glossary

**File:** `section-9-approval-checklist.md` §9.1 item 32
**Finding:** Item 32 evidence read "13-entry table" for `appendices.md` Appendix F.
The actual Appendix F glossary contains 16 entries (confirmed by counting entries in
Appendix F of the v0.1 appendices.md file, which lists: ZONAL, MAN_MARK, INTERCEPT_RUNNER,
COVER_GK_ZONE, HOLD_SHAPE pool, MarkDirective, MarkAssignment, MarkHysteresisState,
OffsideLineState, TackleIntentRequest, threat score, displacement cost, last-man,
offside trap, hysteresis, anti-chaos invariants = 16 entries).

**Resolution (v0.2):** Corrected "13-entry" → "16-entry".

---

### AR-S1-L3 — §4.4.2 Accessor Declares Non-Existent Type `LocalPhase`

**File:** `section-4.md` §4.4.2
**Finding:** The Stage 1 accessor declaration read:
```
LocalPhase  PositioningAI.GetPhase(TeamId team);
```
There is no `LocalPhase` type defined anywhere in the spec set. The canonical phase enum
from #12 is `Phase` (values: `IN_POSSESSION`, `OUT_OF_POSSESSION`, `TRANSITION`). The
`Local` prefix appears to be a copy-paste artifact.

**Resolution (v0.2):** Changed `LocalPhase` → `Phase`.

---

### AR-S1-L4 — Appendix A.9 Typo: "restart restart pass"

**File:** `appendices.md` Appendix A.9
**Finding:** Prose in Appendix A.9 read "the time for a restart restart pass" — double
occurrence of "restart".

**Resolution (v0.2):** Corrected to "the time for a restart pass".

---

## Files Modified by PASS-1 Fix Pass

| File | Severity Fixes | Version |
|------|---------------|---------|
| `section-1.md` | M7 | v0.1 → v0.2 |
| `section-2.md` | H2, H4, M2, M3, M6 (partial) | v0.1 → v0.2 |
| `section-3.md` | H1, H3, H5, H6, M4, M5 | v0.1 → v0.2 |
| `section-4.md` | L3, M7 | v0.1 → v0.2 |
| `section-5.md` | L1 | v0.1 → v0.2 |
| `section-6.md` | M1, M7 | v0.1 → v0.2 |
| `section-8.md` | M6, M7 | v0.1 → v0.2 |
| `section-9-approval-checklist.md` | M1 (×2), L2 | v0.1 → v0.2 |
| `appendices.md` | L4 | v0.1 → v0.2 |

Files NOT modified (no findings): `section-7.md`

---

## PASS 2 Findings (identified during post-fix review of v0.2)

### PASS-2-H1 — §3.13 Missing `overriddenThisTick` Reset at Tick Start

**File:** `section-3.md` §3.13
**Finding:** The `overriddenThisTick` field in `MarkAssignment` is declared in §2.2.2 as a
"per-tick transient; reset to false at tick start." But the §3.13 main-loop pseudocode had
no step that cleared this flag before Step 4 (emergency override). Since `assignments` is
a retained `Span<MarkAssignment>` buffer (zero-allocation architecture per FR-DA-006), the
`overriddenThisTick = true` values set on tick T-1 would persist into tick T. This would
cause Step 5 (regular assignment loop) to skip agents that were emergency-overridden on
T-1 but not on T, retaining their stale T-1 emergency assignments instead of computing
fresh ones.

**Impact:** Critical logic defect — emergency assignment state bleeds across tick boundaries,
causing systematic under-assignment for any team that transitions out of emergency state.

**Resolution (v0.3):** Added Step 3b to §3.13 main loop:
```
for i in 0..holdShapePool.count:
    assignments[i].overriddenThisTick = false
```
Inserted after Step 3 (pool build) and before Step 4 (emergency override). The per-iteration
cost is O(N_HOLD) = O(10) scalar writes — negligible in the §6.2 budget.

---

### PASS-2-M1 — §3.10.3 Invariant 1 Argmin Over Empty Set (Post-H5-Fix Edge Case)

**File:** `section-3.md` §3.10.3
**Finding:** After the H5 fix (adding `AND NOT overriddenThisTick` to the Invariant 1 argmin
filter), a new edge case emerged: if all non-ZONAL DEFENSE-line agents have
`overriddenThisTick = true` (both last-man agent and GK-cover agent are on the DEFENSE line),
the filter set `{a : DEFENSE AND mode != ZONAL AND targetEntityId != null AND NOT overriddenThisTick}`
is empty. Calling `argmin` over an empty set is undefined behavior (null-ref or silent wrong
result depending on the loop structure). The code would then write `assignments[null] = ...`
or select an arbitrary slot.

**Impact:** Edge-case runtime defect — affects scenarios where emergency protocols consume
all non-ZONAL backline capacity. The F4 post-loop fallback is the correct terminal handler
for this; the algorithm should reach it rather than crashing.

**Resolution (v0.3):** Added `if eligiblePool is empty: break` before the argmin call.
The `break` exits the pass loop, causing the post-loop check to evaluate remaining invariants
and emit F4 if still violated.

---

## Files Modified by PASS-2 Fix Pass

| File | Severity Fixes | Version |
|------|---------------|---------|
| `section-3.md` | PASS-2-H1, PASS-2-M1 | v0.2 → v0.3 |

---

## PASS 3 Findings

### PASS-3-L1 — §1.6 Boundary Matrix Still Lists `Anticipation` After M6 Fix

**File:** `section-1.md` §1.6 (boundary matrix row for #7 Perception System)
**Finding:** The M6 fix removed `Anticipation` from §2.3 inputs table and XC-014-005 in section-8.md,
but the §1.6 boundary matrix row for #7 still read: `attribute lookups ('FirstTouch', 'Tackling',
'Anticipation'), 'isActive'`. Listing `Anticipation` in the boundary matrix implies #14 consumes it
from #7, contradicting the M6 resolution. The PASS-1 fix was not propagated to all sites.

**Resolution (v0.3, section-1.md):** Removed `Anticipation` from §1.6 #7 boundary matrix row.
Retained `FirstTouch` (threat score §3.5) and `Tackling` (declared for future use) with brief
annotations. Section-1.md version bumped v0.2 → v0.3.

---

## Files Modified by PASS-3 Fix Pass

| File | Severity Fixes | Version |
|------|---------------|---------|
| `section-1.md` | PASS-3-L1 | v0.2 → v0.3 |

---

## PASS 4 Verification

PASS 4 adversarial review found no new issues. All findings are resolved:
- 17 PASS-1 findings (6H / 7M / 4L): all resolved in v0.2
- 2 PASS-2 findings (1H / 1M): both resolved in section-3.md v0.3
- 1 PASS-3 finding (1L): resolved in section-1.md v0.3

The spec is clean.

---

## Version History

| Version | Date | Author | Summary |
|---------|------|--------|---------|
| v1 | May 17, 2026 | AI agent | Initial adversarial review document. PASS 1: 17 findings (6H / 7M / 4L). All 17 resolved in v0.2 fix pass applied same day. PASS 2: 2 additional findings (1H / 1M) in section-3.md after v0.2 fix application; both resolved in v0.3. PASS 3: 1 finding (1L) — Anticipation not fully purged from section-1.md §1.6; resolved in section-1.md v0.3. PASS 4: clean — no further findings. Total: 20 findings (8H / 8M / 4L), all resolved. |
