# Club Finances & Economy #40 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** September 10, 2026 (v1.0 — ERR-030-050 review correction: T1b includes the resume carrier/coherence guard; T2 remains runtime production. Prior update follows)
**Last Updated (prior):** September 10, 2026 (v0.9 — T1b landed: #30 season-save composition + frame bump, ERR-030-049)
**Last Updated (prior):** September 7, 2026 (v0.8 — PR #363 Codex arithmetic correction)
**Last Updated (prior):** September 7, 2026 (v0.7 — PR #363 follow-up review correction)
**Version:** 1.0
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
- **T2** — Wire `SettleFinances` at #30's **new** reserved step (b') (after the (a') #43 insertion point,
  before (c) regenerate); wire `CreateInitial` at league/game bootstrap for every `ClubId` (#30-driven, not
  #40-driven); consume the T1b-held finance state through those runtime producers/mutators; and add the
  `PlayerDatabase` reference only here, when
  the specified `Squad.ClubId`
  enumeration becomes a real consumer. Expose `AvailableTransferBudget`/`ApplyTransaction` for #31/#34/#42
  to call once those specs exist. No #30 tick-order change beyond the KD-6 back-prop already filed (KD-6).
- **T3** — Deep tier: per-day revenue accrual (matchday/sponsorship, a new daily #30 tick-order slot — the
  #41 pattern), the stochastic sponsorship-variance draw (promotes `DOMAIN_TAG_CLUB_FINANCES = 0x29` /
  `SubsystemOrdinals.ClubFinances = 91`, keyed on `(clubId, seasonNumber, purpose)`), the FFP soft-penalty
  modulating the next season's projected budget, non-identity `BoardModifier` consumption when #45 lands, and
  non-identity wage-ledger producers when #31/#34 land — all defaulting to their Stage-2 identities via a
  config dial (one code path, KD-4/KD-8).

## 7.2 Deferred (recorded, not built)

- **Per-day revenue accrual.** Stage-2 settles once per season; a Stage-3 daily accrual (matchday attendance
  revenue, sponsorship instalments) would need a new daily #30 tick-order slot analogous to #41's
  `AdvanceMedicalDay` insertion — not built here (KD-1).
- **Periodic wage cash-out.** Stage-2 `ApplyTransaction` records a wage as a change to the liability
  `WageBillAggregate` only (never `Balance`, §3.2/FR-FN-016). The periodic (weekly/monthly) *payment* of that
  wage bill — the step that actually debits `Balance` from `WageBillAggregate` — is a deep-tier accrual on
  the same future daily slot as revenue accrual; not built here, so at Stage 2 the wage bill is a liability
  figure that never drains cash.
- **Stochastic sponsorship/revenue variance.** The genuine first draw site on the reserved
  `_RESERVED_0x29_`/91 namespace slot; promotes the tag only when this lands (KD-2).
- **FFP soft-penalty.** A deep-tier adjustment to the *next* season's projected budget, composing
  multiplicatively with `BoardModifier`; defaults to "no penalty" at Stage 2 (KD-4).
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
- **#30 (season loop):** owns `SettleFinances` invocation timing (KD-6) and the one-time
  `ClubFinances.CreateInitial` bootstrap per club; #40 MUST NOT reference #30 or drive its own club-bootstrap
  independently (the one-way composition, FR-FN-027).
- **#27 (squad/player data):** at T2, the `Squad.ClubId` enumeration #40 reads for F6's club-universe check
  becomes the authoritative club-identity source; before T2 there is no #27 consumer and therefore no
  `PlayerDatabase` asmdef reference. #40 MUST NOT gain a second, competing club-identity notion.

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
   ERR-030-036). Until T2 wires `CreateInitial` the predicate cannot fire — every save carries the
   empty set over a destination that also carries an empty block — so this is hardening a path that has
   no live traffic yet. It is deliberate: #30 Appendix B.1 already requires any family added to the
   frame to bring its own guard, and the alternative is writing the fourth one after the fourth loss.
3. **No `financesWired` flag; the resume seam is T1b (ERR-030-050 correction).** The flag is unnecessary
   because FR-FN-025 puts #40 on the #28 side of ERR-030-038's line — an emptied *destination* is
   unambiguously a drop. But the prior conclusion that `SeasonLoop.Restore` could wait for the producer
   was incorrect: a populated block can arrive from `Load` before runtime production is wired. T1b
   therefore includes `financesOrNull`, loop-held canonical state, Save forwarding, and exact non-empty
   coherence with `SeasonState.ClubIds`; T2 remains `CreateInitial` and `SettleFinances` only.

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
#endregion