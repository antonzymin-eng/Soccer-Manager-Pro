# New-Game Setup & Database Editor #47 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.6 — AR-5 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.5 AR-4, v0.4 AR-3, v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.6
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#47** · **FR prefix:** `FR-ED` · **Wave:** 7 · **Tier:** S2
> **Promoted from:** `docs/tracking/spec-plans/spec-47-new-game-setup-db-editor.md` v0.1

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #47 plan defers, against **verified** upstream source
rather than assumption. Design only — no code, no section files, no registry row.

**The plan's §4 is wrong, and the reason is architectural rather than clerical.** It states the editor
*"adds no new save block and does not touch `SEASON_SAVE_FORMAT_VERSION` / `WORLD_STORE_FORMAT_VERSION`"*.
That holds for a **generated** game and fails for an **authored** one, because this project does not save
rosters — it regenerates them from the world seed (§2(a)). A player the user edits is, by construction, no
longer a function of the seed. KD-1 is that consequence followed through; it is the single decision #47
turns on, and it is not in the plan.

A second, smaller gap: the plan calls the loader seam a *"read/write contract"*. **There is no writer**
(§2(b)).

## 1. Scope

**#47 owns:** the **new-game setup flow** (start point, league/club selection, world seed) and the
**authoring surface** over #27's data format — plus, per KD-1, the **authored-database artifact** and its
identity.

**#47 does not own:**

| Not owned | Owner | How #47 relates |
|---|---|---|
| The roster/attribute model and its validation grammar | **#27** | the editor reads and writes that format; it never redefines it (KD-2) |
| Generation from a seed (`LeagueBootstrap`, `RosterGenerator`) | **#27/#30** | authored data is an **alternative source** for the same `League`, never a patch of the generator (KD-1) |
| The season loop that plays a database | **#30** | #47 hands over data and references no sim loop (KD-5) |
| Competition instance definitions | **#43** (FR-CP-004, config-assigned at genesis) | custom-league authoring writes that config at depth (KD-3) |
| The UI shell hosting the editor | **#38** | #38 hosts; #47 owns no navigation or layout (KD-4) |
| Live-save migration | **#50** | an authored database is an **input artifact**, not a live save (KD-1) |

## 2. What already exists (verified)

**(a) The game does not save rosters — it regenerates them, and that is what makes authoring hard.**
`LeagueBootstrap.Generate(ulong worldSeed, int clubCount) → League` builds an N-club league from one seed;
`League` is a sealed class that **is** the `ISquadProvider`; and `SeasonSaveCodec` contains **no roster
data** at all (verified: no roster field in the codec). The root `CLAUDE.md` states the invariant plainly —
rosters are *"REGENERATED from the world seed rather than saved"*, which is why `LeagueBootstrapGoldenVectorTests`
pins a golden digest and why the #27 draw budget is contract-locked.

**Consequence — the decision #47 exists to make:** an authored player is not derivable from any seed.
Either the authored data lives **in the save**, or an authored career depends on an external file that can
move, change, or disappear. The plan's *"adds no new save block"* is therefore only true of the case that
needs no editor. KD-1 resolves it.

**(b) The authoring format has a parser and no writer.** `SquadFileLoader` exposes exactly
`Parse(string text, int clubId) → Squad`. There is **no** `Write`, `Serialize`, or `ToText` anywhere in
`src/player-database/` or `src/season-save/` for this format.

