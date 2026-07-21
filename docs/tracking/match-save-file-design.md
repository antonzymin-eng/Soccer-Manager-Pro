# On-Disk Match Save File — Match Engine (Design Supplement)

> **Created:** July 21, 2026
> **Last Updated:** July 21, 2026 (v0.3 — **AR-2 (0H+0M+1L) — CONVERGENCE (L-only round closes the
> cycle).** Fresh-eyes re-walk of the whole v0.2 surface. Verified sound with no change: (a) the
> boot-header/state split (KD-2 O1(b)) — the file carries `matchSeed` + `(SnapshotHeader,
> SnapshotPayload)`, and `RestoreFromSnapshot` needs exactly that triple plus the caller-supplied
> `ISquadProvider`, which is intentionally NOT in the file (KD-4); (b) the fail-loud gate order —
> format-version first, then the two nested length-bounds (fingerprint strings via `ReadString`,
> payload via an explicit bound), then the trailing-byte guard, mirroring `WorldStateSerializer`'s
> `ReadCount` posture; (c) the fingerprint round-trips its 6 `ValidateAgainst` fields so the KD-3
> gate is a real end-to-end check through disk (a save under `CreateStage0Dev` validates; a
> tampered/foreign fingerprint is rejected), closing O3 for the on-disk path. L-1: v0.2 KD-6 said
> the codec "reuses `SaveManager`'s atomic contract" without noting `SaveManager` exposes no
> raw-blob write, so the atomic steps are re-implemented (not delegated) in `MatchSaveManager` —
> clarified. Prior v0.2 — **AR-1: 0H+3M+2L, all resolved.** M-1: v0.1 persisted only
> `(header, payload)` and reconstructed `matchSeed` via a fixed dev seed — wrong; `RestoreFromSnapshot`
> needs the ACTUAL boot seed, so the file now carries it as a boot-header (KD-2 / KD-7 O1(b)). M-2:
> v0.1 skipped the `EnvironmentFingerprint`, so a disk load reconstructed `Fingerprint = null` and the
> KD-6 gate never ran through disk — the fingerprint's 6 fields are now serialized + reconstructed
> (KD-3). M-3: v0.1 had no trailing-byte / payload-length bound guard, so a truncated or padded file
> read past/short of the buffer silently — added the KD-6 fail-loud guards. L-1: the format version
> is a THIRD distinct version (file-framing), separate from the two snapshot schema versions — pinned
> in KD-1. L-2: the `ISquadProvider` is a Load-time PARAMETER, not persisted (the file references
> rosters by ClubId; the caller owns the roster store) — pinned in KD-4.)
> **Status:** DESIGN SUPPLEMENT — AR-CONVERGED (Stage 0+1 integration scaffolding; NOT a numbered
> spec, same governance class as `match-engine-design.md` and `snapshot-deserialize-design.md`). This
> is the **G-Phase 3 "on-disk `SaveManager` fold"** deliverable
> (`snapshot-deserialize-design.md` N1 / `match-engine-design.md` §5 Phase G-Phase 3): the on-disk
> save-file format that *calls* the in-memory reader landed in Phases 1 + 2.
> **Author:** —
> **Purpose:** Authoritative design for the **on-disk match save file** — the durable artifact that
> persists a running `MatchEngine` to disk and reconstructs it via `MatchEngine.RestoreFromSnapshot`.
> Read `CLAUDE.md`, `src/CLAUDE.md`, `docs/tracking/match-engine-design.md`, and
> `docs/tracking/snapshot-deserialize-design.md` first.

---

## 0. Scope and governance

The match engine is not covered by any of the 26 approved specs. It is Stage 0+1 integration
scaffolding governed by `docs/tracking/match-engine-design.md`. Snapshot save/restore *in memory* is
governed by `docs/tracking/snapshot-deserialize-design.md`, whose **N1 non-goal** and **Phase 3** both
name the one piece it deliberately deferred:

> **N1** — On-disk save-file format / `SaveManager` wiring. `SaveManager` exists but writes the header
> `Fingerprint = null` and there is no season save-file root; this note produces the *in-memory* reader
> those will call.

