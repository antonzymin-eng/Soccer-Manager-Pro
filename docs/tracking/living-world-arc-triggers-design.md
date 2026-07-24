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
— fires `SpawnArc` on the **edge-triggered, latched** threshold crossing (KD-7, §2.8 — a trigger fires
once when its signal first crosses `Threshold` and re-arms only when it drops back below, so a
sustained signal spawns exactly one arc, not one per day), and records `SpawnCause` inline (it cannot
be reconstructed later, per the `SpawnCause` contract). Per-kind `AdvanceState` state machines + the
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
  `WorldStore.Snapshot()` **fails loud** when the `world.arcs` cursor is non-zero (`RngCursor` or
  `ActionOrdinal` — both checked; the `EnableGkHeading` Phase-1 durable-capture-fails-loud precedent).
- **Phase E2 (bump 2 → 3).** Serialize `world.arcs` `RngCursor` + `ActionOrdinal` into the composite
  in fixed order **after** the `world.text` RNG block; flag-on becomes snapshot-safe. **v2 payloads
  are rejected fail-loud at the `WORLD_STORE_FORMAT_VERSION` gate — no in-place migration at Stage 0.**
  Because the season save frames the world blob opaquely (item 3), this bump rides *inside* the world
  sub-blob with **no `SEASON_SAVE_FORMAT_VERSION` change**.

  **E2 acceptance predicate (named so the eventual code is checkable):** *save@N of a flag-on run →
  `WorldStore.Restore` → advance to N+K, and the snapshot digest chain is byte-identical to an
  uninterrupted flag-on run advanced N→N+K.* This is the GK/Heading Phase-2 lock, not an
  absolute-golden rebaseline.

### 2.8 KD-7 — firing semantics: edge-triggered with a per-(entity,trigger) latch

A trigger's threshold test defines *when it may fire*, but not *how often*. The two readings of a bare
`signal >= Threshold` level test are both wrong at Stage 0:

- **Pure level** (fire on every tick the signal sits `>= Threshold`) floods `ArcEngine` — `SpawnArc`
  has no dedupe, and a persistently-high signal (a long-running feud, a season-long form slump) would
  spawn one arc **per day** for the whole duration. That is not what an "arc" is (a bounded narrative
  episode, §3.4), and it makes the `world.arcs` cursor advance unboundedly.
- **Pure stateless edge** ("crossing") is undefined without remembering the previous tick.

**Decision — edge-triggered with a latch.** A trigger fires on the tick its signal **first crosses**
from below `Threshold` to `>= Threshold`, then stays **armed-off** (latched) and does **not** fire
again until the signal drops back **below** `Threshold`, which **re-arms** it. Concretely the
evaluator holds a per-scope-key latch:

- **Scope key** = `entityId` for entity-scoped triggers, the `ARC_BOARD_SCOPE_KEY` sentinel (§8.2,
  `int.MinValue` — never a real entity id) for board/squad-level triggers (so a board trigger latches
  once globally, disambiguated from its siblings by `TriggerId`, not per entity).
- **Latch state** = the set of currently **armed-off** `(scopeKey, TriggerId)` pairs. A pair absent
  from the set is **armed**.
- **Per evaluated tick, per (scopeKey, trigger):** if armed and `signal >= Threshold` → **fire**
  (draw + `SpawnArc`) and **add** the pair to the armed-off set; if armed-off and `signal < Threshold`
  → **remove** the pair (re-arm); otherwise no-op, **no draw**.

**This latch is cross-tick engine state and MUST be serialized (E2).** It is exactly the GK/Heading
Phase-2 latch precedent this document already cites (`_saveCommittedForGk` /
`_headerCommittedThisEpisode` — engine-level state that *gates trigger re-commits*, whose omission
from a snapshot re-fires an already-fired trigger on restore and diverges). Omitting the latch from
the E2 block would make a flag-on `save@N → restore → advance` re-fire every still-above-threshold
trigger the instant of restore — the precise completeness-bug class §2.7's acceptance predicate
exists to catch. §8.4 pins the loop; §8.6 serializes the set; §9 test 10 locks single-fire + re-arm
and test 11 is the re-fire-after-restore lock.

**Departed-entity latch entries** (an armed-off entity that vanishes from canon) are harmless: they
sit inert until the entity returns and drops below threshold (re-arm) — a bounded, deterministic set
under the Stage-0 stub's stable entity roster; a future canon-pruning pass may prune them, but nothing
requires it for correctness.

