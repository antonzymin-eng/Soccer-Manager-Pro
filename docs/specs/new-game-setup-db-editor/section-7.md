# New-Game Setup & Database Editor #47 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** October 30, 2026 (v0.2 — §7.5 records implementation-time findings under roadmap C6; prior: July 27, 2026 (v0.1 — initial section-file set))
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `NewGameConfig` + `SquadFileWriter` and its **round-trip suite** — the boundary corpus, the club-scoping lock, the grammar-conformance lock. Nothing wired into the root, no artifact, no editor screen. | **Inert** — and already valuable: the writer is the half §1.4(b) found missing |
| **T1** | The setup flow wired to the root's **generated** branch. `AuthoredDatabase` and its codec exist and are tested, but nothing composes the sub-blob. | **Live and byte-identical.** A generated game started through the flow equals one started in code (T-ED-ID-001) |
| **T2** | **The phase that changes the save.** Compose the **optional** authored sub-blob (ERR-030-017, bumping `SEASON_SAVE_FORMAT_VERSION`); land `season-save`'s authored-source factory (ERR-030-018); wire the root's authored branch. | **Live.** A generated game is still byte-identical; an **authored** game now persists its rosters |
| **T3** | The editor **screen** as a #38-hosted mode; authored nationality pins; custom-competition genesis config (gated on #43, which is APPROVED). | **Named activation** — the authoring UX |

**T1 is genuinely behaviour-neutral, and T2 is genuinely not — but only on one branch.** That asymmetry
is #47's whole shape (KD-7): the outer `SEASON_SAVE_FORMAT_VERSION` bumps at T2 for **every** save (a
frame change), while the sub-blob itself appears **only** for authored games. A generated career's frame
must still be byte-identical to pre-#47 **after** the bump, and T-ED-ID-002 asserts exactly that — "no
block", not "empty block".

**The predicted T2 failure is silent and is the one to test for first.** If the sub-blob is composed but
the **load** path does not require it, an authored save with a missing blob falls back to generation and
**loads a wrong world that looks merely odd** (F7 / T-ED-I-006). Nothing crashes, nothing corrupts, and the
player's authored league is quietly gone.

**T3 is the large one in effort and the small one in contract** — see R-6.

## 7.2 Deep-tier extensions (designed for, not built)

- **The editor screen** (T3): a #38-hosted mode over the data layer, via `IViewModelSource<T>` and
  commands. #38 owns navigation, layout and input; **no data-model logic crosses into it** (FR-ED-028).
- **Custom leagues and cups** (T3): authored as **#43 genesis config** (FR-CP-004 — *"`CompetitionId` MUST
  be config-assigned at genesis"*), never by driving a runtime API. The ordering is already satisfied,
  since #43 is APPROVED.
- **Authored nationality pins** (T3): entries in **#36's existing table**, with the precedence rule
  FR-ED-026 supplies. **#36 needs no new surface** — this is the sparse-overlay half of KD-1, distinct
  from the whole-database replacement rosters use.
- **Authored-file migration across #47's own format versions** (FR-ED-032): distinct from #50's
  **save** migration, because an authored database is an **input artifact** rather than a live save. A
  player's `.db` from an older build is a file-import concern; a save from an older build is #50's.
- **Import/export of a sub-database** (a single club, a single squad) — additive over the same writer and
  the same parser, with the same round-trip contract. No new authority.
- **Editor undo/redo** — a #38/#47-boundary concern over the artifact's value semantics; it needs no
  change to the format, because every state is a valid artifact.

## 7.3 Explicitly not planned

- **A second validation authority.** Not as a "friendly validator", not as a schema, not as a pre-check
  (FR-ED-017). An editor-side check is a UX affordance whose **disagreement with the loader is a bug in
  the check** (F2), and T-ED-I-002/003 assert that in both directions.
- **Applying authored data as an override over generation.** At database scale it runs the generator only
  to discard 100% of it, **and** re-couples authored data to the generator's **draw order** — precisely
  what the golden vector exists to freeze (FR-ED-007).
- **Partial authoring** — some clubs authored, the rest generated (FR-ED-010). It is neither a source nor
  a sparse overlay, and it re-opens the coupling above. If it is ever wanted it is **its own decision with
  its own determinism argument**, not something smuggled in behind the artifact.
- **Applying the strength ramp to authored clubs** (FR-ED-009). It would silently re-tune every authored
  player away from what the author typed — a failure with no error and no visible cause.
- **A hash-plus-external-file reference instead of the sub-blob.** Smaller, and it makes a career depend
  on a file the player can move, edit or lose, with a mismatch **stranding the save with no recovery
  path** (§4.5).
- **Falling back to generation when an authored blob is missing.** The silent-wrong-world failure (F7).
- **A parallel player/squad/attribute model in the editor** (FR-ED-004). The
  `PlayerAttributes`-vs-`AgentMovement.PlayerAttributes` precedent; it diverges the moment #27 adds a
  field.
- **#47 constructing a `League`** (FR-ED-003), which would take a `season-save` reference and
  transitively the whole simulation.
- **Translating authored proper nouns.** They are routed as slot values, not translated (FR-ED-031). A
  club called *"Deportivo"* is called that in every locale.

## 7.4 Risks carried

