# Squad / Player #27 — Stage-1+ Deferrals: Detailed Implementation Plan

> **Created:** July 23, 2026 · **Updated:** July 23, 2026 (v0.2 — expanded from coordination plan to
> implementation-ready detail).
> **Status:** PLAN (implementation-ready coordination — no section files, no `SPEC_INDEX.md` row of
> its own). Governs *completing the three Stage-1+ items #27 deferred* (`squad-player-data-design.md`
> §4 last row): **on-disk save-format squad persistence, transfer market, aging**.
> **Master-plan home:** §4.2 → §4.3 · **Governed by:** `management-layer-spec-roadmap.md` + `spec-plans/`.
> **Authority boundary:** for the *internals* of #28/#30/#31 (FRs, formulas, exact field sets) the
> **approved section files are authoritative** (`docs/specs/player-progression-lifecycle/`,
> `.../season-competition-loop/`, and #31's future set). This plan owns the **cross-track
> implementation sequencing, the concrete codebase touch-points, the save-frame extension, RNG
> registration, and the test/commit slicing** — grounded in the real APIs (§2). Where it proposes a
> surface beyond what a section file already pins, it is marked **(proposed — confirm vs. section
> files at implementation time)**.

---

## 0. How to read this

The three deferrals are already decomposed by the roadmap onto owning specs (§1). Most are APPROVED
specs with no code yet. This document turns "which spec owns it" into "what files, signatures, blobs,
streams, tests, and commits actually land, in what order." It is organized as **four implementation
tracks** (A aging, B persistence, C transfers, D initial-DB residual), a **cross-track sequence**
(§7), a consolidated **determinism/save-version ledger** (§8), and a **test/CI strategy** (§9). Every
type signature in §3–§6 is written against a real, verified API (§2) or explicitly flagged proposed.

---

## 1. Mapping recap (verified July 23, 2026)

| #27 §4 deferral | Owner | Status | Track |
|---|---|---|---|
| **Aging** (age/decline/growth/retirement/regens) | **#28** Player Progression & Lifecycle (`FR-PG`) | **APPROVED** (11 section files, R-01..R-05) | **A** |
| **Persistence — career-state roster** | **#28** (serialize) + **#30** Season Loop (compose) | both **APPROVED** | **B** |
| **Persistence — initial/reference/shipped roster** | **#27 residual → #47** New-Game Setup & DB Editor | #27 residual open; **#47 PLAN** (Wave 7) | **D** |
| **Transfer market** (windows/bids/contracts/negotiation) | **#31** Transfers, Contracts & Negotiation (`FR-TX`) | **PLAN** (Wave 4; pre-supplement) | **C** |
| *(adjacent)* training growth input | **#29** Training System (`FR-TR`) | **APPROVED** — fully deterministic, no RNG stream | feeds A |
| *(adjacent)* budget/wage constraint | **#40** Club Finances & Economy (`FR-FN`) | PLAN (Wave 2) | feeds C |

**Load-bearing invariant this plan protects:** #27's canonical `PlayerAttributes`/`PlayerRecord`
struct stays **frozen** (no CA/PA fields, no #27 version bump). Over-time state lives in *parallel*
owner-blocks (#28 lifecycle, #31 contracts, #40 economy), each handed to #30's `SeasonSaveCodec`
frame as an opaque, independently version-gated sub-blob (contracts C-1..C-7, §11).

---

## 2. Grounding — the real APIs each track builds on

Verified against source July 23, 2026. Signatures in §3–§6 reuse these exactly.

**Player database (`src/player-database/`, `TacticalDirector.PlayerDatabase`)**
```csharp
struct PlayerRecord { int PlayerId; string FirstName, LastName; int Age; PlayerPosition Position; PlayerAttributes Attributes; static CreateDefault(int playerId); }
static class RosterGenerator { static Squad Generate(DeterministicRngService rng, int streamIndex, int clubId, int count); }   // per player: Reserve(streamIndex, FIELDS_PER_PLAYER) -> 36x DrawReserved -> CloseReservation; PlayerId = clubId*CLUB_SQUAD_SIZE + localIndex
static class PlayerDatabaseConstants { CLUB_SQUAD_SIZE=25; ATTRIBUTE_COUNT=31; IDENTITY_DRAWS_PER_PLAYER=5; FIELDS_PER_PLAYER=36; ATTRIBUTE_MIN=1; ATTRIBUTE_MAX=20; WEAK_FOOT_MIN=1; WEAK_FOOT_MAX=5; }
static class NameCatalogue { /* 32 first + 32 last, APPEND-only */ }
```
**Deterministic RNG (`src/deterministic-sim/`, `TacticalDirector.DeterministicSim`)**
```csharp
int    RegisterStream(string siteId, int subsystemOrdinal, int entityId, ushort streamVersion);   // -> streamIndex
ushort Reserve(int streamIndex, int count);
ushort DrawReserved(int streamIndex, int index, out ulong value);
void   CloseReservation(int streamIndex);
ref readonly RngStreamState GetStreamState(int streamIndex);            // cursor + actionOrdinal, for serialization
ushort RestoreStream(int streamIndex, in RngStreamState restored);     // restore cursor on load
static class SubsystemOrdinals { PlayerDatabase = 81; /* PlayerProgression = 82 lands at #28 T2 */ }
```
**Living world (`src/living-world/`, `TacticalDirector.LivingWorld`)**
```csharp
sealed class WorldStore {
  WorldStore(int managerId); WorldStore(int managerId, ulong worldSeed);
  uint CurrentWorldTick { get; }                 // one worldTick = one calendar day
  void AdvanceDay();                             // runs the injected WorldLoop one day
  byte[] Snapshot(); static WorldStore Restore(byte[] payload);   // WORLD_STORE_FORMAT_VERSION + DOMAIN_TAG_LIVING_WORLD gated
}
```
**Season save root (`src/season-save/`, `TacticalDirector.SeasonSave` — owned by #30)**
```csharp
static class SeasonSaveCodec {                    // treats every sub-blob opaque; never parses it
  static byte[]          Encode(byte[] worldBlob, byte[] matchBlobOrNull);      // <-- extended by #30/#28 (§4)
  static SeasonSaveBlobs Decode(byte[] blob);                                   // version gate -> matchPresent flag -> length-prefixed blocks; overflow-safe Require + trailing-byte guard
}
static class SeasonSaveManager { static void Save(WorldStore, MatchEngine, string path); static SeasonSaveContents Load(string path, ISquadProvider squads = null); }
readonly struct SeasonSaveContents { WorldStore World; MatchEngine Match; }     // <-- extended by #30/#28 (§4)
static class SeasonSaveConstants { uint SEASON_SAVE_FORMAT_VERSION = 1; }       // <-- bumped by #30 (1->2), #28 (2->3)
```

---

## 3. Track A — Aging & lifecycle (#28)

**New assembly** `src/player-progression/` — namespace `TacticalDirector.PlayerProgression`,
`references: ["TacticalDirector.PlayerDatabase", "TacticalDirector.DeterministicSim"]` (the two
upstreams; it references **neither** season-save nor match-engine — the season-save root references
*it*, per #28 §3). Bottom-of-graph, world-tick, not a hot path (KD-6 class — plain classes/arrays).

### A.0 — Gates
- **Depends on:** #27 (`PlayerRecord`/`RosterGenerator`/`NameCatalogue`/constants) + #16 (RNG). Both landed.
- **T0 is buildable now** (pure types; no #30). **T1 needs Track B's #30 season frame** (composes the
  blob). **T2 needs #30's day-advance loop implemented** (#28 §10 L-2 — wiring T2 before #30's seam
  code exists binds a phantom). **T3 needs #29's producer** (APPROVED; consumed as a neutral-defaulted input).

### A.1 — T0: lifecycle value types + pure projections (behaviour-neutral, buildable now)

| File | Contents |
|---|---|
| `PlayerProgressionConstants.cs` | `[GT]` `RETIREMENT_AGE=36`, `DECLINE_AGE`, `GROWTH_AGE`, `POINT_COST`, `ABILITY_MAX`; `[FIXED]` `DAYS_PER_YEAR`, `PROGRESSION_SAVE_FORMAT_VERSION=1`; balance-pass locks per #28 §8 (values from the section files). |
| `PlayerLifecycle.cs` | value type keyed by `PlayerId`: `int PotentialAbility`, `int CurrentAbility` *(derived cache, KD-1 — never a second accumulator)*, `int GrowthCursor` *(integer fixed-point, the sole accumulator)*, `uint AgeAnchorDay`, `bool RetirementFlag`, `uint RetirementDay`. |
| `TrainingInput.cs` | value type + `static TrainingInput Neutral` (the KD-2 seam #29 writes; neutral until #29 lands). |
| `GrowthProjection.cs` | `static Deltas Project(in PlayerLifecycle lc, int age, PlayerPosition pos, bool curveEnabled, in TrainingInput t)` — the sole attribute-mutation function; §4.3 ±1/yr identity when `curveEnabled` off (KD-8), integer fixed-point cursor accrual/spend (KD-1). |
| `RegenGenerator.cs` | `static PlayerRecord GenerateOne(DeterministicRngService rng, int streamIndex, int clubId, int newPlayerId, ...)` — the single-player analogue of `RosterGenerator.Generate`; **same** `Reserve(streamIndex, FIELDS_PER_PLAYER)`/`DrawReserved`/`CloseReservation` pattern + `NameCatalogue`; takes an explicit `newPlayerId` (never reuses a retiree's — KD-3). |
| `RetirementResult.cs` / `RegenResult.cs` | boundary signals (`PlayerId` removed / `PlayerRecord` inserted). |
| `LifecycleViewModel.cs` | read-only value-copy view (age/CA/PA/retirement) for #31/#38 (observer-neutral, KD-7). |

**Tests** (`src/player-progression/tests/`): `GrowthProjectionTests` (curve-off == literal §4.3 step,
byte-for-byte; cursor accrual/spend integer-exact; PA ceiling respected); `RegenGeneratorTests`
(same seed+club → same record, fresh `PlayerId`, `FIELDS_PER_PLAYER` budget lock — the
`RosterGeneratorTests` posture); `PlayerLifecycleTests` (CA-derived-from-attributes, never diverges).
**Acceptance:** whole tree green under the dotnet gate; no behaviour wired yet (no engine calls).
**Commit slice:** one PR, `feat(progression): #28 T0 lifecycle types + pure projections`.

### A.2 — T1: the persistence sub-blob (needs Track B §4)

`ProgressionEngine.cs` (sealed) gains `byte[] Snapshot()` / `static ProgressionEngine Restore(byte[])`
under `PROGRESSION_SAVE_FORMAT_VERSION`, serialized via #16 `CanonicalSerializer`, layout:

```
u32  PROGRESSION_SAVE_FORMAT_VERSION           # version gate (fail-loud on mismatch)
byte DOMAIN_TAG_PLAYER_PROGRESSION (0x20)      # tag gate
u32  managedCount                              # ReadCount-guarded (overflow-safe bound, the SeasonSaveCodec.Require posture)
repeat managedCount:
     PlayerRecord (PlayerId, names, Age, Position, PlayerAttributes[31]+WeakFoot)   # the evolving career-state record (KD-4 — serialize, don't regenerate)
     int PotentialAbility; int GrowthCursor; u32 AgeAnchorDay; byte RetirementFlag; u32 RetirementDay
int  NextPlayerId                              # monotonic regen-id cursor (KD-3)
RngStreamState progressionStream               # cursor + actionOrdinal (GetStreamState/RestoreStream) — a mid-boundary regen resumes byte-exact
# trailing-byte guard: read must end exactly at buffer end
```
**Invariant (KD-4 / §9):** `|managed block| == |managed roster|` — a vacancy is filled 1:1, so the blob
is size-stable across seasons. **Tests:** `ProgressionSaveTests` — round-trip field-identity; fail-loud
on bad version / out-of-bounds length prefix / trailing bytes. **Commit:** `feat(progression): #28 T1
PROGRESSION_SAVE_FORMAT_VERSION block`. *(Composition into the season frame is Track B.)*

### A.3 — T2: wire the world-tick steps + register the RNG stream (needs #30 loop)

`ProgressionEngine` exposes the two steps #30 invokes at its reserved seams:
```csharp
void AdvanceDay(uint worldDay, in TrainingInputs training);                 // per-day: age accrual + GrowthProjection (banked daily, KD-1); flags retirement at RETIREMENT_AGE (KD-5)
(RetirementResult[] retired, RegenResult[] regens) RunSeasonBoundary(...);  // applies deferred roster mutations only (KD-6); #30/#27 apply the Squad remove+insert
```
- **RNG registration (the first draw site):** at the first regen, register
  `rng.RegisterStream("player-progression.regen", SubsystemOrdinals.PlayerProgression, entityId: clubId, streamVersion: 1)`.
  **This is where the code const `SubsystemOrdinals.PlayerProgression = 82` lands** (add to
  `src/deterministic-sim/SubsystemOrdinals.cs`) — never earlier (registering a zero-draw stream is the
  `world.arcs` phantom-surface class, FR-LW-031). The `0x20` domain tag is already promoted in #16 §3.4.
- **Wiring:** #30's `AdvanceDay` loop calls `ProgressionEngine.AdvanceDay` at its reserved tick-order
  slot; #30's season-boundary roll calls `RunSeasonBoundary` and applies the `RegenResult`/`RetirementResult`
  to the roster (#28 never mutates `Squad` directly — KD-5/KD-7).
**Tests:** `ProgressionEngineTests` — retirement flagged mid-season, `Squad` mutation only at boundary;
two-run multi-season projection byte-identical (aging half draw-free); `TrainingInput.Neutral` step ==
no-training step (KD-2 seam neutrality). **Commit:** `feat(progression): #28 T2 world-tick steps + regen stream`.

### A.4 — T3: deep CA/PA curve dial + #29 training input
Flip `curveEnabled` on (a config dial, KD-8); `GrowthProjection` reads a non-neutral `TrainingInput`
from #29. No new save version (same fields, richer values). **Tests:** curve-on deterministic;
curve-on ≠ curve-off only where the curve is configured. **Commit:** `feat(progression): #28 T3 deep curve + #29 input`.

---

## 4. Track B — Persistence: extend the season frame (owned by #30)

The career-state roster (Track A) and #30's season state persist by **growing the `SeasonSaveCodec`
frame with more opaque sub-blobs** — the codec's never-parse contract means each blob keeps its own
version gate and the world/match blobs stay byte-untouched.

### B.1 — Codec + contents + manager extension
```csharp
// SeasonSaveCodec — extend from 2 blobs to N opaque length-prefixed blobs (each optional-by-version):
static byte[] Encode(byte[] worldBlob, byte[] seasonBlobOrNull, byte[] progressionBlobOrNull, byte[] matchBlobOrNull);
static SeasonSaveBlobs Decode(byte[] blob);   // adds seasonPresent/progressionPresent flags beside matchPresent; same overflow-safe Require + trailing-byte guard
// SeasonSaveContents — carry the reconstructed halves:
readonly struct SeasonSaveContents { WorldStore World; SeasonLoop Season; ProgressionEngine Progression; MatchEngine Match; }
// SeasonSaveManager.Save/Load — thread season + progression blobs (Save: season.Snapshot()/progression.Snapshot(); Load: SeasonLoop.Restore()/ProgressionEngine.Restore())
```
### B.2 — Version sequencing (C-5 — the main coordination hazard)
| Step | Frame version | Blobs in frame | Landed by |
|---|---|---|---|
| today | `SEASON_SAVE_FORMAT_VERSION = 1` | world, match | (shipped) |
| #30 lands | **1 → 2** | world, **season**, match | Track B / #30 T-phase |
| #28 T1 lands | **2 → 3** | world, season, **progression**, match | Track A.2, after #30 |

Whoever lands second rebases on the other's frame layout (#30 §9 / #28 §9 ordering note). Only the
**outer** frame version sequences; each inner blob (`WORLD_STORE_FORMAT_VERSION`,
`PROGRESSION_SAVE_FORMAT_VERSION`, `MATCH_SAVE_FORMAT_VERSION`, the season block's own version) is
independently gated. **No cross-version migration at Stage 0** (KD-4 — fail-loud on mismatch; #50 owns
migration later). **Tests:** composed round-trip (world+season+progression+optional match through one
file, field-identical); fail-loud on each version gate; mid-day + mid-boundary restore == uninterrupted.

---

## 5. Track C — Transfer market (#31) — the only track needing new design

#31 is a one-page PLAN (Wave 4). It is dependency-heavy (critical path `#33 → #31`, hard reads on
**#40** economy + **#30** window calendar), so it is correctly **last**. Its detailed plan is the
**promotion pipeline first**, then a first-cut T-phase decomposition the supplement/section files pin.

### C.1 — Promotion pipeline (the #21–#30 path)
1. Open `docs/tracking/transfers-contracts-design.md` (DESIGN SUPPLEMENT) — answer #31's plan KD-1..KD-5
   (valuation input vector + personality-as-multiplier; the #40 write-seam boundary; the reusable
   negotiation surface #32/#34 consume; clause/loan/wage serialization; rival-AI-bid draw timing).
2. Self-adversarial review to convergence (AR-1 → clean/L-only).
3. Author 11 section files at `IN REVIEW`, FR prefix `FR-TX`, register `SPEC_INDEX.md` row.
4. Section-file PASS-1 → AR-2 convergence.
5. File back-props at approval: allocate `DOMAIN_TAG_TRANSFERS = 0x23` / `SubsystemOrdinals.Transfers = 85`
   in #16 §3.4 (roadmap §6 — **currently proposed, no catalogue row yet**); #30 season-save composition note.
6. R-01..R-05 sign-off → APPROVED.

### C.2 — First-cut T-phase decomposition (proposed — the supplement pins it)
| Phase | Surface (proposed) | Determinism / save |
|---|---|---|
| **T0** | `TransferValuation` (pure `value(playerAttrs, age, need) → price`, the §4.3 minimal identity); `Contract` value type (wage/length); `TransferWindow` calendar reader over #30's cursor | draw-free; no save |
| **T1** | the reusable **negotiation loop / offer-response seam** (#32/#34 consume — KD-3) + transfer-action command APIs (`SetX`-style, UI-driven, never direct mutation) | draw-free minimal |
| **T2** | persistence: contracts → durable `WORLD_STORE_FORMAT_VERSION` block; window/in-flight negotiation → season sub-blob (frame `3 → 4`) | opaque sub-blobs, version-gated |
| **T3** | deep: agents/clauses/loans/wage-structures modulating the same valuation; #33 personality as a multiplier; #40 budget as a read-only constraint; rival-AI bids draw from `0x23` | `0x23` stream lights up |

**Behaviour-neutral identity (C-7 test):** a #33-unconfigured negotiation reproduces the deterministic
§4.3 valuation exactly. **Fail-loud:** over-budget bid / malformed contract / action outside a window /
bid on a player not in #27's pool.

---

## 6. Track D — Initial/reference-roster on-disk DB (#27/#47 residual)

The one genuine open sliver. Today the *initial* roster is a `RosterGenerator` draw or a per-squad
`SquadFileLoader` text import; there is no shipped, editable, **full-world** DB format. Owned by #47
(Wave 7), but the **format-only pass can land earlier** (the editor UI is the large, later part).

- **Format:** a multi-club world-DB text/JSON parsed to `Squad[]`, load-time only — extend the
  `SquadFileLoader` grammar to multiple `[club N]` sections (or a JSON sibling, master plan §4.6 "JSON
  for V1"), fail-loud on unknown key/section/duplicate/out-of-range, omitted field ⇒ documented default.
- **Determinism/save:** **none.** It is a load-time *source* that produces the initial roster world;
  it is **not** a save-version item and enters no digest — only the resulting `PlayerRecord` values
  matter (the `SquadFileLoader`/tactic-file-loader posture). The *career-state* of that roster persists
  via Track A/B once a career starts.
- **Handoff (verbatim, #28 §4 / KD-4):** *"A future shipped-database / on-disk-roster pass (#47 / a
  #27 Stage-1+ deliverable) supplies the initial roster; #28 remains the owner of the career-state
  roster…"* **Tests:** format round-trip ↔ `Squad[]`; parity with `RosterGenerator`/`SquadFileLoader`
  defaults; every fail-loud gate. **Open decision (R-1):** land a format-only pass early, or wait for
  #47's editor — decide when a real starting world is first required.

---

## 7. Cross-track sequence & critical path

Slice of the roadmap critical path `#27 → #30 → #33 → #31`, annotated with the deferral each wave
discharges. **Bold** = a gate another track waits on.

```
NOW      Track A.1  #28 T0 (pure lifecycle types + GrowthProjection + RegenGenerator)   [no new dep]
                        │
Wave 1   Track B    ►  #30 SPINE: day-advance loop + season state + SEASON_SAVE_FORMAT_VERSION 1→2   ◄ gates A.2, A.3, C, and every world-tick spec
                        │
Wave 2   Track A.2  ►  #28 T1 progression blob → frame 2→3        (career-state PERSISTENCE)
         Track A.3  ►  #28 T2 world-tick steps wired at #30 seams (AGING goes live) + regen stream 0x20/82
                       #29 training (writes A's neutral TrainingInput seam) · #40 economy (C's constraint)
                        │
Wave 3              ►  #33 personalities/morale                    (gates C's deep valuation)
                        │
Wave 4   Track C    ►  #31 transfers: supplement → section files → T0..T3   (TRANSFER MARKET)
                        │
Wave 7   Track D    ►  #47 initial-roster DB format + editor        (initial-roster PERSISTENCE sliver)
                       (format-only pass pullable earlier — R-1)
Anytime  Track A.4     #28 T3 deep curve + #29 input               (after #29 producer + A.3)
```

**Net:** aging is buildable immediately (A.1) and behaviourally live by Wave 2; career-state
persistence is Wave 2; transfers Wave 4; the shipped initial-DB is Wave 7 or a pulled-forward
format-only pass. Only #31 is blocked on undesigned work (correctly last); everything else is
implementation against approved specs.

---

## 8. Determinism & save-version ledger (consolidated)

**Determinism (know reserved vs. proposed — C-7):**

| Spec | Tag / ordinal | State in `#16 §3.4` | Draw site | Lands |
|---|---|---|---|---|
| #27 | `0x1F` / 81 | in code (`PlayerDatabase = 81`) | `player-database.roster-generation`, `entityId=clubId` | shipped |
| #28 | `0x20` / 82 | **promoted** (ERR-028-001) | `player-progression.regen`, `entityId=clubId` — regen **only** (aging is draw-free) | const + stream at **A.3 (T2)** |
| #29 | `0x21` / 83 | **reserved, NOT promoted** (ERR-029-001) | none — fully deterministic, no stream | (future stochastic extension only) |
| #30 | `0x22` / 84 | reserved at approval | `season-loop.season-events`, `entityId=seasonNumber` | const + stream at #30 T2 |
| #31 | `0x23` / 85 | **roadmap §6 proposed — no catalogue row** | rival bids / agent demands (deep tier) | allocated at #31 promotion |
| #40 | `0x29` / 91 | **roadmap §6 proposed — no catalogue row** | (economy stochastics, if any) | allocated at #40 promotion |

**Save-format versions (only the outer frame sequences; inner blobs independently gated):**

| Version const | Current | After | Owner |
|---|---|---|---|
| `SEASON_SAVE_FORMAT_VERSION` | 1 | 2 (#30 season) → 3 (#28 progression) → 4 (#31 transfers) | #30 root |
| `PROGRESSION_SAVE_FORMAT_VERSION` | — (new) | 1 | #28 |
| `WORLD_STORE_FORMAT_VERSION` | (current) | +contracts block (#31) | #22/#31 |
| `MATCH_SAVE_FORMAT_VERSION` | (current) | untouched | #matches |

---

## 9. Test & CI strategy

- **Per-track suites** (§3–§6) run under the **full dotnet gate** (`tools/dotnet-ci/run-gate.sh`) —
  whole tree green, quarantine empty, any new failure/compile-error fails CI. Each T-phase PR must pass.
- **Behaviour-neutral locks** (the #21/#27/#30 discipline): every "identity" claim is a byte-for-byte
  digest/round-trip test — curve-off == §4.3 step; `TrainingInput.Neutral` == no-training;
  #33-unconfigured negotiation == deterministic valuation.
- **Round-trip determinism** at every persistence step, including **mid-year** (A.2), **mid-boundary**
  (A.3), **mid-negotiation/mid-window** (C.T2), through the composed season file (B).
- **Capstone scenario** (once A.3 + #30 are wired): a `multi-season-aging` `#19 ScenarioRunner`
  scenario — build a roster, advance N seasons through the real #30 loop, assert aged state + a
  two-run determinism digest (the match-engine-capstone precedent). Owning specs `{16,19,27,28,30}`.

---

## 10. Risks & open decisions

- **R-1 (open decision) — the initial-DB residual has no supplement.** The one persistence sliver not
  covered by an approved spec's implementation. Parked at #47 (Wave 7); pull a **format-only** pass
  forward if a real starting world is needed before the editor UI. *Decide when that need first arises.*
- **R-2 — save-version sequencing (§8/C-5).** The main coordination hazard: #30/#28/#31 all bump
  `SEASON_SAVE_FORMAT_VERSION`. Land in wave order; each rebases on the prior frame layout; fail-loud
  gates catch a desync. Mitigated by §7's ordering.
- **R-3 — #31 is dependency-heavy.** Phantoms if authored before #40/#33 exist. Its Wave-4 position is
  deliberate; do not pull it forward.
- **R-4 — #27 struct-freeze pressure (C-1).** Every lifecycle/valuation consumer will want a summary
  field on `PlayerAttributes`; resist — CA is a *derived* summary (#28 KD-1), never a stored #27 field.
  A #27 record bump ripples through every projection + the roster-reference snapshot.
- **R-5 — #28 T2 must not precede #30's seam code** (#28 §10 L-2). Enforced by §7 (A.3 sits in Wave 2,
  after the Wave-1 #30 spine).

---

## 11. Contracts preserved (condensed — full text in the owning specs)

- **C-1** #27's canonical struct is **frozen** (no CA/PA fields, no #27 version bump; #28 KD-4).
- **C-2** one attribute-mutation writer (`GrowthProjection`); training/transfers are **inputs**, never parallel mutations (#28 KD-2/KD-7).
- **C-3** serialize the career-state roster, don't regenerate on load (#30 KD-5); only the *initial* roster is generated/imported.
- **C-4** the season-save root is the only assembly that sees both sub-blobs; each is **opaque + version-gated** (SeasonSaveCodec never-parse).
- **C-5** save-format version sequencing — outer frame sequences by wave order; inner blobs independently gated (§8).
- **C-6** command-seam discipline — UI drives public `SetX`-style commands, never direct mutation (#28 KD-7 / #31 §7).
- **C-7** determinism band — register a stream only at its first *draw site* (FR-LW-031); reserved (0x20/0x22) ≠ proposed (0x23/0x29); aging & minimal-valuation are draw-free.

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-23 | Initial coordination plan — mapping of the three #27 §4 Stage-1+ deferrals to owners (#28/#30/#27-#47/#31), contracts C-1..C-7, wave sequence, residual initial-DB sliver. |
| — | 2026-07-23 | AR-1 (2M+1L) + AR-3 (1L) fixes: #29 determinism (reserved-not-promoted), #31/#40 tags (proposed, no catalogue row), §4.6 mis-citation, #28 handoff citation (§4). Converged. |
| 0.2 | 2026-07-23 | **Expanded to a detailed implementation plan.** Added §2 grounded-API reference (verified against `SeasonSaveCodec`/`DeterministicRngService`/`RosterGenerator`/`WorldStore` source); four implementation tracks (A aging #28 T0–T3 with file lists + signatures + the `PROGRESSION_SAVE_FORMAT_VERSION` blob layout + RNG registration; B season-frame codec extension + version-sequencing table; C #31 promotion pipeline + first-cut T-phases; D #27/#47 initial-DB format-only pass); §7 cross-track sequence diagram; §8 consolidated determinism/save-version ledger; §9 test/CI strategy incl. the `multi-season-aging` capstone; §10 risks; §11 condensed contracts. Section files remain authoritative for #28/#30/#31 internals; proposed-beyond-section-file surfaces are flagged. |
