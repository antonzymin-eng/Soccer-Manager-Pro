# Save Migration & Versioning #50 — Section 1: Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 1.1 Purpose

**#50 decides whether a save file written by an older build may be opened, and turns it into bytes the
current codecs accept.** It owns the version **classification** of a save on load, the **migration
transform chain**, the **refusal policy**, and — per KD-2 — the **generation version** that makes
derived-not-stored data migratable at all.

It exists because the project currently has the *opposite* of migration, everywhere, on purpose: every
codec reads its own version first and **refuses** anything else. That posture is correct while nothing has
shipped, and becomes a career-destroying one the day it has.

## 1.2 In scope / out of scope

**In scope**

- Classification of a save into `Current` / `Migratable` / `TooNew` / `Unsupported` / `Corrupt`, from
  version fields alone.
- The **registry and runner** for per-blob migration steps.
- `WORLD_GENERATION_VERSION` and the gate over regenerated-not-persisted data (KD-2).
- The refusal policy, and the non-destructive write discipline around a successful migration.
- The version **comparison** #39's cloud-conflict UX needs (KD-5).

**Out of scope**

| Not owned | Owner | How #50 relates |
|---|---|---|
| The save formats and their version constants | each owning spec — #30, #22, match-engine, and every per-spec sub-blob | #50 migrates **across** bumps; it defines none (KD-3) |
| The generators, **including the frozen old versions** | **#27 / #30** | they stay in their owning assemblies; #50 holds registered **delegates**, never the code (KD-2 / §4.4) |
| Corruption detection | the codecs' existing fail-loud gates | #50 classifies **before** them, never instead of them (KD-1) |
| The data a transform produces | the spec that made the bump | it writes the step; #50 runs the chain (KD-3) |
| Cloud storage and conflict UX | **#39** | #50 supplies the comparison; #39 owns the interaction (KD-5) |
| User-facing refusal text | **#49** | #50 emits an identity + slots; #49 renders (KD-4) |

**The generators being out of scope is not a formality.** §4.4 shows that #50 holding old generator code
directly would make the migration layer transitively depend on the entire simulation in order to open a
file.

## 1.3 Dependencies

| Spec | Relationship |
|---|---|
| **#30** Season & Competition Loop | Owns `SeasonSaveCodec` and the outer frame the `SaveOriginStamp` lands in (ERR-030-019). |
| **#27** Squad / Player Data Layer | Owns `RosterGenerator`; `ERR-027-003` records that its draw contract is under `WORLD_GENERATION_VERSION`. |
| **#22** Living World | Owns `WORLD_STORE_FORMAT_VERSION` / `WORLD_SNAPSHOT_FORMAT_VERSION`. |
| **#16** Deterministic Simulation | **Untouched** — no stream, no domain tag, no ordinal (KD-7). |
| **#49** Localization | #50 is a text **producer**; refusal messages are identities + slots. |
| **#39** Steam Packaging & Release | **Consumes** #50's comparison for cloud conflict (KD-5). |
| Every spec with a sub-blob | Supplies its own migration steps at its own bumps (KD-3). |

## 1.4 What already exists (verified, not assumed)

**(a) The version surface is 12 constants in shipped code and 25 across the specs.** Shipped:
`SEASON_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`, `WORLD_STORE_FORMAT_VERSION`,
`WORLD_SNAPSHOT_FORMAT_VERSION`, `MATCH_SAVE_FORMAT_VERSION`, `SNAPSHOT_SCHEMA_VERSION`,
`PROGRESSION_SAVE_FORMAT_VERSION`, `PHASE_A_PAYLOAD_FORMAT_VERSION`, `RECORD_FORMAT_VERSION`,
`FIELD_WIDTH_SCHEMA_VERSION`, `SCENARIO_MANIFEST_FORMAT_VERSION`, `SCHEMA_VERSION`. Specified-but-unbuilt
adds thirteen more sub-blob versions, and **this wave alone added four**.

