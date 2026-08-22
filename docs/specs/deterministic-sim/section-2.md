# Deterministic Simulation Specification #16 — Section 2: System Overview

## 2.0 Identifier Taxonomy
This spec uses a single prefix family for internal traceability. Cross-spec references continue to use the CLAUDE.md taxonomy (`XC-`, `FM-`, `EC-`, `ERR-`).

| Prefix | Scope | Example |
|---|---|---|
| `FR-DS-NNN` | Functional requirement (this spec) | `FR-DS-003` |
| `VR-DS-NNN` | Verification requirement (this spec) | `VR-DS-001` |
| `OPS-DS-NNN` | Operational/governance requirement | `OPS-DS-001` |
| `T-DS-<area>-NNN` | Test card | `T-DS-ORDER-001` |
| `GV-<area>-NNN` | Golden vector | `GV-RNG-001` |
| `XC-NNN` | Cross-spec reference (CLAUDE.md taxonomy) | `XC-016-001` |
| `FM-NNN` | Formula reference (CLAUDE.md taxonomy) | `FM-016-001` |
| `EC-NNN` | Edge-case reference (CLAUDE.md taxonomy) | `EC-016-001` |
| `ERR-NNN` | Spec Error Log entry (CLAUDE.md taxonomy) | `ERR-016-001` |

The earlier `FR-DET-` / `VR-DET-` / `OPS-DET-` outline prefixes are deprecated and have been superseded by the `-DS-` family.

## 2.1 Functional Requirements
- **FR-DS-001:** Simulation MUST execute with canonical per-tick phase order.
- **FR-DS-002:** Authoritative intra-phase iteration MUST use deterministic key order.
- **FR-DS-003:** Authoritative random draws MUST use deterministic stream derivation and branch-safe consumption.
- **FR-DS-004:** Snapshot serialization MUST follow canonical binary layout and schema versioning.
- **FR-DS-005:** Replay engine MUST reconstruct deterministic state from snapshot + authoritative inputs.
- **FR-DS-006:** Save/load transaction MUST be atomic across all authoritative domains.
- **FR-DS-007:** Divergence detection MUST classify mismatches as hard desync, soft drift, or cosmetic divergence.
- **FR-DS-008:** Tooling MUST emit first divergent tick/phase/field and RNG cursor diffs.
- **FR-DS-009 (Stage 5+):** Cross-platform certification suite MUST pass before release. Stage gating is normative: at Stage 0 this requirement is NOT a release gate (single-machine determinism only); at Stage 5+ it becomes a blocking release gate. See `FR-DS-009-GATE` in §5.5 for the per-stage enforcement table — that gate is the operational binding of this FR.
- **FR-DS-010:** At match start the runtime MUST capture `EnvironmentFingerprint` (worker count, scheduler policy, reduction topology, SIMD level, float-model hash) and embed it in every snapshot header for that match; mid-match mutation of any pinned field is forbidden.
- **FR-DS-011:** A Tier-B field present in digest scope without an approved tolerance row in the tolerance matrix MUST fail validation with `ERR_DS_TIERB_TOLERANCE_MISSING`; no silent fallback epsilon is permitted.
- **FR-DS-012:** The replay engine MUST execute the 8-step lifecycle (§4.2.2) in strict order; each step MUST fail deterministically with its assigned error code and MUST NOT proceed to the next step on failure.
- **FR-DS-014:** Every snapshot header MUST carry a `buildHash` identifying the compiled binaries the run executed on, computed per §2.3.2 over a **declared** authoritative assembly closure; a restore or replay whose recorded `buildHash` differs from the live one MUST abort with `ERR_DS_REPLAY_BUILD_MISMATCH` before any state is rehydrated. `buildHash` MUST NOT appear in any digest preimage. Added v1.2 (`ERR-016-009`).
- **FR-DS-013:** Stage-0 `float` fields classified Tier-A MUST satisfy both §1.3.1.1 conditions (pinned execution environment recorded in `EnvironmentFingerprint`, and deterministic reduction topology); fields that cannot satisfy these conditions MUST be classified Tier-B with an approved tolerance row.

