# Adversarial Review — `outline-detailed.md` v1.0

**Created:** May 15, 2026
**Reviewer:** AI agent (claude/positioning-ai-specs-50o0D), self-adversarial pass.
**Scope:** `outline-detailed.md` v1.0 measured against CLAUDE.md, SPEC_INDEX.md, and adjacent approved specs (#1, #2, #7, #8, #16, #17, #18, #20).
**Severity legend:** **H** = blocks section-file authoring; **M** = must resolve during draft; **L** = follow-up.
**Resolution:** All 13 findings addressed in v1.1 same day. See `outline-detailed.md` §9.4 Finding-to-Resolution Map.

---

## Verified premises
- `SPEC_INDEX.md` row 12: NOT STARTED. Upstream #1/#2/#7/#8/#16/#17/#20 all APPROVED.
- #16 reached `APPROVED` May 14; patching its §3.4 is a real cost.
- CLAUDE.md "Interface Design Principle": never write interfaces against unspecified consumers — ERR-001/ERR-004 trap.

---

## Findings

### AR-V1-01 [H] KD-9 unilateral domain-tag allocation
§3.9 and "Next Steps #1" presume #12 may pick `DOMAIN_TAG_POSITIONING_AI = 0x16` and patch #16 §3.4. (1) Value collision risk — other unstarted Phase B/C specs may legitimately need the next slot. (2) Authority — #16 is APPROVED; patch revisions need lead-developer sign-off, not an outline declaration.
**Resolution v1.1:** demote to `_TBD_`; file `ERR-012-001` requesting a Phase B/C block-allocation policy.

### AR-V1-02 [H] KD-3 #8 boundary mis-stated
v1.0 claims "#8 selects action for the on-ball agent; #12 selects positional target for off-ball agents". Wrong: #8 evaluates `MOVE_TO_POSITION` (one of 7 actions) for every off-ball agent per tick, sourcing `TargetPosition` from `TacticalContext.GetFormationSlot(AgentId)` per `decision-tree/section-3-1.md` L702, L707–725. #12 is the upstream PRODUCER of that slot, not a competitor.
**Resolution v1.1:** KD-3 rewritten. #12 is the Stage 0 Formation Engine; #8 §3.1.7 explicitly anticipates this ("Stage 1 wires the Formation Engine"). #12 → #8 via `TacticalContext.FormationSlot[]` write before #8's per-agent loop.

### AR-V1-03 [H] Compositor §3.7 ordering by fiat
v1.0 picks press-first → run-second by fiat; no rationale, no worked example. Locks #13 and #15 into asymmetric authority.
**Resolution v1.1:** KD-13 added. Stage 0 has no #13/#15 overrides (per Interface Design Principle); the compositor §3.7 simplifies to a 7-step within-#12 pipeline. Stage 1+ §7.x declares a conflict-policy TABLE (not a fixed order) when downstream specs reach `IN REVIEW`.

### AR-V1-04 [H] EntityId tie-break unfair under §3.6 spacing
"Shift later-EntityId agent first" systematically penalises the same shirt numbers across a 90-min match. Deterministic but not tactically correct. Conflates "deterministic iteration" (#16 §3.2.5 requirement) with "deterministic outcome assignment" (which #16 does not require).
**Resolution v1.1:** KD-14 added. Spacing displacement is cost-based (smaller required move wins); EntityId is the terminal tie-break only when costs are equal within `SPACING_EPSILON_M2`.

### AR-V1-05 [H] §6.3 budget pinned against unactivatable gate
Per-tick budget cites `certification-platform.md` which the CLAUDE.md May 6 OPEN ISSUE confirms is unactivatable on Stage 0. Drafting against placeholder is the `_TBD_` rot #18 / #19 spent weeks resolving.
**Resolution v1.1:** KD-15 added. Stage 0 budget pinned against a NAMED reference host (Ryzen 7 5800X @ 4.5 GHz, single thread, Mono, Unity 2022.3 LTS) with explicit cert-host-supersedes caveat.

### AR-V1-06 [M] Placeholder FRs FR-PA-019..045
A "detailed outline" that resolves H-1 ("missing metadata") shouldn't itself ship with 27 placeholder FRs.
**Resolution v1.1:** §2.1 fully enumerated — 48 FRs with source citations.

### AR-V1-07 [M] "Six archetypes" unsourced
Number 6 (4-3-3, 4-2-3-1, 4-4-2, 3-5-2, 3-4-3, 5-3-2) asserted without grep against `docs/planning/`.
**Resolution v1.1:** KD-7 reduced to 3 archetypes (4-4-2, 4-3-3, 4-2-3-1 — most common modern shapes); additional shapes deferred to §7.6. Planning-doc grep listed as Outstanding Question #1.

### AR-V1-08 [M] Hysteresis constants `[GT]` without derivation
v1.0 ships `ANCHOR_DWELL_TICKS=5`, `LINE_HYSTERESIS_M=3.0`, `LANE_HYSTERESIS_M=2.0` as `[GT]` with no justification. CLAUDE.md requires worked-example justification for tunable values.
**Resolution v1.1:** All hysteresis constants demoted to `[EST]` (KD-12). Promotion to `[GT]` requires Appendix A derivation entries at section-file draft time.

### AR-V1-09 [M] Event-tick edge semantics unspecified
v1.0 lists `ACTION_INTENT` consumption (event-stream) on a 10 Hz tick (tick-aligned). Mid-tick event arrival semantics declared as a "failure mode" rather than normal-path semantics.
**Resolution v1.1:** KD-10 rewritten — no event channels at Stage 0; phase computed locally (§3.0). FR-PA-045 covers mid-tick input changes as a deliberate deferred-to-next-tick policy.

### AR-V1-10 [M] Float-determinism hazard at hard-spacing boundary
v1.0 binds determinism to "iteration order + RNG + digest" but never addresses `float` sqrt + comparison at 1.5m boundary — the canonical case where two near-identical configurations could pick different sides across runs.
**Resolution v1.1:** KD-16 added. `SPACING_EPSILON_M2 = 1e-4 m²` on squared-distance comparisons. FR-PA-015.

### AR-V1-11 [L] Tactical-intensity producer unnamed
`[0,1]` input consumed by §3.5 with no producer named at Stage 0 (no coach UI).
**Resolution v1.1:** FR-PA-032 — per-archetype `[GT]` default field in `PositioningAIConstants.cs`.

### AR-V1-12 [L] Two-catalogue file split
v1.0 §4.2 lists `PositioningConstants.cs` + `FormationCatalogue.cs`. Violates #20 §4.2 FR-CS-025 (one `<SpecName>Constants.cs` per spec).
**Resolution v1.1:** KD-17 added. Single `PositioningAIConstants.cs` with `#region Formation Archetypes` per #20.

### AR-V1-13 [L] Phase enum unsourced
Four phase values asserted in §1.4; never tied to a #17 channel or any other source.
**Resolution v1.1:** KD-10 — phase is a LOCAL enum computed in §3.0 from ball + possession state. Not a cross-spec enum at Stage 0.

---

## Cross-cutting concern (closed v1.1)
Four of v1.0's strongest claims (AR-V1-01, AR-V1-02, AR-V1-09 fabricated channel names, AR-V1-13) rested on superficial reads of upstream specs rather than grep-verified citations. v1.1 grep-verified `decision-tree/section-3-1.md` and `event-system/section-3.md` before refactoring. Section-file authoring MUST re-grep at draft time.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 15, 2026 | AI agent (claude/positioning-ai-specs-50o0D) | Initial self-adversarial review of `outline-detailed.md` v1.0. 13 findings (5 H / 5 M / 3 L). All resolved in `outline-detailed.md` v1.1 same day. |
