# National Teams & International Management #36 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #36 has no `[EST]` constants, so that region does not appear. `[GT]` values are
**illustrative pending the T3 balance pass** — with one exception flagged in A.4, where a `[GT]` edit has a
**save-visible** effect.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `NATIONAL_TEAM_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The sub-blob's own version gate (KD-6). Independent of `SEASON_SAVE_FORMAT_VERSION`. |
| `NATION_TEAM_ID_BASE` | `100000` | `[FIXED]` | The floor of the **disjoint** national-team id range (FR-NT-026). Chosen above any plausible `ClubId` so the root composite's routing is a single comparison and can never mis-route. **`[FIXED]`, not `[GT]`:** lowering it into `ClubId` space would make a national team resolve some club's squad — F2 guards both sides of the seam for that reason. |
| `NT_NOT_ADVANCED_SENTINEL` | `uint.MaxValue` | `[FIXED]` | The unadvanced window cursor. **Not `0`** — day `0` is a legal world day, and a `0` sentinel silently no-ops a day-0 advance instead of failing loud (#33 FR-HS-008). |
| `NT_NATION_SALT` | `0x4E6174696F6E7300` | `[FIXED]` | Domain-separates `Derive`'s mix from every other keyed derivation over the same `worldSeed`. Changing it **re-nationalises every unpinned player in every existing career** — a deliberate, save-visible act, never a tidy-up. |
| `NT_SQUAD_SIZE` | `23` | `[FIXED]` | Call-ups per national team per window. `[FIXED]` because it bounds the serialized `CallUp` block; a designer-facing squad-size dial would be a deep-tier addition with its own version consideration. |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `NATION_COUNT` | `Enum.GetValues(typeof(NationId)).Length − 1` | `[DERIVED]` | Excludes `None`. Derived from the enum, never a literal — two surfaces carrying private copies of a member count is the `POSITION_COUNT` parallel-surface defect. |
| `NT_WEIGHT_TOTAL` | `Σ NationCatalogue[i].Weight` | `[DERIVED]` | The modulus in `Derive` (§3.1). **Never set independently:** if it exceeds the true sum, `r` can fall past every band and reach the terminating `throw`; if it is smaller, the tail nations become unreachable. **T-NT-U-004 locks it** for exactly that reason. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `PlayerId`, `PlayerRecord`, `PlayerAttributes`, `Squad` | #27 | Read-only. **Nothing added, nothing changed** — the whole of FR-NT-001/002. |
| `PlayerDatabaseConstants.FIELDS_PER_PLAYER` | #27 | Named here **only** to record that #36 does not change it. |
| `SeasonCalendar` | #30 | The window is **derived** from it; #36 never mutates it (FR-NT-015). |
| `ISquadProvider` | **match-engine** | **Never implemented and never named** (FR-NT-004). Listed so its absence is deliberate rather than accidental. |
| `_RESERVED_0x28_`, `SubsystemOrdinals.NationalTeams` (90) | #16 §3.4 | `[CROSS-PENDING]` — **already present and already correct** for a draw-free spec. Stays reserved, possibly permanently (KD-8). |

### A.4 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `NationCatalogue` weights | per Appendix C | **The one `[GT]` set whose edits are save-visible** (FR-NT-014): changing a weight changes `NationOf` for **every unpinned player in every existing career**. It is a balance parameter that behaves like a schema change, and §7.5 R-1 records that #27's golden-vector discipline is the right model if it ever needs pinning. |
| `NT_MAX_CALLUPS_PER_CLUB` | `3` | Bounds **#36's own** contribution to any one club's reduction (FR-NT-018), so a single club is never gutted by #36 alone. It does **not** bound the *composition* with #44's filter — that is the seam's shared obligation (FR-NT-019 / ERR-030-016). |
| `NT_WINDOWS_PER_SEASON` | `4` | How many windows `DeriveWindows` produces from `SeasonCalendar`. |
| `NT_WINDOW_LENGTH_DAYS` | `10` | Each window's span. Together with the count, this is the whole of #36's calendar footprint — and neither value is written back to #30. |
| `NT_BUDGET_NATION_OF_US` | `1` | §6.3 ceiling for one `NationOf` call. A **ceiling, not a measurement** — no certified number exists for #36. **The one to measure first**, since it is the only figure multiplied by pool size. |
| `NT_BUDGET_FILTER_US` | `20` | §6.3 ceiling for one `FilterAvailable` call. Same caveat. |
| `NT_BUDGET_SELECT_MS` | `5` | §6.3 ceiling for one `SelectCallUps` over the managed pool. **In milliseconds deliberately** — a few-times-a-season operation should not carry a loop-step budget. Same caveat. |

**Where a nationality *field* is not.** No per-player nationality constant, field, or default appears
anywhere in this catalogue, and that is FR-NT-001 rather than an omission: an unpinned player's
nationality is a **function**, not a stored value, and costs **zero bytes** (§6.4).

## Appendix B — Save sub-blob layout (KD-6)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** — the
outer codec sees a length-prefixed byte block and never parses it (FR-NT-031).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `NATIONAL_TEAM_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below it is interpreted (F9). |
| 2 | `CurrentWindowIndex` | `i32` | From `WindowCursor`. |
| 3 | `LastAdvancedWorldDay` | `u32` | The F5 guard's state. `NT_NOT_ADVANCED_SENTINEL` round-trips as itself. |
| 4 | `CallUpCount` | `i32` | Length prefix — read through the overflow-safe bound compared against `total − offset`, never `offset + need` (F9). |
| 5 | per call-up × count | — | `NationTeamId` (`i32`); `PlayerId` (`i32`); `CalledWorldDay` (`u32`). |
| 6 | `NationPinCount` | `i32` | Length prefix, same bound treatment. |
| 7 | per pin × count | — | `PlayerId` (`i32`); `NationId` (`i32`). |
| 8 | `IntlMinutesCount` | `i32` | Length prefix, same bound treatment. **Zero at the minimal tier.** |
| 9 | per minutes entry × count | — | `PlayerId` (`i32`); `MinutesTotal` (`i32`). |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F9). |

