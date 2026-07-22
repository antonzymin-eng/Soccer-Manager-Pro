# Match Analytics & Statistics #37 — Section 9: Approval Checklist

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.3 — PASS-1 (2M+3L) → AR-2 convergence; R-01..R-05 signed; APPROVED)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/match-analytics-statistics-design.md` v0.2

---

Checklist entries are verified against real source; nothing is checked without a programmatically
verifiable anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec —
implementation gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[GT]` model/grid; `[CROSS]`
      `GOAL_WIDTH_M`).
- [x] No `[EST]` tags present.
- [x] Every §3 algorithm has ranges + a worked example (xG penalty-spot App. C; possession/territorial
      hand-fractions §3.1/§3.4).
- [x] KD-1 in-scope set = the verified 8-record ledger inventory + positional sample (Appendix B);
      producer-gated deferrals named (Appendix D).
- [x] KD-2 xG-as-shape / producer-gated stated; the goal-position-is-crossing-point correction is
      explicit (§3.3 / §1.6).

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-AN-001..021 (grep-verified in §2/§5).
- [ ] Layer built — **NOT STARTED** (forward design; §7 T-phase T0..T2).
- [ ] The KD-7 read-only ledger tap on `MatchEngine` — NOT STARTED (T1).
- [ ] Two-run determinism + observer-neutrality proven — NOT STARTED (T1 tests, §5).

## 9.3 Review gates

- [x] **PASS-1 adversarial review of the section files — RUN July 22, 2026 (0H+2M+3L); all fixed.**
      **M-1 (correctness — event loss):** §3.5/§4.4 leaned on the `match-viewer` background-*sampling*
      model, which drops ticks — fine for visual replay, **wrong for event counting** (a skipped tick
      drops its foul/goal/card record). Fixed structurally: the ledger tap is now consumed **every tick,
      losslessly** (only the *positional* sample may stride via `TERRITORIAL_SAMPLE_STRIDE`), and the
      contract is **enforced** by a new **F6** fail-loud (`ObserveTick` throws on a non-consecutive
      `currentTick`) + `T-AN-FAIL-004`. **M-2 (contradictory control flow):** §3.2 listed
      `PossessionChangedEvent` as a routed record while §3.5's loop treated it as an unreachable `elif`
      fallthrough; reworked so possession is a named **known handler** and the other six records route to
      tallies. **L-1:** `SubstitutionEvent` carries a direct `Team` byte (verified in
      `src/event-system/SubstitutionEvent.cs`), so the App. B / §3.2 "slot team" routing → `Team`
      (direct), no KD-6 lookup. **L-2:** §3.4 territorial credit disambiguated (the team into whose
      **attacking** half the ball has advanced = territorial dominance). **L-3:** `MatchPhaseChangedEvent`
      is observed-but-unused at Stage 1 — noted as phase-boundary context with a §7.5 per-half-split seam.
- [x] **AR-2 convergence sweep — RUN July 22, 2026 (0H+0M; L-only ⇒ CONVERGENCE).** Re-read all 11 files:
      verified the M-1 every-tick fix does not contradict KD-7's lifetime-decoupled pull (the host loop
      calls `ObserveTick` after each `RunTick` — every-tick AND decoupled, not an engine-internal
      subscription); completed the F6 guard across §2.3/§3.5/§5; made the §3.2/App. B "§7.5" cross-ref
      valid by adding the per-half-split seam bullet; re-verified the 8-record ledger inventory, the
      no-reader claim, the crossing-point/shot-origin distinction, and the no-RNG/tag/ordinal identifiers.
      Cycle closes per the #21–#30 L-only-round convention.
- [x] **No #16 §3.4 cross-cite — CONFIRMED July 22, 2026.** #37 registers no domain tag / ordinal / RNG
      (KD-5), so — unlike #30's `0x22` — there is nothing to allocate and no `_RESERVED_` placeholder is
      warranted (the `match-viewer` presentation class).
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 22, 2026 (§9.5).**

## 9.4 Consistency gates

