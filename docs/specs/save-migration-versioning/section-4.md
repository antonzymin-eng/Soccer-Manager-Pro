# Save Migration & Versioning #50 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.SaveMigration`** at `src/save-migration/`, referencing
**`TacticalDirector.DeterministicSim`** and nothing else.

```
load path (root) ──▶ {#50, codecs}          #50 ──▶ {deterministic-sim}
#39              ──▶ #50                    boundary(MigrationTextBoundary) ──▶ {#50, #49}
root registers   ──▶ #50 : { format steps (from each bumping spec),
                             frozen generators (from #27 / #30) }
```

**Acyclic, and #50 is a leaf.** It operates on **bytes** and on **registered delegates**, never on domain
types, so it references no spec's assembly — **including the ones whose blobs it migrates**. A transform
for #45's board block lives in #50's registry without #50 knowing what a board is: the step is supplied by
#45's own T-phase and closes over its layout, while #50 sees an opaque `byte[]`.

**The `DeterministicSim` reference is real and is the only one.** KD-7 splits the transform classes, and a
generation migration must run a frozen generator **against a `DeterministicRngService`** exactly as the
live generator does — so #50 needs the RNG service type even though it draws nothing itself.

**The leaf claim is a constraint, not a description of a happy accident** (§4.4).

## 4.2 File layout

```
src/save-migration/
├── SaveMigrationConstants.cs      # the Appendix A catalogue — WORLD_GENERATION_VERSION lives here
├── SaveClass.cs                   # runtime classification; deliberately NOT ordinal-pinned
├── BlobKind.cs                    # APPEND-only — it is the REGISTRY KEY (FR-MG-017)
├── SaveOriginStamp.cs             # KD-2; read from the FRAME, never from a sub-blob
├── SaveVersionClassifier.cs       # FM-MG-01 — reads version fields ONLY
├── IMigrationStep.cs              # the contract the BUMPING SPEC implements
├── MigrationRegistry.cs           # (blobKind, fromVersion) -> step; + the FR-MG-022 completeness check
├── MigrationRunner.cs             # FM-MG-02 — frame first, then each sub-blob independently
├── GenerationRegistry.cs          # version -> frozen generator DELEGATE (never the code, §4.4)
├── GenerationGate.cs              # FM-MG-03
├── MigrationCommit.cs             # FM-MG-04 — temp -> fsync -> VerifyLoadable -> rename
├── MigrationRefusal.cs            # identity + slots; #50 bakes no string
├── SaveConflictComparison.cs      # FM-MG-05 — facts for #39
└── tests/
```

**`MigrationTextBoundary.cs` is deliberately absent from this tree.** It references both #50 and
`TacticalDirector.Localization`, and FR-LC-012 makes a sim/loop-side reference to the latter a **build
error** — so placing it here would not merely be untidy.

**No codec lives here, and none is edited** (FR-MG-003). #50 adds a layer **in front of** the codecs; the
day a codec's gate is relaxed to accommodate a migration is the day the seam stops being safe.

**No generator code lives here** (§4.4). `GenerationRegistry` holds delegates.

**CS0104 pre-check.** #50 introduces `SaveClass`, `BlobKind`, `SaveOriginStamp`, `SaveVersionClassifier`,
`IMigrationStep`, `MigrationRegistry`, `MigrationRunner`, `GenerationRegistry`, `GenerationGate`,
`MigrationCommit`, `MigrationRefusal`, `SaveConflictComparison`. Each was checked against every name that
could be in scope with it before authoring, because this project has hit CS0104 twice
(`TacticTranslation`, `PlayerAttributes`). **`SaveClass` is the one worth naming**: it is deliberately not
`SaveState`, `SaveStatus` or `SaveVersion`, all of which read as save-domain types rather than as a
load-time classification.

## 4.3 Composition on the load path

```
# in the ROOT — the only layer that sees both #50 and the codecs
bytes  := File.ReadAllBytes(path)
cls    := SaveVersionClassifier.Classify(bytes)

switch (cls)
{
    case Current:      break;                                   # zero transforms (FR-MG-019/037)
    case Migratable:   bytes = MigrationRunner.Run(bytes);      # then fall through to the SAME load
                       break;
    default:           throw Refuse(new MigrationRefusal(cls, …));   # #49 renders (FR-MG-026)
}

stamp := ReadOriginStamp(bytes);                                # from the FRAME (FR-MG-010)
switch (GenerationGate.Check(stamp))
{
    case Ok:           break;
    case Materialise:  bytes = MaterialiseInto(bytes, stamp);   # FM-MG-03; runs ONCE
                       break;
    case Refuse:       throw Refuse(…);                          # F5
}

contents := SeasonSaveManager.Load(bytes, squads);              # the UNMODIFIED codec (FR-MG-003)
```

