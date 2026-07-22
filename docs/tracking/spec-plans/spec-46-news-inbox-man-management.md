# Spec #46 — News, Inbox & Man-Management — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#46** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.5 / §5 · **Tier:** S2 min → S4 deep · **Wave:** 6 · **FR prefix (proposed):** FR-NW
> **Determinism:** read-only aggregator / writes #33 morale — no RNG stream, no domain tag
> **Purpose:** The manager's comms hub — an inbox aggregating season/transfer/board/media events plus talk-to-player man-management that writes to #33 morale.

## 1. Scope
Two halves of one manager-facing hub: (a) a **read-only inbox** aggregating events emitted by the season loop, transfers, board, and media into a browsable feed, and (b) **man-management** talk-to-player interactions whose outcomes write to #33 morale. Both surface text through #22's `InteractionTextGenerator`. The inbox aggregates; it never produces canonical events of its own.
Out of scope: the events themselves (owned by #30/#31/#45/#35), the morale model (#33 — written to, not owned), the text generator (#22), the rendering of the inbox (UI #38).

## 2. Staging (minimal-first → deep)
Minimal (Stage 2) = a read-only event feed with no man-management writes — the **identity** the deep tier modulates. Deep (Stage 4) enables talk-to-player interactions that apply bounded morale deltas through #33 on the same aggregation + interaction code path — the aggregator stays read-only; only the man-management branch writes.

## 3. Dependencies
- **Upstream (needs):** #30 Season Loop, #31 Transfers/Contracts, #45 Board/Ownership, #35 Media (event producers it aggregates), #22 `InteractionTextGenerator` (surface text for man-management prompts).
- **Downstream (consumers):** #38 UI (renders the inbox and man-management dialogues), #33 morale (written to by man-management — the one write direction).

## 4. Persistent state & save impact
Inbox items are largely a **derived view** over already-persisted events (mirrors #37 analytics / #44 discipline read-only derivation) — minimise new stored state. Read/unread flags and any pending man-management state are a small season-state addition → `SEASON_SAVE_FORMAT_VERSION` bump only if retained. Man-management outcomes write to #33's block, not a #46 block.

## 5. Determinism
Read-only/aggregation — **no RNG stream, no domain tag** (consistent with `match-viewer`/analytics being observational, roadmap §6). Man-management text draws consume #22's existing `world.text` cursor via `InteractionTextGenerator.Generate`; the morale write is a deterministic bounded projection into #33, not a draw. Advances on the world tick alongside #30's day-advance loop.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** The inbox is a **read-only aggregator** over event producers (#30/#31/#45/#35) — define the aggregation contract so #46 references producers' emitted events observationally, never mutating them, and no producer references #46.
- **KD-2** Man-management is the **sole write path** — into #33 morale, bounded to #33's valid range. Define the intent/slots passed into #22's `Generate` and the morale-write direction (write-only into #33, read-back for the next prompt).
- **KD-3** No forked text system: build on #22's `InteractionTextGenerator` (the `world.text` sub-stream), sharing the deterministic-text discipline #35 also uses.
- **KD-4** Persistence minimisation: which inbox state is derived-on-read vs stored (read/unread, dismissed) — keep the stored surface tiny.
- **KD-5** Overlap with #35 media: media items surface **in** the inbox but are produced by #35; define the boundary so #46 aggregates without owning press logic.

## 7. Primary surfaces (proposed)
- A read-only inbox aggregation/query surface (proposed) over producer events for #38.
- A man-management interaction entry point (proposed) using #22's `Generate`, writing #33 morale.
- Read/unread state accessors (proposed) — the only candidate for new persisted state.

## 8. Test focus
Aggregator behaviour proven read-only (no producer event mutated); man-management morale write bounded and deterministic; identity behaviour (minimal read-only feed advances byte-identically; man-management disabled == pre-#46 morale); round-trip determinism over any retained read/unread state; fail-loud on unknown intent into `Generate`.

## 9. Open questions / risks
- Boundary blur with #35 media (aggregates vs owns) — the recurring "consumer building a second system" trap.
- Temptation to store the full inbox rather than derive it, inflating save state.
- The single morale-write path must not become a two-way coupling with #33.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