- [x] FR prefix `FR-AN-` verified unclaimed by grep over `docs/` (only this spec's own supplement
      referenced it).
- [x] Candidate number #37 matches the roadmap / `spec-plans/spec-37-…` reservation.
- [x] Cited source APIs verified against real files: the 8-record ledger inventory
      (`MatchEngine.cs` publish sites + `src/event-system/*.cs` payloads); `SerializeLedger` is
      write-only (no reader); the `MatchEngine` observation surface (`BallView`/`AgentView`/
      `AgentTeamId`/`CurrentTick`); `GoalAwardedEvent.BallPosition` = crossing point.
- [x] `SPEC_INDEX.md` row to be added at promotion (`IN REVIEW`), flipped at sign-off.
- [x] No #16 §3.4 cross-cite needed — #37 registers no domain tag / ordinal / RNG (KD-5); this is a
      positive property, not a deferred allocation (no `_RESERVED_` placeholder warranted).

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 22, 2026.** PASS-1 (0H+2M+3L) → AR-2 converged (§9.3, 0H unresolved). This is
> a forward design (nothing built) — sign-off approves the DESIGN, exactly as #21–#30 were approved
> before their T0 code; the §7 T-phase plan is the post-APPROVED implementation sequence.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — the 8-record derivation set / xG shape+worked example (App. C) / possession tick-weighting (every-tick, F6) / territorial binning internally consistent; 21 FRs; constants one tag each, no `[EST]`; cited APIs verified against `src/event-system/*.cs` + `MatchEngine.cs` (8 publish types; `SerializeLedger` write-only; `GoalAwardedEvent.BallPosition` = crossing point; `SubstitutionEvent.Team`) | §2/§3/App. A/B/C | ☑ |
| R-03 | **Cross-spec consistency** — no #16 §3.4 allocation (KD-5, confirmed); the KD-7 tap is an observation surface not a producer; the deferred producers named (Appendix D), no phantom consumer (FR-LW-031) | §4 / §7 / App. D | ☑ |
| R-04 | **Stage-binding correctness** — presentation-layer cadence (§1.2, not the 60 Hz hot path); read-only / no persistent state / no format bump; producer-gated xG honest about no Stage-1 input | §1 / §4 / §6 | ☑ |
| R-05 | **Approval granted** — PASS-1 + AR-2 resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | ☑ |

## 9.6 Decision

**APPROVED — July 22, 2026.** The section files are authored from the converged design supplement (v0.2,
AR-1 1M → AR-2 clean); the **section-file PASS-1 (0H+2M+3L) → AR-2 convergence** is resolved (§9.3 — the
M-1 lossless every-tick fix, enforced by F6, is the load-bearing one, grounded in the verified
event-ledger inventory); no #16 §3.4 cross-cite is needed (#37 allocates no determinism identifier,
KD-5); and lead-developer R-01..R-05 sign-off is granted (§9.5). `SPEC_INDEX.md` row 37 flips
`IN REVIEW → APPROVED` (29 APPROVED / 0 IN REVIEW). This approves the **forward design** (the #21–#30
pre-T0 precedent); the §7 T-phase plan (T0 value types + pure `XgLocationModel` → T1 the KD-7 ledger tap
+ aggregator + determinism/neutrality locks → T2 the deferred-producer follow-up, its own review) is the
post-APPROVED implementation sequence. Post-APPROVED, non-blocking: the `[GT]` xG-coefficient balance
pass (§7.4, the #21 §9.2 precedent).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial checklist. Content/consistency gates checked; review + implementation gates OPEN by construction (forward design). Status IN REVIEW. |
| 0.3 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L: M-1 lossless every-tick + F6 guard; M-2 possession known-handler; L-1 `SubstitutionEvent.Team`; L-2 territorial disambiguation; L-3 phase-context note) → AR-2 convergence recorded (§9.3); no #16 §3.4 cross-cite (KD-5); R-01..R-05 signed; §9.6 APPROVED. Status APPROVED. |
#endregion