### 2.9 Not in scope for item 1

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
- Edge-trigger + latch (KD-7): a sustained above-threshold signal fires **once** (not per day);
  dropping below re-arms; a flag-on `save@N → restore → advance` does **not** re-fire a still-latched
  trigger (the latch is serialized at E2 — the re-fire completeness lock).

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
predicate, both folded into v0.1). A third pass over the expanded implementation plan (§§8–12) found
0 High / 3 Medium / 2 Low, all applied in v0.3: M-1 the §8.3 `ArcCanonSource` concrete-class fix (an
abstract base + one subclass reintroduced the one-implementation abstraction §2.3/KD-2 rejects); M-2
the §8.9 season-save flag-on canon-threading gap (a flag-on season restore silently stopped
evaluating — `SeasonSaveManager.Load` did not thread the canon source); M-3 the §8.4/§8.5
draw-after-decide reorder + missing-episode skip-no-draw contract (the plan drew the `world.arcs`
cursor before pin resolution, violating the validate-before-draw discipline it cites); L-1 the §8.6/§8.8
E1 fail-loud guard checks `ActionOrdinal` too; L-2 the §8.5 `MaxRngStreams`-headroom note. A fourth
pass caught two regressions the v0.3 fixes themselves introduced (0H+2M+1L), applied in v0.4: M-1 the
M-3 fix's pin-resolution gate was under-specified AND suppressed legitimate pin-less arcs (a crossing
on an edge with no citable episode would never spawn) — §8.4/§8.5 rewritten so the threshold is the
SOLE pre-draw refusal and an empty pin set is a valid pin-less spawn, not a skip; M-2 the canon
plumbing (`SetArcCanon` / `WorldLoop` / `Restore`) was a `/* setter or field */` hand-wave that could
ship the opt-in as a dead setter — §8.6/§8.7 pin the per-tick-argument model (`WorldStore` owns
`_canon`, `RunWorldTick(canon)` reads it live), with a §9 test-4 dead-setter guard; L the §8.3
signature sketch de-abstract-ified. **A sixth pass (a fresh hostile re-read of the core evaluator
loop, the full-re-review rule) found 1 High the prior five passes missed (1H+0M+0L), applied in
v0.5:** **H-1 — trigger firing semantics were unspecified and self-contradictory.** §8.4 described the
threshold as "the SOLE pre-draw refusal" using "crossing" (edge) language over a **stateless level
test** (`signal >= Threshold`, no previous-tick state). Under the literal level reading a
persistently-high signal spawns **one arc per day** for the signal's whole duration (`ArcEngine.SpawnArc`
has no dedupe) — an unbounded `world.arcs` cursor and a flood of arcs; under the intended edge reading
the loop needs per-(entity,trigger) latch state that **E2's §8.6 serialization (cursor + ordinal
only) omitted**, so a flag-on `save@N → restore → advance` would **re-fire every still-above-threshold
trigger** on the first post-restore tick — the exact completeness-bug class the doc itself cites re
the GK/Heading Phase-2 latch. **Resolution (author decision: edge-triggered + latch, the recommended
option):** new KD-7 (§2.8) pins edge-triggered firing with a per-scope-key armed-off latch; §8.4
rewrites the loop as the four-state armed/latched table (rising edge = the sole draw+spawn tick); §8.6
serializes the `_latched` set as a count-prefixed canonical-order block in the E2 world blob (with the
E1 fail-loud extended to a non-empty latch and a `RestoreLatched` canonical-order gate); §9 adds test
10 (single-fire + re-arm) and test 11 (re-fire-after-restore lock, with a drop-the-latch negative
control); §5/§10/§12 updated. Post-v0.5 the plan is **CONVERGED** (the High is resolved; a full
re-read surfaced no new High/Medium). Section-file/code AR cycles run at implementation per the
project convention.

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
| `ArcCanonSource.cs` | `public sealed class` | The §2.2/§2.3 nullable canon-input seam the evaluator reads — a single concrete type (the deterministic in-code Stage-0 producer IS this class; the `ScenarioIndex`/`TeamTacticConfig` parser-swap precedent). NOT an abstract base + one subclass — that reintroduces the one-implementation abstraction KD-2 rejects. The real vol-2/vol-3 producer becomes a *second* concrete type; extract a base/interface only then. |
| `ArcTrigger.cs` | `public readonly struct` | One catalogue row: `TriggerId` + target `ArcKind` + `[GT]` threshold + input-capture rule. |
| `ArcTriggerCatalogue.cs` | `public static class` | The Stage-0 in-code trigger table (APPEND-only, like `InteractionTextCorpus`). |
| `ArcTriggerEvaluator.cs` | `public sealed class` | Registers `world.arcs`, walks canon in FR-LW-017/021 order, applies the KD-7 rising-edge latch, draws the stochastic component, calls `ArcEngine.SpawnArc`. Owns the stream **and the `_latched` armed-off set** (the §8.6-serialized cross-tick state), exposing canonical-order enumerate/restore accessors for `WorldStore` (the `InteractionTextGenerator` + GK/Heading-latch role). |
| `Tests/ArcTriggerTests.cs` | test fixture | §9 case list. |

**Modified files:**