## 2.2 Architecture Overview
Core components:
1. **Tick Orchestrator:** enforces phase graph and legal phase boundaries.
2. **DeterministicRngService:** owns stream derivation, cursor management, reservation API.
3. **Authoritative State Store:** Tier A/B data, ownership metadata, mutation guards.
4. **Snapshot Codec:** canonical binary encoding + digest chain generation.
5. **Replay Runtime:** snapshot load, validation, rehydration, input reapplication.
6. **Determinism Auditor:** per-phase digest generation, diffing, classification.

### 2.2.1 Runtime sequence example
| Step | Action | Required invariant |
|---|---|---|
| 1 | Load authoritative input frame | input timestamp corresponds to current tick |
| 2 | Execute canonical phases | phase order cannot branch |
| 3 | Emit event ledger | sequence IDs monotonically increase |
| 4 | Compute phase digests | canonical serialization order frozen |
| 5 | Save snapshot (if scheduled) | digest chain links to previous snapshot |

## 2.3 Data Structures
- `DeterminismContext { buildHash, matchSeed, schemaVersion, digestVersion }`
- `PhaseDigest { tick, phaseId, digest }`
- `RngStreamKey { subsystemId, entityId, streamVersion }` (persistent per-(subsystem, entity, version); see §3.2.1)
- `RngCursor { streamKey, counter, actionOrdinal }` (per-stream draw counter and reservation index; see §3.2.5)
- `SnapshotHeader { schemaVersion, tick, prevSnapshotDigest, environmentFingerprint }` (see §4.8 for fingerprint contents)
- `DespawnLog : array<DespawnEntry>` where `DespawnEntry { entityId : u32, finalActionOrdinal : u64, finalRngCursor : u64, despawnTick : u64 }` (Tier A authoritative state; canonical sort by `entityId` ascending; see §3.2.5.3)
- `ReplayCursor { tick : u64, phaseOrdinal : u8 }` — the replay runtime's logical position in the canonical pipeline. `phaseOrdinal` follows the §5.10 mapping (`Input=0, Intent=1, AI=2, Physics=3, Resolve=4, Events=5, Snapshot=6`). The `EndOfSnapshot[T]` boundary referenced by §4.2.2 step 7 corresponds to `ReplayCursor { tick=T, phaseOrdinal=6 }` (i.e. immediately after the `Snapshot` phase of tick `T` has committed). The replay runtime MUST advance the cursor exactly one phase boundary at a time during reapplication; no cursor value other than these phase-boundary positions is legal mid-load.
- `ToleranceRow { fieldPath, tier, comparator, toleranceValue, rationale, owner, reviewDate }`
- `ComparatorRegistry = { BitwiseEqual, AbsEpsilon, RelEpsilon }` (normative v1)

**Implementation mapping (added v1.1, `ERR-016-009`).** The list above is the **concept**
inventory. It is NOT a type manifest, and six of its nine entries name no type in
`src/deterministic-sim/` — a reader who took it for one would write six phantom types. **`src/` is
the surface authority; this section is the contract those surfaces satisfy.** Every production file
carries a `// Spec:` header naming the section it implements; that header is the per-file authority.

