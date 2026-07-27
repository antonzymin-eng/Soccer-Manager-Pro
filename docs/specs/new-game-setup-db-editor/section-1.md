# New-Game Setup & Database Editor #47 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Purpose

#47 is the front door: the flow that starts a new career — seed, league size, club — and the surface on
which a player can **author** the world's data rather than accept a generated one.

It is a small contract wrapped around one architectural fact that the spec's own plan got wrong. **This
project does not save rosters; it regenerates them from the world seed.** Everything downstream is built
on that: `SeasonSaveCodec` carries no roster data, `LeagueBootstrapGoldenVectorTests` pins the generation,
and #27's draw budget is contract-locked so a change cannot silently rewrite every existing career.

An authored player is, by construction, **not a function of any seed**. So the plan's claim that the
editor *"adds no new save block"* is true exactly where it needs no editor, and false everywhere the
editor matters. KD-1 is that consequence followed through, and it is the single decision #47 turns on.

## 1.2 Scope

**In scope**

- The **new-game setup flow**: world seed, club count, managed club.
- The **authoring surface** over #27's data format — including the **writer**, which does not exist today.
- The **authored-database artifact**, its identity, and its persistence.
- Authored **nationality pins**, as entries in the table #36 already ships.

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #47's relation |
|---|---|---|
| The roster/attribute model and its validation grammar | **#27** | the editor reads and writes that format; it **never redefines** it (KD-2) |
| Generation from a seed (`LeagueBootstrap`, `RosterGenerator`) | **#27 / #30** | authored data is an **alternative source** for the same `League`, **never a patch** of the generator (KD-1) |
| The season loop that plays a database | **#30** | #47 hands over data and references no sim loop (KD-5) |
| Competition instance definitions | **#43** (FR-CP-004, config-assigned at genesis) | custom-league authoring writes that **config**, not a runtime API (KD-3) |
| The UI shell hosting the editor | **#38** | #38 hosts; #47 owns no navigation or layout (KD-4) |
| Rendering text into a locale | **#49** | authored proper nouns travel as **slot values** (KD-6) |
| Live-save migration | **#50** | an authored database is an **input artifact**, not a live save |

## 1.3 Dependencies

**Upstream (consumed):**

- **#27 Squad / Player Data** — `Squad`, `PlayerRecord`, `PlayerAttributes`, and `SquadFileLoader.Parse`.
  **The only assembly #47's data layer references.**

**Downstream (consumers):**

- **The composition root** — consumes `NewGameConfig` and, for an authored game, hands the artifact to
  `season-save`'s authored-source factory (ERR-030-018).
- **#38 UI** — hosts the editor screen over #47's data layer via `IViewModelSource<T>` and commands.
- **#36** — receives authored nationality entries in the `NationPin` table it already ships.

**Reference DAG**

```
#38 (editor screen) → {#47-data, ui-framework}
#47-data           → {player-database}
root               → {#47-data, season-save, #30, …}
```

**Acyclic, and #47's data layer is a leaf over #27 alone.** In particular it does **not** reference
`season-save`: `League`'s constructor is `internal` there, so #47 produces `Club[]` / `Squad[]` **values**
and the **root** calls the factory. Had #47 constructed the `League` itself it would have needed a
`season-save` reference — and `season-save` references `MatchEngine` and `LivingWorld`, so **an editor
would transitively depend on the whole simulation to author a text file.**

## 1.4 What verification changed

**(a) The game does not save rosters — it regenerates them, and that is what makes authoring hard.**
`LeagueBootstrap.Generate(ulong worldSeed, int clubCount) → League` builds an N-club league from one seed;
`League` **is** the `ISquadProvider`; and `SeasonSaveCodec` contains **no roster data at all**. The root
`CLAUDE.md` states the invariant plainly — rosters are *"REGENERATED from the world seed rather than
saved"* — which is why the golden vector exists and why #27's draw budget is contract-locked.

**Consequence — the decision #47 exists to make:** an authored player is not derivable from any seed.
**Either the authored data lives in the save, or an authored career depends on an external file that can
move, change, or disappear.** The plan's *"adds no new save block"* is therefore only true of the case
that needs no editor.

**And the failure mode is silent.** An authored career that did not persist its rosters would **load with
generated ones** and look merely *wrong* rather than broken — no exception, no corruption, just a
different world than the one the player built.

**(b) The authoring format has a parser and no writer.** `SquadFileLoader` exposes exactly
`Parse(string text, int clubId) → Squad`. There is **no** `Write`, `Serialize`, or `ToText` anywhere in
`src/player-database/` or `src/season-save/` for this format.

