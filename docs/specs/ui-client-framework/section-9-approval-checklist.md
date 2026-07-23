# UI / Client Framework #38 — Section 9: Approval Checklist

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.3 — PASS-1 (2M+2L) → AR-2 convergence; R-01..R-05 signed; APPROVED)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/ui-client-framework-design.md` v0.2

---

Checklist entries are verified against real source; nothing is checked without a programmatically
verifiable anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec —
implementation gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[GT]` presentation-feel).
- [x] No `[EST]` tags present.
- [x] Every §3 contract has rules + a worked example (the §3.5 / App. D navigation transition; the §3.3 /
      App. C intent→seam routing; the §3.4 match-view cadence).
- [x] KD-2 split stated: framework slice only; screens deferred to Wave-7 gated on their data specs
      (§1.3 / §7.1).
- [x] KD-1 layer contract stated with the command-vs-observation surface distinction explicit (§1.6).

## 9.2 Implementation status (forward design — nothing built yet)

- [x] FR set complete + stable: FR-UI-001..023 (grep-verified in §2/§5).
- [ ] Pure substrate built (projection / navigation / dispatch) — **NOT STARTED** (T0).
- [ ] `MatchViewModelSource` + observer-neutrality lock — NOT STARTED (T1).
- [ ] UGUI rendering binding — NOT STARTED (T2, Unity-host-gated).

## 9.3 Review gates

- [x] **PASS-1 adversarial review of the section files — RUN July 22, 2026 (results in §9.3.1); all fixed.**
- [x] **AR-2 convergence sweep — RUN July 22, 2026 (results in §9.3.1); CONVERGENCE.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 22, 2026 (§9.5).**

### 9.3.1 PASS-1 / AR-2 record

