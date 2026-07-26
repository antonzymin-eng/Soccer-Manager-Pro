# Save Migration & Versioning #50 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.6 — AR-5 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.5 AR-4, v0.4 AR-3, v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.6
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#50** · **FR prefix:** `FR-MG` · **Wave:** 8 · **Tier:** S2
> **Promoted from:** `docs/tracking/spec-plans/spec-50-save-migration-versioning.md` v0.1

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #50 plan defers, against **verified** upstream source
rather than assumption. Design only — no code, no section files, no registry row.

The plan is accurate about what exists and correct about the shape of the answer. What verification adds is
**scale and a blind spot**:

- The version surface is **larger than the plan's three examples** — 12 format versions in shipped code and
  25 named across the specs (§2(a)). A per-version migration chain is 25 chains, not one.
- **The largest category of save-visible change in this project is not versioned at all.** Rosters are
  *regenerated from the seed*, not saved, so a generator change rewrites every existing career — and no
  format version covers it, because nothing is serialized (§2(c)). A #50 that migrates only formats would
  deliver a perfectly migrated save containing a different squad. **KD-2** closes it.

## 1. Scope

**#50 owns:** the **version classification** of a save on load, the **migration transform chain**, the
**refusal policy**, and — per KD-2 — the **generation version** that makes derived-not-stored data
migratable at all.

**#50 does not own:**

