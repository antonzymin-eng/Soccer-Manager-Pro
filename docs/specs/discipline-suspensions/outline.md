# Discipline & Suspensions #44 — Outline

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial, promoted from design supplement v0.3)
**Version:** 0.1
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
| 2 | Functional requirements (FR-DC-001..022), data structures, failure modes (F1..F5) |
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
  observational pattern; one tap feeds #37+#44) + a slot→player **occupancy fold** (initial lineup
  + `SubstitutionEvent`s) — never ledger bytes, never post-match slot state (the v1.33
  slot-reset), never a new subscription pattern.
- **KD-3** — fold at fixture resolution; availability filter at the next selection (the
  ERR-030-009 resolve→configure seam); a ban serves one decrement per played fixture of the
  player's club (either resolution path). No off-by-one.
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
- **At T-phase (deferred):** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the FR-TX-022
  re-key migration hook wiring (T2); the #30-owned quick-sim card synthesis + #43 partitions (T3).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial outline, promoted from design supplement v0.3 (AR-converged). Status IN REVIEW. |
#endregion