**Consequence:** "the migration chain" is a **per-blob family of chains** whose count grows with every
management spec. KD-3's granularity decision is therefore not a refinement — it is what keeps #50
implementable at all.

**(b) There is no migration machinery whatsoever, and the current posture is its opposite.** A tree-wide
search for `Migrat` / `Upgrade` in `src/` returns nothing. The root `CLAUDE.md` states the posture
repeatedly in the same words — *"a v1 file is rejected fail-loud, no Stage-0 migration"*, *"v2 payloads
rejected fail-loud, no migration"*.

**Consequence:** #50 does not extend a mechanism; it introduces one **in front of** ten-plus codecs that
all currently answer *"wrong version ⇒ throw"*. That is exactly why KD-1's seam must sit outside them and
must not weaken a single gate.

**(c) The blind spot — generation is unversioned, and it is the biggest save-visible surface there is.**
Rosters are **not saved**. `WorldStore.WorldSeed`'s own doc comment states the mechanism at source:

> *"Load-bearing beyond this assembly: it is the only value in a save file from which a league's rosters
> can be regenerated. **Squads are not persisted**, so resuming a career means calling
> `LeagueBootstrap.Generate(world.WorldSeed, season.ClubCount)` to rebuild the `ISquadProvider` that
> `SeasonSaveManager.Load` needs."*

A career's entire playing population is therefore a function of **two saved integers and the generator's
current code**. The root `CLAUDE.md` records the hazard: a draw-order change, a catalogue reorder or a
one-line `[GT]` tweak *"would silently rewrite every club in every existing save with the whole suite
green"* — closed by KD-10 plus `LeagueBootstrapGoldenVectorTests`.

**That guard is a test, not a runtime gate.** It stops an *accidental* change landing unnoticed in CI. It
does nothing about a **deliberate** change shipped in an update — which is precisely #50's domain. And the
class is wider than rosters: #32's knowledge bands and #36's nationality derivation are both computed on
read from catalogues and `[GT]` tables, with the same property.

**Consequence:** without KD-2, #50 would migrate all 25 formats perfectly and still hand the player a
career whose squads had silently changed — the failure #50 exists to prevent, arriving through the one
door it was not watching.

**(d) The frame is version-first and its sub-blobs are opaque.** `SeasonSaveCodec` writes
`SEASON_SAVE_FORMAT_VERSION` as the **first field**, then a flag byte, then length-prefixed sub-blobs it
*"never parses"*; every sub-blob codec likewise reads its own version first.

**Consequence:** classification is cheap and safe — the outer version is readable without trusting anything
after it, and each sub-blob's version is readable without parsing its body. KD-1 and KD-3 both rest on a
property the format **already has**, rather than one #50 must ask for.

**(e) At least one specified bump is not purely structural.** #45's `ERR-030-009` turns #30's
`JobSecurity` from a `float` scalar into a **derived enum band** over #45's confidence — a representation
change carrying a `SEASON_STATE_FORMAT_VERSION` bump, *"with no migration path"* in its own words.

**Consequence:** a real transform must sometimes **synthesize** a value the old save does not contain.
That bounds what migration can promise (KD-6), and is the honest counter-example to *"migration is a
structural rewrite"*.

## 1.5 Key decisions

### KD-1 — Classification sits **in front of** the codecs and never relaxes them

A `SaveVersionClassifier` reads only version fields — safe per §1.4(d) — and returns one of five classes
(Appendix D). **The codecs are untouched:** a migrated blob is handed to the *current* codec and must pass
its existing fail-loud gates like any other input.

**That is the property which makes the seam safe to add in front of ten codecs.** #50 can only ever
produce bytes those codecs already accept, so a migration bug surfaces as a **refusal**, not as a corrupt
career.

**`TooNew` is a separate class from `Corrupt` on purpose.** They demand the same refusal but different
messages: *"this save is from a newer version of the game"* is actionable; *"this save is damaged"* is not
— and telling a player the wrong one is how a recoverable situation becomes a deleted file.

