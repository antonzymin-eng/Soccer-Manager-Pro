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

## 9.2 Balance review scope

Per KD-7, preset contents reuse #21-pinned values — the catalogue needs shape/reference review
only. The `[GT]`s this spec itself introduces (`ManagerProfile` archetypes, thresholds, interval)
get their own balance review at implementation (#21 G2 pattern).

## 9.3 Review gates

- [ ] PASS-1 adversarial review (pending) — reviewers directed at §3.4 (ladder + hold anti-churn) and the F3 no-post-kickoff-applier audit
- [ ] PASS-2+ until no High findings (pending)
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
#endregion
