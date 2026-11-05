# §5-P8 AR-2 verification evidence

**Created:** November 5, 2026\
**Last Updated:** November 5, 2026 (v1.6 — AR-1 M1 save-resume capability refusal, DOC-L1–L4 repairs and executed verification; fresh full adversarial verification pending.)
**Last Updated (prior):** November 5, 2026 (v1.5 — T6: executed display-boundary mutation guards; rejected-wiring edit and Unity verification limits.)
**Last Updated (prior):** November 5, 2026 (v1.4 — T5: UX authority, current implementation/citation sync and T1–T5 close-out evidence.)
**Last Updated (prior):** November 5, 2026 (v1.3 — T4: #38 dependency rationale, lifecycle scope and appendix version collision corrected.)
**Last Updated (prior):** November 5, 2026 (v1.2 — T3: executed five projection, Home, cache, order, saturation and S0 identity guards; test hygiene.)
**Last Updated (prior):** November 5, 2026 (v1.1 — T2: executed failed-entry, Quit and localized-copy guards; full ClientApp result.)
**Last Updated (prior):** November 5, 2026\
**Version:** 1.6\
**Author:** —\
**Purpose:** Preserve executed behavior guards and non-certifying Linux verification for the six ordered AR-2 tasks and subsequent AR-1 review fixes.

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

## T6 — display and rejected-wiring hardening

The generated solution build passes with zero errors (five existing warnings). The focused formatting
set passes 56 tests; the restored positive set passes after the four temporary mutants below.
Full ClientApp: 269 passed, 0 failed, 3 skipped.

| Temporary mutation | Guard exercised | Observed result |
|---|---|---|
| Remove budget-header clamp | `DisplayedRemainingBudget_ClampsAtZeroInEveryMoneySlot` | Negative remaining budget appeared. |
| Remove insufficient-budget outcome clamp | Same test, exact outcome copy | Negative remaining budget appeared. |
| Remove finances remaining-budget clamp | Same test, full finances comparison | Negative remaining budget appeared. |
| Restore checked uint-to-int close-day conversion | `WindowCloseDay_FormatsTheFullUnsignedRange` | uint.MaxValue threw instead of formatting. |

All four mutants were killed and restored (32 across T1–T6). The two Unity shell guards were inspected
statically: ApplyCurrentScreen returns after rejection or before coordinator initialization; Update does
not refresh screens after ApplyCurrentScreen rejects wiring. This Unity binding is excluded from the
Linux generated solution. Editor compilation, Console, root-visibility runtime and target player build
remain unverified; no Unity readiness or pinned-host timing certification is claimed.

## AR-1 review fixes — M1 and DOC-L1–L4

Fresh independent Codex reviewers substituted explicitly for unavailable named Claude tiers. The
round found 0 High / 1 Medium / 4 Low. M1 and DOC-L1–L4 are fixed and validated here; this is not a
clean-review claim. Fresh full adversarial verification of the final artifact is pending.

M1 reproduced a supported current-format save with optional #31 absent: Continue succeeded but
unconditional owner Diagnose published raw LastError instead of the existing TransfersUnavailable.
The coordinator v1.7 now reads live loop capability on the serialized worker before Diagnose/Submit;
missing #31 state or #28 roster authority yields the typed refusal. Wired sessions keep owner
Diagnose → client upper wage ceiling → mutation. There is no blanket catch of invariant exceptions.
Appendix C.2 v0.7 distinguishes these existing responsibilities under ERR-038-006; no #31 domain issue,
owner wage rule, save format or schema change is introduced.

`ClientCareerCoordinatorTests` v1.8 adds
`ResumedOptionalEmptyTransfers_IsTypedUnavailableWithoutStateChange(false/true)`. Both cases write a
normal current-format manual save using public CareerSaveSlots.Write, omit only optional #31, decode
and Continue it, and submit a real offer. The second retains a previously wired market after an
injected projection failure. Both assert a typed refusal, null operation/projection errors, preserved
offer, no outcome, unchanged Home/finance facts, and continued manual-save usability with byte-identical
full serialized state. The retained view cannot authorize capability.

| Temporary mutation | Guard exercised | Observed result |
|---|---|---|
| Remove the whole live capability guard | Both new optional-empty resume cases | Both fail the HasValue behavior assertion; 2 failed / 0 passed / 0 skipped. |

The single new mutant was killed (two failing test cases), restored byte-for-byte in finally, and the
restored focused set passed. T1–T6 retain their historical total of 32; the cumulative total is now 33.

- Focused ClientApp command: 58 passed / 0 failed / 0 skipped; restored rerun: 58 / 0 / 0.
- Generated solution build: 0 errors, 2 emitted pre-existing warnings (unused BallIntegrationTests
  radius; unassigned PassMechanicsTests Pressure). This is non-certifying Linux shim compilation.
- Full ClientApp: 271 passed / 0 failed / 3 skipped (269 → 271 passed, two new cases).
- Metadata integrity: PASS. Documentation consistency, landing drift and git diff whitespace: PASS.
- Transfers 79 / 0 / 0 and SeasonSave 553 / 0 / 4 remain prior T1 full-suite results; neither was rerun
  for this client/documentation fix. No whole-tree test gate was executed in this review-fix pass.

DOC-L1 restores the historical v1.47 paragraph verbatim from c05cbe5 and moves v2.99/v2.100/v2.101 into
the correct top Updated chain beneath v2.102. DOC-L2 adds bodies for ERR-031-016/017, ERR-030-059 and
ERR-038-006 without allocating replacement IDs. DOC-L3 corrects the snapshot-hint/live-owner/wage route
while keeping ERR-031-016's owner decision Open. DOC-L4 completes #30 §4 v1.7's optional cloned transfer
holding, constructor/Restore coherence, internal persistence bridge, detached snapshot and public bid
mutation inventory under the same incomplete back-propagation ERR-030-059. Current inventory/citation
pointers and all three closeout chains are synchronized; their historical entries remain preserved.

No match-engine, snapshot/save schema, RNG stream/domain/draw-site/order or production simulation rule
changed. Existing goal-rate scenarios, calibration and certified timing were not remeasured: the fix
changes refusal routing before simulation mutation. Unity Editor compilation, Console/runtime,
root visibility, target-player build and pinned-host timing remain unverified.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-11-05 | — | T1 executed owner, worker and headroom mutation checks; Linux/Unity boundary recorded. |
| 1.1 | 2026-11-05 | — | T2: executed failed-entry, Quit and localized-copy guards; full ClientApp result. |
| 1.2 | 2026-11-05 | — | T3: executed five projection, Home, cache, order, saturation and S0 identity guards; test hygiene. |
| 1.3 | 2026-11-05 | — | T4: #38 dependency rationale, lifecycle scope and appendix version collision corrected. |
| 1.4 | 2026-11-05 | — | T5: UX authority, current implementation/citation sync and T1–T5 close-out evidence. |
| 1.5 | 2026-11-05 | — | T6: executed display-boundary mutation guards; rejected-wiring edit and Unity verification limits. |
| 1.6 | 2026-11-05 | — | AR-1 M1 save-resume capability refusal and one killed/restored mutant; DOC-L1–L4 repairs; full ClientApp 271/0/3; fresh full adversarial verification pending. |