| §2.3 name | Actual `src/deterministic-sim/` surface | Status |
|---|---|---|
| `DeterminismContext` | never aggregated into one type — `matchSeed` rides consumer contexts (e.g. `DecisionContext.MatchSeed`) and the match-save blob, `schemaVersion` / `digestVersion` ride `SnapshotHeader` / `SaveManager` / `SnapshotCodec` / `ReplayEngine`, and **`buildHash` is `SnapshotHeader.BuildHash`**, computed by `BuildIdentity.ComputeHash` over the closure `MatchEngineBuildIdentity` declares (v1.2, `ERR-016-009`) | **SPLIT** |
| `PhaseDigest` | computed, never stored — the preimage is locked by the golden-vector corpus (D-01/D-02); the phase enum is `PhaseId.cs` | COMPUTED |
| `RngStreamKey` | three fields on `RngStreamState` (`SubsystemOrdinal`, `EntityId`, `StreamVersion`) plus the packed `StreamKey`; ordinals in `SubsystemOrdinals.cs` | FIELDS ON `RngStreamState` |
| `RngCursor` | two fields on `RngStreamState` (`RngCursor`, `ActionOrdinal`) | FIELDS ON `RngStreamState` |
| `SnapshotHeader` | `SnapshotHeader.cs` | TYPE |
| `DespawnLog` / `DespawnEntry` | `DespawnLog.cs` / `DespawnEntry.cs` | TYPE |
| `ReplayCursor` | `ReplayCursor.cs` | TYPE |
| `ToleranceRow` (§2.3.1) | none | **DEFERRED — Stage 1+** |
| `ComparatorRegistry` | no registry exists; the three approved comparators exist as `DivergenceDetector.CompareTierAFloat` (BitwiseEqual), `CompareTierBFloat` (AbsEpsilon) and `CompareDigests` | **DEFERRED — Stage 1+** |

**Two normative consequences.**

1. **`buildHash` is normative, and it is now built** (v1.2, August 22, 2026 — `ERR-016-009`'s
   substantive half). *Frozen v1.1 text, for the record: "`buildHash` is an open GAP, not an omission
   of convenience. It is a declared field of the replay-identity context and nothing in `src/` carries
   it or a synonym. Until it exists, two builds differing only in compiled code are indistinguishable
   to everything downstream of this section."* That is no longer true. §2.3.2 below defines what
   `buildHash` **is**; the `SaveManager` `Fingerprint = null` item on the same contract stays open and
   is tracked separately.
2. **The names above are NOT rename targets.** `RngStreamState.RngCursor` and its siblings are
   correct as built and are Tier-A serialized state; renaming a serialized field to match a document
   would move state for no behavioural gain. The spec was the thing that needed to tell the truth.

### 2.3.1 Tolerance row operational schema
| Column | Type | Rule |
|---|---|---|
| `fieldPath` | string | stable dotted path, immutable once published |
| `tier` | enum | `A`, `B`, `C` only |
| `comparator` | enum | from approved comparator registry |
| `toleranceValue` | fixed decimal | required when tier = `B` |
| `rationale` | string | mandatory for tier != `A` |
| `owner` | string | team alias |
| `reviewDate` | date | must be <= 180 days old |

### 2.3.2 `buildHash` — build identity (normative, added v1.2)

`buildHash` identifies **the compiled binaries a run executed on**, as distinct from
`EnvironmentFingerprint` (§4.8), which identifies the **host and float model**. Two builds differing
only in compiled game code MUST produce different `buildHash` values; conflating the two axes — so
that a recompiled engine reads as the same run environment — is the defect `ERR-016-009` was filed
against.

**FR-DS-014.** `buildHash` MUST be

```
SHA-256( DOMAIN_TAG_BUILD_IDENTITY ‖ BUILD_IDENTITY_VERSION ‖ moduleCount
         ‖ ( assemblyName ‖ moduleVersionIdHex )* )
```

encoded as 64 lowercase hex characters, where the module list is the **authoritative assembly
closure**, sorted by ordinal `assemblyName`; `moduleVersionIdHex` is the module's compiler-stamped
Module Version ID in canonical 32-char lowercase hex; and every field is written through the
§3.2.4.1 canonical serializer. Constants: §3.4.

**Four binding rules.**

1. **The closure is DECLARED, never DISCOVERED.** Enumerating loaded assemblies at runtime returns
   whatever happens to have been loaded, which differs between a player run, an editor run and a test
   run of one build. The closure is named by the composition root — the one place that already
   references everything it wires — so a missing module is a compile error, not a silently shorter
   hash. Stage-0 closure: the `match-engine` `.asmdef` reference set plus `match-engine` itself.
2. **`buildHash` is OUTSIDE every digest preimage.** It MUST NOT enter the §3.2.3 snapshot-header
   preimage, the §4.8 `EnvironmentFingerprint` preimage, or any `PhaseDigest`. It is an identity
   compared by equality at restore, not state whose integrity a digest protects — and a per-build
   value inside a digest preimage would make every golden vector unreproducible by construction.