Call-ups are written in ascending `(NationTeamId, PlayerId)`, pins and minutes in ascending `PlayerId` —
so the blob is a function of **state**, never of insertion or iteration order (FR-NT-024). An empty store
is a version header, the cursor, and **three zero counts**.

**Row 7 is the one that must not be dropped.** Without the `NationPin` table a transferred player's
nationality **reverts to the derivation of his new id on the next load** — re-introducing, in the save
layer, the exact defect the pin exists to prevent (FR-NT-032). It is small (8 bytes per transfer, nothing
per untransferred player), and it is the difference between the KD-1 design working and appearing to work.

**Decode validates, it does not trust** (FR-NT-033): every `NationId` must be defined and not `None`
(F1); every `NationTeamId` must be `≥ NATION_TEAM_ID_BASE` (F2); entries must be canonically ordered and
duplicate-free (F8); and an `IntlMinutes` entry of `0` must **throw** — FR-NT-023's canonical-drop rule
makes it impossible to produce, so one in a blob means corruption.

**Deliberately absent — four things, each for its own reason:**

1. **Any RNG cursor or stream state.** #36 is draw-free at every tier it owns (KD-8), so there is nothing
   to persist. The tournament draws are #43's, and they live in #43's blob.
2. **Any national squad roster.** A national squad is a **selection view** over #27's pool (FR-NT-022):
   only the `PlayerId` list is stored, never copies of the records. **This is the point of temptation** —
   caching the resolved `PlayerRecord`s alongside the selection would look like an obvious optimisation
   and would create a second truth about a player that goes stale on the next progression tick.
3. **Any per-player nationality for an unpinned player.** Derived (KD-1). Storing it "for speed" is the
   `PlayerRecord`-field cost re-introduced through the save layer.
4. **Any tournament or bracket state.** That is **#43's** sub-blob. #36 stores who was called up, never
   what competition they played in.

