# Discipline & Suspensions #44 — Outline

**Created:** July 24, 2026
**Last Updated:** August 15, 2026, yet later still again and again (v0.6 — reviewed-findings pass,
continuing `ERR-044-008`: KD-2 still said "one tap feeds #37+#44" — the refuted claim `ERR-044-008`
corrected at `section-4.md` §4.3/§4.5, `section-7.md` §7.3 and `section-8.md` §8.1, missed here
because §1's v0.4 "full-file sweep" note covered the section files it had just edited and never
reached this file at all. Restated to name #44's own `IDisciplineTickLedgerTap` and the reference
rule that makes a shared type unreachable)
**Last Updated (prior):** August 15, 2026, yet later still again (v0.5 — the "also recorded, not filed" item
from #44's adversarial-review round 4 (`open-issues.md`): a new "Numbering note" records that the
round-3 fix commits' `src/` file headers say "AR round 5" while the git commit log calls the same
work round 3, and that the section files' own "third/fourth/fifth adversarial-review pass" phrasing
is a third, independently-drifted counter of the identical cycles. Not a defect to fix — recorded so
a reader does not mistake the divergence for one)
**Last Updated (prior):** August 15, 2026, yet later still (v0.4 — L21, second correction in the same pass: the
"Back-props" section's "At T-phase (deferred):" bullet still listed the #30 outer
`SEASON_SAVE_FORMAT_VERSION` bump (T1) as deferred — it landed `5 → 6` at ERR-030-035 two days earlier
(August 13, 2026), verified against `section-4.md` §4.4. Split the bullet: the T1 bump marked LANDED
with its citation; the T2 hygiene-hook wiring and T3 items remain genuinely deferred, matching §7.2's
own current record)
**Last Updated (prior):** August 15, 2026, later still (v0.3 — L21, the spec half of #44's adversarial-review
round 4 (`open-issues.md`): the section map's §2 row said "failure modes (F1..F5)" — stale since **F6**
was added at §2.3 v0.6 (August 13, 2026); `section-2.md`'s failure-mode table carries six rows,
`grep -c`-verified, so corrected to F1..F6)
**Last Updated (prior):** August 15, 2026 (v0.2 — ERR-044-003 stage 1, owner decision: **KD-3** corrected — a
ban serves one decrement per played fixture of the player's club **that he did not appear in**, not
per played fixture full stop; the extremis back-fill (#30 §2.3 F9) can field a suspended player, and
without the exemption that appearance served his ban for free)
**Last Updated (prior):** July 24, 2026 (v0.1 — initial, promoted from design supplement v0.3)
**Version:** 0.6
**Status:** APPROVED

---

## Purpose

**Season-level discipline as a read-only derivation over already-emitted card events**: accumulate
the yellows/reds the match engine already publishes (`CardIssuedEvent`, single-event kind-2
second-yellow promotion — verified against source), apply literal threshold rules, and expose a
**per-player suspension-availability VIEW** the season loop consults at squad selection. #44 reads,
never re-implements; a suspension never mutates a `PlayerRecord` or a #27 `Squad`. **No RNG stream,
no domain tag, no `SubsystemOrdinals` entry** (the #37/#38/#49 read-only class). Live at minimal
(the #41 class): a ban legitimately changes the next lineup — designed, deterministic behaviour.

## Section map

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, out-of-scope seams, dependencies, key decisions (KD-1..KD-8) |
| 2 | Functional requirements (FR-DC-001..022), data structures, failure modes (F1..F6) |
| 3 | Core algorithms: the occupancy fold, thresholds/bans, serving, the availability filter |
| 4 | Architecture, assembly/file layout, the tap read, save composition |
| 5 | Test plan (observer-neutrality + fold + lifecycle + view + save) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and T-phase plan (T0–T3) |
| 8 | References and cross-spec cross-references (XC-044-*) |
| 9 | Approval checklist |
| Appendices | Constant catalogue, save-block layout, worked fold example |

## Governing decisions (see §1)

- **KD-1** — the tally is **persisted** (`DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob) — forced by
  verification: `SerializeLedger` is write-only and no per-fixture ledgers are retained, so
  recompute-on-load has no input.
- **KD-2** — the read is the **#37-class read-only per-tick ledger tap** (FR-AN-002 — the approved
  observational pattern reused, not a type shared with #37: §4.1's reference rule makes #37's
  identically-shaped interface unreachable from either #44 or the composition root, so #44 declares
  its own `IDisciplineTickLedgerTap` over the engine's one-per-tick fill rather than "one tap feeds
  #37+#44" — `ERR-044-008`) + a slot→player **occupancy fold** (initial lineup
  + `SubstitutionEvent`s) — never ledger bytes, never post-match slot state (the v1.33
  slot-reset), never a new subscription pattern.
- **KD-3** — fold at fixture resolution; availability filter at the next selection (the
  ERR-030-009 resolve→configure seam); a ban serves one decrement per played fixture of the
  player's club **that the player did not appear in** (either resolution path — amended
  ERR-044-003 stage 1: the exemption matters only when the extremis back-fill has fielded a
  suspended player, since the filter otherwise excludes him already). No off-by-one.
- **KD-4** — availability is a **VIEW**: `IsAvailable` is a pure predicate; `FilterAvailable`
  returns a reduced value-copy `Squad`; #27 state is never written.
- **KD-5** — de-dup resolved by source: **one event per incident**; kind 2 = yellow +1 AND a
  dismissal in a single event; kind 1 adds no yellow.
- **KD-6** — the tally keys `(PlayerId, CompetitionId)` (`0` at minimal — #43-partitionable);
  hygiene: a transfer **migrates** tally + unserved bans old→new id (bans follow the player — the
  deliberate contrast with #32's drop rule); retirement drops.
- **KD-7** — live-at-minimal staging (the #41 class); neutrality = observer-neutrality +
  no-trigger identity + determinism.
- **KD-8** — season boundary: tallies reset, **unserved bans carry**.

## Back-props

- **At approval:** one — **ERR-030-009**: #30 FR-SN-013's managed-fixture flow gains the
  pre-declared availability-filter null seam (resolve → *filter* → configure). **No #16 change**
  (read-only — no tag/ordinal/stream, a positive property).
- **At T-phase:** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1) — **LANDED** `5 → 6` at
  ERR-030-035, August 13, 2026, §4.4. Still deferred: the FR-TX-022 re-key migration hook wiring
  (T2 — built and unit-tested but zero production callers, §7.2); the #30-owned quick-sim card
  synthesis + #43 partitions (T3).

## Numbering note (adversarial-review passes vs. fix commits)

This folder's section files count the post-C1/C2-landing adversarial-review cycles as ordinal
**passes** — "a third/fourth/fifth adversarial-review pass over the #44 C1/C2 landing" (e.g.
`section-4.md`'s L12(b), `appendices.md`'s L17). The corresponding `src/discipline/` and
`src/season-save/` file headers' `Modified:` comments label the identical work "**AR round 5**"
fixes. Git's own commit log groups the same fixes under a **third** round-3 commit. All three
numbers are correct under their own definition and none is an error to reconcile: the section files
and the file-header comments both count review PASSES (one per review cycle) but the two counters
were kept independently and drifted apart; the commit log counts FIX COMMITS, which bundle one or
more review passes' fixes into however many commits the fixer chose to land them in. A reader
cross-referencing `spec-error-log.md`'s pass-numbered entries against `src/` file headers or
`git log` should expect these counts to diverge and should not read a mismatch as a data-integrity
defect. Recorded here per the round-4 adversarial-review finding (`open-issues.md`) rather than
"corrected" — there is no single correct number to converge on, only three schemes that count
different things.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial outline, promoted from design supplement v0.3 (AR-converged). Status IN REVIEW. |
| 0.2 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: KD-3 corrected to state the played-fixture ban decrement excludes any fixture the player appeared in (the extremis back-fill case), matching the amended FR-DC-011 / `OnClubFixturePlayed`. |
| 0.3 | 2026-08-15 | — | **L21** (#44 adversarial-review round 4, `open-issues.md`): the section-map's §2 row corrected "failure modes (F1..F5)" to **F1..F6** — `section-2.md`'s failure-mode table has carried six rows (F1–F6) since F6 was added at that section's v0.6 (August 13, 2026); this row was never updated to match, verified by `grep -c "^| \*\*F[0-9]\*\* |" section-2.md` returning 6. |
| 0.4 | 2026-08-15 | — | **L21**, second finding: the "Back-props" section's "At T-phase (deferred)" bullet still listed the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1) as deferred two days after it landed `5 → 6` at ERR-030-035 (August 13, 2026) — verified against `section-4.md` §4.4, which has carried the landed figure since its own v0.3. Split into a landed T1 clause and a still-deferred T2/T3 clause, matching §7.2's current record of what has and has not shipped. |
| 0.5 | 2026-08-15 | — | New "Numbering note" section, the "also recorded, not filed" item from round 4's own report: the round-3 fix commits' `src/` file headers say "AR round 5" while the commit log calls the same work round 3, and the section files' "third/fourth/fifth adversarial-review pass" phrasing is a third counter again. All three are internally correct; recorded rather than reconciled, since there is no single number to converge them on. |
| 0.6 | 2026-08-15 | — | **Reviewed-findings pass, continuing `ERR-044-008`.** KD-2's "one tap feeds #37+#44" was the same refuted claim fixed at `section-4.md` §4.3/§4.5, `section-7.md` §7.3 and `section-8.md` §8.1 — missed here because those fixes' own "full-file sweep" claim (`section-1.md` v0.4) covered only the section files that pass had just edited, never this file. Restated: #44 declares its own `IDisciplineTickLedgerTap` because §4.1's reference rule makes #37's identically-shaped interface unreachable from either #44 or the composition root, so no shared type exists even with both assemblies built. No new ERR id — `ERR-044-008`'s own back-prop reaching a site its founding fix missed. See `spec-error-log.md` `ERR-044-008`. |
#endregion
