# Transfers, Contracts & Negotiation #31 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** October 21, 2026 (v0.16 — AR round 5, ERR-031-015: the unposted counterparty is the interim option (b), not a decision (test renamed); new T-TX-BID-009 gate-order and full-contract-set locks; T-TX-REKEY-003 is an allow-list; the BID-008/NEU-001 scans state what they see; §5.7 re-pointed)
**Last Updated (prior):** October 20, 2026 (v0.15 — AR round 5, ERR-031-014 note: T-TX-REKEY-005 names its #28 `PlayerRecord` leg, which compares the weak-foot rating as well as the `[1,20]` attributes)
**Last Updated (prior):** October 20, 2026 (v0.14 — AR round 4, ERR-031-014: §5.7 gains the explicit FR-TX-001..029 traceability table; T-TX-BID-008 and the RNG scan restated to what is asserted)
**Last Updated (prior):** October 20, 2026 (v0.13 — AR round 3, ERR-031-013: T-TX-NEU-001 restated and re-locked; T-TX-DET-003, T-TX-BID-008, T-TX-REKEY-003 live; T-TX-REKEY-005 claims match what is asserted)
**Last Updated (prior):** October 20, 2026 (v0.12 — T-TX-REKEY-005 live)
**Last Updated (prior):** October 20, 2026 (v0.11 — ERR-031-011: window locks; T-TX-NORMAL-001 and §5.7 cover FR-TX-029 / F9)
**Last Updated (prior):** October 20, 2026 (v0.10 — T2 locks live)
**Last Updated (prior):** October 20, 2026 (v0.9 — ERR-031-006: T-TX-BID-005 and §5.7 restated for the stable-id re-home)
**Last Updated (prior):** October 19, 2026 (v0.8 — ERR-031-005: §5.3 re-home locks)
**Last Updated (prior):** October 18, 2026 (v0.7 — T1 locks live: T-TX-DET-001, T-TX-FAIL-001)
**Last Updated (prior):** September 14, 2026 (v0.6 — PR #407 residual review cleanup and T2 hook obligation)
**Last Updated (prior):** September 14, 2026 (v0.5 — PR #407 review corrections and stronger valuation/atomicity locks; prior v0.4 T0 football/Codex locks, v0.3 AR-6, v0.2 AR-3/AR-4, v0.1 initial)
**Version:** 0.16
**Status:** APPROVED

---

## 5.1 Unit — valuation & offer (KD-1)

- **T-TX-VAL-001** — `ValuePlayer` is deterministic, excludes weak foot, and is pinned by a literal golden vector:
  default 31×10 attributes, age 25 ⇒ `100000` under the default T0 config.
- **T-TX-VAL-002** — fractional attribute information is not discarded: raising exactly one canonical attribute
  from 10→11 changes the default golden value to `100322`, proving the base model is not quantised to 20 prices.
- **T-TX-VAL-003** — `AgeCurvePermille` has a strict young discount, `1000‰` at both peak boundaries, a strict
  post-peak decline, and reaches exactly `MinimumAgeMultiplierPermille` at sufficiently old age.
- **T-TX-VAL-004** — `ClubNeedMultiplierPermille`: below-neutral prospective stock > `1000‰`, neutral ==
  `1000‰`, maximum legal stock remains positive and below neutral; counter-band tuning remains in `(0,1000)`.
- **T-TX-VAL-005** — command-level positional need measures stock **excluding the negotiated player** on both
  buy and sell paths; equivalent zero-stock counterparty situations produce mirrored near-value counteroffers.
- **T-TX-OFF-001** — `EvaluateOffer` is draw-free and three-way: exact value accepts; inside the configured band
  counters; beyond it rejects, mirrored for buy/sell.
- **T-TX-OFF-002 (F6 / Codex P2)** — direct evaluator calls reject malformed wage/length terms.

## 5.2 Integration — the bid pipeline (KD-2/KD-7, atomic)

- **T-TX-BID-001** — accepted fair-value buy posts exactly one staged `{Debit,TransferFee,fee}` effect, commits
  spend, re-homes the player under his unchanged id, and inserts the contract keyed by it; `TransferBudget`/
  `WageBillAggregate` remain unchanged.
- **T-TX-BID-002** — accepted sell commits the roster, then removes the managed contract and applies the staged
  `{Credit,TransferFee,fee}` finance copy.
- **T-TX-BID-003 (F1)** — cumulative over-budget buy returns `TransferSubmissionOutcome.InsufficientBudget`,
  not an exception, and leaves finance/spend/roster/contracts unchanged for that attempt.
- **T-TX-BID-004 (F5)** — full destination returns `TransferSubmissionOutcome.SquadFull`, not an exception, with
  no local mutation.
- **T-TX-BID-005 (F2)** — if a producer violates the successful-preview contract with a commit that does not land
  (after `RequestRosterCommit`, `TryGetPlayer` does not report the player on the destination roster) on a **buy**,
  `SubmitBid` fails loud before staged finance/spend/contract state is applied. *(ERR-031-006: originally "returns a
  different id" — the commit returns nothing once ids are stable, ERR-031-005.)*
- **T-TX-BID-006 (F2)** — the same producer breach on a **sell** leaves finance unchanged and deliberately retains
  the old managed contract locally; the external roster may already have moved, which is why T2 must make the
  production preview→commit contract infallible rather than relying on rollback.
- **T-TX-BID-007** — counteroffer/reject are ordinary no-mutation `TransferSubmissionOutcome` values.
- **T-TX-BID-008** — #31 writes no `ClubFinances` field directly and keeps no parallel cash ledger. Live (AR round 3,
  ERR-031-013) as `TransfersT0Tests.SubmitBid_PostsOnlyThroughTheLedger_AndTheTransfersAssemblyHoldsNoFinanceState`:
  (a) an accepted buy's finance equals exactly `FinanceLedger.ApplyTransaction` of the same fee transaction (byte-compared
  through the #40 codec); (b) a reflection scan of every type in the assembly finds no `ClubFinances`- or
  `ClubFinanceEntry`-typed member (also when wrapped in an array, `Nullable<>` or any generic argument) and no
  `Balance`/`Cash`/`Ledger`/`Wallet`-named member; (c) an IL scan of every method body finds no `stfld`/`stsfld` to, and no
  `ldflda`/`ldsflda` (address taken for a `ref` write) of, a `ClubFinances` or `ClubFinanceEntry` field, and no whole-value
  `stobj`/`initobj` of either type outside `SubmitBid`'s single commit of the ledger-staged copy — violating probes for the
  store, the ref-write shape, the whole-value overwrite and the zeroing prove the scan can see each. The scan sees only those
  opcodes: a write through a pre-existing managed pointer or an array element of those types, or an unsafe/`Unsafe.*` store, is
  outside it, and a second whole-value store *inside* `SubmitBid` is not distinguished from the allowed one (leg (a) catches a
  changed result).
  Mutation-verified (a direct `finances.Balance = …` in `SubmitBid`, an `ldflda` ref-write bypass
  `ref long b = ref finances.Balance; b -= 1;`, and a `ClubFinances` field on `TransfersState`, each fail it). The **counterparty** club's #40 entry is not posted at the minimal tier — the recorded INTERIM
  option (b), not a decision (KD-2; the (a)-vs-(b) choice is an OPEN owner decision, §7.2, open-issues
  `transfer-counterparty-ledger`):
  `SeasonTransfersT2Tests.AnAcceptedTransfer_LeavesTheCounterpartyClubsLedgerUntouched_InTheInterimMinimalTier` pins that interim
  behaviour, and resolving the owner decision to (a) must rewrite it.
- **T-TX-BID-009** — `SubmitBid`'s gate order is observable, and the commit tail cannot fail after the irreversible roster
  commit (AR round 5, ERR-031-015). Live as `TransfersT0Tests.SubmitBid_GateOrder_SquadDepletedPrecedesSquadFull_F9BeforeF5`
  (a depleting release **and** a full destination ⇒ `SquadDepleted`, so the F9 gate runs before F5),
  `SubmitBid_GateOrder_UnknownCounterpartyPrecedesTheWindowGate_F6BeforeF4` (an unknown counterparty with the window also closed
  ⇒ `ArgumentOutOfRangeException`, not the F4 `InvalidOperationException`) and
  `SubmitBid_BuyWithAFullContractSet_FailsLoudBeforeTheRosterCommit` (a buy while the managed contract set already holds
  `CLUB_SQUAD_SIZE` contracts throws before `RequestRosterCommit`, with finance, spend, contracts and roster untouched —
  `InsertContract`'s own cap would otherwise throw after the commit). Mutation-verified: swapping the F9 and F5 gates fails the
  first, swapping F6 and F4 fails the second, and deleting the pre-check fails the third.

## 5.3 Roster re-home (KD-7; stable ids since ERR-031-005)

- **T-TX-REKEY-001** — *(restated at ERR-031-005)* a committed transfer keeps the player's id and moves his record
  to the destination roster; ownership is the roster's answer — a player whose id lies outside every packed range
  (a regen) is bought from the club whose roster carries him, and a direction that disagrees with roster ownership
  fails loud. Managed contract state is inserted after a confirmed buy commit or removed after a confirmed sell.
- **T-TX-REKEY-002** — *(restated)* the commit is confirmed through the port before local mutation; a commit that
  does not land fails loud with local state untouched (both directions).
- **T-TX-REKEY-003** — #31 migrates only its own contract state; #28/#33 state is not referenced. Live (AR round 3; an
  allow-list since AR round 5, ERR-031-015): `TransfersT0Tests.TheTransfersAssembly_ReferencesNoOtherSubsystemsPerPlayerState` —
  every `TacticalDirector.*` assembly the compiled assembly references is one of `ClubFinances`, `DeterministicSim`,
  `PlayerDatabase`, `ProjectConstants` (the four `transfers.asmdef` lists), so it names none of the assemblies that own
  PlayerId-keyed state (#28/#29/#41/#44/#33/#30) *or any other project assembly, present or future*; the contract
  insert/remove itself is locked by T-TX-REKEY-001. It is the structural lock FR-TX-011, FR-TX-013 and FR-TX-023 cite.
- **T-TX-REKEY-004** — positional-stock read is read-only and bounded; an invalid producer count fails loud.
- **T-TX-REKEY-005 (live since AR round 2; restated at ERR-031-005)** — a real #30 re-home MUST carry the player's
  whole career with him under the same id — #28 record + lifecycle, #29/#41/#30 rows, #44 tally — and leave nothing
  at the source club; the moved player is then selectable by, and records appearances for, his new club. Locked by
  `SeasonTransfersT2Tests.ABuy_ReHomesTheWholeCareer_UnderTheSameId_AndDebitsExactlyTheFee` (PA, `GrowthCursor`,
  `BirthWorldDay`, `RetirementFlag`/`RetirementDay`, the #28 `PlayerRecord` — id, names, age, position, every `[1,20]`
  attribute **and `WeakFootRating`** (a separate `[1,5]` scale that `PlayerAttributes.ToArray()` excludes, so the record
  leg compares it explicitly; AR round 5 S-L6) — the #29 training row, the #30 appearance row, the full #41
  `InjuryState` — severity, recovery, injury count and last-advanced day — and the #44 tally — yellow count and unserved
  ban — all equal at the buyer and absent at the source), `ASoldPlayer_IsSelectedByTheBuyersNextPlayedRound_AndIsGoneFromTheSellers`
  (the buyer's next played round fields him) and `ABannedPlayer_ServesHisBanAtTheBuyer_AndSitsOutThatClubsNextRound` (the
  #44 ban follows him — ERR-044-014: in a THREE-club league, where each round one club has a bye, his ban ticks exactly in
  the rounds the buyer plays and not in the rounds the club his id names plays, so whose ban a fixture serves is decided by
  the roster, never by the id; he sits out each of the buyer's rounds and the unbanned control is fielded).

## 5.4 Window (KD-6)

- **T-TX-WIN-001** — `IsWindowOpen` is inclusive at both boundaries; outside-window `SubmitBid` fails loud
  before valuation or mutation.
- **T-TX-WIN-002 (ERR-031-011)** — live at T2: `SummerWindowFor` opens on the begin day and closes exactly
  `SUMMER_WINDOW_IN_SEASON_DAYS` days into the season counting the opening fixture day (inclusive); an opening day
  before the begin day and a close past `uint.MaxValue` fail loud; the genesis defaults give `[0, 44]`
  (`TransfersT2Tests.SummerWindowFor_*`). Through `SeasonLoop`, a prompt roll opens on the roll day and is still open
  on the opening fixture day, a prompt and a late roll close on the same day, and `Close − openingFixtureDay` is
  identical at genesis and at a boundary (`SeasonTransfersT2Tests.APromptRoll…`, `…ALateRoll…`,
  `TheCloseSitsTheSameDistance…`).

## 5.5 Save round-trip & determinism (KD-4/KD-8)

- **T-TX-DET-001** — live at T1: `Contract` + `ClubTransferState` restore field-identical across a mid-window save,
  through both the codec (`TransfersT1Tests`) and the season file + `SeasonLoop.Restore` resume path
  (`SeasonSaveTransfersTests`), with non-default spend so a reseed-on-load could not pass.
- **T-TX-DET-002** — live at T2: contracts age/decrement/remove at the season boundary, a retiree's contract is
  removed and a managed regen contracted, spend resets, the window re-derives (open on the roll day, close anchored to the next opening fixture day —
  ERR-031-011), and the rolled
  career still saves (`TransfersT2Tests`, `SeasonTransfersT2Tests.TheRoll_…`); three boundaries expire every genesis
  contract without a throw and the manager can still sell (ERR-031-009).
- **T-TX-INIT-001** — live at T2: `League.CreateLoop` with a #28 store seeds one genesis contract per managed player
  and opens the window on the new-game day; a load never re-seeds (`ALoadNeverReSeeds_…`, after a sale, so a reseed
  could not pass).
- **T-TX-DEP-001 (F9, ERR-031-007)** — a release that would leave the releasing club below a matchday squad returns
  `SquadDepleted` with #40, #28, #31 and the career byte-identical.
- **T-TX-FIN-001 (§7.1 T2)** — through `SeasonLoop.SubmitTransferBid`, every non-accepted outcome (Rejected,
  CounterOffered, InsufficientBudget, SquadFull, SquadDepleted) leaves the live #40 entry byte-identical; an
  accepted buy debits exactly one fee; a raw `TransferFee` posting is refused once #31 is wired. The counterparty AI
  club's entry is not posted in either direction at the minimal tier — the recorded interim option (b), whose (a)-vs-(b)
  owner decision is open (KD-2, §7.2) — and stays byte-identical (`…LeavesTheCounterpartyClubsLedgerUntouched_InTheInterimMinimalTier`).
- **T-TX-DET-003** — two runs over identical transfer activity produce identical transfer state (FR-TX-027 MUST). Live
  (AR round 3, ERR-031-013): `SeasonTransfersT2Tests.TwoRunsOverIdenticalTransferActivity_ProduceByteIdenticalState` — two
  fresh careers replay one fixed script (an accepted buy, a counter-offered bid, an accepted sale, a played season, the roll)
  and end with byte-identical `TransfersSaveCodec` bytes, #40 entries and #28 snapshot, and an identical whole-career
  snapshot; the same career without the script ends elsewhere in all three (non-vacuity). Mutation-verified (a hidden
  call counter in the boundary's wage fails it).
- **T-TX-NEU-001** — no manager transfer action leaves pre-#31 season behavior unchanged; no RNG stream exists. Live,
  restated at AR round 3 (the T2 lock compared only the season state and #28 after a season with no roll, so it never ran
  step (d″) — the only #31 code a no-action career executes): `SeasonTransfersT2Tests.NoTransferAction_IsBehaviourNeutral`
  plays a season, `RollToNextSeason` and one season-2 round on a wired and an unwired career and compares everything but the
  #31 block — #40 entries, the #28 snapshot, #44, the #29/#41 rows, #30's appearance rows and the season state.
  Non-vacuity: `OneAcceptedSale_MovesTheProgressionSnapshot_SoTheNeutralityComparisonIsNotVacuous` — one accepted sale
  makes the wired career's `Progression.Snapshot()` differ from the unwired one's. Whether a transfer changes a *played*
  season is not asserted (the quick-sim's coarse rating model ignores any single sale); transfer sensitivity of play is
  witnessed by the buyer-selection and ban-served locks of §5.3. The "no RNG stream" half (FR-TX-016 / G3 — and only that) is asserted structurally by
  `TransfersT0Tests.TheTransfersAssembly_RegistersNoRngStream_NoMethodCallsOrConstructsAnRngType` (no method calls or constructs
  `System.Random`, `DeterministicRngService` or `RngStreamState`; a probe proves the scan can see one). It is not a proof of
  general determinism; `…ReadsNoClockOrAmbientEntropy_AndEmbedsNoSplitMixConstant` separately scans for wall-clock and entropy
  reads (`DateTime.Now`/`UtcNow`/`Today`, `DateTimeOffset.Now`/`UtcNow`, `Guid.NewGuid`, `Environment.TickCount`, any `Stopwatch` member,
  any `RandomNumberGenerator`/`RNGCryptoServiceProvider` member) and the SplitMix64 literals, each with a probe. The RNG-type scan
  also flags a call to any member that takes a `DeterministicRngService` or `RngStreamState` parameter, with a probe for each.
  Mutation-verified: a stray #40 posting in step (d″) fails the first.
- **T-TX-INT-001** — transfer state/valuation/spend fields are integer; no float/double state is introduced.

## 5.6 Fail-loud vs normal outcomes

- **T-TX-FAIL-001 (F3)** — live at T1: wrong magic (a sibling `FNCE` block), wrong version, every truncation,
  trailing bytes, `clubCount > 1`, contracts without a club, out-of-order ids, F6/F7 contract values, an inverted
  window and negative spend all throw at decode; a pinned golden byte vector locks the layout (ERR-031-003).
- **T-TX-FAIL-002 (F6)** — invalid player id or malformed fee/wage/length throws at consuming seams.
- **T-TX-FAIL-003 (F7)** — `default(Contract)` fails insertion validation.
- **T-TX-NORMAL-001 (F1/F5/F9)** — insufficient budget, full destination and a depleted releasing club are
  explicitly **not** fail-loud paths; they return typed no-mutation submission outcomes (`InsufficientBudget`,
  `SquadFull`, `SquadDepleted`).

## 5.7 Requirement traceability

Every FR-TX-001..029 maps to a T-TX-* test above, a recorded §7 deferral, or a structural enforcement — the explicit table
below (AR round 4, ERR-031-014: G15's FR → test direction was a bare assertion). Every test name in it was verified against `src/`.

| FR | Evidence |
|---|---|
| FR-TX-001 | T-TX-VAL-001/002: `TransfersT0Tests.ValuePlayer_IsDeterministic_WeakFootExcluded_AndGoldenVectorPinned` (golden `100000`, one-point `100322`); T-TX-INT-001: `T0StateAndCommands_ContainNoFloatOrDoubleFields` |
| FR-TX-002 | T-TX-VAL-003/004/005: `AgeCurvePermille_HasStrictDiscounts_PeakBoundary_AndFloor`, `ClubNeedMultiplier_ValuesScarcityAboveNeutralAndOverstockBelowNeutral` (also pins `ClubNeedPerPlayerPermille ≥ 1`; the loader's refusal of a configured 0 is not exercisable in-process — a `static readonly` bound once from the process-wide config), `SubmitBid_PositionalNeedExcludesNegotiatedPlayerSymmetrically`; T-TX-REKEY-004: `SubmitBid_InvalidPositionalStockFromThePort_FailsLoudWithoutCommit` |
| FR-TX-003 | T-TX-OFF-001: `EvaluateOffer_UsesAcceptedCounterAndRejectedBands_ForBuyAndSell`; T-TX-BID-007: `SubmitBid_CounterOffer_MutatesNothing`, `SubmitBid_RejectedBuy_MutatesNothing`; no draw — FR-TX-016's scan |
| FR-TX-004 | T-TX-BID-001/003: `SubmitBid_AcceptedBuy_PostsFeeOnly_CommitsSpend_AndInsertsContractUnderTheStableId`, `SubmitBid_SecondBuyOverStaticCeiling_ReturnsInsufficientBudgetWithoutSecondMutation` |
| FR-TX-005 | T-TX-BID-001/002: `…AcceptedBuy_PostsFeeOnly…`, `SubmitBid_Sell_CommitsRosterBeforeRemovingContract_AndCreditsFee`; T-TX-BID-008: `SubmitBid_PostsOnlyThroughTheLedger_AndTheTransfersAssemblyHoldsNoFinanceState`; T-TX-FIN-001: `SeasonTransfersT2Tests.EveryNonAcceptedOutcome_LeavesFinancesRosterAndContractsByteIdentical`, `ARawTransferFeePosting_IsRefusedOnceTransfersAreWired` |
| FR-TX-006 | T-TX-BID-001: `SubmitBid_AcceptedBuy_PostsFeeOnly_CommitsSpend_AndInsertsContractUnderTheStableId` (asserts `TransferBudget` unchanged by the posting); T-TX-BID-008 (no parallel ledger); T-TX-BID-003 (the ceiling is static, never decremented by `ApplyTransaction`) |
| FR-TX-007 | T-TX-BID-003; T-TX-DET-002: `SeasonTransfersT2Tests.TheRoll_AgesContracts_ResetsSpend_RedrawsTheWindow_AndKeepsTheCareerSaveable`; `TransfersT2Tests.PrepareSeasonBoundary_AgesRemovesExpiredAndRetired_SeedsArrivals_AndResetsSpendAndWindow` |
| FR-TX-008 | T-TX-BID-003: `SubmitBid_OverBudgetBuyWithExtremeDebt_ReturnsInsufficientBudgetBeforeFinanceStaging`; T-TX-NORMAL-001 |
| FR-TX-009 | T-TX-BID-001..007 (no-mutation of each ordinary outcome); T-TX-BID-009 (gate order: `SubmitBid_GateOrder_SquadDepletedPrecedesSquadFull_F9BeforeF5`, `SubmitBid_GateOrder_UnknownCounterpartyPrecedesTheWindowGate_F6BeforeF4`; the full-contract-set pre-check: `SubmitBid_BuyWithAFullContractSet_FailsLoudBeforeTheRosterCommit`); T-TX-DEP-001; T-TX-WIN-001: `SubmitBid_OutsideWindow_FailsBeforeValuationOrMutation`; T-TX-FIN-001: `EveryNonAcceptedOutcome_…` |
| FR-TX-010 | T-TX-OFF-001/002 (the evaluator takes a caller-supplied valuation: `EvaluateOffer_MalformedWageOrLength_FailsLoudAtReusableSeam`); structural: `TransferSubmissionOutcome` and `NegotiationOutcome` are distinct types |
| FR-TX-011 | structural: `TacticalDirector.Transfers` references only ClubFinances, DeterministicSim, PlayerDatabase and ProjectConstants (its asmdef), so no #32/#34 type can be named — enforced by T-TX-REKEY-003's allow-list (`TheTransfersAssembly_ReferencesNoOtherSubsystemsPerPlayerState`); the staff influence is the §7.2 deferral (identity constant `IDENTITY_MULTIPLIER_PERMILLE`) |
| FR-TX-012 | T-TX-DET-001: `TransfersT1Tests.Encode_PinsTheAppendixBLayout_ByteForByte`; `SeasonSaveTransfersTests.TheTransfersBlock_SitsInItsOwnFrameSlot` |
| FR-TX-013 | structural: the transfers asmdef does not reference LivingWorld (T-TX-REKEY-003's allow-list excludes it), so #31 cannot touch `WORLD_STORE_FORMAT_VERSION`; durable contracts live in the season-save block (`TheTransfersBlock_SitsInItsOwnFrameSlot`) |
| FR-TX-014 | T-TX-FAIL-001: `TransfersT1Tests.Decode_ASiblingSubBlob_IsRefusedAtTheMagic`, `Decode_AnotherGeneration_IsRefused`, `Decode_EveryTruncation_IsRefusedLoud`, `Decode_TrailingByte_IsRefused` |
| FR-TX-015 | §7.2 deferral (deep-tier fields append behind `deepTransfersEnabled`); the minimal schema is byte-pinned by `Encode_PinsTheAppendixBLayout_ByteForByte` |
| FR-TX-016 | `TransfersT0Tests.TheTransfersAssembly_RegistersNoRngStream_NoMethodCallsOrConstructsAnRngType` (G3); `…ReadsNoClockOrAmbientEntropy_AndEmbedsNoSplitMixConstant` |
| FR-TX-017 | §7.1/§7.2 deferral (stochastic rival bidding is the first #31 draw site; no deep-tier draw exists at minimal) |
| FR-TX-018 | T-TX-DET-001: `TransfersT1Tests.Encode_PinsTheAppendixBLayout_ByteForByte` (the pinned layout carries no cursor field) |
| FR-TX-019 | T-TX-WIN-002: `TransfersT2Tests.SummerWindowFor_OpensOnTheBeginDay_AndClosesInclusiveFromTheOpeningFixtureDay`, `SeasonTransfersT2Tests.APromptRoll_OpensOnTheRollDay_AndCoversTheOpeningFixtureDay`, `APromptRollAndALateRoll_CloseTheWindowOnTheSameDay` |
| FR-TX-020 | T-TX-WIN-001: `TransfersT0Tests.TransferWindow_IsInclusiveAtBothEdges`, `SeasonTransfersT2Tests.ABidOutsideTheWindow_FailsLoud_WithNoMutation` |
| FR-TX-021 | T-TX-REKEY-001/002: `SubmitBid_PlayerWhoseIdIsOutsideHisClubsPackedRange_IsOwnedByTheRoster`, `SubmitBid_RosterCommitThatDoesNotLand_FailsBeforeLocalStateMutation`; T-TX-REKEY-005: `SeasonTransfersT2Tests.ABuy_ReHomesTheWholeCareer_UnderTheSameId_AndDebitsExactlyTheFee` |
| FR-TX-022 | T-TX-BID-004: `SubmitBid_FullDestination_ReturnsSquadFullWithoutFinanceOrContractMutation`, `SeasonTransfersT2Tests.AFullDestination_IsSquadFull_AndADepletedSeller_IsSquadDepleted` |
| FR-TX-023 | T-TX-REKEY-001/003/005: `…ABuy_ReHomesTheWholeCareer…`, `TransfersT0Tests.TheTransfersAssembly_ReferencesNoOtherSubsystemsPerPlayerState`; T-TX-BID-001 (the contract is keyed by the unchanged id) |
| FR-TX-024 | T-TX-NEU-001: `SeasonTransfersT2Tests.NoTransferAction_IsBehaviourNeutral`, `OneAcceptedSale_MovesTheProgressionSnapshot_SoTheNeutralityComparisonIsNotVacuous` |
| FR-TX-025 | T-TX-BID-001..007; T-TX-WIN-001; `SeasonTransfersT2Tests.ARawTransferFeePosting_IsRefusedOnceTransfersAreWired` (the command is the only path); `SeasonSaveTransfersTests.TheLoopClonesItsState_SoTheCallersReferenceIsNotASecondWriter` |
| FR-TX-026 | T-TX-FAIL-002/003: `TransfersT0Tests.SubmitBid_MalformedOffer_FailsLoud`, `SubmitBid_AnUnknownCounterpartyClub_FailsLoud_BeforeAnyValuation`, `EvaluateOffer_MalformedWageOrLength_FailsLoudAtReusableSeam`, `InsertContract_DefaultContract_FailsLoud` |
| FR-TX-027 | T-TX-DET-001: `TransfersT1Tests.RoundTrip_NonDefaultMidWindowState_IsFieldIdentical`, `SeasonSaveTransfersTests.SaveLoad_ThroughTheLoop_RoundTripsNonDefaultTransfersState_AndResumesIt`; T-TX-DET-003: `SeasonTransfersT2Tests.TwoRunsOverIdenticalTransferActivity_ProduceByteIdenticalState` |
| FR-TX-028 | T-TX-INIT-001: `SeasonTransfersT2Tests.CreateLoop_WithAProgressionStore_SeedsOneGenesisContractPerManagedPlayer`, `ALoadNeverReSeeds_TheResumedLoopCarriesTheSavedContracts`, `TransfersT2Tests.CreateAtGenesis_SeedsOneContractPerPlayer_AtTheGenesisTerms`; T-TX-DET-002: `TheRoll_AgesContracts_ResetsSpend_RedrawsTheWindow_AndKeepsTheCareerSaveable` |
| FR-TX-029 | T-TX-DEP-001: `TransfersT0Tests.SubmitBid_AReleaseThatDepletesTheSellingSquad_ReturnsSquadDepleted_WithNoMutation`; T-TX-NORMAL-001; T-TX-FIN-001: `SeasonTransfersT2Tests.AFullDestination_IsSquadFull_AndADepletedSeller_IsSquadDepleted` |


Every T-TX-* id in this section
now has a live test (AR round 3 landed T-TX-BID-008, T-TX-DET-003 and T-TX-REKEY-003, which G15 had counted without one). T0 now locks the reviewed public
API shape, precise integer valuation, symmetric positional need, typed normal command outcomes, and both sides
of the defensive commit-not-landed breach. T2 owed — and AR round 2 landed — the production carry-everything re-home lock in
T-TX-REKEY-005 (the re-home carries the player's #28 record + lifecycle, #29/#41/#30 rows and #44 tally under the
same id and leaves nothing behind). *(ERR-031-006: originally "the production hook-dispatch/no-op lock" — the
re-key hook was deleted at ERR-031-005.)*

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §5 test plan; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3/AR-4 fee-only, aging/seeding and re-key coverage updates. |
| 0.3 | 2026-07-23 | — | AR-6 load-vs-genesis and managed↔AI hook corrections. |
| 0.4 | 2026-09-14 | — | T0 close-out: positional scarcity, counter band, reusable evaluator validation and no-mutation negotiation coverage. |
| 0.5 | 2026-09-14 | — | PR #407 review correction: literal golden/fractional valuation vectors, strict age boundaries, symmetric exclude-player need, typed budget/full outcomes, and preview-mismatch-before-local-mutation regression. |
| 0.6 | 2026-09-14 | — | Residual review cleanup: add sell-direction preview/commit breach coverage and pin the T2 production hook-dispatch plus observable managed↔external #31 no-op regression. |
| 0.7 | 2026-10-18 | — | T1 landing: T-TX-DET-001 and T-TX-FAIL-001 move from deferred to live, with the golden vector and sibling-magic refusal (ERR-031-003). |
| 0.8 | 2026-10-19 | — | ERR-031-005: §5.2/§5.3 restated for a stable-id re-home — T-TX-REKEY-001 locks roster-judged ownership including an off-range (regen) id; T-TX-REKEY-002 confirms the landing; T-TX-REKEY-005 becomes the T2 carry-everything integration lock. |
| 0.9 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): T-TX-BID-005 restated as "a commit that does not land" (was "returns a different id"); §5.7's T2 obligation is the T-TX-REKEY-005 carry-everything lock, not a hook-dispatch/no-op lock. The v0.6 row's hook-dispatch pin is superseded. |
| 0.10 | 2026-10-20 | — | #31 T2: T-TX-DET-002 / T-TX-INIT-001 / T-TX-NEU-001 live; new T-TX-DEP-001 (ERR-031-007) and T-TX-FIN-001 (the #40 staging seam). |
| 0.11 | 2026-10-20 | — | ERR-031-011 (AR round 2): new T-TX-WIN-002 locks the re-anchored window (unit edges + roll-timing independence through `SeasonLoop`); T-TX-DET-002 restated; T-TX-NORMAL-001 adds F9 `SquadDepleted`; §5.7 counts FR-TX-001..029. T-TX-REKEY-005's "(deferred T2)" label is left for a later pass. |
| 0.12 | 2026-10-20 | — | AR round 2: T-TX-REKEY-005 is live (named tests: whole-career equality at the buyer and absence at the source, buyer selection, #44 ban served by the new club); the "(deferred T2)" label and the §5.7 "owes" wording are retired. |
| 0.13 | 2026-10-20 | — | ERR-031-013 (AR round 3): T-TX-NEU-001's T2 lock never rolled, compared only `SeasonState` and #28, and compared a played season to an unplayed `NewCareer` for "non-vacuity" — restated and rewritten as played season → roll → season-2 round, the whole career snapshot minus the #31 block, wired vs unwired, with a one-sale non-vacuity leg; the unasserted "no RNG stream" claim is now asserted structurally. T-TX-DET-003 (FR-TX-027), T-TX-BID-008 and T-TX-REKEY-003 go live (no test existed for them although G15 was ✅). T-TX-REKEY-005 now compares the full `InjuryState` and a yellow-count tally, and its ban test runs a three-club league so "decided by the roster, never by the id" is observable. T-TX-FIN-001 records the unposted counterparty ledger. |
| 0.14 | 2026-10-20 | — | ERR-031-014 (AR round 4, R4-L4/R4-L1): §5.7 asserted that every FR maps to a test but listed only a few; it now carries the explicit 29-row table (every cited test name grep-verified against `src/`). T-TX-BID-008 (c) also scans `stsfld`/`ldflda`/`ldsflda` and (b) unwraps `Nullable<>`/generic arguments; the RNG-type scan is renamed `…RegistersNoRngStream…` and its claim narrowed to FR-TX-016/G3, with a separate clock/entropy/SplitMix-literal scan. |
| 0.15 | 2026-10-20 | — | AR round 5 (S-L6, ERR-031-014 note): T-TX-REKEY-005 names the #28 `PlayerRecord` leg (id, names, age, position, every `[1,20]` attribute) and adds `WeakFootRating`, which `PlayerAttributes.ToArray()` excludes; the lock is `SeasonTransfersT2Tests.AssertSameRecord`. No requirement changed. |
| 0.16 | 2026-10-21 | — | ERR-031-015 (AR round 5, L-A/D/E/G): the unposted counterparty is the recorded interim option (b), not "by design" — `…_ByDesign` renamed `…_InTheInterimMinimalTier` in BID-008 and FIN-001 (owner decision open, §7.2); new T-TX-BID-009 locks the F9-before-F5 and F6-before-F4 gate order and the full-contract-set pre-check (each mutation-verified); T-TX-REKEY-003 becomes an allow-list of the four asmdef references; the BID-008 IL-scan wording states only the opcodes it sees and gains a stobj/initobj whole-value scan; the NEU-001 scan covers every `Stopwatch` and `RandomNumberGenerator`/`RNGCryptoServiceProvider` member and any member taking a `DeterministicRngService`/`RngStreamState` parameter; §5.7 re-points FR-TX-006 to T-TX-BID-001 (where the `TransferBudget`-unchanged assertion lives), FR-TX-009's gate order to T-TX-BID-009, and FR-TX-011/013 to REKEY-003. The header `Version` had lagged the history (0.14 against rows to 0.15) and now reads 0.16. |
#endregion
