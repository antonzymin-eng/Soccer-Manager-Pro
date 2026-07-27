# Save Migration & Versioning #50 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #50 has no `[EST]` constants and — because it takes **no determinism
reservation** (KD-7) — **no `[CROSS-PENDING]` constants either**, so neither region appears.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `WORLD_GENERATION_VERSION` | `1` | `[FIXED]` | The version of everything **regenerated rather than persisted** (FR-MG-011). **`[FIXED]`, emphatically not `[GT]`:** it is an identity, not a dial. Making it tunable would let it be changed casually — and the entire point of KD-2 is that a generation change must be a **deliberate, expensive** decision, since it costs every existing career either a materialised blob or a refusal. |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `BLOB_KIND_COUNT` | `Enum.GetValues(typeof(BlobKind)).Length` | `[DERIVED]` | Derived from the enum, **never a hand-maintained literal** — the `POSITION_COUNT` precedent, where two assemblies each carried a private copy of an enum's member count. FR-MG-022's completeness check sweeps this range, so a lagging literal would silently stop checking the newest blob kind. |
| `BUILD_FRAME_VERSION` | `SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION` | `[DERIVED]` | The build's own frame version. Derived rather than duplicated: a second copy would drift at the next bump and the classifier would compare against a stale number — mis-classifying **current** saves as `TooNew`, which is the most damaging direction for that particular error. |
| `BuildVersionOf(BlobKind)` | each owning spec's format-version constant | `[DERIVED]` | A projection, not a table. Same reasoning, one blob kind at a time; a hand-maintained copy is exactly what FR-MG-022 is designed to catch. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `SEASON_SAVE_FORMAT_VERSION` and the frame layout | #30 | The version #50 reads first, and the frame the `SaveOriginStamp` joins (ERR-030-019). |
| Every sub-blob's leading format-version constant | each owning spec | Read, never declared here. Appendix C is the inventory. |
| `DeterministicRngService` | #16 | Required by FR-MG-035 so a frozen generator draws exactly as the live one does. **#50's only assembly reference** (§4.1). |
| `RosterGenerator`, `LeagueBootstrap` (and their frozen versions) | #27 / #30 | **Never referenced** — registered as **delegates** (§4.4). Listed so the exclusion is deliberate. |
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer` | #49 | Used **only inside `MigrationTextBoundary`**, which is not a #50 assembly (FR-LC-012). |
| The `temp → fsync → rename` discipline | `SaveManager` / `MatchSaveManager` | Inherited, extended one level so the **original** is the temp's fallback (FR-MG-024). |
| The overflow-safe length bound (`total − offset`) | `MatchSaveCodec` | Inherited at every length-prefix read (§3.6). |

### A.4 GT

| Constant | Value | Notes |
|---|---|---|
| `SUPPORTED_FLOOR` | *policy* | The oldest version #50 will migrate from. **The only behavioural `[GT]` in the spec**, and a product decision rather than a balance one (R-5): it sets chain length, test surface, and — per KD-2 — **how many frozen generator versions the build ships**. It governs which *files open*; it can never change how a match, season or world behaves. |
| `MG_BUDGET_CLASSIFY_MS` | `5` | §6.3 ceiling for one `Classify`. |
| `MG_BUDGET_STEP_MS` | `50` | §6.3 ceiling for one `IMigrationStep.Apply` over one blob. |
| `MG_BUDGET_RUN_MS` | `500` | §6.3 ceiling for one full `Run` over a whole save. |
| `MG_BUDGET_MATERIALISE_MS` | `5 000` | §6.3 ceiling for one generation materialisation. In **seconds** deliberately — it performs a whole league generation, once ever per save, on a screen where waiting is expected. |

**The last four are ceilings, not measurements.** No certified number exists for #50 and none is invented
here: a certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #50 has no implementation to measure. **`MG_BUDGET_CLASSIFY_MS` is the
one to measure first** — not because it is largest (it is smallest) but because it is the only cost
multiplied by **file count** rather than by a user action (§6.3).

**No `[GT]` constant in this catalogue affects the simulation** (§9.2), and `SUPPORTED_FLOOR` is where a
reader should check that claim rather than take it on trust: it decides whether a file opens, and nothing
downstream of a successful open depends on its value.

## Appendix B — The `SaveOriginStamp` frame layout (ERR-030-019)

Two fields, appended to #30's **outer frame** beside `SEASON_SAVE_FORMAT_VERSION`, carrying a
`SEASON_SAVE_FORMAT_VERSION` bump.

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `WorldGenerationVersion` | `i32` | The KD-2 migration input. Read by the classifier **without parsing any sub-blob** (FR-MG-010). |
| 2 | `BuildId` | `i32` | **Diagnostic only** (FR-MG-009). Never a migration input. |

**Frame placement is the whole point, and the bump is its accepted price.** KD-1's classifier reads only
version fields and parses no blob body; a stamp inside the season-state sub-blob would force it to parse
into one in order to classify, defeating the property that makes classification cheap **and** safe. The
supplement's own AR-3 caught this after placing the stamp inside the blob first, so the placement is a
corrected decision rather than a default.

**`BuildId` must never become a migration input**, and this is the appendix's one trap. It reads like a
more precise version number than a format version, and using it would make two builds that share a format
**falsely incompatible** — an entire class of spurious refusals generated by a field whose only purpose is
to help diagnose them. §2.2 carries the same warning at the field itself, because that is where the
temptation actually appears.

**This is the whole of #50's persistent footprint** (§4.5). #50 introduces **no sub-blob of its own**: a
spec that migrates other people's data should not become the twenty-sixth format version. There is
consequently **no version constant owned by #50**, no #50 row for its own registry, and nothing for a
future #50 bump to migrate.

**Deliberately absent — three things:**

1. **Migration history.** A chain of *"was migrated from v3 by build 412"* records would be a second,
   unversioned format living inside the version system, growing with every update, read by nothing.
2. **A materialisation marker.** Whether a save's rosters came from a generator or from materialisation
   is already answered by #47's authored-blob presence; a second flag would be a two-truths hazard.
3. **Any timestamp.** Conflict resolution compares **versions, never timestamps** (KD-5), and storing one
   would invite the comparison FR-MG-029 exists to replace.

## Appendix C — The version-surface inventory

**25 format versions**, which is why KD-3's per-blob model is a feasibility decision rather than a
refinement (§1.4(a)).

### C.1 Shipped in code (12)

`SEASON_SAVE_FORMAT_VERSION` · `SEASON_STATE_FORMAT_VERSION` · `WORLD_STORE_FORMAT_VERSION` ·
`WORLD_SNAPSHOT_FORMAT_VERSION` · `MATCH_SAVE_FORMAT_VERSION` · `SNAPSHOT_SCHEMA_VERSION` ·
`PROGRESSION_SAVE_FORMAT_VERSION` · `PHASE_A_PAYLOAD_FORMAT_VERSION` · `RECORD_FORMAT_VERSION` ·
`FIELD_WIDTH_SCHEMA_VERSION` · `SCENARIO_MANIFEST_FORMAT_VERSION` · `SCHEMA_VERSION`

### C.2 Specified but unbuilt (13)

`ACADEMY_` · `BOARD_` · `COMPETITION_` · `DISCIPLINE_` · `FINANCE_` · `HUMAN_SYSTEMS_` · `MEDICAL_` ·
`SCOUTING_` · `STAFF_` · `TRAINING_` · `TRANSFERS_` — plus `INBOX_` (#46), `MEDIA_` (#35),
`NATIONAL_TEAM_` (#36) and `AUTHORED_DB_` (#47) from this wave.

**Two observations that shape the spec rather than merely describing it:**

1. **The list grows with every management spec** — this wave alone added four — so any design requiring
   #50 to *know* each format would be obsolete on arrival. KD-3's opaque-`byte[]` steps are what make the
   count irrelevant to #50 itself: the registry grows, the runner does not change.
2. **Not one of these covers the generation surface**, and that is the whole of §1.4(c). Twenty-five
   version constants, and the largest save-visible thing in the project — the identity of every player in
   every club — is governed by none of them, because it is **not serialized at all**.

### C.3 Not in the migration surface

`SCENARIO_MANIFEST_FORMAT_VERSION`, `FIELD_WIDTH_SCHEMA_VERSION` and `RECORD_FORMAT_VERSION` version
**tooling and diagnostic** artifacts rather than saves. They are listed in C.1 because they are real
format versions a reader will find, and excluded here because migrating them would mean #50 owning
formats no career depends on. **A save that references one is a save with a tooling artifact embedded in
it**, which is its own defect, not a migration case.

## Appendix D — The classification matrix

The authoritative form of FR-MG-002. §5.1 walks every cell.

| Class | Condition | Action | Message class |
|---|---|---|---|
| `Current` | every version equals the build's | load directly; **zero** transforms | none |
| `Migratable` | strictly older, **and** a registered chain reaches current at **every** level | run the chain, then load through the **unmodified** codec | none on success |
| `TooNew` | any version exceeds the build's | **refuse** — a build cannot know a future format | *"from a newer version of the game"* — **actionable** |
| `Unsupported` | older than `SUPPORTED_FLOOR`, or no chain reaches current | **refuse** | *"too old for this build"* |
| `Corrupt` | a version field is unreadable, out of range, or not a known value | **refuse** | *"damaged"* — **not** actionable |

**Aggregation (FR-MG-008):** the file's class is the **most severe** of its per-blob classes —
`Corrupt` > `TooNew` > `Unsupported` > `Migratable` > `Current`.

**`Corrupt` dominates `TooNew` deliberately.** Both refuse, so the ordering looks cosmetic; it is not.
A damaged file reported as merely futuristic tells the player to wait for a patch that will never help,
while a futuristic file reported as damaged invites them to delete a save that is perfectly good on the
machine that wrote it. **The two refusals demand different actions from the player**, which is why
FR-MG-005 makes them distinct classes rather than one `Refused`.

**Nothing defaults to `Migratable`.** An unrecognised version at any level is refused (FR-MG-004/006).
The asymmetry is the argument: a refusal is recoverable — reinstall the old build, the file is untouched
(FR-MG-023) — while a transform run over garbage writes a **plausible-looking** career, and there is no
recovery from a save that loads and is wrong.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 with the `[FIXED]`-not-`[GT]` argument for `WORLD_GENERATION_VERSION`; A.2 deriving the build's own versions from their owners rather than duplicating them; A.3 Cross with the deliberate generator exclusion; A.4 GT; B the two-field frame stamp with its three deliberate absences; C the 25-version inventory with the two observations that shape the design; D the classification matrix with the aggregation rule). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the four `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline — **the #45 PASS-1 M-2 defect, now seen for the eighth time in this wave**, which at this point is a process finding about section authoring order rather than eight independent slips; added to A.4 along with `SUPPORTED_FLOOR`, whose absence had left §9.2's *"no `[GT]` affects the simulation"* claim with nothing to check itself against. **M:** added **A.2 `BUILD_FRAME_VERSION` / `BuildVersionOf`** — the classifier compares against the build's own versions, and nothing said those must be **derived** rather than copied; a stale copy mis-classifies **current** saves as `TooNew`, which is the most damaging direction that error can take. **L:** A.1 gained the reason the generation version is an identity rather than a dial; B gained the `BuildId` trap (repeated from §2.2 because an appendix is where a future implementer looks for the layout, and the temptation lives at the field) and the note that the stamp is the **whole** of #50's persistent footprint; **C.3 added**, excluding the three tooling/diagnostic versions from the migration surface so a future reader does not try to write steps for them; D gained the explicit argument for why `Corrupt` dominating `TooNew` is not cosmetic — the two refusals demand different actions from the player. |
#endregion
