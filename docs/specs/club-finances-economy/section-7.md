# Club Finances & Economy #40 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** September 11, 2026 (v1.4 — T3a foundation: pure daily-revenue accounting + season accumulator lifecycle; live producers/tick/RNG remain T3b+)
**Last Updated (prior):** September 11, 2026 (v1.3 — T2b review correction: Restore-only migration of the legacy empty T1b finance block; generic composition does not silently initialize)
**Last Updated (prior):** September 11, 2026 (v1.2 — T2b implemented: #30 bootstrap invocation, live boundary settlement, and finance command/read surfaces; ERR-030-051 atomicity correction)
**Last Updated (prior):** September 11, 2026 (v1.1 — PR #392 formalizes T2a/T2b: T2a is the #27-backed bootstrap factory/reference activation; T2b is #30 invocation + settlement wiring)
**Last Updated (prior):** September 10, 2026 (v1.0 — ERR-030-050 review correction: T1b includes the resume carrier/coherence guard; T2 remains runtime production. Prior update follows)
**Last Updated (prior):** September 10, 2026 (v0.9 — T1b landed: #30 season-save composition + frame bump, ERR-030-049)
**Last Updated (prior):** September 7, 2026 (v0.8 — PR #363 Codex arithmetic correction)
**Last Updated (prior):** September 7, 2026 (v0.7 — PR #363 follow-up review correction)
**Version:** 1.4
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.ClubFinances` assembly: value types (`FinanceTransactionKind`, `FinanceLineItem`,
  `ClubFinances`, `FinanceTransaction`, `BoardModifier`, `FinancesViewModel`), the deterministic
  `SettleFinances` + `PrizeMoneyForPosition` + `ApplyTransaction` + `AvailableTransferBudget`, and
  `ClubFinancesConstants`. Behaviour-neutral by construction (KD-8). The assembly carries the cross-cutting
  `ProjectConstants` reference required by Code Standards #20 for `[GT]` `GameplayConfig.Get*` loading;
  this is not a domain ownership seam (FR-FN-027 / §4.1).
- **T1a** — Standalone, uncomposed persistence block: `ClubFinanceEntry` + self-identifying
  `ClubFinancesSaveCodec` (`FINANCE_SAVE_MAGIC`, `FINANCE_SAVE_FORMAT_VERSION` = 1), canonical ascending
  `ClubId` order, exact-consumption framing, and fail-loud corruption gates. This phase adds the
  `DeterministicSim` dependency for `CanonicalSerializer` / `SaveBlobFramingHelpers` but deliberately does
  **not** modify #30, `SeasonSaveCodec`, or `SEASON_SAVE_FORMAT_VERSION`.
- **T1b** *(landed September 10, 2026)* — Compose the T1a finance sub-blob into #30's `SeasonSaveCodec` and coordinate #30's composing
  `SEASON_SAVE_FORMAT_VERSION` bump. This is the first phase in which #40 persistence participates in the
  full season-save envelope and the cross-assembly save/restore acceptance cases can run. Delivered as
  the #30-side back-prop **ERR-030-049**: `SEASON_SAVE_FORMAT_VERSION` 6 → 7, a new mandatory `FNCE`
  sub-blob between the #44 discipline block and the optional match block, the typed `FinanceBlock`
  frame handle (the ERR-029-005 discipline), a required `ClubFinanceEntry[]` parameter on
  `SeasonSaveManager.Save`, `SeasonSaveContents.Finances` on the restore side, and
  `RequireDestinationCarriesNoFinances` — the fourth of #30 Appendix B.1's destination guards, keyed on
  emptiness because FR-FN-025 leaves #40 no legitimate drained state. `season-save` gains the
  `TacticalDirector.ClubFinances` reference (an intra-Tier-7 edge; #40 gains none). T1b also includes
  the loop-held canonical finance carrier, `SeasonLoop.Restore(..., financesOrNull)`, forwarding through
  `Save(SeasonLoop, ...)`, and the shared current-season coherence guard: empty is legal pre-T2; once
  non-empty, finance ClubIds exactly equal `SeasonState.ClubIds` (ERR-030-050).
- **T2a** — Activate the planned `PlayerDatabase` dependency together with its first real consumer: a pure
  #40 factory over caller-supplied canonical `Squad[]` values that reads only `Squad.ClubId`, creates one
  `ClubFinances.CreateInitial(StartingClubBalance)` entry per distinct club, canonicalizes by ascending
  `ClubId`, and fails loud on a null/empty/malformed or duplicate club set. This phase does **not** itself
  start a game or mutate #30; it only provides the typed bootstrap transform that T2b's composition root
  invokes. T1b persistence/resume guards remain unchanged.
- **T2b** *(implemented September 11, 2026)* — #30 now owns the one-time production invocation through
  `League.CreateLoop`: it feeds the league's canonical `Squad[]` set into T2a's
  `ClubFinanceEntry.CreateInitialForSquads` exactly once and composes the resulting entries into
  `SeasonLoop`. `RollToNextSeason()` computes every club's `FinanceStep.SettleFinances` result at the
  reserved step (b') from the final table, before (c) regenerate, through `SeasonFinanceRuntime`. The
  values are staged until `BeginNextSeason` succeeds and only then copied into the live finance array,
  preserving #30's refused-roll atomicity (ERR-030-051) without moving the semantic settlement point.
  `SeasonLoop.FinanceView`, `AvailableTransferBudget`, and `ApplyTransaction` expose the #40-owned
  observer/query/command surfaces without granting direct field mutation. T2b also closes the T1b empty-
  block compatibility edge explicitly: `SeasonFinanceCoherence.Normalize` remains validation-only, while
  `SeasonLoop.Restore` alone invokes `NormalizeLegacyRestore` to initialize a persisted empty T1b block
  from the restored `SeasonState.ClubIds`. The generic constructor may still represent legacy/unwired
  emptiness for low-level compatibility, but every finance read/command and season settlement fails loud
  on that state; a forgotten finance argument therefore cannot grant a club starting cash silently.
- **T3a** *(this slice)* — Build the deep-tier **accounting foundation only** without fabricating tuning or
  upstream producers: `FinanceStep.AccrueDailyRevenue` atomically adds already-derived non-negative
  sponsorship + matchday revenue to `Balance` and `SeasonRevenueAccrued`, with an exact identity-off path and
  checked arithmetic. `SettleFinances` closes `SeasonRevenueAccrued` to zero at the season boundary so the
  current-season accumulator cannot bleed across seasons; `FfpBalanceWindow` carries until the FFP slice.
  This slice adds no #30 daily invocation, no revenue `[GT]` magnitudes/formula, no draw, no namespace
  promotion, no new save field/version, and no assembly edge. Stage-2 behavior remains exact because the
  deep accrual gate is off and the accumulator is already zero (KD-8).
- **T3b+** — Complete the live deep tier in contract-first slices: define sponsorship/matchday amount
  formulas and `[GT]` catalogue, then add the new daily #30 tick-order invocation (the #41 pattern); add the
  stochastic sponsorship-variance draw only when its genuine consumer exists, atomically promoting
  `DOMAIN_TAG_CLUB_FINANCES = 0x29` / `SubsystemOrdinals.ClubFinances = 91` keyed on
  `(clubId, seasonNumber, purpose)`; add the FFP soft-penalty that consumes the completed season state before
  T3a's accumulator reset; consume non-identity `BoardModifier` when #45 lands; and accept non-identity wage
  ledger producers when #31/#34 land. All remain identity-defaulted until their producer/config contract is
  real (KD-4/KD-8).

## 7.2 Deferred after T3a

- **Live per-day revenue production and invocation.** The pure T3a accounting mutation now exists, but the
  amount model does not: sponsorship/matchday formulas and their `[GT]` values remain unspecified, and #30
  does not yet call the primitive. The live step therefore remains deferred until #40 defines those amounts;
  only then should #30 add a daily tick-order slot analogous to #41's `AdvanceMedicalDay` insertion (KD-1).
- **Periodic wage cash-out.** Stage-2 `ApplyTransaction` records a wage as a change to the liability
  `WageBillAggregate` only (never `Balance`, §3.2/FR-FN-016). The periodic (weekly/monthly) *payment* of that
  wage bill — the step that actually debits `Balance` from `WageBillAggregate` — is a deep-tier accrual on
  the same future daily slot as revenue accrual; not built here, so at Stage 2 the wage bill is a liability
  figure that never drains cash.
- **Stochastic sponsorship/revenue variance.** The genuine first draw site on the reserved
  `_RESERVED_0x29_`/91 namespace slot; promotes the tag only when this lands (KD-2). T3a intentionally leaves
  the namespace reserved because it performs no draw.
- **FFP soft-penalty/window update.** A deep-tier adjustment to the *next* season's projected budget,
  composing multiplicatively with `BoardModifier`; defaults to "no penalty" at Stage 2 (KD-4). T3a defines
  only the lifecycle prerequisite: the future calculation consumes the completed season's prior
  `SeasonRevenueAccrued` before `SettleFinances` returns that field reset to zero. The formula and
  `FfpBalanceWindow` update semantics remain deferred.
- **Non-identity `BoardModifier` (#45).** #45 becomes the producer of a real board-driven multiplier
  (takeover windfalls, confidence-linked budget adjustments); no #45 interface is built ahead of that
  (FR-LW-031).
- **Non-identity wage-ledger producers (#31/#34).** Player-contract and staff-contract wage line items via
  `ApplyTransaction`; the ledger structure exists today, empty (KD-5).
- **A "remaining budget net of season spend" running total.** Stage 2's `TransferBudget`/`WageBudget` are
  static per-season ceilings, not decremented by `ApplyTransaction` (§1.6); a deep-tier extension could add
  a derived "remaining" field if #31 needs it, without changing the ceiling semantics.
- **A genuinely stochastic FFP/board-confidence interaction** beyond the fixed multiplicative composition. If
  a later extension needs this, it composes as an additional multiplicative term or an additional keyed draw
  purpose on the same T3 stream — no second stream is needed, since the stream is already keyed per-purpose
  (the #41 keyed-derivation append-only-purpose precedent — ERR-041-012: no stream).

## 7.3 Seam contracts recorded for downstream authors

- **#31 (transfer market):** becomes the caller of `AvailableTransferBudget`/`ApplyTransaction`. #31 MUST NOT
  write `ClubFinances` fields directly, MUST NOT maintain a parallel budget/ledger total, and MUST NOT expect
  `TransferBudget`/`WageBudget` to decrement as `ApplyTransaction` calls accumulate (they are season
  constants set by `SettleFinances`, §1.6).
- **#34 (staff, future):** becomes a second caller of `ApplyTransaction` (`LineItem = StaffWage`) — the same
  contract as #31's wage line items; #34 MUST NOT add a second wage-aggregation path.
- **#45 (board & ownership, future):** becomes the producer of a non-identity `BoardModifier`. #45 MUST
  supply a **positive** `BudgetMultiplierMillPermille`; every non-positive value, including
  `default(BoardModifier)`, reaching `SettleFinances` fails loud by design (FR-FN-018). #45 MUST NOT add a
  second budget-multiplier path.
- **#43 (promotion/relegation, future):** when it lands, its transform inserted at #30's step (a') produces
  the post-promotion division/`finalTablePosition` #40's step (b') already reads — no #40-side change is
  needed (the KD-6 ordering rationale is written to anticipate this); #43 MUST NOT itself call
  `SettleFinances` or otherwise reach into #40.
- **#30 (season loop):** owns `SettleFinances` invocation timing (KD-6) and the one-time production
  bootstrap invocation per club. T2a's `CreateInitialForSquads(Squad[])` is a pure transform over the
  canonical squads #30 supplies; its presence in #40 does **not** make #40 a lifecycle/composition owner.
  T3a likewise exposes only a pure accounting transform; #30 owns the future daily invocation once #40's
  amount model exists. #40 MUST NOT reference #30, discover clubs independently, or invoke either lifecycle
  path on its own (the one-way composition, FR-FN-027).
- **#27 (squad/player data):** at T2a, the `Squad.ClubId` enumeration #40 reads becomes the authoritative
  club-identity source and the `PlayerDatabase` asmdef reference becomes live in the same landing. #40 MUST
  NOT gain a second, competing club-identity notion.

## 7.4 T0/T1a implementation critique record

The first implementation pass on September 4, 2026 was reviewed against the approved #40 contract,
active Code Standards #20, repository gates, and then an external review of PR #363. The review found and
resolved the following landing defects:

1. **`[GT]` loading / reference mismatch.** The approved architecture omitted the cross-cutting foundation
   required by active Code Standards for `[GT]` loading. §4.1/§4.3 now name `ProjectConstants` explicitly;
   the implementation uses the established loader and introduces no alternate loader.
2. **Canonical assembly-tier seating.** A new production `.asmdef` is not conformant until its folder is
   placed in Code Standards #20 §3.5.2 in the same landing. `club-finances` is a Tier-7 Management assembly:
   long-horizon state above a single match, with only downward/foundation references.
3. **General-test allocation marker.** Structural tests use reflection/file inspection in general unit tests;
   their files carry Code Standards #20 §3.9.4's explicit general-unit-test allocation-relaxation marker.
4. **Source documentation surface.** The production/test files carry the required file headers, XML summaries
   on public APIs, and append-only version histories; no unused friend-assembly widening remains.
5. **External-review phase correction.** The second commit had already landed a complete standalone save codec
   while the PR/spec still called the slice “T0” and claimed all T1 persistence deferred. §7.1 now names that
   real boundary as **T1a**, keeps `SeasonSaveCodec` composition/version bump in **T1b**, and the PR scope is
   relabeled accordingly. The same review found `PlayerDatabase` referenced with no consumer; the edge is
   removed from T0/T1a and assigned to T2, where `Squad.ClubId` is first consumed. Executable locks now cover
   the exact current asmdef boundary, no-RNG serialized shape, the upper budget clamp, decode ordering, and short/
   truncated framing.
6. **Board-modifier domain correction.** A follow-up review found that the new negative-multiplier test had
   silently made a nonsense board multiplier valid without spec authority. FR-FN-018/F4 and §3.1 now reject
   every non-positive multiplier before arithmetic; the regression lock asserts that negative input fails loud.
7. **Overflow-safe board scaling.** Codex found that an accepted large `[GT]`-derived base ceiling multiplied
   by a positive non-identity board factor could overflow `long` before the upper clamp executed. §3.1 and
   `FinanceStep` now use quotient/remainder scaling with a division-only pre-cap; T-FN-INT-002 locks both the
   saturating extreme and unchanged below-cap integer-floor result.

That landing (PR #363, merged to `main`) therefore delivered **T0 + T1a**.

## 7.5 T1b landing record

The September 10, 2026 landing delivers **T1b**, filed against #30 as **ERR-030-049** (spec + code in
one commit, the standing rule). Three decisions in it are worth recording because a later reader will
otherwise have to re-derive them:

1. **The block is mandatory, not presence-flagged.** #40 has no absent case, only an empty one: a club
   universe with no entries yet is a well-formed zero-club block. Flagging it would add a presence bit
   distinguishing "no clubs" from "no block" — a distinction with no meaning — and would force a second
   frame bump the day T2 bootstraps the entries. The five sibling career blocks all reason this way
   (#30 Appendix B).
2. **The destination guard landed with the block, ahead of its producer.** Its three siblings were each
   written *after* a career's state had already been deleted (ERR-028-008, the career-triple guard,
   ERR-030-036). Until T2b invokes the bootstrap the predicate cannot fire — every save carries the
   empty set over a destination that also carries an empty block — so this is hardening a path that has
   no live traffic yet. It is deliberate: #30 Appendix B.1 already requires any family added to the
   frame to bring its own guard, and the alternative is writing the fourth one after the fourth loss.
3. **No `financesWired` flag; the resume seam is T1b (ERR-030-050 correction).** The flag is unnecessary
   because FR-FN-025 puts #40 on the #28 side of ERR-030-038's line — an emptied *destination* is
   unambiguously a drop. But the prior conclusion that `SeasonLoop.Restore` could wait for the producer
   was incorrect: a populated block can arrive from `Load` before runtime production is wired. T1b
   therefore includes `financesOrNull`, loop-held canonical state, Save forwarding, and exact non-empty
   coherence with `SeasonState.ClubIds`; T2a/T2b remain runtime bootstrap/settlement work only.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan (T0–T3) + deferred extensions + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): §7.2 records the deferred periodic wage cash-out (the step that debits `Balance` from `WageBillAggregate`). |
| 0.3 | 2026-08-08 | — | **ERR-041-012 back-prop**: the append-only-purpose precedent citation renamed — #41 has a keyed derivation, not an `injuries.occurrence` stream. |
| 0.4 | 2026-09-04 | — | **T0 implementation critique/back-prop.** Records and discharges the `ProjectConstants` `[GT]` loader dependency mismatch, mandatory Tier-7 seating, and §3.9.4 general-test allocation marker. |
| 0.5 | 2026-09-04 | Codex | **T0 critique closure.** Adds the source-documentation-template correction and records removal of the unused friend-assembly surface. |
| 0.6 | 2026-09-06 | — | **PR #363 external-review correction.** Reclassifies the already-landed standalone codec as T1a, creates T1b for #30 composition/version bump, defers the unused PlayerDatabase edge to its first T2 consumer, and records the added regression locks. |
| 0.7 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** Records the deliberate non-positive `BoardModifier` fail-loud decision and corrects the clamp-coverage wording. |
| 0.8 | 2026-09-07 | — | **PR #363 Codex correction.** Records overflow-safe board scaling and corrects the downstream #45 seam from “non-zero” to the normative positive-multiplier contract. |
| 0.9 | 2026-09-10 | — | **T1b landed (ERR-030-049).** §7.1's T1b entry records what shipped (frame v6 → 7, the mandatory `FNCE` sub-blob, the typed `FinanceBlock` handle, the required `Save` parameter, `SeasonSaveContents.Finances`, `RequireDestinationCarriesNoFinances`, the intra-Tier-7 `season-save` → `club-finances` reference); T2 gains the `SeasonLoop` resume seam T1b deliberately deferred; new §7.5 records the three decisions a later reader would otherwise re-derive — mandatory-not-flagged, the guard landing ahead of its producer, and why #40 needs neither a wiring flag nor a `Restore` parameter yet. |
| 1.0 | 2026-09-10 | — | **ERR-030-050 review correction.** T1b includes the `SeasonLoop` restore/carrier/save seam and exact current-season finance coherence; T2 is limited to `CreateInitial`, `SettleFinances`, producer wiring and consumer exposure. The v0.9 producer-wired deferral remains historical but is superseded. |
| 1.1 | 2026-09-11 | — | **PR #392 T2 split back-prop.** T2a owns only the pure #27-backed bootstrap factory and activation of the consumed `PlayerDatabase` edge; T2b owns #30's production invocation plus step-(b') settlement wiring. §7.3 explicitly keeps lifecycle/composition ownership in #30 so the factory is not a second bootstrap authority. |
| 1.2 | 2026-09-11 | — | **T2b implementation / ERR-030-051.** Records `League.CreateLoop` as #30's one-time bootstrap owner, the live staged settlement at (b'), post-commit finance installation preserving refused-roll atomicity, and the public observer/query/command surfaces. T3 becomes the next phase. |
| 1.3 | 2026-09-11 | — | **T2b review correction.** Legacy empty T1b state is migrated only through `SeasonLoop.Restore`/`NormalizeLegacyRestore`; ordinary normalization remains validation-only, and generic legacy/unwired empty composition fails loud on finance use rather than silently receiving starting cash. |
| 1.4 | 2026-09-11 | OpenAI | **T3a foundation.** Splits the deep tier into a mergeable draw-free accounting/lifecycle foundation versus future live producers/invocation/RNG/FFP work; records the new pure accrual primitive, current-season revenue reset, unchanged FFP window, and unchanged dependency/save/RNG shape. |
#endregion