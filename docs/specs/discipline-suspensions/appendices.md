# Discipline & Suspensions #44 — Appendices

**Created:** July 24, 2026
**Last Updated:** August 16, 2026, later (v0.10 — final fixer pass over the reviewed-findings round.
**`ERR-044-016`** (M6): Appendix C's worked example was not producible by the live engine —
`MatchEngine.SubstitutePlayer` resets `_yellowCards[outSlot] = 0` on every substitution and
`ApplyCardAndCheckSentOff` returns kind 2 only when the recipient slot's OWN count is `>= 2` after
increment, so a slot's first booking since a reset can never legitimately be a kind-2. A kind-0 card
for slot 7 now sits between the substitution and the kind-2 card, with the ticks and resulting
tallies recomputed around it and the engine precondition (`_yellowCards[slot] >= 1`) stated
explicitly; the id arithmetic verified at `ERR-044-001` is unchanged. **M7**: Appendix A's four
`[GT]` threshold/ban rows renamed ALL_CAPS → PascalCase, matching `section-2.md`/`section-3.md` and
`DisciplineConstants.cs`. **L6**: the `CardLedgerFold.cs:66` line citation (both in this file's own
v0.8-chain header and its version-history table row) replaced with the member name `NO_PLAYER`,
annotated in place rather than silently rewritten.)
**Last Updated (prior):** August 16, 2026 (v0.9 — `ERR-044-014`, adversarial-review H1: Appendix C's worked
example updated for the amended `OnClubFixturePlayed(clubId, clubPlayerIds, fieldedPlayerIds)`
signature. Both ids are served because they are on club 7's ROSTER — the membership rule §3.3 now
reads rather than derives — not because `183 / 25 = 191 / 25 = 7`, which remains in the text as the
packing that makes the example's ids coherent)
**Last Updated (prior):** August 15, 2026, later still (v0.8 — reviewed-findings pass. **M5 (new id
`ERR-044-013`):** Appendix A gains a `CardLedgerFold.NO_PLAYER` row (`[FIXED]`, `-1`) — verified
present as `src/discipline/CardLedgerFold.cs`'s `NO_PLAYER` member (L6, August 16, 2026: the prior
`:66` line citation was verified-against-wrong-line, replaced with the member name — a line number is
not a stable citation), caller-facing (the constructor's F1-class guard
depends on the distinction) and used normatively by Appendix C below, but never catalogued. **L3:**
the `LEAGUE_COMPETITION_KEY` row (v0.7, `ERR-044-012`) renamed to `LeagueCompetitionKey` — ALL_CAPS
next to a `[CROSS]` tag contradicted `src/CLAUDE.md` §3.2.3's PascalCase rule for `[CROSS]`, the same
correction L17 already made for `CardKindYellow`/`Red`/`SecondYellow` two rows below; the two mentions
of the ALL_CAPS name in this file's own v0.7 history are left as-is, describing the code's state at
that landing)
**Last Updated (prior):** August 15, 2026, later (v0.7 — `ERR-044-012`, back-prop owed by
`src/discipline/DisciplineConstants.cs` v1.3 (M26, not filed with that code change): `LEAGUE_COMPETITION_KEY`
was `[FIXED]` in Appendix A, but it is a verbatim copy of APPROVED Competition Structure #43's
`LEAGUE_COMPETITION_ID` (`docs/specs/competition-structure/appendices.md`) — the root `CLAUDE.md` tag
table makes a value defined in another approved spec and consumed read-only `[CROSS]`, never `[FIXED]`,
the identical argument already applied to `CardKindYellow`/`Red`/`SecondYellow` two rows below.
Re-tagged; the literal `0` is unchanged, since #43 has no `src/` assembly to bind a compiler-checked
mirror to)
**Last Updated (prior):** August 13, 2026, later still (v0.5 — L17, a fifth adversarial-review pass over the
#44 C1/C2 landing: `CARD_KIND_YELLOW`/`RED`/`SECOND_YELLOW` were `[FIXED]` in the code but are #17
`CardIssuedEvent.CardKind` domain ordinals #44 consumes read-only — retagged `[CROSS]`, renamed
`CardKindYellow`/`CardKindRed`/`CardKindSecondYellow`, and Appendix A gains one row per constant
instead of folding them into the `CardIssuedEvent`/`SubstitutionEvent` ordinal row)
**Last Updated (prior):** August 13, 2026, later still (v0.4 — L12(a), a third adversarial-review pass over
the #44 C1/C2 landing: Appendix A's catalogue row "the 18-slot `ConfigureSquads` minimum … the F5
filter floor" removed — F5 was withdrawn at ERR-044-003 (§2.3), `src/discipline/DisciplineConstants.cs`
has no such constant, and #44 has implemented no viability gate since that withdrawal)
**Last Updated (prior):** August 13, 2026 (v0.3 — ERR-044-001, C1/C2 landing back-prop: Appendix B gains
the magic-before-version row + the MUST rule and Appendix A the `DISCIPLINE_SAVE_MAGIC` row;
Appendix C re-worked onto real engine ids after its "slot 19" worked example was verified
unimplementable, with the hedge deleted)
**Last Updated (prior):** July 24, 2026 (v0.2 — cross-set AR pass 3; prior v0.1 initial)
**Version:** 0.10
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `DISCIPLINE_SAVE_MAGIC` | `[FIXED]` | `"DISC"` (`0x44495343`) | the sub-blob's self-identifying leading tag, checked BEFORE the version (ERR-044-001 — a format version is not a format identifier). |
| `DISCIPLINE_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the discipline sub-blob's own version gate (KD-1); gated second, behind the magic. |
| `CardLedgerFold.NO_PLAYER` | `[FIXED]` | -1 | the occupancy-seed sentinel: this agent id maps to no player (§2.2, §3.1). Caller-facing — the constructor throws on any other negative occupancy value (F1's "any gap" refusal depends on the distinction) — and used normatively by Appendix C ("every other bench id `NO_PLAYER`"); had no catalogue row until `ERR-044-013`. |
| `YellowAccumulationThreshold` | `[GT]` | 5 (illustrative) | yellows per accumulation ban (§3.2); balance-pass-pinned against real-competition rules. |
| `AccumBanMatches` | `[GT]` | 1 (illustrative) | ban length for an accumulation threshold crossing. |
| `SecondYellowBanMatches` | `[GT]` | 1 (illustrative) | ban length for a kind-2 dismissal. |
| `StraightRedBanMatches` | `[GT]` | 2 (illustrative) | ban length for a kind-1 dismissal. |
| `LeagueCompetitionKey` | `[CROSS]` | 0 | the minimal-tier `CompetitionId` partition key (FR-DC-012) — a **verbatim copy** of APPROVED Competition Structure #43's `LEAGUE_COMPETITION_ID` (`docs/specs/competition-structure/appendices.md`, value 0, FR-CP-004), consumed read-only and never set independently here (`ERR-044-012` — the identical `CardKindYellow`/`Red`/`SecondYellow` argument two rows below, applied to this constant). PascalCase per `src/CLAUDE.md` §3.2.3's `[CROSS]` naming rule, the `CardKindYellow` precedent (L17) applied to this constant too. The literal `0` stays a literal rather than a compiler-checked mirror: #43 has no `src/` assembly yet to bind a `CompetitionStructureConstants` reference to. |
| `CardIssuedEvent` 0x06 / `SubstitutionEvent` 0x08 | `[CROSS]` | #17/engine | the fold's inputs (payloads verified — XC-044-001); kinds `{0,1,2}` with the single-event kind-2 contract. |
| `CardKindYellow` | `[CROSS]` | 0 | `CardIssuedEvent.CardKind`'s own domain ordinal (#17 Appendix A row 0x06 — "0=Yellow, 1=Red, 2=SecondYellow"); #44 consumes it read-only and never sets it independently (L17 — was `[FIXED]` `CARD_KIND_YELLOW`, the ALL_CAPS/`[FIXED]` mistagging root `CLAUDE.md`'s tag table rules out for a value defined in another spec). |
| `CardKindRed` | `[CROSS]` | 1 | as `CardKindYellow` — #17 Appendix A row 0x06 domain ordinal 1. Carries NO yellow (FR-DC-006). |
| `CardKindSecondYellow` | `[CROSS]` | 2 | as `CardKindYellow` — #17 Appendix A row 0x06 domain ordinal 2, the promoted second caution the engine emits as ONE event (KD-5 / FR-DC-006). |

**Tag note:** the `[GT]` magnitudes are illustrative pending the balance pass (the #21 G2
precedent) — the reviewed contract is the shapes (threshold-and-residual accumulation, additive
stacking, per-fixture serving), not the numbers.

## Appendix B — Discipline sub-blob layout (KD-1)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block
(every length-prefixed read `Require`-bounded against `total − offset`):

| Field | Type | Notes |
|---|---|---|
| magic | u32 | `DISCIPLINE_SAVE_MAGIC` = `"DISC"` (`0x44495343`); **checked BEFORE the version** (ERR-044-001) |
| version | u32 | `DISCIPLINE_SAVE_FORMAT_VERSION`; gate second (F3) |
| entryCount | u32 | `Require`-bounded (0 at genesis) |
| per entry: PlayerId | i32 | **strictly ascending `(PlayerId, CompetitionId)`** across entries (F3) |
| per entry: CompetitionId | i32 | `0` at minimal (the #43 partition key) |
| per entry: Yellows | i32 | `≥ 0` (F3) |
| per entry: BanMatchesRemaining | i32 | `≥ 0` (F3); carries across `RollToNextSeason` |
| (trailing-byte guard) | — | `if (o != len) throw` (F3) |

**A format version is not a format identifier (MUST, ERR-044-001).** Every sub-blob format under
the season frame sits at version 1, and this layout's `version | entryCount | entries…` prefix is
byte-shaped identically to the progression, training, medical and appearance blocks — so without the
leading magic, a transposed `byte[]` among `SeasonSaveCodec.Encode`'s now-seven identically-typed
payloads decodes cleanly, completely and silently as the wrong subsystem's state. This layout
originally specified the block version-first with no magic — the fourth instance of the defect
ERR-029-005 / ERR-041-009 turned into a MUST in #29 §4.4 and #41 §4.4 (ERR-028-004 is the third);
the table above is the correction's load-time half, `DisciplineConstants.DISCIPLINE_SAVE_MAGIC`
its implementation, and `SeasonSave.DisciplineBlock` the compile-time half at #30's frame.

**No RNG-state field of any kind** (FR-DC-016 — #44 has none — the magic is deliberately NOT an
RNG domain tag). Deep extensions (per-offence classes) **append** behind the version gate.

## Appendix C — Worked fold example (end to end)

**Re-worked at ERR-044-001, August 13, 2026 — the prior worked example put a bench player at
"slot 19" and hedged "(or 19 → 191, absorbed either way)".** Verified against the live engine
(§7.1 T2's own obligation, "the `Incoming`-id semantics verified against the live engine"):
`MatchEngineConstants.SQUAD_SIZE = 22`, so **19 is an on-pitch slot**, not a bench identity — the
hedge was the spec declining to verify a fact it could have checked. `MatchEngine.SubstitutePlayer`
derives a bench player's agent id as the **synthetic** `SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM
+ benchIndex`, which for `teamId = 0` (home) and `SUBSTITUTES_PER_TEAM = 7` occupies `[22, 29)` —
never an on-pitch index. Had this example been implemented as written, every post-substitution card
would have been misattributed and F1 would never have fired (an id in `[22, 29)` has no occupancy
entry under the old seeding).

Fixture N (engine-resolved), home team (`teamId = 0`). Lineup seeds on-pitch slot 7 → PlayerId 183
(club 7 — `183 / 25 = 7`); bench `benchIndex = 0` → PlayerId 191 (club 7, local 16 — same club, as a
lineup must be), occupying the synthetic agent id `22 + 0 * 7 + 0 = 22`. The occupancy seed spans
every agent id the engine can address (`SQUAD_SIZE + 2 * SUBSTITUTES_PER_TEAM = 36` entries): agent
id 7 → 183, agent id 22 → 191, every other bench id `NO_PLAYER` until used.

**`ERR-044-016` (August 16, 2026) — a kind-0 card now sits between the substitution and the kind-2
card, and the ticks are recomputed around it.** The version below this one had the kind-2 at tick
12 000 arrive as slot 7's FIRST card since the substitution — unproducible by the live engine:
`MatchEngine.SubstitutePlayer` resets `_yellowCards[outSlot] = 0` on every substitution, and
`ApplyCardAndCheckSentOff` returns kind 2 only when the RECIPIENT SLOT's own count is `>= 2` **after**
the increment — so a slot can only ever emit a kind-2 as its **second** booking since the last reset,
never its first. The id arithmetic (`183 / 25 = 191 / 25 = 7`, the synthetic bench id `22`) was
already verified correct at `ERR-044-001` and is unchanged; only the tap sequence and the resulting
tallies are recomputed.

Tap sequence: tick 4 000 `CardIssuedEvent{Recipient: 7, Kind: 0}` → occupant of slot 7 is 183 →
`Yellows` 4 → 5 ⇒ ban 1, `Yellows` 0 (threshold 5). Tick 9 000
`SubstitutionEvent{Outgoing: 7, Incoming: 22, Team: 0}` → `ApplySubstitution` reads the occupant of
the *synthetic* id 22 (191) and moves it onto slot 7 — occupancy[7] := 191 (slot identity is
unchanged; only who occupies it moves) — and clears the vacated bench id 22 to `NO_PLAYER`
(`ERR-044-021`: a later record naming agent id 22 now fails loud, F1, rather than silently
double-booking 191). This also resets the engine's OWN `_yellowCards[7]` to 0 (v1.33) — a fact about
the engine's card-KIND decision, never read by the fold, which tracks 191's accumulation tally by
occupancy, not by the slot's reset-prone internal count.

Tick 10 500 `CardIssuedEvent{Recipient: 7, Kind: 0}` (`ERR-044-016`, new) → occupant of slot 7 is
now **191** → `Yellows` 0 → 1, no ban (below threshold 5). **This card is the engine precondition
`_yellowCards[7] >= 1` that tick 12 000's booking needs to be legitimately emitted as a kind-2**: the
engine increments `_yellowCards[7]` to 1 here (kind 0, since `1 < 2`), so the NEXT booking on this
slot increments it to 2 and is returned as kind 2. Tick 12 000 `CardIssuedEvent{Recipient: 7, Kind: 2}`
→ occupant of slot 7 is still 191 → `Yellows` 1 → 2 (still below threshold) **and** ban +1
(`SecondYellowBanMatches`, one event, one yellow, one dismissal — KD-5). Player 191 ends the fixture
at `Yellows = 2, BanMatchesRemaining = 1`; player 183 ends it at `Yellows = 0, BanMatchesRemaining = 1`
(unchanged from tick 4 000 — the substitution moved occupancy, not his own tally).

Fixture N+1 selection: `FilterAvailable` excludes 183 and 191, so neither appears in club 7's fielded
eleven; after N+1 is played, `OnClubFixturePlayed(7, clubPlayerIds, fieldedPlayerIds)` decrements
both bans to 0 (both ids are on club 7's roster, so both are in `clubPlayerIds` — the §3.3 membership
rule, which since `ERR-044-014` READS the roster rather than deriving it from
`183 / 25 = 191 / 25 = 7`; neither id is in `fieldedPlayerIds`, so the ERR-044-003 stage 1 exemption
does not apply — see that section for the case where it would) — **both available for N+2, but only
183's row is DROPPED.** 183 reaches `(Yellows = 0, BanMatchesRemaining = 0)` and is removed
immediately per FR-DC-017's canonical-minimality rule; 191 reaches `(Yellows = 2,
BanMatchesRemaining = 0)` — a real, nonzero row that **survives**, carrying his two accumulated
yellows toward the next accumulation ban. The two players end the worked example in different states
for the same reason: the ban was served either way, but only one of them was clean going in.
All integer; two runs identical; #27 squads byte-untouched.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial appendices (constant catalogue, sub-blob layout, end-to-end worked fold example), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR pass 3 (M): Appendix C's bench player re-keyed 201 → **191** — 201 derives to club 8 (`201 / 25 = 8`), an impossible teammate of club-7's 183, and `OnClubFixturePlayed(7)` would never have decremented it; the example now derives coherently (`183 / 25 = 191 / 25 = 7`). |
| 0.3 | 2026-08-13 | — | **ERR-044-001** (C1/C2 landing, §7.1 T2's verification obligation): Appendix B's layout gains the `DISCIPLINE_SAVE_MAGIC` row before the version and the magic-before-version MUST (the ERR-029-005/ERR-041-009 class's fourth instance); Appendix A gains the matching catalogue row; Appendix C's worked example — verified against the live engine and found unimplementable, "slot 19" being an on-pitch index under `SQUAD_SIZE = 22` — re-derived onto the real synthetic bench-id formula (`SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM + benchIndex`), with the "(or 19 → 191, absorbed either way)" hedge deleted. |
| 0.4 | 2026-08-13 | — | **L12(a)**, a third adversarial-review pass over the C1/C2 landing: Appendix A's "the 18-slot `ConfigureSquads` minimum \| `[CROSS]` \| match engine \| the F5 filter floor" row deleted — F5 was withdrawn at ERR-044-003 (§2.3 F5, `section-2.md` v0.4) and no such constant exists in `DisciplineConstants.cs`; #44 implements no viability gate of any kind since that withdrawal, so nothing in the catalogue names one. |
| 0.5 | 2026-08-13 | — | **L17**, a fifth adversarial-review pass over the C1/C2 landing: `CARD_KIND_YELLOW`/`RED`/`SECOND_YELLOW` are #17 `CardIssuedEvent.CardKind` domain ordinals (Appendix A row 0x06) #44 consumes read-only and never sets independently — the root `CLAUDE.md` tag table makes that `[CROSS]`, not `[FIXED]`, and `src/CLAUDE.md` §3.2.3 makes `[CROSS]` PascalCase, not `ALL_CAPS`. Retagged and renamed in code (`CardKindYellow`/`CardKindRed`/`CardKindSecondYellow`); Appendix A's single combined `CardIssuedEvent 0x06 / SubstitutionEvent 0x08` row (about the EVENT ordinals, a different fact) is unchanged, and gains three sibling rows — one per card-kind constant, each citing #17 Appendix A row 0x06 by value. |
| 0.6 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: Appendix C's worked example updated for the amended `OnClubFixturePlayed(clubId, fieldedPlayerIds)` signature, noting that its two example players are already filtered out and so are not in the fielded eleven (the exemption does not fire in this worked case). |
| 0.7 | 2026-08-15 | — | **`ERR-044-012`**, back-prop owed by `DisciplineConstants.cs` v1.3 (M26): `LEAGUE_COMPETITION_KEY` re-tagged `[FIXED]` → `[CROSS]` — it is a verbatim copy of APPROVED #43's `LEAGUE_COMPETITION_ID` (FR-CP-004), consumed read-only, the identical argument already applied to the `CardKindYellow`/`Red`/`SecondYellow` rows. Literal value `0` unchanged; #43 has no `src/` assembly to bind a real mirror to. See `spec-error-log.md` `ERR-044-012`. |
| 0.8 | 2026-08-15 | — | **Reviewed-findings pass.** **`ERR-044-013`** (M5, new id): Appendix A gains `CardLedgerFold.NO_PLAYER` (`[FIXED]`, `-1`, verified as `src/discipline/CardLedgerFold.cs`'s `NO_PLAYER` member) — the occupancy-seed sentinel, caller-facing and used normatively by Appendix C's worked example, but never declared in §2.2 or catalogued here before now (`section-2.md` v0.9 gains the matching §2.2 declaration). **L3:** the `LEAGUE_COMPETITION_KEY` row renamed `LeagueCompetitionKey` — ALL_CAPS beside a `[CROSS]` tag contradicted `src/CLAUDE.md` §3.2.3, the same PascalCase correction L17 already made for the `CardKind*` rows; another agent's `src/discipline/DisciplineConstants.cs` change tracks the same rename. See `spec-error-log.md` `ERR-044-013`. *(L6, August 16, 2026: the `:66` line citation above and in this file's own v0.9-chain header was verified-against-wrong-line — replaced with the member name; a line number is not a stable citation across later edits.)* |
| 0.9 | 2026-08-16 | — | **`ERR-044-014`** (adversarial review, H1): Appendix C's worked example updated for the amended `OnClubFixturePlayed(clubId, clubPlayerIds, fieldedPlayerIds)` signature, and its parenthetical rewritten — both ids are served because they are ON club 7's roster, which §3.3 now reads directly, rather than because `183 / 25 = 191 / 25 = 7`. The arithmetic is left visible as the packing that makes the example's ids coherent (v0.2's own fix), not as the membership rule. |
| 0.10 | 2026-08-16, later | — | **Final fixer pass, three findings.** **`ERR-044-016`** (M6): a kind-0 `CardIssuedEvent` for slot 7 inserted between the substitution (tick 9 000) and the kind-2 card, now at tick 12 000, with a new tick 10 500; the engine precondition `_yellowCards[slot] >= 1` stated explicitly, and the resulting tallies recomputed — player 191 ends at `Yellows = 2, BanMatchesRemaining = 1` rather than `1, 1`, and after `OnClubFixturePlayed` serves both bans to 0, only 183's row is dropped (his reaches `(0, 0)`); 191's survives at `(2, 0)`, carrying his yellows toward the next accumulation ban. **M7**: Appendix A's `YELLOW_ACCUMULATION_THRESHOLD`/`ACCUM_BAN_MATCHES`/`SECOND_YELLOW_BAN_MATCHES`/`STRAIGHT_RED_BAN_MATCHES` rows renamed PascalCase. **L6**: the `CardLedgerFold.cs:66` citation in this file's own v0.8-chain header and version-history row replaced with the member name `NO_PLAYER`. See `spec-error-log.md` `ERR-044-016`, `ERR-044-017`. |
#endregion
