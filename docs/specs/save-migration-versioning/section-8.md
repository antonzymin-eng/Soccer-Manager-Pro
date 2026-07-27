# Save Migration & Versioning #50 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-050-001 | `SeasonSaveCodec` — the outer frame | Writes `SEASON_SAVE_FORMAT_VERSION` as the **first field**, then a flag byte, then length-prefixed sub-blobs it *"never parses"*. **The property KD-1 rests on**, and one the format already has. |
| XC-050-002 | Every sub-blob codec's leading version field | Each reads its own version first — so a sub-blob's version is readable without parsing its body (KD-3). |
| XC-050-003 | The 12 shipped format-version constants | `SEASON_SAVE_`, `SEASON_STATE_`, `WORLD_STORE_`, `WORLD_SNAPSHOT_`, `MATCH_SAVE_`, `SNAPSHOT_SCHEMA_`, `PROGRESSION_SAVE_`, `PHASE_A_PAYLOAD_`, `RECORD_`, `FIELD_WIDTH_SCHEMA_`, `SCENARIO_MANIFEST_`, `SCHEMA_` — plus thirteen specified-but-unbuilt (Appendix C). |
| XC-050-004 | The tree-wide *"no migration"* posture | *"a v1 file is rejected fail-loud, no Stage-0 migration"*; *"v2 payloads rejected fail-loud, no migration"*. **#50 introduces the first migration machinery in the project**, in front of codecs that all currently refuse. |
| XC-050-005 | `WorldStore.WorldSeed`'s doc comment | *"**Squads are not persisted**, so resuming a career means calling `LeagueBootstrap.Generate(world.WorldSeed, season.ClubCount)`."* **The primary source for §1.4(c)** — the finding the whole spec turns on. |
| XC-050-006 | `LeagueBootstrapGoldenVectorTests` / KD-10 | The **CI** guard against an accidental generation change. #50 supplies the **runtime** one it never was. |
| XC-050-007 | `RosterGenerator`'s `FIELDS_PER_PLAYER` draw contract | Under `WORLD_GENERATION_VERSION` (FR-MG-011 / ERR-027-003) — its draw order and per-player budget are save-visible without being saved. |
| XC-050-008 | #45's **ERR-030-009** | `JobSecurity` `float` → derived enum band, *"with no migration path"* in its own words. **The queued counter-example to "migration is a structural rewrite"** and the concrete case behind KD-6. |
| XC-050-009 | #47's `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob | The **existing** shape a generation materialisation writes into (KD-2) — so the repair adds no new save shape. |
| XC-050-010 | #44's tally / #46's inbox items / #47's authored rosters | The three prior appearances of *cannot-be-recomputed ⇒ must-be-persisted*. #50's materialisation is the **fourth**, with the twist that the source has not vanished but **changed meaning**. |
| XC-050-011 | `MatchSaveManager` / `SaveManager` — `temp → fsync → rename` | The atomic-write discipline FR-MG-024 extends one level, making **the original the temp's fallback**. |
| XC-050-012 | `MatchSaveCodec`'s overflow-safe bound (`total − offset`) | The length-prefix hardening #50 inherits at every read (§3.6) — and #50 reads more untrusted length prefixes than anything else in the tree. |
| XC-050-013 | #49 FR-LC-002 / 004 / 012 / 013 / 015 / 008a | The producer contract: no baked strings, `Render(in LocalizedTextRequest)`, no sim-side reference to the localization assembly, a **sibling boundary adapter**, the intent-value pre-gate, base-locale coverage. |
| XC-050-014 | #39 — cloud saves and conflict UX | **Consumes** `CompareForConflict` (KD-5). The dependency runs #39 → #50, never the reverse. |
| XC-050-015 | #38 `IViewModelSource<T>` / FR-UI-001 | #50 exposes no view model; the reverse-reference scan is extended to #50's assembly. Listed so the **absence** of a UI surface is deliberate. |
| XC-050-016 | #16 §3.4 | **No row and no `_RESERVED_` placeholder for #50** — consistent with the `0x2A` note that read-only / presentation / infra specs take no tag. Nothing to file, and **nothing to promote later** (FR-MG-036). |