**Consequence:** the plan's *"read/write contract"* is half-built. #47 must define the **writer**, and its
correctness condition is a **round-trip against the existing parser** — the encode/decode asymmetry class
this project has already been bitten by (#30 T1's `SeasonState`, constructible but not decodable).

**(c) Three supporting facts, each removing a decision rather than adding one.** The authoring grammar is
explicitly *"NOT a determinism-pinned wire format"* and parser-swap-ready, **provided** the editor binds
to the loader's **types** rather than its syntax; validation already exists, is fail-loud, and has had two
defects — one caught by a round-trip test, one only by a later adversarial review; and `LeagueBootstrap`
already validates the setup parameters #47 collects, failing loud with messages naming the constant to
change.

## 1.5 Key decisions

### KD-1 — An authored database is a source for `League`, and an authored game saves its rosters

Two halves, and the second is the one the plan misses.

**(i) Source, not patch.** `League` gains a **second origin** — built from an authored database — beside
`LeagueBootstrap.Generate`. Everything downstream is **source-agnostic**, because everything downstream
already talks to `League` through `ISquadProvider` and `CreateSeason`. The generator is **not modified**,
so a generated game keeps its exact byte-for-byte behaviour and the golden vector is untouched.

**`League`'s constructor is `internal` to `season-save`, so that second origin is a `season-save`
addition, not a #47 one** (ERR-030-018). #47 produces the authored **values** (`Club[]` + `Squad[]`, both
existing types) and the **root** hands them to the factory. This is better than widening `League`'s
constructor: #47 stays a leaf over `player-database` alone, and the assembly that owns `League`'s
invariants keeps sole responsibility for constructing one.

**Authored clubs carry no strength ramp, and that is not an omission.** `Club` holds a `StrengthDelta` —
the seeded ramp `LeagueBootstrap` applies so a generated table is *"not 20 statistically identical
teams"*. An authored database specifies attributes **directly**, so the differentiation is already in the
data; applying a ramp on top would **silently re-tune every authored player away from what the author
typed**. Authored clubs therefore take `StrengthDelta = 0`. This is the one place the two origins
genuinely differ, so it is stated rather than left to be discovered when an authored league plays oddly.

*Rejected:* apply authored data as an **override layer** over generation (generate, then patch). At
database scale a fully authored league is **100% overrides**, so the generator would run only to be
discarded — and the authored result would depend on the generator's **draw order**, re-coupling authored
data to the very thing the golden vector exists to freeze. (The override shape is right for a **sparse**
fact like #36's nationality pins, and #47 uses it there. The distinction is *sparse overlay* vs *whole-
database replacement*.)

**(ii) An authored career's save must carry its rosters.** Since rosters are regenerated rather than
saved (§1.4(a)), an authored game's save needs the data itself. It lands as an opaque, independently
version-gated **`AUTHORED_DB_SAVE_FORMAT_VERSION`** sub-blob composed into #30's `SeasonSaveCodec` (the
#40/#42/#43/#44/#45 pattern), written **only** for authored games:

- a **generated** game writes no sub-blob and is byte-identical to pre-#47 — so the plan's claim is
  preserved exactly where it was true;
- an **authored** game is **self-contained**: it does not depend on the editor, the source file, or the
  machine that made it.

*Rejected:* store a **content hash + external file reference** and fail loud on mismatch (the
`EnvironmentFingerprint` discipline). Smaller in the save, and rejected anyway: it makes a career depend
on a file the player can move, edit or lose, and a hash mismatch would **strand a save with no recovery
path**. The project's own precedent is decisive — `MatchSaveManager` deliberately made the match file
self-sufficient by carrying the boot seed rather than referencing it, and the season save is *"one file"*.

**This is the third instance of one pattern** (#44's discipline tally, #46's inbox items, now authored
rosters): *the thing cannot be recomputed, therefore it must be persisted.* Worth naming, because the
instinct each time was to derive.

### KD-2 — #27's loader is the single validation authority; the writer is validated by round-trip

The editor performs **no validation of its own**. It writes text and hands it to `SquadFileLoader.Parse`;
if the parse throws, the data was invalid. This avoids the two-sources-of-truth drift the plan names — and
the risk is not hypothetical, since the loader's **own** gates have needed correcting twice.

**The writer's correctness condition is a round-trip, not a review:** for any `Squad` the parser accepts,
`Parse(Write(squad)) == squad` **field-for-field**. That single property covers the encode/decode
asymmetry class, and it is the test that caught `SquadFileLoader`'s club-scoping defect at #27 T0. The
loader's *other* historical defect — an unbounded `age` — escaped to a later adversarial review, **which
is the argument for having the lock rather than relying on review.**

**Editor-side checks are a UX affordance, never an authority.** An editor may grey out an out-of-range
value *before* the user commits it, but the commit still goes through `Parse`, and **a check that
disagrees with the loader is a bug in the check**. Stated this way round because the UX need is
legitimate, and "do not add checks" would simply be ignored.

### KD-3 — Minimal is generated-world setup only; custom competitions are #43-gated

The setup flow collects `worldSeed`, `clubCount` and the managed club, and **each is already gated by the
code that consumes it**: `clubCount` by `LeagueBootstrap.Generate` (including its name-catalogue and
`MaxRngStreams` refusals), the managed club by `League.CreateSeason(managedClubId)`, and `worldSeed` by
nothing, because **every `ulong` is valid**.

