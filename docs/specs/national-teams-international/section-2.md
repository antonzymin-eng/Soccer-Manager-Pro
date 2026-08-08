# National Teams & International Management #36 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** August 8, 2026 (v0.3 — ERR-030-029 back-prop: F7's shared empty-squad obligation is SETTLED at #30 §3.4; pointer added, contract unchanged)
**Last Updated (prior):** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.3
**Status:** APPROVED

---

## 2.1 Functional requirements

**Ownership & the #27 boundary**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NT-001 | #36 MUST NOT add a field to `PlayerRecord`, MUST NOT change `RosterGenerator`'s draw order or count, and MUST NOT change `PlayerDatabaseConstants.FIELDS_PER_PLAYER`. **#27 is untouched at every level.** | MUST | KD-1 |
| FR-NT-002 | #36 MUST NOT cause any change to the `LeagueBootstrapGoldenVectorTests` digest or to any `RosterGenerator` digest. **No existing career may be disturbed by #36's landing.** | MUST | KD-1 |
| FR-NT-003 | #36's assembly MUST reference **only** `TacticalDirector.PlayerDatabase` (#27), at every tier. It MUST NOT reference #30, #43, #44, #29, #41, `SeasonSave`, or `MatchEngine`. | MUST | KD-3 |
| FR-NT-004 | #36 MUST NOT implement `ISquadProvider`. That type is declared in `src/match-engine/`, and implementing it would force a `MatchEngine` reference for one signature (FR-NT-003). | MUST | KD-3 |
| FR-NT-005 | All #36 state and formulas MUST be **integer**. No float MUST appear at any tier. | MUST | KD-6 |
| FR-NT-006 | #36 MUST be the **sole writer** of its own state (call-ups, the window cursor, minutes, pins). | MUST | KD-6 |

