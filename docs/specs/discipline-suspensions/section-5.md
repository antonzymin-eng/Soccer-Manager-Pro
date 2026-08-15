# Discipline & Suspensions #44 — Section 5: Test Plan

**Created:** July 24, 2026
**Last Updated:** August 15, 2026 (v0.4 — M25, the spec half of #44's adversarial-review round 4
(`open-issues.md`): this was the one section file no back-prop pass had touched since v0.3 — it still
mandated **T-DC-BAN-004 (F5)**, the fail-loud `ERR-044-003` withdrew at the C1/C2 landing (August 13),
while §9's G14 ✅ certified total FR→test traceability against a table that no longer matched §2.3's
current failure-mode set. T-DC-BAN-004 marked WITHDRAWN in place (git tracks history; not deleted, per
this project's own convention for a retired requirement) with a pointer to §2.3's F5 note. Four rows
added for tests that exist in `src/discipline/tests/` and were enforced in production but had no §5
row: **T-DC-FOLD-004** (F6, the bound-`[GT]` commit guard), **T-DC-BAN-006** (ERR-044-003 stage 1's
fielded-eleven exemption, landed August 15), **T-DC-VIEW-003** (FR-DC-009's all-suspended `null`
return, ERR-044-005), **T-DC-SAV-003** (F2's negative-`PlayerId` refusal at both boundary sites).
§5.6 updated to reflect the corrected table; §9's G14 re-checked against it and left unchanged — see
that section's own version history for the verification note.)
**Last Updated (prior):** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## 5.1 Observer-neutrality & identity (KD-7) — the headline

- **T-DC-NEU-001** — an engine-resolved fixture with the fold tapped is **digest-identical** to
  the same fixture unobserved (the `match-viewer` lock; FR-DC-003).
- **T-DC-NEU-002** — a season with no threshold-crossing cards is byte-identical to pre-#44
  except #44's own sub-blob (the filter passes every squad through unchanged; FR-DC-018).

## 5.2 The fold (KD-2/KD-5)

- **T-DC-FOLD-001 (de-dup)** — a scripted kind-{0, 0, 2, 1} sequence yields exactly: 3 yellows
  for the kind-0/0/2 recipient-players and dismissal bans for the kind-2 and kind-1 recipients —
  a kind-2 counts **one** yellow + **one** dismissal, never a yellow-then-red pair (FR-DC-006).
- **T-DC-FOLD-002 (occupancy)** — a card before a substitution attributes to the outgoing
  player; a card after, to the incoming player (occupancy at the card's tick, FR-DC-005); the
  engine's v1.33 slot-reset never leaks into the tally (a subbed-off player's cards persist).
- **T-DC-FOLD-003 (F1/F4)** — a card/sub for an unmapped agent id fails loud; a `CardKind`
  outside `{0,1,2}` fails loud; an unknown Tier A ordinal is **ignored** (FR-DC-004 — the
  contrast is deliberate and both directions are locked).
- **T-DC-FOLD-004 (F6, atomic commit)** — `Commit` validates all four bound `[GT]`s
  (`YellowAccumulationThreshold`, `AccumBanMatches`, `SecondYellowBanMatches`,
  `StraightRedBanMatches`) **before** applying the first buffered card: an invalid yellow
  threshold or a negative ban length refuses the whole call, leaves `DisciplineState` untouched,
  and leaves the fold itself uncommitted so a corrected retry still succeeds (`CardLedgerFoldTests.
  Commit_WithAnInvalidYellowThreshold_RefusesBeforeApplyingAnyCard_AndLeavesTheFoldUncommitted`,
  `Commit_WithAnInvalidBanLength_RefusesBeforeApplyingAnyCard` — §3.1's M13 atomicity property).
  `DisciplineRules.AddYellow`/`ApplyCard` carry the identical guard directly, for a caller that
  never goes through a fold (`RequireYellowThreshold_BelowOne_Throws`,
  `RequireBanLength_Negative_Throws`).

## 5.3 Thresholds, bans & serving (KD-3)

- **T-DC-BAN-001** — the §3.2 worked example: 4 yellows + kind-0 ⇒ `Yellows 0`, ban 1; 4 yellows
  + kind-2 ⇒ `Yellows 0`, ban 2 (stacking); kind-1 ⇒ ban +2, yellows untouched (FR-DC-007).
- **T-DC-BAN-002 (off-by-one)** — a card in fixture N ⇒ the player is filtered from fixture
  N+1's selection ⇒ available again for N+2 after a 1-match ban (the §3.3 ordering lock,
  FR-DC-010/011).
- **T-DC-BAN-003 (serving path-independence)** — a ban decrements on the club's quick-sim
  fixtures exactly as on engine-resolved ones (FR-DC-011).
- **T-DC-BAN-004 (F5) — WITHDRAWN, ERR-044-003 (August 13, 2026).** *Superseded text, kept for
  history rather than deleted (this project's standing convention — git tracks history, IDs are not
  reused): "a filter reducing the squad below the 18 `ConfigureSquads` consumes fails loud."* #44's
  original F5 fail-loud was withdrawn at the C1/C2 landing — #30 §2.3 F9 (approved after this spec)
  settles the identical depleted-squad event by back-filling instead, and #44 implements **no
  viability gate at all** (`src/discipline/Availability.cs`); see §2.3's F5 row and the
  "ERR-044-003 (F5 vs #30 §2.3 F9)" note below it for the full account. No test exists or should
  exist for a fail-loud #44 no longer performs.
- **T-DC-BAN-005 (both squads)** — an **opponent** player banned by accumulation is filtered from
  the engine-resolved fixture against the managed club (both clubs' resolved squads pass the
  resolve→configure seam — FR-DC-010); a managed-squad-only filter fails this test.
- **T-DC-BAN-006 (ERR-044-003 stage 1, the fielded-eleven exemption)** — a banned player fielded
  through #30 §2.3 F9's extremis back-fill does **not** have his ban served by that fixture, while
  an unfielded banned team-mate in the same call still serves one match, asserted in the same call
  so the test cannot pass by the method exempting everybody unconditionally
  (`DisciplineRulesTests.OnClubFixturePlayed_ExemptsAFieldedBannedPlayer_ButStillServesAnUnfieldedTeammate`,
  FR-DC-011 as amended). A fielded player who carries no ban gains no row just for having played
  (`OnClubFixturePlayed_AFieldedPlayerWithNoBan_IsUnaffected_NoRowInvented`); a null
  `fieldedPlayerIds` fails loud, since the exemption is required, not optional
  (`OnClubFixturePlayed_NullFieldedPlayerIds_Throws`); the exemption does not suppress FR-DC-017's
  immediate `(0,0)` drop for a *different* player served in the same call
  (`OnClubFixturePlayed_ExemptionDoesNotBlockTheFRDC017DropForANonFieldedPlayer`).

## 5.4 The view (KD-4)

- **T-DC-VIEW-001** — exercising every #44 path leaves the #27 canonical squads byte-identical
  (`FilterAvailable` returns a reduced value copy; FR-DC-001/009 — the #32 T-SC-VIEW-001 class).
- **T-DC-VIEW-002** — `IsAvailable` is a pure predicate; an absent entry ⇒ available; a
  pass-through filter returns an equal (but distinct-copy) squad.
- **T-DC-VIEW-003 (FR-DC-009, the all-suspended case, ERR-044-005)** — `FilterAvailable` returns
  `null`, not a zero-player `Squad`, when every player in the squad is suspended (`Squad`'s own
  constructor refuses `players.Length == 0`, so there is no reduced value copy to return) —
  `AvailabilityTests.FilterAvailable_EveryPlayerSuspended_ReturnsNull`. A null `squad`/`state`
  argument fails loud rather than being read as "nothing suspended"
  (`FilterAvailable_NullArguments_Throw`). `FilterAvailable` is FR-DC-009's own surface, not #44's
  production path — #30's composed seam consumes `MarkSuspended`'s removal mask directly and never
  calls this method.

## 5.5 Save, boundary & hygiene (KD-1/KD-6/KD-8)

- **T-DC-SAV-001** — the sub-blob round-trips field-identical (populated tallies + active bans;
  empty at genesis); fail-loud on version/length/trailing/non-ascending-keys/negative values
  (F3); **no RNG-state field** (schema-shape, FR-DC-016).
- **T-DC-SAV-002 (boundary + canonical minimality)** — `RollToNextSeason`: yellows reset, an
  unserved ban carries and still serves; a `(0,0)` entry is dropped **immediately** wherever it
  arises (mid-season serve-out and boundary alike), so two equivalent runs serialize identical
  bytes (FR-DC-017).
- **T-DC-SAV-003 (F2, negative-`PlayerId` refusal)** — a negative `PlayerId` is refused at both
  boundary sites named in §2.3's F2 row: the file boundary
  (`DisciplineSaveCodecTests.Decode_NegativePlayerId_Throws`, an explicit F3 gate ahead of the
  constructor so the exception and message come from the same layer as every other F3 refusal in
  the file) and `DisciplineEntry`'s own constructor, which the codec test exercises transitively
  and which is also reachable directly through `DisciplineRules.MigratePlayerId`'s negative-target
  refusal (`DisciplineRulesTests.MigratePlayerId_NegativeTargetId_ThrowsAndWritesNothing` — the
  migration conflict-refusal is atomic, nothing written on refusal).
- **T-DC-HYG-001** — a re-key migrates tally + unserved bans old→new `PlayerId` verbatim (a
  banned player stays banned through a transfer); retirement drops the entry; a conflicting
  migration target fails loud (FR-DC-013/F2).
- **T-DC-DET-001** — two-run determinism: the same fixture events produce byte-identical
  `DisciplineState` and identical filtered squads (FR-DC-021).
- **T-DC-INT-001** — every field integer; no float (static/reflection assertion, FR-DC-020); #44
  registers no RNG stream (FR-DC-019 — the #40 cursor-untouched class).

## 5.6 Requirement traceability

Every FR-DC-001..022 maps to a T-DC-* test above **or** a recorded §7 deferral (quick-sim
synthesis, #43 partitions, the T-phase hygiene wiring — each locked at its minimal boundary now).
This includes the two failure-mode rows §2.3 carries beyond F1..F4 — **F6** (T-DC-FOLD-004) and the
**withdrawn** F5 (T-DC-BAN-004, kept as a withdrawal record rather than a live requirement) — and
FR-DC-009's `null`-return case (T-DC-VIEW-003) and FR-DC-011's fielded-eleven exemption
(T-DC-BAN-006), both added at ERR-044-005/ERR-044-003 respectively but previously untested at the
§5 level despite being enforced in production.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §5 (observer-neutrality/identity, fold, thresholds/serving, view, save/boundary/hygiene, traceability), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M follow-through): T-DC-SAV-002 extended to lock the immediate `(0,0)` drop + identical-bytes property. |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M follow-through): new **T-DC-BAN-005** locks the both-squads filter coverage (a banned opponent excluded from the engine-resolved fixture — the case the managed-club-only tests never exercised). |
| 0.4 | 2026-08-15 | — | **M25** (#44 adversarial-review round 4, `open-issues.md`): this section had not been touched since v0.3 (July 24) despite five back-prop passes over the C1/C2 landing touching every other #44 section file. **T-DC-BAN-004** (F5's fail-loud) marked WITHDRAWN in place, pointing at §2.3's F5/ERR-044-003 note, rather than deleted or silently left mandating a test #44 no longer performs — §9's G14 ✅ certified total FR→test traceability against a table this left stale. New **T-DC-FOLD-004** (F6, the bound-`[GT]` atomic-commit guard, §5.2), **T-DC-BAN-006** (ERR-044-003 stage 1's fielded-eleven exemption, §5.3), **T-DC-VIEW-003** (FR-DC-009's all-suspended `null` return, ERR-044-005, §5.4), **T-DC-SAV-003** (F2's negative-`PlayerId` refusal at both boundary sites, §5.5) — all four cite tests already present and green in `src/discipline/tests/`, verified by reading each named test method directly rather than by inference from the surrounding code. §9 checked against the corrected table (see that section's own version history) and its G14 ✅ left standing — the claim is true of this table, it was the table that was stale. |
#endregion
