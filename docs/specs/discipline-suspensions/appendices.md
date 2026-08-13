# Discipline & Suspensions #44 — Appendices

**Created:** July 24, 2026
**Last Updated:** August 13, 2026, later still (v0.5 — L17, a fifth adversarial-review pass over the
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
**Version:** 0.5
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `DISCIPLINE_SAVE_MAGIC` | `[FIXED]` | `"DISC"` (`0x44495343`) | the sub-blob's self-identifying leading tag, checked BEFORE the version (ERR-044-001 — a format version is not a format identifier). |
| `DISCIPLINE_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the discipline sub-blob's own version gate (KD-1); gated second, behind the magic. |
| `YELLOW_ACCUMULATION_THRESHOLD` | `[GT]` | 5 (illustrative) | yellows per accumulation ban (§3.2); balance-pass-pinned against real-competition rules. |
| `ACCUM_BAN_MATCHES` | `[GT]` | 1 (illustrative) | ban length for an accumulation threshold crossing. |
| `SECOND_YELLOW_BAN_MATCHES` | `[GT]` | 1 (illustrative) | ban length for a kind-2 dismissal. |
| `STRAIGHT_RED_BAN_MATCHES` | `[GT]` | 2 (illustrative) | ban length for a kind-1 dismissal. |
| `LEAGUE_COMPETITION_KEY` | `[FIXED]` | 0 | the minimal-tier `CompetitionId` partition key (FR-DC-012; aligns with #43's `LEAGUE_COMPETITION_ID = 0`). |
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

Tap sequence: tick 4 000 `CardIssuedEvent{Recipient: 7, Kind: 0}` → occupant of slot 7 is 183 →
`Yellows` 4 → 5 ⇒ ban 1, `Yellows` 0 (threshold 5). Tick 9 000
`SubstitutionEvent{Outgoing: 7, Incoming: 22, Team: 0}` → `ApplySubstitution` reads the occupant of
the *synthetic* id 22 (191) and moves it onto slot 7: occupancy[7] := 191 (slot identity is
unchanged; only who occupies it moves). Tick 12 000 `CardIssuedEvent{Recipient: 7, Kind: 2}` →
occupant of slot 7 is now **191** → `Yellows` +1 **and** ban +1 (one event, one yellow, one
dismissal — KD-5). Fixture N+1 selection: `FilterAvailable` excludes 183 and 191; after N+1 is
played, `OnClubFixturePlayed(7)` decrements both to 0 (`183 / 25 = 191 / 25 = 7` — the §3.3
club-derivation rule) — available for N+2. The engine's slot-7 yellow count was reset by the
substitution (v1.33) and **never read** — the tally kept 183's card via occupancy, not the slot.
All integer; two runs identical; #27 squads byte-untouched.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial appendices (constant catalogue, sub-blob layout, end-to-end worked fold example), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR pass 3 (M): Appendix C's bench player re-keyed 201 → **191** — 201 derives to club 8 (`201 / 25 = 8`), an impossible teammate of club-7's 183, and `OnClubFixturePlayed(7)` would never have decremented it; the example now derives coherently (`183 / 25 = 191 / 25 = 7`). |
| 0.3 | 2026-08-13 | — | **ERR-044-001** (C1/C2 landing, §7.1 T2's verification obligation): Appendix B's layout gains the `DISCIPLINE_SAVE_MAGIC` row before the version and the magic-before-version MUST (the ERR-029-005/ERR-041-009 class's fourth instance); Appendix A gains the matching catalogue row; Appendix C's worked example — verified against the live engine and found unimplementable, "slot 19" being an on-pitch index under `SQUAD_SIZE = 22` — re-derived onto the real synthetic bench-id formula (`SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM + benchIndex`), with the "(or 19 → 191, absorbed either way)" hedge deleted. |
| 0.4 | 2026-08-13 | — | **L12(a)**, a third adversarial-review pass over the C1/C2 landing: Appendix A's "the 18-slot `ConfigureSquads` minimum \| `[CROSS]` \| match engine \| the F5 filter floor" row deleted — F5 was withdrawn at ERR-044-003 (§2.3 F5, `section-2.md` v0.4) and no such constant exists in `DisciplineConstants.cs`; #44 implements no viability gate of any kind since that withdrawal, so nothing in the catalogue names one. |
| 0.5 | 2026-08-13 | — | **L17**, a fifth adversarial-review pass over the C1/C2 landing: `CARD_KIND_YELLOW`/`RED`/`SECOND_YELLOW` are #17 `CardIssuedEvent.CardKind` domain ordinals (Appendix A row 0x06) #44 consumes read-only and never sets independently — the root `CLAUDE.md` tag table makes that `[CROSS]`, not `[FIXED]`, and `src/CLAUDE.md` §3.2.3 makes `[CROSS]` PascalCase, not `ALL_CAPS`. Retagged and renamed in code (`CardKindYellow`/`CardKindRed`/`CardKindSecondYellow`); Appendix A's single combined `CardIssuedEvent 0x06 / SubstitutionEvent 0x08` row (about the EVENT ordinals, a different fact) is unchanged, and gains three sibling rows — one per card-kind constant, each citing #17 Appendix A row 0x06 by value. |
#endregion
