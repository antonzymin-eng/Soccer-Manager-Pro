# Scripted Build-Up Structures Specification #24 — Section 9: Approval Checklist

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

Entries verified against actual files; nothing checked without a verifiable anchor.

## 9.1 Content gates

- [x] Every constant tagged (§3.4: 2 `[DERIVED]`, 3 `[GT]` scalars + `[GT]` tables); no `[EST]`
- [x] Every formula has units, ranges, worked example (FM-BU-01..03)
- [x] KD-5 perception-boundary invariant cited verbatim (§1.4) — satisfied trivially (no opponent data consumed)
- [x] Zero-value-identity check: `BuildUpStructure.None = 0` is the identity row
- [x] Supplement deviation recorded: KD-3 documents the deliberate refinement of the supplement's `TransitionWon` gating (opt-in dial + suppression window), with the default-neutrality rationale
- [ ] `[CITATION-PENDING]` rows in §8.2 verified or replaced (gate for `APPROVED`)

## 9.2 Balance-pass carve-out

Appendix A overlay magnitudes and the §3.4 `[GT]` scalars are illustrative pending the
implementation balance pass (#21 G2 precedent). Reviewed contract: gates, identity rows, zone
hysteresis shape, suppression semantics.

## 9.3 Review gates

- [x] PASS-1 adversarial review — **run July 8, 2026: 0H+3M+2L, all resolved in the v0.2 fix pass same day** (`adversarial-review-section-files-v1.md`; M-1 = intra-team possession events re-arming the suppression window, verified against the actual `PossessionChangedEvent` payload)
- [x] PASS-2 not required (PASS-1 found no High findings)
- [ ] §2.3 back-props filed as ERR entries, landed atomically with `APPROVED`
- [ ] Lead-developer R-01..R-05 sign-off (pending)

## 9.4 Consistency gates

- [x] FR prefix `FR-BU-` verified unclaimed by grep (July 8, 2026)
- [x] Candidate number #24 matches the `SPEC_INDEX.md` reservation
- [x] Away-team mirror test present in §5 (T-BU-I-002) per the ERR-008-002 lesson
- [ ] `SPEC_INDEX.md` status flip at sign-off

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial checklist. |
| 0.2 | 2026-07-08 | — | PASS-1 run and resolved (0H+3M+2L); PASS-2 not required (no High). |
#endregion