| Not owned | Owner | How #50 relates |
|---|---|---|
| The save formats and their version constants | each owning spec (#30, #22, match-engine, and the per-spec sub-blobs) | #50 migrates **across** bumps; it defines none (KD-3) |
| The generators themselves, including frozen old versions | **#27 / #30** | they stay in their owning assemblies; #50 holds registered delegates, never the code (KD-2/§10) |
| Corruption detection | the codecs' existing fail-loud gates | #50 classifies **before** them, never instead of them (KD-1) |
| The data a transform produces | the spec that made the bump | it writes the step; #50 runs the chain (KD-3) |
| Cloud storage and conflict UX | **#39** | #50 supplies the version comparison #39's conflict rule needs (KD-5) |
| User-facing refusal text | **#49** | #50 emits an identity + slots; #49 renders (KD-4) |

## 2. What already exists (verified)

**(a) The version surface is 12 constants in code and 25 in the specs.** Shipped:
`SEASON_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`, `WORLD_STORE_FORMAT_VERSION`,
`WORLD_SNAPSHOT_FORMAT_VERSION`, `MATCH_SAVE_FORMAT_VERSION`, `SNAPSHOT_SCHEMA_VERSION`,
`PROGRESSION_SAVE_FORMAT_VERSION`, `PHASE_A_PAYLOAD_FORMAT_VERSION`, `RECORD_FORMAT_VERSION`,
`FIELD_WIDTH_SCHEMA_VERSION`, `SCENARIO_MANIFEST_FORMAT_VERSION`, `SCHEMA_VERSION`. Specified but unbuilt
adds thirteen more sub-blob versions (`ACADEMY_`, `BOARD_`, `COMPETITION_`, `DISCIPLINE_`, `FINANCE_`,
`HUMAN_SYSTEMS_`, `MEDICAL_`, `SCOUTING_`, `STAFF_`, `TRAINING_`, `TRANSFERS_`, plus `INBOX_` / `MEDIA_` /
`NATIONAL_TEAM_` / `AUTHORED_DB_` from this wave's supplements).

**Consequence:** "the migration chain" is a **per-blob family of chains**, and the number of them grows with
every management spec. KD-3's granularity decision is therefore not a refinement — it is what keeps #50 from
being unimplementable.

**(b) There is no migration machinery whatsoever, and the current posture is the opposite of migration.**
A tree-wide search for `Migrat` / `Upgrade` in `src/` returns **nothing**. Every codec gates its version and
**refuses**; the root `CLAUDE.md` says so repeatedly in the same words — *"a v1 file is rejected fail-loud,
no Stage-0 migration"*, *"v2 payloads rejected fail-loud, no migration"*.

**Consequence:** #50 does not extend an existing mechanism; it introduces one **in front of** ten-plus
codecs that all currently answer "wrong version ⇒ throw". That is exactly why KD-1's classification seam
must sit *outside* the codecs and must not weaken a single one of their gates.

**(c) The blind spot: generation is unversioned, and it is the biggest save-visible surface there is.**
Rosters are **not saved**. `WorldStore.WorldSeed`'s own doc comment states the mechanism at source:

> *"Load-bearing beyond this assembly: it is the only value in a save file from which a league's rosters can
> be regenerated. **Squads are not persisted**, so resuming a career means calling
> `LeagueBootstrap.Generate(world.WorldSeed, season.ClubCount)` to rebuild the `ISquadProvider` that
> `SeasonSaveManager.Load` needs."*

So a career's entire playing population is a **function of two saved integers and the generator's current
code**. The root `CLAUDE.md` records the hazard that follows: a draw-order change, a catalogue reorder, or a
one-line `[GT]` tweak *"would silently rewrite every club in every existing save with the whole suite
green"* — closed by KD-10 plus `LeagueBootstrapGoldenVectorTests`.

**That guard is a test, not a runtime gate.** It stops an *accidental* change from landing unnoticed in CI.
It does nothing about a **deliberate** change shipped in an update — which is precisely #50's domain. And
the class is wider than rosters: #32's knowledge bands and #36's nationality are both *derived on read* from
catalogues and `[GT]` tables, with the same property.

**Consequence:** without KD-2, #50 would migrate all 25 formats perfectly and still hand the player a career
whose squads had silently changed — the failure #50 exists to prevent, arriving through the one door it
wasn't watching.

**(d) The frame is version-first and its sub-blobs are opaque.** `SeasonSaveCodec` writes
`SEASON_SAVE_FORMAT_VERSION` as the **first field**, then a flag byte, then length-prefixed sub-blobs it
*"never parses"*. Every sub-blob codec likewise reads its own version first.

**Consequence:** classification is cheap and safe — the outer version is readable without trusting anything
after it, and each sub-blob's version is readable without parsing its body. KD-1 and KD-3 both rest on this,
and it is a property the format already has rather than one #50 must ask for.

**(e) At least one specified bump is not purely structural.** #45's `ERR-030-009` turns #30's `JobSecurity`
from a `float` scalar into a **derived enum band** over #45's confidence — a representation change carrying a
`SEASON_STATE_FORMAT_VERSION` bump, *"with no migration path"* in its own words.

**Consequence:** a real transform must sometimes **synthesize** a value the old save does not contain. That
bounds what migration can promise (KD-6) and is the honest counter-example to "migration is a structural
rewrite".

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | The classification seam + an **empty** chain: a current-version save classifies as `Current` and runs zero transforms; anything else is refused exactly as today. Byte-identical to pre-#50 behaviour, because the refusal *is* today's behaviour. |
| **Deep** | Registered per-bump transforms; the generation-version gate (KD-2); the #39 conflict comparison (KD-5). |

The minimal tier is worth landing early **precisely because it changes nothing**: it puts the seam in place
while there is still nothing to migrate, so the first real bump has somewhere to register instead of
provoking an emergency design.

## 4. Key decisions

### KD-1 — Classification sits **in front of** the codecs and never relaxes them

A `SaveVersionClassifier` reads only the version field(s) — safe per §2(d) — and returns one of:

| Class | Meaning | Action |
|---|---|---|
| `Current` | every version equals the build's | load directly; **zero** transforms |
| `Migratable` | strictly older, and a registered chain reaches current | run the chain, then load through the **unmodified** codec |
| `TooNew` | any version exceeds the build's | **refuse** — a build cannot know a future format |
| `Unsupported` | older than the supported floor, or no chain | **refuse** |
| `Corrupt` | version field unreadable / not a known value | **refuse** |

**The codecs are untouched.** A migrated blob is handed to the *current* codec and must pass its existing
fail-loud gates like any other input. This is the property that makes the seam safe to add in front of ten
codecs: #50 can only ever produce bytes those codecs already accept, so a migration bug surfaces as a
refusal rather than a corrupt career.

**`TooNew` is a separate class from `Corrupt` on purpose.** They demand the same refusal but different
messages — "this save is from a newer version of the game" is actionable; "this save is damaged" is not, and
telling a player the wrong one is how a recoverable situation becomes a deleted file.

**The conservative-classification rule** (the plan's §9 risk, which is the right risk): a save is
`Migratable` **only** on an exact, registered version match at every level. Anything unrecognised is
refused. Mis-classifying corrupt data as migratable would run a transform over garbage and write a
plausible-looking career; that asymmetry is why the default is refusal.

### KD-2 — A **generation version**, because the formats are not the only thing that changes

This is the decision the plan does not contain, and §2(c) is the argument for it.

`WORLD_GENERATION_VERSION` [FIXED] is stamped into the save at genesis and covers **everything that
regenerates rather than persists**: `RosterGenerator`'s draw order and budget, `LeagueBootstrap`'s
catalogues and strength ramp, and the derived-on-read tables (#32's band widths, #36's `NationCatalogue` and
weighting). On load:

- **equal** ⇒ regeneration reproduces the same world; proceed;
- **older, with a registered generation migration** ⇒ the migration **materialises** the affected data into
  the save (it can no longer be regenerated) and stamps the current version;
- **older, with none** ⇒ **refuse** — better than silently handing back a different squad.

**Materialisation is the only honest repair, and it is the fourth appearance of this pattern.** #44's
tally, #46's inbox items and #47's authored rosters all reached the same place: *the source is gone, so the
data must be persisted*. Here the source is not gone but has *changed meaning*, which amounts to the same
thing. The mechanism already exists — #47's `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob holds exactly this
shape of data, so a generation migration writes an authored-style blob and the career continues as if
authored (KD-3's per-blob model absorbs it with no new machinery).

**But materialisation needs the OLD generator, and the new build does not have it — this is the cost, and
it must be paid explicitly.** To write the v(N) rosters into the save, something must *produce* them, and
only v(N)'s generator code can: the seed alone is not enough once the code has changed. So generation
migration is possible **only** for versions whose generator the build still ships.

`GenerationRegistry` therefore retains **every generator version back to the supported floor**, keyed by
`WORLD_GENERATION_VERSION`, and a migration runs the *old* generator once to materialise, then never again.
The consequences, stated rather than discovered:

- Old generator code is **retained, frozen, and covered by its own golden vector** — it is now save-format
  code, not live code, and editing it is as breaking as editing a codec.
- The supported floor (§11 R-5) is not just a test-surface decision: it is **how many generator versions the
  build carries**. That is the real cost of a long support window, and it is the argument for a short one.
- A generation version whose generator has been dropped classifies `Unsupported` and is **refused** — which
  is the correct outcome, and is why the refusal branch above is not a placeholder.

*Rejected alternative:* materialise **every** save eagerly at save time, so no old generator is ever needed.
Rejected — it writes every club's full roster into every save forever to insure against a change that may
never happen, discarding the regenerate-don't-save design (and its save-size benefit) to solve a problem
that only exists at a bump. Retaining N generators is bounded by the floor; inflating every save is not.

**A generation bump is therefore expensive by design**, and that is a feature: it makes "just tweak the
`[GT]` ramp" visible as a decision that costs every existing career either a materialised blob or a refusal.
Today that decision is invisible and guarded only by a test.

*Rejected alternative:* forbid generation changes post-ship. Rejected as unenforceable — balance work will
want them, and a rule that must never be broken will be broken quietly.

### KD-3 — Migration is **per-blob**, versioned independently, composed by chains

A transform is registered for one `(blobKind, fromVersion)` and produces `fromVersion + 1` of that blob
only. The frame's opaque sub-blob discipline (§2(d)) means a season-state bump requires **no** knowledge of
the world-store, match, or any per-spec blob — they pass through byte-untouched, exactly as they do today
when the frame version bumps around them.

**Ownership is the bumping spec's**, which is what keeps the chain writable: the spec that changes a layout
supplies the step that reads its old layout, because it is the only party that knows both. #50 owns the
**registry and the runner**, not the steps — the same relationship #38 has to screens, and #17 to event
types.

**Ordering within one file:** the frame is classified and migrated first (it determines the sub-blob
inventory), then each sub-blob independently. A blob whose version is current runs zero steps, so a typical
update migrates one blob and copies the rest.

### KD-4 — Refusal is loud, specific, and loses nothing

A refused save is **never modified and never deleted** — the file is left exactly as found, so a player who
upgrades, hits a refusal, and reinstalls the old build still has their career. This is stated because the
tempting implementation (migrate in place, roll back on error) is precisely the one that loses data when the
rollback is the thing that fails.

A successful migration **writes to a new file** and leaves the original until the new one is written and
re-read successfully — the `temp → fsync → rename` discipline `SaveManager` and `MatchSaveManager` already
implement, extended one level: the *original* is the temp's fallback.

User-facing messages route through **#49** as a producer (identity + slots — the version numbers are slot
values), so #50 bakes no strings. Each refusal class (§KD-1) is its own intent, because collapsing them is
the failure mode named there.

### KD-5 — #39 conflict resolution compares **versions, not timestamps**, and #50 supplies the comparison

Cloud conflict is #39's UX. What #50 owns is the **fact** #39 needs: given two saves, their classifications
and their relative version ordering. The rule #50 pins:

- a save that is `TooNew` for this build **cannot be resolved by this build at all** — it is not "the newer
  copy wins", it is "this build must not touch it", which is a different and safer statement;
- a migrated save is **written back only on the player's next explicit save**, never silently on load.
  Otherwise opening a career on a second machine would rewrite the cloud copy into a format the first
  machine's older build then refuses — turning a read into a data-loss event.

### KD-6 — What migration promises (and what it cannot)

**A migrated save is *valid*, not *counterfactually identical*.** Where a bump is purely structural, the
migrated bytes equal what the new build would have written for the same career, and that is testable. Where
a bump **synthesizes** — #45's `JobSecurity` float → band (§2(e)) is the concrete case — the new value is a
*choice made by the transform*, and a career migrated at that point differs from one played natively through
the same fixtures.

That difference is unavoidable, so the contract is stated rather than implied: migration guarantees the save
**loads, is internally coherent, and is deterministic from that point forward**. It does not guarantee the
counterfactual. Forward determinism is what actually matters to a player and is what §9 tests.

### KD-7 — Determinism posture and identity

Infra: no RNG stream, no domain tag, no `SubsystemOrdinal`; #16 has no row for #50 and needs none. **Two
classes of transform, both deterministic by different means** — a distinction AR-2's `GenerationRegistry`
made necessary and which must not be blurred:

- **Format transforms** are pure functions of bytes — no draw, no clock, no simulation — so the same input
  always migrates to the same output, testable by golden vector directly.
- **Generation migrations** are *not* byte-pure: they run a frozen old generator, which draws from its own
  seeded streams (KD-2). They are still fully deterministic — same seed, same frozen code, same output — but
  their golden vector pins *the generator's output for a given seed*, not a byte→byte mapping, and they must
  run against a `DeterministicRngService` exactly as the live generator does.

Calling both "pure byte transforms" would be the kind of imprecision that produces a test asserting the
wrong property.

**Identity:** with an empty chain, a current-version save classifies `Current`, runs zero transforms, and
loads through the unmodified codec — byte-identical to pre-#50, and every non-current save is refused
exactly as today.

## 5. Persistent state (shape)

```
SaveOriginStamp : { WorldGenerationVersion (int),      # KD-2 — the new one
                    BuildId (int) }                    # informational: which build wrote this save
```

Stamped at genesis and rewritten on each save. `BuildId` is **diagnostic only** — never a migration input,
because migrating off a build number rather than a format version would make two builds sharing a format
falsely incompatible.

Everything else #50 reads already exists: the version field at the head of each blob (§2(d)).

**The stamp lands in the outer frame, beside `SEASON_SAVE_FORMAT_VERSION` — not inside the season-state
sub-blob.** KD-1's classifier reads *only* version fields and parses no blob body; putting the generation
version inside a sub-blob would force it to parse into one to classify, defeating the property that makes
classification safe (§2(d)). Frame placement keeps the gate as cheap and as untrusting as the version check
it sits beside.

The cost is that it is a **`SEASON_SAVE_FORMAT_VERSION` bump** rather than a season-state one — filed as
such in §8.1. **#50 still adds no sub-blob of its own**: a spec that migrates other people's data should not
become the twenty-sixth format version.

## 6. Determinism posture

- Infra; no stream, no tag, no ordinal (KD-7).
- **Format** transforms are pure byte→byte functions; **generation** migrations run a frozen seeded
  generator (KD-7). Both are deterministic; only the first is byte-pure.
- Migration runs **outside** the tick loop, on load, before any subsystem is constructed.
- A migrated save's forward simulation is deterministic under the standard contracts (KD-6).
- The classifier reads only version fields and never trusts a payload (KD-1).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `Classify(in header) → SaveClass` | load path → #50 | reads version fields only (KD-1) |
| `IMigrationStep { BlobKind, FromVersion, Apply(bytes) → bytes }` | owning spec → #50 registry | one step per bump, written by the bumping spec (KD-3) |
| `MigrationRunner.Run(save) → migrated` | load path → #50 | composes chains per blob; zero steps is the common case |
| `GenerationGate.Check(stamp) → Ok / Materialise / Refuse` | load path → #50 | the KD-2 gate; `Materialise` is reachable **only** when `GenerationRegistry` still holds that version's generator, otherwise `Refuse` |
| `GenerationRegistry.Register(version, generator)` | root → #50 | frozen generators stay in their owning assemblies and are registered as delegates (§10) |
| `MigrationRefusal` (class + slots) | #50 → #49 | identity + version slots; #50 bakes no strings (KD-4) |
| `CompareForConflict(a, b) → …` | #50 → #39 | classification + ordering; #39 owns the UX (KD-5) |

## 8. Cross-spec back-props

### 8.1 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-030-019** | #30 / `SeasonSaveCodec` **frame** | Add the `SaveOriginStamp` (`WorldGenerationVersion` + `BuildId`) to the **outer frame**, beside `SEASON_SAVE_FORMAT_VERSION`, carrying a `SEASON_SAVE_FORMAT_VERSION` bump. Frame placement is load-bearing, not incidental: KD-1's classifier must read the generation version **without parsing any sub-blob** (§5). Without the stamp, KD-2's gate has nothing to read. |
| **ERR-027-003** | #27 / `LeagueBootstrap` | Record that `RosterGenerator`'s draw contract, the club-name catalogue, and the strength ramp are covered by `WORLD_GENERATION_VERSION`, and that changing any of them post-ship requires a version bump plus a generation migration (KD-2). The golden vector stays as the CI guard; this is the **runtime** one it never was. (`ERR-027-001`/`-002` are filed and resolved, so `-003` is the next free number — verified, not assumed.) |

### 8.2 Deferred (land at the named tier)

- Every per-bump `IMigrationStep` — each lands with **its own** spec's bump, never in advance (there is
  nothing to migrate until a format changes twice).
- The `WORLD_GENERATION_VERSION` bump itself, at the first post-ship generation change.
- #39's conflict UX over `CompareForConflict` (KD-5).

### 8.3 Explicitly **not** back-props

- **The codecs** — untouched, deliberately. #50 adds a layer in front; weakening a single fail-loud gate
  would defeat the reason the layer is safe (KD-1).
- **#49** — #50 is a text producer through the existing adapter extension point, like #35/#46/#48.
- **#39** — consumes #50's comparison; the dependency runs that way (KD-5).
- **#16** — no stream, no tag, nothing reserved (KD-7).

## 9. Test focus

**The KD-1 classification matrix, exhaustively:** current / older-with-chain / older-without-chain / too-new
/ unreadable, each producing its own outcome, with `TooNew` and `Corrupt` **distinguishable** (the message
matters, KD-4). **Transform determinism, per class (KD-7):** a **format** step is golden-vectored byte→byte; a
**generation** migration is golden-vectored on *the frozen generator's output for a pinned seed*, run
against a real `DeterministicRngService` — asserting byte-purity there would test a property it does not
have. **Post-migration validity:** a migrated blob passes the *current* codec's unmodified gates, and the
loaded career advances deterministically (KD-6's forward-determinism promise, not counterfactual identity —
the test must not assert what KD-6 declines to promise). **The KD-2 generation lock, which is the one this
spec exists to add:** a save whose `WORLD_GENERATION_VERSION` differs is refused or materialised, **never
silently regenerated** — constructed by perturbing a `[GT]` generation input and asserting the old save does
not load with different squads (the failure §2(c) shows is currently possible and invisible).
**Non-destructive refusal:** a refused save file is byte-identical after the attempt (KD-4), and a failed
migration leaves the original intact. **Identity:** an empty chain + current save ⇒ zero transforms, byte-identical
to pre-#50. **Sub-blob isolation:** a season-state bump migrates that blob and leaves every other blob
byte-untouched (KD-3).

## 10. Reference DAG

```
load path (root) → {#50, codecs}        #50 → { }        #39 → #50        boundary → {#50, #49}
root registers → { format steps (from each bumping spec), frozen generators (from #27/#30) } → #50
```

**Acyclic, and #50 is a leaf.** It operates on **bytes** and on **registered delegates**, never on domain
types, so it references no spec's assembly — including the ones whose blobs it migrates. A transform for
#45's board block lives in #50's registry without #50 knowing what a board is: the step is supplied by #45's
own T-phase and closes over its layout, while #50 sees an opaque `byte[]`.

**The same inversion covers KD-2's `GenerationRegistry`, and it has to.** The frozen old generators are
`RosterGenerator` / `LeagueBootstrap` code, so they stay in **their owning assemblies** (versioned there)
and are registered with #50 as delegates by the root. Had #50 held them directly it would reference
`player-database` and `season-save` — and `season-save` reaches `MatchEngine` and `LivingWorld`, so the
migration layer would transitively depend on the entire simulation to open a file. The leaf claim is
therefore a real constraint on where retained generators live, not a description of a happy accident.

## 11. Risks and standing options

- **R-1 — the generation blind spot is the whole reason this spec is more than plumbing** (KD-2). If #50
  ships without it, the project will have a migration system that provably cannot detect its most likely
  breaking change. Highest-priority item in the spec.
- **R-2 — 25 version constants and rising** (§2(a)). The per-blob model (KD-3) scales, but the registry's
  bookkeeping does not stay free; a spec that bumps a version and forgets to register a step turns an old
  save into `Unsupported` with no diagnosis. A build-time check that every version between the floor and
  current has a registered step is the mitigation, and it belongs in #50's §5.
- **R-3 — "migrate in place, roll back on failure"** will be proposed because it is simpler (KD-4). It is
  also the only design here that can lose a career.
- **R-4 — KD-6's honesty may be read as weakness.** A synthesized field means a migrated career is not the
  career that would have been played. Saying so is better than a guarantee that quietly fails at the first
  representation change — and #45's `JobSecurity` bump is already queued to be exactly that.
- **R-5 — the supported floor is a product decision with a real code cost.** It determines chain length and
  test surface — and, per KD-2, **how many frozen generator versions the build ships**. #50 defines the
  mechanism and leaves the floor a policy constant, but the floor should be chosen knowing it is measured in
  retained code, not just in test cases.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.6.** AR-1 (0H+2M) → v0.2, AR-2 (1H) → v0.3,
   AR-3 (0H+2M) → v0.4, AR-4 (0H+3M) → v0.5, AR-5 (0H+0M+2L) → v0.6 = **CONVERGENCE** (an L-only round
   closes the cycle, per the project convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/save-migration-versioning/`, FR
   prefix `FR-MG`.
3. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** at promotion.
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`**, landing the §8.1 back-props atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. The plan's shape is correct and verification supplies scale and a blind spot. **Scale:** the version surface is **12 constants in shipped code and 25 across the specs**, not the three the plan names, so migration is a per-blob family of chains (KD-3) rather than one chain. **The blind spot (KD-2):** rosters are *regenerated from the seed rather than saved*, so a change to `RosterGenerator`'s draw order, the club-name catalogue, or the strength ramp rewrites every existing career — and **no format version covers it**, because nothing is serialized. The existing guard is `LeagueBootstrapGoldenVectorTests`, a **CI test**, which stops an accidental change but says nothing about a deliberate one shipped in an update — precisely #50's domain. A #50 without a `WORLD_GENERATION_VERSION` would migrate all 25 formats perfectly and still hand the player a different squad. The same applies to #32's knowledge bands and #36's nationality, both derived on read. The repair is **materialisation** — the fourth appearance of *cannot-be-recomputed ⇒ must-be-persisted* (#44, #46, #47, now #50), reusing #47's authored-blob shape. **KD-1** puts classification in front of the codecs without weakening any of their gates, so a migration bug surfaces as a refusal rather than a corrupt career, and separates `TooNew` from `Corrupt` because the two demand different messages. **KD-6** bounds the promise honestly: migration guarantees a valid, forward-deterministic save, not counterfactual identity — #45's `JobSecurity` float→band bump is already queued as the case that proves it. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 2M, both resolved.** **M-1** — §8.1 asserted that *"`spec-error-log.md` shows no `ERR-027-*` filed yet"* and left the number open. **Two are filed and resolved** (`-001` the `0x1F` allocation, `-002` the additive position overload), so the claim was false and the correct next number is `ERR-027-003` — now stated as verified. A back-prop table is the worst place to guess an id, since the guess is what someone files against. **M-2** — §2(c), the spec's load-bearing finding, was cited only to the root `CLAUDE.md` (a tracking summary) when the primary source is better: `WorldStore.WorldSeed`'s own doc comment says *"Squads are not persisted, so resuming a career means calling `LeagueBootstrap.Generate(world.WorldSeed, season.ClubCount)`"*. Quoted at source, which also sharpens the point — a career's whole playing population is a function of **two saved integers and the generator's current code**. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 1H, resolved.** **H-1** — KD-2 said a generation migration *"materialises the affected data into the save"*, which **the new build cannot do**: materialising the v(N) rosters requires running v(N)'s generator, and the update ships v(N+1). The seed is not enough once the code has changed — the design promised an impossible operation, and would have been discovered only when someone tried to write the first generation migration. Resolved with a `GenerationRegistry` that **retains every generator version back to the supported floor**, with the consequences stated: retained generators are frozen save-format code under their own golden vectors; the supported floor is measured in **shipped code**, not just test cases (R-5 sharpened); and a version whose generator has been dropped is `Unsupported` and refused — which is why that branch is not a placeholder. The eager-materialise-everything alternative is rejected for inflating every save forever to insure against a bump that may never come. |
| v0.4 | July 26, 2026 | **AR-3 fix pass: 0H + 2M, both consequences of AR-2's fix rippling.** **M-1** — KD-7 still claimed every transform is *"a pure function of bytes — no draw, no clock, no simulation"*, which AR-2 falsified: a generation migration **runs a frozen old generator**, which draws from seeded streams. Split into the two classes, both deterministic by different means, because a test written against "byte-pure" would assert the wrong property for half of them. **M-2** — §5 placed the `SaveOriginStamp` **inside** #30's season-state sub-blob, which would force KD-1's classifier to parse into a blob body to read the generation version — defeating the read-only-version-fields discipline that makes classification safe in the first place. Moved to the outer frame beside `SEASON_SAVE_FORMAT_VERSION`, accepting the frame bump as the price, and `ERR-030-019` re-targeted accordingly. |
| v0.5 | July 26, 2026 | **AR-4 fix pass: 0H + 3M — all three the same AR-2 ripple, reaching further than AR-3 caught.** **M-1** §6 and **M-2** §9 both still asserted byte-purity for *every* transform, the claim AR-3 corrected in KD-7 alone; §9's version would have produced a test asserting a property generation migrations do not have. **M-3** — §10 claimed #50 *"references no spec's assembly"* because it operates on bytes, but AR-2's `GenerationRegistry` retains **old generator code**: held directly, #50 would reference `player-database` and `season-save`, and `season-save` reaches `MatchEngine` and `LivingWorld` — a migration layer transitively depending on the whole simulation to open a file. Resolved by the inversion the format steps already use: frozen generators stay in their owning assemblies and are **registered as delegates** by the root. Recorded as a constraint on where retained generators live, not a happy accident. *(One High fix has now propagated into six stale statements across three rounds — the clearest case yet for re-reading the whole document rather than the diff.)* |
| v0.6 | July 26, 2026 | **AR-5 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). The sweep specifically re-checked every remaining statement about transform purity and the leaf DAG for further AR-2 ripple and found none. **L-1** — §7's `GenerationGate` row did not say that `Materialise` is reachable only when the registry still holds that version's generator, which is the whole constraint AR-2 introduced; added, along with the `Register` surface. **L-2** — §1's not-owned table listed the formats but not the **generators**, whose ownership (owning assembly, registered as a delegate) is what keeps §10's leaf claim true. |
