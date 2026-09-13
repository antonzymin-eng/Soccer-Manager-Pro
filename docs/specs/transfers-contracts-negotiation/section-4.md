# Transfers, Contracts & Negotiation #31 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** September 12, 2026 (v0.4 — T0 landing: phase-real dependencies, consumer-owned roster port, file-layout status)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-6 fix pass; prior v0.2 AR-3, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

At **T0**, **`TacticalDirector.Transfers`** (`src/transfers/`) references **`#27 PlayerDatabase`**
(player records + canonical attributes), **`#40 ClubFinances`** (the constraint + single commit path), and the
cross-cutting **`TacticalDirector.ProjectConstants`** foundation solely for Code Standards #20-mandated
`GameplayConfig.Get*` loading of #31's `[GT]` catalogue. The ProjectConstants edge is configuration plumbing,
not a gameplay-ownership seam.

The originally approved architecture named **`#16 DeterministicSim`** for the eventual save/RNG machinery.
T0 deliberately does **not** carry that dead edge: minimal #31 is draw-free and has no codec yet. The #16
reference lands only with the first real consumer (T1 canonical save framing if required by the codec, and in
all cases no later than T3's first stochastic rival-bid draw). This is the same phase-real dependency discipline
used by #40. T0 references **neither #30 nor #22** — the composition root invokes #31 and, at T2, adapts #31's
consumer-owned roster port to #30's roster owner. #31's downstream consumers (#32/#34/#38) reference **it**,
never the reverse.

```
T0 current:  #31 Transfers ──► #27 PlayerDatabase       [player + attribute reads]
                         ├──► #40 ClubFinances          [budget query + transaction commit]
                         └──► ProjectConstants          [[GT] GameplayConfig loading]

T2 future: compositionRoot ──► #31 Transfers            [commands / tick invocation]
                              └─ adapts #31 ITransferRosterPort to #30 roster owner
T3 future: #32/#34/#38 ─────► #31 Transfers            [downstream consumers]
```

Acyclic; no sim assembly references #31's consumers (FR-LW-031).

## 4.2 File layout (lands incrementally by T-phase)

| File | Contents |
|---|---|
| `Contract.cs` | `Contract` value type (T0, live; FR-TX-015 append discipline) |
| `Offer.cs` / `NegotiationOutcome.cs` | counterparty-generic offer/response values (T0, live; KD-3) |
| `TransferWindow.cs` | `TransferWindow` + inclusive `IsWindowOpen` (T0, live); `DeriveSummerWindow` remains T2 composition work (KD-6) |
| `ClubTransferState.cs` | season-scoped window + `CommittedSpendThisWindow` state (T0, live) |
| `PlayerValuation.cs` | `ValuePlayerPermille` + `MeanAttributeRating` + `AgeCurvePermille` (T0, live; KD-1) |
| `NegotiationEngine.cs` | `EvaluateOffer` (T0, live); deep multi-day state machine remains T3 (KD-3) |
| `TransfersState.cs` | managed contract store + club state + `OnPlayerRekeyed` semantics (T0, live; KD-7) |
| `ITransferRosterPort.cs` | consumer-owned read/preflight/commit seam for the already-specified #30 T2 roster producer (T0, live) |
| `TransferCommands.cs` | atomic `SubmitBid` command surface (T0, live; KD-8) |
| `TransfersSaveCodec.cs` | `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob encode/decode (T1, deferred) |
| `TransfersConstants.cs` | Appendix A catalogue using `GameplayConfig` for `[GT]` values (T0, live) |

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

T0 intentionally ships **no codec and no #30 save composition**. `TransfersState.ContractsSnapshot()` returns
contracts in canonical `PlayerId` order so T1 can encode deterministically without changing T0 command
semantics.

## 4.5 Interface contracts recorded for the composition root & #30

- **T0 roster consumer port (live):** #31 owns `ITransferRosterPort` because #31 is the specified consumer and
  #30/composition is the already-specified T2 producer. `TryGetPlayer` is read-only; `TryPreviewRosterCommit`
  validates ownership/capacity and returns the exact destination `PlayerId` without mutation;
  `RequestRosterCommit` is required to be infallible after a successful preview and return that same id. This
  preserves `SubmitBid`'s validate-all-first atomic contract without giving #31 a #30 assembly reference or
  direct #27 roster mutation. T0 tests use a fake producer; the production adapter is T2 work.
- **The composition root** (season loop) MUST: invoke #31's world-tick step at #30's new tick-order slot;
  route `SubmitBid`/transfer commands from the UI to #31; supply committed season/calendar values by copy;
  and adapt `ITransferRosterPort` to #30's roster owner. It MUST NOT let the UI mutate #31 state directly. It
  MUST call `SeedInitialContracts` (§3.8) **only at new-career genesis** and reconstruct `TransfersState` from
  the sub-blob on **load** — never both (re-seeding a loaded career would destroy restored contracts).
- **#30** MUST, at the T-phase: (a) add the transfers tick-order null-seam slot (ERR-030-004, at approval —
  §8); (b) build the mid-season roster commit + `DispatchRosterMoveHook` capability behind the port (KD-7, at
  T2 — ERR-030-005); (c) bump `SEASON_SAVE_FORMAT_VERSION` (exact version coordinated at T1) composing the
  sub-blob. #30 stays producer-only for #22 (FR-SN-017 unaffected — #31 adds no #22 surface).
- **#40** is consumed read-only (`AvailableTransferBudget`) + through its one mutation path
  (`ApplyTransaction`); #31 adds nothing to #40 (FR-FN-013 already names #31 the `ApplyTransaction` caller).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §4 (assembly/reference direction, file layout, the reusable seam, save composition, root/#30/#40 interface contracts). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3 (L): outer `SEASON_SAVE_FORMAT_VERSION` no longer hardcoded "2 → 3" (coordinated at T1, exact version TBD — §4.4/§4.5); the T2 mid-season build cites ERR-030-005. |
| 0.3 | 2026-07-23 | — | AR-6 (M): §4.5 composition-root contract now pins `SeedInitialContracts` at new-career genesis vs sub-blob decode on load (never both). |
| 0.4 | 2026-09-12 | — | T0 implementation back-prop: records live phase-real T0 edges (#27/#40/ProjectConstants), defers unused #16 until a real T1/T3 consumer, records `ITransferRosterPort`, and marks the T0 file layout live. |
#endregion