## 8.2 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-030-019** | #30 / `SeasonSaveCodec` **frame** | Add the `SaveOriginStamp` (`WorldGenerationVersion` + `BuildId`) to the **outer frame**, beside `SEASON_SAVE_FORMAT_VERSION`, carrying a `SEASON_SAVE_FORMAT_VERSION` bump. **Frame placement is load-bearing, not incidental:** KD-1's classifier must read the generation version **without parsing any sub-blob** (FR-MG-010), and a stamp inside the season-state blob would force it to parse into one to classify — defeating the property that makes classification safe. Without the stamp, KD-2's gate has nothing to read. |
| **ERR-027-003** | #27 / `LeagueBootstrap` | Record that `RosterGenerator`'s draw contract, the club-name catalogue and the strength ramp are covered by `WORLD_GENERATION_VERSION`, and that changing any of them post-ship requires a version bump **plus** a generation migration (KD-2). The golden vector stays as the CI guard; this is the **runtime** guard it never was. |

**Both ids were verified free against `spec-error-log.md` rather than assumed** — `ERR-027-001` and
`-002` are filed and resolved, so `-003` is genuinely next; and `ERR-030-019` is unclaimed both in the log
and across every spec folder, with `-015`..`-018` and `-020`..`-024` taken by this wave's siblings. That
check is recorded because three specs in this same wave proposed ids that had **already been filed**
(§9.4.1).

## 8.3 Deferred — land at the named tier

- **Every per-bump `IMigrationStep`**, each with **its own** spec's bump (T3+). Never in advance: there is
  nothing to migrate until a format changes twice, and a speculative step would transform from a version
  no save carries.
- **The first `WORLD_GENERATION_VERSION` bump**, at the first post-ship generation change (KD-2).
- **#39's conflict UX** over `CompareForConflict` (KD-5).
- **The supported floor's value** — a policy constant, chosen knowing it is measured in **retained
  generator code** (R-5).

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **The codecs — nothing, deliberately.** #50 adds a layer in front; weakening a single fail-loud gate
  would defeat the reason the layer is safe (FR-MG-003). This is the absence that matters most, and
  T-MG-BOUND-002 asserts it behaviourally because a reference graph cannot.
- **#16 — no row, no `_RESERVED_` placeholder, nothing at all.** No stream, no tag, no ordinal; the one
  draw in the system belongs to a **frozen generator** on its own seeded service (FR-MG-036). As with #46
  and #48, that also means #50 has **nothing to promote later**.
- **#49 — nothing.** #50 is a producer through the documented adapter extension point, like #35, #46 and
  #48; #49's core is untouched.
- **#39 — nothing.** #39 consumes #50's comparison; the dependency runs that way (KD-5).
- **#38 — nothing.** #50 exposes no view model and owns no screen. A refusal is rendered by whatever
  surface asked for the load.
- **#45 — nothing, despite ERR-030-009 being the case KD-6 is written around.** That bump is #45's to
  make and #45's step to write (FR-MG-018); #50 records it as the worked example, not as a change.
- **#27 beyond ERR-027-003 — nothing.** The generators stay where they are; #50 holds delegates (§4.4).
  In particular `RosterGenerator` is **not** moved, wrapped or re-homed.
- **The match engine and world store — nothing.** #50 completes before either exists (FR-MG-038).

## 8.5 References

#50 introduces **no external citation**. Its content is a classification, a registry, a transform-chain
discipline and a write protocol composed entirely from this project's own approved specs and shipped
source; there is no published result it rests on, and inventing a citation to decorate the section would
be the fabrication the project's rules forbid.

The one thing worth recording in place of a citation is that **the strongest evidence in this spec is a
doc comment in shipped code** — `WorldStore.WorldSeed`'s (XC-050-005). §1.4(c) quotes it at source rather
than citing the root `CLAUDE.md`'s summary of it, because the finding it supports is the reason #50 is
more than plumbing, and a tracking summary is a weaker authority than the code it summarises.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-050-001..016; two approval-time back-props, **both ids verified free against the error log rather than assumed** — recorded explicitly because three siblings in this wave proposed already-filed ids; §8.3 deferred items, each tied to the tier that makes it real; the not-a-back-prop list led by **the codecs**, which is the absence under the most pressure and the one a reference graph cannot check; §8.5 records that the spec's strongest evidence is a doc comment in shipped source, quoted at source rather than through a tracking summary). Status IN REVIEW. |
#endregion
