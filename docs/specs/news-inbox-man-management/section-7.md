# News, Inbox & Man-Management #46 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `SourceTag` / `ItemKind` / `InboxIntent` + `PayloadSchema` + the value types + `InboxStore` (`Append` / `Query` / read marks) and their tests. No projectors, no adapter, no man-management. | **Inert** — the feed is empty and every surface is still exercisable (§4.1) |
| **T1** | `InboxSaveCodec` + the round-trip / fail-loud / ordinal-stability suite. Still not composed into the season save. | **Inert** |
| **T2** | **First non-inert phase.** Compose the sub-blob (bumps `SEASON_SAVE_FORMAT_VERSION`); land the **#30 match projector** at its site; add `InboxTextBoundary` and the base-locale catalogue rows. **Man-management stays off.** | **Live**, and **behaviour-neutral at the #33 seam**: items are stored and nothing anyone simulates from changes |
| **T3** | The deep tier: man-management (`TryTalkToPlayer`, the pending deltas, the ERR-030-024 drain generalization), plus the #35 / #44 / #45 / #31 projectors **as their producers land**. | **Named activation** — the first phase in which #46 changes morale |

**T2 is behaviour-neutral in a stronger sense than #35's**, and the reason is worth naming: #35's T2
neutrality is a property of its *authored catalogue* (a non-zero consequence row moves morale), whereas
#46's minimal tier has **no consequence path at all** — man-management is deep-tier. So #46's T2 is neutral
by *construction*, not by data.

**Each projector lands with its producer, never ahead of it** (FR-LW-031). This is why the T-phase table
lists four of them under T3 rather than pinning a date: #46 does not wait on #31/#35/#44/#45, and none of
them waits on #46.

**The predicted T3 failure is the drain.** ERR-030-024 generalizes #30's step-3 seam from one producer to
a loop with a post-sum clamp; wiring #46's `TryTakePendingDelta` without the clamp gives two producers a
combined delta outside the field's contract, and it will not be visible in any #46-local test (§5.5
T-NW-I-002 exists for exactly that).

## 7.2 Deep-tier extensions (designed for, not built)

- **Richer filtering and categorisation** — `InboxFilter` gains predicates. Pure query-side; touches no
  stored state and no schema.
- **The remaining projectors** (#35 press, #44 discipline, #45 board, #31 transfers) — each is root-side
  code plus an `ItemKind` append and a payload schema. **Additive by construction**: a new `SourceTag`
  extends `InboxCursors`' array rather than reordering it (FR-NW-012).
- **Post-match statistics in an item** — possible **only** if the root captures #37's view models at
  emission time, because #37 holds no state and cannot be called after the fact (§1.4(c)). That is a
  *root* extension, not a #46 change, and it widens a payload schema rather than adding a mechanism.
- **A "mark all as unread" or pinning affordance** — pinning would need a bounded pinned-set with the same
  log-bounded discipline as the exception set (FR-NW-018); recorded so it is not implemented as a per-item
  flag, which is the shape KD-6 rejected.
- **Morale-sensitive man-management outcomes** — wanted eventually, and **only** available as a **routed
  committed value** the root supplies *into* the interaction (FR-NW-006). Never a #46-side accessor call,
  which would be the FR-HS-025 two-way coupling.

## 7.3 Explicitly not planned

- **Reading morale.** Not at any tier, through any surface (FR-NW-006). FR-HS-025 bars two-way coupling
  with a consumer, and #46 is the one consumer that also causes a write. §5.8's structural assertion is
  the mechanical defence.
- **Writing morale directly.** #46 causes a #33-owned mutation and never performs one (KD-3). A
  `ApplyManManagementDelta` mutator would give #33 two write sites, where FR-HS-002's whole value comes
  from having one.
- **Owning any producer's logic.** No press question (#35), no suspension rule (#44), no board state
  (#45). Closed structurally by KD-2's reference direction rather than by discipline.
- **A per-item read flag.** It would grow a byte per item forever and make *"mark all read"* an O(n)
  rewrite of the whole blob (KD-6).
- **A query that writes.** Not as a cleanup, not as a cache, not as a "compact while we're here"
  (FR-NW-020). It would collapse the KD-7 argument and oblige #46 to take a tick slot.
- **A stored rendered string, or a stored `InboxIntent`.** The first bakes the save's locale (FR-LC-006);
  the second couples a save to a catalogue ordinal (FR-NW-030).
- **An unbounded archive.** See R-1.

## 7.4 Risks carried

- **R-1 — the retention window will be argued with.** A player who wants a career-long news archive meets
  `INBOX_RETENTION_DAYS`. The knob is `[GT]`, but an unbounded log is a **save-size commitment**, not a
  tuning choice. If archival history is wanted it should be its own compact aggregate — the #22
  `ColdStore` compression pattern — not a raised bound. Standing option, not a debt.
- **R-2 — the payload schema is a convention inside a versioned blob.** `Payload`'s per-`(SourceTag,
  ItemKind)` meaning has no version of its own, so changing what slot 3 of a match item means silently
  re-reads every old item. FR-NW-011 makes it APPEND-only and F2 makes the *arity* checkable — but the
  *meaning* of an existing slot is still only protected by discipline, which is the residual risk. A
  meaning change must be treated as an `INBOX_SAVE_FORMAT_VERSION` bump.
- **R-3 — "the inbox should just read the producers directly"** is the tempting simplification that would
  make #46 reference five specs and re-open the FR-LW-031 bar that currently lets it be authored at all
  (§4.1). KD-2's structural assertion (T-NW-BOUND-001) is what catches it.
- **R-4 — #46's emission ordering is inherited, not owned.** Projectors run inside #30's pinned tick
  order at their producers' steps, so if that order is renumbered, #46's inter-source item ordering on a
  given day changes with it. #46 cites no step number, so nothing in it needs renumbering — but the
  *observable feed order* is downstream of a sequence #46 does not control. Stated so it is not later
  read as a #46 defect.
- **R-5 — ERR-033-003 supersedes a back-prop of a supplement that is itself unapproved.** If #35 lands
  first and unchanged, #46's approval must rename the field before either has an implementation. Cheap
  now, expensive after #35 T2. Sequencing note, not a design risk.
- **R-6 — the shared `ExternalDeltaPermille` field moves a correctness dependency outside #46.** A
  per-producer field would have made over-contribution structurally impossible; the single field makes the
  **root's post-sum clamp** load-bearing (§4.5). The trade was made because producer #3 would otherwise
  need a third field on an approved struct, and T-NW-I-002 is what keeps it honest. #35 records the
  mirror risk from its side.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with T2's neutrality identified as stronger than #35's — neutral by construction rather than by authored data — deep-tier extensions, the not-planned list, risks R-1..R-6 incl. the inherited-ordering property as R-4 and the residual payload-*meaning* risk as R-2, which FR-NW-011's arity check does not cover). Status IN REVIEW. |
#endregion