3. **Mismatch fails closed, before any state is touched.** A restore or replay whose recorded
   `buildHash` differs from the live one MUST abort with `ERR_DS_REPLAY_BUILD_MISMATCH`
   (§3.4 / EC-016-015).
4. **Collision-avoidance, not rebuild-stability.** Under a toolchain that compiles
   non-deterministically the MVID moves on every rebuild and prior saves are refused. That is the
   accepted direction: a false refusal is loud, whereas two different builds hashing alike silently
   validates a divergent replay.

**Why not a CI-stamped commit or the `.asmdef` closure alone** (the two candidates weighed at
`ERR-016-009`): a commit identifies *source*, and the builds where determinism defects actually
surface — a dirty tree, a different compiler, a different target framework — differ as binaries while
sharing one commit, or carry no stamp at all; and the `.asmdef` closure names *which* assemblies
participate, not what is in them, so two builds differing only in compiled code have identical
closures. The closure survives here as the **scope selector**; the MVIDs supply the **content**.

**A format that does not carry `buildHash` records its absence, it does not fake one.** The Stage-0
`SaveManager` header (§3.9.2) carries neither the fingerprint nor the build hash and leaves both null
on load; a format that *does* carry it MUST refuse to write or read an empty value rather than admit
a save whose build identity is unknown.

## 2.4 Failure Modes and Recovery
- **Non-canonical ordering detected:** fail fast with deterministic error ID; reject tick commit.
- **RNG policy violation:** fail lint/build gate if direct non-authoritative RNG use detected.
- **Snapshot schema mismatch:** deterministic load failure; no partial rehydration.
- **Digest chain break:** reject replay source; emit integrity error.
- **Hard desync in certification:** block release candidate and trigger bisect workflow.

### 2.4.1 Recovery behavior matrix
| Failure | Runtime behavior | CI behavior |
|---|---|---|
| schema mismatch | abort load, retain pre-load state | fail test job |
| digest chain break | abort replay | fail certification suite |
| Tier A mismatch | halt comparative replay | open blocker issue automatically |
| Tier B drift | continue replay with warning | fail if out-of-bound |

## 2.5 Version History
- **v1.2 (August 22, 2026):** `ERR-016-009`'s substantive half CLOSED — `buildHash` exists. New
  **§2.3.2** defines it normatively (preimage, the declared-not-discovered closure rule, the
  digest-preimage exclusion, the fail-closed mismatch, and the collision-avoidance-not-rebuild-stability
  contract), new **FR-DS-014** binds it, and the §2.3 mapping table's `DeterminismContext` row moves
  **GAP → SPLIT**: `buildHash` is `SnapshotHeader.BuildHash`, computed by `BuildIdentity.ComputeHash`
  over the closure `MatchEngineBuildIdentity` declares. The v1.1 "open GAP" consequence text is kept
  **frozen and quoted in place** rather than deleted, per this project's preserve-the-record
  convention. §3.4 gains `DOMAIN_TAG_BUILD_IDENTITY = 0x2E`, `BUILD_IDENTITY_VERSION = 1` and
  `ERR_DS_REPLAY_BUILD_MISMATCH = 0x160E`; §3.10 gains EC-016-015. **No `DETERMINISM_DIGEST_VERSION`
  bump and no golden vector moved** — rule 2 is what buys that. The `SaveManager` `Fingerprint = null`
  item on the same contract, and the `ToleranceRow` / `ComparatorRegistry` Stage-1+ deferrals, stay
  open unchanged.
- **v1.1 (August 21, 2026):** `ERR-016-009` — §2.3 gains an implementation-mapping table. Six of the nine
  listed structures (`DeterminismContext`, `PhaseDigest`, `RngStreamKey`, `RngCursor`, `ToleranceRow`,
  `ComparatorRegistry`) name no type in `src/deterministic-sim/`, while §4.2 has been explicitly
  non-normative since v0.7 and §4.4 gives module paths (`sim/tick/*` …) that match no directory in the
  tree — leaving §2.3 as the de facto type manifest it was never marked as. Each row now names its real
  surface and status; `src/` is declared the surface authority. Two items are recorded rather than
  fixed: **`buildHash` has no representation anywhere in `src/`** (an open gap on the replay-identity
  contract), and `ToleranceRow`/`ComparatorRegistry` are marked Stage-1+ deferrals rather than left
  reading as built. No rename and no code change — the serialized field names are correct as built.
