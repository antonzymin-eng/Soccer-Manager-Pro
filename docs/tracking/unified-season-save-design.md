# Unified Season Save File — Season Composition Root (Design Supplement)

> **Created:** July 22, 2026
> **Last Updated:** July 22, 2026 (v0.5 — **Code adversarial review (fresh pass at the user's request):
> 0H + 0M + 2L, both fixed.** L-1: `MatchSaveManager.Save` argument-guard order restored to
> engine-before-path (the v1.1 `Encode` delegation had flipped it) — a both-invalid call throws
> `ArgumentNullException` not `ArgumentException`, keeping the refactor exactly behaviour-identical
> (`MatchSaveManager.cs` v1.2). L-2: `Load_NoMatchSeason_WithProvider_IgnoresProvider` added (locks R4;
> `SeasonSaveManagerTests` 18 → 19). Everything else verified sound (overflow-safe per-read `Require`
> guards, opaque-blob frame + trailing guard, FR-LW-003 layering, non-mutating capture, atomic-write
> mirror). Full dotnet gate re-run: PASSED, 0 failures (19 season-save + 279 match-engine; whole tree
> green).)
> **Last Updated (prior):** July 22, 2026 (v0.4 — **LANDED.** Implemented per the converged design; the code
> self-adversarial review found nothing on the small faithful-mirror surface (layering verified —
> `match-engine` and `living-world` reference neither each other; atomic-write mirror; opaque-blob frame;
> provider threading; non-mutating capture). New `src/season-save/` assembly (`TacticalDirector.SeasonSave`,
> references `MatchEngine` + `LivingWorld` + `DeterministicSim`): `SeasonSaveConstants`
> (`SEASON_SAVE_FORMAT_VERSION` [FIXED] = 1), `SeasonSaveBlobs` (deframe result), `SeasonSaveCodec`
> (pure frame/deframe of the two opaque sub-blobs, overflow-safe bounds + fail-loud gates),
> `SeasonSaveContents` (`WorldStore` + nullable `MatchEngine`), `SeasonSaveManager` (`Save(world,
> matchOrNull, path)` / `Load(path, squads) → SeasonSaveContents`, atomic temp→fsync→rename).
> `MatchSaveManager` gains public `Encode(engine) → byte[]` / `Restore(blob, squads) → MatchEngine` (KD-5;
> `Save`/`Load` refactored to delegate, behaviour-identical — all 279 match-engine tests still green). New
> `SeasonSaveManagerTests` (18): disk round-trip determinism for a no-match season + a season with a
> neutral / distinct-squad match (via `ISquadProvider`), `SeasonSaveCodec` round-trip (with/without match,
> empty world) + all fail-loud gates, and `SeasonSaveManager` missing/corrupt/no-provider/null-world/
> overwrite. No `SNAPSHOT_SCHEMA_VERSION` / `WORLD_STORE_FORMAT_VERSION` / `MATCH_SAVE_FORMAT_VERSION`
> change. **Implementation-time scope note:** the booking-cursor (KD-8) case §5 mentions is a property of
> the nested match blob (already locked by `MatchSaveManagerTests`) and needs the match-test-internal
> cursor seam, so it is not re-driven at the season level — the season frame provably cannot alter it
> (KD-2). **Full dotnet gate: PASSED, 0 failures (whole tree green; 18 new season-save tests; SDK via
> apt).** This closes the snapshot-deserialize N2 / Phase G-Phase 3 unified season save.)
> **Last Updated (prior):** July 22, 2026 (v0.3 — **Self-adversarial review AR-2: 0H + 0M + 1L — CONVERGENCE
> (L-only round closes the cycle).** Fresh-eyes re-walk of the whole v0.2 surface. Verified sound with no
> change: (a) the FR-LW-003 resolution — the season root is the only assembly that may see both blobs, and
> neither referenced assembly references the other (KD-1); (b) the two-opaque-blob frame keeps all four
> inner versions untouched and adds only the fourth season-frame version (KD-2/KD-4); (c) the optional
> match block is strictly last, so a no-match file ends at the world block and the trailing-byte guard is
> exact (KD-3/KD-8); (d) the public match blob API (KD-5) is additive and leaves `MatchSaveManager`'s own
> behaviour identical; (e) the inherited fingerprint/MXCSR gates. L-1: pinned that `Save` computes both
> sub-blobs (`world.Snapshot()` then `MatchSaveManager.Encode(engine)`) **fully, before opening the file**,
> and that neither capture mutates its source — the `MatchSaveManager.Save` "blob-before-file" precedent
> (KD-8). Cycle converged — ready to implement.)
> **Last Updated (prior):** July 22, 2026 (v0.2 — **Self-adversarial review AR-1: 0H + 2M + 3L, all resolved.**
> M-1: pin the `SeasonSaveCodec.Encode` null-guard on `worldBlob` and that match presence keys on
> `matchBlobOrNull == null`, not on length (KD-8 / §4). M-2: the §5 world-half determinism acceptance was
> under-specified — "field-identical" is now pinned to the `WorldStoreTests` re-`Snapshot()` byte-equality
> comparison, and the reference store is the saved store itself (capture is non-mutating). L-1: the season
> save **reuses, not replaces**, `WorldStore.Snapshot`/`Restore` — still the living-world assembly's own
> standalone composite save (N1 note). L-2: the world (calendar-day) and match (match-tick) clocks are
> independent by design — a reader must not expect them synchronized (R3). L-3: record the inherited
> property that the match restore's fingerprint + MXCSR float-mode gates run on season Load unchanged, so
> a season saved under a divergent environment is rejected (KD-5).)
> **Last Updated (prior):** July 22, 2026 (v0.1 — initial design supplement, pre-AR.)
> **Status:** DESIGN SUPPLEMENT — AR-CONVERGED, LANDED July 22, 2026 (Stage 0+1 integration scaffolding; NOT a numbered spec, same
> governance class as `match-engine-design.md`, `snapshot-deserialize-design.md`, and
> `match-save-file-design.md`). This is the **G-Phase 3 "N2 unified season save"** deliverable
> (`snapshot-deserialize-design.md` N2 / `match-save-file-design.md` N1 / `match-engine-design.md`
> §5 Phase G-Phase 3): the on-disk save file that folds the living-world `WorldStore` composite together
> with an optional in-progress match into one file.
> **Author:** —
> **Purpose:** Authoritative design for the **unified season save file** — the durable artifact that
> persists a season (the living-world `WorldStore` composite plus, when one is in progress, a running
> `MatchEngine`) to disk and reconstructs both. Read `CLAUDE.md`, `src/CLAUDE.md`,
> `docs/tracking/match-engine-design.md`, `docs/tracking/snapshot-deserialize-design.md`, and
> `docs/tracking/match-save-file-design.md` first.

---

## 0. Scope and governance

Neither the match engine nor the living world is covered by any of the 26 approved specs' persistence
gates directly at the season level. Two save capabilities already exist and are governed by their own
notes:

- **The on-disk match save** — `MatchSaveManager` / `MatchSaveCodec` (governed by
  `docs/tracking/match-save-file-design.md`) persist a running `MatchEngine` to disk and reconstruct it
  via `MatchEngine.RestoreFromSnapshot`.
- **The living-world composite save** — `WorldStore.Snapshot()` / `WorldStore.Restore()` (governed by
  Living World #22 §4.6 / §7.1 KD-10) persist the whole `WorldStore` (the §4.6 four-store block + the
  manager id + the `world.text` RNG cursor + the FR-LW-022 membership roster) as one canonical byte blob.

Both `match-save-file-design.md` and `snapshot-deserialize-design.md` name the one thing they
deliberately deferred:

> **N2 (match-save-file-design.md N1)** — The unified match/season save (folds the `WorldStore`
> composite into the same file). **Blocked on FR-LW-003 + the season save-file root.**

This note designs that root. **FR-LW-003** (Living World #22 §2) is a MUST:

> No match hot-path assembly (`Physics`/`Mechanics`/`AI`) may reference this assembly; the match engine
> consumes nothing from it and is only read by it via outcome events.

So the match engine cannot reference the living-world assembly, and the living-world assembly does not
reference the match engine (its asmdef references only `DeterministicSim`). Neither save can host the
other's blob from inside its own assembly. The **season save-file root** is the assembly that resolves
this: a new persistence/composition layer that sits **above both** and references both, exactly as
`match-viewer` sits above `match-engine`. It writes **one file** that carries the two independent,
self-contained blobs side by side.

### 0.1 What already exists

| Piece | Status |
|---|---|
| `WorldStore.Snapshot()` → `byte[]` / `static WorldStore.Restore(byte[])` (living-world composite) | ✅ exists (`WORLD_STORE_FORMAT_VERSION` 2) |
| `MatchSaveCodec.Encode(matchSeed, header, payload)` / `Decode(byte[])` (match blob codec) | ✅ exists (`MATCH_SAVE_FORMAT_VERSION` 1) |
| `MatchSaveManager.Save(engine, path)` / `Load(path, squads)` (match on-disk file) | ✅ exists |
| `MatchEngine.MatchSeed` / `CaptureDurableHeader` / `CaptureDurablePayload` (durable capture) | ✅ exist (`MatchSeed` public; capture seams **internal** to match-engine) |
| `MatchEngine.RestoreFromSnapshot(header, payload, matchSeed, squads)` (reader) | ✅ exists, public |
| `CanonicalSerializer` (−0.0 / NaN-safe Write/Read primitives) | ✅ exists (`DeterministicSim`) |
| The §4.6.1.1 atomic-write contract (temp→fsync→rename) | ✅ exists as a pattern (re-implemented in `SaveManager`, `MatchSaveManager`) |
| **Season save-file root: the assembly + file that folds the WorldStore composite + optional match** | ❌ this note |

The gap is small and precise: an assembly above both `match-engine` and `living-world`, a byte frame
that packs `WorldStore.Snapshot()` + an optional match blob into one self-describing, version-gated
file, an atomic writer, and a fail-loud loader that reconstructs both.

---

## 1. Goals and non-goals

**Goals:**

- **G1** — A new **season save-file root** assembly (`TacticalDirector.SeasonSave`) that references both
  `match-engine` and `living-world` (and `DeterministicSim`), respecting FR-LW-003 (neither referenced
  assembly references the other; the root is not a Physics/Mechanics/AI hot-path assembly).
- **G2** — A byte codec that frames the living-world composite blob and an **optional** match save blob
  into one self-describing, version-gated season blob, and deframes it — fail-loud on any framing/bound
  violation.
- **G3** — `SeasonSaveManager.Save(world, matchOrNull, path)` — captures the `WorldStore` composite and
  (when present) the running match, encodes the season blob, and writes it to disk **atomically** (the
  §4.6.1.1 contract).
- **G4** — `SeasonSaveManager.Load(path, squads) → SeasonSaveContents` — reads the file, deframes it, and
  reconstructs the `WorldStore` (always) and the `MatchEngine` (only if the save carried a match).
- **G5** — **Disk round-trip determinism** as the acceptance criterion, for **both** cases (with and
  without an in-progress match): `Save a season → Load it → advance both halves` reproduces an
  uninterrupted run — the `WorldStore` field-identical + resuming its `world.text` stream (the #22 §4.6
  property, now through the season file) **and** the match's digest chain byte-for-byte (the
  `match-save-file-design.md` G4 property, now through the season file). This composes the two existing
  round-trip properties through **one** file.

**Non-goals (deferred):**

- **N1** — Folding the living-world block **into the match snapshot** (`SNAPSHOT_SCHEMA_VERSION` bump).
  This design deliberately does **not** do that — it would require the match snapshot to host the
  living-world block and the match engine to reference the living-world assembly, which **FR-LW-003
  forbids**. The two blobs live side by side in one file instead (KD-2). This clarifies (supersedes) the
  older aspiration recorded in `WorldStateSerializer.cs` / `WorldStore.cs` that the season root would
  fold the §4.6 block into "the canonical snapshot field set with a #16 `SNAPSHOT_SCHEMA_VERSION` bump".
  The season save **reuses, not replaces**, `WorldStore.Snapshot`/`Restore` (they remain the living-world
  assembly's own standalone composite save; the season root nests the blob they already produce) and
  `MatchSaveManager` (the standalone match file stays); it adds a bundling layer above both. *(AR-1 L-1.)*
- **N2** — Transfer market, aging, career progression, and the **match-outcome → world ingest** (Living
  World #22 WorldLoop phase 1). The ingest's producer (structured match-outcome events) does not exist,
  and FR-LW-031 forbids building a phantom consumer; the season file bundles a world and a match that are
  coherent *by construction* (same file) but carries **no cross-reference key** linking the match's
  ClubIds to world entity ids — that linkage is career-mode work (`#27` Stage-1+, master plan §4.3/§4.4).
- **N3** — A multi-slot / auto-save rotation policy, or compression. One season save = one file.
- **N4** — The native MXCSR live-mode query (host-blocked; the match restore's KD-6 float-mode gate runs
  unchanged inside `RestoreFromSnapshot` — this note does not touch it).

---

## 2. Key decisions

### KD-1 — A new season save-file root assembly, above both, respecting FR-LW-003

The root is a new assembly `TacticalDirector.SeasonSave` (`src/season-save/`), referencing
`TacticalDirector.MatchEngine`, `TacticalDirector.LivingWorld`, and `TacticalDirector.DeterministicSim`.
FR-LW-003 bars a **match hot-path assembly** (Physics/Mechanics/AI) from referencing the living-world
assembly — the season root is **none of those**: it is a persistence/composition-root tooling layer, the
same layer class as `match-viewer` (which references `match-engine`). Neither `match-engine` nor
`living-world` gains a reference to the other; both stay independent, and the root composes them. This is
the *only* place both blobs can be assembled, precisely because it is the only assembly that may see both.

### KD-2 — The file is a thin frame over two self-contained blobs (no cross-parse)

The season blob is a frame around **two existing, independently version-gated byte blobs**:

1. `WorldStore.Snapshot()` — the living-world composite (its own `WORLD_STORE_FORMAT_VERSION` gate +
   trailing-byte guard, re-checked by `WorldStore.Restore`).
2. The **match save blob** (`MatchSaveManager.Encode` — see KD-5), when a match is in progress (its own
   `MATCH_SAVE_FORMAT_VERSION` gate + the three inner snapshot versions + trailing-byte guard, re-checked
   by the match decode/restore path).

The season codec **never parses the internals of either** — it stores each as an opaque length-prefixed
sub-blob and hands it back verbatim on load. This is exactly how `WorldStore.Snapshot` nests the
`WorldStateSerializer` block behind a length prefix, and it keeps the season frame ignorant of (and
robust to) every future change inside either blob: the nested version gates catch drift, and the season
codec adds only its own frame gate. It also means the **three versions the match save already frames plus
`WORLD_STORE_FORMAT_VERSION` are all untouched** — the season file only adds a fourth (KD-4).

### KD-3 — The match blob is optional (a `matchPresent` flag byte)

A season always has a world, but only *sometimes* has an in-progress match (between fixtures there is
none). The frame carries a `matchPresent` flag byte: `1` ⇒ a match blob follows the world blob; `0` ⇒ no
match, the world blob is the whole payload. `Load` returns a `SeasonSaveContents` whose `Match` is `null`
in the no-match case. The unified save must round-trip **both** cases (G5), so both are first-class and
both are tested.

### KD-4 — A fourth distinct format version

The first field is a `SEASON_SAVE_FORMAT_VERSION` u32 — a **fourth** version, distinct from every version
it frames:

- `DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION` — the #16 header framing schema.
- `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` — the match-engine body schema.
- `MatchEngineConstants.MATCH_SAVE_FORMAT_VERSION` — the match file framing.
- `LivingWorldConstants.WORLD_STORE_FORMAT_VERSION` — the world composite framing.
- `SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION` — **the season file framing (this note).**

On load, a `SEASON_SAVE_FORMAT_VERSION` mismatch throws (fail-loud) — **no cross-version migration at
Stage 0**, the same posture as every sibling version gate. The four inner versions ride inside their
respective sub-blobs and are re-checked by `WorldStore.Restore` / the match decode path.

### KD-5 — Reuse the match save through a public blob API, don't reach into internals

To compose the match sub-blob the season root needs the match save **as a byte value**. The match capture
seams (`CaptureDurableHeader` / `CaptureDurablePayload`) are **internal** to `match-engine`, so the season
root (a different assembly) cannot call them. `MatchSaveManager` gains two **public** convenience methods:

- `byte[] MatchSaveManager.Encode(MatchEngine engine)` — capture the durable snapshot + `MatchSaveCodec.Encode`.
- `MatchEngine MatchSaveManager.Restore(byte[] blob, ISquadProvider squads = null)` — `MatchSaveCodec.Decode` + `RestoreFromSnapshot`.

`Save`/`Load` are refactored to delegate to these (`Save` = `Encode` + atomic write; `Load` =
`ReadAllBytes` + `Restore`), so the match save's own behaviour is unchanged. This exposes "the match save
as a value" — genuinely useful in its own right — **without** exposing the capture internals, and lets the
season root treat the match save as an opaque blob (KD-2). (Rejected alternative: `InternalsVisibleTo` the
season assembly — it would leak *all* match-engine internals for a two-method need; the public blob API is
the correct, minimal seam.) `MatchSaveManager.Restore` is `RestoreFromSnapshot`, so the match restore's
**fingerprint + MXCSR float-mode gates (KD-6 of the match design / §4.8.2) run on season `Load` unchanged**
— a season whose match blob was captured under a divergent environment is rejected, surfaced through
`SeasonSaveManager.Load`. *(AR-1 L-3.)*

### KD-6 — `ISquadProvider` is a Load-time parameter, never persisted

Inherited from the match save (`match-save-file-design.md` KD-4): a distinct-squad match's blob carries
roster **identity** (ClubId), not the attribute values, and `RestoreFromSnapshot` re-projects them from a
caller-supplied `ISquadProvider`. The provider is **not** written to the season file — the caller owns the
roster store. `SeasonSaveManager.Load(path, squads)` threads it straight into `MatchSaveManager.Restore`
**only when a match blob is present**; a no-match season ignores it (a neutral match needs none).

### KD-7 — Codec split from disk I/O (the `match-save-file-design.md` KD-5 precedent)

The pure frame codec (`SeasonSaveCodec.Encode`/`Decode`, no `System.IO`) is separated from the disk
manager (`SeasonSaveManager.Save`/`Load`, atomic file I/O + capture/restore glue) — the project's
testability split (`WorldStateSerializer` vs `WorldStore`, `MatchSaveCodec` vs `MatchSaveManager`). The
codec frames/deframes **two byte blobs** and is exhaustively unit-testable in memory (round-trip with and
without the match blob, every fail-loud gate) with no temp files; the manager adds only the
`world.Snapshot()` + `MatchSaveManager.Encode(engine)` capture on the way in and the `WorldStore.Restore`
+ `MatchSaveManager.Restore` reconstruction on the way out.

### KD-8 — Atomic write re-implements the §4.6.1.1 contract; fail-loud on every framing violation

`Save` writes to `path + ".tmp"` on the same directory (= same volume, §4.6.1.1 requirement 1),
`Flush(flushToDisk: true)` (the fsync barrier), then atomically renames onto `path` (`File.Replace` when
the destination exists, `File.Move` otherwise — the `SaveManager` netstandard2.1 lesson). A write failure
cleans up the temp and surfaces. `SaveManager` and `MatchSaveManager` both re-implement these steps
(neither exposes a raw-blob atomic write), so `SeasonSaveManager` re-implements the same well-understood
~15-line mirror rather than delegating. `Encode` fails loud on a null `worldBlob` (a season always has a
world — `ArgumentNullException`, mirroring `MatchSaveCodec`'s null guards); a **null** `matchBlobOrNull`
means "no match" (writes `matchPresent = 0`), so **presence keys on the argument being null, not on its
length** — a non-null blob is written even if 0-length (a real match blob never is, and a 0-length one
would simply fail loud at the inner match decode). `Decode` fails loud (throws) on: a null blob; a
`SEASON_SAVE_FORMAT_VERSION` mismatch (KD-4); a `matchPresent` flag that is neither 0 nor 1; a length
prefix (world or match) that would read past the buffer (the `MatchSaveCodec.Require` overflow-safe
posture); and any trailing bytes after the declared content (a truncation/padding guard). The inner blob
drift is caught by `WorldStore.Restore` / the match decode path themselves. *(AR-1 M-1.)*

---

## 3. On-disk layout (v1 as designed; v4 current — see §3.1)

All integers little-endian, via `CanonicalSerializer`. Each sub-blob is `u32 length + length raw bytes`.

```
u32   SEASON_SAVE_FORMAT_VERSION           // KD-4 season-frame version gate
u8    matchPresent                         // KD-3 (1 = a match blob follows the world blob; 0 = none)

// ── World composite block (always present) ────────────────────────────────
u32   worldBlobLength                      // = WorldStore.Snapshot().Length
byte  worldBlob[worldBlobLength]           // opaque WorldStore composite (its own version gate)

// ── Match block (present iff matchPresent == 1) ───────────────────────────
u32   matchBlobLength                      // = MatchSaveManager.Encode(engine).Length
byte  matchBlob[matchBlobLength]           // opaque match save blob (its own version gate)
```

**Decode bounds (KD-8, fail-loud):** `SEASON_SAVE_FORMAT_VERSION` is checked first; `matchPresent` must
be 0 or 1; each `*BlobLength` is checked against the remaining buffer before the copy (a corrupt count
throws, never `OverflowException`/OOM or a silent short read — the overflow-safe `Require(offset, need,
total)` guard `MatchSaveCodec` uses); after the last declared block the read offset must equal the blob
length exactly (trailing-byte guard). The match block is read **only when `matchPresent == 1`**.

### 3.1 Layout amendments (v2–v4) — the frame as it exists in code

The v1 sketch above is the frame as designed; three landings have since amended it, each a
`SEASON_SAVE_FORMAT_VERSION` bump with no migration (KD-4):

- **v2 (#30 T1, FR-SN-020):** the season-state sub-blob (`[len u32]season`) between the world and
  match blocks.
- **v3 (#29/#41 T1, FR-TR-018 / FR-MD-017):** the #29 training and #41 medical sub-blobs between the
  season block and the optional match block — both **mandatory** (an empty career is a zero-club
  block, not an absent one), both typed at the `Encode` seam (`TrainingBlock` / `MedicalBlock`) and
  self-identified by a leading magic (ERR-029-005 / ERR-041-009).
- **v4 (the #29/#41 balance pass, ERR-041-010(b)):** the #30 appearance sub-blob
  (`AppearanceBlock`, magic `"APPR"`) after the medical block — the per-player fielded-XI record
  supplying #41's FR-MD-010 `MatchLoad`, #30-owned because neither career sibling block may describe
  the other's domain. Mandatory on the same grounds.

Current frame: `version → matchPresent → [len]world → [len]season → [len]training → [len]medical →
[len]appearance → ([len]match iff matchPresent)`. The optional match block stays last, where its
presence flag can govern it. (#50's `SaveOriginStamp` and #47's conditional authored-db block remain
future amendments — see #30 Appendix B.)

---

## 4. Component design

- **`src/season-save/season-save.asmdef`** — `TacticalDirector.SeasonSave`; references
  `TacticalDirector.MatchEngine`, `TacticalDirector.LivingWorld`, `TacticalDirector.DeterministicSim`
  (KD-1).
- **`SeasonSaveConstants`** (static) — `[FIXED] SEASON_SAVE_FORMAT_VERSION = 1` (KD-4).
- **`SeasonSaveBlobs`** (readonly struct) — the deframe result: `byte[] WorldBlob` + `byte[] MatchBlob`
  (`null` when absent). Pure bytes; no reconstructed objects (keeps the codec object-free, KD-7).
- **`SeasonSaveCodec`** (static) — `byte[] Encode(byte[] worldBlob, byte[] matchBlobOrNull)` +
  `SeasonSaveBlobs Decode(byte[])`. Pure; no `System.IO`; exact-size buffer; fail-loud decode (KD-8).
- **`SeasonSaveContents`** (readonly struct) — the `Load` result: `WorldStore World` (never null) +
  `MatchEngine Match` (null when the save carried no match).
- **`SeasonSaveManager`** (static) — `Save(WorldStore world, MatchEngine matchOrNull, string path)`
  (capture both → `SeasonSaveCodec.Encode` → atomic write) + `Load(string path, ISquadProvider squads =
  null) → SeasonSaveContents` (read file → `SeasonSaveCodec.Decode` → `WorldStore.Restore` +, when
  present, `MatchSaveManager.Restore`).
- **Additions to `match-engine`** (KD-5): `MatchSaveManager.Encode(MatchEngine) → byte[]` +
  `MatchSaveManager.Restore(byte[], ISquadProvider = null) → MatchEngine` (both public); `Save`/`Load`
  refactored to delegate. No new `MatchEngine` state, no schema change, no capture-seam visibility change.

Off the 60 Hz hot path (a save is a user/host action, not per-tick), so the allocations in
Encode/Decode/Save/Load are permitted (the `WorldStore.Snapshot` / `MatchSaveManager` precedent).

`Save` captures **both** sub-blobs fully — `world.Snapshot()` then `MatchSaveManager.Encode(engine)` (when
a match is present) — and encodes the season blob **before opening the file**, then does the atomic write
(the `MatchSaveManager.Save` blob-before-file precedent). Neither capture mutates its source (both are
pure reads), so a failed write leaves the live `WorldStore`/`MatchEngine` and any existing destination
untouched. *(AR-2 L-1.)*

---

## 5. Acceptance criteria (definition of done)

- **Codec:** in-memory `Encode`→`Decode` round-trips both blobs verbatim, **with a match blob and
  without one**; every fail-loud gate has a test (bad format version, bad `matchPresent` flag, oversized
  world length, oversized match length, trailing bytes, null blob).
- **Disk round-trip determinism (G5), no-match season:** `Save` a `WorldStore` (populated: some
  interactions, at least one arc, a non-zero `world.text` cursor) with `matchOrNull == null` to a temp
  file, `Load` it, then run identical `AdvanceDay` + `GenerateInteractionText` sequences on both the
  loaded store and the **saved store itself** (capture is non-mutating — the `WorldStoreTests` idempotent
  round-trip precedent — so the saved store is a valid uninterrupted reference). "Field-identical" is
  asserted the `WorldStoreTests` way: `loaded.Snapshot()` byte-equals `reference.Snapshot()` after the
  identical advance, and the generated text strings are equal (the #22 §4.6 property, through the season
  file). Loaded `Match` is null. *(AR-1 M-2.)*
- **Disk round-trip determinism (G5), season with an in-progress match:** `Save` the same populated world
  **plus** a running match at tick N (neutral; and a distinct-squad `ConfigureSquads` match via an
  `ISquadProvider` — the case that exercises the provider threading through the season `Load`), `Load` it,
  tick the match to N+K and advance the world → the match digest chain is byte-identical to an
  uninterrupted run **and** the world is field-identical, both through the one season file. (The
  booking-cursor / KD-8 case is a property of the **match blob**, which the season frame nests
  unchanged — it is locked at the match level by `MatchSaveManagerTests` and needs the match-test-internal
  cursor seam, so it is not re-driven here; the season frame provably cannot alter it, per KD-2.)
- **Manager fail-loud:** `Load` of a missing file, a truncated file, a distinct-squad-match season loaded
  with no provider, and `Save` with a null world each fail loud.
- **Match save unchanged:** the `MatchSaveManager.Save`/`Load` refactor onto `Encode`/`Restore` leaves
  every existing `MatchSaveManagerTests` green (behaviour-identical).
- **Full dotnet gate PASSED**, whole tree green; no `SNAPSHOT_SCHEMA_VERSION` / `WORLD_STORE_FORMAT_VERSION`
  / `MATCH_SAVE_FORMAT_VERSION` change (this note only adds a fourth frame around unchanged blobs).

---

## 6. Risks

- **R1 — Frame/blob drift.** A future field added inside the world or match blob but the season frame
  unaware. Mitigation: the season codec stores each blob **opaquely** behind a length prefix and never
  parses it, so an inner change cannot desync the season frame; the inner version gates + the season
  trailing-byte guard turn any real corruption into a fail-loud, and the disk round-trip determinism
  tests turn a mis-framed blob into a divergence — the same defence the nested designs already rely on.
- **R2 — Same-volume temp assumption.** `path + ".tmp"` is on the same directory as `path`, so the
  rename is atomic on one volume (§4.6.1.1). A path whose directory is not writable fails loud at the
  temp write (cleaned up). Directory fsync stays the documented Stage-0 Windows carve-out (`SaveManager`).
- **R3 — Coherence of the bundled world and match.** The file bundles a world and a match that are
  coherent *by being saved together*, but carries no key linking the match's ClubIds to world entity ids
  (N2 / career-mode). At Stage 0 this is correct: the two are independent state and are restored
  independently; a mismatched pairing is a caller error the file format does not (and need not) police.
  Their **clocks are independent by design** — the world advances on `WorldClock` (calendar day, KD-4 of
  #22) and the match on `MatchClock` (match tick); the season file captures each at its own current
  position and a reader must not expect them synchronized. *(AR-1 L-2.)*
- **R4 — `ISquadProvider` supplied for a no-match season.** Harmless: `Load` reconstructs the world and
  ignores `squads` when `matchPresent == 0` (never calls `MatchSaveManager.Restore`). A distinct-squad
  match season loaded with a null/incomplete provider fails loud inside the match restore factory (R4 of
  the match design), surfaced through `SeasonSaveManager.Load`.

---

## 7. Verification approach

Per project convention: this note is adversarially reviewed to convergence before code, then the
implementation is adversarially reviewed to convergence. The central correctness property (disk
round-trip determinism for both the no-match and in-progress-match cases) is the strongest verification —
it composes the real `WorldStore` capture/restore, the real match capture/restore, the season codec,
atomic file I/O, and the decoder, and catches exactly the framing/ordering/bound/optionality class of
defect a per-method unit test would miss.

---

## 8. Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial design supplement. Scope (unified season save = the G-Phase 3 N2 season root folding the WorldStore composite + optional match), FR-LW-003 resolution (new root assembly above both, KD-1), two-opaque-blob frame (KD-2), optional match (KD-3), fourth format version (KD-4), public match blob API (KD-5), layout, components, acceptance, risks. Pre-AR. |
| 0.2 | 2026-07-22 | — | **Self-adversarial review AR-1: 0H + 2M + 3L, all resolved.** M-1: `SeasonSaveCodec.Encode` null-guard on `worldBlob` + presence keyed on `matchBlobOrNull == null` not length (KD-8 / §4). M-2: §5 world-half determinism acceptance pinned to the `WorldStoreTests` re-`Snapshot()` byte-equality comparison + saved-store-as-reference (capture is non-mutating). L-1: season save reuses, not replaces, `WorldStore.Snapshot`/`MatchSaveManager` (N1). L-2: world/match clocks independent by design (R3). L-3: match restore's fingerprint + MXCSR gates run on season Load unchanged (KD-5). |
| 0.3 | 2026-07-22 | — | **Self-adversarial review AR-2: 0H + 0M + 1L — CONVERGENCE.** Fresh-eyes re-walk; FR-LW-003 resolution, two-opaque-blob frame, optional-match-last framing, additive public match API, and inherited gates verified sound with no change. L-1: pinned that `Save` computes both sub-blobs and encodes the season blob before opening the file, and that neither capture mutates its source (KD-8 / §4). Cycle converged — ready to implement. |
| 0.4 | 2026-07-22 | — | **LANDED.** New `src/season-save/` assembly (`SeasonSaveConstants`/`SeasonSaveBlobs`/`SeasonSaveCodec`/`SeasonSaveContents`/`SeasonSaveManager`) + `MatchSaveManager.Encode`/`Restore` public blob API (KD-5). New `SeasonSaveManagerTests` (18). Booking-cursor case not re-driven at season level (nested match-blob property, KD-2). Full dotnet gate PASSED, 0 failures (whole tree green; SDK via apt). Closes snapshot-deserialize N2 / Phase G-Phase 3. |
| 0.5 | 2026-07-22 | — | **Code adversarial review (fresh pass over the shipped diff): 0H + 0M + 2L, both fixed.** L-1: `MatchSaveManager.Save`'s argument-guard order had flipped in the `Encode` delegation (path-before-engine) — restored engine-before-path so a both-invalid call still throws `ArgumentNullException` not `ArgumentException`, keeping the refactor exactly behaviour-identical (`MatchSaveManager.cs` v1.2). L-2: added `Load_NoMatchSeason_WithProvider_IgnoresProvider` locking R4 (a provider on a no-match season is ignored, `Match == null`) — `SeasonSaveManagerTests` 18 → 19. Verified sound with no change: every raw `CanonicalSerializer` read is preceded by an overflow-safe `Require` (a corrupt length > `int.MaxValue` casts negative and is caught by the `need < 0` branch); the two-opaque-blob frame + trailing guard + `matchPresent`/empty-world paths; FR-LW-003 layering; non-mutating capture behind the determinism tests; the atomic-write mirror. Full dotnet gate re-run: PASSED, 0 failures (whole tree green; 19 season-save + 279 match-engine tests). |
