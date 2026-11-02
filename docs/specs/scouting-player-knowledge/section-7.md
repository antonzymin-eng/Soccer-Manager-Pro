# Scouting & Player Knowledge #32 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** October 30, 2026 (v0.4 — §7.4 records implementation-time findings under roadmap C6; prior: July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial))
**Version:** 0.4
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Scouting` assembly: value types (`AttributeEstimate`, `KnownPlayer`),
  `KnowledgeView` (`ResolveBand` + `EstimateFor` — the identity path: fog off, `BAND_MAX`,
  `[truth, truth]`, zero draws), `ScoutingState`, `ScoutingConstants`. Behaviour-neutral by
  construction (KD-8).
- **T1** — `ScoutingSaveCodec` (`SCOUTING_SAVE_FORMAT_VERSION` = 1) + composition into #30's season
  save (the `SeasonSaveCodec` sub-blob; #30's outer `SEASON_SAVE_FORMAT_VERSION` bump coordinated
  here — exact version TBD, §4.4). Fail-loud gates (F3/F4) incl. the canonical-order decode.
- **T2** — wire the world-tick step at #30's **new scouting slot** (ERR-030-007, declared at
  approval — §8): a structural null seam (`AdvanceScoutingDay` no-ops with fog off, FR-SC-022).
  **No RNG stream registered; no command is callable yet** (`AssignScout` requires `fogEnabled`,
  FR-SC-020 — the command *code* lands here, its activation is T3).
- **T3** — deep fog (`fogEnabled`): the band half-width table live, the keyed accuracy draws (the
  **first draw site — promotes `DOMAIN_TAG_SCOUTING = 0x24` / `SubsystemOrdinals.Scouting = 86`**,
  spec-text-first, ERR-016), the `AssignScout`/`CancelAssignment` commands active, #34
  `ToScoutQuality` consumption (`DaysPerBand`), reports + `RankByEstimate`, and the roster-event
  hygiene drops (consuming #31's roster-move hook when ERR-030-005's build lands, and the #28
  season-boundary lifecycle coordination for retirement).

## 7.2 Deferred (recorded, not built)

- **Frozen-at-report staleness / knowledge decay.** The pinned Stage-3 semantic is the live-form
  window (FR-SC-010); freezing the window at last-report truth requires storing per-attribute
  snapshots (the state KD-1 avoids), and knowledge decay (bands degrading over unscouted seasons)
  is its natural companion. Both are one deferral: a stored-snapshot overlay tier.
- **Quantized-truth centering.** Rejected at Stage 3 (quantization error breaks the containment
  invariant unless widths absorb it); recorded as the alternative freshness model.
- **Quality-bounded knowledge ceilings.** A poor scout unable to reach `BAND_MAX` on a world-class
  player (quality entering achievable-band, not just speed) — requires storing the achieved width
  explicitly (the KD-4 retroactivity analysis).
- **`WeakFootRating` / potential (PA) fog.** `WeakFootRating` is exact at Stage 3 (FR-SC-008); a PA
  star-rating estimate would read #28's career state read-only. Both need their own width models.
- **Multiple scouts / assignment queues.** `MAX_ACTIVE_ASSIGNMENTS = 1` (the single ChiefScout
  slot); a deep #34 staff pool widens this with per-scout assignment lanes.
- **Nation/league coverage scouting.** Region-scoped passive knowledge accrual (FM-style "scouting
  range") — a coverage overlay above the per-player band model.
- **AI-manager fog + negotiation-seam reuse.** AI clubs scouting under fog (per-AI-manager
  overlays) and #32-side offers through #31's FR-TX-010 counterparty-generic seam — the far-deep
  autonomous-AI tier.
- **Report events into #46.** Band-up events aggregated into the news/inbox when #46 lands
  (FR-LW-031 — no interface built).
- **Manager job changes (the #45 neighbourhood).** FR-SC-009's own-squad rule and the Appendix-B
  own-squad codec gates assume **`managedClubId` is career-constant** — true today (no feature
  moves it). A future job-change feature MUST run a **re-club sweep** before the own-squad rule
  flips: drop overlay entries for the new club's players (they become omniscient) and cancel an
  assignment targeting one (the FR-SC-019 semantics applied to manager movement rather than
  player movement) — otherwise a previously-scouted entry becomes own-squad-incoherent and trips
  the F4 codec gate. Recorded here so the assumption is named, not discovered.

## 7.3 Seam contracts recorded for downstream authors

- **#27 (Squad/Player Data):** #32 reads `PlayerRecord`/`PlayerAttributes`/`AttrIdx` read-only via
  caller-supplied value copies; it never writes #27 state (FR-SC-001) and never re-declares the
  attribute schema.
- **#34 (Staff & Backroom):** #32 consumes `ToScoutQuality` of the ChiefScout slot-holder (deep) and
  owns the baseline `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000` #34's spec text left open; #33
  judgement reaches #32 only through #34's projection (#34's sole-path discipline).
- **#31 (Transfers):** #32 informs the manager's decisions; the action is #31's `SubmitBid`,
  unchanged; #31's counterparty valuation reads truth (FR-TX-001) — fog is the manager's condition.
  The FR-TX-010 seam reuse activates only at the far-deep AI-scouting tier.
- **#30 (season loop):** owns the tick slot (ERR-030-007) + the season-save composition; #32 never
  references #30. The roster-move hook (#31 FR-TX-022) and the #28 lifecycle coordination
  (FR-TX-028) deliver the FR-SC-019 drops at T3.
- **#38 (UI, future):** renders `KnownPlayer`/report view models (FR-UI-002 shape) and drives
  `AssignScout`/`CancelAssignment` through the command seam; MUST NOT mutate #32 state directly and
  MUST NOT reach around the view to #27 truth for un-fully-scouted external players (FR-UI-004 —
  the view is the only attribute surface those screens see).
- **#46 (news/inbox, future) / #42 (youth academy, future):** aggregate report events / reuse the
  knowledge-overlay pattern; #32 builds no interface for them (FR-LW-031).

## 7.4 Findings recorded against implemented code (roadmap C6, October 30, 2026)

Recorded, not applied. This spec has no `src/` assembly, so under roadmap C6 these findings are discharged
in the landing named in each bullet, in the same commit as the code they govern. Found by the October 30,
2026 wide check of assembly-less specs against implemented code on `main`. The normative text above is
unchanged.

- **Per-day idempotence of `AdvanceScoutingDay` (ERR-030-027, August 7, 2026).** #30 now runs career-day
  slots 0–11 at the top of `AdvanceAndPlayNextRound` and again on the next day advance for the same world
  day (`SeasonLoop.RunCareerDaySteps`, two call sites), relying on each live step being idempotent per day
  through its own cursor. §3.4's `AdvanceScoutingDay` increments `DaysIntoBand` with no per-day guard, so once
  active (T3, `fogEnabled`) an assignment would accrue two days on every fixture day. The T3 landing MUST add
  a serialized last-advanced-world-day guard (the #33 / #45 F6 shape), with the §2 / §3 / Appendix B text that
  carries it and a test that a same-day re-run advances nothing. Inert until then: the slot is a null seam
  while fog is off.

- **Slot number.** #32's slot is **step 9**, under **ERR-030-022** (#30 §3.3.1), not step 7 / ERR-030-007 as
  §1, FR-SC-022, §4, §7.1, §8 and §9 still say; the world-day tick is step 12. #30 §3.3.1 deliberately left
  citing specs unpatched. Correct these citations at T2.

- **Save-block integration (#30 Appendix B rules added after this spec's approval).** From the phase that
  composes this block into the season frame (**T1**):
  (a) a guard against overwriting a populated destination block (ERR-030-036), keyed **per family**: on
  emptiness only if a destination that once carried this state can never legitimately present as empty
  (the #28 / #40 case); otherwise on an explicit wiring fact passed through **every** public write entry point
  (the #44 case, ERR-030-038 / ERR-030-039). Provisional reading: the overlay can legitimately empty (it is always empty with fog off,
  and FR-SC-019 drops entries), so a wiring fact is likely required. Confirm the classification against Appendix B at landing.
  (b) A `SeasonLoop.Restore` resume parameter as soon as the frame can carry populated state (ERR-030-050).
  (c) Where the block is club-keyed, the equivalent of #40's current-season club-universe coherence rule
  (ERR-030-050).
  (d) Composition in `client-app`'s `CareerSession` (`StartNew` / `Resume`, landed October 18, 2026) as well as
  in `SeasonLoop` — a second composition root this plan predates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T-phase plan T0–T3, deferred extensions, downstream seam contracts), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M-1): T2/T3 reconciled with the fog-off command gate — the T2 slot is a structural null seam and the commands are uncallable until T3 flips `fogEnabled` (the v0.1 text had the commands "landing" usable at T2, contradicting FR-SC-020). |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (L): new §7.2 **manager job-change** deferral row — the career-constant-`managedClubId` assumption named, with the required re-club sweep (drop new-club entries + cancel a targeting assignment) recorded so a future #45-class feature does not trip the F4 own-squad codec gate. |
| 0.4 | 2026-10-30 | — | §7.4: C6 findings recorded from the wide check against implemented code (scouting assignment idempotence, slot number, save-block integration). No normative text changed. |
#endregion