**Nationality (KD-1)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NT-007 | `NationOf(playerId)` MUST be `NationPins[playerId] ?? Derive(worldSeed, playerId)` — a **pin-then-derive** lookup. | MUST | KD-1 |
| FR-NT-008 | `Derive` MUST be a **pure keyed function** of `(worldSeed, playerId)` against the `NationCatalogue` — **no RNG stream, no draw, no cursor**. | MUST | KD-1/KD-8 |
| FR-NT-009 | `NationCatalogue` MUST carry an **ORDINAL STABILITY — APPEND-only** contract. A reorder changes `NationOf` for **every** player in **every** existing career, with no version gate to catch it. | MUST | KD-1 |
| FR-NT-010 | A `NationPin` MUST be written **only** on a `PlayerId` re-key (via #31's FR-TX-022 roster-move hook) or by #47 authoring. It MUST NOT be written for an untransferred player. | MUST | KD-1 |
| FR-NT-011 | On a re-key, the **pre-transfer** nation MUST be resolved and pinned to the **new** id, before the old id becomes unresolvable. | MUST | KD-1 |
| FR-NT-012 | A pin whose value **equals** its derivation MUST still be stored. The pin's job is to survive a key change the derivation cannot; "optimising away" a redundant-looking pin re-opens the transfer defect. | MUST | KD-6 |
| FR-NT-013 | A `NationPin` MUST be **dropped on retirement**, in lockstep with #28's boundary churn — an unpruned pin table outlives its pool and grows monotonically across a career. | MUST | KD-6 |
| FR-NT-014 | The nationality **distribution** MUST be a `[GT]` weighting over the catalogue, and MUST be documented as a **save-visible** balance parameter: changing it changes `NationOf` for every existing player in every existing career. | MUST | KD-1 |

**The window and the availability filter (KD-2)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NT-015 | The international window MUST be a **read-only derivation** over #30's `SeasonCalendar`. #36 MUST NOT mutate the calendar, insert a day, or reorder a fixture (the #31 FR-TX-019 precedent). | MUST | KD-2 |
| FR-NT-016 | Withdrawal MUST be a **value-copy squad reduction** consumed at #30's existing FR-SN-013 resolve→**filter**→configure seam. #36 MUST NOT introduce a new #30 seam. | MUST | KD-2 |
| FR-NT-017 | #36's filter MUST be a **pure removal** — it MUST NOT add or substitute a player. This is what makes composition with #44's filter **order-independent**, and a future non-removal filter would require an explicit order (ERR-030-016). | MUST | KD-2 |
| FR-NT-018 | #36's own contribution MUST be bounded by `NT_MAX_CALLUPS_PER_CLUB`, so a single club is never gutted by #36 alone. | MUST | KD-2 |
| FR-NT-019 | #36 MUST NOT define the **empty-squad floor** policy. It is a **shared** obligation of #44/#36/#30 at the seam, recorded by ERR-030-016; #36 bounds its own contribution and no more. | MUST | KD-2 |
| FR-NT-020 | Window advance MUST be a `worldDay` comparison off `LastAdvancedWorldDay`, whose unadvanced sentinel MUST be `uint.MaxValue`, **not** `0`. Re-advancing the same day MUST be a **no-op**; a day **gap** MUST **fail loud**. | MUST | KD-2 |

**Call-up selection (KD-5)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NT-021 | Selection MUST be **draw-free**: a deterministic ranking (mean attributes, `PlayerId` tie-break) over the eligible pool, capped per club — the `LineupSelector` model. | MUST | KD-5/KD-8 |
| FR-NT-022 | Only the **selection** (a list of `PlayerId`s per national team) MUST be stored. #36 MUST NOT store copies of `PlayerRecord`s — a national squad is a **view** over #27's pool. | MUST | KD-6 |
| FR-NT-023 | A `CallUp` for a player who **retires or leaves the pool** MUST be **dropped**; on a #31 **re-key** it MUST **migrate** — following #44's ban rule rather than #32's drop rule, because a call-up is a live selection of a person, not a stale fact about a squad slot. | MUST | KD-6 |
| FR-NT-024 | Entries MUST be **canonically ordered** (`NationTeamId` then `PlayerId`, ascending, no duplicates), with a **fail-loud decode gate**, so two equivalent states cannot serialize differently. | MUST | KD-6 |

**Tournaments and identities (KD-3)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NT-025 | #36 MUST define **no** fixture generator, table, bracket, or draw. An international competition MUST be a **#43 instance**. | MUST | KD-3 |
| FR-NT-026 | National-team ids MUST come from a **disjoint reserved range** at or above `NATION_TEAM_ID_BASE`, above any `ClubId`, and MUST never be re-keyed. | MUST | KD-3 |
| FR-NT-027 | Squad resolution MUST be exposed as `TryResolveNationSquad(nationTeamId, out Squad) → bool` over the `PlayerDatabase.Squad` type. The **root** composes it into #30's single `ISquadProvider` (FR-NT-004). | MUST | KD-3 |
| FR-NT-028 | International **minutes** MUST reach #29/#41 as **routed committed integers**, never by #36 writing their state or by them referencing #36. The routing MUST NOT be built before minutes exist (FR-LW-031). | MUST | KD-4 |

**Determinism & persistence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NT-029 | #36 MUST be **draw-free at every tier it owns** — no `RegisterStream`, no domain-tag promotion. `_RESERVED_0x28_` / `SubsystemOrdinals 90` MUST stay **RESERVED**, possibly permanently. | MUST | KD-8 |
| FR-NT-030 | If a #36-owned stochastic surface is ever introduced, the `0x28` promotion MUST happen **at its first draw site**, as an explicit decision on the record. | MUST | KD-8 |
| FR-NT-031 | `NATIONAL_TEAM_SAVE_FORMAT_VERSION` [FIXED] = 1; #36's state MUST land as an **opaque, independently version-gated** sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump. | MUST | KD-6 |
| FR-NT-032 | The sub-blob MUST carry the **`NationPin` table**. Omitting it makes a transferred player's nationality revert to the derivation of his new id on the next load — re-introducing, in the save layer, the exact defect the pin prevents. | MUST | KD-6 |
| FR-NT-033 | Every field MUST round-trip **field-identical**; **serialize, don't regenerate**. Restore MUST **fail loud** on version mismatch, an out-of-bounds length prefix (overflow-safe bound against `total − offset`), trailing bytes, a non-canonical or duplicated entry, an undefined `NationId`, or a `NationTeamId` outside the reserved range. | MUST | KD-6 |
| FR-NT-034 | The layout MUST be **APPEND-only**. | MUST | KD-6 |

## 2.2 Data structures

```csharp
// APPEND-only, NEVER reordered (FR-NT-009): the ordinal keys Derive(), so a reorder changes
// NationOf for EVERY player in EVERY existing career, with no version gate to catch it.
public enum NationId : int { None = 0, /* catalogue members; see Appendix C */ }

// The SELECTION only -- never records (FR-NT-022). A national squad is a view over #27's pool.
public struct CallUp
{
    public int  NationTeamId;      // >= NATION_TEAM_ID_BASE, disjoint from ClubId (FR-NT-026)
    public int  PlayerId;
    public uint CalledWorldDay;
}

public struct WindowCursor
{
    public int  CurrentWindowIndex;
    public uint LastAdvancedWorldDay;   // sentinel NT_NOT_ADVANCED_SENTINEL = uint.MaxValue, NOT 0
}

// Deep tier; empty at minimal (no international match is played -- KD-5). A ZERO entry is never
// recorded (the #44 canonical-drop rule).
public struct IntlMinutes { public int PlayerId; public int MinutesTotal; }

// KD-1: written ONLY on a #31 re-key (FR-TX-022 hook) or by #47 authoring. ABSENT for every
// untransferred player -- which is the overwhelming majority at every moment of every career.
// A pin equal to its derivation is STILL stored (FR-NT-012).
public struct NationPin { public int PlayerId; public NationId Nation; }
```

**Types #36 consumes but does not declare:**

| Type | Owner | #36's use |
|---|---|---|
| `PlayerId`, `PlayerRecord`, `PlayerAttributes`, `Squad` | #27 | read-only; `Squad` is the return type of `TryResolveNationSquad` |
| `SeasonCalendar` | #30 | the window is **derived** from it as a value; #36 names no #30 type in its own state |
| `ISquadProvider` | **match-engine** | **never implemented and never named** (FR-NT-004) — the root composes |
| `CompetitionFormat`, the #43 registry | #43 | the **root** registers instances; #36 supplies ids and squads |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | An undefined `NationId`, or `NationId.None`, reaching a consuming seam or appearing on decode. | **Fail loud**. `None` is a gate value, never a nationality. |
| **F2** | A `NationTeamId` **outside** the reserved range (`< NATION_TEAM_ID_BASE`) at any seam or on decode. | **Fail loud** — it would collide with a `ClubId` and route to the wrong provider at the composite (FR-NT-026). |
| **F3** | A `NationPin` written for a player who has **not** been re-keyed or authored. | **Fail loud** at the write seam. The pin table's bound is *transfer volume*; a stray write makes it grow with pool size and quietly re-introduces the stored-field cost KD-1 avoids. |
| **F4** | A re-key hook invoked **after** the old `PlayerId` has become unresolvable, so the pre-transfer nation cannot be read. | **Fail loud** (FR-NT-011). Pinning the *post*-transfer derivation would silently record the wrong nationality — the exact defect the pin exists to prevent, committed by the mechanism meant to prevent it. |
| **F5** | A `worldDay` gap past `LastAdvancedWorldDay`, or a re-advance of the same day. | **Fail loud** / **no-op**, respectively (FR-NT-020, the #33 F6 guard). |
| **F6** | A call-up selection exceeding `NT_MAX_CALLUPS_PER_CLUB` for any club. | **Fail loud** at selection — a bounded contribution is what keeps #36's share of the empty-squad risk defined (FR-NT-018). |
| **F7** | A squad reduced below a fieldable eleven **by the composition** of #36's and #44's filters. | **Not #36's failure mode.** It belongs to the seam, as a shared #44/#36/#30 obligation (FR-NT-019 / ERR-030-016). #36 bounds its own contribution and no more; inventing a private policy here is what would make the two filters disagree. **SETTLED (ERR-030-029, balance-pass AR pass 12):** #30 §3.4 now owns the rule — press the least-injured back in until the engine's own selector can field the formation; terminal case fails loud (#30 §2.3 F9). #36 inherits it unchanged. |
| **F8** | A non-canonical, duplicated, or out-of-order `CallUp` entry on decode. | **Fail loud** (FR-NT-024) — two equivalent states must not serialize differently. |
| **F9** | Bad `NATIONAL_TEAM_SAVE_FORMAT_VERSION`, an out-of-bounds length prefix, or trailing bytes on restore. | **Fail loud** — version gate read **first**; the bound compared against `total − offset`, never `offset + need`, which can wrap negative on a crafted near-`int.MaxValue` prefix. |

**Deliberately not a failure mode: an absent `NationPin`.** It is the **normal** state for the
overwhelming majority of players and simply falls through to the derivation (FR-NT-007). Stated because
the pin table's *presence* is exceptional and a reader could mistake absence for an error.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-NT-001..034, data structures, F1..F9) from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **F4** — the re-key hook must read the **pre-transfer** nation, and a hook invoked after the old id is unresolvable would pin the *post*-transfer derivation, silently recording the wrong nationality **via the very mechanism meant to prevent it**; FR-NT-011 states the ordering and F4 makes it loud. **M:** added **FR-NT-012 / F3** — a pin equal to its derivation must **still** be stored (the obvious "skip the redundant pin" optimisation re-opens the transfer defect), and conversely a pin written for an untransferred player must fail loud, since the table's whole cost argument rests on being bounded by transfer volume rather than pool size. **L:** added **FR-NT-017** (the filter must be a pure removal — the property that makes composition order-free, previously stated only as an observation), **FR-NT-030** (the `0x28` promotion happens at a first draw site, on the record), and the *"an absent pin is not a failure"* note; `NationId`, `WindowCursor` and `IntlMinutes` written out. |
| 0.3 | 2026-08-08 | — | **ERR-030-029 back-prop (balance-pass AR pass 12, M4)**: F7's "whatever the seam settles on" now has an answer — #30 §3.4's depleted-squad back-fill rule (least-injured first, selector-probed, fail-loud terminal case, #30 §2.3 F9). Pointer only; #36's own contract unchanged. |
#endregion
