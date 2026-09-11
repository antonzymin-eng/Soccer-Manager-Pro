# Club Finances & Economy #40 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** September 10, 2026 (v0.5 — T1b landed: the #30 composition edge is now real, ERR-030-049)
**Last Updated (prior):** September 6, 2026 (v0.4 — PR #363 external-review correction: phase-real dependencies and T1a/T1b persistence boundary)
**Last Updated (prior):** September 4, 2026 (v0.3 — T1 self-identifying save framing back-prop)
**Version:** 0.5
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

At **T0 + T1a**, `TacticalDirector.ClubFinances` (`src/club-finances/`) references only
`TacticalDirector.DeterministicSim` (#16) and the cross-cutting `TacticalDirector.ProjectConstants`
foundation. `DeterministicSim` is consumed by T1a's canonical save framing; `ProjectConstants` is consumed
solely by the Code Standards #20-mandated `GameplayConfig.Get*` loading of #40's `[GT]` catalogue.

The `TacticalDirector.PlayerDatabase` (#27) edge is **T2-only**: it lands together with the first real
`Squad.ClubId` enumeration/bootstrap consumer. Carrying that edge at T0/T1a would be a dead architectural
reference. At every phase #40 does **not** reference `MatchEngine`, `LivingWorld`, `SeasonSave`, #30, #31,
#34, or #45; those higher/future systems call down into #40 rather than reversing ownership (FR-FN-027).

```
T1b current:  ClubFinances (#40) ──▶ DeterministicSim (#16)     [canonical save framing]
                                └──▶ ProjectConstants           [[GT] GameplayConfig loading]
T1b current:  #30 SeasonSave ──────▶ ClubFinances (#40)         [compose finance sub-blob]
T2 future:    ClubFinances (#40) ──▶ PlayerDatabase (#27)       [Squad.ClubId enumeration only]
T2+ future:   #31/#34/#45 ─────────▶ ClubFinances (#40)         [query/commands/modifier producer]
```

The T1b edge is **intra-tier**: `season-save` and `club-finances` are both Tier-7 Management (Code
Standards #20 §3.5.2), which permits the reference and forbids only a cycle. The direction is the one
FR-FN-027 fixes — #30 reads down into #40, never the reverse — so #40's own reference set is unchanged
by T1b.

The foundation edges introduce no domain ownership seam and #40 introduces no alternate config/serialization
framework. #27's assembly remains schema-untouched when the T2 reference lands.

## 4.2 File layout (lands incrementally by T-phase)

```
src/club-finances/
├── club-finances.asmdef
├── FinanceTransactionKind.cs         // the transaction-direction enum (T0)
├── FinanceLineItem.cs                // the transaction-classification enum (T0)
├── ClubFinances.cs                   // the #40-owned per-club state (T0; persisted by T1a)
├── ClubFinanceEntry.cs               // stable ClubId + ClubFinances persisted record (T1a)
├── FinanceTransaction.cs             // the ApplyTransaction input value (T0)
├── BoardModifier.cs                  // KD-4 identity routing seam (T0)
├── FinanceStep.cs                    // SettleFinances + PrizeMoneyForPosition (T0)
├── FinanceLedger.cs                  // ApplyTransaction + AvailableTransferBudget (T0)
├── FinancesViewModel.cs              // KD-8 observer (T0)
├── ClubFinancesSaveCodec.cs          // self-identifying finance sub-blob (T1a)
├── ClubFinancesConstants.cs          // Appendix A catalogue (T0/T1a constants)
└── tests/ …
```

## 4.3 Seam contracts

- **From #16 (T1a):** `ClubFinancesSaveCodec` consumes `CanonicalSerializer` and
  `SaveBlobFramingHelpers` for canonical framing. No RNG API or stream is consumed.
- **From ProjectConstants (T0):** #40's `[GT]` catalogue uses the existing `GameplayConfig.Get*` loader.
  This is a foundation/config-loading dependency only; it creates no gameplay ownership or mutation seam.
- **From #27 (T2 only, not present in the T0/T1a asmdef):** the `Squad.ClubId` enumeration is read to know
  which clubs require a `ClubFinances` entry. #40 declares no write path into `PlayerAttributes`/
  `PlayerRecord`; the future dependency is club identity only and lands atomically with its consumer.
- **To #30 (T1b/T2):** T1b composes #40's opaque codec into `SeasonSaveCodec` and bumps the composing format.
  T2 then makes `RollToNextSeason()` invoke `SettleFinances` per club at the reserved step (b') and calls
  `ClubFinances.CreateInitial` once per club at league/game bootstrap. #40 never references #30.
- **From #31 (future):** `AvailableTransferBudget` is a read-only query; `ApplyTransaction` is the single
  command #31 invokes on a committed deal. #40 declares no interface into #31 — #31 is a caller only.
- **From #34 (future):** staff wage line items reach #40 through the same `ApplyTransaction` command; no #34
  interface exists today.
- **From #45 (future):** `BoardModifier` is a value parameter passed into `SettleFinances`; #45 becomes the
  producer of a non-identity value when it exists. No #45 interface is built today.
- **To #38:** `FinancesViewModel` is a read-only value-copy observer; #38 pulls it.
- **Club lifecycle:** unlike #28/#41's per-`PlayerId` roster churn, a `ClubFinances` entry, once created by
  T2 bootstrap, is never removed by a season roll (FR-FN-025).

## 4.4 The T1a self-identifying `FINANCE_SAVE_FORMAT_VERSION` sub-blob codec

`ClubFinancesSaveCodec` is an opaque, independently version-gated sub-blob. **T1a owned only this standalone
codec**; composition into #30's `SeasonSaveCodec` and the coordinating outer `SEASON_SAVE_FORMAT_VERSION`
bump were **T1b**, and landed September 10, 2026 (ERR-030-049: frame `6 → 7`, the mandatory `FNCE` block
between #44's discipline block and the optional match block). The split kept each implementation phase
matched to the code: the codec was built and validated without pretending the season-save envelope had
already changed. The byte layout below is unchanged by T1b — the frame nests this block opaquely and
never parses it, so `FINANCE_SAVE_FORMAT_VERSION` stays 1.

A version word alone does not identify a format because multiple sibling sub-blobs legitimately sit at
version 1. The block therefore starts with fixed `FINANCE_SAVE_MAGIC = 0x464E4345` (`FNCE`) and rejects a
wrong magic before interpreting the version or payload. Fixed framing is 12 header bytes (magic + version +
count) and 52 bytes per club record (`i32 ClubId` + six `i64` finance fields).

```
EncodeFinances(perClubFinances) -> bytes:
    WriteU32(FINANCE_SAVE_MAGIC)
    WriteU32(FINANCE_SAVE_FORMAT_VERSION)
    WriteCount(perClubFinances.Count)
    for (clubId, f) in perClubFinances (ClubId ascending):
        WriteI32(clubId)
        WriteI64(f.Balance)
        WriteI64(f.TransferBudget)
        WriteI64(f.WageBudget)
        WriteI64(f.WageBillAggregate)
        WriteI64(f.SeasonRevenueAccrued)
        WriteI64(f.FfpBalanceWindow)
    # No RNG cursor/action ordinal block at minimal.

DecodeFinances(bytes) -> perClubFinances:
    magic = ReadU32(); if magic != FINANCE_SAVE_MAGIC: throw
    version = ReadU32(); if version != FINANCE_SAVE_FORMAT_VERSION: throw
    count = ReadCount()
    previousClubId = below int.MinValue
    for i in [0, count):
        clubId = ReadI32(); if clubId <= previousClubId: throw
        previousClubId = clubId
        ... read six i64 fields ...
        if transferBudget < 0 or wageBudget < 0 or wageBillAggregate < 0: throw
    if bytesRemaining != 0: throw
```

`ClubId` is treated as #27's stable opaque signed `int` identity; T1a does not need an assembly reference to
#27 merely to serialize that identity value. Encode sorts a copy of caller keys and refuses duplicates;
decode requires strictly ascending keys. All fields serialize through #16's `CanonicalSerializer`; signed
`long` fields round-trip bitwise, including negative `Balance` (debt). **Serialize, don't regenerate.**

## 4.5 RNG-namespace reservation (KD-2) — not registered at Stage 2

`_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` is filed as a placeholder row (ERR-040-001) —
reserved, **not** a named/promoted tag, because the minimal tier has no draw. No code constant is declared and
no stream is registered at T0–T2; the actual `DOMAIN_TAG_CLUB_FINANCES = 0x29` promotion, code const, and first
stochastic draw land together at #40 T3, keyed on `(clubId, seasonNumber, purpose)`. T1a therefore serializes
no `RngCursor` or `actionOrdinal`, and adding #40 through T2 leaves existing stream cursors unchanged.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly, file layout, seam contracts, save codec, reserved namespace slot. Status IN REVIEW. |
| 0.2 | 2026-09-04 | — | **T0 implementation back-prop.** Adds the cross-cutting `TacticalDirector.ProjectConstants` reference required by active Code Standards #20 for #40's `[GT]` loading. |
| 0.3 | 2026-09-04 | Codex | **T1 implementation back-prop.** Adds leading `FINANCE_SAVE_MAGIC`, fixed framing, canonical ClubId ordering/duplicate rejection, and signed ClubId preservation. |
| 0.4 | 2026-09-06 | — | **PR #363 external-review correction.** Defines the implemented codec as standalone T1a, moves #30 composition/version bump to T1b, and removes the dead PlayerDatabase edge until its T2 `Squad.ClubId` consumer lands. |
| 0.5 | 2026-09-10 | — | **T1b landed (ERR-030-049).** §4.1's reference diagram promotes the `#30 SeasonSave → ClubFinances` edge from future to current and records that it is an intra-Tier-7 reference (permitted; only cycles are forbidden) leaving #40's own reference set unchanged. §4.4 records the composition and the outer frame bump `6 → 7` as landed, and states that the block's own byte layout and `FINANCE_SAVE_FORMAT_VERSION = 1` are untouched by it — the frame nests the block opaquely. |
#endregion
