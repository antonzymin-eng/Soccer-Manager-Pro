# Scouting & Player Knowledge #32 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.3
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

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T-phase plan T0–T3, deferred extensions, downstream seam contracts), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M-1): T2/T3 reconciled with the fog-off command gate — the T2 slot is a structural null seam and the commands are uncallable until T3 flips `fogEnabled` (the v0.1 text had the commands "landing" usable at T2, contradicting FR-SC-020). |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (L): new §7.2 **manager job-change** deferral row — the career-constant-`managedClubId` assumption named, with the required re-club sweep (drop new-club entries + cancel a targeting assignment) recorded so a future #45-class feature does not trip the F4 own-squad codec gate. |
#endregion