**#47 adds a front-end and no gate of its own.** Where it must tell the user *why* a value was refused, it
**surfaces the exception the consumer already throws** rather than pre-checking — KD-2's
validation-authority rule applied to the setup flow.

Custom leagues and cups are deep-tier and gated on #43: authoring them means writing the **genesis
config** FR-CP-004 describes (*"`CompetitionId` MUST be config-assigned at genesis"*), not driving a
runtime API. The ordering is already satisfied, since #43 is APPROVED.

### KD-4 — The editor is a #38-hosted mode over #47's own data layer

The **data layer** — parse, write, validate-by-parse, the authored artifact — is a **non-UI assembly**.
The editor **screen** is a #38 screen consuming it through `IViewModelSource<T>` and dispatching edits as
commands.

So #38 owns navigation, layout and input; #47 owns the format and the artifact; **no data-model logic
lives in the presentation layer**; and the editor is **separable** — a headless authoring run is possible,
because the data layer has no UI dependency.

**#47's data layer references `player-database` and nothing else** — not `season-save` either, since the
root constructs the `League` (KD-1(i)). No sim loop, no `MatchEngine`, no `Localization`.

### KD-5 — Handoff is a value artifact; #47 never references #30

Setup produces a `NewGameConfig` — `{ worldSeed, clubCount, managedClubId, hasAuthoredDb }` — a plain
value, with the `AuthoredDatabase` itself **travelling beside it** rather than embedded, so a generated
setup carries nothing. The flag selects the branch.

The **root** consumes it: generated ⇒ `LeagueBootstrap.Generate`; authored ⇒ `League` from the artifact.
#47 references neither #30 nor the composition root — exactly as #46's projectors and #49's adapters
invert their directions.

### KD-6 — Determinism: tooling, and the seed is an input rather than a draw

No RNG stream, no domain tag, no `SubsystemOrdinal`; the roadmap classifies the editor as **tooling**, and
#16's catalogue has **no row and no `_RESERVED_` placeholder** for #47. Authoring is human-driven; the
world seed is a **parameter #47 collects**, and every draw made from it belongs to #27/#30. **#16 is
untouched.**

**Authored names route through #49's seam as slot values, not as translation targets** — and it is worth
being exact, because FR-LC-001 says *"**all** user-facing text"* and a club name is user-facing. The
resolution is **not an exemption**: #49's `NamedSlotSet` carries proper nouns as **already-formatted
string values** (precisely how #22 passes `SubjectName` / `OpponentName` today), so an authored name
reaches the player **through** the seam while never being a `LocalizationKey` and never being translated.
**FR-LC-001 is satisfied by routing, not by translating.**

What follows for #47: authored names are stored **as authored** — no locale baked, no key allocated — and
the sub-blob stays locale-independent under FR-LC-006. A club called *"Deportivo"* is called that in every
locale, which is correct for a proper noun and is the same treatment `ClubNameCatalogue` entries already
get.

### KD-7 — Behaviour-neutral identity, and its precise condition

A **generated** game started through #47's setup flow calls the same `LeagueBootstrap.Generate` with the
same parameters and is **byte-identical** to one started in code: no sub-blob is written, no stream is
registered, no `PlayerRecord` or draw budget changes, and **the golden vector is untouched**.

**#47's entire save-format footprint is conditional on the user having authored something.** That
conditionality is the claim a reviewer should check first — which is why it is a key decision rather than
a clause inside KD-1 — and §5.2 asserts it directly, including the negative half: a generated game writes
**no** authored sub-blob at all, not an empty one.

## 1.6 Determinism posture

- **Tooling**: no stream, no tag, no ordinal (KD-6). The world seed is an **input**; every draw from it
  belongs to #27/#30.
- **Authoring is human-driven and outside the tick loop entirely** — #47 has no cadence.
- A **generated** game is byte-identical to pre-#47 (KD-7), golden vector included.
- An **authored** game is deterministic **from its saved data**: the same authored database yields the
  same `League`, the same season, and **no dependence on generation order** — which is precisely what the
  rejected override design would have destroyed.
- The authored sub-blob round-trips byte-identically and is **canonically ordered**, so two equivalent
  databases cannot serialize differently.
- **All state is locale-independent** (FR-LC-006): authored names are stored as authored, with no locale
  baked and no key allocated (KD-6).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope, out-of-scope table, leaf DAG with the transitive-`season-save` argument, §1.4's verification findings — the regenerated-rosters consequence and the missing writer — KD-1..KD-6 from supplement v0.6 plus **KD-7** promoted to its own decision, determinism posture). KD-7 is separated because the *conditionality* of #47's save footprint is what a reviewer checks first, and §1.4(a)'s silent failure mode (an authored career loading with generated rosters) is stated where the decision is made rather than only in the risk list. Status IN REVIEW. |
#endregion