**APPEND-only** (FR-NT-034). New fields go at the **end** behind a version bump. Appending a `NationId`
member is **not** a layout change and needs no bump — but **inserting or reordering one is a silent
catastrophe with no gate to catch it**, because the ordinal keys `Derive`'s inverse-transform walk
(§3.8(d)).

## Appendix C — The nation catalogue and the id ranges

### C.1 `NationId` — the catalogue

| Member | Ordinal | Weight (`[GT]`) | Notes |
|---|---|---|---|
| `None` | `0` | — | The F1 gate value; **never a nationality**, never stored, never returned. |
| *(catalogue members)* | `1…NATION_COUNT` | per the `[GT]` weighting | Ordinals are consumed by `Derive`'s inverse-transform walk in **ordinal order** (§3.1). |

**The roster's membership and weights are `[GT]` and are authored for balance**, not tabulated here as
fixed values — a league should be able to be predominantly one nation with a realistic minority spread,
and that is a tuning decision. What **is** fixed is the discipline around them:

- **APPEND-only, never reordered** (FR-NT-009). Inserting a member shifts every subsequent acceptance
  band and **re-nationalises the entire world**, in every existing career, **with no version gate to
  catch it** — the save loads cleanly and every player is from somewhere else. This is the same class as
  #46's `ItemKind` and #35's `MediaIntent` contracts, and it is asserted by T-NT-U-007.
- **`NT_WEIGHT_TOTAL` is `[DERIVED]` from the weights** (A.2), never set beside them.
- **Changing a weight is save-visible** (A.4 / FR-NT-014), which is why it is called out at the
  declaration rather than only in the risk list.

**Not tabulated: real-world nationality distributions.** The weights are in-game tuning values, not a
demographic claim, and giving them an external citation would lend them an authority they should not
carry (§8.5). §5.2's distribution test asserts *shape against the weights*, never against any external
figure.

### C.2 Id ranges

| Range | Owner | Notes |
|---|---|---|
| `[0, NATION_TEAM_ID_BASE)` | **`ClubId`** — #27 / league bootstrap | Re-keyed on transfer at the **player** level; club ids themselves are stable (#43 FR-CP-016). |
| `[NATION_TEAM_ID_BASE, ∞)` | **`NationTeamId`** — #36 | **Never re-keyed**, so #43's *"`ClubId`s never re-key"* invariant holds trivially for them. |

**The disjointness is what makes the root composite total and safe** (§4.4): every id is either in the
national range or below it, the routing is one comparison, and there is no id that both providers could
claim. F2 enforces the boundary **inside #36 as well as at the router**, so the guard does not depend on
the composite being written correctly.

**A national team is an entrant id, not a club.** It has a squad (a view over call-ups) and it enters #43
competitions, but it has no finances, no board, no facilities, no staff, and no roster of its own —
none of the specs keyed by `ClubId` should ever see one. That is stated here because the disjoint range
makes it *possible* to hand a `NationTeamId` to a `ClubId` consumer, and F2 is the only thing that
catches it.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed incl. the id-base and salt rationales, A.2 Derived with the `NT_WEIGHT_TOTAL` failure modes in both directions, A.3 Cross, A.4 GT with the save-visible catalogue caveat; B save layout with row 7 called out as the one that must not be dropped and the four deliberately-absent items; C.1 the catalogue's discipline and C.2 the id ranges). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the three `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline (the #45 PASS-1 M-2 defect, now seen for the fourth time in this wave) — added to A.4. **L:** A.1 gained the reason `NATION_TEAM_ID_BASE` is `[FIXED]` rather than `[GT]` (lowering it into `ClubId` space makes a national team resolve some club's squad) and the `NT_NATION_SALT` save-visibility note; A.2 spelled out **both** failure directions for a hand-set `NT_WEIGHT_TOTAL` (an over-large total reaches the terminating `throw`; an under-large one makes the tail nations unreachable); B gained the decode-validates paragraph and the resolved-records *point of temptation*; C.2 gained the closing note that a `NationTeamId` must never reach a `ClubId` consumer, since the disjoint range makes that mechanically possible and F2 is the only guard. |
#endregion
