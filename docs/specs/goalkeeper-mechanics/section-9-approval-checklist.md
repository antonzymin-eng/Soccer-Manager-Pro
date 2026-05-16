# Goalkeeper Mechanics Specification #11 — Section 9: Approval Checklist

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Programmatic quality gate for advancement of
Goalkeeper Mechanics #11 from `IN REVIEW` to `APPROVED`. Every
entry is verifiable against source files; no fabricated values.

---

## 9.1 Constant-Tag Verification (KD-9)

Programmatic check (grep-based against §3.4 master table and
`GoalkeeperConstants.cs` once it exists): every constant carries
exactly one of `{[GT], [EST], [FIXED], [DERIVED], [CROSS],
[CROSS-PENDING]}`.

| Constant category | Tag set | Verified |
|-------------------|---------|----------|
| §3.4.1 GK volume / reach (9 rows) | All `[GT]` | ☐ Pending grep gate |
| §3.4.2 GK timing / hold-rule (7 rows) | `[FIXED]` × 1, `[GT]` × 5, `[DERIVED]` × 1 | ☐ Pending grep gate |
| §3.4.3 Reaction pipeline (11 rows) | `[GT]` × 10, `[CROSS]` × 1 | ☐ Pending grep gate |
| §3.4.4 Dive kinematics (12 rows) | All `[GT]` | ☐ Pending grep gate |
| §3.4.5 Handling quality (17 rows) | All `[GT]` | ☐ Pending grep gate |
| §3.4.6 Rush dispatch (3 rows) | All `[GT]` | ☐ Pending grep gate |
| §3.4.7 Distribution geometry (6 rows) | All `[GT]` | ☐ Pending grep gate |
| §3.4.8 Cross-claim duel (6 rows) | All `[GT]` | ☐ Pending grep gate |
| §3.4.9 Project invariants (8 rows) | `[DERIVED]` × 1, `[CROSS]` × 6, `[CROSS-PENDING]` × 1 | ☐ Pending grep gate |

Total inventory: ≈79 rows across 9 subsections. Inventory
discipline closure verified at §3.4 (every symbol in §3.2–§3.8
pseudocode is a row or an explicitly named per-call output /
local variable).

---

## 9.2 Cross-Spec Reference Verification

Every `XC-011-NNN` (§8.4.1; 15 entries) resolves to a specific
section in the named upstream spec. Verification gate: grep the
target spec for the cited section header.

| ID | Target | Verified |
|----|--------|----------|
| `XC-011-001` | #1 §1.2 | ☐ Pending grep gate |
| `XC-011-002` | #1 §3.1.11.2 | ☐ Pending grep gate |
| `XC-011-003` | #1 §3.1 possession surface | ☐ Pending (OI-006 verification posture; back-prop ERR-011-002 filed if absent) |
| `XC-011-004` | #6 §4.5 | ☐ Pending grep gate |
| `XC-011-005` | #10 KD-7 / §3.7 | ☐ Pending grep gate |
| `XC-011-006` | #12 §3.3.3 | ☐ Pending grep gate |
| `XC-011-007` | #16 §3.4 | ☐ Pending grep gate (gates on ERR-011-001 / `0x17` or `0x1D` allocation) |
| `XC-011-008` | #17 §3.2.1 | ☐ Pending grep gate |
| `XC-011-009` | #5 §3 | ☐ Pending grep gate (anchor pinned during implementation) |
| `XC-011-010` | #3 §3.4.2 | ☐ Pending grep gate |
| `XC-011-011` | #7 §3 | ☐ Pending grep gate (anchor pinned during implementation) |
| `XC-011-012` | #2 §3.5.6 | ☐ Pending grep gate |
| `XC-011-013` | #8 §1.7 | ☐ Pending grep gate (anchor pinned during implementation) |
| `XC-011-014` | #18 Appendix F.0 | ☐ Pending grep gate |
| `XC-011-015` | #18 §6 | ☐ Pending grep gate |

---

## 9.3 Sign-Off Requirements

Sign-offs required before `IN REVIEW → APPROVED`:

- **R-01 Lead-developer sign-off.** Reviews KD-1…KD-21 against
  project invariants and confirms §2 FR catalogue completeness.
- **R-02 Physics-owner sign-off.** Reviews §3.3 dive kinematics
  + §3.5 hand-ball contact geometry + §3.5.3 closed-form parry /
  deflect / spill helpers for physical plausibility.
- **R-03 Determinism-owner sign-off.** Reviews KD-7 governance:
  the 4 draw-site IDs, iteration order discipline,
  `DOMAIN_TAG_GOALKEEPER` allocation + collision-management with
  ERR-012-001.
