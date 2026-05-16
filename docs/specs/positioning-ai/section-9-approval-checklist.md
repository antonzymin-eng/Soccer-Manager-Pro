# Positioning AI Specification #12 — Section 9: Approval Checklist

**Created:** May 15, 2026
**Last Updated:** May 16, 2026 (v0.2 — PASS-1 adversarial fix pass landed)
**Version:** 0.2
**Status:** DRAFT — PASS-1 adversarial review applied; pending `[EST]` → `[GT]` promotion (Appendix A derivations) before lead-developer sign-off.

---

## 9.1 Self-Contained Spec Content

- [x] All v1.0 outline-review findings (13) resolved (cross-referenced in §9.4).
- [x] All v1.1 self-adversarial findings (13) resolved (cross-referenced in §9.4).
- [x] All v1.2 Outstanding Outline-Phase Questions (4) resolved.
- [x] All 47 active FRs cross-referenced to KDs or §-references (FR-PA-034 deleted; 48 IDs total).
- [x] All constants tagged per KD-12 (`[EST]` for outline-stage placeholders flagged for §6.1 promotion).
- [ ] All cross-spec citations grep-verified at draft time (deferred to §8.5 re-run before lead-developer sign-off).
- [x] Every formula in §3 has a worked example (FR-PA-041).
- [x] Every failure mode F1–F6 has a unit test referenced (FR-PA-042..047).

## 9.2 Cross-Spec Sign-Offs Required

- [ ] **#16 lead-developer ratification** of `DOMAIN_TAG_POSITIONING_AI = 0x16` allocation via `ERR-012-001` Phase B/C block (`0x16…0x1B` covering #10/#11/#12/#13/#14/#15). Until ratified, the value is `[CROSS-PENDING]`.
- [ ] **#8 owner acknowledgement** of the producer-side write contract for `TacticalContext.FormationSlot` populated via direct field write at #8 Step 2 (KD-3 boundary; AR-S1-04 — `Stage0Default()` remains match-init-only).
- [ ] **#8 owner one-line patch** to fix `ERR-012-002` (stale "Spec #14" reference at `decision-tree/section-3-1.md` L716).

## 9.3 KD-Sequencing Preconditions

| ID | Precondition | Status |
|---|---|---|
| (a) | `ERR-012-001` resolved (`DOMAIN_TAG_POSITIONING_AI` allocated) | OPEN |
| (b) | All `[CROSS-PENDING]` tags promoted to `[CROSS]` | OPEN — depends on (a) |
| (c) | Hysteresis `[EST]` constants promoted to `[GT]` with derivation entries in Appendix A (`ANCHOR_DWELL_TICKS`, `LINE_HYSTERESIS_M`, `LINE_DWELL_TICKS`, `LANE_HYSTERESIS_M`, `PHASE_HYSTERESIS_TICKS`, `PHASE_LOOSE_VELOCITY_THRESHOLD`, `OFFSET_RANGE_X_M`, `OFFSET_RANGE_Y_M`) | OPEN |
| (d) | Archetype count confirmed against `master-development-plan.md` §3.2 (Stage 0: 3 families; Stage 1: 10 named variants) | DONE — confirmed in v1.2 outline |
| (e) | Lead-developer R-01..R-05 sign-off pass | OPEN |

## 9.4 Finding-to-Resolution Map