- **R-1 — the authored save-format consequence will be resisted**, because *"the editor writes a file, the
  game reads the file"* is the intuitive model and it is exactly what §1.4(a) forbids from surviving a
  save/load. **The failure mode is silent**: an authored career would load with **generated** rosters and
  look merely *wrong* rather than broken. T-ED-I-005 (load with the source file deleted) and T-ED-I-006
  (missing blob throws) are what catch it.
- **R-2 — a parallel data model in the editor.** The `PlayerAttributes` collision is the precedent; the
  mitigation is that the artifact stores **#27's `Squad` outright** (FR-ED-004), asserted by
  T-ED-BOUND-004.
- **R-3 — editor-side validation will be added for UX and become a second authority** (KD-2). Stated as
  *"a check that disagrees with the loader is a bug in the check"* rather than *"do not add checks"*,
  because the UX need is **legitimate** and an absolute prohibition would simply be ignored. T-ED-I-002/003
  make the rule mechanical.
- **R-4 — partial authoring is not designed here** (FR-ED-010). Recorded as a **standing option
  deliberately not smuggled in**, because it is the most natural feature request against this spec and the
  most expensive to retrofit: it would re-open the generator coupling and re-introduce draw-order
  dependence.
- **R-5 — the `season-save` reference is one refactor away.** Constructing the `League` inside #47 is the
  obvious simplification, and it transitively pulls `MatchEngine` and `LivingWorld` — an editor that boots
  the match engine to author a text file. T-ED-BOUND-001 is the mechanical defence, and it should be read
  as *the* structural assertion of this spec rather than one of several.
- **R-6 — the editor is a large UX surface and a small spec.** As with #48, the contract is modest and the
  interface work is not. The spec should not imply otherwise: T3's effort is dominated by #38 screens,
  and #47's own contribution there is view-model projections and commands.
- **R-7 — the authored blob is two orders of magnitude larger than any other management block** (§6.4).
  ~100 KB is acceptable today and does not grow with career length, but it is the one place #47 spends
  something a player could notice, and the judgement should be re-checked if #27's roster model grows.

## 7.5 Findings recorded against implemented code (roadmap C6, October 30, 2026)

Recorded, not applied. This spec has no `src/` assembly, so under roadmap C6 these findings are discharged
in the landing named in each bullet, in the same commit as the code they govern. Found by the October 30,
2026 wide check of assembly-less specs against implemented code on `main`. The normative text above is
unchanged.

- **The resume path regenerates the league.** §4.4 gives the root two construction paths for a *new* game
  only. The client's composition root (`src/client-app/CareerSession.cs`, landed October 18, 2026) rebuilds
  every resumed career through `LeagueBootstrap.Generate(worldSeed, clubCount)`, taking club identity
  (names) from the generator and rosters from the save. For an authored save that is F7's silent wrong world
  at the resume layer: authored club names would be replaced by generated ones. T2 MUST branch the resume
  path on the authored flag and build the league from the authored block (FR-ED-007 applies on resume as
  well as at new game), with a test that an authored save never reaches the generator on resume.
- **Roster persistence now overlaps #28.** Since #28 T1 (August 8, 2026; #28 KD-4) the season frame's
  mandatory `PROG` block carries the evolving `PlayerRecord` set, and a resumed career reads its rosters from
  it. Appendix B's per-squad `PlayerRecord` section therefore persists a second roster that matches `PROG`
  on day 0 and diverges as soon as #28 advances. §1.4(a)'s premise, that an authored career which did not
  persist its rosters would load generated ones, no longer holds once `PROG` is composed; club identity
  (Appendix B row 3) and nation pins (row 7) are the parts nothing else persists. Decide at T2 whether the
  squad section is still needed (for example as the immutable authored origin). Either way `PROG` stays the
  career roster authority, and the authored squads MUST NOT be read as the current roster on resume.
- **Save-block integration (#30 Appendix B rules added after this spec's approval).** From the phase that
  composes the block (**T2**):
  (a) the ERR-030-036 destination guard, keyed per family. Emptiness is not a sound key here: a generated
  game legitimately writes no block at all. Provisional reading: the authored flag already serves as the
  wiring fact, and Appendix B (ERR-030-017) already requires flag and block presence to agree in both
  directions; ERR-030-039 additionally requires that fact to be passed through **every** public write entry
  point. Confirm against Appendix B at landing.
  (b) A `SeasonLoop.Restore` (or `CareerSession` resume) carrier for the authored block as soon as the frame
  can carry it (ERR-030-050).
  (c) T1's "setup flow wired to the root's generated branch": the generated branch now exists as
  `NewCareerSetup` + `CareerSession.StartNew` (roadmap C4). Bind to that path rather than adding a parallel
  one, so T-ED-ID-001's identity claim is made against the code players actually run.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with T1's genuine neutrality and T2's one-branch asymmetry stated precisely — the frame bumps for every save while the sub-blob appears only for authored ones — the predicted silent T2 failure named, deep-tier extensions incl. the artifact-vs-save migration split, the not-planned list, risks R-1..R-7 with the `season-save` reference flagged as *the* structural assertion and the ~100 KB blob recorded as R-7). Status IN REVIEW. |
| 0.2 | 2026-10-30 | — | §7.5: C6 findings recorded from the wide check against implemented code (resume path regenerates the league, roster overlap with #28 `PROG`, save-block integration). No normative text changed. |
#endregion
