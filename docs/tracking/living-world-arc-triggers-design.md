# Living World Arc-Triggers / BackgroundTierSim / Season-Save-Fold Design Supplement

> **Status:** DESIGN SUPPLEMENT (pre-implementation) — same governance class as
> `living-world-system-design.md` / `snapshot-deserialize-design.md` / `match-engine-design.md`.
> NOT a numbered spec; #22 (`docs/specs/living-world/`) is the authority and this document only
> plans how three of its documented seams get built.
> **Created:** 2026-07-23
> **Author:** —
> **Governs:** the three Living World #22 items still listed open at the tail of the living-world
> OPEN ISSUES entry — (1) arc **trigger evaluators** + the `world.arcs` RNG sub-stream registration;
> (2) the **BackgroundTierSim** phase-5 summary-update seam; (3) folding the living-world composite
> into the **unified season save**.
> **Scope tier:** *Bounded triggers* — item 1 is a real (opt-in, default-off) build; items 2 and 3
> are, respectively, a documentation-only seam-sharpening and an already-shipped no-op.

---

## 0. Why this document exists

The living-world OPEN ISSUES entry (`CLAUDE.md`) closes with three items tagged "**Still open**":
arc trigger evaluators + `world.arcs`, BackgroundTierSim phase-5, and the unified-season-save fold.
A planning pass over the current tree found the three are in three *different* states, so a single
"implement all three" plan would be wrong:

| # | Item | Actual state (2026-07-23, verified in source) |
|---|------|-----------------------------------------------|
| 3 | Fold living-world composite into the unified season save | **Already shipped** (2026-07-22, `SeasonSaveManager` v-landing / `CLAUDE.md` v2.31). Nothing to build. |
| 1 | Arc trigger evaluators + `world.arcs` sub-stream | Genuinely deferred, but blocked on **missing upstream canon**, not effort. Buildable now only behind an opt-in seam. |
| 2 | BackgroundTierSim phase-5 summary updates | Genuinely deferred; blocked on the **same** upstream canon **and** the spec defines **no update formula**. |

This document records item 3's closure, designs item 1 as an opt-in build, and sharpens item 2's
seam without building it.

### 0.1 The shared blocker (FR-LW-031)

Both items 1 and 2 read canonical **vol-2/vol-3 human-systems** state (pulse divergence, ego clash,
board patience; transfers/sackings/form swings) that does not exist yet. Per **FR-LW-031** (no
interface/accessor produced against an unspecified consumer) neither can be built as its *real* self.
The two are not symmetric, though:

- **Item 1** has a narrow, well-shaped input surface (a handful of `[GT]`-thresholded scalars per
  entity) that maps 1:1 onto the existing `SpawnCause.Input { short Key; float Value }`, and a real
  consumer already exists (`ArcEngine.SpawnArc`, fully live). A minimal deterministic Stage-0 **stub
  canon source** — the established `ScenarioIndex` / `TeamTacticConfig` "author in code now, swap the
  producer later" pattern — gives the evaluator a genuine producer and the `world.arcs` stream a
  genuine draw site, dissolving the phantom concern.
- **Item 2** has **no spec formula** (§3.5 / FR-LW-024 are qualitative) *and* a broad, undesigned
  input surface. Stubbing it would mean inventing the club-AI outcome model — genuinely premature.
  It stays a sharpened seam.

---

## 1. Item 3 — unified-season-save fold (CLOSED, recorded here for completeness)

Verified in source:

- `src/season-save/SeasonSaveManager.cs` — `Save(WorldStore world, MatchEngine matchOrNull, string
  path)` calls `world.Snapshot()` (mandatory) and `MatchSaveManager.Encode(matchOrNull)` (optional),
  frames both via `SeasonSaveCodec.Encode`. `Load(path, ISquadProvider)` calls
  `WorldStore.Restore(blobs.WorldBlob)` and `MatchSaveManager.Restore(blobs.MatchBlob, squads)`.
- `src/season-save/season-save.asmdef` references **both** `TacticalDirector.MatchEngine` and
  `TacticalDirector.LivingWorld` — the only assembly above both, which is what FR-LW-003 requires
  (neither of those two may reference the other).
- The world blob is the **always-present** sub-blob; the match blob is the optional one
  (`matchPresent` flag). The codec treats each as opaque, independently version-gated bytes.

**Action:** none. The `CLAUDE.md` living-world OPEN ISSUES "Still open" list should drop the
"fold … into the unified match/season save" clause; it was superseded by the July-22 season-save
landing and is stale.

---

## 2. Item 1 — Arc trigger evaluators + `world.arcs` (opt-in build)