**Consequence:** the plan's *"read/write contract"* is half-built. #47 must define the **writer**, and the
writer's correctness condition is a round-trip against the existing parser — the encode/decode asymmetry
class this project has already been bitten by (#30 T1's `SeasonState`, constructible but not decodable).
KD-2 makes the parser the arbiter rather than the writer.

**(c) The authoring grammar is deliberately human-facing and parser-swap-ready.** `SquadFileLoader` mirrors
`TeamTacticFileLoader`'s grammar, and the tactic loaders carry an explicit contract the root `CLAUDE.md`
repeats: the Stage-0 text format is *"NOT a determinism-pinned wire format"*, and the Stage-1 `[GT]` loader
*"may replace the grammar leaving `Apply` untouched."*

**Consequence:** KD-1's parser-swap requirement is satisfied by construction *provided* the editor binds to
the **loader's types** (`Squad`, `PlayerRecord`) rather than to its syntax — which is the substance of the
plan's KD-1 and is cheap to state now.

**(d) Validation already exists, is fail-loud, and had to be fixed once.** `SquadFileLoader` bounds every
numeric key; the July-16 AR found `age` unbounded against the loader's own *"out-of-range int all throw"*
contract and closed it. #27's own history also records `SquadFileLoader` computing `PlayerId` from a raw
section index instead of the club-scoped formula — caught by a round-trip test.

**Consequence:** the loader is the validation authority, and both defects it has had were **found by
round-trip tests**. KD-2 adopts both facts.

**(e) #36 has already created the authored-override mechanism #47 needs for nationality.**
`national-teams-international-design.md` KD-1 makes nationality a pin-then-derive lookup with a `NationPin`
table, and states in terms: *"#47's authoring lands in this same table — an authored entry is a pin like any
other… because the table ships at approval (for re-keys) #47 adds no #36 surface at all."* It also names the
one thing #47 must decide: **precedence** when a player has both an authored nationality and a transfer pin.

**(f) `LeagueBootstrap` already bounds the setup parameters #47's flow collects.** `Generate` validates
`clubCount ∈ [2, MaxClubCount]` (32), fails loud when the name catalogue is too small, and fails loud when
club count would exhaust `MaxRngStreams` — with messages naming the constant to change.

**Consequence:** the setup flow's validation is **already written**; #47 collects parameters and lets
`Generate` refuse. KD-3's minimal tier needs no new gate.

**(g) #43 pins where custom competitions come from.** FR-CP-004 — *"`CompetitionId` MUST be config-assigned
at genesis (deterministic; instance 0 = 0; never reused)"* — so authoring a custom cup means writing that
genesis config, not calling a #43 API at runtime.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | Setup flow over **generated** worlds only: seed, club count, managed club — each already gated by the code that consumes it (KD-3). **No editor**, no authored artifact, no save change. A game started this way is byte-identical to one started in code today. |
| **Deep** | The authoring surface: load → edit → write #27's format, the authored-database artifact (KD-1), authored nationality pins (§2(e)), and — gated on #43 — custom competitions (KD-3). |

The split is deliberate and load-bearing: **everything that changes the save format lives in the deep
tier**, so the minimal tier is a pure front-end over machinery that already exists and already validates.

## 4. Key decisions

### KD-1 — An authored database is a **source for `League`**, and an authored game **saves its rosters**

Two halves, and the second is the one the plan misses.

**(i) Source, not patch.** `League` gains a second origin — built from an authored database — beside
`LeagueBootstrap.Generate`. Everything downstream is source-agnostic, because everything downstream already
talks to `League` through `ISquadProvider` and `CreateSeason`. The generator is **not** modified, so a
generated game keeps its exact byte-for-byte behaviour and the golden vector is untouched (the property
#36's KD-1 also protects, by a different route).

**`League`'s constructor is `internal` to `season-save`, so that second origin is a `season-save` addition,
not a #47 one** — a back-prop #47 owes (§8.1, `ERR-030-018`). #47 produces the authored **values**
(`Club[]` + `Squad[]`, both existing types) and the **root** hands them to a `season-save` factory. This is
better than widening `League`'s constructor: #47 stays a leaf over `player-database` alone, and the assembly
that owns `League`'s invariants keeps sole responsibility for constructing one.

**Authored clubs carry no strength ramp, and that is not an omission.** `Club` holds a `StrengthDelta` — the
seeded ramp `LeagueBootstrap` applies so a generated table is *"not 20 statistically identical teams"*. An
authored database specifies attributes **directly**, so the differentiation is already in the data; applying
a ramp on top would silently re-tune every authored player away from what the author typed. Authored clubs
therefore take `StrengthDelta = 0` and no ramp is applied. This is the one place the two `League` origins
genuinely differ, so it is stated rather than left to be discovered when an authored league plays oddly.

*Rejected alternative:* apply authored data as an **override layer** over generation (generate, then patch).
Rejected at database scale — a fully authored league is 100% overrides, so the generator would run only to
be discarded; and it would make the authored result depend on the generator's draw order, re-coupling
authored data to the very thing #27's golden vector exists to freeze. (The override shape is right for a
*sparse* fact like #36's nationality pins, and #47 uses it there — §2(e). The distinction is sparse
overlay vs whole-database replacement.)

**(ii) An authored career's save must carry its rosters.** Since rosters are regenerated rather than saved
(§2(a)), and an authored roster is not derivable from any seed, an authored game's save needs the data
itself. It lands as an opaque, independently version-gated
**`AUTHORED_DB_SAVE_FORMAT_VERSION`** sub-blob composed into #30's `SeasonSaveCodec` (the
#40/#42/#43/#44/#45 pattern), written **only** for authored games:

- A **generated** game writes no sub-blob and is byte-identical to pre-#47 — so the plan's §4 claim is
  preserved exactly where it was true.
- An **authored** game is self-contained: it does not depend on the editor, the source file, or the machine
  that made it.

*Rejected alternative:* store a **content hash + external file reference** and fail loud on mismatch (the
`EnvironmentFingerprint` discipline). Smaller in the save, and rejected anyway: it makes a career depend on
a file the player can move, edit, or lose, and a hash mismatch would strand a save with no recovery path.
The project's own precedent is decisive — `MatchSaveManager` deliberately made the match file
self-sufficient by carrying the boot seed rather than referencing it, and the season save is *"one file"*.

**This is the third time this pattern has appeared** (#44's discipline tally, #46's inbox items, now
authored rosters): *the thing cannot be recomputed, therefore it must be persisted.* Worth naming as a
pattern, because the instinct each time was to derive.

### KD-2 — #27's loader is the **single validation authority**; the writer is validated by round-trip

The editor performs **no** validation of its own. It writes text and hands it to `SquadFileLoader.Parse`;
if the parse throws, the data was invalid. This avoids the two-sources-of-truth drift the plan's §9 names —
and §2(d) shows the risk is not hypothetical, since the loader's *own* gates have needed correcting twice.

**The new writer's correctness condition is a round-trip, not a review:** for any `Squad` the parser
accepts, `Parse(Write(squad)) == squad` field-for-field. That single property covers the encode/decode
asymmetry class (#30 T1); it is also the test that caught `SquadFileLoader`'s club-scoping defect at #27 T0
(the `age`-bound defect was found by a later adversarial review, not by a test — which is the argument for
having the round-trip lock rather than relying on review).

**Editor-side checks are a UX affordance, never an authority.** An editor may grey out an out-of-range
value *before* the user commits it, but the commit still goes through `Parse`, and a check that disagrees
with the loader is a **bug in the check**. Stated this way round, "add a friendly validator" cannot quietly
become a second gate.

### KD-3 — Minimal is **generated-world setup only**; custom competitions are #43-gated

The minimal boundary the plan asks for: the setup flow collects `worldSeed`, `clubCount`, and the managed
club, and **each is already gated by the code that consumes it** — `clubCount` by `LeagueBootstrap.Generate`
(§2(f), including the name-catalogue and `MaxRngStreams` refusals), the managed club by
`League.CreateSeason(managedClubId)`, and `worldSeed` by nothing because every `ulong` is valid. #47 adds a
front-end and **no gate of its own**; where it needs to tell the user *why* a value was refused, it surfaces
the exception the consumer already throws rather than pre-checking (KD-2's rule, applied to setup).

Custom leagues/cups are deep-tier and gated on #43 — authoring them means writing the **genesis config**
FR-CP-004 describes (§2(g)), not driving a runtime API. Authoring competitions before #43 exists would be
the phantom-dependency trap the plan flags; the ordering is already satisfied since #43 is APPROVED.

### KD-4 — The editor is a **#38-hosted mode over #47's own data layer**

The data layer (parse/write/validate-by-parse, the authored artifact) is a **non-UI assembly**; the editor
*screen* is a #38 screen consuming it through `IViewModelSource<T>` and dispatching edits as commands.
So: #38 owns navigation, layout and input; #47 owns the format and the artifact; **no data-model logic lives
in the presentation layer** (the plan's KD-4 concern), and the editor is separable — a headless authoring
run is possible because the data layer has no UI dependency.

**#47's data layer references `player-database` and nothing else** — not `season-save` either, since the
root constructs the `League` (KD-1(i)). No sim loop, no `MatchEngine`, no `Localization`.

### KD-5 — Handoff is a **value artifact**; #47 never references #30

Setup produces a `NewGameConfig` — `{worldSeed, clubCount, managedClubId, hasAuthoredDb}` — a plain value,
with the `AuthoredDatabase` itself travelling beside it (the flag says which branch the root takes; the
artifact is not embedded in the config, so a generated setup carries nothing). The **root** consumes it: generated ⇒ `LeagueBootstrap.Generate`; authored ⇒ `League` from the
artifact. #47 references neither #30 nor the composition root, exactly as #46's projectors and #49's
adapters invert their directions.

### KD-6 — Determinism: tooling, and the seed is an **input** not a draw

No RNG stream, no domain tag, no `SubsystemOrdinal`; the roadmap classifies the editor as tooling and #16's
catalogue has **no row and no `_RESERVED_` placeholder** for #47. Authoring is human-driven; the world seed
is a *parameter #47 collects*, and every draw made from it belongs to #27/#30. **#16 is untouched.**

**Authored names route through #49's seam as slot values, not as translation targets** — and it is worth
being exact, because FR-LC-001 says *"**all** user-facing text"* and a club name is user-facing. The
resolution is not an exemption: #49's `NamedSlotSet` carries proper nouns as **already-formatted string
values** (that is precisely how #22 passes `SubjectName`/`OpponentName` today), so an authored name reaches
the player *through* the seam while never being a `LocalizationKey` and never being translated. FR-LC-001 is
satisfied by routing, not by translating.

What follows for #47: authored names are stored as authored (no locale baked, no key allocated), and the
sub-blob stays locale-independent under FR-LC-006. A club called "Deportivo" is called that in every
locale — correct for a proper noun, and the same treatment `ClubNameCatalogue` entries already get.

### KD-7 — Behaviour-neutral identity

A generated game started through #47's setup flow calls the same `LeagueBootstrap.Generate` with the same
parameters and is **byte-identical** to one started in code: no sub-blob is written (KD-1(ii)), no stream is
registered, no `PlayerRecord` or draw budget changes, and the golden vector is untouched. #47's entire
save-format footprint is conditional on the user having authored something.

## 5. Persistent state (shape)

```
NewGameConfig      : { WorldSeed (ulong), ClubCount (int), ManagedClubId (int),
                       HasAuthoredDb (bool) }              # a transient handoff value, not saved

AuthoredDatabase   : { Clubs[]   : { ClubId (int), Name (string) },   # no strength field: the constructed
                                                                     # Club takes StrengthDelta = 0 (KD-1(i))
                       Squads[]  : PlayerDatabase.Squad,   # #27's type, not a parallel model
                       NationPins[] : (PlayerId, NationId) }   # §2(e) — #36's table, authored entries
```

The authored database is serialized **only for an authored game** (KD-1(ii)) under
`AUTHORED_DB_SAVE_FORMAT_VERSION`, version gate first, overflow-safe `Require(offset, need, total)` length
prefixes against `total − offset`, trailing-byte guard, fail loud on all three, APPEND-only layout. Entries
are in canonical ascending `ClubId` / `PlayerId` order with a fail-loud non-ascending gate, so two equivalent
databases cannot serialize differently.

**It stores #27's `Squad` type, not a #47 copy of it.** A parallel player model in the editor is the
duplicate-truth failure this project has hit before (`PlayerAttributes` vs `AgentMovement.PlayerAttributes`),
and it would silently diverge the moment #27 adds a field.

## 6. Determinism posture

- Tooling: no stream, no tag, no ordinal (KD-6); the world seed is an input.
- Authoring is human-driven and outside the tick loop entirely.
- A **generated** game is byte-identical to pre-#47 (KD-7).
- An **authored** game is deterministic from its saved data: same authored database ⇒ same `League` ⇒ same
  season, with no dependence on generation order.
- The authored sub-blob round-trips byte-identically and is canonically ordered.

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `SquadFileWriter.Write(Squad) → string` | #47 → #27's format | **the missing half** (§2(b)); correctness = `Parse(Write(s)) == s` (KD-2). `Squad` is a sealed class, so no `in` modifier |
| `SquadFileLoader.Parse` | #27 (existing) | the **only** validation authority (KD-2) |
| `NewGameConfig` (value) | #47 → root | the handoff; #47 references no sim loop (KD-5) |
| `AuthoredDatabase` load/save | #47 ↔ artifact | the deep-tier authored artifact (KD-1) |
| Editor view models + edit commands | #47 ↔ #38 | `IViewModelSource<T>` in, commands out (KD-4) |

## 8. Cross-spec back-props

### 8.1 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-030-017** | #30 §3 / the season-save composition | Record that `SeasonSaveCodec` composes an **optional** `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob, present only for an authored game (KD-1(ii)), and that a **generated** game's frame is unchanged. This is the one place #47 touches the save, and it is conditional. |
| **ERR-030-018** | `season-save` / `League` | An **authored-source factory** for `League` (`Club[]` + `Squad[]` in, no strength ramp applied — KD-1(i)). `League`'s constructor is `internal`, so this must live in `season-save`; #47 supplies values and the root calls it. Also records that a `League` built this way is `ISquadProvider`-identical to a generated one, which is what keeps every downstream consumer source-agnostic. |

### 8.2 Deferred (land at the named tier)

- The authored sub-blob's `SEASON_SAVE_FORMAT_VERSION` bump, when the deep tier first composes it in.
- Custom-competition genesis-config authoring, on #43's FR-CP-004 shape (KD-3).
- The Stage-0+1 text→binary parser swap: the editor binds to loader **types**, so the swap is #27's and
  leaves #47's contract intact (§2(c)).

### 8.3 Explicitly **not** back-props

- **#27** — nothing to change. The writer (§2(b)) is a **new surface #47 adds over #27's format**, not an
  amendment to #27's model or grammar; it belongs with the editor because #27 has no writer *because* it
  never needed one.
- **#36** — nothing to change. Authored nationalities are entries in the `NationPin` table #36 already
  ships (§2(e)). **#47 does owe the precedence rule #36 names:** an authored pin is **overwritten** by a
  later transfer re-key pin, because the re-key is a live event about a player who has moved and the authored
  value described his starting state. Recorded here so #36's open question closes.
- **#43** — nothing to change; custom competitions write the genesis config FR-CP-004 already defines.
- **#16** — no row, no reservation, nothing needed (KD-6).
- **#50** — an authored database is an input artifact, not a live save; migrating *saves* stays #50's, and
  migrating an authored **file** across format versions is #47's own concern at the deep tier.

## 9. Test focus

**The KD-2 round-trip, which is the load-bearing one:** `Parse(Write(squad)) == squad` field-for-field over
a corpus including every boundary value the loader gates (this is the lock that caught `SquadFileLoader`'s
club-scoping defect at #27 T0; its `age`-bound defect escaped to a later review, which is why the lock
matters — §2(d)); and malformed authored data fails loud **through the loader**, with no
editor-side path that accepts what `Parse` rejects. **The KD-7 identity lock:** a generated game started
through #47's setup flow produces a byte-identical save and a byte-identical `League` to one started in
code, and writes **no** authored sub-blob — plus the `LeagueBootstrapGoldenVectorTests` digest asserted
unchanged in #47's own suite (the #36 precedent: make the cost of touching generation visible in the
consumer's tests too). Authored round-trip determinism over the sub-blob, including canonical ordering and
the non-ascending fail-loud gate; an authored save loading **without the source file present** (KD-1(ii)'s
self-containment — the property the rejected hash-reference design would have failed); authored-vs-re-key
pin precedence (§8.3); setup-parameter refusal delegated to `LeagueBootstrap.Generate` (out-of-range
`clubCount` throws from there, not from #47); and **structural** assertions that #47's data layer references
**`player-database` and nothing else** — explicitly not `season-save` (the AR-1 finding: it transitively
pulls `MatchEngine` and `LivingWorld`, so an editor would depend on the whole sim), not a sim loop, and not
`TacticalDirector.Localization`.

## 10. Reference DAG

```
#38 (editor screen) → {#47-data, ui-framework}
#47-data → {player-database}
root → {#47-data, season-save, #30, …}        #47 → { } toward the sim
```

**Acyclic, and #47's data layer is a leaf over #27 alone.** It does **not** reference `season-save`: because
`League`'s constructor is internal there (KD-1(i)), #47 produces `Club[]`/`Squad[]` values and the **root**
calls the authored-source factory. Had #47 constructed the `League` itself it would have needed a
`season-save` reference — and `season-save` references `MatchEngine` and `LivingWorld`, so an editor would
have transitively depended on the whole sim to author a text file.

## 11. Risks and standing options

- **R-1 — the authored save-format consequence will be resisted** (KD-1(ii)), because "the editor writes a
  file, the game reads the file" is the intuitive model and it is exactly what §2(a) forbids from surviving a
  save/load. The failure mode is silent: an authored career would load with **generated** rosters and look
  merely "wrong" rather than broken. §9's load-without-the-source-file test is what catches it.
- **R-2 — a parallel data model in the editor** (§5). The `PlayerAttributes` collision is the precedent; the
  mitigation is that the artifact stores #27's `Squad` outright.
- **R-3 — editor-side validation will be added for UX and become a second authority** (KD-2). Stated as
  "a check that disagrees with the loader is a bug in the check" rather than "do not add checks", because
  the UX need is legitimate and an absolute prohibition would just be ignored.
- **R-4 — partial authoring is not designed here.** A database that authors *some* clubs and generates the
  rest is neither KD-1(i)'s source nor #36's sparse overlay, and it would re-open the coupling KD-1 rejects.
  If it is wanted, it is its own decision with its own determinism argument. Standing option, deliberately
  not smuggled in.
- **R-5 — the editor is a large UX surface and a small spec.** As with #48, the contract is modest and the
  interface work is not; the spec should not imply otherwise.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.6.** AR-1 (0H+2M) → v0.2, AR-2 (0H+2M+1L) → v0.3,
   AR-3 (0H+2M+1L) → v0.4, AR-4 (0H+1M+1L) → v0.5, AR-5 (0H+0M+2L) → v0.6 = **CONVERGENCE** (an L-only
   round closes the cycle, per the project convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/new-game-setup-db-editor/`, FR
   prefix `FR-ED`.
3. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** at promotion.
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`**, landing the §8.1 back-prop atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. **The plan's §4 is refuted:** it claims the editor *"adds no new save block"*, but this project **regenerates rosters from the world seed rather than saving them** (`SeasonSaveCodec` carries no roster data; `LeagueBootstrapGoldenVectorTests` pins the generation), so an authored player — not derivable from any seed — cannot survive a save/load without being persisted. **KD-1** follows that through: an authored database is a **second source for `League`** (never a patch over the generator, which would re-couple authored data to the draw order the golden vector freezes), and an authored game writes an optional `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob so the career is self-contained — with the smaller hash-plus-external-file design rejected on the `MatchSaveManager` self-sufficiency precedent. A **generated** game writes nothing and stays byte-identical, so the plan's claim is preserved exactly where it was true. Second gap: the plan calls the loader seam a *"read/write contract"* when `SquadFileLoader` is **parse-only** — #47 must supply the writer, whose correctness condition is `Parse(Write(s)) == s` (**KD-2**), the same round-trip that caught both of `SquadFileLoader`'s historical defects. #36's `NationPin` table absorbs authored nationalities, and #47 answers the precedence question #36 left open (a later re-key pin overwrites an authored one). This is the **third** instance of *cannot-be-recomputed ⇒ must-be-persisted* (#44, #46, now #47), and it is named as a pattern. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 2M, both resolved.** **M-1** — KD-1(i) had `League` "gain a second origin" while §8 filed no back-prop for it: `League`'s constructor is **`internal` to `season-save`**, so an authored-source factory is a `season-save` change (now `ERR-030-018`). The fix also **narrows the DAG** — #47 supplies `Club[]`/`Squad[]` values and the root constructs, so #47 references `player-database` alone; had it built the `League` itself it would have taken a `season-save` reference, and `season-save` references `MatchEngine` and `LivingWorld`, making an editor transitively depend on the whole sim to author a text file. **M-2** — the authored artifact dropped `Club.StrengthDelta`, the seeded ramp that stops a generated table being statistically uniform. Authored data specifies attributes directly, so the ramp must **not** be applied (it would re-tune every authored player away from what the author typed): authored clubs take `StrengthDelta = 0`, stated as the one genuine difference between the two `League` origins. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 2M + 1L, all resolved.** **M-1** — KD-4 still said the data layer references *"`season-save` for `League` construction"*, stale against AR-1's own fix (the root constructs, precisely so #47 does not take that reference) and contradicting both KD-1(i) and §10. **M-2** — KD-3 claimed `LeagueBootstrap.Generate` *"already validates all three"* setup parameters; it validates `clubCount`, the managed club is gated by `League.CreateSeason`, and `worldSeed` needs no gate at all. Restated per-parameter, with the rule that #47 surfaces the consumer's exception rather than pre-checking — KD-2's validation-authority discipline applied to the setup flow. **L-1** — KD-2 claimed the round-trip test caught **both** historical `SquadFileLoader` defects; §2(d) records that one was found by adversarial review. Corrected, and turned into the sharper point: the review-found one is the argument for having the lock. |
| v0.4 | July 26, 2026 | **AR-3 fix pass: 0H + 2M + 1L, all resolved.** **M-1** — §9 still asserted that *"the two historical `SquadFileLoader` defects were both caught this way"*, the exact claim AR-2 corrected in KD-2 and did not propagate here — the same fix-one-place-miss-the-other pattern the full re-read keeps catching. **M-2** — §9's structural assertions listed sim-loop / `MatchEngine` / `Localization` but **not `season-save`**, which is the reference AR-1 specifically established #47 must not take (it transitively pulls `MatchEngine` and `LivingWorld`); the test that would catch a regression of AR-1's fix was missing from the test list. **L-1** — §5's `Clubs[]` comment read as though the artifact carried a strength field set to zero; clarified that it carries none and the constructed `Club` takes `StrengthDelta = 0`. |
| v0.5 | July 26, 2026 | **AR-4 fix pass: 0H + 1M + 1L, both resolved.** **M-1** — KD-6 claimed authored club names sit outside #49 because *"FR-LC-001 governs user-facing interface text"*, which is an exemption FR-LC-001 does not grant: it says **all** user-facing text, and a club name is user-facing. Replaced with the accurate mechanism — proper nouns travel through the seam as `NamedSlotSet` **slot values** (exactly how #22 passes `SubjectName`/`OpponentName`), so the name is routed without being a `LocalizationKey` and without being translated. Satisfying a MUST beats arguing around it, and the corollary matters for §5: names are stored as authored, with no locale baked. **L-1** — KD-5 described `NewGameConfig` as carrying an *"optional authored database"* while §5 gave it a `HasAuthoredDb` flag; reconciled — the flag selects the branch and the artifact travels beside the config. |
| v0.6 | July 26, 2026 | **AR-5 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — §3's minimal-tier row still said the setup parameters are *"everything `LeagueBootstrap.Generate` already accepts"*, the phrasing AR-2 corrected in KD-3 (the managed club is `CreateSeason`'s); pointed at KD-3 rather than restating it, so the two cannot drift again. **L-2** — the writer surface was typed `Write(in Squad)`, but `Squad` is a sealed **class**, where an `in` modifier is legal and meaningless. |
