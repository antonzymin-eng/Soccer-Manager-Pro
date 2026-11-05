# §5-P8 AR-2 verification evidence

**Created:** November 5, 2026\
**Last Updated:** November 5, 2026 (v1.1 — T2: executed failed-entry, Quit and localized-copy guards; full ClientApp result.)
**Last Updated (prior):** November 5, 2026\
**Version:** 1.1\
**Author:** —\
**Purpose:** Preserve executed behavior guards and non-certifying Linux verification for the six ordered AR-2 tasks.

The source base is `c05cbe5` on `work`. Each mutant below was applied temporarily, compiled, and
executed against the named test; a compilation error was not accepted as a killed mutant. The original
source was restored in a `finally` block and the positive regression was rerun. No match-engine file,
RNG behavior, schema or save format changed. Unity 6000.4.9f1 compilation/runtime remains unverified here.

## T1 — owner preflight and headroom

- Generated solution build: 0 errors; five existing warnings outside this slice.
- Transfers: 79 passed, 0 failed, 0 skipped.
- ClientApp: 253 passed, 0 failed, 3 skipped (full suite, including the existing four-match S0 repeatability/observer test).
- SeasonSave: 553 passed, 0 failed, 4 skipped (full suite, including the existing multi-fixture engine scenario).
- Metadata integrity and documentation consistency: PASS.
- Configured pre-commit policy (bootstrap cache-preparation mode): PASS; quarantine empty. The cloud run exceeded 60 seconds, so no certified pre-commit timing claim is made.

| Temporary mutation | Guard exercised | Observed result |
|---|---|---|
| Each issue return changed to None: WindowClosed, ManagedCounterparty, UnknownCounterparty, UnknownPlayer, WrongDirection, FeeNegative, WageNegative, LengthTooShort | `Diagnose_IsReadOnly_AndAgreesWithSubmitBid` (all nine parameter cases) | Each of the eight mutants failed a behavior assertion. |
| Valid-owner branch returns WrongDirection | Same owner diagnosis test | Failed the None/valid-offer case. |
| Headroom returns the ceiling without subtracting spend | `RemainingBudget_AfterBuy_IsTheHeadroomUsedBySubmitBid` | Failed the post-buy remaining-budget assertion. |
| Worker skips `issue != None` refusal branch | `StaleMarket_RepeatedCompletedSale_IsTypedRefusalWithoutStateChange` | Failed typed refusal after a completed sale and failed market projection. |
| SeasonLoop diagnosis wrapper returns None | `TransferObserverWrappers_UseLiveOwnershipAndOwnerHeadroomWithoutMutation` | Failed live ownership assertion after the buy. |
| SeasonLoop headroom wrapper passes unwired/null state | Same observer-wrapper test | Failed remaining budget after the buy. |

All 13 mutants were killed; the restored stale-sale and typed-refusal tests passed (2/2).
The restored observer-wrapper test passed (1/1), and the final client career/transfer subset passed.
The stale-sale regression also checks LastError null, unchanged balance/contracts/committed fees,
and a recovered market with only one player removed.

## T2 — visible projection errors and consistent entry state

ClientApp full suite: 257 passed, 0 failed, 3 skipped. Focused coordinator/copy tests: 38 passed.
The restored positive set passed after all three mutants. The old public coordinator constructor is
preserved; a second overload admits the host's localized CareerScreenText.

| Temporary mutation | Guard exercised | Observed result |
|---|---|---|
| Remove the failed-entry holder Clear | `FailingEntryHomeProjection_ReleasesTheCareerAndPreservesTheAutosave` (Start, Load, Continue) | Fresh projection after a stopped retry exposed the incorrectly retained career. |
| Remove Quit's LastProjectionError reset | `QuitToMainMenu_ClearsTheProjectionFailure` | The projection error survived Quit. |
| Replace injected entry copy with a new base-locale formatter | `FailedEntry_UsesTheInjectedLocalizedMessage` | The admitted translated message was lost. |

The entry tests retain the original screen, clear career snapshots, preserve the autosave bytes and
verify Continue remains available. The Unity notice-chain edit was statically inspected; pinned-host
compilation/runtime is still pending.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-11-05 | — | T1 executed owner, worker and headroom mutation checks; Linux/Unity boundary recorded. |
| 1.1 | 2026-11-05 | — | T2: executed failed-entry, Quit and localized-copy guards; full ClientApp result. |
