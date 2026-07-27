# New-Game Setup & Database Editor #47 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 2.1 Functional requirements

**Ownership and boundaries**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-001 | #47's **data layer** MUST reference `TacticalDirector.PlayerDatabase` (#27) and **nothing else** — explicitly **not** `season-save`, which transitively pulls `MatchEngine` and `LivingWorld`, nor any sim loop, nor `TacticalDirector.Localization`. | MUST | KD-4 |
| FR-ED-002 | #47 MUST NOT modify `LeagueBootstrap`, `RosterGenerator`, or any generation path. A **generated** game's behaviour and its golden vector MUST be untouched. | MUST | KD-1 |
| FR-ED-003 | #47 MUST NOT construct a `League`. `League`'s constructor is `internal` to `season-save`; #47 produces `Club[]` / `Squad[]` **values** and the **root** calls the authored-source factory (ERR-030-018). | MUST | KD-1 |
| FR-ED-004 | #47 MUST NOT declare a parallel player, squad, or attribute model. The artifact MUST store **#27's `Squad`** outright. | MUST | §2.2 |
| FR-ED-005 | #47's data layer MUST have **no UI dependency**, so a headless authoring run is possible. | MUST | KD-4 |
| FR-ED-006 | #47 MUST NOT reference #30 or the composition root. Handoff is a **value** (KD-5). | MUST | KD-5 |

**The authored database as a source**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-007 | An authored database MUST be a **source** for `League`, never a **patch** over a generated one. Generation MUST NOT run for an authored game. | MUST | KD-1 |
| FR-ED-008 | A `League` built from authored data MUST be `ISquadProvider`-identical in shape to a generated one, so every downstream consumer is **source-agnostic**. | MUST | KD-1 |
| FR-ED-009 | Authored clubs MUST take `StrengthDelta = 0` and **no strength ramp MUST be applied**. An authored database specifies attributes directly, and a ramp would silently re-tune every authored player away from what the author typed. | MUST | KD-1 |
| FR-ED-010 | **Partial authoring** — a database authoring some clubs and generating the rest — MUST NOT be supported at any tier described here. It is neither a source nor a sparse overlay, and it would re-open the generator coupling FR-ED-007 rejects. | MUST | §7.4 R-4 |

**Persistence of an authored game**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-011 | An **authored** game MUST persist its rosters in an opaque, independently version-gated `AUTHORED_DB_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec`. | MUST | KD-1 |
| FR-ED-012 | A **generated** game MUST write **no** authored sub-blob — not an empty one — and its save frame MUST be byte-identical to pre-#47. | MUST | KD-7 |
| FR-ED-013 | An authored save MUST be **self-contained**: loadable with the editor absent, the source file absent, and on a different machine. It MUST NOT reference an external file by path or hash. | MUST | KD-1 |
| FR-ED-014 | The sub-blob MUST be **canonically ordered** — clubs by ascending `ClubId`, players by ascending `PlayerId` — with a **fail-loud non-ascending gate**, so two equivalent databases cannot serialize differently. | MUST | KD-1 |
| FR-ED-015 | Every field MUST round-trip **field-identical**; **serialize, don't regenerate**. Restore MUST **fail loud** on version mismatch, an out-of-bounds length prefix (overflow-safe bound against `total − offset`), trailing bytes, or a non-ascending / duplicated entry. The layout MUST be **APPEND-only**. | MUST | KD-1 |
| FR-ED-016 | The sub-blob MUST be **locale-independent** (FR-LC-006): authored names stored as authored, with no locale baked and no `LocalizationKey` allocated. | MUST | KD-6 |

**Validation and the writer**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-017 | **`SquadFileLoader.Parse` MUST be the single validation authority.** #47 MUST NOT implement a second validation path, and every commit MUST go through `Parse`. | MUST | KD-2 |
| FR-ED-018 | The writer's correctness condition MUST be the round-trip `Parse(Write(squad)) == squad`, **field-for-field**, for every `Squad` the parser accepts. | MUST | KD-2 |
| FR-ED-019 | An editor-side check MUST be a **UX affordance only**. A check that **disagrees** with the loader MUST be treated as a bug in the check, never as a second gate. | MUST | KD-2 |
| FR-ED-020 | The writer MUST NOT emit any construct `Parse` rejects, and MUST NOT depend on parser behaviour outside the documented grammar. | MUST | KD-2 |
| FR-ED-021 | The editor MUST bind to the **loader's types** (`Squad`, `PlayerRecord`), **never to its syntax**, so the Stage-0+1 text→binary parser swap leaves #47's contract intact. | MUST | §1.4(c) |

