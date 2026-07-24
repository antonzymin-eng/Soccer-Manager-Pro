# SPEC_INDEX.md — Canonical Specification Registry

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** July 24, 2026, latest same day (**Discipline & Suspensions #44 authored + advanced `→ IN
> REVIEW → APPROVED`** — Wave 5's second spec (season-level card accumulation/thresholds/bans as a
> **read-only derivation** over already-emitted engine events). **The load-bearing verifications:** a
> promoted second yellow publishes **ONE `CardIssuedEvent` with `CardKind = 2`** (never a yellow-then-red
> pair — `ApplyCardAndCheckSentOff`, the KD-5 de-dup rule); `Recipient` is a match **agent id**, and the
> engine's slot discipline resets on substitution (v1.33) — so the read is a **tick-ordered occupancy
> fold** (lineup + `SubstitutionEvent`s → the occupant at the card's tick), fed by the **#37-class per-tick
> ledger tap** (FR-AN-002, the approved observational pattern — one tap feeds #37+#44); `SerializeLedger`
> is write-only and no ledgers are retained, so the tally **persists** (`DISCIPLINE_SAVE_FORMAT_VERSION`
> sub-blob — recompute is impossible, KD-1 forced). **Availability is a VIEW** (pure predicate + reduced
> value-copy squads; #27 never written); **live at minimal** (the #41 class — a ban legitimately changes
> the next lineup; neutrality = observer-neutrality + no-trigger identity + determinism). Hygiene:
> **bans MIGRATE on a transfer re-key** (they follow the player — the recorded contrast with #32's
> drop-on-transfer knowledge rule); the `(PlayerId, CompetitionId)` key pre-shapes #43 partitions;
> immediate `(0,0)`-entry drop (canonical representation). **No RNG stream / domain tag / ordinal**
> (the #37/#49 read-only positive property — no #16 row). **One approval-time back-prop:** ERR-030-009 —
> the #30 FR-SN-013 **availability-filter null seam** (resolve → *filter* → configure;
> `season-competition-loop` section-2/3 v0.8; `spec-error-log.md` v1.40). Supplement AR-1 (2M+1L) → AR-2
> (2L) → CONVERGENCE; section-file AR PASS-1 (1M) → PASS-2 (2L) → CONVERGENCE. Count: **41 APPROVED / 0
> IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 24, 2026, later same day (**Competition Structure #43 authored + advanced `→ IN
> REVIEW → APPROVED`** — Wave 5's first spec (cups/continental/promotion-relegation over #30's loop).
> **KD-1:** a league IS a competition instance (`CompetitionFormat.RoundRobin`); **instance 0 is a binding
> row** — an id/tag recording "the league lives in #30", no stored #30 object (FR-SN-032/033 respected), so
> the minimal singleton collection executes **no code on the season path** and a season is byte-identical
> to bare #30. **KD-2 (headline revision):** knockout/group draws are **position-independent keyed draws**
> (`competition.draws`, `entityId = competitionId`, fixed-radix ordinals over `(seasonNumber, roundIndex,
> slotIndex, purpose)`) — the plan's serialized-cursor proposal dropped (a match-tick pattern that would
> race across same-day competitions); nothing RNG-serialized. **KD-3:** brackets persisted
> (serialize-don't-regenerate) with fail-loud coherence gates; a restore never re-rolls. **KD-4:**
> promotion/relegation is a membership-only transform at #30's **pre-declared (a')** (FR-SN-031), before
> #40's (b') — `ClubId`s never re-key; the code-side (a') hook + deep fixture-day driver are soft-reserved
> **ERR-030-008** T-phase coordinations. **KD-7:** canonical ascending-`ClubId` order at every draw-feeding
> surface (keyed Fisher–Yates; shuffled-input equivalence locked). **One approval-time back-prop:**
> ERR-043-001 — the #16 §3.4 **A-04 placeholder sweep** (`_RESERVED_0x2B_` #42 / `_RESERVED_0x2C_` #43 /
> `_RESERVED_0x2D_` #45, completing the roadmap §6 block `0x20`–`0x2D`; `deterministic-sim/section-3.md`
> v1.0.14, `spec-error-log.md` v1.39); `0x2C` stays reserved (draw-free minimal). **No #30/#40 change** —
> #43 is the first management spec whose #30 spec-text seams (the (a') point, the §7 generalization row)
> were all reserved ahead. Supplement AR-1 (2M+1L) → AR-2 (2L) → CONVERGENCE; section-file AR PASS-1
> (1M+1L) → PASS-2 clean → CONVERGENCE. Count: **40 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 24, 2026 (**Scouting & Player Knowledge #32 authored + advanced `→ IN REVIEW →
> APPROVED`** — Wave 4's third and final spec (per-manager attribute fog-of-war over #27 truth; scout
> assignments/reports/recommendations). **The governing invariant** (roadmap §5): knowledge is a **VIEW over
> #27's true attributes, NEVER a mutation** — enforced structurally (`EstimateFor` takes `in PlayerRecord`
> value copies via `ISquadProvider`; no storage reference; readonly view types; the T-SC-VIEW-001
> byte-identity lock). **KD-1 (headline):** the overlay stores only a per-player **knowledge band**; the
> per-attribute `[Min,Max]` ranges are **derived on read** (band → strictly-decreasing `[GT]` half-width
> table, terminal 0 ⇒ `BAND_MAX` collapses to `[truth, truth]` arithmetically) re-centred by **stateless
> keyed noise** on `(playerId, band, attrIdx)` — deliberately NOT `worldDay`-keyed (stable until a band
> advance), no cursor, nothing RNG-serialized — dissolving the plan's save-bloat + re-roll risks by
> construction; freshness is the pinned **live-form window** semantic (width is the scouted quantity).
> **KD-2:** own-squad omniscience (managed-club players always `BAND_MAX`); fog covers external players' 31
> attributes only. **KD-3/KD-6:** minimal = fog-off omniscient identity, **draw-free** (zero-width reads
> short-circuit before any draw) ⇒ `_RESERVED_0x24_`/86 **stays reserved** (promotes at #32 T3's first
> accuracy draw); one `SCOUTING_SAVE_FORMAT_VERSION` season-save sub-blob (canonical ascending-`PlayerId`
> order; **no** `WORLD_STORE` bump — a deliberate, argued revision of the plan §4's WorldStore proposal:
> the composite is #22-owned, five sibling precedents). **KD-4:** #34 `ToScoutQuality` scales assignment
> **speed only** (`DaysPerBand`), never widths (the retroactivity trap); #32 defines
> `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000`, closing #34's open baseline with no #34 edit. **KD-5:** ranking
> is #32's own pure read-only query; #32 issues no offers (the manager acts via #31 `SubmitBid`).
> **One approval-time back-prop:** ERR-030-007 (the #30 scouting tick-order step-7 null seam, after staff;
> `AdvanceDay` → step 8; `spec-error-log.md` v1.38). Supplement AR-1 (3M+2L) → AR-2 (3L) → CONVERGENCE;
> section-file AR PASS-1 (3M+1L) → PASS-2 (1M+2L) → PASS-3 clean → CONVERGENCE. **This completes Wave 4**
> (#31 → #34 → #32). Count: **39 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, latest same day (**Staff & Backroom #34 authored + advanced `→ IN REVIEW →
> APPROVED`** — Wave 4's second spec (coaches/scouts/physios as attributed entities that modulate #29/#41/#33/#31).
> A **Stage-3 system with a pulled-forward identity scaffold**: the managed club holds a real neutral-baseline
> staff roster whose quality **projections return each consumer's own identity type** (`MedicalModifier` #41 /
> `CoachingModifier` #29 / `staffMult` #31 / `MentoringPlan` #33), neutral ⇒ each type's exact `Identity`, so a
> neutral-staff season is **byte-identical to pre-#34** (KD-3/KD-5/KD-8). **KD-1:** hiring reuses #31's
> `NegotiationOutcome` + the atomic-commit pattern but a thin staff `StaffOffer`/`EvaluateStaffOffer` — the
> negotiated quantity is a **wage, not a fee** (#31's `EvaluateOffer` tests fee), year-round (no window).
> **KD-2:** a fresh staff data layer (distinct skill vocabulary; per-club **role slots** 1:1 with `StaffRole`;
> stable monotonic `StaffId`). **KD-6:** the scaffold posts no `StaffWage` (FR-FN-015 preserved verbatim, no #40
> back-prop at approval); the deep wage gate reads #40's running `WageBillAggregate + wage ≤ WageBudget` — **no
> #34 wage counter** (a counter would be the parallel wage total FR-FN-015 forbids). **KD-7:** a hire changes a
> mutable `EmployerClubId`, so `StaffId` never re-keys — **no #30 roster-commit, no migration hook** (a simpler
> divergence from #31's KD-7). **One approval-time back-prop:** ERR-030-006 (the #30 staff tick-order step-6
> null seam; `AdvanceDay` → step 7); `0x26`/88 stays reserved (draw-free). Supplement AR-1 (1H+4M+1L) → AR-2
> (1M+1L) → AR-3 CONVERGENCE; section-file AR PASS-1 (2M) → PASS-2 (1M regression) → CONVERGENCE. Count:
> **38 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, latest same day (**Transfers, Contracts & Negotiation #31 authored +
> advanced `→ IN REVIEW → APPROVED`** — Wave 4's first spec (the recruitment engine; owns the reusable
> negotiation seam #32/#34 consume). **KD-1:** the Stage-2 counterparty valuation is a **pure deterministic
> integer function** over #27 attributes + age + club-need — the identity #33 personality (a multiplicative
> bias) and #28 CA modulate at the deep tier, never a replacement path. **KD-2:** the #40 boundary — read
> `AvailableTransferBudget`, commit via `ApplyTransaction`, #31 owns a `committedSpendThisWindow` counter
> (FR-FN-004 gives #40 none), no parallel ledger, **validate-all-before-commit atomicity** (no half-written
> deal). **KD-3:** the offer/response seam is counterparty-generic (authored once for #32/#34). **KD-4:** one
> `TRANSFERS_SAVE_FORMAT_VERSION` season-save sub-blob (durable contracts + season-scoped window/spend), **no**
> `WORLD_STORE` bump. **KD-5:** draw-free minimal ⇒ `_RESERVED_0x23_` / ordinal 85 **stay reserved** (the #40
> ERR-040-001 precedent); rival bidding is the deep-tier first draw. **KD-6:** the transfer-window model is
> #31-owned (#30 has none), derived read-only from `SeasonCalendar`. **KD-7:** a transfer **re-keys** the
> club-scoped `PlayerId` through a NEW #30 mid-season roster-commit entry point + roster-move hook; #31
> migrates only its own `Contract`. **One approval-time back-prop:** ERR-030-004 (the #30 transfers tick-order
> step-5 null seam, the #41 ERR-030-002 precedent — a deep-tier position reservation, empty at minimal); #16
> unchanged (draw-free). Section-file AR-1 (3M+1L) → AR-2 (1L) → CONVERGENCE. Count: **37 APPROVED / 0 IN
> REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, latest same day (**Personalities, Morale & Squad Dynamics #33 authored +
> advanced `→ IN REVIEW → APPROVED`** — Wave 3's gating spec (the vol-2 human-systems producer #22 was built
> to consume read-only). **KD-1 (headline):** #33's #22 read surface is matched **verbatim** to the FR-LW-004
> `PlayerEdge` contract — exactly the pairwise scalar `∈ [0,1]` per player↔player ordered pair (no baseline;
> #22 owns the `Affinity`/`Trust` `b`); one-directional (#33 writes canon, #22 mirrors, refuses via
> `ApplyEvent`), and the mirror write needs **one new** `MemoryStore.SetPlayerEdgeMirror` seam (a #22 T-phase
> code addition, no schema/arc-logic change; `T-LW-U-035` green). **KD-4:** cliques are a **derived** read
> (mutual `> 600‰` = #22's `0.6`) — no double-truth. **KD-6:** minimal is draw-free ⇒ `_RESERVED_0x25_` /
> ordinal 87 **stay reserved** (the #40 precedent). **KD-7:** `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` season-save
> sub-blob, **no** `WORLD_STORE` bump. **Zero approval-time cross-spec back-props** — #30 slot 3 + FR-SN-017 +
> #22 FR-LW-004/FR-LW-032 were all pre-declared (the roadmap §4 sequencing payoff). Section-file AR-1 (5M+4L)
> → AR-2 (1M+2L) → CONVERGENCE. Count: **36 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, latest same day (**Club Finances & Economy #40 authored + advanced
> `→ IN REVIEW → APPROVED`** — Wave 2's fourth spec (per-club budgets/wage ledger/prize money; the
> counterparty-constraint layer #31 reads). Promoted from the converged design supplement
> (`docs/tracking/club-finances-economy-design.md` v0.2, design-AR 1M+1L → clean) to an 11-file section set
> (FR-FN-001..028); section-file **AR-1 (1M — wage `ApplyTransaction` conflated cash with commitment; now a
> wage transaction moves the `WageBillAggregate` liability only, cash items move `Balance` only) → AR-2 →
> AR-3 clean, CONVERGENCE.** **KD-1/KD-6:** `SettleFinances` is a season-boundary step (`budget =
> f(finalTablePosition, prizeMoney)`, pure integer, no per-day step) inserted at #30's `RollToNextSeason()`
> step (b') after the (a') #43 point (ERR-030-003). **KD-2:** minimal tier is draw-free, so `_RESERVED_0x29_`
> / `SubsystemOrdinals` 91 **stay RESERVED, not promoted** (ERR-040-001, the #29 `0x21` precedent). **KD-3:**
> one-way #31→#40 read-only budget query + a #40-owned `ApplyTransaction`. **KD-7:** `FINANCE_SAVE_FORMAT_
> VERSION` season-save sub-blob (not `WORLD_STORE_FORMAT_VERSION`). Integer currency throughout; #34/#45
> deferred via identity `BoardModifier`. Count: **35 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, latest same day (**Injuries & Medical #41 authored + advanced
> `→ IN REVIEW → APPROVED`** — Wave 2's third spec (injury occurrence/severity/recovery on the world
> tick). Promoted from the converged design supplement (`docs/tracking/injuries-medical-design.md` v0.2,
> design-AR 2M+2L → clean) to a full 11-file section set (FR-MD-001..027); section-file **AR-1 (1M — float
> arithmetic → integer per-mille throughout) → AR-2 (1M — fixed-radix action-ordinal for append parity) →
> AR-3 clean, CONVERGENCE.** **KD-1:** all draws on ONE world-tick `injuries.occurrence` stream, keyed
> position-independently on `(playerId, worldDay, purpose)` — no free-running cursor, nothing to persist,
> the match tick never draws (the plan's dual-clock hazard dissolved). **KD-6:** a #30 tick-order back-prop
> (ERR-030-002) appends the injuries null seam as step 4. **KD-7:** a `MEDICAL_SAVE_FORMAT_VERSION`
> season-save sub-blob (not `WORLD_STORE_FORMAT_VERSION`). #16 §3.4 promotes `DOMAIN_TAG_INJURIES_MEDICAL =
> 0x2A` / `SubsystemOrdinals` 92 (ERR-041-001, spec-text-first). Reads #29's `InjuryRiskContribution`
> read-only; staff (#34) via an identity `MedicalModifier`; #34/#27-injury-proneness deferred. Count:
> **34 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, latest same day (**Training System #29 authored + advanced
> `→ IN REVIEW → APPROVED`** — Wave 2's second spec (the training seam #28 KD-2 reserved and #30's
> day-advance slot-2 null seam). Promoted from the converged design supplement
> (`docs/tracking/training-system-design.md` v0.4, design-AR **1H+1M+2L** → AR-2/AR-3 clean) to a full
> 11-file section set (FR-TR-001..024), then section-file PASS-1 (**0H+1M**) → AR-2 → AR-3 convergence.
> **Scope:** weekly-directed, **daily-accrued** training on the **world tick** — a per-player focus drives
> a deterministic **conditioning** cursor + a **training-fatigue** accumulator, and (deep tier) a granular
> per-attribute growth **input to #28's curve** — as one code path with a dial (Stage-2 minimal = the
> identity the deep tier extends, KD-8). **KD-1 (headline)** reconciles world-tick training-fatigue with
> match-tick fatigue (`1 − AerobicPool`) via a **single accumulator + a pure one-directional projection**
> into the match-boot caller-supplied starting fatigue (the `PlayerAttributeProjection` KD-P4 seam) — no
> shared counter, no write-back, not stored (save-exact). **KD-2** keeps attribute mutation single-owned by
> #28: a **pure `ComputeTrainingInput`** feeds #28's `GrowthProjection` at #30's slot-1 seam (no reorder, no
> staleness); #29 never writes `PlayerAttributes` and #28's assembly stays schema-untouched. **KD-6:** #29
> is **fully deterministic — NO RNG stream**, so `_RESERVED_0x21_` / `SubsystemOrdinals` 83 **stay reserved**
> (deliberately NOT promoted, unlike #28's `0x20` — #29 has no genuine draw site; a zero-draw stream would
> be the FR-LW-031 phantom class). The design-AR caught this self-contradiction (H — the draft invented a
> `training.session` stream while citing FR-LW-031) and a Form/Fitness two-cursor muddle (M — collapsed to
> one `Condition` cursor; match-driven "form" deferred to its owner); the section-file PASS-1 caught a day-0
> idempotency sentinel collision (M — fixed with a `uint.MaxValue` NOT_ADVANCED sentinel + `Create` factory).
> #16 §3.4 (v1.0.10) records the no-promotion (ERR-029-001, RESOLVED); no `DETERMINISM_DIGEST_VERSION` bump.
> R-01..R-05 signed (§9 checklist v0.2); all 11 files `Status: APPROVED`. **Approves the forward design**
> (the #21–#30 pre-T0 precedent); the §7 T-phase plan is post-APPROVED. Count: **33 APPROVED / 0 IN REVIEW
> / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026, later same day (**Player Progression & Lifecycle #28 authored +
> advanced `→ IN REVIEW → APPROVED`** — Wave 2's first spec (the aging/lifecycle seam #30 KD-2/KD-6
> already reserved a null slot for). Promoted from the converged design supplement
> (`docs/tracking/player-progression-lifecycle-design.md` v0.3, AR-1 2M+2L → AR-2 3M → AR-3 clean) to a
> full 11-file section set (FR-PG-001..024), then section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) →
> AR-3 convergence. **Scope:** player lifecycle on the **world tick** — aging/decline/growth via a
> CA/PA model over #27's `PlayerAttributes`, retirement, and regen/newgen production — as **one code
> path with a config dial** (the deep curve reduces to the literal master-plan §4.3 step when off,
> KD-8), driven by #30's day-advance loop + season-boundary roll at the seams #30 reserved. **KD-1**
> pins a byte-exact **integer fixed-point** growth model: `[1,20]` attributes are the single source of
> truth, `GrowthCursor` the only accumulator, CA a derived summary, PA the ceiling, and age is derived
> from a single serialized `BirthWorldDay` (no discrete rollover — the PASS-1 M-1 fix collapsed a
> two-representation age muddle). **KD-2** makes the #29 training seam a **method input defaulted to
> neutral** (training is an *input* to #28's single growth function, no phantom interface). **KD-4**
> keeps CA/PA in a #28-owned career-state block keyed by `PlayerId` (the complete `PlayerRecord` +
> lifecycle overlay, serialize-don't-regenerate); **#27's canonical struct stays schema-untouched.**
> **KD-5** flags retirement on the world tick but applies roster removal + regens only at the season
> boundary (never mid-fixture). The #16 §3.4 cross-cite **promotes** the reserved `_RESERVED_0x20_` /
> `SubsystemOrdinals` 82 rows → `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` (per-club `progression.regen`
> stream, the #27 pattern; PASS-1 M-2 fix), ERR-028-001, filed at approval — no
> `DETERMINISM_DIGEST_VERSION` bump. R-01..R-05 signed (§9 checklist v0.2); all 11 files
> `Status: APPROVED`. **Approves the forward design** (the #21–#30 pre-T0 precedent); the §7 T-phase
> plan is post-APPROVED. Count: **32 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 23, 2026 (**Localization & Accessibility #49 (seam + template-contract slice)
> authored + advanced `→ IN REVIEW → APPROVED`** — Wave 1's final item (the seam #38's KD-5 was written
> against). Promoted from the converged design supplement (`docs/tracking/localization-seam-template-design.md`
> v0.2, AR-1 2H+1M+1L → AR-2 clean) to a full 11-file section set (FR-LC-001..020 + FR-LC-008a), then
> section-file PASS-1 (1H+1M+1L) → AR-2 convergence. **Scope: the seam + template contract only** (the #38
> framework/screens precedent) — the single `ILocalizer` routing point (static `Resolve(LocalizationKey)` +
> procedural `Render(LocalizedTextRequest)`), the template model (named-placeholder + a bounded plural/gender
> selector), the **localize-after-generate** determinism boundary (KD-2 — the transform is display-side, so
> #22's `world.text` draw + serialized memory stay locale-independent and a save round-trips byte-identically
> across display locales), the one-way reference direction, the fallback policy, and the **retrofit of the
> one built producer** (#22 `InteractionTextGenerator`); translated **locales** + the **a11y content surface**
> are Wave 8. **PASS-1 H-1 fix was structural:** the generic seam was coupled to the concrete #22 producer on
> its core contract (an `InteractionIntent` overload + `ForInteraction` factory + fixed subject/opponent/score
> slots), forcing a core rewrite when #35/#46 land — split into a **producer-agnostic core** (references
> nothing sim-side) + a **per-producer boundary adapter** (`LivingWorldTextBoundary`, the only sim-side
> reference), the #38 generic-substrate rule the spec cited but had violated. **M-1 fix:** the corpus
> migration split the pre-draw gate — the sim-side gate is now an intent-VALUE roster check, and new
> FR-LC-008a asserts construction-time roster coverage (every defined intent has ≥1 base template) so
> `variantCount ≥ 1` holds by construction (preserving #22's no-cursor-on-refusal invariant). #49 registers
> **no domain tag / ordinal / RNG** (FR-LC-017, the `match-viewer`/#37/#38 class), so **no #16 §3.4
> cross-cite**. R-01..R-05 signed (§9 checklist v0.3); all 11 files `Status: APPROVED`. **Approves the
> forward design** (the #21–#38 pre-T0 precedent); the #22 retrofit + Wave-8 content are post-APPROVED.
> **This completes Wave 1** (#30 spine + #37 analytics + #38 UI framework + #49 localization seam). Count:
> **31 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 22, 2026, later same day (**UI / Client Framework #38 (framework slice) authored +
> advanced `→ IN REVIEW → APPROVED`** — Wave 1 presentation substrate. Promoted from the converged design
> supplement (`docs/tracking/ui-client-framework-design.md` v0.2, AR-1 2M → AR-2 clean) to a full 11-file
> section set (FR-UI-001..023), then section-file PASS-1 (0H+2M+2L) → AR-2 convergence. **KD-2 split
> settled:** the FRAMEWORK slice only — the view-model contract (immutable read-only projections), the
> navigation shell (a pure UGUI-free state machine), the command-dispatch discipline (mutation ONLY
> through existing public seams), and the one concrete match-view surface over `LiveMatchStreamer`; the
> tactics + management **screens** are deferred to Wave-7 screen specs, each gated on its data spec (the
> phantom-consumer trap). **KD-1 layer contract** grounded in the verified `match-viewer` no-reverse-
> reference lock + the `LiveMatchServer` disjoint-surface precedent (playback holds no `MatchEngine`). The
> PASS-1 M-2 fix was structural: a live-match command must be **marshaled onto the streamer's sim thread**
> (FR-UI-023 / F6 / a presentation-side `EnqueueIntent`), not called cross-thread — the write-side of
> KD-3's decoupling. #38 registers **no domain tag / ordinal / RNG** (FR-UI-022, the `match-viewer`
> class), so **no #16 §3.4 cross-cite**. R-01..R-05 signed (§9 checklist v0.3); all 11 files
> `Status: APPROVED`. **Approves the forward design** (the #21–#37 pre-T0 precedent); the UGUI rendering
> binding stays Unity-host-gated. Count: **30 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 22, 2026, later same day (**Match Analytics & Statistics #37 authored + advanced
> `→ IN REVIEW → APPROVED`** — Wave 1 read-only presentation prerequisite. Promoted from the converged
> design supplement (`docs/tracking/match-analytics-statistics-design.md` v0.2, AR-1 1M → AR-2 clean) to
> a full 11-file section set (FR-AN-001..021), then section-file PASS-1 (0H+2M+3L) → AR-2 convergence
> (0H+0M, L-only ⇒ CONVERGENCE). **KD-1 settled against verified source:** the match engine publishes
> only **8 Tier A record types** into the digest-bearing ledger (Possession/Foul/Card/Goal/Substitution/
> Offside/Restart/MatchPhaseChanged) and **no shot/pass/tackle/save event** (those ordinals are
> registered but have no producer), and `SerializeLedger` is write-only (no reader) — so #37's scope is
> exactly the derivable set + the observational positional sample, and shots/passes/tackles/shot-geometry
> xG are a **named, producer-gated match-engine follow-up** (Appendix D), never a phantom consumer
> (FR-LW-031). The PASS-1 M-1 fix was structural (the ledger tap must be consumed **every tick**
> losslessly — a sampling reader would drop a tick's foul/goal; enforced by the new F6 fail-loud). #37
> registers **no domain tag / ordinal / RNG** (KD-5, the `match-viewer` class) so **no #16 §3.4 cross-cite
> is needed** — a positive property, not a deferred allocation. R-01..R-05 signed (§9 checklist v0.3); all
> 11 section files at `Status: APPROVED`. **Approves the forward design** (the #21–#30 pre-T0 precedent).
> Count: **29 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 22, 2026, later same day (**Season & Competition Loop #30 advanced `IN REVIEW → APPROVED`**
> — Wave 1 critical-path spine spec complete. Section-file PASS-1 (1H+2M+2L) + AR-2 convergence sweep
> (0H+0M, L-only ⇒ CONVERGENCE) both resolved — the H-1 fix was structural (the loop resolved one
> fixture per fixture-day and skipped the round's other `N/2−1`, leaving the league table undefined for
> N>2; new KD-9 makes a fixture-day resolve the whole round, managed club through the full `MatchEngine`
> + the rest through a deterministic round-resolution model, `SeasonState.ManagedClubId` added). The #16
> §3.4 cross-cite is filed (`deterministic-sim/section-3.md` §3.4 v1.0.8 gains `DOMAIN_TAG_SEASON_LOOP =
> 0x22` + the `_RESERVED_0x20_`/`_RESERVED_0x21_` placeholders for #28/#29 — the roadmap §6 contiguous
> block; spec-text-first per ERR-030-001, `spec-error-log.md` v1.33 — code const at #30 T2, no phantom
> stream); the #22 phase-1 cross-check found no contract to violate (producer-only, ingest gated on #33
> per `FR-LW-032`); and R-01..R-05 sign-off is granted (§9 checklist v0.3). All 11 section files flip
> `Status: APPROVED`. **Approves the forward design** (the #21–#26 pre-T0 precedent — nothing built; §7
> T-phase plan is the implementation sequence). Count: **28 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 22, 2026, later same day (**Season & Competition Loop #30 promoted `→ IN REVIEW`**
> — Wave 1 of the management-layer roadmap (the critical-path career spine `#27 → #30 → #33 → #31 → #38
> → #39`) begun. Promoted from the converged design supplement `docs/tracking/season-competition-loop-design.md`
> v0.2 (AR-1 1M+3L → AR-2 clean) to a full 11-file section-file set at `docs/specs/season-competition-loop/`
> (FR prefix **FR-SN**; FR-SN-001..034). Forward-design spec (nothing built yet — the #21–#26 IN-REVIEW
> posture, unlike #27 which documented already-built code): defines deterministic round-robin fixtures, a
> live league table, a calendar/match-day loop, board objectives, and multi-season continuity, owning the
> `SeasonSaveManager` composition root (season state as a third opaque sub-blob; `SEASON_SAVE_FORMAT_VERSION`
> 1 → 2). The load-bearing design decision (AR-1 M-1): #30 is the #22 phase-1 **producer** only — ingest
> activation is deferred to #33 per `FR-LW-032` (a MUST gating activation on match-outcome events **and**
> the human-systems model), not wired here. Registry row #30 added; the `0x22`/84 back-prop + R-01..R-05
> sign-off are the remaining gates (§9.3). #28/#29 (Wave 2) remain unpromoted (no rows). Count: **27
> APPROVED / 1 IN REVIEW (#30) / 0 NOT STARTED.**)
> **Last Updated (prior):** July 22, 2026, later same day (**Squad / Player Data Layer #27 advanced `IN REVIEW → APPROVED`**
> — Wave 0 of the management-layer roadmap complete. Section-file PASS-1 (0H+1M+2L) + AR-2 convergence
> sweep (0H+1M — the §7.2 GK/Heading projection deferral was stale, since `PlayerAttributeProjection.cs`
> v1.2 landed `ToGoalkeeper`/`ToHeading` the same day; new §7.3 records LANDED — L-only otherwise ⇒
> CONVERGENCE) both resolved; the #16 §3.4 cross-cite is filed (`deterministic-sim/section-3.md` §3.4
> gains `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` + the retroactive `DOMAIN_TAG_LIVING_WORLD = 0x1E` row the
> `0x1F` allocation follows; back-props ERR-027-001 + ERR-022-001, `spec-error-log.md` v1.32); and
> lead-developer R-01..R-05 sign-off is granted (§9 checklist v0.3). All 11 section files flip
> `Status: APPROVED`. Count: **27 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 22, 2026 (**Squad / Player Data Layer #27 promoted `→ IN REVIEW`** — the
> data-layer design supplement `docs/tracking/squad-player-data-design.md` v0.6 (converged AR-1..AR-2)
> promoted to a full 11-file section-file set at `docs/specs/squad-player-data/` (FR-SQ-001..026),
> per the #21–#26 precedent. Unusually, the implementation already exists and is wired
> (`src/player-database/` + T1/T2/T3 + LineupSelector landed), so the section files DOCUMENT a
> landed layer and record its T-phase wiring status in §7. Governed forward by the sibling design
> docs (`player-attribute-projection-design.md`, `squad-roster-reference-design.md`,
> `lineup-selection-design.md`). Section-file PASS-1 adversarial review + lead-developer R-01..R-05
> sign-off are the remaining gates (§9.3). Count: **26 APPROVED / 1 IN REVIEW (#27) / 0 NOT
> STARTED.**)
> **Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 all advanced `IN REVIEW → APPROVED`** —
> lead-developer R-01..R-05 sign-off granted on all four (each spec's §9 checklist v0.4); the last
> closable gates closed in the same pass: the #26 Bradley citation VERIFIED (Bradley & Noakes 2013,
> *J Sports Sci* 31(15):1627–1638, DOI 10.1080/02640414.2013.796062, PMID 23808376 — index-level
> corroboration, publisher/Crossref direct resolution still environment-blocked), and the seven
> cross-spec back-props FILED and landed atomically with the flips (spec-error-log.md v1.30:
> ERR-021-005/006/007 → #21 `TeamTactic` field + Appendix B appends in pinned order #23 → #24 → #25;
> ERR-012-007/008/009 → #12 §3.7.1 pipeline amendments incl. the `SlotIndex` single-writer contract;
> ERR-008-012 → #8 §3.2.2.1 marked-pass-target anchor row). Carried forward post-APPROVED,
> non-blocking: the `[GT]` balance passes (#21 G2 precedent) and the #26 engine-substrate gates
> (T2 halves/`MATCH_TICKS_TOTAL`, T4 goal-detection — upstream-owned prerequisites, not spec-text
> gates). Count: **26 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 10, 2026 (**#23–#26 post-PASS-1 gates closed where closable** — §8
> citation rows closed for #23/#24/#25 (verified / replaced / reclassified; #26 Wilson verified,
> Bradley row pending with a recorded environment-blocked attempt); #25 Appendix A completed
> (all three `FormationFamily` tables); #26 A.1 member names pinned against the #21 enums.
> Count unchanged: **22 APPROVED / 4 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 8, 2026, later same day (**PASS-1 adversarial reviews run on #23–#26,
> all resolved in same-day v0.2 fix passes**: #23 0H+1M+3L; #24 0H+3M+2L; #25 1H+1M+3L (+ PASS-2
> re-read clean at H/M per the High-found rule); #26 0H+1M+2L. Per-spec
> `adversarial-review-section-files-v1.md` files filed; §9.3 gates updated. Remaining gates:
> `[CITATION-PENDING]` §8 rows, back-prop ERRs at `APPROVED`, #25 Appendix-A family completeness,
> R-01..R-05 sign-off. Count unchanged: **22 APPROVED / 4 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** July 8, 2026 (Candidates **#23–#26 promoted from RESERVED to registry rows at
> `IN REVIEW`** — all four authored as full 11-file section-file sets (v0.1) from their design
> supplements, per each supplement's §6 promotion pipeline: `dismarking-ai/` (#23, FR-DM),
> `build-up-structures/` (#24, FR-BU), `positional-rotations/` (#25, FR-RO), `tactical-presets/`
> (#26, FR-TP). The RESERVED section below records the promotion.)
> **Last Updated (prior):** July 7, 2026 (New "RESERVED (design-supplement stage — not yet promoted)"
> section added, reserving candidate numbers **#23–#26** for the two design supplements opened
> the same day — `docs/tracking/advanced-positional-behaviors-design.md` (dismarking #23,
> scripted build-up structures #24, positional rotations #25) and
> `docs/tracking/game-model-ai-manager-design.md` (tactical presets & AI-manager selection #26).
> No registry rows added — none is promoted yet; see root `CLAUDE.md` OPEN ISSUES for the full
> entry. **Count remains 22 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** June 22, 2026 (Living World System #22 advanced `IN REVIEW → APPROVED` — lead-developer R-01..R-05 sign-off granted; §9.5 decision APPROVED; all 11 section files at status APPROVED (checklist v1.0). Ten section-file adversarial passes (no High in any; all M/L resolved) + T0 `src/living-world/` scaffolding (data types + pure math + ordinal/determinism unit tests) landed. G2 `[GT]` balance pass carried forward as the sole post-APPROVED Stage-1 item; runtime activation gated on KD-10. **Count: 22 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**)
> **Last Updated (prior):** June 21, 2026 (Living World System #22 added at `NOT STARTED → IN REVIEW`, then section-file PASS-1 fix pass landed same day (v0.2). Second Stage-1 forward spec (Priority 6); 10 section files + appendices + `adversarial-review-section-files-v1.md`, promoted from the design supplement `docs/tracking/living-world-system-design.md` v0.7 after four adversarial passes + five recorded scope decisions. Off-pitch interaction-generation layer that consumes the vol-2/vol-3 human-systems model read-only and adds per-edge episodic memory, deterministic procedural text, and emergent narrative arcs. Section-file PASS-1..10 resolved (4M+3L → 3M+3L → 2M+2L → 1M+3L → 1M+2L → 1M+2L → 1M+1L → 1M+1L → 1M+1L → 2M+1L; no High in any pass since PASS-1 — design stable; findings are localized implementation-grade detail). PASS-10 fixed two cold-store/membership edges opened by the PASS-8/9 fixes (ColdSummary `NextEpisodeId` for episodeId monotonicity; demotion never orphans an arc-pinned episode). ≥75 tests. PASS-3 caught a `PlayerEdge` double-authority hazard (now a read-only mirror); PASS-4 added the verifying test + closed the §2.1 no-write-back gap; PASS-5 scoped FR-LW-016 provenance to arcs (interaction provenance implicit); PASS-6 split the RNG into `world.arcs`/`world.text` sub-streams (periodic/aperiodic draw-interleaving hazard). 34 FRs (FR-LW-001..034); XC-022-001..014; ERR-022-001..004 back-props (season-loop is a §7.1 forward deliverable, not an ERR); ≥74 tests. **T0 scaffolding landed June 21, 2026:** `src/living-world/` data types + pure math + ordinal/determinism units (self-contained, engine-free; CI project-gen validated — services land as KD-10 prerequisites are wired). **Outstanding gates (§9.4):** R-01..R-05 lead-developer sign-off (block added to §9.4, awaiting signature); `[GT]` balance pass (G2, carried forward post-APPROVED). G1 section-file PASS-1..10 DONE. Runtime activation gated on KD-10 (world store + season loop; vol-2/vol-3 impl.; `[GT]` config-loader; match-outcome events). Count: **21 APPROVED / 1 IN REVIEW (#22) / 0 NOT STARTED**.)
> **Last Updated (prior):** June 20, 2026, later same day (Tactical Instructions #21 advanced `IN REVIEW → APPROVED` — lead-developer R-01..R-05 sign-off granted; section files at v0.3 after PASS-1 (2H+4M+4L) + PASS-2 (1H+4M+1L) adversarial reviews, both resolved. **Post-APPROVED follow-up (non-blocking, Stage-1 implementation-time):** the `RoleWeightModifiers`/§3.2 **balance pass** that pins the illustrative `[GT]` values (G2 in §9.2) — precedent: #8 draft-level approval, #9 §9.8, #16 post-approved items. Runtime activation remains gated on the `[GT]` config-loader + match-engine Phase C/D (KD-8). Count: **21 APPROVED / 0 IN REVIEW / 0 NOT STARTED**.)
> **Last Updated (prior):** June 20, 2026 (Tactical Instructions #21 added at `NOT STARTED → IN REVIEW` — first Stage-1 forward spec beyond the Stage-0 set of 20; all 11 section files authored v0.1, promoted from the design supplement `docs/tracking/tactical-instruction-layer-design.md` v0.3 after three adversarial passes.)
> **Last Updated (prior):** May 18, 2026 (Goalkeeper Mechanics #11, Positioning AI #12, and Defensive AI #14 all advanced `IN REVIEW → APPROVED`; lead-developer R-01..R-05 signed all three; ERR-011-001/012-001/014-001/014-004 all resolved; count now **20 APPROVED / 0 IN REVIEW / 0 NOT STARTED** — Stage 0 specification phase COMPLETE)
> **Purpose:** Single source of truth for spec numbers, folder names, and approval status. Every cross-reference in every spec must match the numbers in this file.

---

## HOW TO USE THIS FILE

- Before writing ANY spec cross-reference, verify the number here.
- Before creating a new spec folder, add the entry here first.
- Approval status here overrides any status stated in individual spec files.

---

## SPECIFICATION REGISTRY

| # | Specification | Folder | Priority | Status | Approved |
|---|---------------|--------|----------|--------|----------|
| 1 | Ball Physics | `ball-physics/` | 1 | APPROVED | Feb 8, 2026 |
| 2 | Agent Movement | `agent-movement/` | 1 | APPROVED | Apr 27, 2026 |
| 3 | Collision System | `collision-system/` | 1 | APPROVED | Feb 19, 2026 |
| 4 | First Touch Mechanics | `first-touch/` | 1 | APPROVED | Feb 22, 2026 |
| 5 | Pass Mechanics | `pass-mechanics/` | 1 | APPROVED | May 6, 2026 |
| 6 | Shot Mechanics | `shot-mechanics/` | 2 | APPROVED | Apr 27, 2026 |
| 7 | Perception System | `perception-system/` | 2 | APPROVED | Apr 22, 2026 |
| 8 | Decision Tree | `decision-tree/` | 2 | APPROVED | Apr 27, 2026 |
| 9 | Fixed64 Math Library | `fixed64-math/` | 2 | APPROVED | May 15, 2026 |
| 10 | Heading Mechanics | `heading-mechanics/` | 3 | APPROVED | May 16, 2026 |
| 11 | Goalkeeper Mechanics | `goalkeeper-mechanics/` | 3 | APPROVED | May 18, 2026 |
| 12 | Positioning AI | `positioning-ai/` | 3 | APPROVED | May 18, 2026 |
| 13 | Pressing AI | `pressing-ai/` | 4 | APPROVED | May 17, 2026 |
| 14 | Defensive AI | `defensive-ai/` | 4 | APPROVED | May 18, 2026 |
| 15 | Attacking AI | `attacking-ai/` | 4 | APPROVED | May 18, 2026 |
| 16 | Deterministic Simulation | `deterministic-sim/` | 4 | APPROVED | May 14, 2026 |
| 17 | Event System | `event-system/` | 5 | APPROVED | May 13, 2026 |
| 18 | Performance Optimization Strategy | `performance-optimization/` | 5 | APPROVED | May 15, 2026 |
| 19 | Testing Strategy & Framework | `testing-strategy/` | 5 | APPROVED | May 15, 2026 |
| 20 | Code Standards & Style Guide | `code-standards/` | 5 | APPROVED | May 11, 2026 |
| 21 | Tactical Instructions | `tactical-instructions/` | 6¹ | APPROVED | Jun 20, 2026 |
| 22 | Living World System | `living-world/` | 6¹ | APPROVED | Jun 22, 2026 |
| 23 | Dismarking & Marker-Awareness AI | `dismarking-ai/` | 6¹ | APPROVED | Jul 10, 2026 |
| 24 | Scripted Build-Up Structures | `build-up-structures/` | 6¹ | APPROVED | Jul 10, 2026 |
| 25 | Positional Rotations | `positional-rotations/` | 6¹ | APPROVED | Jul 10, 2026 |
| 26 | Tactical Presets & AI-Manager Selection | `tactical-presets/` | 6¹ | APPROVED | Jul 10, 2026 |
| 27 | Squad / Player Data Layer | `squad-player-data/` | 6¹ | APPROVED | Jul 22, 2026 |
| 28 | Player Progression & Lifecycle | `player-progression-lifecycle/` | 6¹ | APPROVED | Jul 23, 2026 |
| 29 | Training System | `training-system/` | 6¹ | APPROVED | Jul 23, 2026 |
| 30 | Season & Competition Loop | `season-competition-loop/` | 6¹ | APPROVED | Jul 22, 2026 |
| 37 | Match Analytics & Statistics | `match-analytics-statistics/` | 6¹ | APPROVED | Jul 22, 2026 |
| 38 | UI / Client Framework (framework slice) | `ui-client-framework/` | 6¹ | APPROVED | Jul 22, 2026 |
| 40 | Club Finances & Economy | `club-finances-economy/` | 6¹ | APPROVED | Jul 23, 2026 |
| 41 | Injuries & Medical | `injuries-medical/` | 6¹ | APPROVED | Jul 23, 2026 |
| 33 | Personalities, Morale & Squad Dynamics | `personalities-morale-dynamics/` | 6¹ | APPROVED | Jul 23, 2026 |
| 49 | Localization & Accessibility (seam + template-contract slice) | `localization-accessibility/` | 6¹ | APPROVED | Jul 23, 2026 |
| 31 | Transfers, Contracts & Negotiation | `transfers-contracts-negotiation/` | 6¹ | APPROVED | Jul 23, 2026 |
| 34 | Staff & Backroom | `staff-backroom/` | 6¹ | APPROVED | Jul 23, 2026 |
| 32 | Scouting & Player Knowledge | `scouting-player-knowledge/` | 6¹ | APPROVED | Jul 24, 2026 |
| 43 | Competition Structure | `competition-structure/` | 6¹ | APPROVED | Jul 24, 2026 |
| 44 | Discipline & Suspensions | `discipline-suspensions/` | 6¹ | APPROVED | Jul 24, 2026 |

¹ Priority 6 = Stage-1 forward (first spec authored after the Stage-0 set of 20 was complete); the 1–5 scale covered the Stage-0 spec set only.

---

## RESERVED (design-supplement stage — not yet promoted)

**All four reservations promoted July 8, 2026** — #23–#26 were reserved here July 7, 2026 for the
two design supplements (`docs/tracking/advanced-positional-behaviors-design.md` #23–#25;
`docs/tracking/game-model-ai-manager-design.md` #26) and moved to registry rows at `IN REVIEW`
the next day when their section files were authored, matching the #21/#22 precedent (design note →
promoted spec, row added at promotion). The section is retained (empty) as the standing mechanism
for future design-supplement reservations.

| # | Working title | Design supplement | Reserved |
|---|---------------|--------------------|----------|
| — | *(none currently reserved)* | | |

Note: the June 27, 2026 Match Engine integration entry in root `CLAUDE.md` OPEN ISSUES explicitly
recorded that the (unnumbered) `src/match-engine/` composition root "does not introduce #23" —
consistent with #23 having been free to reserve and now promote.

---

## STATUS DEFINITIONS

| Status | Meaning |
|--------|---------|
| APPROVED | Lead developer signed off. Ready for implementation. |
| SUSPENDED | Was approved or in review, but audit findings require re-review before sign-off. |
| IN REVIEW | All sections written. Awaiting lead developer sign-off. |
| IN PROGRESS | Actively being written. Not all sections complete. |
| NOT STARTED | No work begun. |

---

## NOTES

- **Pass Mechanics (#5):** Originally approved Feb 22, 2026 → suspended March 25, 2026 (19 audit findings) → re-approved May 6, 2026 after all 19 findings fixed (per `fix-manifest-pass-mechanics.md`) plus §3.3–§3.9 follow-up findings F-A01 / F-A02 resolved (option-3 hybrid: spinBase/spinMax columns added to §3.1.4; WINDUP_FRAMES/FOLLOWTHROUGH_FRAMES localized in §3.8.10). Re-review packet at `pass-mechanics/re-review-packet.md`.
- **April 27, 2026 sign-off pass:** Agent Movement (#2), Shot Mechanics (#6), and Decision Tree (#8) all approved by lead developer. Decision Tree (#8) approved at "draft-level" quality gate; comprehensive audit candidate for follow-up before implementation.
- **May 2, 2026 status reconciliation:** Deterministic Simulation (#16) reclassified from `NOT STARTED` to `IN PROGRESS`. Section files (sections 1–9 + appendices) exist at v0.5–v0.7 but were authored ahead of formal status update. Counts now: 7 APPROVED, 1 SUSPENDED, 0 IN REVIEW, 1 IN PROGRESS, 11 NOT STARTED.
- **May 6, 2026 status reconciliation:** Fixed64 Math Library (#9) reclassified from `NOT STARTED` to `IN REVIEW` after Pass 2 adversarial critique fixes landed. All sections (1–9 + appendices) present at v0.2–v0.3; awaiting lead developer sign-off per `fixed64-math/section-9-approval-checklist.md`. Counts after that change: 7 APPROVED, 1 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.
- **May 6, 2026 (later same day) — Pass Mechanics (#5) re-approved.** F-A01 / F-A02 resolved via option-3 hybrid; lead developer sign-off granted. Counts now: **8 APPROVED, 0 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.** Stage 0 Priority 1–2 spec set is now complete (Pass Mechanics was the last suspended Priority 1–2 spec).
- **May 11, 2026 — Code Standards & Style Guide (#20) approved.** Section files + appendices authored May 7–8 from `outline-detailed.md` v1.3; adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off completed. Counts: **9 APPROVED, 0 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 9 NOT STARTED.**
- **May 12, 2026 — Testing Strategy & Framework (#19) reclassified `NOT STARTED` → `IN REVIEW`.** Initial section-file draft authored May 12 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied (3 H / 6 M / 8 L findings, all resolved). Per KD-2 sequencing in spec §9.3.6–§9.3.8, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved. Counts: **9 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 8 NOT STARTED.**
- **May 13, 2026 — Performance Optimization Strategy (#18) reclassified `NOT STARTED` → `IN PROGRESS`.** Detailed outline `outline-detailed.md` v1.0 authored from `outline.md` v1.0 + May 6 adversarial review (6 H / 5 M / 2 L findings, all resolved); v1.1 issued same day to resolve `ERR-018-001` via **KD-3 inversion** (Spec #18 now owns the trace pipeline; Spec #16 retains canonical record format at §3.2.4.1, regression scenarios at §5, and determinism-of-emission veto over tick-pipeline trace points at §3.1). Establishes 11 cross-cutting design decisions: KD-2 per-spec §6 ratify-not-override, KD-3 (inverted) #16 boundary, KD-4 #19 boundary, KD-7 Tier-C-only degradation, KD-8 loop separation, KD-6 determinism-aware profiling, KD-10 hot-path enumeration via per-spec §6 union. Section files remain stubs. Unblocks Spec #19 KD-2 precondition (b). `TBD-NORMATIVE` tags throughout for citations of #16 (`IN PROGRESS`) and #19 (`IN REVIEW`). Counts: **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 2 IN PROGRESS, 6 NOT STARTED.**
- **May 14, 2026 — Performance Optimization Strategy (#18) reclassified `IN PROGRESS` → `IN REVIEW`.** Section files authored May 13, 2026 (v0.1) from `outline-detailed.md` v1.1; PASS-1 adversarial review (4 H / 6 M / 13 L findings) resolved in v0.2 (May 14, 2026): ERR-018-002..011 all resolved. All 10 section files + appendices at v0.2. Outstanding `TBD-NORMATIVE` blockers listed at §9.4.1 — advancement to `APPROVED` requires #16 reaching Tier 2 `APPROVED` and #19 reaching `APPROVED`. Lead-developer sign-off pending. Counts: **10 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 1 IN PROGRESS, 6 NOT STARTED.**
- **May 14, 2026 (later same day) — Performance Optimization Strategy (#18) v0.3 fix pass landed; status remains `IN REVIEW`.** PASS-2 adversarial review filed against v0.2 section files (2 H / 5 M / 8 L findings, 15 total); root cause for H-1 / H-2 / M-1 traced to PR #59 + PR #60 parallel-branch merge collision on the v0.2 fix pass (both branches authored independent fixes for the same PASS-1 findings and merged without de-duplication, leaving duplicate Appendix F.0 schemas, duplicate §3.10 constants rows, and duplicate v0.2 version-history rows across seven section files). v0.3 fix pass applied same day: ERR-018-012..018 all resolved; Appendix F.0 consolidated to single canonical 13-field schema with merged anchor rows; §3.10 duplicates removed; version-history rows consolidated; `[HotPathAllocExempt]` C# signature deferred to Stage 0+1 per Interface Design Principle; FR-PO-019 split into FR-PO-019 (MAY) + FR-PO-019a (MUST), FR count now 82; §3.5.2 example rewritten to apply §3.9.1 ±20% promotion tolerance before +5% gate. All 10 section files + appendices at v0.3. Counts unchanged: **10 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 1 IN PROGRESS, 6 NOT STARTED.**
- **May 14, 2026 (later same day) — Deterministic Simulation (#16) reclassified `IN PROGRESS` → `IN REVIEW`.** Tier 1 (Conditional Approval) sign-off granted by lead developer. Per §9 v1.1 two-tier approval model, this advances the self-contained spec content (§1–§7, §9.1, §9.2, §9.5 #1–#3, §9.5 #5) to `IN REVIEW`; Tier 2 (Final Approval / `APPROVED`) remains gated on the §9.4.2 items reviewed below. Upstream-spec gate for Tier 2 is now cleared: #9 IN REVIEW (May 6), #17 APPROVED (May 13, beyond gate), #18 IN REVIEW (May 14), #19 IN REVIEW (May 12). Outstanding Tier 2 items: (i) §9.5 #4(c) `serialize-canonical-corpus.md` golden-vector file authoring; (ii) §8.3.1 re-audit of all four upstream rows and removal of `[TBD-NORMATIVE: pending #N]` suffix; (iii) §9.3 QA-automation + platform-certification sign-offs. **Former item (iv) (§9.5 #4(a)/(b) CI-runnability chicken-and-egg) RESOLVED in §9 v1.3 same day** — criterion split into spec-level (gates #16 Tier 2; hand-verifiable today) and implementation-level (folds into §5.5 `FR-DS-009-GATE`; NOT a #16 blocker). Counts: **10 APPROVED, 0 SUSPENDED, 4 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.**
- **May 15, 2026 (later same day still) — Performance Optimization Strategy (#18) reclassified `IN REVIEW` → `APPROVED`.** §9 v1.0 lead-developer sign-off granted per `performance-optimization/section-9-approval-checklist.md`. KD-2 sequencing satisfied: #16 Tier 2 `APPROVED` May 14; #19 `APPROVED` May 15 (earlier same day); #18 v1.0 `[TBD-NORMATIVE]` sweep landed May 15 across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices — 60 citation-qualifier instances resolved against #16 APPROVED text and #19 v1.0.1 patch-revised text. §9.4.1 blocker list marked RESOLVED. **Counts: 14 APPROVED, 0 SUSPENDED, 0 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.** Priority 5 spec set is now fully complete. Stage 0 NOT-STARTED-only block: #10 / #11 / #12 / #13 / #14 / #15 (Priority 3+4 AI specs).
- **May 16, 2026 — Heading Mechanics (#10) reclassified `NOT STARTED` → `APPROVED`.** Section files v0.1 authored May 16 from `outline-detailed.md` v1.1 (with prior outline PASS-1 fix pass landed in v1.1); section-files PASS-1 adversarial review (`adversarial-review-section-files-v1.md`, 21 findings: 5 H / 9 M / 7 L) filed and resolved in v0.2 same day; OI-001..OI-005 closure fix pass + lead-developer R-01..R-05 sign-off completed in v0.3 same day. **All Outstanding Items RESOLVED:** (i) `ERR-010-001` `DOMAIN_TAG_HEADING = 0x16` allocation landed in #16 §3.4 v1.0.2 (patch revision; pure namespace allocation, no `DETERMINISM_DIGEST_VERSION` bump; follows ERR-017-001 / `0x15` precedent); #10 §3.1 row promoted `[CROSS-PENDING] → [CROSS]` atomically; (ii) OI-002 channel-registry rows re-anchored to #18 Appendix F.0 schema with Stage 0+1 deliverable schedule (v0.1 / v0.2 incorrectly cited #18 §3.10); (iii) OI-003 DOI verification — 5/5 academic DOIs verified, 2 v0.1 fabricated references (Bull 1985, Auger & Pellegrini 2007) replaced with real equivalents (Babbs 2001 / Tomczak et al. 2021); (iv) OI-005 anchors pinned (#3 §3.4.2 `ICollisionEventConsumer`; #8 §1.7.2 Stage 0 deferral row with §4.6.1 Stage 0+1 activation framing; #17 §3.2.1 `Publish API surface`; #1 §3.1.11.2 `Ball.ApplyKick`/`GetBallState` surface). OI-006 (`certification-platform.md` Stage-0 host pin) remains carved out — does not block #10 sign-off per §6.1 / §9.4; gates `FR-PO-052` perf-gate activation only. KD-18 jump-kinematics ownership (#10-owned synthetic Z trajectory at Stage 0; retires to AM #2 native Z at Stage 1+ per §7.8) preserved. **Counts: 15 APPROVED, 0 SUSPENDED, 1 IN REVIEW (#12), 0 IN PROGRESS, 4 NOT STARTED (#11 / #13 / #14 / #15).** Stage 0 Priority 3 spec set advances; #11 Goalkeeper Mechanics is the last Priority 3 spec NOT STARTED.
- **May 15, 2026 (later same day) — Testing Strategy & Framework (#19) reclassified `IN REVIEW` → `APPROVED`.** §9 v1.0 lead-developer sign-off granted per `testing-strategy/section-9-approval-checklist.md`. KD-2 sequencing preconditions all cleared: (a) #16 Tier 2 `APPROVED` May 14 (§9.3.7); (b) #18 outline + section files v0.3 IN REVIEW (§9.3.6); (c) `[TBD-NORMATIVE]` sweep landed in #19 §1.0.1 patch May 15 across §1 / §3 / §4 / §5 / §6 / §8 / appendices (§9.3.8) — 51 citation-qualifier instances resolved against #16's APPROVED text and #18's v0.3 surface. **Downstream effect:** clears Spec #18's last KD-2 gate for its own `IN REVIEW → APPROVED` advancement (precondition was "#19 APPROVED"). Counts: **13 APPROVED, 0 SUSPENDED, 1 IN REVIEW (#18), 0 IN PROGRESS, 6 NOT STARTED.**
- **May 15, 2026 — Fixed64 Math Library (#9) reclassified `IN REVIEW` → `APPROVED`.** Lead-developer sign-off granted per `fixed64-math/section-9-approval-checklist.md` v1.0 (May 15, 2026). All §9.1–§9.6 evidence-anchored items verified against cited section files. §9.2 engine + gameplay owner sign-offs granted (gameplay owners for #1 / #2 / #3 / #6 / #8 confirmed against §8.4 typed cross-references; #6 has no Fixed64-dependent surface at Stage 0 — parameter-based physics consumes Ball Physics arithmetic transitively). §9.7 reciprocal `XC-016-NNN` resolved via Deterministic Simulation #16 §8.3.2 (May 14, 2026) — comparator-glossary deferral documented under CLAUDE.md "Interface Design Principle"; §6.6 forward-binding constraint preserved at Stage 5+. §9.8 implementation-time deliverables (golden-vector corpus, CI bench, harness digest, owning-team ledger) remain post-APPROVED follow-ups and were never approval-gating. **Stage 0 effect:** none — #9 binds Fixed64 to Stage 5+ per §8.1 v0.2; Stage 0 continues to use `float` + state-snapshot determinism. Counts: **12 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.** Priority 2 spec set is now fully complete.
- **May 14, 2026 (later same day) — Deterministic Simulation (#16) reclassified `IN REVIEW` → `APPROVED` (Tier 2).** All §9.4.2 Tier 2 gates cleared: §9.5 #4(a)/(b)/(c) spec-level sub-conditions all SATISFIED (golden-vector files at `golden-vectors/hkdf-sha256-kat.md` v1.1 / `siphash-2-4-kat.md` v1.1 / `serialize-canonical-corpus.md` v1.0); §8.3.1 cross-spec re-audit COMPLETE (§8 v1.2) — all four rows promoted to `complete` (#9 with documented Stage-5+ comparator-glossary deferral per new §8.3.2; #17 / #18 / #19 fully aligned); ERR-017-001 closed atomically by allocating `DOMAIN_TAG_EVENT_LEDGER = 0x15` in #16 §3.4 v1.0.1 (next value after `DOMAIN_TAG_ENV_FP = 0x14`; pure namespace allocation, no `DETERMINISM_DIGEST_VERSION` bump); §9.3 sign-offs (lead-developer Tier 2, QA-automation, platform-certification) granted (§9 v1.7), with platform-certification carrying the explicit Stage-0 host-platform-pin caveat per `certification-platform.md` OPEN ISSUES (`FR-DS-009-GATE` is unactivatable on Stage 0 until host-platform pinning lands — that is a lead-developer task outside spec-approval scope). Downstream effects: unblocks #17 §3.10 `[CROSS-PENDING]` → `[CROSS]` promotion (`DOMAIN_TAG_EVENT_LEDGER`); satisfies the #18 / #19 KD-2 `TBD-NORMATIVE` precondition (a) for their own Tier 2 / `APPROVED` advancement. Counts: **11 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.**
- **May 13, 2026 — Event System (#17) APPROVED.** Section files authored May 13, 2026 from `outline-detailed.md` v1.1; section-files PASS 1 adversarial critique (20 findings) and PASS 2 adversarial critique (2 H / 6 M / 7 L findings) both resolved same day. Lead-developer sign-off granted May 13, 2026. All 10 section files + appendices at v0.3. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation in #16 §3.4) remains open pending #16 approval; `[CROSS-PENDING]` tag in #17 §3.10 / §3.4.2 resolves atomically with #16. Counts: **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 7 NOT STARTED.**
- **May 17, 2026 (later same day still) — Defensive AI (#14) reclassified `NOT STARTED → IN PROGRESS`.** All 11 section files authored v0.1 from `outline-detailed.md` v1.0: `section-1.md` through `section-9-approval-checklist.md` and `appendices.md` (PR #81, merged same day). KD-5 Option B resolved (`TacticalContext.MarkDirective?` nullable field via ERR-014-001, mirroring #13's `PressDirective?` precedent). Q2–Q5 outstanding outline questions resolved at draft time (Q2: #3 §3.3 agent-agent collision surface; Q3: attribute normalisation `(attr−1)/19.0f`; Q4: GK zone expressed as `distToOwnGoal` scalar; Q5: `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` per ERR-012-001 Phase B/C block). 37 FRs; 27 constants (22 `[GT]` + 4 `[CROSS]`/`[CROSS-PENDING]`); all `[EST]` → `[GT]` with Appendix A derivations; 85+ test targets; ≤0.12 ms per-tick budget. ERR-014-001..004 pre-filed. **Outstanding (gates `IN PROGRESS → IN REVIEW`):** PASS-1 adversarial review + v0.2 fix pass; ERR-014-001 (#8 §2.2.6 `TacticalContext.MarkDirective?` amendment) ratified; ERR-014-004 (#16 §3.4 `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` allocation). **Counts: 16 APPROVED, 0 SUSPENDED, 2 IN REVIEW (#11 / #12), 1 IN PROGRESS (#14), 1 NOT STARTED (#15).**
- **May 17, 2026 (later same day) — Pressing AI (#13) reclassified `IN REVIEW` → `APPROVED`.** All gate items resolved in v0.3 same day: ERR-013-001 Option B (`PressDirective?` nullable field added to `TacticalContext` #8 §2.2.6 v1.1.1; freeze-then-amend pattern); ERR-013-004 resolved (#8 §3.1 L753 one-token patch: "Fatigue System #13" → "Pressing AI #13"); ERR-013-005 resolved (`DOMAIN_TAG_PRESSING_AI = 0x19` allocated in #16 §3.4 v1.0.3; `[CROSS-PENDING]` → `[CROSS]` promoted atomically); ERR-013-007 / ERR-013-008 resolved (`GetPhase` + `GetLine` elevated to Stage 1 publication commitments in #12 §4.5.1 v0.3 patch); Appendix A derivations complete (A.1–A.4; four hysteresis `[EST]` constants promoted to `[GT]`; no `[EST]` tags remain in #13); `T-C-` / `T-X-` prefix conformance verified — mapping table added to #19 §3.1.4 v1.0.2 confirming both are Simulation-layer test-requirement IDs; OI-012 `PressOverride` composition contract confirmed in #12 §7.3 v0.3 (present-tense update); R-01..R-05 signed (May 17, 2026). OI-002 (#17 channel-registry rows for `PRESS_TRIGGERED` / `PRESS_DISENGAGED`) remains open non-blocking — lands at Stage 1 first commit. **Counts: 16 APPROVED, 0 SUSPENDED, 2 IN REVIEW (#11 / #12), 0 IN PROGRESS, 2 NOT STARTED (#14 / #15).**
- **May 18, 2026 (Stage 0 specification phase COMPLETE) — Goalkeeper Mechanics (#11), Positioning AI (#12), and Defensive AI (#14) all reclassified `IN REVIEW` → `APPROVED`.** Resolved atomically: ERR-011-001 (`DOMAIN_TAG_GOALKEEPER = 0x1D` in #16 §3.4 v1.0.5 — value shifted from proposed `0x17` because #12 reached `APPROVED` first per KD-7 first-to-`APPROVED` precedent); ERR-012-001 (`DOMAIN_TAG_POSITIONING_AI = 0x17` in #16 §3.4 v1.0.5; all 8 hysteresis `[EST]` → `[GT]` per Appendix A.1–A.8; GK constants `GK_DEPTH_M`/`GK_ADVANCE_FACTOR`/`GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]` per KD-13); ERR-014-001 (`TacticalContext.MarkDirective?` added to #8 §2.2.6 v1.1.3); ERR-014-004 (`DOMAIN_TAG_DEFENSIVE_AI = 0x1A` in #16 §3.4 v1.0.5). All R-01..R-05 sign-offs granted May 18, 2026. **ALL 20 SPECS APPROVED. Stage 0 specification phase is complete. `src/` development may now begin. Counts: 20 APPROVED, 0 SUSPENDED, 0 IN REVIEW, 0 IN PROGRESS, 0 NOT STARTED.**
- **May 18, 2026 (later same day) — Attacking AI (#15) reclassified `IN REVIEW` → `APPROVED`.** Lead-developer R-01..R-05 sign-off granted. All §9.3 preconditions (a)–(g) COMPLETE: (a) ERR-015-001 — `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocated in #16 §3.4 v1.0.4; `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]` in #15 §6.1.9; (b) ERR-015-002 — `TacticalContext.AttackIntent[]?` added to #8 §2.2.6 v1.1.2; Stage 1+ RUNNER override note added to #8 §3.1.7; (c) 0 `[CROSS-PENDING]` remain in #15; (d)–(e) already COMPLETE at IN REVIEW; (f) ERR-015-005 — "Attacking AI (#15)" added to #8 §1.3.2 v1.1.2; (g) R-01..R-05 signed. ERR-015-003/004 (#17 channel-registry rows) remain OPEN as Stage-1-deferred non-blocking items. **Counts: 17 APPROVED, 0 SUSPENDED, 3 IN REVIEW (#11 / #12 / #14), 0 IN PROGRESS, 0 NOT STARTED.**
- **May 18, 2026 — Attacking AI (#15) reclassified `NOT STARTED` → `IN REVIEW`.** Section files v0.1 authored May 17, 2026 from `outline-detailed.md` v1.1 (9 section files + appendices.md, 3,306 lines total); PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) filed and resolved in v0.2 fix pass May 18, 2026. **Key fixes:** (H) §3.3 WEAK_SIDE condition — was `weakSideCount < MIN_WEAK_SIDE_AGENT_THRESHOLD` (allowed up to 4 WEAK_SIDE agents), corrected to `attackingPool.count >= MIN_WEAK_SIDE_AGENT_THRESHOLD and weakSideCount == 0` (pool-size gate, one agent max); (M) `ATTACK_DWELL_TICKS [EST]` → `[GT]` throughout sections 2/3 (promotion was declared in §6.1 and Appendix A §A.1 but not propagated to §2.2.4 / FR-AT-022 / FR-AT-023 / §3.12 / §3.14); (M) §3.12 `HysteresisEntry` struct completed (added `candidateRole` and `candidateDwell` fields the algorithm uses; `prevPhase` moved to `TransitionHoldState`); (M) §2.2.4 / §2.2.5 struct field names made consistent with §3.9 / §3.12 / §3.13 / §6.5; (M) §3.13 `hysteresisState.prevPhase` → `transitionHoldState.prevPhase`; (M) §5.2 T-AT-U-048 wrong expected output corrected; (L) §3.8 `overloadFlank = NONE` removed; §6.5 memory estimates corrected. **Outstanding (gates `IN REVIEW → APPROVED`):** (a) ERR-015-001 `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocation in #16 §3.4; (b) ERR-015-002 `TacticalContext.AttackIntent[]?` ratification in #8 §2.2.6; (c) ERR-015-005 one-token back-prop to #8 §1.3.2; (d) #12 Positioning AI reaches APPROVED; (e) ERR-015-003 / ERR-015-004 channel rows in #17 §3.10; (f) lead-developer R-01..R-05 sign-off. **Counts: 16 APPROVED, 0 SUSPENDED, 4 IN REVIEW (#11 / #12 / #14 / #15), 0 IN PROGRESS, 0 NOT STARTED.**
- **May 17, 2026 — Defensive AI (#14) reclassified `NOT STARTED` → `IN REVIEW`.** Section files v0.1 authored from `outline-detailed.md` v1.0; PASS-1 adversarial review (`adversarial-review-section-files-v1.md`, 17 findings: 6 H / 7 M / 4 L) filed and resolved in v0.2 fix pass same day. **Key fixes:** H1 §3.3.3 hysteresis pre-check now guards `mode != ZONAL`; H2/H4 missing struct fields declared (`overriddenThisTick`, `isManuallyAssigned` in `MarkAssignment`; four-field `MarkHysteresisState`); H3 Invariant 1 `>` → `>=`; H5 Invariant 1 demotion candidate null/override guard; H6 offside trap loop skips emergency overrides. **Outstanding (gates `IN REVIEW → APPROVED`):** (a) ERR-014-001 `TacticalContext.MarkDirective?` ratification in #8 §2.2.6; (b) #12 Positioning AI reaches APPROVED; (c) ERR-014-002 / ERR-014-003 channel rows in #17 §3.10; (d) ERR-014-004 `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` allocation in #16 §3.4; lead-developer R-01..R-05 sign-off. **Counts: 16 APPROVED, 0 SUSPENDED, 3 IN REVIEW (#11 / #12 / #14), 0 IN PROGRESS, 1 NOT STARTED (#15).**
- **May 17, 2026 — Pressing AI (#13) reclassified `NOT STARTED` → `IN REVIEW`.** Section files v0.1 authored May 17 from `outline-detailed.md` v1.0; PASS-1 adversarial review (`adversarial-review-section-files-v1.md`, 17 findings: 6 H / 7 M / 4 L) filed same day; all 6 H and 7 M findings resolved in v0.2 fix pass; all 4 L findings addressed with OI tracking. **Key fixes:** AR-S1-H1 `#8 §1.4.21` → `#8 §1.3.2` anchor (stale anchor class hazard); AR-S1-H2 BACKWARD_PASS reformulated using `TargetPosition - passerPosition` direction (PassAttemptEvent has no velocity field; FR-08 → FR-10 citation corrected); AR-S1-H3 fatigue-inversion corrected in §3.3 (`Stamina ≤ CEILING` → `Fatigue < CEILING`) with §3.0 preamble added; AR-S1-H4 `GetPhase` / `GetLine` Stage 1+ back-prop requests filed as ERR-013-007 / ERR-013-008; AR-S1-H5 FR-PR-023 rewritten (threat-score selection; category-error clause removed); AR-S1-H6 `r.perceivedPressure` replaced with locally-computed `geometricPressureOn(r)`; `receiverProgressionGain` formula + worked example added. **Outstanding (gates `IN REVIEW → APPROVED`):** ERR-013-001 mechanism choice (OI-001); ERR-013-005 domain-tag (OI-006); Appendix A derivation entries for `[EST]` constants (OI-003); ERR-013-007 / ERR-013-008 #12 back-prods (OI-008 / OI-009); #19 test-prefix conformance grep (OI-010); lead-developer R-01..R-05 sign-off (OI-004). **Counts: 15 APPROVED, 0 SUSPENDED, 3 IN REVIEW (#11 / #12 / #13), 0 IN PROGRESS, 2 NOT STARTED (#14 / #15).**
- **May 16, 2026 (later same day) — Goalkeeper Mechanics (#11) reclassified `NOT STARTED` → `IN REVIEW`.** Section files v0.1 authored from `outline-detailed.md` v1.2; section-files PASS-1 adversarial review (`adversarial-review-section-files-v1.md`, 11 findings: 3 H / 5 M / 3 L) filed and resolved in v0.2 same day. **Outstanding (gates `IN REVIEW → APPROVED`):** (i) `ERR-011-001` `DOMAIN_TAG_GOALKEEPER` allocation in #16 §3.4 (proposed `0x17`; if ERR-012-001 lands first, shifts to `0x1D` per KD-7 collision-management policy); (ii) OI-002 #18 Appendix F.0 channel-registry rows for 12 `gk.*` channels at Stage 0+1; (iii) OI-003 DOI verification for four `[CITATION-PENDING]` external references in §8.3 (Savelsbergh 2002 already DOI-verified; Opta/StatsBomb retained as commercial-data class); (iv) OI-005 atomic patch to Positioning AI #12 §3.3.3 / §6 promoting `GK_DEPTH_M`/`GK_ADVANCE_FACTOR`/`GK_LATERAL_FACTOR` `[EST]` → `[GT]` per KD-13 (gated on #11 IN REVIEW); (v) OI-006 `Ball.SetPossessor` surface verification against Ball Physics #1 §3.1; (vi) lead-developer R-01..R-05 sign-off. KD-12 dive-Z ownership preserved (#11-owned synthetic Z trajectory at Stage 0; retires to AM #2 native Z at Stage 1+ per §7.5). **Counts: 15 APPROVED, 0 SUSPENDED, 2 IN REVIEW (#11 / #12), 0 IN PROGRESS, 3 NOT STARTED (#13 / #14 / #15).**
- **June 20, 2026 (later same day) — Tactical Instructions (#21) advanced `IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted (§9.4). Section files at v0.3 after two adversarial passes — PASS-1 (`adversarial-review-section-files-v1.md`, 2H+4M+4L) and PASS-2 (`…-v2.md`, 1H+4M+1L), all resolved; PASS-2 H-1 (digest-identity vs FR-TI-028) and PASS-1 H-1/H-2 (Tempo phantom hook, StyleProfile double-drive) were the load-bearing finds, each caught by fact-checking the seam claims against source. **Post-APPROVED follow-up (non-blocking):** G2 `RoleWeightModifiers`/§3.2 **balance pass** — the `[GT]` values in §3/Appendix A are illustrative until a numerical-mirror + balance review pins them at Stage-1 implementation time; tests assert shape/direction, not magnitude (§5.6). This deferral follows the #8 (draft-level) / #9 (§9.8) / #16 (post-approved items) precedent and does **not** gate sign-off. ERR-021-001..004 remain Stage-1 implementation-time back-props (§8.3). Runtime activation stays gated on the `[GT]` config-loader + match-engine Phase C/D (KD-8). **Counts: 21 APPROVED, 0 SUSPENDED, 0 IN REVIEW, 0 IN PROGRESS, 0 NOT STARTED.**
- **June 22, 2026 — Living World System (#22) advanced `IN REVIEW → APPROVED`.** Second Stage-1 forward spec (Priority 6); lead-developer R-01..R-05 sign-off granted (§9.4); §9.5 decision APPROVED; all 11 section files at status APPROVED (checklist v1.0). Off-pitch interaction-generation layer that consumes the vol-2/vol-3 human-systems model **read-only** and adds the three constructs canon lacks: per-edge episodic memory, deterministic procedural text (`InteractionIntent` ≠ surface), and emergent narrative arcs. Ten section-file adversarial passes (no High in any; 4M+3L → … → 2M+1L, all resolved) — load-bearing finds: PASS-3 `PlayerEdge` double-authority (now a read-only mirror of vol-2 §2.1), PASS-6 RNG `world.arcs`/`world.text` sub-stream split (aperiodic draw interleaving), PASS-10 `ColdSummary.NextEpisodeId` (episodeId monotonicity across cold-store). 34 FRs (FR-LW-001..034); XC-022-001..014; ERR-022-001..004 Stage-1 back-props (non-blocking); ≥75 tests. **T0 `src/living-world/` scaffolding landed:** data types + `LivingWorldMath` (§3.1 update/decay + FR-LW-021 eviction tiebreak) + ordinal/determinism unit tests (self-contained, engine-free; asmdefs CI-gen-validated). **Post-APPROVED (non-blocking):** G2 `[GT]` balance pass — §3/Appendix A magnitudes illustrative, tests assert shape/direction not value (precedent #21 G2 / #8 draft-level / #9 §9.8 / #16). Runtime activation gated on KD-10 (world store + season loop; vol-2/vol-3 impl.; `[GT]` config-loader; match-outcome events). **Counts: 22 APPROVED, 0 SUSPENDED, 0 IN REVIEW, 0 IN PROGRESS, 0 NOT STARTED.**
- **June 20, 2026 — Tactical Instructions (#21) added `NOT STARTED → IN REVIEW`.** First spec beyond the Stage-0 set of 20; a Stage-1 forward spec for the manager-facing tactical instruction input layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems (#8/#11–#15). Promoted from `docs/tracking/tactical-instruction-layer-design.md` v0.3 (three adversarial passes). 11 section files at v0.1; 32 FRs (FR-TI-001..032); ≥78 tests; XC-021-001..014; ERR-021-001..004 (all Stage-1 implementation-time, non-blocking for approval). **Outstanding gates (§9.2):** formal section-file PASS-1 + fix pass; `RoleWeightModifiers`/§3.2 balance pass (values currently illustrative); lead-developer R-01..R-05 sign-off. **Runtime activation** separately gated on the `[GT]` config-loader + match-engine Phase C/D (KD-8). Counts: **20 APPROVED, 0 SUSPENDED, 1 IN REVIEW (#21), 0 IN PROGRESS, 0 NOT STARTED.**
- **July 22, 2026 — Squad / Player Data Layer (#27) added `→ IN REVIEW`.** First management-layer
  spec promoted (candidate set #27–#50 scoped in `docs/tracking/management-layer-spec-roadmap.md` +
  `spec-plans/`). Section files v0.1 authored from `docs/tracking/squad-player-data-design.md` v0.6
  (converged AR-1..AR-2 + T0 implementation-time corrections). FR-SQ-001..026; the canonical
  `PlayerAttributes` record + deterministic `RosterGenerator` + `SquadFileLoader` in
  `src/player-database/` are already built, and T1/T2 (attribute projection / `ConfigureSquads`,
  closing `ERR-007`), T3 (roster-reference snapshot field), the Phase-2 distinct-squad restore
  re-projection, and `LineupSelector` are already wired — so the section files document a landed
  layer and record wiring status in §7. **Outstanding gates (§9.3):** section-file PASS-1
  adversarial review + v0.2 fix pass; formal `SubsystemOrdinals.PlayerDatabase = 81` /
  `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` cross-cite confirmation in DeterministicSim #16 §3.4;
  lead-developer R-01..R-05 sign-off. Count: **26 APPROVED, 1 IN REVIEW (#27), 0 NOT STARTED.**
- **July 22, 2026 (later same day) — Squad / Player Data Layer (#27) advanced `IN REVIEW → APPROVED`.**
  Wave 0 of the management-layer roadmap complete. PASS-1 (0H+1M+2L) + AR-2 convergence sweep
  (0H+1M — §7.2 GK/Heading projection deferral stale; new §7.3 records `ToGoalkeeper`/`ToHeading`
  LANDED — L-only otherwise ⇒ CONVERGENCE) resolved; the #16 §3.4 cross-cite filed
  (`DOMAIN_TAG_PLAYER_DATABASE = 0x1F` + the retroactive `DOMAIN_TAG_LIVING_WORLD = 0x1E` it follows;
  ERR-027-001 + ERR-022-001, `spec-error-log.md` v1.32); lead-developer R-01..R-05 sign-off granted
  (§9 checklist v0.3). All 11 section files `Status: APPROVED`. Count: **27 APPROVED, 0 IN REVIEW,
  0 NOT STARTED.**
- **Specs were renumbered** during early development. Original plan had different ordering. Many early-written files contain stale spec numbers from the old scheme. The numbers in this file are canonical. See FORMER NUMBERING table below.

---

## FORMER NUMBERING (for reference when fixing stale references)

These are numbers that appear in older spec files and are WRONG:

| Old Reference | Correct # | Spec Name |
|---------------|-----------|-----------|
| First Touch = #11 | **#4** | First Touch Mechanics |
| Pass Mechanics = #4 | **#5** | Pass Mechanics |
| Shot Mechanics = #5 | **#6** | Shot Mechanics |
| Perception System = #6 | **#7** | Perception System |
| Decision Tree = #7 | **#8** | Decision Tree |
| Fixed64 Math = #8 | **#9** | Fixed64 Math Library |
| Heading Mechanics = #9 | **#10** | Heading Mechanics |
| Goalkeeper = #10 | **#11** | Goalkeeper Mechanics |