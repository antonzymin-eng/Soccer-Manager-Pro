# New-Game Setup & Database Editor #47 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.GameSetup`** at `src/game-setup/`, referencing **only**
`TacticalDirector.PlayerDatabase` (#27).

```
#38 (editor screen) ──▶ {#47-data, ui-framework}
#47-data            ──▶ {player-database}                 ← and nothing else
root                ──▶ {#47-data, season-save, #30, …}
```

**The one reference #47 must not take is `season-save`, and the reason is transitive.** `League`'s
constructor is `internal` there, so constructing a `League` inside #47 would require the reference — and
**`season-save` references `MatchEngine` and `LivingWorld`.** An editor would then transitively depend on
the **whole simulation to author a text file**: a headless authoring run would boot the match engine, and
the editor's build would be gated on the sim's.

Instead #47 produces `Club[]` / `Squad[]` **values** and the **root** calls `season-save`'s
authored-source factory (ERR-030-018). This is the same inversion #46's projectors and #49's boundary
adapters use, and it is what keeps §5.5's structural assertion worth making.

**CS0104 pre-check.** #47 introduces `NewGameConfig`, `AuthoredDatabase`, `AuthoredClub`,
`SquadFileWriter`, `AuthoredDbCodec`, `EditorViewModelSource`. Each was checked against every name that
could be in scope with it before authoring, because this project has hit CS0104 twice
(`TacticTranslation`, `PlayerAttributes`). `Squad`, `PlayerRecord` and `League` are **consumed, never
re-declared** — and `AuthoredClub` is deliberately **not** named `Club`, which is `season-save`'s.

## 4.2 File layout

```
src/game-setup/
├── GameSetupConstants.cs        # the Appendix A catalogue — no magic numbers in formula code
├── NewGameConfig.cs             # the transient handoff value (KD-5)
├── AuthoredClub.cs              # id + name; NO strength field (FR-ED-009)
├── AuthoredDatabase.cs          # the artifact; stores #27's Squad outright (FR-ED-004)
├── SquadFileWriter.cs           # FM-ED-01 — the missing half; validated by round-trip
├── AuthoredDbCodec.cs           # the optional sub-blob, version gate first (FR-ED-011)
├── EditorViewModelSource.cs     # IViewModelSource<T> projections for #38 (KD-4)
└── tests/
```

**No validator file exists, and that is FR-ED-017 rather than an omission.** `SquadFileLoader.Parse` is
the single authority; a `AuthoredDataValidator.cs` in this tree would be the second one, and it would
acquire rules the moment someone wanted a friendlier message.

**No `League` construction file exists** (FR-ED-003) — the factory is `season-save`'s (§4.4).

**No editor *screen* lives here.** Layout, navigation and input are #38's; this assembly is the **non-UI
data layer**, which is what makes a headless authoring run possible (FR-ED-005).

**`SquadFileWriter.cs` sits beside no parser**, deliberately: the parser is #27's, and co-locating a copy
of it here — even a "read it back to check" helper — is how a second grammar starts.

## 4.3 The #38 hosting split (KD-4)

```
# in #38 — the SCREEN
class DatabaseEditorScreen
{
    IViewModelSource<SquadEditView> _source;         # #47's data layer, projected
    void OnCommit(EditCommand cmd) => _dispatcher.Dispatch(cmd);   # commands out
}
```

| Layer | Owns |
|---|---|
| **#38** | navigation, layout, input, the screen's lifecycle |
| **#47** | the format, the writer, the artifact, the view-model projections |
| **#27** | the model and **all** validation |

**No data-model logic lives in the presentation layer** (FR-ED-028). The test that keeps this honest is
not a review convention: because #47's data layer has **no UI dependency**, a headless authoring run
exercises the whole contract, and anything that only works through the screen is by definition in the
wrong place.

**Editor-side checks are a UX affordance, never an authority** (FR-ED-019). A screen may grey out an
out-of-range value before commit — the UX need is legitimate, and forbidding it outright would simply be
ignored — but **the commit still goes through `Parse`**, so a permissive check cannot admit bad data. It
can only mislead the user, which makes a disagreeing check a **bug in the check** (F2).

## 4.4 The root's two construction paths (KD-1 / KD-5)

```
# root-side — the root already references #47, season-save and #30
StartNewGame(in NewGameConfig cfg, AuthoredDatabase authored /* null when !cfg.HasAuthoredDb */):
    League league = cfg.HasAuthoredDb
        ? SeasonSave.LeagueFactory.FromAuthored(authored.ToClubs(), authored.Squads)   # ERR-030-018
        : LeagueBootstrap.Generate(cfg.WorldSeed, cfg.ClubCount);                      # UNCHANGED

    var season = league.CreateSeason(cfg.ManagedClubId);
    if (cfg.HasAuthoredDb) saveComposer.AttachAuthoredDb(authored);                    # ERR-030-017
```

**The generated branch is untouched** (FR-ED-002). Same call, same parameters, same draw budget, same
golden vector. That is what makes KD-7's identity claim exact rather than approximate.

**The authored branch never runs the generator** (FR-ED-007). This is the *source, not patch* decision:
at database scale a fully authored league is **100% overrides**, so a generate-then-overlay design would
run the generator only to discard all of it — **and** would make the authored result depend on the
generator's draw order, re-coupling authored data to precisely what the golden vector exists to freeze.

**The factory guards what #47 cannot.** `FromAuthored` enforces ascending-unique ids, one squad per club,
and `StrengthDelta == 0` (§3.3). Placing those guards in `season-save` rather than #47 is deliberate: the
assembly that owns `League`'s invariants keeps sole responsibility for constructing one, so a second
caller cannot bypass them.

## 4.5 Save composition (KD-1(ii))

#47's sub-blob is composed into #30's `SeasonSaveCodec` alongside #40's, #45's, #44's and the rest, as a
length-prefixed **opaque** block: the outer codec never parses it, so `AUTHORED_DB_SAVE_FORMAT_VERSION`
and `SEASON_SAVE_FORMAT_VERSION` move independently (FR-ED-011). Layout in Appendix B.

**It is the only conditional sub-blob in the save.** Every other management block is written for every
career; #47's is written **only when `HasAuthoredDb`** (FR-ED-012) — and a generated game writes **no
block at all, not an empty one**, which is what preserves byte-identity with pre-#47 rather than merely
approximating it.

**The two facts must agree** (F8): a save flagged authored carries a sub-blob, and one flagged generated
does not. A mismatch means half the write path ran and the other half did not, and the decode gate is
what turns that into an error rather than a wrong world.

**An authored save is self-contained** (FR-ED-013): loadable with the editor absent, the source file
absent, and on a different machine. The rejected alternative — a **content hash plus an external file
reference** — was smaller in the save and was rejected because it makes a career depend on a file the
player can move, edit or lose, with a hash mismatch **stranding the save with no recovery path**. The
project's own precedent is decisive: `MatchSaveManager` deliberately made the match file self-sufficient
by carrying the boot seed rather than referencing it, and the season save is *"one file"*.

**Migration posture: none — pre-bump saves are rejected fail-loud.** Migrating **saves** across versions
is #50's subject; migrating an authored **file** across #47's own format versions is #47's, at the deep
tier (FR-ED-032). Recording the split here means #50 inherits a stated position rather than an assumption.

## 4.6 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#27** | The editor **reads and writes #27's format** and stores **#27's `Squad`** outright. `SquadFileLoader.Parse` is the **single validation authority**. **Nothing in #27 changes** — the writer is a new surface *over* the format, not an amendment to it (§8.4). |
| **`season-save`** | Owns `League` and its `internal` constructor, and therefore owns the **authored-source factory** (ERR-030-018) and its guards. **#47 does not reference it** (§4.1). |
| **#30** | Composes the **optional** sub-blob (ERR-030-017) and plays whatever `League` it is handed. **#47 references #30 never**; the handoff is a value (KD-5). |
| **#36** | Authored nationalities are entries in **#36's existing `NationPin` table**. #36 needs **no new surface**; #47 supplies the precedence rule #36 left open — a later re-key pin **overwrites** an authored one (FR-ED-026). |
| **#38** | Hosts the editor screen over #47's data layer via `IViewModelSource<T>` and commands. **#38 owns the UI; #47 owns the data.** |
| **#43** | Custom competitions are authored as **genesis config** (FR-CP-004), never by a runtime API. Deep tier; **no #43 change**. |
| **#49** | Authored proper nouns travel as **`NamedSlotSet` slot values** — routed through the seam, never translated, never allocated a key (FR-ED-031). **No #49 change.** |
| **#16** | **Untouched — no row and no `_RESERVED_` placeholder.** #47 is tooling: no stream, no tag, no ordinal, and the `worldSeed` is an input rather than a draw (FR-ED-029/030). |
| **#50** | Registers `AUTHORED_DB_SAVE_FORMAT_VERSION`; inherits the save-vs-artifact split (FR-ED-032). |

**Standing review item:** #47 performs **no** write to #27, `season-save`, #30 or #36 state. #47's data
layer references only #27, so its own graph proves most of it — but **#27's types are reachable**, so the
no-mutation property against `Squad` and `PlayerRecord` is asserted **behaviourally** in §5.5, and the
**root's** two-path construction (§4.4) is asserted there too, since that is the one place both origins
are visible at once.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (leaf assembly with the transitive-`season-save` argument spelled out — an editor would otherwise boot the match engine to author a text file — the CS0104 pre-check incl. the deliberate `AuthoredClub`-not-`Club` naming, file layout with three deliberate absences, the #38 hosting split, the root's two construction paths, the conditional sub-blob and the rejected hash-reference design, neighbour contracts). The standing review item is scoped to the **root's two-path construction** as well as to #47, since that is the only place both `League` origins are visible together. Status IN REVIEW. |
#endregion
