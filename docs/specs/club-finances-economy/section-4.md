# Club Finances & Economy #40 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** September 4, 2026 (v0.2 — T0 back-prop: explicit ProjectConstants config dependency)
**Last Updated (prior):** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.2
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly `TacticalDirector.ClubFinances` (`src/club-finances/`), references
`TacticalDirector.PlayerDatabase` (#27), `TacticalDirector.DeterministicSim` (#16), and the cross-cutting
`TacticalDirector.ProjectConstants` foundation solely for the Code Standards #20-mandated
`GameplayConfig.Get*` loading of #40's `[GT]` catalogue. It does **not** reference `MatchEngine`,
`LivingWorld`, `SeasonSave`, #30, #31, #34, or #45; #30's season-save assembly and #31's (future)
transfer-market assembly reference *it* (the one-way composition, FR-FN-027).

```
#30 SeasonSave/RollToNextSeason ──▶ ClubFinances (#40) ──▶ PlayerDatabase (#27)   [reads Squad.ClubId enumeration]
#31 Transfer Market (future)    ──▶ ClubFinances (#40) ──▶ DeterministicSim (#16)  [reserved namespace only;
                                                                                     no stream at minimal, KD-2]
#34 Staff (future)               ──▶ ClubFinances (#40) ──▶ ProjectConstants       [[GT] GameplayConfig loading only]
#45 Board & Ownership (future)   ──▶ ClubFinances (#40)
```

The `ProjectConstants` edge is architectural infrastructure, not a domain seam: it does not change #40's
one-way ownership graph and exists only because active Code Standards #20 require `[GT]` values to use the
established `GameplayConfig.Get*` loader. #40 does not introduce another config loader.

#27's assembly is **schema-untouched**: `Squad.ClubId` is #27's own already-published identity field, so #40
reads it without #27 gaining a reference to #40. #40 does not consume `PlayerAttributes`/`PlayerRecord`
fields at Stage 2 — only the `ClubId` enumeration, to know the stable set of clubs requiring a
`ClubFinances` entry (F6, §2.3).

## 4.2 File layout (proposed; lands at T-phase)

```
src/club-finances/
├── club-finances.asmdef
├── FinanceTransactionKind.cs         // the transaction-direction enum
├── FinanceLineItem.cs                // the transaction-classification enum
├── ClubFinances.cs                   // the #40-owned per-club state (serialized)
├── FinanceTransaction.cs             // the ApplyTransaction input value
├── BoardModifier.cs                  // KD-4 identity routing seam
├── FinanceStep.cs                    // SettleFinances + PrizeMoneyForPosition
├── FinanceLedger.cs                  // ApplyTransaction + AvailableTransferBudget
├── FinancesViewModel.cs              // KD-8 observer
├── ClubFinancesSaveCodec.cs          // FINANCE_SAVE_FORMAT_VERSION sub-blob (T1)
├── ClubFinancesConstants.cs          // Appendix A catalogue
└── Tests/ …
```

## 4.3 Seam contracts

- **From #27 (F6/§1.3):** the `Squad.ClubId` enumeration is read to know which clubs require a
  `ClubFinances` entry; #40 declares **no** write path into `PlayerAttributes`/`PlayerRecord` and does not
  read either at Stage 2 — the dependency is club-identity only.
- **From ProjectConstants (T0/code standards):** #40's `[GT]` catalogue uses the existing
  `GameplayConfig.Get*` loader. This is a foundation/config-loading dependency only; it creates no
  gameplay ownership or mutation seam and introduces no new loader.
- **To #30 (KD-6/KD-7):** #30's `RollToNextSeason()` invokes `SettleFinances` per club at the new reserved
  step (b') (after (a')'s #43 promotion/relegation insertion point, before (c) regenerate), and calls
  `ClubFinances.CreateInitial` once per club at league/game bootstrap (never #40 itself — #40 does not
  observe club creation; #30 drives it, the one-way `#30 → #40` composition). #40 declares no interface for
  #30 — #30 calls #40's public API.
- **From #31 (KD-3, future):** `AvailableTransferBudget` is a read-only query; `ApplyTransaction` is the
  single command #31 invokes on a committed deal. #40 declares **no** interface into #31 — #31 is a caller
  only.
- **From #34 (KD-5, future):** staff wage line items reach #40 through the same `ApplyTransaction` command
  #31 uses (`LineItem = StaffWage`); no #34 interface exists today (FR-LW-031).
- **From #45 (KD-4, future):** `BoardModifier` is a value parameter passed into `SettleFinances`; #45
  becomes the producer of a non-identity value when it lands. No #45 interface is built today.
- **To #38 (KD-8):** `FinancesViewModel` is a read-only value-copy observer; #38 pulls it.
- **Club lifecycle (FR-FN-025):** unlike #28/#41's per-`PlayerId` regen/retire churn, #40 exposes no
  insert/remove entry point beyond `CreateInitial` — a `ClubFinances` entry, once created, is never removed
  by a season roll (clubs are a stable universe, KD-7). #30 calls `CreateInitial` exactly once per club, at
  league/game bootstrap, not at every season roll.

## 4.4 The `FINANCE_SAVE_FORMAT_VERSION` sub-blob codec

`ClubFinancesSaveCodec` is an opaque, independently version-gated sub-blob composed into #30's
`SeasonSaveCodec` — the same pattern #28's `PROGRESSION_SAVE_FORMAT_VERSION`, #29's
`TRAINING_SAVE_FORMAT_VERSION`, and #41's `MEDICAL_SAVE_FORMAT_VERSION` blocks use. The codec never parses
#30's other sub-blobs and vice-versa; #30's composing outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated
at #40's T1 exactly as it was for #28/#29/#41.

```
EncodeFinances(perClubFinances) -> bytes:
    WriteU32(FINANCE_SAVE_FORMAT_VERSION)
    WriteCount(perClubFinances.Count)                       # overflow-safe (fail loud on corrupt count, F5)
    for (clubId, f) in perClubFinances (ClubId ascending):  # deterministic club order
        WriteI32(clubId)
        WriteI64(f.Balance)
        WriteI64(f.TransferBudget)
        WriteI64(f.WageBudget)
        WriteI64(f.WageBillAggregate)
        WriteI64(f.SeasonRevenueAccrued)
        WriteI64(f.FfpBalanceWindow)
    # NO RNG cursor block — the minimal tier registers no stream at all (KD-2/FR-FN-008/009); a future T3
    # deep-tier draw stays keyed/position-independent, so even then there is no cursor to serialize (the
    # #28/#41 precedent).

DecodeFinances(bytes) -> perClubFinances:
    version = ReadU32(); if version != FINANCE_SAVE_FORMAT_VERSION: throw          # F3
    count = ReadCount()                                       # overflow-safe bound guard (F5)
    for i in [0, count):
        clubId = ReadI32()
        balance = ReadI64()
        transferBudget = ReadI64(); wageBudget = ReadI64()
        wageBillAggregate = ReadI64()
        seasonRevenueAccrued = ReadI64(); ffpBalanceWindow = ReadI64()
        if transferBudget < 0 or wageBudget < 0 or wageBillAggregate < 0: throw     # F1 coherence gate
        ... reconstruct ClubFinances ...
    if bytesRemaining != 0: throw                             # trailing-byte guard, F5
```

Fail-loud gates per F1/F3/F5 (the `MatchSaveCodec` / `WorldStateSerializer.ReadCount` posture). All fields
serialized via #16's `CanonicalSerializer` (bitwise round-trip); **serialize, don't regenerate**.

## 4.5 RNG-namespace reservation (KD-2) — not registered at Stage 2

`_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` is filed as a **placeholder row** (ERR-040-001,
against `deterministic-sim/section-3.md`) at section-file approval — reserved, **not** a named/promoted tag,
because the minimal tier has no draw (KD-2). No code constant is declared and no stream is registered at
T0–T2; the actual `DOMAIN_TAG_CLUB_FINANCES = 0x29` promotion, the code const, and the first
`club-finances.sponsorship-variance` stream registration all land together at #40 T3, keyed on `(clubId,
seasonNumber, purpose)` — position-independent, so there is never a cursor to persist even once the stream
exists (the #28/#41 keyed-draw precedent). Because nothing is registered before T3, the minimal tier's
addition leaves every existing stream's cursor byte-identical trivially (FR-FN-028).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly, file layout, seam contracts, save codec, reserved namespace slot. Status IN REVIEW. |
| 0.2 | 2026-09-04 | — | **T0 implementation back-prop.** Adds the cross-cutting `TacticalDirector.ProjectConstants` reference required by active Code Standards #20 for #40's `[GT]` `GameplayConfig.Get*` loading. Domain ownership/reference direction is unchanged; no new loader or gameplay seam is introduced. |
#endregion