**The migrated path rejoins the normal path immediately**, and that is the design's central safety
property: there is **one** load path, and migration only decides which bytes enter it.

**The generation gate runs after format migration, not before.** The stamp's location in the frame may
itself have moved between frame versions, so reading it is only well-defined once the frame is current.

**Migration completes before any subsystem is constructed** (FR-MG-038): no engine, no world store, no
season loop exists while this runs, so there is nothing for a partially-migrated file to corrupt.

## 4.4 The delegate inversion — why the leaf claim is true

**Format steps invert as one would expect:** each is written by the bumping spec, registered by the root,
and held by #50 as an `IMigrationStep` over `byte[]`.

**KD-2's `GenerationRegistry` has to invert the same way, and this is the non-obvious part.** The frozen
old generators *are* `RosterGenerator` / `LeagueBootstrap` code. Had #50 held them directly it would
reference `player-database` **and** `season-save` — and `season-save` reaches `MatchEngine` and
`LivingWorld`, so **the migration layer would transitively depend on the entire simulation in order to
open a file**. That is not merely inelegant: it would make the component that must run before anything
else exists depend on everything that does not exist yet.

So frozen generators stay in **their owning assemblies**, versioned there, and are registered with #50 as
delegates by the root:

```
# in the ROOT
generationRegistry.Register(1, (rng, seed, clubs) => LeagueBootstrapV1.Generate(rng, seed, clubs));
generationRegistry.Register(2, (rng, seed, clubs) => LeagueBootstrap  .Generate(rng, seed, clubs));
```

**A retained generator is save-format code** (FR-MG-016): frozen, covered by its own golden vector, and as
breaking to edit as a codec. The `LeagueBootstrapGoldenVectorTests` precedent already exists for the live
one; a retained one needs the same treatment for a stronger reason, since nothing about it is exercised by
normal play.

## 4.5 State and persistence

**#50 adds exactly one thing to a save — the `SaveOriginStamp` — and nothing else** (Appendix B). It
introduces **no sub-blob of its own**: a spec that migrates other people's data should not become the
twenty-sixth format version.

**The stamp lands in the outer frame, beside `SEASON_SAVE_FORMAT_VERSION`, and the placement is
load-bearing** (FR-MG-010). KD-1's classifier reads *only* version fields and parses no blob body; putting
the generation version inside a sub-blob would force it to parse into one in order to classify, defeating
the property that makes classification safe in the first place. The cost is that this is a
**`SEASON_SAVE_FORMAT_VERSION` bump** rather than a season-state one, and it is accepted knowingly
(ERR-030-019).

**Everything else #50 reads already exists**: the version field at the head of each blob (§1.4(d)).

**The supported floor is a policy constant, not a mechanism** (Appendix A). #50 defines how migration
works and leaves how far back it reaches to the product — while recording that the floor is measured in
**retained generator code**, not just in test cases (R-5).

## 4.6 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **the codecs** | **Untouched, deliberately** (FR-MG-003). #50 hands them bytes; their gates adjudicate. Relaxing one to accommodate a migration would remove the reason the seam is safe. |
| **#30** | Owns the frame; hosts the `SaveOriginStamp` (ERR-030-019). #50 reads the frame's leading version and never parses a sub-blob body. |
| **#27** | Owns `RosterGenerator`; retains frozen versions under `WORLD_GENERATION_VERSION` (ERR-027-003). Registered as a delegate, never referenced. |
| **#49** | #50 is a **producer**: refusal identities + version slots, rendered by a sibling `MigrationTextBoundary`. #49's core is untouched. |
| **#39** | **Consumes** `CompareForConflict`. #50 owns the facts; #39 owns the UX. The dependency runs one way. |
| **every bumping spec** | Supplies its own `IMigrationStep` at its own bump. #50 owns the registry and the runner, never a step — the relationship #38 has to screens and #17 to event types. |
| **#16** | **Untouched — no stream, no tag, no ordinal, no `_RESERVED_` row** (FR-MG-036). |
| **the match engine / world store** | **Never referenced.** #50 runs before either exists. |

**Standing review item:** #50's safety rests on two properties a reference graph cannot prove — that
**no codec gate was weakened** to accommodate a migration, and that **no step reaches outside its own
blob**. §5 asserts both behaviourally, and both are the kind of thing a well-meaning fix under release
pressure would break first.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (the leaf assembly with its single, justified `DeterministicSim` reference; file layout with three deliberate absences — no boundary, no codec, no generator code; the load-path composition showing the migrated path rejoining the **one** normal load path, and the generation gate running *after* format migration because the stamp's own location may have moved; §4.4 the delegate inversion, with the transitive-dependency argument that makes the leaf claim a constraint rather than an accident; state and persistence, with the frame-placement cost accepted knowingly; neighbour contracts and the standing review item naming the two properties a reference graph cannot prove). Status IN REVIEW. |
#endregion
