# Transfers, Contracts & Negotiation #31 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-3 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly **`TacticalDirector.Transfers`** (`src/transfers/`, at the T-phase). References **`#27
PlayerDatabase`** (player pool + attributes), **`#40 ClubFinances`** (the constraint + commit path), and
**`#16 DeterministicSim`** (namespace; the world-tick `DeterministicRngService` only when the deep tier
draws). It references **neither #30 nor #22** — the composition root (the season loop, which already owns
`SeasonSave`) invokes #31 and routes its commands, so the reference is one-way (`compositionRoot → #31`), the
established layer discipline. #31's downstream consumers (#32/#34/#38) reference **it**, never the reverse.

```
compositionRoot (season loop) ──► #31 Transfers ──► { #27, #40, #16 }
        │                               ▲
        └─ invokes SubmitBid / the      └── #32 (scouting), #34 (staff) reuse the KD-3 seam (deferred)
           world-tick slot / roster commit
```

Acyclic; no sim assembly references #31's consumers (FR-LW-031).

## 4.2 File layout (proposed, at T-phase)

| File | Contents |
|---|---|
| `Contract.cs` | `Contract` value type (FR-TX-015 append discipline) |
| `Offer.cs` / `NegotiationOutcome.cs` | the counterparty-generic offer/response seam (KD-3) |
| `TransferWindow.cs` | `TransferWindow` + `IsWindowOpen` / `DeriveSummerWindow` (KD-6) |
| `PlayerValuation.cs` | `ValuePlayerPermille` + `MeanAttributeRating` + `AgeCurvePermille` (KD-1) |
| `NegotiationEngine.cs` | `EvaluateOffer` (+ deep multi-day state machine) (KD-3) |
| `TransfersState.cs` | per-club `ClubTransferState` + contract store; `OnPlayerRekeyed` (KD-7) |
| `TransferCommands.cs` | `SubmitBid` etc. — the #38-UI command seams (KD-8) |
| `TransfersSaveCodec.cs` | `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-4) |
| `TransfersConstants.cs` | the Appendix A catalogue |

## 4.3 The reusable negotiation seam (KD-3)

`EvaluateOffer(in Offer, long counterpartyValuation)` and the deep in-flight negotiation state machine are
authored **generically over a caller-supplied `counterpartyValuation`**. #32 (scouting) passes a
scout-knowledge-fogged valuation; #34 (staff hiring), if it reuses per its own KD, passes a staff-valuation.
#31 builds **no** #32/#34 interface (FR-LW-031) — it publishes the seam; the consumers attach when they land.
#31's own #34-staff-influence-on-valuation enters as a `staffMult` defaulting to `1000‰` (identity) until #34
produces a non-identity value (the #21 `TacticTranslation` / #41 `MedicalModifier` routing-seam pattern).

## 4.4 Save composition (KD-4)

`TransfersSaveCodec.Encode(in TransfersState) → byte[]` produces the opaque sub-blob; the composition root
appends it to #30's `SeasonSaveCodec` frame as an additional opaque sub-blob, and the outer
`SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at T1 (**exact version TBD** — #28/#29/#33/#40/#41
each also defer an outer bump, so the number is assigned by whichever T-phase lands first, not hardcoded here).
The codec mirrors the `SeasonSaveCodec` fail-loud posture exactly: version-gate first
(`TRANSFERS_SAVE_FORMAT_VERSION`, F3), an overflow-safe `Require(offset, need, total)` bound against
`total − offset` on every length-prefixed read, and a trailing-byte guard. The block is **opaque to
`SeasonSaveCodec`** (it never parses it) and carries its own inner version gate — the world/season/match blobs
stay byte-untouched (FR-SN-020 preserved). Layout in Appendix B.

## 4.5 Interface contracts recorded for the composition root & #30

- **The composition root** (season loop) MUST: invoke #31's world-tick step at #30's new tick-order slot;
  route `SubmitBid`/transfer commands from the UI to #31; supply committed season/calendar values by copy;
  and route `RequestRosterCommit` to #30's roster owner. It MUST NOT let the UI mutate #31 state directly.
- **#30** MUST, at the T-phase: (a) add the transfers tick-order null-seam slot (ERR-030-004, at approval —
  §8); (b) build the mid-season `RequestRosterCommit` entry point + `DispatchRosterMoveHook` (KD-7, at T2 —
  ERR-030-005); (c) bump `SEASON_SAVE_FORMAT_VERSION` (exact version coordinated at T1) composing the sub-blob.
  #30 stays producer-only for #22
  (FR-SN-017 unaffected — #31 adds no #22 surface).
- **#40** is consumed read-only (`AvailableTransferBudget`) + through its one mutation path
  (`ApplyTransaction`); #31 adds nothing to #40 (FR-FN-013 already names #31 the `ApplyTransaction` caller).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §4 (assembly/reference direction, file layout, the reusable seam, save composition, root/#30/#40 interface contracts). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3 (L): outer `SEASON_SAVE_FORMAT_VERSION` no longer hardcoded "2 → 3" (coordinated at T1, exact version TBD — §4.4/§4.5); the T2 mid-season build cites ERR-030-005. |
#endregion