**The conservative-classification rule:** a save is `Migratable` **only** on an exact, registered version
match at every level; anything unrecognised is refused. Mis-classifying corrupt data as migratable would
run a transform over garbage and write a plausible-looking career, and that asymmetry is why the default
is refusal.

### KD-2 — A **generation version**, because formats are not the only thing that changes

`WORLD_GENERATION_VERSION` `[FIXED]` is stamped into the save at genesis and covers **everything that
regenerates rather than persists**: `RosterGenerator`'s draw order and budget, `LeagueBootstrap`'s
catalogues and strength ramp, and the derived-on-read tables (#32's band widths, #36's `NationCatalogue`
and weighting). On load: **equal** ⇒ proceed; **older with a registered generation migration** ⇒
**materialise** the affected data into the save and stamp the current version; **older with none** ⇒
**refuse**.

**Materialisation is the only honest repair, and it is the fourth appearance of this pattern.** #44's
tally, #46's inbox items and #47's authored rosters all reached the same place — *the source is gone, so
the data must be persisted*. Here the source is not gone but has **changed meaning**, which amounts to the
same thing. The mechanism already exists: #47's authored sub-blob holds exactly this shape of data, so a
generation migration writes an authored-style blob and the career continues as if authored.

**But materialisation needs the OLD generator, and the new build does not have it. This is the cost, and
it is paid explicitly.** To write the v(N) rosters into the save, something must *produce* them, and only
v(N)'s generator code can — the seed alone is not enough once the code has changed. So a generation
migration is possible **only** for versions whose generator the build still ships. `GenerationRegistry`
therefore retains **every generator version back to the supported floor**, and a migration runs the old
generator once to materialise, then never again.

Three consequences, stated rather than discovered:

- Old generator code is **retained, frozen, and covered by its own golden vector** — it is now
  save-format code, not live code, and editing it is as breaking as editing a codec.
- The supported floor (R-5) is not merely a test-surface decision: it is **how many generator versions the
  build carries**. That is the real cost of a long support window, and the argument for a short one.
- A generation version whose generator has been dropped classifies `Unsupported` and is **refused** —
  which is the correct outcome, and why that branch is not a placeholder.

*Rejected:* **materialise every save eagerly at save time**, so no old generator is ever needed. It writes
every club's full roster into every save forever to insure against a change that may never happen,
discarding the regenerate-don't-save design and its save-size benefit to solve a problem that exists only
at a bump. Retaining N generators is bounded by the floor; inflating every save is not.

*Rejected:* **forbid generation changes post-ship.** Unenforceable — balance work will want them, and a
rule that must never be broken will be broken quietly.

**A generation bump is expensive by design, and that is the feature.** It makes *"just tweak the `[GT]`
ramp"* visible as a decision that costs every existing career either a materialised blob or a refusal.
Today that decision is invisible and guarded only by a test.

### KD-3 — Migration is **per-blob**, versioned independently, composed by chains

A transform is registered for one `(blobKind, fromVersion)` and produces `fromVersion + 1` of **that blob
only**. The frame's opaque sub-blob discipline (§1.4(d)) means a season-state bump requires **no**
knowledge of the world-store, match or any per-spec blob — they pass through byte-untouched, exactly as
they do today when the frame version bumps around them.

**Ownership is the bumping spec's**, which is what keeps the chain writable: the spec that changes a
layout supplies the step that reads its old layout, because it is the only party that knows both. #50 owns
the **registry and the runner**, not the steps — the same relationship #38 has to screens and #17 to event
types.

**Ordering within one file:** the frame is classified and migrated first (it determines the sub-blob
inventory), then each sub-blob independently. A typical update migrates one blob and copies the rest.

### KD-4 — Refusal is loud, specific, and loses nothing

**A refused save is never modified and never deleted** — the file is left exactly as found, so a player
who upgrades, hits a refusal and reinstalls the old build still has their career. Stated explicitly
because the tempting implementation — *migrate in place, roll back on error* — is precisely the one that
loses data when the rollback is the thing that fails.

