# Deterministic Simulation Specification #16 — Section 9: Approval Checklist

## 9.1 Content Checklist
- [x] All required sections present
- [x] FR coverage complete
- [x] Determinism tier policy defined
- [x] Snapshot/replay contract defined
- [x] Divergence taxonomy and localization outputs defined
- [x] Operational tables/examples added per section

## 9.2 Quality Checklist
- [x] Normative keywords used consistently
- [x] Cross-references verified against outline terms (v0.7 migrated all FR-DET- / VR-DET- / OPS-DET- prefixes to FR-DS- / VR-DS- / OPS-DS-; §2.0 documents the supersession; verified May 2, 2026)
- [x] Failure behavior explicitly deterministic
- [x] Save/load equivalence protocol documented
- [x] Comparator and tolerance semantics documented

## 9.3 Review Checklist
- [ ] Open issues logged
- [ ] Lead developer sign-off
- [ ] QA automation sign-off
- [ ] Platform certification owner sign-off
- [ ] Cross-spec audit rows marked complete

## 9.4 Decision
- Status: `IN PROGRESS` (matches `SPEC_INDEX.md`; promotion to `IN REVIEW` is gated on completing §9.5 implementation-readiness item below).
- Recommended next gate: detailed implementation-plan review with owning teams and test harness owners.
- Sequencing constraint: cross-spec audit rows in §8.3.1 are deferred dependencies on specs #9, #17, #18, #19 (all currently `NOT STARTED`). Final approval of #16 is blocked until those specs reach at least `IN REVIEW`.

## 9.5 Density and Operational Depth Checklist
The first three items measure **presence** of operational artifacts. The fourth item is the actual implementation-readiness gate; the others alone are insufficient for sign-off.
- [x] Each section contains at least one operational table or matrix
- [x] Each core section includes at least one concrete scenario/example
- [x] Replay/save-load/RNG policies include executable-style artifacts
- [ ] Section-level reviewers confirm implementation readiness depth (gating item)

**Acceptance criteria for the gating item** — this checkbox MAY be checked only after ALL of the following are satisfied:
1. All §4.2.2 lifecycle steps have at least one identified test card in §5.3 or §5.11. *(Pass 6 verification: each of §4.2.2 steps 1–8 maps to T-DS-SAVE-005 / T-DS-FAULT-008..010 / T-DS-ENV-007 / T-DS-FAULT-009 / T-DS-RNG-002 / T-DS-FAULT-010 / T-DS-ORDER-001 respectively; satisfied at v1.0.)*
2. All §3.4 error codes (`ERR_DS_*`) have at least one fault-injection test case in §5.3 or §5.11. *(Pass 6 verification: §5.2 now contains an explicit Error-code → fault-injection traceability block covering `ERR_DS_REPLAY_BOUNDARY` → T-DS-FAULT-010, `ERR_DS_TIERB_NONFINITE` → T-DS-FAULT-011, `ERR_DS_RNG_BUDGET_MISMATCH` → T-DS-FAULT-012, `ERR_DS_STORAGE_ATOMICITY` → T-DS-FAULT-013, `ERR_DS_ENV_MUTATION` → T-DS-FAULT-014. Pre-existing codes are covered by T-DS-FAULT-008/009 and T-DS-ENV-007. Satisfied at v1.0.)*
3. The §3.6.1 phase ownership table has been reviewed and signed off by the implementation owner for the Tick Orchestrator.
4. The §3.4 `RNG_KDF` / `RNG_STREAM_HASH` constants have been verified correct against the following named artifacts: (a) RFC 5869 §A.1–A.3 known-answer test vectors for HKDF-SHA256, executed against the `DeterministicRngService` implementation and committed as `docs/specs/deterministic-sim/golden-vectors/hkdf-sha256-kat.md`; (b) the SipHash-2-4 reference test vectors from Aumasson & Bernstein 2012 Appendix A, committed as `docs/specs/deterministic-sim/golden-vectors/siphash-2-4-kat.md`; (c) the `SerializeCanonical` reference corpus described in §3.2.4.1, committed as `docs/specs/deterministic-sim/golden-vectors/serialize-canonical-corpus.md`. All three artifacts MUST exist and pass before this checkbox can be checked.
5. The tolerance matrix placeholder in §3.4 (Tier B default comparator) has a named owner team and a review-date field entry.

## 9.6 Version History
- **v1.0 (May 4, 2026):** Pass 6 verification annotations added to §9.5 #1 and §9.5 #2 (both criteria now mechanically satisfied at the spec level; §5.2 traceability block + T-DS-FAULT-010..014 binding added in section-5.md v1.0). #3 (Tick Orchestrator implementation-owner sign-off) and #4 (three golden-vector files) and #5 (tolerance-matrix owner team + review-date) remain unchecked — those are external artifacts.
- **v0.9 (May 3, 2026):** Third-pass critique L-L resolution. §9.5 acceptance criterion #4 now names three concrete verification artifacts (RFC 5869 HKDF-SHA256 KAT vectors, SipHash-2-4 reference vectors, `SerializeCanonical` reference corpus), each with its required path under `golden-vectors/`. The criterion is now falsifiable.
- **v0.8 (May 2, 2026):** §9.2 cross-reference checkbox checked (FR-DET- migration verified). §9.5 measurable acceptance criteria added for the implementation-readiness gate (D-20).
