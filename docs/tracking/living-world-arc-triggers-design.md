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

## Version History

- **v0.1 (2026-07-23):** Created. Records item 3 (season-save fold) as already shipped; designs item 1
  (arc trigger evaluators + `world.arcs`) as an opt-in, default-off build behind a nullable
  `ArcCanonSource` seam with a distinct-key `world.arcs` stream and two-phase serialization; sharpens
  item 2 (BackgroundTierSim phase-5) as a documented seam without building it. Incorporates the
  planning-stage AR pass-2 Lows (cataloged world-scoped RNG entity-sentinel block; named E2 acceptance
  predicate).