| File | Change |
|------|--------|
| `LivingWorldConstants.cs` | +Fixed region rows (§8.2). |
| `WorldStore.cs` | Construct the evaluator at boot; wire it into the loop; E1/E2 serialization; `Restore` canon-source param. |
| `WorldLoop.cs` | Phase-4 evaluator call, null-guarded (a fifth nullable seam arg). |
| `season-save/SeasonSaveManager.cs` | **Slice 2 only** (§8.9 fix (a)): `Load` gains `ArcCanonSource canon = null`, passed into `WorldStore.Restore(blobs.WorldBlob, canon)` — the `ISquadProvider` Load-time-parameter precedent. Needed so a flag-on world round-trips through the season file; without it the restored season silently stops evaluating (§8.9). `season-save.asmdef` already references `TacticalDirector.LivingWorld`, so no asmdef change. |
| `LivingWorldConstants.cs` version history + `WorldStore.cs`/`WorldLoop.cs`/`SeasonSaveManager.cs` version history | Append rows per FR-CS-056. |

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

/// <summary>[FIXED] KD-7 latch scope key for board/squad-level triggers (§2.8). Real entity ids are
/// >= 0, so a negative sentinel never collides; board triggers share it and are disambiguated by
/// TriggerId. int.MinValue sorts first deterministically in the canonical latch order (§8.6).</summary>
public const int ARC_BOARD_SCOPE_KEY = int.MinValue;
```

`WORLD_STORE_FORMAT_VERSION` stays `2` at E1 and becomes `3` only at E2 (§8.6). `InteractionTextGenerator`'s
`entityId: -1` (line 60) is updated to `WORLD_STREAM_ENTITY_TEXT` (behaviour-identical — same value —
but cataloged). Per-kind `[GT]` trigger thresholds + `ARC_TRIGGER_*` lifetime rows go in `#region GT`.

### 8.3 `ArcCanonSource` (KD-2 — a single concrete, nullable seam)

**Concrete `sealed class`, NOT `abstract` + one subclass.** An abstract base with a single concrete
subclass is the same one-implementation abstraction §2.3/KD-2 rejects (it differs from a phantom
interface only in keyword). Ship one concrete class; the Stage-0 in-code producer *is* this class.
The real vol-2/vol-3 producer, when it lands, is a *second* concrete type — extract a base or
interface at that point, not before.

```csharp
public sealed class ArcCanonSource
{
    // Canon read in FR-LW-017/021 order. Entity-scoped signals iterated by ascending entity id;
    // board/squad-level signals keyed by ArcKind ordinal. Scalars only — they map 1:1 onto
    // SpawnCause.Input { short Key; float Value }. Every value MUST be finite (WriteF32 is Tier-A
    // no-NaN); the evaluator gates before capture.
    //
    // Stage 0: constructed as a deterministic pure function of the world state it is handed (no
    // System.Random, no clock) — e.g. from MemoryStore edge salience/relationship values — so it is
    // reproducible across Snapshot/Restore. Signatures only below (a sealed class needs real bodies —
    // these are backed by the captured state, elided here):
    public int EntitySignalCount => /* captured count */;
    public int EntityIdAt(int index) => /* ascending id */;
    public float EntitySignal(int index, short key) => /* e.g. pulse divergence, ego clash */;
    public float BoardSignal(ArcKind kind, short key) => /* board patience etc. */;
}
```

**null is the default and means "skip phase-4 trigger evaluation"** (§2.2). This class is the drop-in
slot for the real vol-2/vol-3 producer.

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

**Firing is edge-triggered against a serialized latch (KD-7, §2.8), not a bare level test.** The
evaluator owns `_latched` — the set of currently **armed-off** `(scopeKey, TriggerId)` pairs (scopeKey
= `entityId` for entity-scoped rows, `ARC_BOARD_SCOPE_KEY` for board rows). Per (scopeKey, trigger), with
`above = signal >= Threshold` (NaN fails closed via a negated compare — the store-seam precedent, so a
non-finite signal is treated as below threshold):

| armed-off? | `above`? | action |
|-----------|----------|--------|
| no (armed) | yes | **FIRE** (draw + `SpawnArc`, below) and **add** the pair to `_latched` |
| no (armed) | no  | no-op, **no draw** |
| yes | yes | no-op (still latched), **no draw** |
| yes | no  | **re-arm**: **remove** the pair from `_latched`, **no draw** |

So the **only tick that draws + spawns is the rising edge** (armed → above); a sustained-high signal
fires exactly once, and a non-edge tick leaves the `world.arcs` cursor untouched — the
`InteractionTextGenerator` "a refusal consumes no cursor" discipline (§8.5). Both the fire (add) and
the re-arm (remove) mutate `_latched`, which is why the set is serialized at E2 (§8.6): a
`save@N → restore → advance` that dropped the set would re-fire every still-above trigger on the first
post-restore tick — the KD-7 completeness bug, locked out by §9 test 10.

**On a rising edge**, in order:

