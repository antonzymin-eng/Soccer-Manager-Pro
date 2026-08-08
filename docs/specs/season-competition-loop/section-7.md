# Season & Competition Loop Specification #30 — Section 7: Future Extensions & T-phase Plan

**Created:** July 22, 2026
**Last Updated:** August 8, 2026 (v0.3 — the lint sweep: §7's preamble tensed to plan-since-executed)
**Last Updated (prior):** July 22, 2026 (v0.2 — section-file PASS-1 reconciliation, §9.3)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

Authored forward ("nothing is built yet" as of July 22); **the T-phase plan has since been executed** —
T0–T3 all landed July 2026, the #29/#41 T2 wiring and the balance pass on top (currency corrected at
the lint sweep, August 8, 2026). §7.1 is kept as the plan the landings followed; §7.2 the deliberate
deferrals; §7.3 the generalization seams later specs attach to.

## 7.1 T-phase plan (forward)

- **T0 — value types + fixtures + table (behaviour-neutral world floor).** `SeasonState`, `Fixture`,
  `FixtureScheduler` (pure `Generate`), `LeagueTableRow`/`LeagueTable` (`ApplyResult` + tie-break
  `OrderedView`), `SeasonCalendar`, `BoardObjective`/`BoardState`, `MatchResult`, `SeasonViewModel`.
  No `MatchEngine` / `WorldStore` wiring yet; unit + determinism tests only. Behaviour-neutral by
  construction (no orchestrator touched).
- **T1 — save/restore.** New `SeasonStateCodec` (the §3.6 sub-blob); `SeasonSaveCodec.Encode`/`Decode`
  gain the season block; `SeasonSaveManager.Save`/`Load` gain the season parameter;
  `SEASON_SAVE_FORMAT_VERSION` **1 → 2** + new `SEASON_STATE_FORMAT_VERSION`. Round-trip + fail-loud
  tests. The world and match blobs stay byte-untouched (FR-SN-020).
- **T2 — the day-advance loop + the match-outcome producer.** `SeasonLoop` composition root;
  `AdvanceToNextFixtureDay` (KD-2 fixed tick order, only the world tick live);
  `AdvanceAndPlayNextRound(ISquadProvider)` (resolves the whole round, KD-9 — the managed fixture through a real `MatchEngine`, the rest via the round-resolution model, applies every result, emits the
  producer event — **not** #22 ingest, KD-3); the §16 §3.4 back-prop (`DOMAIN_TAG_SEASON_LOOP = 0x22`
  / `SubsystemOrdinals.SeasonLoop = 84`, only #30's row — `0x20`/`0x21` stay gaps for #28/#29).
  Behaviour-neutral floor test (FR-SN-026). The `#19 ScenarioRunner` `season-multi-fixture` capstone.
- **T3 — season-boundary roll + multi-season continuity.** `RollToNextSeason` (the KD-6 restartable
  transform); two-run + restartability tests; the #43 insertion point (a') left explicit but empty.

## 7.2 Deferrals (out of scope, each its own spec)

- **#22 phase-1 ingest activation** — deferred to #33's landing per `FR-LW-032` (activation needs
  match-outcome events **and** vol-2/vol-3). #30 is the producer only (KD-3). The ingest entry point
  is a #22 wiring change, co-defining the payload against `FR-LW-027`/`FR-LW-032`/living-world KD-9/KD-10 — filed
  as a #22 back-prop then, not invented now (FR-LW-031).
- **Finances (#40)** — budget-from-league-finish attaches to the KD-6 boundary roll; not here.
- **Cups / continental / promotion-relegation (#43)** — the multiple-competition + knockout-draw
  generalization; promotion/relegation is the boundary-roll transform at insertion point (a').
- **Discipline / suspensions (#44)** — a read-only view over the match card ledger (like #37) that
  the loop's squad selection would consume; not here.
- **On-disk *roster* persistence, transfers, aging, training** — Squad/Player Data #27 Stage-1+ /
  #28/#29/#31 (master plan §4.3/§4.4).
- **UI (#38)** — renders the `SeasonViewModel`; the loop exposes it (KD-7) but owns no UI.

## 7.3 Generalization seams (where later specs attach)

| Later spec | Seam #30 provides | Contract |
|---|---|---|
| #43 Competition Structure | the boundary-roll insertion point (a') + fixture/table types taking a competition set as *data* | promotion/relegation is a transform between finalize and regenerate; multiple competitions reuse `FixtureScheduler`/`LeagueTable` per competition |
| #40 Finances | the KD-6 boundary roll (finalize → board → **finances** → regenerate) | budget from final league position; a new world-state block, not #30's |
| #33 Human-systems | the FR-SN-016 match-outcome producer event | #22 phase-1 ingest activates when #33 lands (`FR-LW-032`) |
| #37 Analytics / #38 UI | `SeasonViewModel` (read-only, observer-neutral) | table + fixtures + calendar, value copies |
| #44 Discipline | the match card ledger the loop already reads for `MatchResult` | a suspension-availability view the loop's selection consumes |

The **single most load-bearing** forward property is KD-2's fixed day-advance tick order: #28/#29/#33
each slot into a pre-declared null-seam position, so getting the order right *now* — with only the
world tick live — avoids a re-pin across every Wave-2+ spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial forward T-phase plan (T0..T3), deferrals, and the #43/#40/#33/#37/#44 generalization seams. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 reconciliation (whole-round KD-9 command/API rename, living-world-KD disambiguation, KD/FR label fixes). See section-9 §9.3. |
| 0.3 | 2026-08-08 | — | **Lint sweep**: the preamble still read "nothing is built yet" of a spec whose T-phases all landed (the pass-13-L4 class, in §7). |
#endregion