**PASS-1 — July 22, 2026 (0H+2M+2L); all fixed.**
- **M-1 (self-contradiction):** Appendix B had `MatchViewModelSource` reading `MatchEngine.BallView`/
  `AgentView` **directly**, contradicting §3.4 / FR-UI-005 ("a pure-observation surface holds only the
  streamer, no engine") — and reading the engine from the render thread while the streamer ticks it on
  another thread is the exact data race the streamer exists to prevent. Fixed: the source reads the
  streamer's published immutable `LiveMatchFrame`; the engine surfaces are what the **streamer** reads to
  build a frame (Appendix B rewritten).
- **M-2 (cross-thread command race):** §3.3 called `engine.SetTeamTactic` directly from the dispatcher,
  but during a live streamed match the engine is owned by the streamer's tick thread and the seams are
  not documented cross-thread-safe — a UI-thread command races the tick. Fixed structurally: **FR-UI-023
  + F6** — a live-match command is **marshaled onto the sim thread** via a new presentation-side
  `LiveMatchStreamer.EnqueueIntent` (applied between ticks on the tick thread, the write-side analogue of
  the read-side `TryGetLatestFrame` handoff; §3.3 / §4 / KD-3 / T-UI-DISPATCH-004). Single-threaded
  contexts (pre-kickoff, turn-based advance) call the seam directly.
- **L-1:** Appendix C attributed `AdvanceRound → #30` to the *match-tactics* dispatcher; it is a *season*
  dispatcher's intent — the table now splits routing by owning dispatcher.
- **L-2:** the `Pop`-below-root throw (T-UI-NAV-002) had no FR/failure-mode anchor; folded into FR-UI-011.

**AR-2 — July 22, 2026 (0H+0M; L-only ⇒ CONVERGENCE).** Re-read all 11 files: the M-2 `EnqueueIntent`
addition stays in the presentation layer (the streamer is `match-viewer`; the engine's public seams are
unchanged — only the *thread* they are called from is fixed), so no new sim surface and no phantom
dependency; the split-dispatcher framing is consistent across §3.3 / FR-UI-013 / Appendix C; the
single `IntentKind` enum spanning both dispatchers is fine (each throws F3 on a kind it does not own). The
one L (a stale "22 FRs" in R-02 → 23) is fixed. Cycle closes per the #21–#37 L-only-round convention.

## 9.4 Consistency gates

- [x] FR prefix `FR-UI-` verified unclaimed by grep over `docs/specs/**` (0 hits before this spec).
- [x] Candidate number #38 matches the roadmap / `spec-plans/spec-38-…` reservation; the row is scoped
      "framework slice" so Wave-7 screen specs are distinct later.
- [x] Cited source APIs verified against real files: the layer taxonomy (#20 §3.5.2; no asmdef references
      `MatchViewer`); `LiveMatchStreamer.TryGetLatestFrame`/`Start`/`Stop`/`Pause`/`Resume`/
      `SetSpeedMultiplier`; `LiveMatchServer` holds no `MatchEngine`; the public command seams
      `SetTeamTactic`/`SetPlayerTactic`/`SubstitutePlayer`/`ConfigureSquads`; the observation surface.
- [x] `SPEC_INDEX.md` row added at promotion (`IN REVIEW`), flipped at sign-off.
- [x] No #16 §3.4 cross-cite needed — #38 registers no domain tag / ordinal / RNG (FR-UI-022); a positive
      property, not a deferred allocation (no `_RESERVED_` placeholder).

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 22, 2026.** PASS-1 → AR-2 converged (§9.3.1 / §9.6, 0H unresolved). This is a
> forward design (nothing built) — sign-off approves the DESIGN, exactly as #21–#37 were approved before
> their T0 code; the §7 roadmap is the post-APPROVED sequence.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — the projection / navigation / dispatch contracts + the match-view cadence internally consistent; 23 FRs; constants one tag each, no `[EST]`; cited APIs verified against `match-viewer` + `MatchEngine.cs` | §2/§3/App. A/B/C/D | ☑ |
| R-03 | **Cross-spec consistency** — no #16 §3.4 allocation (FR-UI-022); the no-reverse-reference + public-seam-only-mutation invariants; the generic substrate references nothing sim-side, concrete surfaces only built assemblies (no phantom dependency); screens/seams deferred to their owning specs (KD-2/KD-4) | §4 / §7 | ☑ |
| R-04 | **Stage-binding correctness** — presentation-layer cadence (§1.2, not the sim loops); read-only / no persistent state / no format bump; UGUI rendering honestly Unity-host-gated | §1 / §4 / §6 | ☑ |
| R-05 | **Approval granted** — PASS-1 + AR-2 resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | ☑ |

## 9.6 Decision

**APPROVED — July 22, 2026.** The section files are authored from the converged design supplement (v0.2,
AR-1 2M → AR-2 clean); the section-file PASS-1 → AR-2 convergence is resolved (Version History §9.3.1); no
#16 §3.4 cross-cite is needed (#38 allocates no determinism identifier, FR-UI-022); and lead-developer
R-01..R-05 sign-off is granted (§9.5). `SPEC_INDEX.md` row 38 flips `IN REVIEW → APPROVED` (30 APPROVED /
0 IN REVIEW). This approves the **forward design** (the #21–#37 pre-T0 precedent); the §7 plan (T0 pure
substrate → T1 `MatchViewModelSource` + observer-neutrality lock → T2 Unity-host-gated UGUI binding; and
the Wave-7 screen specs, each gated on its data spec) is the post-APPROVED sequence. Post-APPROVED,
non-blocking: the UGUI rendering binding (Unity-host access) and each screen spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial checklist. Content/consistency gates checked; review + implementation gates OPEN by construction (forward design). Status IN REVIEW. |
| 0.3 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L: M-1 match-view source reads the streamer frame not the engine; M-2 live-match command marshaled onto the sim thread via FR-UI-023/F6 + `EnqueueIntent`; L-1 dispatcher-split routing; L-2 Pop-below-root) → AR-2 convergence recorded (§9.3.1); R-01..R-05 signed; §9.6 APPROVED. Status APPROVED. |
#endregion
