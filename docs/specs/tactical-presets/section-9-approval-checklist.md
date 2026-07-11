# Tactical Presets & AI-Manager Selection Specification #26 — Section 9: Approval Checklist

**Created:** July 8, 2026
**Last Updated:** July 10, 2026, later same day (v0.4 — APPROVED)
**Version:** 0.4
**Status:** APPROVED

---

Entries verified against actual files; nothing checked without a verifiable anchor.

## 9.1 Content gates

- [x] Every constant tagged (§3.5: 4 `[GT]` scalars + `[GT]` tables + 1 `[FIXED]` ordering contract); no `[EST]`
- [x] Every formula has units, ranges, worked example (FM-TP-01..04; Appendix B numerics)
- [x] Supplement §4 open questions resolved to concrete decisions: Q1 → KD-7 (compose-only ⇒ shape/reference review suffices, no independent numeric sign-off for preset contents), Q2 → KD-3 (gate, not clock file), Q3 → §7.1 explicit deferral, Q4 → §7.4 UI deferral
- [x] KD-5 own-state-only invariant stated as the team-level perception-boundary analogue, per the supplement's §6 step 3 requirement
- [x] #21 cited as the hard dependency in §1.1/§1.3, per the same requirement
- [x] Zero-value-identity: `ManagerMode.Human = 0` is the subsystem-level identity
- [x] `[CITATION-PENDING]` rows verified or replaced (gate for `APPROVED`) — **fully closed July 10, 2026 (later same day)**: Wilson VERIFIED (ISBN 978-0-7528-8995-5); Bradley VERIFIED (Bradley & Noakes 2013, *J Sports Sci* 31(15):1627–1638, DOI 10.1080/02640414.2013.796062, PMID 23808376 — index-level corroboration, publisher/Crossref direct resolution still environment-blocked; same evidence class as the Wilson row); see §8.2 v0.3
- [x] A.1 preset compositions' Tempo/Passing/Width member names pinned against the #21 enums — **closed July 10, 2026** (every A.1 value verified against `src/tactical-instructions/`; full member rosters recorded in Appendix A v0.3; PASS-1 L-2)
- [x] PASS-1 M-1 engine-substrate gates tracked — **carried forward post-APPROVED (upstream-owned, July 10, 2026)**: T2 half-time trigger + `MATCH_TICKS_TOTAL` `[CROSS-PENDING]` promotion (halves/match-length model) and T4 live `goalDiff` (first goal-detection producer) are prerequisites of those *implementation phases*, owned by the match-engine substrate, not spec-text gates — the same class as #21's runtime-activation gating (KD-8) and #22's KD-10 upstreams, neither of which blocked sign-off. §3.2/§3.4 record the explicit prerequisite gates; the phases cannot land until the substrate exists

## 9.2 Balance review scope

Per KD-7, preset contents reuse #21-pinned values — the catalogue needs shape/reference review
only. The `[GT]`s this spec itself introduces (`ManagerProfile` archetypes, thresholds, interval)
get their own balance review at implementation (#21 G2 pattern).

## 9.3 Review gates

- [x] PASS-1 adversarial review — **run July 8, 2026: 0H+1M+2L, all resolved in the v0.2 fix pass same day** (`adversarial-review-section-files-v1.md`; M-1 = §3.2/§3.4 consumed nonexistent engine score/halves state with no recorded gate)
- [x] PASS-2 not required (PASS-1 found no High findings)
- [x] Lead-developer R-01..R-05 sign-off — **SIGNED July 10, 2026** (§9.5)

## 9.4 Consistency gates

- [x] FR prefix `FR-TP-` verified unclaimed by grep (July 8, 2026)
- [x] Candidate number #26 matches the `SPEC_INDEX.md` reservation
- [x] No back-props to approved specs required at T0–T3 (§2.3) — consistent with the additive-composition claim
- [x] `SPEC_INDEX.md` status flip — **DONE July 10, 2026** (row 26, Approved Jul 10, 2026)


## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 10, 2026.** All five gates ticked by the lead developer.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — all sections (§1–§9 + appendices) present per the template | 11 files, all present | ☑ |
| R-02 | **Technical accuracy** — FM-TP-01..04 + Appendix B numerics consistent (PASS-1 L-1 sensitivity values re-derived ~39.4′/~52.5′); 20 FRs (FR-TP grep-verified); constants one-tag-each, no `[EST]`; A.1 preset compositions pinned against the actual #21 enum members (full rosters recorded, Appendix A v0.3) | §3; Appendices A/B; PASS-1 (0H+1M+2L, all resolved v0.2) | ☑ |
| R-03 | **Cross-spec consistency** — #21 cited as the hard dependency (§1.1/§1.3); boot-vs-mid-match seam distinction carried from the supplement's AR-1 (appliers pre-kickoff, `SetTeamTactic`/`SetPlayerTactic` mid-match); §8.2 fully closed (Wilson + Bradley & Noakes 2013 DOI 10.1080/02640414.2013.796062 both VERIFIED); no back-props required at T0–T3 (§2.3, consistent with additive composition) | §1 / §2.3 / §8.2 v0.3 | ☑ |
| R-04 | **Stage-binding correctness** — `ManagerMode.Human = 0` zero-value identity; KD-2 coarse decision cadence (never the 10 Hz stride); KD-5 own-state-only invariant (no opponent private-tactic reads — opponent-aware adaptation explicitly deferred, FR-LW-031 reasoning); PASS-1 M-1 engine-substrate prerequisites recorded as explicit T2/T4 gates + `[CROSS-PENDING]` row rather than phantom consumption | §1.4 / §2 / §3.2 / §3.4 / §7 | ☑ |
| R-05 | **Approval granted** — `SPEC_INDEX.md` row 26 flipped; carried-forward items pinned: own-`[GT]` balance review (§9.2) + the upstream-owned engine-substrate gates (§9.1) | `SPEC_INDEX.md` row 26; §9.1 / §9.2 | ☑ |

## 9.6 Decision

**APPROVED — July 10, 2026.** Lead-developer R-01..R-05 sign-off granted. PASS-1 resolved
(0H+1M+2L, v0.2; PASS-2 not required); §8.2 fully closed (the Bradley row VERIFIED July 10, 2026,
later same day — Bradley & Noakes 2013, *J Sports Sci* 31(15):1627–1638, DOI
10.1080/02640414.2013.796062, PMID 23808376); A.1 compositions pinned against the #21 enums; no
back-props required (§2.3). Carried forward post-APPROVED, non-blocking: (i) the §9.2 balance
review of this spec's own `[GT]`s (archetypes/thresholds/interval — #21 G2 pattern); (ii) the
§9.1 engine-substrate gates (T2 halves/`MATCH_TICKS_TOTAL`, T4 goal-detection), which gate those
implementation phases on upstream match-engine deliverables, not this spec's text. Preset
contents reuse #21-pinned values per KD-7 (shape/reference review sufficed).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial checklist. |
| 0.2 | 2026-07-08 | — | PASS-1 run and resolved (0H+1M+2L); §9.1 gains the A.1-pinning and engine-substrate gate items. |
| 0.3 | 2026-07-10 | — | A.1 member-name pinning closed; citation gate partially closed (Wilson verified; Bradley row pending with a recorded environment-blocked attempt). Remaining open: Bradley citation; engine-substrate gates (upstream-owned); sign-off; status flip. |
| 0.4 | 2026-07-10 | — | **APPROVED.** Bradley citation VERIFIED (§8.2 v0.3); engine-substrate gates reclassified carried-forward upstream-owned; sign-off granted; status flip done; §9.5 table + §9.6 decision added. |
#endregion