- **v1.0 (May 4, 2026):** Pass 4 / Pass 5 critique resolution. (a) Pass 4 L-3: FR-DS-009 stage-qualified ("Stage 5+") and pointed at `FR-DS-009-GATE` (§5.5) for operational binding. (b) Pass 5 M-3: `DespawnLog` and `DespawnEntry` added to §2.3 data structures, classified Tier A, canonical sort key declared. (c) Pass 5 M-4: `ReplayCursor { tick, phaseOrdinal }` data structure added with legal-value definition keyed to the §4.2.2 step 7 `EndOfSnapshot[T]` assertion. (d) Pass 4 L-2: §2.6.2 replay-lifecycle example mirrored to the 8-step §4.2.2 normative form with explicit "see §4.2.2 for normative" pointer.
- **v0.8 (May 2, 2026):** Added FR-DS-010..013: EnvironmentFingerprint recording, Tier-B tolerance enforcement, replay 8-step lifecycle, Stage-0 float Tier-A classification gate (B-8).
- **v0.7 (May 2, 2026):** Added §2.0 Identifier Taxonomy; corrected `RngStreamKey` (removed `actionOrdinal` from key) and extended `RngCursor` (added `actionOrdinal`); extended `SnapshotHeader` with `environmentFingerprint`.
- **v0.5:** Added runtime sequence, tolerance schema, and failure recovery matrix.
- **v0.3:** Added explicit FR set tied to refined outline and determinism governance.

## 2.6 End-to-End Data Flow Example
### 2.6.1 Tick lifecycle with checkpoints
1. Tick begins with deterministic context.
2. Authoritative input frame accepted and validated.
3. Phase pipeline executes with ordering constraints.
4. Event ledger commits ordered entries.
5. Digest stream records phase digests.
6. Optional snapshot emits header + payload + digest.

### 2.6.2 Replay lifecycle with checkpoint resume
This subsection is illustrative; the **normative** lifecycle is §4.2.2 (8 steps with per-step error codes). The 8 steps mirrored here for cross-reference:
1. Load snapshot bytes (`ERR_DS_SCHEMA_INCOMPATIBLE` on load failure).
2. Validate `schemaVersion` and `digestVersion` (`ERR_DS_SCHEMA_INCOMPATIBLE`).
3. Validate `EnvironmentFingerprint` against live runtime (`ERR_DS_REPLAY_ENV_MISMATCH`).
4. Validate `prevSnapshotDigest` chain link (`ERR_DS_DIGEST_CHAIN_BREAK`).
5. Rehydrate authoritative state — Tier A + Tier B; includes despawn tombstone log per §3.2.5.3 (`ERR_DS_SCHEMA_INCOMPATIBLE`).
6. Restore RNG cursors and `actionOrdinal` per stream (`ERR_DS_RNG_STREAM_MISSING`).
7. Verify `ReplayCursor` is at `EndOfSnapshot[T]` (`ERR_DS_REPLAY_BOUNDARY`).
8. Reapply authoritative input log from `T+1` (propagates from `RunTick`).

Side-effects on non-authoritative subsystems (UI, audio, VFX, telemetry) MUST NOT be triggered during steps 1–7. See §4.2.2 for the normative form; this section is for cross-reference only.

## 2.7 Ownership and Escalation Matrix
| Domain | Primary owner | Backup owner | Escalation trigger |
|---|---|---|---|
| Tick order | Gameplay systems | Runtime systems | phase order mismatch |
| RNG service | Runtime systems | Gameplay systems | cursor drift |
| Snapshot codec | Runtime systems | Tools team | schema mismatch |
| Certification harness | QA automation | Release engineering | hard desync in CI |