**Setup flow**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-022 | The setup flow MUST collect `worldSeed`, `clubCount` and `managedClubId`, and MUST add **no validation gate of its own**. | MUST | KD-3 |
| FR-ED-023 | Where a parameter is refused, #47 MUST **surface the consumer's own exception** — `LeagueBootstrap.Generate` for `clubCount`, `League.CreateSeason` for the managed club — rather than pre-checking. Every `ulong` is a valid `worldSeed` and MUST NOT be gated. | MUST | KD-3 |
| FR-ED-024 | Custom leagues and cups MUST be authored as **#43 genesis config** (FR-CP-004), never by driving a runtime #43 API. Deep tier only. | MUST | KD-3 |

**Nationality pins**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-025 | Authored nationalities MUST be entries in **#36's existing `NationPin` table**. #47 MUST NOT declare a parallel nationality store, and #36 MUST need no new surface. | MUST | §1.4(c) |
| FR-ED-026 | **Precedence:** an authored pin MUST be **overwritten** by a later transfer re-key pin. The re-key is a live event about a player who has moved; the authored value described his **starting** state. | MUST | §3.4 |

**The #38 boundary and determinism**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ED-027 | The editor **screen** MUST be a #38 screen consuming #47's data layer via `IViewModelSource<T>` and dispatching edits as commands. #38 owns navigation, layout and input. | MUST | KD-4 |
| FR-ED-028 | **No data-model logic MUST live in the presentation layer.** | MUST | KD-4 |
| FR-ED-029 | #47 MUST register **no RNG stream**, promote **no domain tag**, and take **no `SubsystemOrdinal`** — and **no `_RESERVED_` placeholder row MUST be filed** either. #16 is untouched. | MUST | KD-6 |
| FR-ED-030 | The `worldSeed` MUST be treated as an **input parameter**; #47 MUST make no draw from it. | MUST | KD-6 |
| FR-ED-031 | Authored proper nouns MUST travel through #49's seam as **`NamedSlotSet` slot values** — routed, never translated, and never allocated a `LocalizationKey`. FR-LC-001 is satisfied **by routing**. | MUST | KD-6 |
| FR-ED-032 | An authored database MUST be treated as an **input artifact**, not a live save. Migrating **saves** across versions is #50's; migrating an authored **file** across #47's own format versions is #47's, at the deep tier. | MUST | §8.4 |

## 2.2 Data structures

```csharp
// A transient handoff VALUE -- never saved (KD-5). The flag selects the root's branch; the
// AuthoredDatabase travels BESIDE the config rather than embedded, so a generated setup
// carries nothing.
public readonly struct NewGameConfig
{
    public readonly ulong WorldSeed;        // every ulong is valid -- NOT gated (FR-ED-023)
    public readonly int   ClubCount;        // gated by LeagueBootstrap.Generate
    public readonly int   ManagedClubId;    // gated by League.CreateSeason
    public readonly bool  HasAuthoredDb;
}

// The deep-tier authored artifact. Stores #27's OWN types -- a parallel model here is the
// PlayerAttributes-vs-AgentMovement.PlayerAttributes duplicate-truth defect, and it would
// diverge silently the moment #27 adds a field (FR-ED-004).
public sealed class AuthoredDatabase
{
    public AuthoredClub[] Clubs;            // ascending ClubId, no duplicates (FR-ED-014)
    public Squad[]        Squads;           // #27's type; ascending PlayerId within each
    public (int PlayerId, int NationId)[] NationPins;   // #36's table; authored entries (FR-ED-025)
}

// NOTE what is absent: no strength field. The constructed Club takes StrengthDelta = 0 and
// no ramp is applied (FR-ED-009) -- authored attributes ARE the differentiation.
public readonly struct AuthoredClub
{
    public readonly int    ClubId;
    public readonly string Name;            // a PROPER NOUN: stored as authored, no locale baked,
}                                           // no LocalizationKey allocated (FR-ED-031)
```

