# D-P7-6 — Interactive career matches: amendment draft to Season & Competition Loop #30

**Created:** November 2, 2026\
**Last Updated:** November 2, 2026\
**Version:** 0.2\
**Status:** DRAFT — PROPOSED, NOT APPROVED. Not a normative spec. No work is authorized by this file;
it takes effect only when approved and applied to the owning specs with their ERR entries (§8).\
**Purpose:** turn the owner's D-11 product decision (roadmap §5.1 register, as amended November 2,
2026: PM-2 managed fixtures are played interactively; there are no mid-match saves; quitting a match
abandons it and returns to Career Home with the round unplayed) into a reviewable technical contract
for #30, with the cross-spec consequences for #41, #44 and the match client named. It replaces the
bullet-list contract first recorded in
[`interactive-unity-client-design.md`](interactive-unity-client-design.md) §5-P7 *Remaining* item 2.\
**Scope:** documentation only. No source, test, schema or format change in this file.\
**Owning specs:** #30 (`docs/specs/season-competition-loop/`); consequences for #41
(`injuries-medical/`), #44 (`discipline-suspensions/`) and the client design supplement. Roadmap
ordering: [§5.1a](path-to-playable-roadmap.md#51a-working-sequence-to-early-access--november-2-2026)
career lane item 2.

---

## 1. What exists today (verified against `main` `4bcc2ae`)

| Fact | Source |
|---|---|
| A round resolves inside one synchronous call. `PlayNextRound` runs the matchday-morning career day-steps, then resolves fixtures **in fixture-index order**, applying each one immediately (appearances → table → outcome event → mark played → ban serving → card commit), then advances the cursor. | `src/season-save/SeasonLoop.cs` `PlayNextRound` (~l.1030–1185); #30 §3.4 |
| KD-9 / FR-SN-012: a fixture-day resolves the **whole** round; resolving a strict subset is forbidden. | #30 §1.4 KD-9, §2.1 FR-SN-012 |
| Non-managed fixtures are resolved by `RoundResolutionModel.Resolve(fixture, seed, seasonNumber, homeMean, awayMean, worldDay)`: keyed per fixture, no shared cursor. | `SeasonLoop.ResolveFixture` (~l.1804) |
| Matchday-morning career day-steps (slots 2 and 4, including the injury occurrence draw) run before any fixture and are idempotent per day. | `SeasonLoop.cs` ~l.960–967, ~l.1090–1098; ERR-030-027 |
| The managed engine is booted by `BootFixtureEngine` (availability filter, #29 entry fatigue, `ConfigureSquads`) from a fixture-keyed seed. `PlayThroughEngine` seeds a `CardLedgerFold` from `PlayerIdsByAgentId()` before the first tick and pumps `fold.ObserveTick(tap)` after **every** `RunTick`; a skipped pump loses that tick's cards. | `SeasonLoop.PlayThroughEngine` (~l.1905–1960) |
| The fold tracks slot **occupancy** through `SubstitutionEvent`s, so cards are attributed correctly across substitutions. It is not serialized, which is safe only while no save can be taken mid-fixture. | `src/discipline/CardLedgerFold.cs` type remarks (L2, L19) |
| `FieldedXi` returns the **starting** eleven. Both #41's appearance record and #44's ban-serving exemption consume it; the code records (M5) that they are correct only because no production path substitutes. | `SeasonLoop.FieldedXi` remarks (~l.2105–2122) |
| The career client never writes a match block and `CareerSession` refuses to load one. | `CareerSaveSlots.cs:75`; `CareerSession.cs:185` |
| `season-save` does not reference `match-client-core`; `client-app` references both. | the three `.asmdef` files |
| `MatchSession` has a private engine-wrapping constructor, a public `AttachTickObserver` (runs after every tick on the sim thread) and `TickOnce`. | `src/match-client-core/MatchSession.cs` l.72–107, 247, 287 |

## 2. Contract (proposed)

### 2.1 Owner decisions this contract implements

- **Interactive managed fixtures** (D-11).
- **No mid-match saves** (D-11 as amended November 2, 2026). Saving is unavailable from entering the
  matchday until the player is back on Career Home. Mid-match saving was costed in v0.1 of this draft
  (a new managed-fixture sub-blob, season-save format 8 → 9, a restore path, and every engine snapshot
  schema change invalidating mid-match saves) and withdrawn.
- **Quitting a match abandons it** and returns to Career Home with the round unplayed. No result,
  card, appearance or table change from the abandoned match is kept. No forfeit, no instant
  finish, no kickoff autosave: a player who wants to replay a matchday can already reload a manual
  save, so blocking replays would prevent nothing.
- **No starting-XI changes in PM-2** (owner answer to v0.1 Q1, November 2, 2026): D-P7-4's automatic
  selection stands; Tactics Setup changes team tactics only.

A crash or an application exit during a match loses the match and everything since the last save, as
an exit anywhere else in the career does today.

### 2.2 Round order: unchanged

Fixtures keep resolving in fixture-index order and are applied by the existing per-fixture block. The
managed engine is booted at `BeginManagedFixture`, before any fixture is applied; its result is folded
in at its own index position during `CompleteManagedFixture`. The synchronous `AdvanceAndPlayNextRound`
becomes `Begin` + a tick loop + `Complete`, so there is one implementation.

Booting the managed engine before the earlier-indexed fixtures are applied is expected to give the same
engine: the managed squad's availability, entry fatigue and seed do not depend on other clubs'
fixtures, and nothing is applied between boot and kickoff. Lock T-IM-01 proves headless results,
digests and save bytes are unchanged.

### 2.3 Round states

| State | Entered by | Season state | Save available |
|---|---|---|---|
| **Closed** | cursor advance; `AbandonManagedFixture`; any load | pre-round | yes, as today |
| **Open** | `BeginManagedFixture` | matchday-morning steps done; nothing applied | no |
| **Live** | first engine tick | as Open | no |
| **Ended** | `MatchEnded` | as Open | no; the client calls `CompleteManagedFixture` before Post-Match |
| **Committed → Closed** | `CompleteManagedFixture` | round fully applied, cursor advanced | yes; the D-P7-2 post-round autosave follows |

`AbandonManagedFixture` from Open, Live or Ended discards the engine, the fold and the participation
record and returns to Closed. The matchday-morning steps that `Begin` ran stay run; they are idempotent
for the day, so the next `Begin` repeats them as no-ops and boots the same engine.

While a round is Open, Live or Ended, every other mutating `SeasonLoop` command (`AdvanceDays`,
`AdvanceToNextFixtureDay`, `AdvanceAndPlayNextRound`, `RollToNextSeason`, `SubmitTransferBid`, finance
transactions) is refused with a named failure mode (new F-row), and the client offers no save.
Read-only views stay available.

### 2.4 Seams and ownership

**`season-save` (#30) owns the engine, the fold and the participation record.** New public surface on
`SeasonLoop` (names indicative):

- `ManagedFixtureHandle BeginManagedFixture()` — runs the existing guards and day-steps, boots the
  engine through `BootFixtureEngine`, seeds the fold and the participation record, enters Open. Refused
  in `QuickSimAll` mode and when the round has no managed fixture.
- `ManagedFixtureHandle.Engine` — the booted engine, for the client to wrap; never re-booted.
- `ManagedFixtureHandle.ObserveTick()` — pumps the fold tap and the participation record for the tick
  just completed. MUST run exactly once after every engine tick; the handle counts engine ticks against
  observations and fails loud on a gap (the fold's existing lossless-consumption rule).
- `MatchResult[] CompleteManagedFixture()` — requires `Engine.MatchEnded` and no observation gap;
  resolves the round in index order (§2.2) and advances the cursor.
- `void AbandonManagedFixture()` — §2.3.

`SeasonLoop.ActiveMatch` stays the synchronous-path accessor.

**`match-client-core` gains one entry point:** `MatchSession.Attach(MatchEngine engine)` — wraps an
engine it did not boot (the existing private constructor), applying no boot mutator. The client calls
`AttachTickObserver(_ => handle.ObserveTick)`, so the observation runs after every tick on the sim
thread at any playback speed. `match-client-core` takes no reference to `season-save`.

**`client-app` composes them:** Career Home → Tactics Setup → Match → Post-Match → Career Home as new
named moves, plus Match → Career Home through abandon (`interactive-unity-client-design.md` §5-P7,
ux-experience-architecture §3.1). `CareerSession` keeps refusing a slot that holds a match block.

### 2.5 Participation: who played

The existing finalization consumes the starting eleven for two purposes. With substitutions possible,
both need the **participation set**: every player who occupied an on-pitch slot at any tick, per club.
On the headless path (no substitutions) it equals the starting eleven, so headless behaviour is
unchanged.

- **#41 appearance record** (`RecordFixtureAppearances`, FR-MD-010 window): proposed rule — any minutes
  on the pitch is an appearance. **#41 owner sign-off needed** (ERR on #41).
- **#44 ban serving** (`OnClubFixturePlayed`, ERR-044-003 stage 1 exemption): proposed rule — "fielded"
  means "in the participation set". A suspended player is never on the bench (the availability filter
  runs on the squad), so the only route in is the existing extremis back-fill. **#44 owner sign-off
  needed** (ERR on #44).
- The record is derived from the same `SubstitutionEvent` stream the fold already folds; implement it
  once, beside the fold, not as a second walker.

### 2.6 Pre-kickoff choices

Team tactics chosen on Tactics Setup are manager commands applied at tick 0 through the existing
command queue. The starting eleven is not editable (§2.1).

## 3. Open questions

No product questions remain open. The participation rules in §2.5 need the #41 and #44 spec owners'
acceptance.

## 4. Normative edits proposed for #30 (applied only after approval)

- **KD-9 / FR-SN-012:** a round may span `BeginManagedFixture` … `CompleteManagedFixture` or
  `AbandonManagedFixture`; no fixture of the round is applied before `Complete`, and `Complete` applies
  all of them.
- **FR-SN-013:** the managed fixture's appearance and serving inputs come from the participation set
  (§2.5).
- **New FR-SN-035..** (numbers allocated at application): the round states and command refusals
  (§2.3); the `Begin`/`ObserveTick`/`Complete`/`Abandon` seams and the observation-gap fail-loud
  (§2.4); commit before Post-Match.
- **FR-SN-021 / KD-1:** a career save is never taken while a round is Open, Live or Ended; the frame's
  `matchPresent` slot stays as specified but the career route never writes it. **No
  `SEASON_SAVE_FORMAT_VERSION` change.**
- **§2.3 failure modes:** a command while Open/Live/Ended; an observation gap; `Complete` before full
  time; `Complete` or `Abandon` with no open round.
- **§4.3:** the handle is the interactive owner; `_activeMatch` remains the synchronous-path accessor.

## 5. Locks (proposed §5 additions)

| ID | Lock |
|---|---|
| T-IM-01 | **No-input parity.** Same seed, rosters, tactics and configuration: an interactive managed match with no manager commands, driven through `MatchSession.Attach` + `TickOnce`, equals the headless `ManagedThroughEngine` round: final digest, every `MatchResult`, committed card tally, appearance records, bans, and post-round `SeasonLoop.Snapshot()` and save bytes. |
| T-IM-02 | **Input replay.** Same initial state + the same tick-stamped command log gives the same final digest and committed season state. |
| T-IM-03 | **Season-commit equivalence.** Given the same managed result and participation set, `CompleteManagedFixture` and the synchronous path produce byte-identical `SeasonLoop.Snapshot()` and identical subsystem blocks. |
| T-IM-04 | **Abandon.** After `Abandon` from Open, Live (including after a substitution and a card) or Ended: no result, card, appearance, ban or table change is applied; the state equals the state immediately after `Begin`; a new `Begin` boots an engine with the same initial digest; a full replay then commits normally. |
| T-IM-05 | **Exactly once.** `Complete` before full time is refused; a second `Complete` is refused; no card or ban is applied twice. |
| T-IM-06 | **Command refusal.** Every mutating command named in §2.3 is refused while Open/Live/Ended and leaves state byte-identical. |
| T-IM-07 | **Observation gap.** Ticking the engine without `ObserveTick` makes `Complete` fail loud. |
| T-IM-08 | **Participation.** With a substitution, the substituted-on player gets an appearance and counts as fielded for #44; with none, the set equals the starting eleven. |

Every lock is mirrored for the managed club at home and away.

## 6. What this does not do

- No mid-match save, no persisted command log, no change to the season save format.
- No change to round order, non-managed results, the quick-sim model, engine behaviour or any `[GT]`.
- No forfeit, instant finish or kickoff autosave.
- No starting-XI selection.
- No #38 screen-spec change beyond the client-design navigation moves.

## 7. Cost and sequencing

About "L" for career item 2: one #30 amendment without a format change, a `match-client-core` entry
point, #41/#44 participation errata, and the client route. It runs in the career lane;
`src/match-engine/` should need no change (if it does during the W8 B→C window, it carries
digest-neutrality evidence or waits, per the roadmap). Host: compile plus filtered EditMode for the
`SeasonSave`, `MatchClientCore`, `ClientApp`, `Discipline` and `InjuriesMedical` test assemblies
(D-01a items 1–2); no whole-MatchEngine run unless engine source changes.

## 8. Path to approval

1. #41 and #44 owners accept or amend §2.5.
2. Adversarial review of this draft to convergence (the spec-promotion / W8 C precedent).
3. Owner approval of the wording.
4. Application landing: #30 sections 1–5, #41 and #44 errata, ERR entries allocated against that head
   (`spec-error-log.md`), client-design §5-P7 updated. Code follows in its own landing.

## Review history

| Version | Date | Change |
|---|---|---|
| 0.2 | 2026-11-02 | Owner decisions applied: no starting-XI changes (v0.1 Q1, answer (a)); no mid-match saves; quitting a match abandons it and returns to Career Home with the round unplayed; no kickoff autosave (v0.1 Q2/Q3 withdrawn). Removed: the managed-fixture sub-blob, season-save format 8 → 9, restore path and live-save locks, and the managed-first round order (index order kept; headless path unchanged). Added: `AbandonManagedFixture`, the abandon lock, save unavailability while a round is open. Size back to about "L". Not yet adversarially reviewed. |
| 0.1 | 2026-11-02 | Initial draft, from the November 2 review of the client-design bullet contract: substitutions vs starting-XI consumers, unserialized card fold, engine/observer ownership across assemblies, round order and states, save-policy owner questions. |
