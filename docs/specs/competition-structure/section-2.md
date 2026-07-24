# Competition Structure #43 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-CP-001..027)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-CP-001 | A league MUST be a competition **instance** (`CompetitionFormat.RoundRobin`), not a separate type; `CompetitionFormat { RoundRobin = 0, Knockout = 1, GroupThenKnockout = 2 }` MUST be ordinal-stable. | MUST | KD-1 |
| FR-CP-002 | Instance 0 MUST be a **binding row only** (an id/tag — "the league lives in #30"): #43 MUST hold no #30 object or live reference; instance-0 reads MUST go through the composition root against #30's read surface (FR-SN-032/033 respected). | MUST | KD-1 |
| FR-CP-003 | A season under the singleton collection (`{instance 0}`) MUST advance **byte-identical** to bare #30: no draw, no stream registered, the (a') point unvisited, the sub-blob = version + the instance-0 binding only. | MUST | KD-8 |
| FR-CP-004 | `CompetitionId` MUST be config-assigned at genesis (deterministic; instance 0 = 0; never reused); the registry MUST store and decode instances in ascending `CompetitionId` order. | MUST | KD-1 |
| FR-CP-005 | Entrant sets MUST be stored, decoded, and fed to every draw in **canonical ascending `ClubId` order**; a shuffled-input entrant set MUST produce the same pairings as the canonical input (no unordered iteration ever feeds a draw). | MUST | KD-7 |
| FR-CP-006 | Round-robin instances MUST reuse #30's `FixtureScheduler.Generate(clubIds, seed)` and `LeagueTable` per instance; #43 MUST NOT re-implement fixture-generation or table logic. The per-instance seed MUST be a **pure, draw-free derivation** `instanceSeed = DeriveInstanceSeed(worldSeed, competitionId, seasonNumber)` (no stream, no cursor — a deterministic hash, the #30 `DeriveNextSeasonSeed` class); distinct instances MUST NOT share a fixture sequence. | MUST | KD-1/KD-2 |
| FR-CP-007 | *(deep)* Knockout/group draws MUST be **position-independent keyed draws** on the `competition.draws` stream (`entityId = competitionId`) with a fixed-radix action ordinal over `(seasonNumber, roundIndex, slotIndex, purpose)` (§3.2; APPEND-only purposes); **no cursor** MUST exist or be serialized. | MUST | KD-2 |
| FR-CP-008 | The minimal tier MUST register **no** RNG stream; `_RESERVED_0x2C_` / `SubsystemOrdinals.Competition = 94` MUST remain RESERVED at approval (created by the ERR-043-001 sweep); promotion to `DOMAIN_TAG_COMPETITION = 0x2C` happens at the deep tier's first draw (spec-text-first). | MUST | KD-2 |
| FR-CP-009 | *(deep)* A round's drawn permutation MUST be keyed Fisher–Yates over the canonical entrant base (draw `i` keyed with `slotIndex = i`); pairings are `drawn[0]v[1], [2]v[3], …`. | MUST | KD-2/KD-7 |
| FR-CP-010 | *(deep)* `BracketState` MUST persist each resolved round (entrant list + winners) — serialize-don't-regenerate; a load MUST reconstruct from the blob and MUST NOT re-roll any draw. | MUST | KD-3 |
| FR-CP-011 | *(deep)* Bracket coherence MUST fail loud (F4): a winner must be one of its pairing's two entrants; round entrant counts must halve; per-round lists in canonical layout (§3.3). | MUST | KD-3/F4 |
| FR-CP-012 | #43 state MUST persist as an opaque, independently version-gated `COMPETITION_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec`; the codec MUST NOT parse it; #43 MUST NOT bump `WORLD_STORE_FORMAT_VERSION`; instance 0's league data MUST NOT be duplicated into it. | MUST | KD-6 |
| FR-CP-013 | The sub-blob codec MUST fail loud (F3) on a version mismatch, an out-of-bounds length prefix (overflow-safe `total − offset` bound), or trailing bytes — the `SeasonSaveCodec` posture. | MUST | KD-6/F3 |
| FR-CP-014 | The serialized block MUST contain no `RngCursor`/`actionOrdinal` field (keyed draws — the FR-TX-018/FR-SC-014 posture). | MUST | KD-2/KD-6 |
| FR-CP-015 | *(deep)* Promotion/relegation MUST be a **pure deterministic transform** (no draw) at FR-SN-031's (a') — bottom `RELEGATION_COUNT` of division `d` swap with top `PROMOTION_COUNT` of division `d+1` over the divisions' **final** standings (#30's FR-SN-007 total order precludes ties) — executed **before** #40's (b') finance settlement. | MUST | KD-4 |
| FR-CP-016 | The transform MUST mutate **division membership only**: `ClubId`s never re-key; no cross-system migration hook is dispatched (squads/finances/knowledge key by stable ids and never notice). | MUST | KD-4 |
| FR-CP-017 | *(deep)* The transform's membership output MUST be applied to **every** division instance's entrant set — including instance 0's `SeasonState.ClubIds` via #30's command API — **before** roll step (c) regenerates fixtures; the code-side (a') hook is a T-phase #30 coordination (soft-reserved ERR-030-008), not built at approval. | MUST | KD-4 |
| FR-CP-018 | The transform MUST be a **no-op in a one-division world** and MUST run inside #30's restartable boundary roll (a mid-roll save restores to the same continuation — the FR-SN-029 contract extended over (a')). | MUST | KD-4 |
| FR-CP-019 | *(deep)* The merged fixture-day view MUST be a pure function of the per-competition round→day mappings, enforcing one fixture per club per day and scheduling cup rounds only on days their entrants are league-free; #30's `SeasonCalendar` MUST NOT be modified; the root MUST query the merged view only when the collection has >1 competition. | MUST | KD-5 |
| FR-CP-020 | #43 fixtures/results MUST carry their `CompetitionId` (the #44 suspension-scoping surface); #43 MUST build no #44/#36/#38/#40 interface (FR-LW-031). | MUST | KD-1 |
| FR-CP-021 | #43 MUST own no money: prize money remains #40's (b') read of post-promotion standings (per-competition prize money is a #40 deep extension). | MUST | §1.2 |
| FR-CP-022 | Competition/bracket views MUST be read-only value copies (the FR-SN-033 / FR-UI-002 class); reading MUST NOT mutate #43 state. | MUST | KD-8 |
| FR-CP-023 | Every id/round/slot/count field MUST be integer; #43 MUST introduce **no** float. | MUST | §1.5 |
| FR-CP-024 | *(deep)* A drawn season MUST be two-run deterministic from one world seed; a draw MUST be stable across call orders and save→restore; two competitions' draws MUST be mutually independent (distinct `entityId`). | MUST | KD-2 |
| FR-CP-025 | Genesis-vs-load: minimal genesis is the instance-0 binding only; a load MUST reconstruct the registry/brackets/membership from the sub-blob and MUST NOT re-seed or re-draw. | MUST | KD-6 |
| FR-CP-026 | *(deep)* A knockout pairing whose fixture resolves **level** MUST determine its winner via a **keyed tie-break draw**: `ShootoutTiebreak` purpose, ordinal over `(seasonNumber, roundIndex, pairingIndex, ShootoutTiebreak)`, winner = the pairing entrant at `draw mod 2` — deterministic, cursor-free, stable across call orders and save/restore. A non-level result MUST make no tie-break draw. Extra time / replays / two-legged aggregation are §7 extensions that **replace** this rule as a reviewed change, never silently. | MUST | KD-2/KD-3 |
| FR-CP-027 | *(deep)* A `Knockout` instance's entrant count — and a `GroupThenKnockout` instance's group count (its knockout-stage entrant count) — MUST be a **power of two ≥ 2**, validated **fail-loud at genesis/config time** (F2); byes/preliminary rounds are a §7 extension. The §3.3 halving/pairing gates presume this validated shape. | MUST | KD-3/F2 |

## 2.2 Data structures

```csharp
public enum CompetitionFormat : byte { RoundRobin = 0, Knockout = 1, GroupThenKnockout = 2 }  // ordinal-stable (FR-CP-001)

// KD-1 — a competition instance. Instance 0 is a BINDING ROW (no #30 object — FR-CP-002).
public sealed class Competition
{
    /* int CompetitionId;                    // genesis-assigned, deterministic; 0 = the #30 league (FR-CP-004)
       CompetitionFormat Format;
       int[] EntrantClubIds;                 // canonical ascending (FR-CP-005); EMPTY for instance 0 (lives in #30)
       RoundRobin (non-league): Fixture[] + LeagueTable   — the #30 types, per instance (FR-CP-006)
       Knockout:  BracketState               — persisted rounds (FR-CP-010)
       GroupThenKnockout: group RoundRobin instances + BracketState */
}

// KD-3 (deep) — the persisted bracket. Canonical layout per §3.3.
public sealed class BracketState
{ /* per round: int[] entrants (the DRAWN pairing order — drawn[0]v[1], …); int[] winners; */ }

// The registry (KD-1/KD-6). Serialized ascending CompetitionId; instance 0 = binding only.
public sealed class CompetitionSet { /* Competition[] (canonical order); division chain (deep, KD-4) */ }

// KD-2 (deep) — draw purposes; APPEND-only, never reorder.
public enum CompetitionDrawPurpose : byte { Pairing = 0, GroupAssign = 1, ShootoutTiebreak = 2 /* deep may APPEND */ }
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | An operation naming an unknown `CompetitionId`, or an entrant `ClubId` outside the #27 club universe | **Fail loud** — identity validity is a caller-contract bug (the #31 F6 class). |
| **F2** | *(deep)* A draw requested on a non-knockout instance / before the prior round fully resolves; a transform invoked with mismatched division tables (a club in neither/both divisions); a `Knockout`/`GroupThenKnockout` instance configured with a non-power-of-two entrant/group count (FR-CP-027); merged-view slotting finding **no legal day** for a cup round under the `[GT]` spacing (a config-coherence error, at season-scheduling time) | **Fail loud** — command/sequencing/config-contract bugs. |
| **F3** | Competition sub-blob: bad `COMPETITION_SAVE_FORMAT_VERSION` / out-of-bounds length prefix / trailing bytes | **Fail loud** — the `SeasonSaveCodec` posture (FR-CP-013). |
| **F4** | Decoded bracket/registry incoherence: winner ∉ its pairing; non-halving round counts; non-ascending `CompetitionId`s / entrant `ClubId`s; a duplicate entrant | **Fail loud** — corrupt state never repaired silently (FR-CP-011, the #32 F4 / #22 strict-order class). |
| **F5** | *(deep)* A draw-ordinal input outside its fixed radix bound (`roundIndex`/`slotIndex`/`purpose`) | **Fail loud** — the §3.2 bound guards (the #41/#32 keyed-ordinal discipline). |
| **F6** | `default(Competition)`-shaped state reaching a consuming seam — an **empty entrant set on a non-league instance** is the discriminator (`Format = RoundRobin(0)` and `CompetitionId = 0` are both legitimate values) | **Fail loud** — the zero-value-trap discipline (the #41/#34/#32 F-class); instance 0 is exempt (its entrant set legitimately lives in #30). |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §2 (FR-CP-001..025, data structures, F1..F6), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M): FR-CP-006 pins the per-instance seed as a **pure draw-free derivation** (`DeriveInstanceSeed(worldSeed, competitionId, seasonNumber)`, distinct instances never share a fixture sequence) — §3.1 introduced it with no FR/test coverage, letting an implementer draw it from a stream and break the draw-free minimal claim. |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3: **M-1** — new FR-CP-026 pins the **keyed knockout tie-break** (`ShootoutTiebreak` purpose, winner = entrant at `draw mod 2` on a level result) — a drawn cup match had NO specified winner while F4 required one, an undefined behaviour on a common input. **M-2** — new FR-CP-027 pins the **power-of-two entrant/group-count config gate** (fail-loud at genesis) — the §3.3 halving/pairing gates silently assumed 2^k; byes deferred to §7.2. **L** — F2 gains the non-power-of-two config and merged-view slotting-infeasibility conditions. `ShootoutTiebreak = 2` appended to `CompetitionDrawPurpose`. |
#endregion
