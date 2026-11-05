# §5-P8 AR-2 verification evidence

**Created:** November 5, 2026\
**Last Updated:** November 5, 2026 (v1.4 — T5: UX authority, current implementation/citation sync and T1–T5 close-out evidence.)
**Last Updated (prior):** November 5, 2026 (v1.3 — T4: #38 dependency rationale, lifecycle scope and appendix version collision corrected.)
**Last Updated (prior):** November 5, 2026 (v1.2 — T3: executed five projection, Home, cache, order, saturation and S0 identity guards; test hygiene.)
**Last Updated (prior):** November 5, 2026 (v1.1 — T2: executed failed-entry, Quit and localized-copy guards; full ClientApp result.)
**Last Updated (prior):** November 5, 2026\
**Version:** 1.4\
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

## T3 — test gaps and hygiene

ClientApp full suite: 266 passed, 0 failed, 3 skipped. The focused positive set passed 84 tests,
including the same restored set after the mutants. Production code is unchanged by this task.

| Temporary mutation | Guard exercised | Observed result |
|---|---|---|
| Force each failed projection's assignment (Home, NewCareer, Load, TransferMarket, Finances) | `FailedProjection_PreservesItsNonDefaultView` (five cases) | All five mutants failed a non-default retained-field assertion. |
| Remove failed-Home unblocking | `FailedHomeAfterProgress_PreservesCareerAndUnblocksProgress` | Progress remained blocked. |
| Remove the long cache's value comparison | `LongCell_ChangedValueReformats_RepeatReturnsTheSameInstance` | 6L incorrectly reused 5L text. |
| Remove the UI hint's window check, then its fee check | `HintCheckOrder_WindowAndFeePrecedeTheWagePolicy` | Each mutant lost the promised first issue. |
| Change wage saturation > to >= | `ContractWageSum_ExactlyLongMaxIsExact_OneMoreUnitSaturates` | Exact long.MaxValue was incorrectly flagged. |
| Remove the saturated flag assignment | Same wage-boundary test | One extra unit was not flagged. |
| Change the S0 minute role from I to L | `LoadedCompiledCatalogueHasStableManifestAndEveryRoleIsCovered` | The new ui.s0 argument-type identity assertion failed. |

All 12 mutants were killed and restored. The dead FirstManagedPlayer helper was removed; headers now
name nine screens and §5-P8; the harness and shutdown threads are background threads, preserving timeout
assertions while allowing a failed test host to terminate.

## T4 — #38 bookkeeping

Documentation only. FR-UI-010 and Appendix C.2 state the actual dependency choice and define a career
command as running-loop mutation. C.2 explicitly lists P7b save/load/preview/creation operations outside
that definition; §7 qualifies intent/coordinator routing and removes transfer from missing seams.
The original appendix amendment is correctly v0.4, T1's revision v0.5, and this correction v0.6.
The ERR-038-006 row carries the corrected rationale, worker diagnosis and version sequence.
Documentation consistency and drift checks pass; production/test source is identical to the passed T3 tree.

## T5 — documentation authority and close-out

The detailed UX plan v1.37 owns the current S2 status; the journey supplement v0.4 supplies audit/design
evidence without overriding it. §5-P8 (client plan v0.42) describes the T1–T4 implementation. Maintained
currency chains in open issues, roadmap v0.97 and the file manifest were synchronized. The ERR-031-016
original client guard count is five; T1's worker policy is separately named. The unsupported original
per-test pre-fix execution claim is withdrawn; actual AR-2 mutation checks remain recorded above.

Docs only. Documentation consistency and drift pass. Latest full source results remain Transfers79/0/0,
ClientApp266/0/3, SeasonSave553/0/4 (passed/failed/skipped). The optional broader recurring-defect lint
still reports 82 pre-existing errors; T4 removed its pre-existing #38 duplicate-version error.
No new source/header lint error was introduced. Unity Editor compile and UI runtime remain unverified.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-11-05 | — | T1 executed owner, worker and headroom mutation checks; Linux/Unity boundary recorded. |
| 1.1 | 2026-11-05 | — | T2: executed failed-entry, Quit and localized-copy guards; full ClientApp result. |
| 1.2 | 2026-11-05 | — | T3: executed five projection, Home, cache, order, saturation and S0 identity guards; test hygiene. |
| 1.3 | 2026-11-05 | — | T4: #38 dependency rationale, lifecycle scope and appendix version collision corrected. |
| 1.4 | 2026-11-05 | — | T5: UX authority, current implementation/citation sync and T1–T5 close-out evidence. |
