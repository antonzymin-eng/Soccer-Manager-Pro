# Discipline & Suspensions #44 — Section 5: Test Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.3
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

## 5.3 Thresholds, bans & serving (KD-3)

- **T-DC-BAN-001** — the §3.2 worked example: 4 yellows + kind-0 ⇒ `Yellows 0`, ban 1; 4 yellows
  + kind-2 ⇒ `Yellows 0`, ban 2 (stacking); kind-1 ⇒ ban +2, yellows untouched (FR-DC-007).
- **T-DC-BAN-002 (off-by-one)** — a card in fixture N ⇒ the player is filtered from fixture
  N+1's selection ⇒ available again for N+2 after a 1-match ban (the §3.3 ordering lock,
  FR-DC-010/011).
- **T-DC-BAN-003 (serving path-independence)** — a ban decrements on the club's quick-sim
  fixtures exactly as on engine-resolved ones (FR-DC-011).
- **T-DC-BAN-004 (F5)** — a filter reducing the squad below the 18 `ConfigureSquads` consumes
  fails loud.
- **T-DC-BAN-005 (both squads)** — an **opponent** player banned by accumulation is filtered from
  the engine-resolved fixture against the managed club (both clubs' resolved squads pass the
  resolve→configure seam — FR-DC-010); a managed-squad-only filter fails this test.

## 5.4 The view (KD-4)

- **T-DC-VIEW-001** — exercising every #44 path leaves the #27 canonical squads byte-identical
  (`FilterAvailable` returns a reduced value copy; FR-DC-001/009 — the #32 T-SC-VIEW-001 class).
- **T-DC-VIEW-002** — `IsAvailable` is a pure predicate; an absent entry ⇒ available; a
  pass-through filter returns an equal (but distinct-copy) squad.

## 5.5 Save, boundary & hygiene (KD-1/KD-6/KD-8)

- **T-DC-SAV-001** — the sub-blob round-trips field-identical (populated tallies + active bans;
  empty at genesis); fail-loud on version/length/trailing/non-ascending-keys/negative values
  (F3); **no RNG-state field** (schema-shape, FR-DC-016).
- **T-DC-SAV-002 (boundary + canonical minimality)** — `RollToNextSeason`: yellows reset, an
  unserved ban carries and still serves; a `(0,0)` entry is dropped **immediately** wherever it
  arises (mid-season serve-out and boundary alike), so two equivalent runs serialize identical
  bytes (FR-DC-017).
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

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §5 (observer-neutrality/identity, fold, thresholds/serving, view, save/boundary/hygiene, traceability), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M follow-through): T-DC-SAV-002 extended to lock the immediate `(0,0)` drop + identical-bytes property. |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M follow-through): new **T-DC-BAN-005** locks the both-squads filter coverage (a banned opponent excluded from the engine-resolved fixture — the case the managed-club-only tests never exercised). |
#endregion