> **G-Phase 3** — the native float-mode query into the KD-6 seam (host-blocked), then **the on-disk
> `SaveManager` fold** + unified season save consume the reader.

This note designs that on-disk fold. It is the read/write bridge between the two capabilities that
already exist — `MatchEngine.SerializeWorldState` (the writer, through the durable-capture seams) and
`MatchEngine.RestoreFromSnapshot` (the reader, Phases 1 + 2) — and a **file**. It does **not** design
the unified match/season save (N2, folds the living-world `WorldStore` composite into the same file;
blocked on FR-LW-003 and the season save-file root), the transfer market, or aging.

### 0.1 What already exists

| Piece | Status |
|---|---|
| `MatchEngine.SerializeWorldState` (writer) → `SnapshotPayload` | ✅ exists (v17) |
| Durable-capture seams (`SnapshotHeader` + `SnapshotPayload` deep copies) | ✅ exist (internal, snapshot-deserialize Phase 1) |
| `MatchEngine.RestoreFromSnapshot(header, payload, matchSeed, squads)` (reader) | ✅ exists (Phases 1 + 2) |
| `CanonicalSerializer` (−0.0 / NaN-safe Write/Read primitives) | ✅ exists |
| `SaveManager` (deterministic-sim atomic-write contract, §4.6.1.1) | ✅ exists — but writes its own `(header,payload)` blob format, omits the boot seed + the fingerprint, and cannot reference `match-engine` (layering) |
| **On-disk match-save file (boot-header + header + payload) + atomic writer/loader** | ❌ this note |

The gap is small and precise: a byte format that packs the three things `RestoreFromSnapshot` needs —
the boot `matchSeed` (which the payload does not carry, KD-7 O1), the `SnapshotHeader`, and the
`SnapshotPayload` — plus an atomic writer and a fail-loud loader.

---

## 1. Goals and non-goals

**Goals:**

- **G1** — A byte codec that encodes `(matchSeed, SnapshotHeader, SnapshotPayload)` into a single
  self-describing, version-gated blob and decodes it back, fail-loud on any framing/bound violation.
- **G2** — `MatchSaveManager.Save(engine, path)` — captures a durable snapshot from a running engine
  and writes the blob to disk **atomically** (the `SaveManager` §4.6.1.1 temp→fsync→rename contract).
- **G3** — `MatchSaveManager.Load(path, squads) → MatchEngine` — reads the blob and reconstructs a
  ready-to-tick engine via `RestoreFromSnapshot`.
- **G4** — **Disk round-trip determinism** as the acceptance criterion: `save engine A at tick N to a
  file → Load the file → tick to N+K` reproduces A's digest chain byte-for-byte — the on-disk
  analogue of the in-memory G3 (`snapshot-deserialize-design.md` KD-5), for both the neutral path and
  a distinct-squad (`ConfigureSquads`) match through an `ISquadProvider`.

**Non-goals (deferred):**

- **N1** — The unified match/season save (folds `WorldStore` composite). Blocked on FR-LW-003 + the
  season save-file root.
- **N2** — Replay-scrub / rewind UI (the reader enables it; presentation is Stage-1 UI).
- **N3** — The native MXCSR live-mode query (host-blocked; the KD-3 gate wires the recorded fingerprint
  today and becomes a real float-mode gate when the query lands — unchanged by this note).
- **N4** — A multi-snapshot / auto-save rotation policy, save-slot management, or compression. One
  save = one file with one snapshot.

---

## 2. Key decisions

### KD-1 — A single version-gated blob format, distinct format version

The file is one blob written/read through `CanonicalSerializer` (the −0.0 / NaN-canonicalising encoder
the payload is already written with). The **first field is a `MATCH_SAVE_FORMAT_VERSION` u32** — a
**third** version distinct from the two snapshot schema versions it frames:

- `DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION` — the #16 header FRAMING schema (`SnapshotHeader`).
- `MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` — the match-engine BODY schema (`SerializeWorldState`).
- `MatchEngineConstants.MATCH_SAVE_FORMAT_VERSION` — the FILE framing (this note).

