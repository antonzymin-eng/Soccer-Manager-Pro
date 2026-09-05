# Decision Tree Specification #8 — Adversarial Review & Critique

**Date:** 2026-05-03, 13:58 UTC
**Reviewer mode:** adversarial / implementation-risk focused
**Target:** Decision Tree Specification #8 — sections 1, 2, 3.1, 3.3 (sampled across full spec)
**Spec status under review:** APPROVED (draft-level quality gate, signed off 2026-04-27);
explicitly flagged in CLAUDE.md OPEN ISSUES and SPEC_INDEX.md notes as a candidate for
comprehensive audit before implementation.
**Selection rationale:** Decision Tree #8 has no prior adversarial-critique log file in
its spec folder; the only existing adversarial reviews in `docs/specs/` (Deterministic
Sim #16, Fixed64 Math #9) are dated 2026-05-01 / 2026-05-02. Decision Tree #8's last
recorded review is its 2026-04-27 sign-off, more than 5 days old per today's date. Per
the OPEN ISSUES item "Decision Tree (#8) draft-level quality gate," a full programmatic
audit of #8 is an outstanding follow-up.

---

## Executive Summary

The spec is structurally complete and reads well at section level, but a programmatic
cross-section consistency check surfaces **a hard contradiction in the possession
sourcing model**, **multiple struct-shape mismatches between §2.2 (the data-structure
authority) and §3.1 / §3.3 (the algorithm sections that consume those structs)**, and
**an attribute-caching invariant that §3.3 violates**. Several of these are not
documentation polish — they are concrete signals that the spec cannot be implemented as
written without a normative amendment.

Top risks (severity ordered):
1. **C-1 (Critical):** Possession source is contradictory across §1.2/§2.1.2 (says
   `PerceptionSnapshot.HasPossession`) vs §3.1.1 (says `MatchContext.PossessingAgentId`,
   and explicitly states `PerceptionSnapshot` has no such field).
2. **C-2 (Critical):** `MatchContext` struct (§2.2.5) does not declare
   `PossessingAgentId`, `PossessedByTeam`, or `OpponentGoalCentre`, all of which
   §3.1.1 / §3.1.4 / §3.1.5 read as if they exist.
3. **H-1 (High):** `DecisionContext` field naming drift — §2.2.4 names the possession
   flag `HasPossession`; §3.1.1.2 / §3.1.2 / §3.1.3 read it as `AgentHasBall`. These
   are the same field by intent but cannot both be implemented.
4. **H-2 (High):** §3.3.2.1 reads raw `context.AgentState.Composure` and re-normalises,
   directly contradicting §2.2.4 which prescribes a one-time read into `A_Composure`
   in Step 2 ("Attribute reads must not be repeated in Steps 3–5").
5. **H-3 (High):** `DecisionContext.MatchSeed` is referenced in §3.3.2.1 / §3.3.3.1
   but is not declared in §2.2.4. Determinism (FR-04) depends on this field; it must
   be authoritative on the carrier struct.
6. **M-1 (Medium):** §3.3.2.1 references `context.AgentState.AgentId` and
   `context.AgentState`, but §2.2.4 declares `DecisionContext` with flat fields
   (`AgentId` directly on the struct), no nested `AgentState` member. This is a
   second access-path divergence beyond H-1 and H-2.
7. **M-2 (Medium):** §2.2.5 / §2.2.4 do not surface `MatchSeed` anywhere on a struct
   visible to the DT, yet §3.3 requires it. The injection contract is unspecified.
8. **M-3 (Medium):** Section 1 §1.7.1 attribute-dependency table omits `WorkRate`,
   `Positioning`, and `Aggression`, which §3.1's "ATTRIBUTE DEPENDENCY FLAGS" header
   and §2.2.4's `A_*` field list both consume. The §1.7.1 contract is incomplete.
9. **L-1 (Low):** §2.1.2 Step 5 narrative shows a "superseded note" pointing readers
   to §3.3 for the authoritative noise formula. The superseded prose still resides
   inside the body of an APPROVED section. It should be excised, not annotated.
10. **L-2 (Low):** §2.2.4 documents an "implementation constraint" warning on
    `VisibleTeammates[]` requiring NativeArray or ArrayPool, but the constraint is
    conditional on Section 6 verification ("Section 6 must validate the chosen
    pattern"). Section 6 was not pulled into approval scope here; if Section 6
    contradicts the §2.2.4 prescription, §2 cannot have been correctly approved.
11. **L-3 (Low):** §2.2.5's `BallZone` carries an "XC-NOTE" warning that Perception
    §3.4 *might* in a future revision define conflicting zone thresholds. The current
    state is unverified — either confirm clean and remove the note, or escalate to a
    blocking finding.

---

## Methodology

1. Surveyed `docs/specs/decision-tree/` for existing adversarial / audit logs — none
   present. Decision Tree #8 has only a §9 Approval Checklist (draft-level rigor),
   not a programmatic audit comparable to Pass Mechanics #5 or Shot Mechanics #6.
2. Read §1 (full), §2.1 + §2.2 + §2.3 + §2.4 (full), §3.1 (lines 1–540), §3.3 (lines
   1–390 covering the SelectAction algorithm and noise hash). Sampled remaining 3.x
   sections by table-of-contents; deeper coverage deferred to a follow-up pass.
3. Cross-checked struct definitions in §2.2 against every algorithmic field access in
   §3.1 / §3.3.
4. Cross-checked attribute lists in §1.7.1 against §3.1's ATTRIBUTE DEPENDENCY FLAGS
   header and against §2.2.4 `DecisionContext` `A_*` fields.
5. Cross-checked Section 1 design decisions (KD-1 … KD-7) against subsequent sections
   for narrative drift.

---

## Findings

### C-1 — Possession source contradiction (Critical)

**Locations:**
- `section-1.md` §1.5 narrative ("Ball possession is binary and sourced exclusively
  from `PerceptionSnapshot`")
- `section-2-1-to-2-2.md` §2.1.2 Step 3 ("If agent has ball (sourced from
  `PerceptionSnapshot.HasPossession`)")
- `section-2-1-to-2-2.md` §2.2.4 (`DecisionContext.HasPossession` documented as
  "True if this agent is currently in possession of the ball")
- `section-3-1.md` §3.1.1.1 ("The `PerceptionSnapshot` struct … does **not** contain
  a `HasBall` boolean or `PossessingAgentId` field. … Possession state is therefore
  sourced from `MatchContext`.")

**Finding:** §1 and §2 establish `PerceptionSnapshot.HasPossession` as the authoritative
possession source. §3.1.1.1 directly contradicts this and reroutes the source to
`MatchContext.PossessingAgentId`, citing the Perception System struct as evidence.
Both cannot be true. If `PerceptionSnapshot` contains no `HasPossession` field (as
§3.1.1.1 asserts), §2.1.2 Step 3's contract is unimplementable; if it does, §3.1.1.1's
introduction is wrong. This is the single largest implementation hazard in the spec
because it controls option-set branching (§3.1.2) which controls everything downstream.

**Recommended solution:**
1. Resolve which authority owns possession state. The architecture argument in §3.1.1.1
   ("Perception is epistemic; possession is game state") is sound — bind it.
2. Amend §1.5 narrative to cite `MatchContext.PossessingAgentId` as the source.
3. Amend §2.1.2 Step 3 to read "If agent has ball (sourced from
   `MatchContext.PossessingAgentId == AgentId`)".
4. Amend §2.2.4 `DecisionContext` field documentation: `HasPossession` is computed
   in Step 2 from `MatchContext.PossessingAgentId == AgentId`; it is **not** a field
   read from the snapshot.
5. Confirm with Perception System #7 that no `HasPossession` field exists on
   `PerceptionSnapshot`. If one does exist, §3.1.1.1's premise is wrong and the
   problem flips: §3.1 must be rewritten.

**Severity:** Critical — blocks implementation start.

---

### C-2 — `MatchContext` struct missing fields used by algorithm sections (Critical)

**Locations:**
- `section-2-1-to-2-2.md` §2.2.5 — `MatchContext` struct definition (declares only
  `HomeScore`, `AwayScore`, `MatchTimeSeconds`, `Possession`, `Phase`, `BallZone`).
- `section-3-1.md` §3.1.1 reads `MatchContext.PossessingAgentId` and
  `MatchContext.PossessedByTeam` (neither declared in §2.2.5).
- `section-3-1.md` §3.1.4.2 reads `MatchContext.OpponentGoalCentre` (not declared).
- `section-3-1.md` §3.1.5.x and §3.1.7.x further reference `MatchContext` fields
  for goal direction and formation slot derivation.

**Finding:** Algorithmic sections use a richer `MatchContext` than the data-structures
section declares. §2.2.5's `Possession` is an enum (`{HOME_TEAM, AWAY_TEAM, CONTESTED}`),
not a `PossessingAgentId : int`. The two are not equivalent: §3.1.1.2's possession
classification rule (`AgentHasBall = (MatchContext.PossessingAgentId == this.AgentId)`)
cannot be evaluated against an enum. `OpponentGoalCentre` (§3.1.4.2) and
`PassedByTeam` (§3.1.1.2) are not present at all.

**Recommended solution:**
1. Amend §2.2.5 `MatchContext` definition to add:
   - `public readonly int PossessingAgentId;` (−1 = loose)
   - `public readonly TeamSide PossessedByTeam;` (or equivalent)
   - `public readonly Vector2 OpponentGoalCentre;` (per-team-perspective; orchestrator
     populates)
   - `public readonly Vector2 OwnGoalCentre;` (symmetric)
2. If the orchestrator is expected to inject a per-team `MatchContext` view, document
   that explicitly — currently §2.2.5 reads as a single shared struct.
3. Decide whether `PossessionState` enum is redundant given `PossessingAgentId` and
   either remove it or document both with a derivation rule.

**Severity:** Critical — every option-generation gate that references a missing
`MatchContext` field is unimplementable.

---

### H-1 — Field-name drift: `HasPossession` vs `AgentHasBall` (High)

**Locations:**
- `section-2-1-to-2-2.md` §2.2.4 — declares `public bool HasPossession;` on
  `DecisionContext`.
- `section-3-1.md` §3.1.1.2 — assigns and reads
  `DecisionContext.AgentHasBall`.
- `section-3-1.md` §3.1.2, §3.1.3.1, §3.1.6 — gate conditions consistently use
  `AgentHasBall`.

**Finding:** `HasPossession` and `AgentHasBall` are intended to be the same field
but are written as if they are distinct. C# is not tolerant of this; one must give.
The recurrence across §3.1.x suggests the algorithmic section was written against
an earlier struct draft that named the field `AgentHasBall`, and §2 was renamed
later without back-propagation.

**Recommended solution:**
1. Pick the canonical name. `AgentHasBall` is the more readable consumer-side name;
   `HasPossession` is the more concise carrier-side name. Recommend `HasPossession`
   to match the FR-05 acceptance criteria text (§2.3.2 already says "If
   `HasPossession = true`").
2. Replace every occurrence of `AgentHasBall` in §3.1.1 / §3.1.2 / §3.1.3 / §3.1.6
   with `HasPossession`.
3. Add a §2.2.4 field comment that the canonical name is `HasPossession`; cross-link
   from §3.1.1.2 so future readers do not introduce a third variant.

**Severity:** High — silent build break at first compile attempt; symptomatic of a
larger §2-vs-§3 sync gap (see H-2, H-3, M-1).

---

### H-2 — Attribute caching invariant violation in `SelectAction()` (High)

**Locations:**
- `section-2-1-to-2-2.md` §2.1.2 Step 2 ("`PlayerAttributes` are accessed via
  `AgentState` and must be read **exactly once**, here, and cached in
  `DecisionContext` for all downstream steps. … Attribute reads must not be repeated
  in Steps 3–5.")
- `section-2-1-to-2-2.md` §2.2.4 — `DecisionContext.A_Composure` is the cached
  normalised form.
- `section-3-3.md` §3.3.2.1 line 183: `float A_Composure = (context.AgentState.Composure - 1) / 19.0f;`

**Finding:** §3.3.2.1's `SelectAction()` re-reads raw `Composure` from
`AgentState.Composure` and re-normalises inline, instead of reading the
already-cached `DecisionContext.A_Composure`. This violates the §2 contract twice:
first by reading attributes in Step 5, second by reading them through `AgentState`
rather than `DecisionContext`. The contract was introduced specifically to prevent
mid-tick attribute mutation hazards and to enforce the no-omniscience boundary.

**Recommended solution:**
1. Replace §3.3.2.1 line 183 with:
   `float A_Composure = context.A_Composure;`
2. Add §3.3.2.2 invariant `INV-SEL-08: SelectAction() reads no attribute fields not
   already cached in DecisionContext (Step 2)`.
3. Sweep §3.2 and §3.4 for the same pattern. (Spot-checked: §3.1 reads via
   `context.A_Decisions` correctly; §3.3 is the outlier.)
4. Add a §5 unit test that asserts `AgentState.PlayerAttributes` is not accessed
   during Steps 3–5 (mock `AgentState` with throwing accessors).

**Severity:** High — silent semantic divergence; a future Stage 1 attribute change
that updates `AgentState.Composure` mid-tick would cause §3.3 to disagree with §3.1
about the agent's composure within one heartbeat.

---

### H-3 — `DecisionContext.MatchSeed` undeclared but required by determinism (High)

**Locations:**
- `section-2-1-to-2-2.md` §2.2.4 — `DecisionContext` declared without `MatchSeed`.
- `section-3-3.md` §3.3.2.1 reads `context.MatchSeed`; §3.3.3.1 takes
  `ulong matchSeed` as a parameter to `ComputeOptionNoise`.
- `section-1.md` §1.7.1 dependency table lists `matchSeed` as "✅ Established
  (Simulation root)" but does not bind it to a specific struct field.

**Finding:** The deterministic noise hash (FR-04 critical path) depends on
`matchSeed` being available to `SelectAction()`. §3.3 reads it from `DecisionContext`,
but §2.2.4 does not declare the field. The carrier struct for the determinism
contract is unspecified.

**Recommended solution:**
1. Add to §2.2.4: `public ulong MatchSeed;` with comment "Set once per match by
   orchestrator; copied into each agent's DecisionContext at construction time;
   immutable for the match duration."
2. Document in §1.7.1 that `matchSeed` is delivered to the DT via
   `DecisionContext.MatchSeed`, populated by the simulation orchestrator at agent
   construction.
3. Cross-reference Pass Mechanics #5 / Shot Mechanics #6 to confirm they use the
   same propagation path. If they receive `matchSeed` differently, document the
   asymmetry intentionally.

**Severity:** High — FR-04 is unverifiable without knowing where `matchSeed` lives.

---

### M-1 — Access-path divergence: `context.AgentState.X` vs flat `context.X` (Medium)

**Locations:**
- `section-2-1-to-2-2.md` §2.2.4 — `DecisionContext` is a flat struct
  (`AgentId`, `HeartbeatTick`, `A_Composure`, …) with no nested `AgentState` field.
- `section-3-3.md` §3.3.2.1 reads `context.AgentState.Composure` and
  `context.AgentState.AgentId`.

**Finding:** A second symptom of the §2/§3 sync gap (H-1 is the first). §3.3 was
written against a `DecisionContext` shape that wraps `AgentState`; §2 declares a
flattened shape.

**Recommended solution:**
1. Replace §3.3.2.1 access paths:
   - `context.AgentState.AgentId` → `context.AgentId`
   - `context.AgentState.Composure` → (per H-2) → `context.A_Composure` (already
     normalised; remove the `(- 1)/19.0f` step).
2. Grep §3.2 and §3.4 for `AgentState.` and reconcile.

**Severity:** Medium — does not change behaviour but blocks compilation.

---

### M-2 — Orchestrator-to-DT seed/state injection contract is unspecified (Medium)

**Locations:**
- `section-1.md` §1.7.1 lists `matchSeed`, `BallState.Position`, `AgentState.Position`,
  etc., but does not specify the call shape that delivers them.
- §4 "Architecture, file layout, interface contracts" was not pulled in for this
  pass; the §2 narrative implies fields are "injected at DT construction" or
  "updated each tick by orchestrator" but the call signature is not normative.

**Finding:** The DT's lifecycle (constructor signature, per-tick update method,
`ReceiveSnapshot()` companion methods) is not enumerated in a single normative place.
This becomes blocking once H-3 is resolved (the answer dictates whether `MatchSeed`
is constructor-injected or per-tick-injected).

**Recommended solution:**
1. Section 4 should add an explicit "DT public API surface" table:
   `Constructor(matchSeed, agentId, isHomeTeam, formationSlot, …)`,
   `void ReceiveSnapshot(in PerceptionSnapshot)`,
   `void UpdateTick(in MatchContext, in TacticalContext, in AgentState)`,
   etc.
2. Reconcile against Pass Mechanics #5 / Shot Mechanics #6 caller patterns to ensure
   the orchestrator can drive all three uniformly.

**Severity:** Medium — ambiguity, not a contradiction.

---

### M-3 — §1.7.1 attribute dependency list is incomplete vs §2.2.4 / §3.1 (Medium)

**Locations:**
- `section-1.md` §1.7.1 lists: `Decisions`, `Composure`, `Vision`, `Anticipation`,
  `Passing`, `Technique`, `Shooting`, `KickPower`, `WeakFootRating`, `Crossing`.
- `section-2-1-to-2-2.md` §2.2.4 declares additional `A_*` fields:
  `A_Aggression`, `A_WorkRate`, `A_Stamina`, `A_Positioning`, `A_Agility`,
  `A_Pace`, `A_LongShots`, `A_Dribbling`.
- `section-3-1.md` ATTRIBUTE DEPENDENCY FLAGS header explicitly calls out
  `WorkRate`, `Positioning`, and (already-flagged) `Decisions` and `Anticipation`.

**Finding:** §1.7.1 is supposed to be the spec's authoritative list of attribute
dependencies. It is missing at least 8 of the 15 attributes that §2.2.4 declares
the DT will read. Subsequent sections rely on these (`A_LongShots` in §3.1.4,
`A_Aggression`/`A_WorkRate` implicitly via §3.1.7/§3.1.8, etc.). The §3.1 ATTRIBUTE
DEPENDENCY FLAGS header is itself partial.

**Recommended solution:**
1. Make §1.7.1 the single source of truth: add every attribute in §2.2.4's `A_*`
   field block, sourced from `Agent Movement #2 §3.5.6` with verification status
   (PRESENT, TBD-FLAGGED-AS-ERR-011, etc.).
2. Have §3.1's ATTRIBUTE DEPENDENCY FLAGS header reference §1.7.1 rather than
   re-listing.
3. ERR-011 was created (per §3.1) but `spec-error-log.md` is incomplete (per
   CLAUDE.md OPEN ISSUES). Confirm ERR-011 is logged with the full attribute list.

**Severity:** Medium — implementation will discover missing attributes one-by-one,
producing cascade requests on Spec #20 (Code Standards) and Agent Movement #2
amendments.

---

### L-1 — Superseded prose left inside an APPROVED section (Low)

**Locations:**
- `section-2-1-to-2-2.md` §2.1.2 Step 5 lines 200–208: a "Superseded note" warns
  readers that the noise formula in this section is not authoritative; §3.3 owns
  the formula.

**Finding:** Approved sections should not advertise their own staleness. The
warning is the right intent — §3.3 is canonical — but the correct fix is to delete
the §2.1.2 noise formula and replace with one sentence: "The noise injection
formula is defined in §3.3." Leaving the wrong formula and a "but see §3.3" note
invites implementer error.

**Recommended solution:**
1. Excise the §2.1.2 noise pseudocode (lines ≈194–208).
2. Replace with: "Noise term is added per option per the §3.3 algorithm. See §3.3
   for the authoritative formula and its derivation."
3. Bump §2 version (1.1 → 1.2) with a version-history entry citing the supersession.

**Severity:** Low — narrative cleanup; no implementation hazard.

---

### L-2 — `VisibleTeammates[]` allocation pattern deferred to §6, but §6 not in scope here (Low)

**Locations:**
- `section-2-1-to-2-2.md` §2.2.4 implementation-constraint warning on
  `VisibleTeammates[]`.

**Finding:** §2.2.4 prescribes "NativeArray (preferred) or `ArrayPool<>.Shared` (if
GC budget permits)" but qualifies that "Section 6 must validate the chosen pattern
against the 4ms DT budget (FR-12)." Section 6 was not pulled into this review pass.
If §6's analysis disagrees with §2.2.4's pattern (e.g., neither pattern fits the
4ms budget for 22 agents × ≤16 visible peers each), then the spec was approved
with an unresolved blocking constraint.

**Recommended solution:**
1. Open §6, locate the GC / allocation analysis, and confirm it matches §2.2.4's
   prescription.
2. If §6 contradicts or omits this analysis, raise as a follow-up critical finding;
   §2.2.4 must be revised to match.
3. As a follow-up to this critique log, perform a deeper §6 pass.

**Severity:** Low (until §6 is checked; could escalate to High).

---

### L-3 — Unverified XC-NOTE warning on `MatchContext.BallZone` (Low)

**Locations:**
- `section-2-1-to-2-2.md` §2.2.5 `BallZone` field comment: "XC-NOTE: The §9 Approval
  Checklist must verify that Perception System §3.4 contains no conflicting zone
  threshold definitions."

**Finding:** This is a forward-looking conditional warning ("if Perception §3.4
defines zone thresholds in a future revision, these values must be unified"). The
spec text does not record whether the cross-check was performed. CLAUDE.md OPEN
ISSUES notes the renumbering cascade was resolved on 2026-04-26, but no record of
this specific Perception §3.4 ↔ DT §2.2.5 zone-threshold consistency check.

**Recommended solution:**
1. Open `docs/specs/perception-system/section-3-4.md` (or the relevant Perception
   System file) and verify it defines no `FieldZone` thresholds.
2. If clean: replace the XC-NOTE with a verified statement
   ("Verified clean against Perception System v1.7, 2026-05-03. No revision needed.").
3. If conflicting: escalate as critical (zone thresholds appearing in two specs is
   the recurring class-of-bug warned about in CLAUDE.md "Stale Spec Numbers in Old
   Files").

**Severity:** Low — currently informational; verification is cheap.

---

## Findings Outside Scope of This Pass (Carry-Forward)

These are areas not deeply audited here; flagging for a follow-up adversarial pass:

- **§3.2 utility scoring:** all 7 action-type formulas, [GT] constant catalogue
  (`UtilityWeights.cs`), worked numerical examples at attribute extremes.
- **§3.4 dispatch routing:** completeness of `PassRequest` / `ShotRequest`
  population logic; AR-5 (silent population failure) mitigation tests.
- **§3.5 state machine:** IDLE → EVALUATING → EXECUTING → INTERRUPTED transitions;
  KD-5 cross-check that DT does not own multi-frame state.
- **§4 architecture:** namespace boundaries; static-analysis rule SA-01 (WorldState
  ban) implementation pattern; file/class layout.
- **§5 test plan:** UT-09 provisional thresholds (§2.3.3 gate) — confirm §3.3.7
  derived thresholds are correctly back-propagated, or §2 must bump to v1.2.
- **§6 performance budget:** 4ms / 22 agents budget allocation; operation count
  vs measured; resolution of L-2 above.
- **§9 approval checklist:** confirm none of the C-1, C-2, H-1, H-2, H-3 issues
  were claimed verified — fabricated checklist values are explicitly listed in
  CLAUDE.md "Things That Have Gone Wrong Before".

---

## Recommended Remediation Sequence

| Order | Item | Rationale |
|-------|------|-----------|
| 1 | C-1 (possession source authority decision) | Architectural; blocks every other fix |
| 2 | C-2 (`MatchContext` field additions) | Follows directly from C-1 resolution |
| 3 | H-1, H-2, H-3, M-1 (§2/§3 sync sweep) | Single PR; mechanical once authority is set |
| 4 | M-2 (§4 API surface table) | Stabilises the orchestrator contract |
| 5 | M-3 (§1.7.1 attribute list completion + ERR-011) | Spec-error-log rebuild touchpoint |
| 6 | L-1, L-2, L-3 (cleanup pass) | Cosmetic but worth doing in same revision |
| 7 | Carry-forward §3.2/§3.4/§3.5/§4/§5/§6/§9 deep audit | Earns Decision Tree #8 a "Pass Mechanics-grade" approval |

**Spec status implication:** Findings C-1, C-2, H-1, H-2, H-3 collectively make the
current Decision Tree #8 spec **not implementation-ready** despite its APPROVED
status. The "draft-level quality gate" approval recorded in §9 is consistent with
this conclusion — the sign-off note in SPEC_INDEX.md and CLAUDE.md OPEN ISSUES
already flags a comprehensive audit as a follow-up. This review is a partial
discharge of that follow-up.

---

## Reviewer Notes

- All findings above are **programmatically verifiable** by reading the cited
  files and comparing struct definitions to field accesses. No fabricated
  references.
- Cross-spec claims (e.g., "Perception System #7 has no `HasPossession` field")
  were taken from §3.1.1.1's own assertion; an independent re-read of
  `docs/specs/perception-system/` is recommended before locking C-1.
- This is an initial pass over §1, §2.1–§2.4, §3.1 first half, and §3.3 first
  half. A second pass should cover §3.2 / §3.4 / §3.5 / §4 / §5 / §6 / §9.

---

*End of Adversarial Review — Decision Tree Specification #8*
*System XI — Specification #8 of 20 | Stage 0: Physics Foundation*
*Review date: 2026-05-03, 13:58 UTC*