1. **Draw one** `world.arcs` value for the stochastic accept/shape component.
2. Build the `SpawnCause` inline (`TriggerId`, the captured `Input[]` scalars, `Cause.WorldTick =
   _clock.CurrentWorldTick`), **resolve the pinnable episodes** — for an entity-scoped trigger, the
   firing edge's memory episodes; the set MAY be empty (`Array.Empty`), which is a valid **pin-less
   spawn**, NOT a skip (`SpawnArc` accepts an empty pin array — verified: its null-check refuses only
   `null`) — and call `ArcEngine.SpawnArc(kind, in cause, pins, spawnTick, maxLifetimeDays)`.
   `SpawnArc` does the atomic FR-LW-018 pinning + rollback, the `ArcKind` gate, and the
   lifetime/overflow gates. These are pure functions of the trigger row + `spawnTick`, so in a correct
   catalogue they always pass; a mis-authored row that fails one is a **fail-loud corruption abort**
   (the whole `AdvanceDay` throws), not a graceful skip. The latch add happens **only after** a
   successful `SpawnArc` return, so a corruption-abort never leaves a phantom armed-off entry.

There is deliberately **no "missing episode ⇒ skip-no-draw" gate**: the pin set is a pure function of
the serialized `MemoryStore` (so the spawn is reproducible across `Snapshot`/`Restore` regardless of
whether it pins zero or many), and gating a spawn on episode existence would make an entire arc class
(a board-level arc firing on an edge with no citable memory) permanently unspawnable. The rising-edge
test is the sole pre-draw gate; the stochastic `world.arcs` draw feeds only the post-edge
accept/shape component, a deterministic function of the (already-consumed) draw and therefore
replay-parity-safe.

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

Per-rising-edge draw (the `InteractionTextGenerator.Generate` discipline — the armed+`above` edge test
runs BEFORE the draw, so a non-edge tick — armed-below, still-latched-above, or re-arming — consumes no
cursor; a rising edge always draws and spawns, §8.4):

```csharp
if (_rng.Reserve(_streamIndex, 1) != 0) throw new InvalidOperationException(...);
if (_rng.DrawReserved(_streamIndex, 0, out ulong draw) != 0) { _rng.CloseReservation(_streamIndex); throw ...; }
_rng.CloseReservation(_streamIndex);   // advances RngCursor by DeclaredBudget (verified line 124-127)
```

**One draw per rising edge, not per tick** — a tick with no rising edge (including a re-arm or a
sustained-latched signal) leaves the cursor untouched, so a flag-off (null canon source) run never
advances it (E1 byte-identity). Registration is
**unconditional at boot** in a fixed position (after `world.text`) so the stream index is
positionally stable across save/restore whether or not a canon source is present.

**`MaxRngStreams` headroom.** The unconditional boot registration makes `world.arcs` the *second*
stream on `WorldStore._rng` (after `world.text`); `RegisterStream` fails loud if the service's
`MaxRngStreams` cap is exhausted. Confirm the cap has room for two (it does today — the service is a
service-wide catalogue sized well above two) as a one-line pre-check when landing Slice 1, so the
unconditional registration can never fail-loud on a tight cap.

### 8.6 `WorldStore` diffs

**Fields + ctor** (both public ctors flow through `WorldStore(int, ulong)`):

```csharp
private ArcTriggerEvaluator _arcTriggers;   // owns world.arcs; constructed unconditionally
private ArcCanonSource _canon;              // nullable; null => no evaluation (default)
// in WorldStore(int managerId, ulong worldSeed):
_arcTriggers = new ArcTriggerEvaluator(_rng);   // registers world.arcs AFTER _text (index order fixed)
_canon = null;
_loop = new WorldLoop(_clock, _memory, _arcs, _membership, _arcTriggers);  // loop holds the evaluator, NOT canon
```

**Canon is `WorldStore`-owned and passed per tick — `WorldLoop` never captures it (the fix for the
opt-in being wired as a dead setter).** `WorldStore._canon` is the single source of truth;
`AdvanceDay` passes it into the loop each tick (`_loop.RunWorldTick(_canon)`, §8.7), so a
post-construction change is always live. Add `public void SetArcCanon(ArcCanonSource canon)` — it sets
`_canon` and nothing else (no forwarding needed, because the loop reads canon as a per-tick argument,
not a field). A `WorldStore(int managerId, ulong worldSeed, ArcCanonSource canon)` overload seeds
`_canon` at construction. **`Restore(payload, canon)` sets `_canon = canon`** after rebuilding the
loop, so a season/direct restore threads the caller's canon into the same live path (§8.9). If a
future refactor instead makes `WorldLoop` hold canon as a field, `SetArcCanon` MUST forward to it and
`Restore` MUST pass canon into the rebuilt loop's ctor — but the per-tick-argument model above avoids
both forwarding seams and is the recommended shape.

**Snapshot()** — E2 inserts the `world.arcs` block between the `world.text` block and the membership
roster (keeping membership last so no existing byte-offset test moves). Current writer order
(`WorldStore.cs:268-288`): header → store block → world.text (seed/cursor/ordinal) → membership. E2
appends **two** things to the world.text region: the cursor/ordinal pair **and the KD-7 latch block**.
The latch is exposed by the evaluator as `LatchEntry[] EnumerateLatchedCanonical()` (an internal
`readonly struct LatchEntry { int ScopeKey; ushort TriggerId; }`, returned sorted by ScopeKey then
TriggerId), `int LatchedCount`, and `void RestoreLatched(LatchEntry[])` (validates canonical strict
ordering + no duplicates, then replaces the set) — the evaluator owns the state; `WorldStore` owns the
byte layout (the GK/Heading `CaptureState`/`RestoreState` split):