On load, a `MATCH_SAVE_FORMAT_VERSION` mismatch throws (fail-loud) — **no cross-version migration at
Stage 0**, the same posture as the payload's own schema gate and `WorldStateSerializer`'s version gate.
(The two *inner* schema versions ride inside the header/payload and are re-checked by
`RestoreFromSnapshot` itself, so a file whose body schema drifted from the running build fails there.)

### KD-2 — The file carries the boot seed (the KD-7 O1(b) boot-header, now decided)

`snapshot-deserialize-design.md` KD-7/O1 recorded the open question: the payload is cross-tick *state*,
not the boot *constants* (the RNG `matchSeed`, the Stage-0 formation) an engine needs to exist, so
`RestoreFromSnapshot` takes `matchSeed` as an explicit parameter, and "either (a) the caller persists
the boot inputs, or (b) a small boot-header is prepended to the save … revisited when the on-disk
save-file root (N1) is designed." **This is that decision point.** The file adopts (b): it prepends a
boot-header carrying `matchSeed` (u64), so the file is self-sufficient — a loader does not need the
caller to separately remember the seed. The Stage-0 formation is `MatchEngineConstants.STAGE0_FORMATION`
(a boot constant, identical for every match), so it is NOT stored — a future change that makes formation
a per-match input adds a field here and bumps `MATCH_SAVE_FORMAT_VERSION`.

### KD-3 — The `EnvironmentFingerprint` is serialized so the KD-6 gate runs through disk

`RestoreFromSnapshot` step 0 validates `header.Fingerprint` against the live `CreateStage0Dev()` tuple
(`snapshot-deserialize-design.md` KD-6) — but only when the header carries a non-null fingerprint (O3:
"validate when present, skip-with-note when null"). At capture time the durable header's fingerprint is
the live `CreateStage0Dev()` (set at boot), so the file **serializes the fingerprint's 6
`ValidateAgainst` fields** (`WorkerCount` i32 + the five length-prefixed strings) and reconstructs it on
load. Consequence: the KD-6 gate is exercised **end-to-end through disk** — a file captured under
`CreateStage0Dev` validates on load, while a file whose fingerprint was tampered/captured under a
different environment is rejected (`ERR_DS_REPLAY_ENV_MISMATCH` → the factory throws). This closes
`snapshot-deserialize-design.md` O3 for the on-disk path (the on-disk header no longer writes
`Fingerprint = null` the way the deterministic-sim `SaveManager` does — that is the specific N1 gap this
note fills). A `fingerprintPresent` flag byte keeps the format forward-compatible with a null fingerprint.

### KD-4 — The `ISquadProvider` is a Load-time parameter, never persisted

A distinct-squad match's payload carries only the roster **identity** (`_rosterClubId[]`, v16), not the
attribute values; `RestoreFromSnapshot` re-projects them from a caller-supplied `ISquadProvider` (Phase 2
/ #27 T3). The provider is **not** written to the file — the file references rosters by `ClubId`, and the
caller owns the roster store (a `Squad` database) that resolves them. `MatchSaveManager.Load(path,
squads)` threads the caller's provider straight into `RestoreFromSnapshot`. This matches the reader's own
contract (the provider is a restore input, not snapshot state) and keeps the save file a pure
match-state artifact, not a roster snapshot. A neutral match needs no provider (both `_rosterClubId ==
NO_ROSTER_CLUB_ID`), exactly as in-memory restore.

### KD-5 — Codec split from disk I/O for testability

The pure byte codec (`Encode`/`Decode`, no `System.IO`) is separated from the disk manager
(`Save`/`Load`, atomic file I/O), the project's testability split (`WorldStateSerializer` vs
`WorldStore`, `ScenarioRunner` vs its file loader, `GameplayConfig` vs `GameplayConfigFileLoader`). The
codec is exhaustively unit-testable in memory (round-trip, every fail-loud gate) with no temp files; the
manager adds only the atomic-write wrapper and the `capture → Encode` / `read → Decode →
RestoreFromSnapshot` glue.

