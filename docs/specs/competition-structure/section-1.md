# Competition Structure #43 — Section 1: Introduction

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Scope

**The competition set as a first-class collection** over #30's season loop: multiple concurrent
competitions a club is entered in (league, domestic knockout cup, continental group+knockout),
**deterministic knockout draws**, and **promotion/relegation** as a season-boundary transform at
the insertion point #30 pre-declared (FR-SN-031 (a')). #43 state advances on the world tick /
fixture-day flow and persists alongside #30's season/career save.

**Stage-2 minimal (always present, behaviour-neutral)** = the collection holds exactly one
competition — the #30 league — as a **binding row** (an id/tag recording "the league lives in
#30"; no stored #30 object, no data migration). Every instance-0 read goes through the composition
root against #30's read surface. No draw is made, no bracket exists, promotion/relegation never
runs (a one-division world), the (a') point stays empty exactly as #30 shipped it — a season
advances **byte-identical to pre-#43**; the only new artifact is #43's own (nearly empty) save
sub-blob (the #34/#32 posture).

**Deep** = populating the collection: a domestic knockout cup, a continental group+knockout
competition — each an **instance** with its own entrant set + format driving the same
`FixtureScheduler`/`LeagueTable`/resolution machinery — **keyed knockout draws** on the
then-registered `competition.draws` stream, a **second division** activating the (a')
promotion/relegation transform, and the **merged fixture-day view** interleaving competitions
congestion-free. Populating the collection is data, not a rewrite; the league instance's path
never changes (KD-8, one code path).

## 1.2 Out of scope (owned elsewhere, referenced as seams)

- **The base fixture/table engine (#30 Season & Competition Loop).** #30 owns
  `FixtureScheduler.Generate(clubIds, seed)` (FR-SN-001 — pure over `(clubIds, seed)`),
  `LeagueTable`/`Empty(clubIds)` + the FR-SN-007 total-order tie-break, `SeasonCalendar`, the
  round-resolution paths (real `MatchEngine` + FR-SN-013a quick-sim), and the boundary roll.
  **Already competition-instance-shaped** (#30 §7: "multiple competitions reuse
  `FixtureScheduler`/`LeagueTable` per competition") — #43 reuses them per instance and rewrites
  nothing (FR-CP-006).
- **Match play (`MatchEngine`)** — #43's fixtures resolve through the same paths #30's do.
- **Discipline / suspensions (#44).** #43 carries a `CompetitionId` on its fixtures/results so #44
  can scope suspensions per competition; no discipline model here (FR-LW-031).
- **National-team tournaments (#36).** A later overlay of the same calendar/competition model; no
  interface built.
- **Finances / prize money (#40).** #40's `SettleFinances` runs at (b') **after** #43's (a') —
  the ordering #40 §1 pinned at its own approval ("the budget depends on the club's post-promotion
  division"). Per-competition prize money is a #40 deep extension. #43 owns no money (FR-CP-021).
- **The season save root (#30).** `SeasonSaveCodec` composes #43's opaque sub-blob; #43 never
  references #30's assembly.

## 1.3 Dependencies

**Upstream (needs):** #30 (the instance-ready machinery + the (a') point, via the composition
root), #27 (the club universe, read-only ids), #16 (determinism namespace; the `competition.draws`
keyed stream at the deep tier).

**Downstream (consumers, deferred — no interface built, FR-LW-031):** #44 (per-competition
suspension scoping via `CompetitionId`), #36 (tournament overlay), #40 (per-competition prize
money, deep), #38 (competition/bracket screens).

Reference DAG: `compositionRoot → {#30, #43}`, `#43 → {#27, #16}` (minimal subset `{#27}`).
**Acyclic.** #43 does **not** reference #30, #40, #44, #36, #38, or #22.

## 1.4 Key decisions

- **KD-1 (a league IS a competition instance; instance 0 is a BINDING, not a stored reference).**
  `CompetitionFormat { RoundRobin, Knockout, GroupThenKnockout }` — a league is a `RoundRobin`
  instance, not a separate type (the minimal-first-as-identity constraint). Instance 0 is a
  **binding row only**: #43 holds no #30 object or live reference (a stored reference would bypass
  FR-SN-032's sole-writer/command-API discipline); instance-0 reads go through the composition
  root against #30's read surface (FR-SN-033's value-copy discipline). `CompetitionId` is
  `[FIXED]` config-assigned at genesis (deterministic; instance 0 = 0; never reused).
- **KD-2 (draws are keyed, not cursor-based — a recorded revision of the plan).** Knockout/group
  draws are position-independent keyed draws on `competition.draws` (`entityId = competitionId`)
  with a fixed-radix ordinal over `(seasonNumber, roundIndex, slotIndex, purpose)` (APPEND-only
  purposes — the #41 §3.1.1 / #32 §3.3 mechanism, and #30's own FR-SN-013a quick-sim shape). The
  plan's serialized-cursor proposal is dropped: a cursor is the *match-tick* pattern, would add
  save state, and would race across competitions drawing on one day — keyed draws dissolve all
  three (`entityId` isolates competitions; nothing serializes). The minimal tier makes **zero
  draws** ⇒ `_RESERVED_0x2C_`/94 stays reserved at approval, promoting at the deep tier's first
  draw.
- **KD-3 (brackets persisted, not regenerated).** Entrants change as rounds resolve, so a bracket
  is state: `BracketState` persists each round's entrant list + winners
  (serialize-don't-regenerate, the #28 KD-4 discipline) with fail-loud coherence gates. A restore
  never re-rolls a draw; the keyed draws make a re-derivation cross-check possible in tests, but
  the blob is authoritative on load.
- **KD-4 (promotion/relegation at the pre-declared (a'); membership-only; the mechanical hook
  named).** The transform is a pure swap over the divisions' final standings (bottom
  `RELEGATION_COUNT` ↔ top `PROMOTION_COUNT`, `[GT]`), mutating **division membership only** —
  `ClubId`s are stable world identities; no re-key, no migration hook (the #34 KD-7 class). It
  runs at FR-SN-031's (a'), before #40's (b'), inside #30's restartable roll (FR-SN-029). **The
  mechanical seam:** the membership output must be applied to every division instance's entrant
  set — including instance 0's `SeasonState.ClubIds` via #30's command API — **before** roll step
  (c) regenerates fixtures; the code-side hook is a T-phase #30 coordination (soft-reserved
  ERR-030-008), landing with #43 T2's second division as its own reviewed change.
- **KD-5 (concurrent scheduling — a #43-owned merged view; #30's calendar untouched).** #30's
  `SeasonCalendar` is and stays the league's round→day mapping. At deep, each instance owns its
  own mapping and #43 exposes a **merged next-fixture-day view** built by deterministic slotting:
  cup rounds only on days their entrants are league-free; one fixture per club per day (FR-SN-003
  lifted to the collection). The root queries the merged view **only when the collection has >1
  competition**; the deep multi-competition fixture-day driver is the other half of the
  soft-reserved ERR-030-008 coordination.
- **KD-6 (persistence — own sub-blob; instance 0 never duplicated).**
  `COMPETITION_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob in #30's `SeasonSaveCodec` (the
  #41/#33/#31/#34/#32 precedent); **no** `WORLD_STORE_FORMAT_VERSION` bump; the league stays in
  #30's blob (one source of truth) — #43's blob is nearly empty at minimal. Canonical-order
  decode gates; no RNG state.
- **KD-7 (canonical entrant ordering — the draw-determinism discipline).** Ascending-`ClubId`
  canonical base at every surface that can feed a draw (registry storage, decode gate, draw
  input); the drawn permutation is keyed Fisher–Yates over that base. The plan's
  iteration-order-over-an-unordered-collection trap is closed by pinning, and locked by the
  shuffled-input equivalence test.
- **KD-8 (behaviour-neutral identity; one code path).** Minimal = the singleton collection
  delegating to #30: no draw, no stream, no transform, (a') empty, the sub-blob nearly empty —
  byte-identical to pre-#43. Deep populates the collection; no consumer switches code paths.

## 1.5 Determinism & coordinate posture

All arithmetic is **integer** (ids, rounds, slots, counts). No float in #43. Round-robin fixtures
stay #30's pure function; knockout draws are keyed (KD-2); promotion/relegation is a pure
deterministic transform (no draw). One world clock; the minimal tier is draw-free.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §1 (scope, out-of-scope seams, dependencies, KD-1..KD-8, determinism posture), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
