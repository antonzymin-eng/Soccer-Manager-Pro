# Discipline & Suspensions #44 — Section 6: Performance

**Created:** July 24, 2026
**Last Updated:** August 15, 2026, later still (v0.4 — reviewed-findings pass, extending `ERR-044-011`:
§6.2's `FilterAvailable` bullet priced a method with **zero production call sites** at a scope
(§2.3/§4.5's "once per managed-fixture selection") ERR-044-002 had already withdrawn — FR-DC-009
itself says `FilterAvailable` is "FR-DC-009's own surface, not #44's production path", and the
production surface, `MarkSuspended`, called at `src/season-save/AvailabilityComposition.cs:162`, had
no §6.2 bullet at all. New `MarkSuspended` bullet at the real O(squad size)/twice-per-fixture-per-path
cost; `FilterAvailable` kept, labelled non-production)
**Last Updated (prior):** August 15, 2026, later (v0.3 — `ERR-044-011`, reviewed-findings pass: §6.2 named a
member, `OnTapRecord`, that does not exist — the real member is `ObserveTick` (`CardLedgerFold.cs`),
`OnTapRecord` being the pre-M27 pseudocode name — and misstated `OnClubFixturePlayed`'s complexity as
"O(active-ban entries for the club), typically 0–2" when `DisciplineRules.cs:265` walks the ENTIRE
state once per call, filtering inline; both restated against the real code, including the 11-element
fielded-eleven scan the ERR-044-003 stage 1 exemption adds per active ban)
**Last Updated (prior):** August 15, 2026 (v0.2 — L21, the spec half of #44's adversarial-review round 4
(`open-issues.md`): §6.3 claimed "three ints per carded player"; `DisciplineEntry` (verified against
`src/discipline/DisciplineEntry.cs`) and Appendix B's byte layout both carry **four** —
`PlayerId`, `CompetitionId`, `Yellows`, `BanMatchesRemaining` — a count stale since this section was
first authored, since `CompetitionId` was part of the entry from v0.1 of §2 onward)
**Last Updated (prior):** July 24, 2026 (v0.1 — initial)
**Version:** 0.4
**Status:** APPROVED

---

## 6.1 Cadence

#44 adds **no engine-side work**: the fold is fed by the same per-tick tap read the root already
performs for #37's class of consumer, and its per-record work is a switch + at most two integer
updates. Off-match, #44 runs per-event only (a filter at selection, a decrement per played
fixture, a boundary sweep per season).

## 6.2 Per-operation cost

- **`ObserveTick`** — O(1) per buffered record: an ordinal switch, an occupancy lookup, and a
  buffered append (`pending.Add`) for a card or an occupancy-array write for a substitution —
  nothing is applied to `DisciplineState` until `Commit` (§3.1). Card and substitution events are
  rare (a handful per match), so the per-tick cost is effectively the ignore branch.
- **`MarkSuspended`** — O(squad size): one pass writing the suspension mask. This is #44's actual
  production path, called by `AvailabilityComposition.Compose`
  (`src/season-save/AvailabilityComposition.cs:162`) once per resolved squad — **twice per fixture,
  both clubs, on both resolution paths** (FR-DC-010), not once per managed-fixture selection.
- **`FilterAvailable`** — O(squad size) value copy. FR-DC-009's own surface, exercised directly by
  this assembly's tests; **not #44's production path** — #30's composed seam consumes
  `MarkSuspended`'s mask directly and never calls this method (§2.2).
- **`OnClubFixturePlayed`** — **O(total discipline entries across every club, not just the calling
  one)**: the walk visits every row of `DisciplineState` once, descending, to find the ones both
  owned by `clubId` and carrying `BanMatchesRemaining > 0` (`src/discipline/DisciplineRules.cs`,
  the `for (int i = _state.Count - 1; ...)` loop). For each such active ban (typically 0–2) it also
  runs an O(|fieldedPlayerIds|) ≈ O(11) linear scan of the fielded eleven to check the
  ERR-044-003 stage 1 exemption (`WasFielded`).
- **Boundary sweep** — O(entries), once per season.

## 6.3 Save cost

O(entries) integer serialization, once per save/load — the smallest sub-blob in the family
(four ints per carded player: `PlayerId`, `CompetitionId`, `Yellows`, `BanMatchesRemaining` —
Appendix B). Empty at genesis.

## 6.4 Budget

Negligible on every axis; no RNG stream, no allocation in the fold's steady state beyond the
filter's value copy. Not a per-tick budget concern.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §6 (cadence, per-operation cost, save cost, budget), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-08-15 | — | **L21** (#44 adversarial-review round 4, `open-issues.md`): §6.3's "three ints per carded player" corrected to **four** (`PlayerId`, `CompetitionId`, `Yellows`, `BanMatchesRemaining`), verified by direct count against `src/discipline/DisciplineEntry.cs`'s four `public readonly int` fields and Appendix B's four `per entry:` rows — both `grep`-counted rather than inferred. |
| 0.3 | 2026-08-15 | — | **`ERR-044-011`**, reviewed-findings pass: §6.2's first bullet renamed `OnTapRecord` → `ObserveTick` (verified against `src/discipline/CardLedgerFold.cs`, which has never had a member named `OnTapRecord` — the pseudocode name §3.1 used before M27's rewrite) and restated to describe buffering rather than direct application; `OnClubFixturePlayed`'s complexity corrected from "O(active-ban entries for the club), typically 0–2" to O(total entries across every club) — `DisciplineRules.cs`'s descending `for` loop walks the whole state once per call — with the O(11) `WasFielded` scan per active ban named explicitly. See `spec-error-log.md` `ERR-044-011`. |
| 0.4 | 2026-08-15 | — | **Reviewed-findings pass, extending `ERR-044-011`.** §6.2's `FilterAvailable` bullet priced a method with zero production call sites (`grep -rn "FilterAvailable" src/` outside `src/discipline/` returns nothing) at a scope — "once per managed-fixture selection" — ERR-044-002 (August 13, 2026) had already withdrawn: FR-DC-010 has covered both clubs of every fixture on both resolution paths since that fix. The real production surface, `Availability.MarkSuspended`, called at `src/season-save/AvailabilityComposition.cs:162` once per resolved squad (twice per fixture, both resolution paths — `SeasonLoop.SelectAvailable`), had no bullet at all. New `MarkSuspended` bullet added at the correct cost and call count; `FilterAvailable`'s bullet kept but relabelled non-production, matching §2.2's `Availability` block. No new ERR id — extends `ERR-044-011`. See `spec-error-log.md` `ERR-044-011`. |
#endregion