### KD-6 — Atomic write mirrors the §4.6.1.1 contract; fail-loud on every framing violation

`Save` writes to `path + ".tmp"` on the same directory (= same volume, §4.6.1.1 requirement 1),
`Flush(flushToDisk: true)` (the fsync barrier), then atomically renames onto `path`
(`File.Replace` when the destination exists, `File.Move` otherwise — the `SaveManager` netstandard2.1
lesson: `File.Move(overwrite:true)` does not exist in Unity's BCL). A write failure cleans up the temp
and surfaces. `SaveManager` exposes **no** raw-blob atomic-write method (only its own `(header,payload)`
format), so these steps are **re-implemented** in `MatchSaveManager`, not delegated — a small,
well-understood mirror of the certified pattern. `Decode` fails loud (throws) on: a
`MATCH_SAVE_FORMAT_VERSION` mismatch (KD-1); a length-prefix that would read past the buffer (the payload
length and the fingerprint string lengths, the `WorldStateSerializer.ReadCount` posture); and any
trailing bytes after the declared content (a truncation/padding guard, `snapshot-deserialize-design.md`
KD-1 / R1). The inner snapshot schema drift is caught by `RestoreFromSnapshot` itself (its own
version-gate + trailing-byte guard over the payload).

---

## 3. On-disk layout (v1)

All integers little-endian, via `CanonicalSerializer`. `string` = u32 length + ASCII bytes.
`digest[32]` = 32 raw bytes (fixed width, not length-prefixed).

```
u32   MATCH_SAVE_FORMAT_VERSION            // KD-1 file-framing version gate
u64   matchSeed                            // KD-2 boot-header

// ── SnapshotHeader block ──────────────────────────────────────────────────
u32   header.SchemaVersion                 // #16 framing schema (round-trip fidelity)
u16   header.DigestVersion
u64   header.Tick
byte  header.PrevSnapshotDigest[32]
byte  header.CurrentSnapshotDigest[32]
u64   header.Cursor.Tick
u8    header.Cursor.PhaseOrdinal
u8    fingerprintPresent                   // KD-3 (1 = the 6 fields follow; 0 = null)
  // if present (EnvironmentFingerprint, the 6 ValidateAgainst fields):
  i32     WorkerCount
  string  SchedulerPolicy
  string  ReductionTopology
  string  SimdFeatureLevel
  string  FloatModelHash
  string  UnicodeNormalizationVersion

// ── SnapshotPayload block ─────────────────────────────────────────────────
u32   payloadLength                        // = SnapshotPayload.BytesWritten
byte  payload[payloadLength]               // the SerializeWorldState body (+ event ledger)
```

**Decode bounds (KD-6, fail-loud):** every `ReadString` length and `payloadLength` is checked against
the remaining buffer before the copy (a corrupt count throws `ArgumentException`, never
`OverflowException`/OOM or a silent short read); `payloadLength` is additionally checked against
`SnapshotPayload.Capacity`; after the payload copy the read offset must equal the blob length exactly
(trailing-byte guard).

---

## 4. Component design

- **`MatchEngine`** gains: a public `MatchSeed` read-only property (the boot seed the save persists),
  and the two durable-capture seams promoted from `TestOnly_` to production internal names
  (`CaptureDurableHeader` / `CaptureDurablePayload`) since they now have a production consumer
  (`MatchSaveManager`) — the snapshot-restore tests are repointed to the production names (mechanical).
- **`MatchSaveContents`** (readonly struct) — the decode result: `MatchSeed` + `Header` + `Payload`.
- **`MatchSaveCodec`** (static) — `byte[] Encode(ulong matchSeed, SnapshotHeader, SnapshotPayload)` +
  `MatchSaveContents Decode(byte[])`. Pure; no `System.IO`; exact-size buffer; fail-loud decode.
- **`MatchSaveManager`** (static) — `Save(MatchEngine, string path)` (capture → Encode → atomic write)
  + `Load(string path, ISquadProvider squads = null)` (read file → Decode → RestoreFromSnapshot).
- **`MatchEngineConstants.MATCH_SAVE_FORMAT_VERSION`** ([FIXED] = 1).

Off the 60 Hz hot path (a save is a user/host action, not per-tick), so the allocations in
Encode/Decode/Save/Load are permitted (the `WorldStore.Snapshot` / `SaveManager` precedent).

---

## 5. Acceptance criteria (definition of done)

- **Codec:** in-memory Encode→Decode round-trips `(matchSeed, header incl. fingerprint + digest chain +
  cursor, payload)` field-for-field; every fail-loud gate has a test (bad format version, oversized
  payload length, oversized fingerprint string length, trailing bytes).
- **Disk round-trip determinism (G4):** `Save` a neutral match at tick N to a temp file, `Load` it, tick
  to N+K → digest chain byte-identical to an uninterrupted run (the in-memory G3 driver, now through
  disk); same for a distinct-squad `ConfigureSquads` match with an `ISquadProvider`; and for a match with
  a booking before the save (the KD-8 cursor regression, through disk).
- **Manager fail-loud:** `Load` of a missing file, a truncated file, and a distinct-squad save with no
  provider each fail loud.
- **Full dotnet gate PASSED**, whole tree green; no `SNAPSHOT_SCHEMA_VERSION` change (the reader/writer
  are unchanged — this note only adds a file frame around them).

---

## 6. Risks

- **R1 — File/reader drift.** A future field added to the header/payload but not the codec truncates the
  save. Mitigation: the header/payload are serialized as opaque blocks (the payload verbatim; the header
  field-by-field kept adjacent to Decode), the trailing-byte guard (KD-6) turns an under-read into a
  fail-loud, and the disk round-trip determinism test turns a mis-ordered field into a digest divergence
  — the same defense the in-memory reader relies on.
- **R2 — Same-volume temp assumption.** `path + ".tmp"` is on the same directory as `path`, so the rename
  is atomic on one volume (§4.6.1.1). A caller passing a path whose directory is not writable fails loud
  at the temp write (cleaned up). Directory fsync stays the documented Stage-0 Windows carve-out
  (`SaveManager`'s note).
- **R3 — Fingerprint round-trip fidelity.** The reconstructed fingerprint must reproduce the 6
  `ValidateAgainst` fields exactly or a same-environment load spuriously fails the KD-3 gate. Mitigation:
  the 6 fields are the whole of `ValidateAgainst`; a round-trip test asserts a Save→Load under
  `CreateStage0Dev` passes the gate, and a tampered-fingerprint file is rejected.

---

## 7. Verification approach

Per project convention: this note is adversarially reviewed to convergence before code (AR-1, AR-2 in the
header), then the implementation is adversarially reviewed to convergence. The central correctness
property (disk round-trip determinism) is the strongest verification — it composes the real writer,
codec, atomic file I/O, decoder, and reader, and catches exactly the omission/ordering/bound class of
defect a per-method unit test would miss.

---

## 8. Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-21 | — | Initial design supplement. Scope (on-disk match save file = the G-Phase 3 `SaveManager` fold / N1), goals, layout, components. |
| 0.2 | 2026-07-21 | — | **Self-adversarial review AR-1: 0H + 3M + 2L, all resolved.** M-1: persist the boot `matchSeed` (the file was not self-sufficient without it — KD-2). M-2: serialize the `EnvironmentFingerprint` so the KD-6 gate runs through disk (KD-3). M-3: add the trailing-byte / length-bound fail-loud guards (KD-6). L-1: pin `MATCH_SAVE_FORMAT_VERSION` as a third distinct version (KD-1). L-2: pin the `ISquadProvider` as a Load parameter, not persisted (KD-4). |
| 0.3 | 2026-07-21 | — | **Self-adversarial review AR-2: 0H + 0M + 1L — CONVERGENCE.** Fresh-eyes re-walk; boot-header/state split, gate order, and fingerprint round-trip verified sound with no change. L-1: clarified that `SaveManager` exposes no raw-blob atomic write, so the atomic steps are re-implemented (not delegated) in `MatchSaveManager` (KD-6). Cycle converged — ready to implement. |