- **R-04 Positioning AI #12 owner co-sign.** Reviews §3.3.0
  consumer contract and the atomic `[EST]` → `[GT]` promotion of
  three #12 GK constants per KD-13.
- **R-05 Heading Mechanics #10 owner co-sign.** Confirms KD-4 /
  KD-14 head-route boundary; reviews §3.6 routing predicate
  against #10 §3.7 duel mechanism.

---

## 9.4 Outstanding Items at Approval Time

- **OI-001 (`ERR-011-001`).** `DOMAIN_TAG_GOALKEEPER`
  `[CROSS-PENDING]` → `[CROSS]` atomic with #16 back-prop landing.
  Status: pending. Collision-management with ERR-012-001 per KD-7
  (whichever lands first takes `0x17`; loser shifts).
- **OI-002 (#18 Appendix F.0 channel rows).** 12 `gk.*` channel
  rows back-propagated to #18 Appendix F.0 13-field schema at
  Stage 0+1 delivery schedule per Heading #10 OI-002 closure
  pattern. Not blocking #11 sign-off.
- **OI-003 (DOI verification for §8.3).** Four
  `[CITATION-PENDING]` external references (Dicks 2010; Spratford
  2009; Suzuki 1988; Williams & Burwitz 1993). One reference
  already DOI-verified (Savelsbergh 2002); Opta/StatsBomb retained
  as commercial-data baseline class. Verification follow-up per
  Heading #10 OI-003 pattern; not blocking sign-off.
- **OI-004 (anchor pinning).** Exact §X.Y anchors for #3
  `ICollisionEventConsumer`, #5 `PassIntent` consumer surface,
  #7 perception-latency surface, #8 GK-branch intent surface
  pinned during implementation.
- **OI-005 (#12 GK constants atomic patch).** Atomic patch
  revision to Positioning AI #12 §3.3.3 / §6 promoting three GK
  constants `[EST]` → `[GT]` per KD-13. Gated on #11 `IN REVIEW`
  transition; coordinated with #12 owner co-sign (R-04).
- **OI-006 (`Ball.SetPossessor` surface verification).** Verify
  surface exists in Ball Physics #1 §3.1; if absent, file
  `ERR-011-002`. Not blocking #11 spec sign-off.
- **OI-007 (AM #2 `GroundedReason.DIVING_SAVE`).** Stage 1+
  cleanup item per §7.5; not blocking #11 sign-off.
- **OI-008 (`certification-platform.md` Stage-0 host pin).** Not
  blocking #11 spec sign-off; blocks `FR-PO-052` perf-gate
  activation only. Shared carve-out with Heading #10 OI-006.

---

## 9.5 Cross-Spec Re-Audit (pre-`APPROVED`)

Verify against current APPROVED versions of #1, #2, #3, #4, #5,
#6, #7, #8, #10, #16, #17 — and IN REVIEW version of #12 — that no
upstream surface cited has shifted between draft start and approval.

Particular attention to:

- #12 §3.3.3 (GK baseline formula) — the §3.3.0 consumer contract
  depends on its exact computational shape (KD-13 / R-04).
- #16 §3.4 (`DOMAIN_TAG` table) — verify `DOMAIN_TAG_GOALKEEPER`
  allocation per ERR-011-001 outcome (`0x17` or `0x1D`).
- #10 §3.7 (head duel mechanism) — verify #11 §3.6 head-route
  defer predicate points at the correct #10 surface.
- #1 §3.1 possession surface — OI-006 verification posture.

---

## 9.6 Post-Approval Follow-ups (not gating)

- Comprehensive audit at draft-level rigor parity with #8
  (Decision Tree precedent).
- AM #2 `GroundedReason.DIVING_SAVE` enum addition at Stage 1+ as
  non-behavioral patch (OI-007 / KD-12 / §7.5).
- DOI verification for the remaining four `[CITATION-PENDING]`
  external references in §8.3 (OI-003).
- Defensive AI #14 / Attacking AI #15 / Pressing AI #13 integration
  verification once those specs reach `IN REVIEW`.

---

## 9.7 Decision

- **Status:** `IN REVIEW`
- **Recommended next status:** `APPROVED` after R-01..R-05 sign-offs
  per §9.3 and OI-001 / OI-005 resolution per §9.4.

---

## 9.8 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; constant-tag inventory (~79 rows), cross-spec verification (15 XC), 5 sign-off requirements, 8 outstanding items | self-pass-1 in `adversarial-review-section-files-v1.md` |
