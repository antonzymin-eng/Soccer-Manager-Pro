# Club Finances & Economy #40 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** September 11, 2026 (v0.8 — T3a: record draw-free daily revenue accounting surface; dependency graph unchanged)
**Last Updated (prior):** September 11, 2026 (v0.7 — T2b: #30 bootstrap/settlement composition is live; ERR-030-051 pins staged commit semantics)
**Last Updated (prior):** September 11, 2026 (v0.6 — PR #392 T2a: the consumed #40 → #27 Squad.ClubId edge is now live; #30 invocation remains T2b)
**Last Updated (prior):** September 10, 2026 (v0.5 — T1b landed: the #30 composition edge is now real, ERR-030-049)
**Last Updated (prior):** September 6, 2026 (v0.4 — PR #363 external-review correction: phase-real dependencies and T1a/T1b persistence boundary)
**Last Updated (prior):** September 4, 2026 (v0.3 — T1 self-identifying save framing back-prop)
**Version:** 0.8
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

At **T0 + T1a**, `TacticalDirector.ClubFinances` (`src/club-finances/`) referenced only
`TacticalDirector.DeterministicSim` (#16) and the cross-cutting `TacticalDirector.ProjectConstants`
foundation. `DeterministicSim` is consumed by T1a's canonical save framing; `ProjectConstants` is consumed
solely by the Code Standards #20-mandated `GameplayConfig.Get*` loading of #40's `[GT]` catalogue.

At **T2a**, `TacticalDirector.PlayerDatabase` (#27) joins that reference set atomically with its first real
consumer: the pure `ClubFinanceEntry.CreateInitialForSquads(Squad[])` bootstrap transform reads canonical
`Squad.ClubId` identities and produces one initial finance entry per distinct club. Carrying that edge before
its consumer would have been a dead architectural reference; carrying it now is the phase boundary §7.1
requires. At every phase #40 does **not** reference `MatchEngine`, `LivingWorld`, `SeasonSave`, #30, #31,
#34, or #45; those higher/future systems call down into #40 rather than reversing ownership (FR-FN-027).

**T3a changes no assembly edge.** `FinanceStep.AccrueDailyRevenue` is a pure #40-owned accounting transform
over caller-supplied integer revenue amounts. It neither discovers the day/fixture nor calls #30, and it
performs no deterministic draw, so the existing `DeterministicSim` reference remains save-framing-only and
the reserved `0x29`/91 namespace remains unpromoted. The later #30 daily invocation is a caller-side T3
composition change on the already-existing `#30 → #40` edge, not a reverse dependency.

```
T3a current:  ClubFinances (#40) ──▶ DeterministicSim (#16)     [canonical save framing only; no draw]
                                 ├──▶ ProjectConstants           [[GT] GameplayConfig loading]
                                 └──▶ PlayerDatabase (#27)       [Squad.ClubId bootstrap transform]
T1b current:  #30 SeasonSave ──────▶ ClubFinances (#40)         [compose finance sub-blob]
T2b current:  #30 SeasonSave ──────▶ ClubFinances (#40)         [League.CreateLoop bootstrap + staged settle at step (b')]
T3 future:    #30 SeasonSave ──────▶ ClubFinances (#40)         [daily revenue invocation once amount producers are specified]
T3/future:    #31/#34/#45 ─────────▶ ClubFinances (#40)         [downstream query/commands/modifier producers]
```

The T1b edge is **intra-tier**: `season-save` and `club-finances` are both Tier-7 Management (Code
Standards #20 §3.5.2), which permits the reference and forbids only a cycle. The direction is the one
FR-FN-027 fixes — #30 reads down into #40, never the reverse. T2a adds only the already-authorized downward
#27 dependency; T3a adds no reference at all.

The foundation edges introduce no domain ownership seam and #40 introduces no alternate config/serialization
framework. #27's assembly remains schema-untouched: T2a consumes the existing `Squad.ClubId` surface only.

## 4.2 File layout (lands incrementally by T-phase)

```
src/club-finances/
├── club-finances.asmdef
├── FinanceTransactionKind.cs         // the transaction-direction enum (T0)
├── FinanceLineItem.cs                // the transaction-classification enum (T0)
├── ClubFinances.cs                   // the #40-owned per-club state (T0; persisted by T1a)
├── ClubFinanceEntry.cs               // persisted record (T1a) + pure Squad.ClubId bootstrap factory (T2a)
├── FinanceTransaction.cs             // the ApplyTransaction input value (T0)
├── BoardModifier.cs                  // KD-4 identity routing seam (T0)
├── FinanceStep.cs                    // SettleFinances + PrizeMoneyForPosition (T0); AccrueDailyRevenue (T3a)
├── FinanceLedger.cs                  // ApplyTransaction + AvailableTransferBudget (T0)
├── FinancesViewModel.cs              // KD-8 observer (T0)
├── ClubFinancesSaveCodec.cs          // self-identifying finance sub-blob (T1a)
├── ClubFinancesConstants.cs          // Appendix A catalogue (T0/T1a constants; later T3 tuning extends it)
└── tests/ …
```

## 4.3 Seam contracts

- **From #16 (T1a):** `ClubFinancesSaveCodec` consumes `CanonicalSerializer` and
  `SaveBlobFramingHelpers` for canonical framing. T3a still consumes no RNG API or stream.
- **From ProjectConstants (T0):** #40's `[GT]` catalogue uses the existing `GameplayConfig.Get*` loader.
  This is a foundation/config-loading dependency only; it creates no gameplay ownership or mutation seam.
- **From #27 (T2a, live):** `CreateInitialForSquads(Squad[])` reads only each canonical `Squad.ClubId`,
  produces one initial entry per distinct club and canonicalizes by ClubId. #40 declares no write path into
  `Squad`, `PlayerAttributes`, or `PlayerRecord`; the caller supplies the club universe and retains lifecycle
  ownership.
- **To #30 (T1b/T2b live; T3 daily invocation deferred):** T1b composes #40's opaque codec into
  `SeasonSaveCodec`. T2b's `League.CreateLoop` is the new-game lifecycle seam: it invokes T2a's bootstrap
  transform exactly once. `SeasonLoop.RollToNextSeason()` computes `SettleFinances` per club at reserved
  step (b') and stages those values until the fallible #30 season commit succeeds; only then are they
  installed (ERR-030-051). T3a now exposes the pure `AccrueDailyRevenue` accounting transform, but #30 does
  not invoke it yet: the daily tick-order insertion and amount producers remain later T3 work. #40 never
  references #30 and never invokes its own bootstrap factory independently.
- **T3a autonomous accounting:** `AccrueDailyRevenue` is #40-owned rather than a second caller-command
  ledger. With its gate disabled it returns the prior state exactly; enabled, it can change only `Balance`
  and `SeasonRevenueAccrued`. The caller may supply already-derived amounts, but the normative amount model
  remains #40-owned and is not delegated to #30/#31/#34/#45 by this surface.
- **From #31 (future):** `AvailableTransferBudget` is a read-only query; `ApplyTransaction` is the single
  externally-commanded ledger command #31 invokes on a committed deal. #40 declares no interface into #31 —
  #31 is a caller only.
- **From #34 (future):** staff wage line items reach #40 through the same `ApplyTransaction` command; no #34
  interface exists today.
- **From #45 (future):** `BoardModifier` is a value parameter passed into `SettleFinances`; #45 becomes the
  producer of a non-identity value when it exists. No #45 interface is built today.
- **To #38:** `FinancesViewModel` is a read-only value-copy observer; #38 pulls it.
- **Club lifecycle:** unlike #28/#41's per-`PlayerId` roster churn, a `ClubFinances` entry, once created by
  T2b's one-time invocation of the T2a transform, is never removed by a season roll (FR-FN-025).

## 4.4 The T1a self-identifying `FINANCE_SAVE_FORMAT_VERSION` sub-blob codec

`ClubFinancesSaveCodec` is an opaque, independently version-gated sub-blob. **T1a owned only this standalone
codec**; composition into #30's `SeasonSaveCodec` and the coordinating outer `SEASON_SAVE_FORMAT_VERSION`
bump were **T1b**, and landed September 10, 2026 (ERR-030-049: frame `6 → 7`, the mandatory `FNCE` block
between #44's discipline block and the optional match block). The split kept each implementation phase
matched to the code: the codec was built and validated without pretending the season-save envelope had
already changed. The byte layout below is unchanged by T1b, T2a, T2b, or T3a — T3a mutates fields that were
already persisted in the six-field record and introduces no new cursor/state field, so
`FINANCE_SAVE_FORMAT_VERSION` stays 1.

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
    # No RNG cursor/action ordinal block at minimal/T3a.

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

`ClubId` is treated as #27's stable opaque signed `int` identity. T1a did not need an assembly reference to
#27 merely to serialize that identity value; T2a adds the reference only because it now reads `Squad.ClubId`
directly. Encode sorts a copy of caller keys and refuses duplicates; decode requires strictly ascending keys.
All fields serialize through #16's `CanonicalSerializer`; signed `long` fields round-trip bitwise, including
negative `Balance` (debt). **Serialize, don't regenerate.**

## 4.5 RNG-namespace reservation (KD-2) — still not registered at T3a

`_RESERVED_0x29_` / `SubsystemOrdinals.ClubFinances = 91` is filed as a placeholder row (ERR-040-001) —
reserved, **not** a named/promoted tag. T3a performs no draw, so no code constant is declared and no stream is
registered through this slice. The actual `DOMAIN_TAG_CLUB_FINANCES = 0x29` promotion, code const, and first
stochastic sponsorship-variance consumer MUST still land atomically in the later T3 stochastic slice, keyed
on `(clubId, seasonNumber, purpose)`. T3a therefore serializes no `RngCursor` or `actionOrdinal`, and adding
its deterministic accounting transform leaves every existing stream cursor unchanged.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly, file layout, seam contracts, save codec, reserved namespace slot. Status IN REVIEW. |
| 0.2 | 2026-09-04 | — | **T0 implementation back-prop.** Adds the cross-cutting `TacticalDirector.ProjectConstants` reference required by active Code Standards #20 for #40's `[GT]` loading. |
| 0.3 | 2026-09-04 | Codex | **T1 implementation back-prop.** Adds leading `FINANCE_SAVE_MAGIC`, fixed framing, canonical ClubId ordering/duplicate rejection, and signed ClubId preservation. |
| 0.4 | 2026-09-06 | — | **PR #363 external-review correction.** Defines the implemented codec as standalone T1a, moves #30 composition/version bump to T1b, and removes the dead PlayerDatabase edge until its T2 `Squad.ClubId` consumer lands. |
| 0.5 | 2026-09-10 | — | **T1b landed (ERR-030-049).** §4.1's reference diagram promotes the `#30 SeasonSave → ClubFinances` edge from future to current and records that it is an intra-Tier-7 reference (permitted; only cycles are forbidden) leaving #40's own reference set unchanged. §4.4 records the composition and the outer frame bump `6 → 7` as landed, and states that the block's own byte layout and `FINANCE_SAVE_FORMAT_VERSION = 1` are untouched by it — the frame nests the block opaquely. |
| 0.6 | 2026-09-11 | — | **PR #392 T2a architecture back-prop.** Promotes the authorized #40 → #27 `PlayerDatabase` edge from future to current with its `Squad.ClubId` bootstrap consumer, records the factory as a pure transform rather than a lifecycle owner, and keeps #30's production invocation/settlement wiring in T2b. |
| 0.7 | 2026-09-11 | — | **T2b / ERR-030-051 architecture back-prop.** Promotes #30's bootstrap/settlement edge to current, records `League.CreateLoop` as the lifecycle owner, and pins settlement-at-(b') with post-commit installation to preserve #30 atomicity. |
| 0.8 | 2026-09-11 | OpenAI | **T3a architecture back-prop.** Records `AccrueDailyRevenue` as a draw-free #40-owned autonomous accounting transform, keeps the assembly graph unchanged, leaves #30 daily invocation deferred, and keeps `0x29`/91 reserved until the first stochastic sponsorship-variance consumer. |
#endregion