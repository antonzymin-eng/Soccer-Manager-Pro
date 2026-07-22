# Squad / Player Data Layer Specification #27 — Section 6: Performance Budget

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Status:** APPROVED

---

## 6.1 Cost placement — club-setup time, never per-tick

Roster generation and text-file parsing run **once at club setup**, never inside the 10 Hz tactical or
60 Hz physics loops (KD-6, FR-SQ-021). The Code Standards #20 zero-allocation game-loop rule governs
the tick pipeline and does **not** apply here — exactly as it does not for `TeamTacticFileLoader` /
`PlayerTacticFileLoader`. `Squad` is a plain `class` holding a `PlayerRecord[]`; `RosterGenerator`
allocates the `PlayerRecord[]` + per-player attribute arrays freely; `SquadFileLoader` allocates strings
and per-section state. None of this touches a per-tick budget, so no budget line item changes.

## 6.2 Generation cost model

Per generated club of `count` players (`count ≤ CLUB_SQUAD_SIZE = 25`):

| Item | Cost |
|---|---|
| RNG draws | `count × FIELDS_PER_PLAYER` = `count × 36` `DrawReserved` calls (one `Reserve`/`CloseReservation` per player) |
| Attribute projection | `count × ATTRIBUTE_COUNT` (= `count × 31`) clamp-and-bias integer ops |
| Allocation | one `PlayerRecord[count]` + `count` × (one `int[31]` attribute array + two name-string references) |

A full 25-player club is therefore `25 × 36 = 900` deterministic draws and a few thousand integer ops —
trivially inside any setup-time budget, incurred once. `SquadFileLoader.Parse` is one linear pass over the
text (O(lines)) with string allocation proportional to input size.

## 6.3 No per-tick budget applies

Because nothing here is a hot path, there is no FR-PO-052 per-tick regression gate for this assembly and
no `[HotPathAllocExempt]` surface. The one performance obligation is that a consumer never calls
`RosterGenerator.Generate` or `SquadFileLoader.Parse` from a tick — a placement contract, enforced by
keeping the assembly out of every orchestrator's per-tick path, not a measured budget.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial budget analysis: club-setup-time placement, generation cost model, no per-tick budget. |
#endregion
