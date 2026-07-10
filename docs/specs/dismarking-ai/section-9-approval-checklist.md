# Dismarking & Marker-Awareness AI Specification #23 — Section 9: Approval Checklist

**Created:** July 8, 2026
**Last Updated:** July 10, 2026 (v0.3)
**Version:** 0.3
**Status:** IN REVIEW

---

Checklist entries are verified against actual files; nothing below is checked without a
programmatically verifiable anchor (CLAUDE.md "Never fabricate verification values").

## 9.1 Content gates

- [x] Every constant in §3.5/Appendix A carries exactly one source tag (8 rows: 7 `[GT]`, 1 `[FIXED]`)
- [x] Every §3 formula has units, valid ranges, and a worked example (FM-DM-01..03)
- [x] KD-1 perception-boundary invariant cited verbatim from the design supplement (§1.4)
- [x] No `[EST]` tags present
- [x] Zero-value-identity check: `DismarkIntensity.Off = 0` is the identity row (KD-4)
- [x] `[CITATION-PENDING]` rows in §8.2 verified or replaced (#11 OI-003 precedent) — **both rows VERIFIED July 10, 2026** (Wilson ISBN 978-0-7528-8995-5; Low et al. DOI 10.1007/s40279-019-01194-7); see §8.2 v0.2

## 9.2 Balance-pass carve-out

`[GT]` magnitudes in §3.5 are illustrative shapes pending the implementation-time balance pass —
the same carve-out #21 §9.2 (G2) carried into its approval. The spec's reviewed contract is the
formula shapes, gates, and identity rows.

## 9.3 Review gates

- [x] PASS-1 adversarial review of section files — **run July 8, 2026: 0H+1M+3L, all resolved in the v0.2 fix pass same day** (`adversarial-review-section-files-v1.md`; M-1 = dwell-update ordering vs the stride phase order)
- [x] PASS-2 not required (PASS-1 found no High findings, per the §6 pipeline step 5 rule)
- [ ] Cross-spec back-props (§2.4) filed as ERR entries and landed atomically with `APPROVED`
- [ ] Lead-developer R-01..R-05 sign-off (pending)

## 9.4 Consistency gates

- [x] FR prefix `FR-DM-` verified unclaimed by grep over `docs/specs/**/*.md` (July 8, 2026; existing active prefixes: AT, CS, DA, DS, EVT, GK, HE, LW, PA, PO, PR, TI, TS)
- [x] Candidate number #23 matches the `SPEC_INDEX.md` reservation (July 7, 2026)
- [ ] `SPEC_INDEX.md` row status flip `IN REVIEW → APPROVED` (at sign-off)

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial checklist; review/sign-off gates open by construction. |
| 0.2 | 2026-07-08 | — | PASS-1 run and resolved (0H+1M+3L); PASS-2 not required (no High). |
| 0.3 | 2026-07-10 | — | §9.1 citation gate closed (both §8.2 rows verified). Remaining open: back-prop ERRs at `APPROVED`; R-01..R-05 sign-off; status flip. |
#endregion