| Review | Finding | Sev | Resolved by |
|---|---|---|---|
| outline.md May-6 | 1. Missing metadata header | H | "METADATA HEADER" in `outline-detailed.md` |
| outline.md May-6 | 2. Section plan deviates from CLAUDE.md template | H | §1–§9 mapping |
| outline.md May-6 | 3. Boundary unstated | H | KD-3..KD-6 + Boundary Matrix (§1.6) |
| outline.md May-6 | 4. Authoring scope creep | H | KD-11 + §7 |
| outline.md May-6 | 5. Formation data ownership | H | KD-7 + KD-17 + `PositioningAIConstants.cs` (§4.2) |
| outline.md May-6 | 6. No determinism plan | M | KD-9 + §3.9 + §4.6 |
| outline.md May-6 | 7. Coordinate convention | M | KD-1 + §1.7 |
| outline.md May-6 | 8. Hysteresis missing | M | KD-8 + §3.8 |
| outline.md May-6 | 9. Tick-rate split | M | KD-2 + §1.7 |
| outline.md May-6 | 10. Constant-tag policy | M | KD-12 + §6.1 |
| outline.md May-6 | 11. Fatigue interaction | L | KD-1 + §3.5 |
| outline.md May-6 | 12. Event production | L | KD-10 + §4.3 + §4.4/4.5 |
| outline.md May-6 | 13. Test-pyramid hint | L | §5.1 |
| AR-V1 | 01. `DOMAIN_TAG_POSITIONING_AI` unilateral allocation | H | KD-9 demoted to `_TBD_` + `ERR-012-001` |
| AR-V1 | 02. #8 boundary mis-stated | H | KD-3 rewritten against #8 §3.1.7/§3.2.6 |
| AR-V1 | 03. Compositor ordering by fiat | H | KD-13 + §3.7 simplified (no Stage 0 overrides) |
| AR-V1 | 04. EntityId tie-break unfair | H | KD-14 cost-based + §3.6.3 |
| AR-V1 | 05. §6.3 placeholder budget | H | KD-15 named reference host (§6.3) |
| AR-V1 | 06. Placeholder FR-PA-019..045 | M | §2.1 fully enumerated (47 active FRs, ID 034 deleted) |
| AR-V1 | 07. Archetype count unsourced | M | KD-7 reduced to 3 + planning-doc grep confirmed in v1.2 |
| AR-V1 | 08. Hysteresis `[GT]` premature | M | KD-12 + demoted to `[EST]` (§6.1) |
| AR-V1 | 09. Event-tick edge semantics | M | KD-10 (no events at Stage 0) + FR-PA-045 |
| AR-V1 | 10. Float-comparison hazard | M | KD-16 + FR-PA-015 + §3.6.1 |
| AR-V1 | 11. Tactical-intensity producer | L | KD-11 + FR-PA-032 (per-archetype default) |
| AR-V1 | 12. Two-catalogue file split | L | KD-17 + #20 FR-CS-025 binding (§4.2) |
| AR-V1 | 13. Phase enum unsourced | L | KD-10 (phase is local, not cross-spec) + §3.0 |
| v1.2 Q1 | Archetype count | — | KD-7 + FR-PA-007 + §7.6 enumeration |
| v1.2 Q2 | `DOMAIN_TAG_POSITIONING_AI` value | — | KD-9 proposed block + `ERR-012-001` filed |
| v1.2 Q3 | `TacticalContext` schema | — | KD-2 + KD-3 corrected (per-agent struct, single `Vector2 FormationSlot`, schema FROZEN); §2.2.3 |
| v1.2 Q4 | `StableHash` field | — | DROPPED; FR-PA-034 deleted |
| AR-S1 (section-files) | 01. §3.5 compactness formula inverted | H | §3.5.2 rewritten `rel *= base/compactness`; §3.5.3 worked example replayed |
| AR-S1 | 02. §3.3.1 line-partition indices off-by-one | H | Per-archetype `lineCutIndices` in Appendix B + AM `defaultLine` override for 4-2-3-1 |
| AR-S1 | 03. §3.7 step order yields stale line/lane | H | §3.7 reordered: line/lane committed AFTER spacing+clamp (step 6); §3.11 pseudocode updated |
| AR-S1 | 04. §4.4.3 misuses `Stage0Default()` | H | §4.4.3 rewritten: per-tick direct field write; FR-PA-002 updated |
| AR-S1 | 05. §3.11 contradicts §3.5.2 | H | §3.5.2 now operates on `(baseSlot − centroid)`; aligns with §3.11 |
| AR-S1 | 06. Single-pass spacing non-convergent | H | `SPACING_MAX_PASSES = 4` iteration with `POSITIONING_SPACING_NONCONVERGENT` fallback |
| AR-S1 | 07. NaN sentinel collides with F3 | H | `SENTINEL_NO_SLOT = Vector2.NegativeInfinity` distinct from NaN; §3.11 isActive filter before NaN guard |
| AR-S1 | 08. `FATIGUE_LATERAL_RELAX_M` unused | M | Removed from §6.1 catalogue; §1.7 citation updated |
| AR-S1 | 09. §3.0.4 phase-hysteresis off-by-one | M | §3.0.3 specifies commit on Nth candidate tick; §3.0.4 worked example replayed |
| AR-S1 | 10. §3.2.2 "8 m" doesn't match formula | M | Corrected to 7.2 m maximum |
| AR-S1 | 11. GK constants `[GT]` ahead of #11 | M | Demoted to `[EST]` with #11-binding caveat |
| AR-S1 | 12. Lane bins float-determinism risk | M | `LANE_EDGES_M[6]` literal array; explicit boundary semantics |
| AR-S1 | 13. Centroid undefined | M | §3.5.0 added: mean of `isActive` outfield positions, GK excluded |
| AR-S1 | 14. §3.6.4 missing post-displacement lane state | M | Worked example now records pre/post-displacement lane |
| AR-S1 | 15. T-U-060 misses inversion | M | T-U-063 directional invariant added |
| AR-S1 | 16. §7.8 Stage 0+1 self-contradiction | M | `SHAPE_TRANSITION` retagged Stage 1+ |
| AR-S1 | 17. Glossary "six steps" mismatch | L | Glossary updated to "seven sequential steps" |
| AR-S1 | 18. `XC-012-NNN` not in error log | L | `ERR-012-003` filed in `spec-error-log.md` as documentary anchor |
| AR-S1 | 19. FR-PA-034 DELETED footnote | L | Original wording retained in §2.1 row |
| AR-S1 | 20. §6.3 Editor + Release contradiction | L | Disambiguated to "Editor playmode profiler" with Player caveat |
| AR-S1 | 21. `SPEC_INDEX.md` status mismatch | L | Row 12 flipped `NOT STARTED → IN REVIEW`; CLAUDE.md OPEN ISSUES entry added |