### 2.1 What exists vs. what this adds

Live today (`src/living-world/ArcEngine.cs`): `SpawnArc` (the complete §3.4 step 1–3 spawn path,
atomic FR-LW-018 pinning), `AdvanceState`, `ResolveArc`, `Update` (the §6.2 expiry sweep, the
WorldLoop phase-4 entry point). Data types `Arc` / `ArcKind` / `SpawnCause` exist. **No `ArcTrigger`
type exists; no `world.arcs` stream is registered; `ArcEngine` holds no RNG reference.**

This adds the evaluator that *reads canon and calls `SpawnArc`*, its trigger catalogue, and the
`world.arcs` stream it draws stochastic components from.

### 2.2 KD-1 — the opt-in mechanism is the canon source itself (not a bool flag)

Following the WorldLoop null-seam precedent (`arcs` / `membership` are nullable injected seams; a
null seam skips its phase), the evaluator reads a **nullable injected `ArcCanonSource`**:

- **`ArcCanonSource == null` ⇒ phase-4 trigger evaluation is skipped ⇒ byte-identical to today.**
  This is the default. No arcs spawn; `world.arcs` draws nothing; its cursor stays 0.
- **A non-null source** (the Stage-0 in-code stub, or a test-injected source) ⇒ triggers evaluate and
  arcs spawn ⇒ a deliberately non-neutral, digest-changing run validated by **comparative
  round-trip**, never absolute byte-identity.

This unifies behaviour-neutrality with the "concrete, not phantom-interface" rule below.

### 2.3 KD-2 — concrete `ArcCanonSource`, not an interface

Ship a concrete `ArcCanonSource` (a sealed class or struct-returning provider) exposing only the
minimal scalar signals triggers threshold on, iterated in **canonical entity-ID order**. Do **not**
introduce an `IArcCanonSource` interface — the project injects concrete nullable seams (WorldLoop's
`arcs`/`membership`; `world.text` shipped as a concrete `InteractionTextGenerator`), and an interface
with one implementation is the phantom shape FR-LW-031 warns against. Promote to an interface only
when a second (real vol-2/vol-3) implementation actually exists. The Stage-0 stub is the drop-in
target for that producer (the `ScenarioIndex` / `TeamTacticConfig` parser-swap precedent).

### 2.4 KD-3 — `ArcTrigger` catalogue + evaluator

New `ArcTrigger` value type: `TriggerId` (→ the `SpawnCause.TriggerId` recorded at spawn) + target
`ArcKind` + a `[GT]` threshold + the input-capture rule that populates `SpawnCause.Input[]`. The
evaluator walks canon in the spec's **fixed deterministic order** — canonical entity-ID order for
entity-scoped triggers, `ArcKind` ordinal order for board/squad-level triggers (FR-LW-017 / FR-LW-021)
— fires `SpawnArc` on threshold crossings, and records `SpawnCause` inline (it cannot be
reconstructed later, per the `SpawnCause` contract). Per-kind `AdvanceState` state machines + the
per-kind lifetime catalogue (`AdvanceState` does no validation today) land here.

### 2.5 KD-4 — `world.arcs` stream needs a DISTINCT key (verified hazard)