A successful migration **writes a new file** and leaves the original until the new one is written and
re-read successfully: the `temp → fsync → rename` discipline `SaveManager` and `MatchSaveManager` already
implement, extended one level so that **the original is the temp's fallback**.

Messages route through **#49** as a producer (identity + slots; the version numbers are slot values), so
#50 bakes no strings. Each refusal class is its own intent, because collapsing them is the failure mode
KD-1 names.

### KD-5 — #39 conflict resolution compares **versions, not timestamps**

Cloud conflict is #39's UX. What #50 owns is the **fact** #39 needs: two saves' classifications and their
relative ordering. Two rules are pinned here:

- a save that is `TooNew` for this build **cannot be resolved by this build at all** — not *"the newer
  copy wins"* but *"this build must not touch it"*, which is a different and safer statement;
- a migrated save is **written back only on the player's next explicit save**, never silently on load.
  Otherwise opening a career on a second machine would rewrite the cloud copy into a format the first
  machine's older build then refuses — turning a read into a data-loss event.

### KD-6 — What migration promises, and what it cannot

**A migrated save is *valid*, not *counterfactually identical*.** Where a bump is purely structural the
migrated bytes equal what the new build would have written for the same career, and that is testable.
Where a bump **synthesizes** — #45's `JobSecurity` float → band (§1.4(e)) is the concrete queued case —
the new value is *a choice made by the transform*, and a career migrated at that point differs from one
played natively through the same fixtures.

That difference is unavoidable, so the contract is stated rather than implied: **migration guarantees the
save loads, is internally coherent, and is deterministic from that point forward.** It does not guarantee
the counterfactual. Forward determinism is what actually matters to a player, and is what §5 tests.

### KD-7 — Determinism posture and identity

Infrastructure: **no RNG stream, no domain tag, no `SubsystemOrdinal`**; #16 has no row for #50 and needs
none. **Two classes of transform, deterministic by different means**, and the distinction must not be
blurred:

- **Format transforms** are pure functions of bytes — no draw, no clock, no simulation — so the same input
  always migrates to the same output, golden-vectorable byte→byte.
- **Generation migrations are *not* byte-pure**: they run a frozen old generator, which draws from its own
  seeded streams. They are still fully deterministic — same seed, same frozen code, same output — but
  their golden vector pins **the generator's output for a pinned seed**, not a byte→byte mapping, and they
  must run against a `DeterministicRngService` exactly as the live generator does.

Calling both *"pure byte transforms"* is the imprecision that produces a test asserting the wrong
property.

**Identity:** with an empty chain, a current-version save classifies `Current`, runs **zero** transforms
and loads through the unmodified codec — byte-identical to pre-#50 — and every non-current save is refused
exactly as today. **The minimal tier's identity claim is unusually strong because refusal *is* today's
behaviour**, so the seam can land before there is anything to migrate.

## 1.6 Staging

| Tier | Content | Behaviour |
|---|---|---|
| **Minimal (the identity)** | The classification seam + an **empty** chain | **Byte-identical to pre-#50** — a current save runs zero transforms, anything else is refused exactly as today |
| **Deep** | Registered per-bump transforms; the KD-2 generation gate; the #39 comparison | Real migration |

**The minimal tier is worth landing early precisely because it changes nothing.** It puts the seam in
place while there is still nothing to migrate, so the first real bump has somewhere to register instead of
provoking an emergency design under release pressure.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 from supplement v0.6 (scope with the generators' ownership called out as load-bearing rather than formal; the five verified facts, with (c) — generation is unversioned and is the biggest save-visible surface — as the spec's reason for existing; KD-1..KD-7, including KD-2's explicitly-paid cost of retaining frozen generators and KD-7's split of the two determinism classes; the two-tier staging whose minimal identity is unusually strong because refusal is already today's behaviour). Status IN REVIEW. |
#endregion