**Types #47 consumes but does not declare:**

| Type | Owner | #47's use |
|---|---|---|
| `Squad`, `PlayerRecord`, `PlayerAttributes` | #27 | stored **outright** in the artifact (FR-ED-004) |
| `SquadFileLoader.Parse` | #27 | **the single validation authority** (FR-ED-017) |
| `League`, `Club` | `season-save` | **never constructed by #47** (FR-ED-003) — the root calls the factory |
| `NationPin` | #36 | authored entries in **#36's** table; no parallel store (FR-ED-025) |
| `NamedSlotSet`, `ILocalizer` | #49 | used only where the **root/#38** renders; #47 stores raw names |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | Authored data that `SquadFileLoader.Parse` rejects. | **Fail loud, through the loader** (FR-ED-017). #47 adds no message of its own beyond surfacing the parser's. |
| **F2** | An editor-side check **disagreeing** with the loader — accepting what `Parse` rejects, or rejecting what it accepts. | **A bug in the check** (FR-ED-019), and a test failure (§5.3). Not a runtime path: the commit goes through `Parse` regardless, so a permissive check cannot admit bad data — it can only mislead the user. |
| **F3** | A `Write` output that `Parse` cannot read back field-identically. | **A writer defect** (FR-ED-018), caught by the round-trip lock. This is the encode/decode asymmetry class #30 T1 was bitten by. |
| **F4** | An out-of-range setup parameter (`clubCount`, `managedClubId`). | **Fail loud from the consumer** — `LeagueBootstrap.Generate` or `League.CreateSeason` — surfaced by #47, **never pre-checked** (FR-ED-023). |
| **F5** | A non-ascending or duplicated `ClubId` / `PlayerId` in the artifact, at write or on decode. | **Fail loud** (FR-ED-014). Without it, two equivalent databases serialize differently and the save stops being a function of state. |
| **F6** | Bad `AUTHORED_DB_SAVE_FORMAT_VERSION`, an out-of-bounds length prefix, or trailing bytes on restore. | **Fail loud** — version gate read **first**; the bound compared against `total − offset`, never `offset + need`, which can wrap negative on a crafted near-`int.MaxValue` prefix. |
| **F7** | An authored save whose sub-blob is **absent**. | **Fail loud.** A save flagged authored with no rosters is unrecoverable — and the alternative is worse than an error: silently falling back to generation produces the exact **silent wrong world** §1.4(a) describes, where the career loads and looks merely *wrong*. |
| **F8** | A **generated** save carrying an authored sub-blob, or an authored save carrying a `HasAuthoredDb = false` marker. | **Fail loud** on decode — the two must agree, and a mismatch means one half of the write path ran and the other did not. |

**Deliberately not a failure mode: an authored database whose clubs differ in strength only through their
attributes.** That is the **normal** case (FR-ED-009). A reviewer looking for the seeded ramp will not find
one, and its absence is the design rather than a missing step.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-ED-001..032, data structures, F1..F6) from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **F7** — nothing said what happens when an authored save's sub-blob is **missing**, and the natural fallback (regenerate) is precisely the **silent wrong world** §1.4(a) identifies as this spec's worst failure; it must fail loud. **M:** added **F8** — the `HasAuthoredDb` marker and the sub-blob's presence are two facts that must agree, and a mismatch means half the write path ran; without the gate a generated save could carry a stale authored blob and load the wrong rosters. **M:** added **FR-ED-010** making the **no-partial-authoring** rule a requirement rather than a risk-list note, since "author a few clubs, generate the rest" is the obvious feature request and it re-opens the generator coupling FR-ED-007 rejects. **L:** added FR-ED-020/021 (the writer emits nothing `Parse` rejects; the editor binds to types not syntax — the property that makes the parser swap free), FR-ED-026 (the pin-precedence rule #36 left open, as a requirement rather than prose), and the *"attribute-only differentiation is not a failure"* note; `NewGameConfig`, `AuthoredDatabase` and `AuthoredClub` written out. |
#endregion
