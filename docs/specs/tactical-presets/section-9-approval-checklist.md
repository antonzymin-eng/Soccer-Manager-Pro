# Tactical Presets & AI-Manager Selection Specification #26 — Section 9: Approval Checklist

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

Entries verified against actual files; nothing checked without a verifiable anchor.

## 9.1 Content gates

- [x] Every constant tagged (§3.5: 4 `[GT]` scalars + `[GT]` tables + 1 `[FIXED]` ordering contract); no `[EST]`
- [x] Every formula has units, ranges, worked example (FM-TP-01..04; Appendix B numerics)
- [x] Supplement §4 open questions resolved to concrete decisions: Q1 → KD-7 (compose-only ⇒ shape/reference review suffices, no independent numeric sign-off for preset contents), Q2 → KD-3 (gate, not clock file), Q3 → §7.1 explicit deferral, Q4 → §7.4 UI deferral
- [x] KD-5 own-state-only invariant stated as the team-level perception-boundary analogue, per the supplement's §6 step 3 requirement
- [x] #21 cited as the hard dependency in §1.1/§1.3, per the same requirement
- [x] Zero-value-identity: `ManagerMode.Human = 0` is the subsystem-level identity
- [ ] `[CITATION-PENDING]` rows verified or replaced (gate for `APPROVED`)
- [ ] A.1 preset compositions' Tempo/Passing/Width member names pinned against the #21 enums (at T0 latest, before any catalogue code — PASS-1 L-2)
- [ ] PASS-1 M-1 engine-substrate gates tracked: T2 half-time trigger + `MATCH_TICKS_TOTAL` `[CROSS-PENDING]` promotion (halves/match-length model); T4 live `goalDiff` (score state via the first goal-detection producer)

## 9.2 Balance review scope

Per KD-7, preset contents reuse #21-pinned values — the catalogue needs shape/reference review
only. The `[GT]`s this spec itself introduces (`ManagerProfile` archetypes, thresholds, interval)
get their own balance review at implementation (#21 G2 pattern).

## 9.3 Review gates

- [x] PASS-1 adversarial review — **run July 8, 2026: 0H+1M+2L, all resolved in the v0.2 fix pass same day** (`adversarial-review-section-files-v1.md`; M-1 = §3.2/§3.4 consumed nonexistent engine score/halves state with no recorded gate)
- [x] PASS-2 not required (PASS-1 found no High findings)
- [ ] Lead-developer R-01..R-05 sign-off (pending)

## 9.4 Consistency gates

- [x] FR prefix `FR-TP-` verified unclaimed by grep (July 8, 2026)
- [x] Candidate number #26 matches the `SPEC_INDEX.md` reservation
- [x] No back-props to approved specs required at T0–T3 (§2.3) — consistent with the additive-composition claim
- [ ] `SPEC_INDEX.md` status flip at sign-off

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial checklist. |
| 0.2 | 2026-07-08 | — | PASS-1 run and resolved (0H+1M+2L); §9.1 gains the A.1-pinning and engine-substrate gate items. |
#endregion