```csharp
// ... after the world.text WriteU64 x3 ...
RngStreamState arcStream = _rng.GetStreamState(_arcTriggers.StreamIndex);
CanonicalSerializer.WriteU64(payload, ref offset, arcStream.RngCursor);
CanonicalSerializer.WriteU64(payload, ref offset, arcStream.ActionOrdinal);

// KD-7 latch (§2.8): the armed-off (scopeKey, TriggerId) set, enumerated in canonical order
// (scopeKey ascending, then TriggerId ascending) so the byte stream is deterministic. Count-prefixed
// via the WriteI32-count / ReadCount discipline the membership block already uses (WorldStore.cs:282).
LatchEntry[] latched = _arcTriggers.EnumerateLatchedCanonical();   // sorted, evaluator-owned
CanonicalSerializer.WriteI32(payload, ref offset, latched.Length);   // WriteI32 count to match ReadCount's ReadI32 (WorldStore.cs:272/282 precedent)
for (int i = 0; i < latched.Length; i++)
{
    CanonicalSerializer.WriteI32(payload, ref offset, latched[i].ScopeKey);   // entityId or board sentinel (may be negative)
    CanonicalSerializer.WriteU16(payload, ref offset, latched[i].TriggerId);
}
// ... then membership roster (unchanged) ...
```

`ComputeSize` gains `+ 8 + 8` for the cursor/ordinal **plus** `+ 4 + latched.Length * (4 + 2)` for the
count-prefixed latch block (size computed from the same enumerated set, so writer and sizer never
disagree). Bump `WORLD_STORE_FORMAT_VERSION` 2 → 3 with the v2→v3 doc note.

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