## 9.5 Lead-Developer Sign-Off Lines

- **R-01** Boundary discipline (KD-3..KD-6, KD-11) — verified that no Stage 0 interface is produced against #13/#14/#15.
  Signed: ___________________________ Date: ___________
- **R-02** Determinism binding (KD-9, §3.9, §4.6) — `DOMAIN_TAG_POSITIONING_AI` allocation acknowledged; digest scope confirmed against #16 §6.2.
  Signed: ___________________________ Date: ___________
- **R-03** Constant-tag discipline (KD-12, §6.1) — every constant carries a valid tag; `[EST]` values have an Appendix A derivation entry.
  Signed: ___________________________ Date: ___________
- **R-04** Performance budget (KD-15, §6.3) — reference-host caveat acknowledged; #18 §3.7 `[HotPathAllocExempt]` binding confirmed.
  Signed: ___________________________ Date: ___________
- **R-05** Cross-spec citation grep (§8.5) — re-run completed; no stale spec numbers in body text.
  Signed: ___________________________ Date: ___________

## 9.6 Decision

- Status: `IN REVIEW` (section files v0.2; PASS-1 adversarial review applied — 7 H / 9 M / 5 L all resolved per §9.4 map).
- Next gate: lead-developer sign-off on R-01..R-05.
- Final gate: `APPROVED` after R-01..R-05 sign-off and §9.3 preconditions all DONE (chiefly: `ERR-012-001` ratification and Appendix A derivation entries for `[EST]` constants).

## 9.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass landed across §1/§2/§3/§4/§5/§6/§7/§8/appendices. All 21 AR-S1 findings (7 H / 9 M / 5 L) resolved per §9.4 map. Status flipped DRAFT → IN REVIEW. |
