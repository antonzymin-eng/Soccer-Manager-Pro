# Discipline & Suspensions #44 — Section 1: Introduction

**Created:** July 24, 2026
**Last Updated:** August 15, 2026, yet later still again (v0.5 — reviewed-findings pass: **`ERR-044-007`**
(M2) corrected KD-4's `FilterAvailable(in Squad) → Squad)` signature — the method has never taken `in
Squad` alone; it is `FilterAvailable(Squad squad, DisciplineState state, int competitionId)`, verified
against `src/discipline/Availability.cs` — omitting `state`/`competitionId` misstated it as a predicate
over `Squad` rather than over `DisciplineState`. **`ERR-044-006`** (M3) removed KD-4's surviving
"byte-identity-locked (the #32 T-SC-VIEW-001 class)" claim, which §9 G6 withdrew and §5.4 traced to a
deleted, tautological test the same day this row was still asserting it; replaced with G6's construction
argument — `Squad` sealed, deep-copying constructor, `GetPlayer` by value)
**Last Updated (prior):** August 15, 2026, yet later still (v0.4 — `ERR-044-008`, reviewed-findings pass: §1.2's
`#37` bullet was a fourth site of "one tap feeds both when built" — the same refuted claim `ERR-044-008`
corrects at §4.3, §7.3 and §8.1 XC-044-002, found on a full-file sweep after fixing those three.
Restated to name #44's own `IDisciplineTickLedgerTap`)
**Last Updated (prior):** August 15, 2026, later still (v0.3 — `ERR-044-009`, reviewed-findings pass: KD-2's
closing sentence still read "the `Incoming`-id semantics are absorbed by the fold either way and
re-verified at T-phase" — the T-phase re-verification this promised ran at T2 and FAILED
(`ERR-044-001`, Appendix C's original "slot 19" worked example was unimplementable), and the sibling
hedge in `appendices.md` was deleted at that same landing while this row was left standing. Restated
onto the verified synthetic bench-id formula, with the occupancy-seed coverage requirement it implies
made explicit)
**Last Updated (prior):** August 15, 2026 (v0.2 — ERR-044-003 stage 1, owner decision: KD-3 corrected — a ban
decrements per played club fixture the player did NOT appear in, not per played fixture full stop;
matters only in the extremis tier, #30 §2.3 F9's depleted-squad back-fill)
**Last Updated (prior):** July 24, 2026 (v0.1 — initial)
**Version:** 0.5
**Status:** APPROVED

---

## 1.1 Scope

**Season-level discipline as a read-only derivation**: accumulate the card events the match engine
already emits, apply literal threshold rules (N yellows → an accumulation ban; a dismissal → an
immediate ban), and expose a **per-player suspension-availability VIEW** consulted at squad
selection. #44 advances at fixture-resolution / world-tick cadence and persists alongside #30's
season/career save.

**The governing posture:** #44 **reads, never re-implements** — the in-match card mechanics
(`CardIssuedEvent` 0x06, second-yellow promotion, sent-off tracking) are `MatchEngine`-owned and
untouched — and **availability is a view, never a mutation** (the roadmap §5 invariant applied to
availability: a suspension filters a value-copy squad; `PlayerRecord`/`Squad` are never written).

**Live at minimal (the #41 class, not an identity scaffold):** a player crossing a threshold in
engine-resolved fixtures **is banned and the next lineup changes** — designed, deterministic
behaviour. The neutrality properties are: (a) **observer-neutrality** (the tap consumption never
perturbs the match digest — the `match-viewer` lock); (b) **no-trigger identity** (a season with
no threshold-crossing cards is byte-identical to pre-#44 except #44's own sub-blob); (c)
**determinism** (same events ⇒ same bans ⇒ same filtered squads).

**Minimal coverage (stated honestly):** quick-sim fixtures produce no cards (FR-SN-013a resolves a
scoreline — grep-verified), so minimal discipline accrues **only from engine-resolved fixtures**
(the managed club's — covering its own and its opponents' players in those matches).
Deterministic, asymmetric by construction; evened by the deferred #30-owned quick-sim synthesis
(§7).

## 1.2 Out of scope (owned elsewhere, referenced as seams)

- **In-match card mechanics (`MatchEngine`).** `ApplyCardAndCheckSentOff` publishes exactly one
  `CardIssuedEvent` per incident (kind 0 = yellow, 1 = straight red, 2 = SecondYellow — the
  promoted second yellow is a **single kind-2 event**, verified against source); slot discipline
  state and the v1.33 substitution reset stay engine-internal.
- **The observational read pattern (#37).** FR-AN-002's read-only **per-tick ledger tap** is the
  approved mechanism; #44 consumes the same tap **pattern** through its own
  `IDisciplineTickLedgerTap` (`ERR-044-008` — §4.1's reference rule makes #37's identical interface
  unreachable from either #44 or the composition root, so the shared TYPE this bullet used to
  promise never arrives) and invents no second read model.
- **The season loop (#30).** Owns the fixture flow (FR-SN-013), squad resolution
  (`ISquadProvider.ResolveByClubId → ConfigureSquads`), the day-advance, and the save root. #44's
  filter acts at the pre-declared resolve→configure seam (ERR-030-009); #44 never references #30.
- **Competition scoping (#43).** #43 carries `CompetitionId` on fixtures/results (FR-CP-020);
  #44's tally carries the partition key from day one (minimal: `0`) — activation is deep.
- **Transfers/retirement lifecycle (#31/#28).** Deliver the re-key/retirement events #44's
  hygiene consumes at T-phase (the FR-TX-022 hook / FR-TX-028 lifecycle coordination).
- **UI (#38).** Renders availability/suspension view models; deferred, no interface built
  (FR-LW-031).

## 1.3 Dependencies

**Upstream (needs):** the engine's Tier A card/substitution events via the #37-class tap (#17's
event model), #27 (`PlayerId`/`Squad`, read-only), #30 (the flow #44 hooks, via the composition
root). **Downstream (deferred consumers):** #38 (screens), #43 (partitions), #46 (news
aggregation of bans).

Reference DAG: `compositionRoot → {#30, #44}`, `#44 → {#17 (event types), #27 (read-only)}`.
**Acyclic.** #44 does **not** reference #30, #43, #38, #16's RNG service, or engine internals.
**No RNG stream, no domain tag, no ordinal** (the #37/#49 positive property — no #16 row needed).

## 1.4 Key decisions

- **KD-1 (persist the tally — forced by verification).** `SerializeLedger` is write-only (the #37
  KD-1 finding) and #30 retains no per-fixture ledgers (`MatchResult` is scoreline-shaped), so
  recompute-on-load has no input. One `DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob (KD-1/§4).
- **KD-2 (the read — the #37-class tap + an occupancy fold).** Post-match slot state is ruled out
  by the v1.33 substitution reset (a subbed-off player's cards vanish from the slot); ledger
  bytes are ruled out as engine-internal. The fold consumes the per-tick tap, seeds slot→player
  occupancy from the fixture's configured lineup (the root holds it), updates occupancy on each
  `SubstitutionEvent`, and attributes each card to the **occupant at the card's tick**. Unknown
  Tier A ordinals are ignored (the FR-AN-019/F5 forward-compatibility posture). **The
  `Incoming`-id semantics are the engine's synthetic bench-agent-id formula, verified — not
  "absorbed either way" (`ERR-044-009`).** `MatchEngine.SubstitutePlayer` derives a bench player's
  agent id as `SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM + benchIndex`, never an on-pitch slot
  index; the T-phase re-verification this row originally deferred ran at T2 and **failed** against
  Appendix C's first worked example, whose "slot 19" bench player was an on-pitch index under
  `SQUAD_SIZE = 22` (`ERR-044-001`). The occupancy seed a caller threads into `CardLedgerFold`'s
  constructor MUST therefore span every agent id the engine can address — on-pitch slots **and**
  the synthetic bench range `[SQUAD_SIZE, SQUAD_SIZE + 2 * SUBSTITUTES_PER_TEAM)` — with F1 firing
  on any gap rather than a card being attributed by guesswork.
- **KD-3 (ordering — no off-by-one).** Fold at fixture resolution; the availability filter runs
  at the **next** selection (ERR-030-009), so a card in fixture N bans for fixture N+1. A ban
  decrements once per **played fixture of the player's club that he did not appear in** —
  engine-resolved or quick-sim alike (the ban's clock is the club's calendar, not the resolution
  path). *(Amended ERR-044-003 stage 1, August 15, 2026: the exemption matters only in the
  extremis tier — #30 §2.3 F9's depleted-squad back-fill can field a suspended player, and without
  it that appearance also served his ban for free.)*
- **KD-4 (availability is a VIEW).** `IsAvailable` is a pure predicate over the tally;
  `FilterAvailable(Squad squad, DisciplineState state, int competitionId) → Squad` returns a
  **reduced value copy** for `ConfigureSquads` — three parameters over the tally and its competition
  partition, never `in Squad` alone (the signature has never had that shape; verified against
  `src/discipline/Availability.cs`, `ERR-044-007`); #27's canonical records are never written — not
  because a lock enforces it, but because `Squad` is sealed, deep-copies in its constructor, and
  returns records **by value**, so #44 has no write surface at all to be locked (G6's construction
  argument). The withdrawn "byte-identity-locked (the #32 T-SC-VIEW-001 class)" framing named a test,
  `T-DC-VIEW-001`, that was deleted as tautological at C1/C2 AR round 1 and never replaced
  (`ERR-044-006`).
- **KD-5 (de-dup — the emission contract, verified).** One event per incident: kind 0 ⇒ yellows
  +1; kind 2 ⇒ yellows +1 **and** a dismissal ban; kind 1 ⇒ a dismissal ban (no yellow).
  Double-counting is structurally impossible; no de-dup table exists.
- **KD-6 (partition key + hygiene — bans FOLLOW the player).** The tally keys `(PlayerId,
  CompetitionId)`, `CompetitionId = 0` at minimal (an `int` — no #43 reference). On a #31
  transfer re-key the tally + unserved bans **migrate** old→new `PlayerId` (a ban follows the
  player — the deliberate contrast with #32's drop-on-transfer knowledge rule, recorded so the
  two hygiene rules are never conflated); retirement drops the entry. Delivery: the FR-TX-022
  roster-move hook / #28 lifecycle coordination, wired at T-phase.
- **KD-7 (live-at-minimal staging).** The #41 precedent: minimal is behavioural (bans change
  lineups); neutrality is the three properties of §1.1, not an always-on identity scaffold.
- **KD-8 (season boundary).** At `RollToNextSeason`: tallies reset; **unserved bans carry** (the
  real-football rule). Genesis = empty; a load reconstructs and never resets a ban.

## 1.5 Determinism & coordinate posture

**No RNG stream, no domain tag, no ordinal.** The accumulation is a pure fold over
already-deterministic Tier A events in the bus's canonical publish order (tick + intra-phase
order, #17-pinned), thresholds are literal integers, serving is a per-fixture decrement — #44 adds
no stochastic surface and inherits round-trip determinism from #30 + the engine. Integer posture
throughout; no float.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §1 (scope, live-at-minimal posture, out-of-scope seams, dependencies, KD-1..KD-8, determinism posture), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: KD-3 corrected to state the ban decrement excludes a fixture the player appeared in (the extremis back-fill case), matching the amended FR-DC-011 / `OnClubFixturePlayed`. |
| 0.3 | 2026-08-15 | — | **`ERR-044-009`**, reviewed-findings pass: KD-2's "absorbed by the fold either way and re-verified at T-phase" closing sentence restated onto the verified `MatchEngine.SubstitutePlayer` synthetic bench-id formula — the T-phase re-verification it promised ran at T2 and failed (`ERR-044-001`), and this row was the one site left citing the disproven assumption after `appendices.md` v0.3 corrected the sibling hedge. See `spec-error-log.md` `ERR-044-009`. |
| 0.4 | 2026-08-15 | — | **`ERR-044-008`**, reviewed-findings pass: §1.2's `#37` bullet corrected — "one tap feeds both when built" is the identical refuted claim fixed at §4.3/§7.3/§8.1, found here on a full-file sweep for other instances; restated to name #44's own `IDisciplineTickLedgerTap`. See `spec-error-log.md` `ERR-044-008`. |
| 0.5 | 2026-08-15 | — | **Reviewed-findings pass.** **`ERR-044-007`:** KD-4's `FilterAvailable(in Squad) → Squad)` signature corrected to the landed three-parameter form (`Squad squad, DisciplineState state, int competitionId`) — verified against `src/discipline/Availability.cs`; the old form misstated the method as a predicate over `Squad` alone rather than over the tally. **`ERR-044-006`:** KD-4's "byte-identity-locked (the #32 T-SC-VIEW-001 class)" clause removed — §9 G6 withdrew the identical parenthetical the same day, tracing it to `T-DC-VIEW-001`, a test deleted as tautological at C1/C2 AR round 1 and never replaced (§5.4); replaced with G6's construction argument. See `spec-error-log.md` `ERR-044-006`, `ERR-044-007`. |
#endregion