// KD-7 latch restore. ReadCount fails loud on a corrupt/oversize prefix (the existing WorldStore.cs:388
// helper: 0 <= count <= remaining bytes). RestoreLatched replaces the evaluator's set.
int latchCount = ReadCount(payload, ref offset);   // existing WorldStore-local helper (WorldStore.cs:388), bound = remaining bytes
var latched = new LatchEntry[latchCount];
for (int i = 0; i < latchCount; i++)
{
    int scopeKey  = CanonicalSerializer.ReadI32(payload, ref offset);
    ushort trigId = CanonicalSerializer.ReadU16(payload, ref offset);
    latched[i] = new LatchEntry(scopeKey, trigId);
}
arcTriggers.RestoreLatched(latched);   // fail-loud on a non-canonical / duplicate-key ordering
```

**E1 (no schema bump)** ships everything above EXCEPT the cursor/ordinal `WriteU64`/`ReadU64` pairs,
**the KD-7 latch block**, and the version bump: the stream is registered and the evaluator wired, but
`Snapshot()` **fails loud** when `arcStream.RngCursor != 0 || arcStream.ActionOrdinal != 0 ||
_arcTriggers.LatchedCount != 0` (a flag-on save is not yet snapshot-safe — the `EnableGkHeading`
Phase-1 `NotSupportedException` durable-capture precedent). All three are checked: the cursor pair is
the resumable draw position, and the latch count is the KD-7 armed-off state — a run that fired
anything advances the cursor (every rising edge draws) so the cursor check alone would catch a fired
trigger, but a run that only **re-armed** (crossed above then dropped below, net-zero cursor motion is
impossible since the fire drew — so this is belt-and-suspenders) or any future draw-discipline change
is covered by gating on the latch too. A null-canon run keeps all three at 0/empty, so `Snapshot()`
succeeds and is byte-identical to today at `WORLD_STORE_FORMAT_VERSION` 2.

### 8.7 `WorldLoop` diff (phase-4)

`WorldLoop` gains **one nullable seam field** (`ArcTriggerEvaluator _arcTriggers`, a fifth ctor arg,
like `_arcs`/`_membership`) and its tick entry point takes **canon as a per-tick argument** —
`RunWorldTick(ArcCanonSource canon = null)` (the default keeps the existing arg-less callers, e.g.
`SeasonWorldLoopTests`, compiling). The loop does NOT hold a canon field, so there is no stale-canon
seam: `WorldStore.AdvanceDay` calls `_loop.RunWorldTick(_canon)` reading its live `_canon` each tick.
Phase 4 currently runs only `_arcs.Update(...)` (`WorldLoop.cs:79-82`); the evaluator call slots in
**before** the expiry sweep so a same-tick-spawned arc is subject to this tick's expiry bound only if
already expired (matches `SpawnArc`'s own `spawnTick`):

```csharp
// Phase 4 — arc evaluation (§3.4).
if (_arcs != null)
{
    if (_arcTriggers != null && canon != null)   // canon is the RunWorldTick parameter
        _arcTriggers.Evaluate(canon, _memory, _arcs, _clock.CurrentWorldTick);   // trigger evaluation
    _arcs.Update(_clock.CurrentWorldTick);                                        // §6.2 expiry sweep
}
```

Null canon ⇒ the evaluate call is skipped ⇒ byte-identical to today. The 2-arg / 4-arg `WorldLoop`
ctors stay (slice-1 hosts unchanged); add a 5-arg ctor carrying `arcTriggers`.

### 8.8 Fail-loud gates (item 1)

- `WORLD_STORE_FORMAT_VERSION` mismatch on `Restore` → `ArgumentException` (existing gate; v2 payloads
  are rejected fail-loud at E2, **no in-place migration** at Stage 0).
- `Snapshot()` at E1 with a non-zero `world.arcs` cursor or a non-empty latch
  (`RngCursor != 0 || ActionOrdinal != 0 || LatchedCount != 0`) → `NotSupportedException` (flag-on not
  yet snapshot-safe).
- `RestoreStream` non-zero return → `InvalidOperationException` (registration-order drift).
- KD-7 latch (E2): `ReadCount` corrupt/oversize prefix → `ArgumentException` (the ReadCount
  precedent); `RestoreLatched` given a non-canonical or duplicate-key ordering → `InvalidOperationException`
  (a tampered/foreign latch block, so the serialized set's canonical-order invariant is enforced on
  read, not trusted).
- Non-finite `SpawnCause.Input.Value` → already gated in `WorldStateSerializer.Serialize`
  (`:102-111`); the evaluator additionally gates at capture time.
- `ArcCanonSource` returning a non-finite signal → the evaluator's negated-compare threshold fails
  closed (no crossing, no draw).

### 8.9 Season-save flag-on restore — thread the canon source through `SeasonSaveManager.Load`

`WorldStore.Restore(payload, canon)` (§8.6) takes the canon source directly, so the **direct
`WorldStore` E2 acceptance path (§2.7) is self-contained** — no season-save change is needed to lock
it. But the item-3 season save (`src/season-save/SeasonSaveManager.cs`, already shipped) restores the
world blob via `WorldStore world = WorldStore.Restore(blobs.WorldBlob)` — **with no canon argument**.
Its `Load` signature is `Load(string path, ISquadProvider squads = null)`. So a **flag-on** world
restored *through the season file* comes back with `_canon == null`, and per §8.7 arc evaluation is
then silently skipped for the rest of the session — the restored season quietly stops spawning arcs
with no error. The bytes round-trip; the behaviour does not.

**This is the item-1 ⇄ item-3 boundary the plan must not leave implicit.** Two acceptable fixes; pick
(a):

- **(a) Thread canon through `SeasonSaveManager.Load` (the `ISquadProvider` precedent).** Change the
  signature to `Load(string path, ISquadProvider squads = null, ArcCanonSource canon = null)` and pass
  `canon` into `WorldStore.Restore(blobs.WorldBlob, canon)`. This is the exact Load-time-parameter,
  never-persisted pattern `ISquadProvider` already establishes (§1 / the match restore). One signature
  change + one pass-through; `SeasonSaveManager.cs` joins the §8.1 modified-file list, and it lands in
  **Slice 2** (it is only meaningful once E2 makes flag-on snapshot-safe).
- **(b) Scope §12 down.** If (a) is deferred, the season-save round-trip claim in §12 MUST be narrowed
  to "flag-off season saves round-trip (byte-identical); a flag-on world round-trips only through the
  direct `WorldStore.Restore(payload, canon)` path — flag-on-through-the-season-file requires the (a)
  `SeasonSaveManager` change, deferred." A flag-off world has `_canon == null` **by design**, so the
  season file already handles it correctly; only the flag-on case needs (a).

Either way, the plan no longer claims a capability it hasn't wired. §12 below reflects (a).

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
4. **Stub-canon spawns deterministically** — inject a canon source (`ArcCanonSource` primed to cross
   one threshold) **via `SetArcCanon` after construction** (guards Medium-2 — the opt-in must be live,
   not a dead setter captured null at loop construction); assert `ArcEngine.ArcCount` increments after
   `AdvanceDay`, the `SpawnCause.TriggerId`/`Input[]` match, and two same-seed runs produce
   byte-identical world state. Assert a **second** `AdvanceDay` with the signal still above threshold
   does **not** spawn again (`ArcCount` unchanged — KD-7 single-fire; the latch is armed-off). Also
   cover a pin-less spawn (a crossing on an edge with no citable episode) — `ArcCount` still increments
   (Medium-1: no missing-episode suppression).
5. **E1 fail-loud** — a flag-on store (canon set, a crossing driven) refuses `Snapshot()` with
   `NotSupportedException` while the cursor is non-zero.
6. **E2 acceptance predicate** — save@N of a flag-on run → `WorldStore.Restore(payload, canon)` →
   `AdvanceDay` to N+K; the resulting `Snapshot()` (and every intermediate arc/cursor) is
   byte-identical to an uninterrupted flag-on run advanced N→N+K. This is the §2.7 named lock.
7. **Trigger-order determinism** — a canon source exposing two entities + one board trigger that all
   cross on the same tick spawn in the pinned (entity-id ascending, then `ArcKind` ordinal) order.
8. **Restore fail-loud gates** — v2-format payload rejected at E2; truncated arcs block rejected;
   `RestoreStream` drift rejected.
9. **Season-save flag-on round-trip (§8.9(a))** — a flag-on `WorldStore` saved via
   `SeasonSaveManager.Save`, then `SeasonSaveManager.Load(path, squads: null, canon)`, then advanced
   N→N+K, is byte-identical to the uninterrupted flag-on run (the §2.7 predicate through the season
   file). Fails if `Load` is not threaded with `canon` (the restored world stops evaluating). Add in
   Slice 2 alongside test 6, in `season-save/tests/`.
10. **Edge-trigger re-arm cycle (KD-7)** — drive a canon signal above threshold (fires, `ArcCount`
    +1), hold it above across several `AdvanceDay`s (no further spawn — latched), drop it below (no
    spawn, re-arms — assert `LatchedCount` drops), then raise it above again (fires, `ArcCount` +1).
    Locks single-fire + re-arm; a bare level test would spawn on every above-threshold day. Slice 1
    (pure forward behaviour, no serialization).
11. **Re-fire-after-restore lock (KD-7 completeness)** — save@N a flag-on run whose trigger is
    **still above threshold and latched**, `WorldStore.Restore(payload, canon)`, then `AdvanceDay`:
    the trigger does **NOT** re-fire (`ArcCount` unchanged on the first post-restore tick, then
    identical to the uninterrupted run thereafter). This is the exact completeness bug the serialized
    latch prevents — a control run that drops the latch bytes on restore re-fires immediately and
    diverges (the negative assertion the test encodes). Subsumed byte-wise by test 6's digest match,
    but asserted behaviourally here because that is where the KD-7 latch earns its serialization.
    Slice 2 (needs the E2 latch block).

## 10. Sequencing (two landing slices, each its own AR + gate)

- **Slice 1 — E1 (no schema bump).** New files §8.1, constants §8.2, evaluator + registration +
  **KD-7 rising-edge latch (forward behaviour)** §8.4/§8.5, `WorldStore`/`WorldLoop` wiring §8.6/§8.7
  minus the serialize pairs **and the latch block**, tests 1–5 + 7 + 10. Ship byte-identical flag-off;
  flag-on deterministic-forward but `Snapshot()` fails loud (on a non-zero cursor **or a non-empty
  latch**). This is the reviewable, byte-neutral landing.
- **Slice 2 — E2 (`WORLD_STORE_FORMAT_VERSION` 2 → 3).** Add the cursor **and the KD-7 latch block**
  serialize/restore §8.6, drop the E1 fail-loud, thread canon through `SeasonSaveManager.Load`
  (§8.9(a)), add tests 6 + 8 + 9 + 11. Comparative round-trip, no absolute rebaseline. The season save
  frames the world blob opaquely, so `SEASON_SAVE_FORMAT_VERSION` is untouched — but the `Load` canon
  threading is required for the flag-on world to keep evaluating after a season restore (test 9);
  without it the round-trip claim in §12 is unmet.

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
  `key(world.arcs) != key(world.text)` locked; **KD-7 edge-trigger single-fire + re-arm** locked
  (test 10 — a sustained signal spawns one arc, not one per day).
- Slice 2: the §2.7 comparative round-trip predicate passes (direct `WorldStore.Restore(payload,
  canon)`), including the **KD-7 re-fire-after-restore lock** (test 11 — a still-latched trigger does
  not re-fire on restore because the latch set is serialized in the E2 world blob). A flag-on
  `WorldStore` also round-trips **through the unified season save** with no
  `SEASON_SAVE_FORMAT_VERSION` change — which requires the §8.9(a) `SeasonSaveManager.Load` canon
  threading (the season codec frames the v3 world blob opaquely, so the season format itself is
  untouched; the missing piece is passing `canon` into `WorldStore.Restore`, not the frame). Without
  §8.9(a) this bullet is unmet — the restored season would stop evaluating — so §8.9(a) is part of
  Slice 2, not optional.
- No `ArcEngine.cs` or `WorldStateSerializer.cs` production change (the arc cursor is a `WorldStore`
  composite field).

---

## Version History

- **v0.5 (2026-07-24):** Applied the pass-6 adversarial-review finding (1H+0M+0L) — the one the prior
  five passes missed, caught by a fresh hostile re-read of the core evaluator loop. **H-1: trigger
  firing semantics were unspecified and self-contradictory** — §8.4 used "crossing" (edge) language
  over a stateless `signal >= Threshold` level test with no previous-tick state, which under the level
  reading spawns one arc **per day** for a sustained signal (no `SpawnArc` dedupe; unbounded
  `world.arcs` cursor) and under the edge reading needs per-(entity,trigger) latch state that E2's
  §8.6 serialization (cursor + ordinal only) omitted — so a flag-on `save@N → restore → advance`
  re-fires every still-above-threshold trigger on restore (the GK/Heading Phase-2 latch completeness
  class the doc already cites). **Resolved (author chose edge-triggered + latch):** new **KD-7 (§2.8)**
  pins edge-triggered firing with a per-scope-key armed-off latch (rising edge = sole draw+spawn; drop
  below = re-arm; sustained-high = one arc, not per-day); §8.4 rewritten as the four-state
  armed/latched table; §8.6 serializes the `_latched` set as a count-prefixed canonical-order block in
  the E2 world blob (evaluator owns the state via `EnumerateLatchedCanonical`/`LatchedCount`/
  `RestoreLatched`, `WorldStore` owns the byte layout — the `CaptureState`/`RestoreState` split), with
  the E1 fail-loud extended to a non-empty latch and a `RestoreLatched` canonical-order/no-duplicate
  gate; §9 adds test 10 (single-fire + re-arm) and test 11 (re-fire-after-restore lock, with a
  drop-the-latch negative control); §2.4/§5/§8.1/§8.5/§8.8/§10/§12 threaded through. §7 records pass-6
  → CONVERGED.
- **v0.4 (2026-07-24):** Applied the pass-4 adversarial-review findings — two regressions the v0.3
  fixes themselves introduced, plus one Low (0H+2M+1L). **M-1 (regression from v0.3 M-3):** the
  pin-resolution "missing episode ⇒ skip-no-draw" gate §8.4 added was under-specified (the `ArcTrigger`
  struct defines no "target episode" to resolve) and wrong (it suppressed valid **pin-less** arcs —
  `SpawnArc` accepts `Array.Empty`, verified — so a board-level arc firing on an edge with no citable
  memory could never spawn). §8.4/§8.5 rewritten: the **threshold crossing is the sole pre-draw
  refusal**; a crossing always draws-then-spawns; an empty pin set is a pin-less spawn, not a skip; the
  draw-before-decide ordering is kept only for the genuine stochastic accept/reject case. §9 test 4
  gains a pin-less-spawn assertion. **M-2 (regression from v0.3 M-2 wiring):** the canon plumbing was a
  `/* + _canon via a setter or field */` hand-wave — `SetArcCanon` (post-construction) plus a
  `WorldLoop` that read `_canon` as a field could ship the opt-in as a **dead setter** (a store
  constructed then `SetArcCanon`'d before `AdvanceDay` would spawn nothing while asserting it should),
  and `Restore` never showed canon reaching the rebuilt loop. §8.6/§8.7 pin the per-tick-argument model
  (`WorldStore` owns `_canon` as the single source of truth; `WorldLoop.RunWorldTick(ArcCanonSource
  canon = null)` reads it live each tick; `SetArcCanon` sets `_canon` with no forwarding;
  `Restore(payload, canon)` sets `_canon = canon`), with a §9 test-4 dead-setter guard. **L:** §8.3's
  `sealed class` signature sketch used abstract-method syntax (`;`-terminated, uncompilable in a sealed
  class); switched to elided expression bodies. §7 records the pass-4 → CONVERGED status.
- **v0.3 (2026-07-24):** Applied the pass-3 adversarial-review findings over the §§8–12 implementation
  plan (0H+3M+2L). **M-1:** `ArcCanonSource` is now a single concrete `sealed class`, not an `abstract`
  base + one `Stage0ArcCanonSource` subclass — the abstract-with-one-impl shape reintroduced exactly
  the one-implementation abstraction §2.3/KD-2 rejects; the file inventory (§8.1) drops the separate
  subclass row and §8.3 spells out the reasoning. **M-2:** new §8.9 closes the item-1 ⇄ item-3 boundary
  gap — a flag-on world restored *through the unified season save* came back with `_canon == null` and
  silently stopped spawning arcs, because `SeasonSaveManager.Load` never threaded a canon source into
  `WorldStore.Restore`; the fix threads `ArcCanonSource canon = null` through `Load` (the
  `ISquadProvider` Load-time-parameter precedent), added to the §8.1 modified-file list, Slice 2 (§10),
  the §12 acceptance bullet, and a new test 9 (§9). **M-3:** §8.4/§8.5 reorder so pin resolution + the
  spawn decision precede the `world.arcs` draw (the validate-before-draw discipline the section cites),
  and pin a missing-episode = skip-no-draw contract so a crossing with no citable episode consumes no
  cursor. **L-1:** the E1 `Snapshot()` fail-loud guard checks `RngCursor != 0 || ActionOrdinal != 0`
  (§8.6/§8.8/§2.7). **L-2:** §8.5 adds the `MaxRngStreams`-headroom pre-check note for the
  unconditional second stream registration. §7 records the pass-3 → CONVERGED status.
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