`DeterministicRngService.ComputeStreamKey` (verified, `DeterministicRngService.cs:268`) hashes
`SipHash-2-4-64((k0,k1), subsystemOrdinal ‖ entityId ‖ streamVersion)` and **excludes `siteId`**;
`DrawReserved` derives values from `StreamKey ‖ ActionOrdinal ‖ index` (`:116`). `world.text` already
occupies `(subsystemOrdinal = LivingWorld = 80, entityId = -1, streamVersion = 1)`. Registering
`world.arcs` at the same triple would compute an **identical `StreamKey`** and draw an identical value
sequence (arcs draw #k == text draw #k for equal `ActionOrdinal`) — a latent determinism-model
defect. (Cursors stay independent because each stream is its own registry slot, so the "don't perturb
the arc cursor" goal survives regardless — but the value-space correlation must not ship.)

**Fix — catalogue a negative-sentinel block for world-scoped LivingWorld streams** in
`LivingWorldConstants` so the discriminators are recorded, not ad hoc:

```
// World-scoped LivingWorld RNG sub-stream entity-id sentinels (real entity ids are >= 0, so
// these never collide; each yields a DISTINCT ComputeStreamKey within subsystemOrdinal 80).
WORLD_STREAM_ENTITY_TEXT       = -1   // world.text   (existing)
WORLD_STREAM_ENTITY_ARCS       = -2   // world.arcs   (this item)
WORLD_STREAM_ENTITY_BACKGROUND = -3   // world.background (reserved for item 2, §3)
```

`world.arcs` registers with `RegisterStream("world.arcs", LivingWorld, WORLD_STREAM_ENTITY_ARCS,
WORLD_ARCS_STREAM_VERSION)` plus new `[FIXED]` `WORLD_ARCS_STREAM_SITE_ID` / `_VERSION` constants.
Register **unconditionally at boot**, in a fixed code position **after** `world.text`, so the stream
index is positionally stable across save/restore whether or not a canon source is injected (restore
re-registers positionally). A unit test asserts `key(world.arcs) != key(world.text)`.

### 2.6 KD-5 — WorldLoop wiring

The evaluator slots into the **existing phase-4 site** (§3.4 arc evaluation), beside the live
`_arcs.Update(...)` §6.2 expiry sweep — NOT phase 1 (match-outcome ingest) and NOT phase 2 (the
human-systems read the triggers *consume*, still a null seam). Null-guard on the `ArcCanonSource`
seam exactly as phases 4/6 guard `_arcs` / `_membership`.

### 2.7 KD-6 — serialization, two-phase (mirrors GK/Heading Phase-1/Phase-2)

- **Phase E1 (no schema bump).** Stream registered + evaluator wired, but the `world.arcs` cursor is
  **not** serialized and `WORLD_STORE_FORMAT_VERSION` stays 2. A null-source run draws nothing
  (cursor 0) ⇒ **byte-identical to today; the existing living-world determinism suite is unchanged**.
  A non-null-source (flag-on) run is deterministic *forward* but not yet snapshot-safe, so
  `WorldStore.Snapshot()` **fails loud** when the `world.arcs` cursor is non-zero (the
  `EnableGkHeading` Phase-1 durable-capture-fails-loud precedent).
- **Phase E2 (bump 2 → 3).** Serialize `world.arcs` `RngCursor` + `ActionOrdinal` into the composite
  in fixed order **after** the `world.text` RNG block; flag-on becomes snapshot-safe. **v2 payloads
  are rejected fail-loud at the `WORLD_STORE_FORMAT_VERSION` gate — no in-place migration at Stage 0.**
  Because the season save frames the world blob opaquely (item 3), this bump rides *inside* the world
  sub-blob with **no `SEASON_SAVE_FORMAT_VERSION` change**.

  **E2 acceptance predicate (named so the eventual code is checkable):** *save@N of a flag-on run →
  `WorldStore.Restore` → advance to N+K, and the snapshot digest chain is byte-identical to an
  uninterrupted flag-on run advanced N→N+K.* This is the GK/Heading Phase-2 lock, not an
  absolute-golden rebaseline.

### 2.8 Not in scope for item 1

Routing arc *resolution effects* into canon (KD-9/KD-10); WorldLoop phases 1/2/5 producers; the
real vol-2/vol-3 canon source (the stub's drop-in successor).

---

## 3. Item 2 — BackgroundTierSim phase-5 (sharpened seam, NOT built)

§3.5 / FR-LW-024 specify only a *qualitative* "cheap, deterministic update of summary state only …
under the same RNG-service determinism rules (a periodic, tick-driven sub-stream) and a bounded
per-tick cost," reflecting club-AI / transfer / sacking / form-swing outcomes. **There is no
formula, and no producer of those outcomes exists.** Stubbing it means inventing the outcome model.
Do not build it. Do the following instead:

1. **Author the phase-5 activation contract** (in this document + the WorldLoop phase-5 comment):
   `BackgroundTierSim` as a **fourth null-injectable WorldLoop seam**; the **periodic tick-driven
   `world.background` RNG sub-stream** it will require (entity sentinel `WORLD_STREAM_ENTITY_BACKGROUND
   = -3` reserved in §2.5 so it gets a distinct key; **its cursor is a future
   `WORLD_STORE_FORMAT_VERSION` bump when built**); the FR-LW-024 bounded-per-tick-cost budget; and
   the exact upstream producers that must land first (abstracted club-AI, vol-3 §2 transfers, vol-3 §4
   governance, structured match-outcome events).
2. **Pin the summary-update target.** The machinery phase-5 will drive already exists: `ColdSummary`
   (`NetRelationship`, `RetainedEpisodes`, `NextEpisodeId`) + `ColdStore.Compress` / `Rehydrate`.
   Today a `ColdSummary` is written only at demotion and read only at promotion
   (`ActiveSetMembership`); phase-5 adds the *in-place update while a contact sits cold*.
   **Two distinct serialization impacts, not one:** updating the existing `ColdSummary` **value
   fields** is a value-only mutation of already-serialized cold-store state (**no schema change for
   those fields**); the **new `world.background` stream cursor** is a separate serialized addition
   that **does** require a `WORLD_STORE_FORMAT_VERSION` bump when phase-5 is built.
3. **Leave the seam null-guarded and unbuilt**, now with a written activation checklist (this §3)
   instead of the one-line phase-5 comment.

---

## 4. Determinism, save, and layering summary

- Item 1's save impact is localized to the **world sub-blob** (`WORLD_STORE_FORMAT_VERSION`, E2 only);
  the season frame (`SEASON_SAVE_FORMAT_VERSION`) is untouched — a direct benefit of item 3 being done.
- Every world-scoped LivingWorld RNG stream gets a **distinct `ComputeStreamKey`** via the cataloged
  negative-entity-sentinel block (§2.5), locked by a key-inequality test.
- FR-LW-003 layering is unaffected: all item-1 code lives inside `TacticalDirector.LivingWorld`; the
  season-save composition root already sits above it.

## 5. Test plan (item 1)

- Two-run field-identity determinism (null source ⇒ existing suite unchanged; stub source ⇒ two runs
  byte-identical to each other).
- Flag-off (null source) byte-identity vs. pre-change engine at `WORLD_STORE_FORMAT_VERSION` 2.
- E1 fail-loud: `WorldStore.Snapshot()` throws when the `world.arcs` cursor is non-zero pre-E2.
- E2 acceptance predicate (§2.7) + fail-loud version/tag/trailing-byte gates.
- `key(world.arcs) != key(world.text)` (and, when item 2 lands, `!= key(world.background)`).
- Trigger-order determinism: entity-ID order / `ArcKind` ordinal order (FR-LW-017/021).

## 6. Open decision (for the author, not resolved here)

Path for the two live items: **B for item 1 (build now behind the null `ArcCanonSource` seam) + A for
item 2 (sharpen the seam)** — the recommendation — or **A for both** (documentation only, build
nothing until real canon lands) or **B for both** (also stub BackgroundTierSim's outcome producers —
larger, speculative). This document is written for the recommended split; §3 is the item-2 half of
Path A regardless.

## 7. Adversarial-review status

Planning-stage AR run over the plan text this document formalizes: pass 1 found 1 High
(Path B item 1 not behaviour-neutral — resolved by the §2.2 null-seam opt-in + §2.7 two-phase
serialization) + 1 Medium (the §2.5 `world.arcs` stream-key collision, verified in source) + 3 Low;
pass 2 found 0 High / 0 Medium / 2 Low (the §2.5 sentinel-block catalogue + the §2.7 named E2
predicate, both folded in here) — **CONVERGENCE**. Section-file/code AR cycles run at implementation
per the project convention.

---

## 8. Detailed implementation plan — item 1 (Path B)

Everything below is verified against the current tree (`ArcEngine.cs`, `WorldLoop.cs`,
`WorldStore.cs`, `InteractionTextGenerator.cs`, `WorldStateSerializer.cs`,
`DeterministicRngService.cs`, `LivingWorldConstants.cs`) so the signatures and byte layouts are the
real ones, not sketches.

### 8.1 File inventory

**New files** (all in `src/living-world/`):

| File | Type | Purpose |
|------|------|---------|
| `ArcCanonSource.cs` | `public abstract class` (Stage-0 stub subclass alongside) | The §2.2/§2.3 nullable canon-input seam the evaluator reads. Concrete, not an interface. |
| `Stage0ArcCanonSource.cs` | `public sealed class : ArcCanonSource` | Deterministic in-code Stage-0 producer (the `ScenarioIndex`/`TeamTacticConfig` parser-swap precedent). |
| `ArcTrigger.cs` | `public readonly struct` | One catalogue row: `TriggerId` + target `ArcKind` + `[GT]` threshold + input-capture rule. |
| `ArcTriggerCatalogue.cs` | `public static class` | The Stage-0 in-code trigger table (APPEND-only, like `InteractionTextCorpus`). |
| `ArcTriggerEvaluator.cs` | `public sealed class` | Registers `world.arcs`, walks canon in FR-LW-017/021 order, draws the stochastic component, calls `ArcEngine.SpawnArc`. Owns the stream (the `InteractionTextGenerator` role). |
| `Tests/ArcTriggerTests.cs` | test fixture | §9 case list. |

**Modified files:**

| File | Change |
|------|--------|
| `LivingWorldConstants.cs` | +Fixed region rows (§8.2). |
| `WorldStore.cs` | Construct the evaluator at boot; wire it into the loop; E1/E2 serialization; `Restore` canon-source param. |
| `WorldLoop.cs` | Phase-4 evaluator call, null-guarded (a fifth nullable seam arg). |
| `LivingWorldConstants.cs` version history + `WorldStore.cs`/`WorldLoop.cs` version history | Append rows per FR-CS-056. |

**Deliberately NOT modified:** `ArcEngine.cs` (its `SpawnArc`/`AdvanceState`/`ResolveArc`/`Update`
surface is already the exact seam the evaluator calls — the class doc even names this landing);
`WorldStateSerializer.cs` (the `world.arcs` cursor is a `WorldStore`-composite field, not a
four-store field — so `WORLD_SNAPSHOT_FORMAT_VERSION` stays 1).

### 8.2 Constants — `LivingWorldConstants.cs`, `#region Fixed`

```csharp
/// <summary>[FIXED] Stable siteId of the periodic tick-driven world.arcs RNG sub-stream
/// (#22 §3.4 / FR-LW-020). Registered by ArcTriggerEvaluator. #16 §3.2.5.1: never change it.</summary>
public const string WORLD_ARCS_STREAM_SITE_ID = "world.arcs";

/// <summary>[FIXED] world.arcs stream version (#16 §3.2.5.1). Bump only on a draw-site
/// reordering; a bump invalidates arc replay parity by design.</summary>
public const ushort WORLD_ARCS_STREAM_VERSION = 1;

// World-scoped LivingWorld RNG sub-stream entity-id sentinels. ComputeStreamKey hashes
// (subsystemOrdinal ‖ entityId ‖ streamVersion) and EXCLUDES siteId, so two world-scoped streams
// in subsystemOrdinal 80 MUST differ here to get distinct keys. Real entity ids are >= 0.
public const int WORLD_STREAM_ENTITY_TEXT       = -1; // world.text (existing — replaces the bare -1)
public const int WORLD_STREAM_ENTITY_ARCS       = -2; // world.arcs (this item)
public const int WORLD_STREAM_ENTITY_BACKGROUND = -3; // world.background (reserved for item 2, §3)
```

`WORLD_STORE_FORMAT_VERSION` stays `2` at E1 and becomes `3` only at E2 (§8.6). `InteractionTextGenerator`'s
`entityId: -1` (line 60) is updated to `WORLD_STREAM_ENTITY_TEXT` (behaviour-identical — same value —
but cataloged). Per-kind `[GT]` trigger thresholds + `ARC_TRIGGER_*` lifetime rows go in `#region GT`.

### 8.3 `ArcCanonSource` (KD-2 — concrete, nullable seam)

```csharp
public abstract class ArcCanonSource
{
    // Canon read in FR-LW-017/021 order. Entity-scoped signals iterated by ascending entity id;
    // board/squad-level signals keyed by ArcKind ordinal. Scalars only — they map 1:1 onto
    // SpawnCause.Input { short Key; float Value }. Every value MUST be finite (WriteF32 is Tier-A
    // no-NaN); the evaluator gates before capture.
    public abstract int EntitySignalCount { get; }
    public abstract int EntityIdAt(int index);                 // ascending
    public abstract float EntitySignal(int index, short key);  // e.g. pulse divergence, ego clash
    public abstract float BoardSignal(ArcKind kind, short key);// board patience etc.
}
```

`Stage0ArcCanonSource` is a deterministic pure function of the world state it is handed (no
`System.Random`, no clock) — e.g. derived from `MemoryStore` edge salience/relationship values, so
it is reproducible across `Snapshot`/`Restore`. It is the drop-in slot for the real vol-2/vol-3
producer; **null is the default and means "skip phase-4 trigger evaluation"** (§2.2).

### 8.4 `ArcTrigger` + evaluator core

```csharp
public readonly struct ArcTrigger
{
    public readonly ushort TriggerId;     // -> SpawnCause.TriggerId
    public readonly ArcKind Kind;
    public readonly bool IsEntityScoped;  // entity-scoped vs board/squad-level (FR-LW-017)
    public readonly short SignalKey;      // which canon scalar this trigger thresholds
    public readonly float Threshold;      // [GT]
    public readonly uint MaxLifetimeDays; // [GT], in [1, ARC_MAX_LIFETIME_DAYS]
}
```

Evaluation order (fixed, deterministic — the load-bearing determinism property):

1. **Entity-scoped triggers**, outer loop over `ArcCanonSource.EntityIdAt(i)` ascending, inner loop
   over the entity-scoped catalogue rows in catalogue (ordinal) order.
2. **Board/squad-level triggers**, iterated by `ArcKind` ordinal.

On a crossing (`signal >= Threshold`, NaN fails closed via a negated compare — the store-seam
precedent), draw **one** `world.arcs` value for the stochastic accept/shape component, build the
`SpawnCause` inline (`TriggerId`, the captured `Input[]` scalars, `Cause.WorldTick =
_clock.CurrentWorldTick`), resolve the pinned source episodes from `MemoryStore`, and call
`ArcEngine.SpawnArc(kind, in cause, pins, spawnTick, maxLifetimeDays)`. `SpawnArc` already does the
atomic FR-LW-018 pinning + rollback, the `ArcKind` gate, and the lifetime/overflow gates — the
evaluator adds no new validation there.

### 8.5 `world.arcs` RNG registration + draw discipline (KD-4)

Registration mirrors `InteractionTextGenerator` exactly, with the **distinct entity sentinel**:

```csharp
public ArcTriggerEvaluator(DeterministicRngService rng)
{
    _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    _streamIndex = rng.RegisterStream(
        LivingWorldConstants.WORLD_ARCS_STREAM_SITE_ID,
        SubsystemOrdinals.LivingWorld,               // 80
        LivingWorldConstants.WORLD_STREAM_ENTITY_ARCS, // -2  (NOT -1 — else key == world.text)
        LivingWorldConstants.WORLD_ARCS_STREAM_VERSION);
}
public int StreamIndex => _streamIndex;
```

Per-crossing draw (the `InteractionTextGenerator.Generate` discipline — all validation/canon reads
run BEFORE the draw so a no-crossing tick consumes no cursor):

```csharp
if (_rng.Reserve(_streamIndex, 1) != 0) throw new InvalidOperationException(...);
if (_rng.DrawReserved(_streamIndex, 0, out ulong draw) != 0) { _rng.CloseReservation(_streamIndex); throw ...; }
_rng.CloseReservation(_streamIndex);   // advances RngCursor by DeclaredBudget (verified line 124-127)
```

**One draw per crossing, not per tick** — a tick with no crossings leaves the cursor untouched, so a
flag-off (null canon source) run never advances it (E1 byte-identity). Registration is
**unconditional at boot** in a fixed position (after `world.text`) so the stream index is
positionally stable across save/restore whether or not a canon source is present.

### 8.6 `WorldStore` diffs

**Fields + ctor** (both public ctors flow through `WorldStore(int, ulong)`):

```csharp
private ArcTriggerEvaluator _arcTriggers;   // owns world.arcs; constructed unconditionally
private ArcCanonSource _canon;              // nullable; null => no evaluation (default)
// in WorldStore(int managerId, ulong worldSeed):
_arcTriggers = new ArcTriggerEvaluator(_rng);   // registers world.arcs AFTER _text (index order fixed)
_canon = null;
_loop = new WorldLoop(_clock, _memory, _arcs, _membership, _arcTriggers /* + _canon via a setter or field */);
```

Add `public void SetArcCanon(ArcCanonSource canon)` (the opt-in). A new
`WorldStore(int managerId, ulong worldSeed, ArcCanonSource canon)` overload is the ergonomic form.

**Snapshot()** — E2 inserts the `world.arcs` block between the `world.text` block and the membership
roster (keeping membership last so no existing byte-offset test moves). Current writer order
(`WorldStore.cs:268-288`): header → store block → world.text (seed/cursor/ordinal) → membership. E2:

```csharp
// ... after the world.text WriteU64 x3 ...
RngStreamState arcStream = _rng.GetStreamState(_arcTriggers.StreamIndex);
CanonicalSerializer.WriteU64(payload, ref offset, arcStream.RngCursor);
CanonicalSerializer.WriteU64(payload, ref offset, arcStream.ActionOrdinal);
// ... then membership roster (unchanged) ...
```

`ComputeSize` gains `+ 8 + 8`. Bump `WORLD_STORE_FORMAT_VERSION` 2 → 3 with the v2→v3 doc note.

**Restore(byte[] payload, ArcCanonSource canon = null)** — the canon source is a **Load-time
parameter, never persisted** (the season-save `ISquadProvider` precedent). After re-deriving `_rng`
and reconstructing `_text`, reconstruct `_arcTriggers = new ArcTriggerEvaluator(rng)` (re-registers
`world.arcs` at the same positional index), then read the cursor/ordinal and `RestoreStream`
(fail-loud on a non-zero code, the `world.text` slice-5 AR-1 L-1 precedent):

```csharp
RngStreamState arcStream = rng.GetStreamState(arcTriggers.StreamIndex);
arcStream.RngCursor = CanonicalSerializer.ReadU64(payload, ref offset);
arcStream.ActionOrdinal = CanonicalSerializer.ReadU64(payload, ref offset);
if (rng.RestoreStream(arcTriggers.StreamIndex, in arcStream) != 0) throw new InvalidOperationException(...);
```

**E1 (no schema bump)** ships everything above EXCEPT the two `WriteU64`/`ReadU64` pairs and the
version bump: the stream is registered and the evaluator wired, but `Snapshot()` **fails loud** when
`arcStream.RngCursor != 0` (a flag-on save is not yet snapshot-safe — the `EnableGkHeading` Phase-1
`NotSupportedException` durable-capture precedent). A null-canon run keeps the cursor at 0, so
`Snapshot()` succeeds and is byte-identical to today at `WORLD_STORE_FORMAT_VERSION` 2.

### 8.7 `WorldLoop` diff (phase-4)

`WorldLoop` gains a fifth nullable seam arg (`ArcTriggerEvaluator arcTriggers` + the canon source, or
an `IArcPhase`-free plain call). Phase 4 currently runs only `_arcs.Update(...)` (`WorldLoop.cs:79-82`).
The evaluator call slots in **before** the expiry sweep so a same-tick-spawned arc is subject to this
tick's expiry bound only if already expired (matches `SpawnArc`'s own `spawnTick`):

```csharp
// Phase 4 — arc evaluation (§3.4).
if (_arcs != null)
{
    if (_arcTriggers != null && _canon != null)
        _arcTriggers.Evaluate(_canon, _memory, _arcs, _clock.CurrentWorldTick);  // trigger evaluation
    _arcs.Update(_clock.CurrentWorldTick);                                       // §6.2 expiry sweep
}
```

Null canon ⇒ the evaluate call is skipped ⇒ byte-identical to today. The 2-arg / 4-arg `WorldLoop`
ctors stay (slice-1 hosts unchanged); add a 5-arg ctor.

### 8.8 Fail-loud gates (item 1)

- `WORLD_STORE_FORMAT_VERSION` mismatch on `Restore` → `ArgumentException` (existing gate; v2 payloads
  are rejected fail-loud at E2, **no in-place migration** at Stage 0).
- `Snapshot()` at E1 with a non-zero `world.arcs` cursor → `NotSupportedException` (flag-on not yet
  snapshot-safe).
- `RestoreStream` non-zero return → `InvalidOperationException` (registration-order drift).
- Non-finite `SpawnCause.Input.Value` → already gated in `WorldStateSerializer.Serialize`
  (`:102-111`); the evaluator additionally gates at capture time.
- `ArcCanonSource` returning a non-finite signal → the evaluator's negated-compare threshold fails
  closed (no crossing, no draw).

## 9. Test plan — `Tests/ArcTriggerTests.cs` (item 1)

Mirrors the `ArcMembershipTests` / `WorldStoreTests` style (field-identity round-trips, two-run
determinism, fail-loud gates):

1. **`key(world.arcs) != key(world.text)`** — register both on one `DeterministicRngService`, assert
   distinct `StreamKey` (the §2.5/KD-4 lock; fails if `world.arcs` reuses `entityId: -1`).
2. **Null-canon byte-identity (E1)** — a `WorldStore` with no canon source produces a `Snapshot()`
   byte-identical to a pre-change store at `WORLD_STORE_FORMAT_VERSION` 2; the existing
   `WorldStoreTests` determinism suite is unchanged.
3. **Flag-off no-op through the loop** — `AdvanceDay()` with null canon spawns no arcs and leaves the
   `world.arcs` cursor at 0.
4. **Stub-canon spawns deterministically** — inject a `Stage0ArcCanonSource` primed to cross one
   threshold; assert `ArcEngine.ArcCount` increments, the `SpawnCause.TriggerId`/`Input[]` match, and
   two same-seed runs produce byte-identical world state.
5. **E1 fail-loud** — a flag-on store (canon set, a crossing driven) refuses `Snapshot()` with
   `NotSupportedException` while the cursor is non-zero.
6. **E2 acceptance predicate** — save@N of a flag-on run → `WorldStore.Restore(payload, canon)` →
   `AdvanceDay` to N+K; the resulting `Snapshot()` (and every intermediate arc/cursor) is
   byte-identical to an uninterrupted flag-on run advanced N→N+K. This is the §2.7 named lock.
7. **Trigger-order determinism** — a canon source exposing two entities + one board trigger that all
   cross on the same tick spawn in the pinned (entity-id ascending, then `ArcKind` ordinal) order.
8. **Restore fail-loud gates** — v2-format payload rejected at E2; truncated arcs block rejected;
   `RestoreStream` drift rejected.

## 10. Sequencing (two landing slices, each its own AR + gate)

- **Slice 1 — E1 (no schema bump).** New files §8.1, constants §8.2, evaluator + registration
  §8.4/§8.5, `WorldStore`/`WorldLoop` wiring §8.6/§8.7 minus the two serialize pairs, tests 1–5 + 7.
  Ship byte-identical flag-off; flag-on deterministic-forward but `Snapshot()` fails loud. This is the
  reviewable, byte-neutral landing.
- **Slice 2 — E2 (`WORLD_STORE_FORMAT_VERSION` 2 → 3).** Add the cursor serialize/restore §8.6, drop
  the E1 fail-loud, add tests 6 + 8. Comparative round-trip, no absolute rebaseline. The season save
  frames the world blob opaquely, so `SEASON_SAVE_FORMAT_VERSION` is untouched (re-verify with one
  season-save round-trip test through a flag-on `WorldStore`).

Each slice runs its own adversarial-review cycle to convergence (the project convention) before the
full dotnet gate.

## 11. Item 2 — BackgroundTierSim phase-5 activation checklist (NOT built)

Recorded so the seam is precise (replacing the one-line `WorldLoop.cs:84-85` comment). When the
upstream producers land, build in this order:

1. **Producer gate.** Requires: abstracted club-AI outcomes, vol-3 §2 transfers, vol-3 §4 governance
   (sackings), structured match-outcome events. Until all exist, do not author the interface
   (FR-LW-031).
2. **`BackgroundTierSim` as the fifth nullable WorldLoop seam** (mirrors `arcs`/`membership`/the new
   `arcTriggers`): null ⇒ phase 5 skipped ⇒ byte-identical.
3. **`world.background` RNG stream** — its own distinct key via `WORLD_STREAM_ENTITY_BACKGROUND = -3`
   (§8.2, already reserved). Its cursor is a **future `WORLD_STORE_FORMAT_VERSION` bump** (a third
   world-scoped cursor block, same shape as §8.6).
4. **Summary-update target.** Phase-5 mutates `ColdSummary` (`NetRelationship`, `RetainedEpisodes`,
   `NextEpisodeId`) in place while a contact sits cold — the value fields are already serialized
   (`WorldStateSerializer.cs:132-135`), so updating them is **no schema change for those fields**; only
   the new stream cursor bumps the version. Bounded per-tick cost per FR-LW-024.
5. **Do not stub the producers.** Stubbing would invent the club-AI outcome model (premature). Item 2
   stays a sharpened seam this document owns.

## 12. Acceptance criteria

- Full dotnet gate PASSED, 0 failures, whole tree green (SDK via apt, the current local-gate posture).
- Slice 1: the existing living-world determinism suite is **unchanged** (byte-identical flag-off);
  `key(world.arcs) != key(world.text)` locked.
- Slice 2: the §2.7 comparative round-trip predicate passes; a flag-on `WorldStore` round-trips
  through the unified season save with no `SEASON_SAVE_FORMAT_VERSION` change.
- No `ArcEngine.cs` or `WorldStateSerializer.cs` production change (the arc cursor is a `WorldStore`
  composite field).

---

## Version History

- **v0.2 (2026-07-23):** Expanded into a detailed implementation plan (§§8–12) — verified against the
  live tree (`ArcEngine`/`WorldLoop`/`WorldStore`/`InteractionTextGenerator`/`WorldStateSerializer`/
  `DeterministicRngService`): file inventory + exact constant declarations, the concrete
  `ArcCanonSource`/`ArcTrigger`/`ArcTriggerEvaluator` signatures, the `world.arcs` registration +
  per-crossing draw discipline, the `WorldStore`/`WorldLoop` diffs with the E1(no-bump)/E2(2→3)
  serialization byte layout, the fail-loud gate set, an 8-case test list, a two-slice landing
  sequence, the item-2 phase-5 activation checklist, and acceptance criteria. Confirmed the
  `world.arcs` cursor is a `WorldStore`-composite field (`WORLD_STORE_FORMAT_VERSION`), so
  `WorldStateSerializer` / `WORLD_SNAPSHOT_FORMAT_VERSION` are untouched, and that `Restore` threads
  the canon source as a Load-time parameter (the season-save `ISquadProvider` precedent).
- **v0.1 (2026-07-23):** Created. Records item 3 (season-save fold) as already shipped; designs item 1
  (arc trigger evaluators + `world.arcs`) as an opt-in, default-off build behind a nullable
  `ArcCanonSource` seam with a distinct-key `world.arcs` stream and two-phase serialization; sharpens
  item 2 (BackgroundTierSim phase-5) as a documented seam without building it. Incorporates the
  planning-stage AR pass-2 Lows (cataloged world-scoped RNG entity-sentinel block; named E2 acceptance
  predicate).
