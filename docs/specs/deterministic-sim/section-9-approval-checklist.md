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

**Two-tier approval model (introduced May 6, 2026).** Final approval of #16 was previously gated on four upstream specs (#9, #17, #18, #19) all reaching `IN REVIEW`. Three of those four (#17, #18, #19) are `NOT STARTED` and represent a multi-month wait. To prevent #16 from sitting as dead weight while its dependencies are written, approval is split into two tiers:

### 9.4.1 Tier 1 — Conditional Approval (target: now)

**Scope:** Self-contained spec content (sections §1–§7, §9.1, §9.2, §9.5 #1–#3, §9.5 #5).

**Allows downstream work to proceed:**
- Implementation of `DeterministicRngService` (§3.2, §3.4 constants), `SnapshotPayload` writer (§3.9.2), and the §3.6.1 phase ownership pipeline against the spec text as it stands today.
- Test-harness development against §5 traceability blocks.
- The two RFC-derived golden-vector files (§9.5 #4 (a) and (b)) — see §9.5 v1.1 footnote.

**Stub-contract artifacts (`TBD-NORMATIVE`):** While #17, #18, #19 are unwritten, the cross-spec citations in §8.3.1 are placeholders. They are tagged `TBD-NORMATIVE` to mark that:
- The interface contract on #16's side is fixed and will not change.
- The downstream spec (when written) MUST conform to the contract as stated in #16, OR if a conflict is found during the downstream spec's drafting, #16 is reopened for a coordinated revision.
- A `TBD-NORMATIVE` row in §8.3.1 carries the same enforceability as an approved cross-reference within #16, but its row in §8.3.1 is suffixed `[TBD-NORMATIVE: pending #N]` until the upstream spec lands.

**Tier 1 status:** `CONDITIONAL APPROVAL — IN REVIEW` — **lead-developer sign-off granted May 14, 2026.** Status flipped `IN PROGRESS → IN REVIEW` in `SPEC_INDEX.md` row 16 atomically with this entry. Upstream-spec gate for Tier 2 (#9 / #17 / #18 / #19 all at `IN REVIEW` or beyond) is also cleared as of May 14, 2026, but the remaining Tier 2 items in §9.4.2 are not — see §9.4.2 for the outstanding work.

### 9.4.2 Tier 2 — Final Approval (target: when #9, #17, #18, #19 reach `IN REVIEW`)

**Gating items not satisfied by Tier 1:**
- §9.5 #4 (c) — `SerializeCanonical` reference corpus (`serialize-canonical-corpus.md`); requires §3.2.4.1 finalization.
- §8.3.1 cross-spec audit rows for #9, #17, #18, #19 — re-audit each row against the actual upstream spec text and remove the `TBD-NORMATIVE` suffix.
- §9.5 #4 (a) and (b) golden-vector files committed and CI-runnable against the implementation.

**Tier 2 status:** `IN PROGRESS — upstream-spec gate cleared May 14, 2026; remaining gating items open.` Upstream-spec gate (§9.4.3) is satisfied as of May 14, 2026: #9 Fixed64 `IN REVIEW` (May 6), #17 Event System `APPROVED` (May 13, beyond gate), #18 Performance Optimization `IN REVIEW` (May 14), #19 Testing Strategy `IN REVIEW` (May 12). Three Tier 2 deliverables remain open:

1. **§9.5 #4(c) — `serialize-canonical-corpus.md`**: not yet authored. `golden-vectors/` currently contains `hkdf-sha256-kat.md` and `siphash-2-4-kat.md` only. Requires §3.2.4.1 finalization plus a worked corpus of canonical serialization input/output pairs covering the byte-exact encoding contract. Owner: spec author. Estimated authoring window: 1–2 days once §3.2.4.1 is frozen.
2. **§8.3.1 cross-spec audit rows**: all four rows (#9 / #17 / #18 / #19) currently labelled `deferred dependency` against the now-stale May 2, 2026 SPEC_INDEX snapshot. Action: line-by-line re-audit against the actual upstream spec text now that all four are at `IN REVIEW` or beyond, then strip the `[TBD-NORMATIVE: pending #N]` suffix from each row. The #17 row also resolves ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation in §3.4) atomically.
3. **§9.5 #4(a)/(b)/(c) golden vectors CI-runnable** — **RESOLVED in v1.3 (May 14, 2026).** The original v0.9 phrasing required each KAT file to be "executed against the `DeterministicRngService` implementation … MUST exist and pass," creating a chicken-and-egg dependency: `src/` cannot exist until #16 reaches `APPROVED`, and #16 cannot reach `APPROVED` until #4 is checked. v1.3 (this revision) splits each of (a), (b), (c) into a **spec-level sub-condition** (the only thing required for #16 Tier 2 `APPROVED`) and an **implementation-level sub-condition** (folded into Stage 0 `FR-DS-009-GATE` per §5.5; NOT a blocker on #16). The §9.5 #4 checkbox is now checked when all three spec-level sub-conditions pass; CI execution against `DeterministicRngService` is tracked under the §5.5 certification gate that runs post-`src/`-existing. **Residual Tier 2 work for #4 after the split (status as of May 14, 2026 hand-verification pass):**
   - **(a) HKDF-SHA256 KAT — spec-level sub-condition SATISFIED** (`hkdf-sha256-kat.md` v1.1). Hand-verification filed finding **F-HKDF-01**: Test Case 1 OKM had a stray nibble inserted between bytes 34–35 (85 hex chars vs canonical 84); corrected to RFC 5869 §A.1 reference value in v1.1. Test Cases 2 and 3 OKM, all three PRK values, and all metadata (IKM/salt/info/L) verified byte-exact against RFC 5869 §A.1–A.3 with no further findings.
   - **(b) SipHash-2-4 KAT — spec-level sub-condition SATISFIED** (`siphash-2-4-kat.md` v1.1). All 64 output rows match Aumasson & Bernstein 2012 Appendix A / `veorq/SipHash` `vectors.h` byte-for-byte. Metadata (key, `k0`/`k1` derivation, c=2/d=4, little-endian output, increasing-length input) all correct. No findings; no content changes required.
   - **(c) `SerializeCanonical` corpus — spec-level sub-condition BLOCKED** on §3.2.4.1 finalization (subsumed by item 1 above).

Additionally, §9.3 review-checklist boxes remain unchecked: open-issues log audit, QA-automation sign-off, platform-certification owner sign-off, and the cross-spec audit-rows-complete bullet (subsumed by item 2 above).

### 9.4.3 Sequencing constraint (unchanged)

Cross-spec audit rows in §8.3.1 are deferred dependencies on specs #9, #17, #18, #19. Tier 2 final approval is blocked until those specs reach at least `IN REVIEW`. Tier 1 is **not** blocked by this — see §9.4.1.

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
4. The §3.4 `RNG_KDF` / `RNG_STREAM_HASH` constants and the §3.2.4.1 `SerializeCanonical` contract have been verified correct against the named artifacts below. **Spec-level vs implementation-level split (introduced v1.3, May 14, 2026).** The §9.5 #4 checkbox is the *implementation-readiness* gate — its purpose is to certify the spec is ready FOR implementation, not to wait for implementation to land. The original v0.9 wording ("executed against the `DeterministicRngService` implementation … MUST exist and pass") created a chicken-and-egg dependency: `src/` cannot exist until #16 reaches `APPROVED`, and #16 cannot reach `APPROVED` until #4 is checked. The revised criterion below splits each artifact into a **spec-level sub-condition** (gates #16 Tier 2 `APPROVED`) and an **implementation-level sub-condition** (folds into Stage 0 `FR-DS-009-GATE` per §5.5; NOT a blocker on #16 approval). The §9.5 #4 checkbox here is checked when **all three spec-level sub-conditions** pass; the implementation-level sub-conditions are tracked separately under §5.5 certification.

   **(a) RFC 5869 §A.1–A.3 HKDF-SHA256 KAT vectors** (`docs/specs/deterministic-sim/golden-vectors/hkdf-sha256-kat.md`):
   - *Spec-level (Tier 2 gate):* file exists; all three test cases (A.1, A.2, A.3) reproduced verbatim from RFC 5869 Appendix A; `IKM`, `salt`, `info`, `L`, `PRK`, `OKM` field values byte-exact against the RFC source as verified by hand by spec author + lead-developer reviewer; §16 §3.2.4 pinned-`info`/`salt=NULL` interpretation documented at the head of the file (Pass 5 H-2 anchor preserved).
   - *Implementation-level (Stage 0 `FR-DS-009-GATE`):* KAT file wired into `Sim.Tests.Determinism.Rng.HkdfSha256KatTests`; `DeterministicRngService.HkdfExtract`+`HkdfExpand` produce bit-exact `OKM` for all three cases under CI; single mismatch is hard failure of `FR-DS-009-GATE`.

   **(b) SipHash-2-4 reference vectors from Aumasson & Bernstein 2012 Appendix A** (`docs/specs/deterministic-sim/golden-vectors/siphash-2-4-kat.md`):
   - *Spec-level (Tier 2 gate):* file exists; the 64 test cases from the reference paper Appendix A reproduced verbatim; fixed 16-byte key (`00 01 02 ... 0f`) and increasing-length input convention documented; §16 §3.4 `RNG_STREAM_HASH` round-count (`c=2`, `d=4`) annotated alongside the cited authority; byte-order and packing conventions (little-endian per the paper) called out explicitly so an implementer cannot accidentally use a big-endian variant.
   - *Implementation-level (Stage 0 `FR-DS-009-GATE`):* KAT file wired into `Sim.Tests.Determinism.Rng.SipHash24KatTests`; `DeterministicRngService.SipHash24` produces bit-exact tag for all 64 cases under CI.

   **(c) `SerializeCanonical` reference corpus** (`docs/specs/deterministic-sim/golden-vectors/serialize-canonical-corpus.md`):
   - *Spec-level (Tier 2 gate):* file exists; corpus enumerates ≥1 worked input/output pair for every type kind enumerated in §3.2.4.1 (primitives, fixed-width integers, floats with NaN normalization, arrays, dictionaries with sort-key rule, optionals, discriminated unions); each pair lists the input as a structured-literal expression and the output as a lowercase-hex byte string; encoding rules cited inline (`tag-byte → length-prefix → payload` per §3.2.4.1, sort-keys-ascending-bytewise per §3.2.4.1 step 4, etc.); review by spec author + lead developer confirms the corpus is consistent with §3.2.4.1 prose without ambiguity (no input maps to two distinct outputs).
   - *Implementation-level (Stage 0 `FR-DS-009-GATE`):* corpus wired into `Sim.Tests.Determinism.Serialization.SerializeCanonicalCorpusTests`; `Serialization.SerializeCanonical(input)` produces bit-exact `output` for every entry under CI.

   **Rationale for the split.** All three artifacts can be hand-verified at the spec level today: (a) and (b) against the published RFC/paper byte strings (no implementation needed — a reviewer reads the spec text plus the RFC and confirms the spec correctly cites the algorithm), (c) by structural review against §3.2.4.1 (no implementation needed — the canonical encoding rules are deterministic enough that a reviewer can apply them to a worked input and verify the output). Implementation correctness (CI execution against `DeterministicRngService` and `SerializeCanonical`) is a Stage 0 deliverable certified via §5.5 `FR-DS-009-GATE`, which itself runs after #16 is `APPROVED` and `src/` exists. Separating the two prevents #16 from being held hostage by an artifact that cannot exist until #16 is approved.
5. The tolerance matrix placeholder in §3.4 (Tier B default comparator) has a named owner team and a review-date field entry.

## 9.6 Version History
- **v1.4 (May 14, 2026, later same day):** §9.5 #4(a) and #4(b) **spec-level sub-conditions marked SATISFIED** after byte-exact hand-verification pass against RFC 5869 §A.1–A.3 (`hkdf-sha256-kat.md` v1.1) and Aumasson & Bernstein 2012 Appendix A / `veorq/SipHash` `vectors.h` (`siphash-2-4-kat.md` v1.1). Finding **F-HKDF-01** filed and fixed in the same pass: Test Case 1 OKM had a stray nibble (85 vs canonical 84 hex chars); corrected. SipHash file: no findings, all 64 vectors plus metadata correct on first review. §9.4.2 item 3 updated with per-sub-condition status. §9.5 #4 checkbox itself remains unchecked because §9.5 #4(c) is still blocked on §3.2.4.1 finalization. No normative-content change in section files beyond the Test Case 1 OKM correction.
- **v1.3 (May 14, 2026):** §9.5 #4 acceptance criterion revised to break the chicken-and-egg dependency flagged in v1.2 §9.4.2 item 3. Each of (a) RFC 5869 HKDF-SHA256 KAT, (b) SipHash-2-4 reference vectors, (c) `SerializeCanonical` reference corpus is now split into a *spec-level sub-condition* (gates #16 Tier 2 `APPROVED`; hand-verifiable today against the cited RFC / paper / §3.2.4.1 prose without needing `src/`) and an *implementation-level sub-condition* (folds into Stage 0 `FR-DS-009-GATE` per §5.5; runs post-`src/`-existing; NOT a blocker on #16). The §9.5 #4 checkbox now ticks when the three spec-level sub-conditions pass. Rationale: the §9.5 gate is implementation-*readiness*, not implementation-*landed*; the original v0.9 wording inverted this. §9.4.2 item 3 reclassified `outstanding → resolved`. No change to `RNG_KDF` / `RNG_STREAM_HASH` constants, no change to the §3.4 algorithm pinning, no change to the cited authorities; this is purely a criterion-scoping fix.
- **v1.2 (May 14, 2026):** Lead-developer Tier 1 sign-off recorded in §9.4.1. SPEC_INDEX.md row 16 flipped `IN PROGRESS → IN REVIEW` atomically with this entry. §9.4.2 expanded with a per-item audit of the three outstanding Tier 2 deliverables: (1) `serialize-canonical-corpus.md` authoring task scoped; (2) §8.3.1 audit-row re-audit task scoped (notes the atomic ERR-017-001 closure); (3) §9.5 #4(a)/(b) CI-runnability chicken-and-egg dependency on `src/` flagged as a candidate §9 v1.2 acceptance-criterion revision (the recommended split: hand-verify-now / CI-integrate-at-Stage-0). Upstream-spec gate for Tier 2 (per §9.4.3) recorded as cleared: #9 IN REVIEW (May 6), #17 APPROVED (May 13), #18 IN REVIEW (May 14), #19 IN REVIEW (May 12). No formula or normative-content change.
- **v1.1 (May 6, 2026):** Two-tier approval model introduced in §9.4 to unblock implementation work without waiting for the multi-month upstream-spec gate. Tier 1 (Conditional Approval) covers self-contained spec content; Tier 2 (Final Approval) waits for #9 / #17 / #18 / #19 to reach `IN REVIEW`. `TBD-NORMATIVE` tag introduced for §8.3.1 placeholder cross-spec citation rows. §9.5 #4 split into (a)/(b) RFC-derived KAT files (authorable now, not gated on Tier 2) and (c) `SerializeCanonical` reference corpus (Tier 2 gated).
- **v1.0 (May 4, 2026):** Pass 6 verification annotations added to §9.5 #1 and §9.5 #2 (both criteria now mechanically satisfied at the spec level; §5.2 traceability block + T-DS-FAULT-010..014 binding added in section-5.md v1.0). #3 (Tick Orchestrator implementation-owner sign-off) and #4 (three golden-vector files) and #5 (tolerance-matrix owner team + review-date) remain unchecked — those are external artifacts.
- **v0.9 (May 3, 2026):** Third-pass critique L-L resolution. §9.5 acceptance criterion #4 now names three concrete verification artifacts (RFC 5869 HKDF-SHA256 KAT vectors, SipHash-2-4 reference vectors, `SerializeCanonical` reference corpus), each with its required path under `golden-vectors/`. The criterion is now falsifiable.
- **v0.8 (May 2, 2026):** §9.2 cross-reference checkbox checked (FR-DET- migration verified). §9.5 measurable acceptance criteria added for the implementation-readiness gate (D-20).
