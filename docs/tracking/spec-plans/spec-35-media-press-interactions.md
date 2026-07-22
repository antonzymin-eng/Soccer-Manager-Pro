# Spec #35 — Media & Press Interactions — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#35** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 4 · **Tier:** S4 · **Wave:** 6 · **FR prefix (proposed):** FR-ME
> **Determinism:** domain tag `0x27` / SubsystemOrdinal 89 (proposed off-pitch block, roadmap §6)
> **Purpose:** Press conferences and press interactions — question generation, answer choices, and their morale/reputation consequences — built as a consumer of #22's text generator and #33 morale.

## 1. Scope
Pre/post-match and event-driven press conferences: deterministic question generation off match/season context, a bounded set of manager answer choices, and the morale/reputation consequences of each choice. Question and answer surface text is generated through #22's `InteractionTextGenerator` (the `world.text` sub-stream); morale effects write through the #33 model's projection.
Out of scope: the text-generation engine itself (owned by #22), the morale model itself (#33), the inbox that surfaces media items (#46), a fresh reputation system if one already belongs to #30/#40.

## 2. Staging (minimal-first → deep)
Minimal surface = one question archetype per event with a small fixed answer set producing a scalar morale/reputation delta — the **identity** the deep tier modulates. Deep tier adds richer question selection (context, rivalry, form), mood-aware phrasings, and multi-target consequences (player, board, fanbase) on the same generate→choose→apply code path.

## 3. Dependencies
- **Upstream (needs):** #30 season/competition events (what prompts a conference), #33 morale (consequence target + phrasing mood), #22 `InteractionTextGenerator` (surface text).
- **Downstream (consumers):** #46 News/Inbox (surfaces conference items), reputation state (manager/club), #38 UI (renders the interaction).

## 4. Persistent state & save impact
Little new persistent state — press interactions are largely transient. Any retained reputation delta or pending-conference queue lands as a small season-state addition → `SEASON_SAVE_FORMAT_VERSION` bump as an opaque version-gated sub-blob if needed; consequences write to #33's world-state block (no new block of its own where avoidable).

## 5. Determinism
World tick (`WorldClock`). New RNG sub-stream + domain tag `0x27` + `SubsystemOrdinals` entry 89 (proposed) for question-selection draws — but text surface draws consume #22's existing `world.text` cursor, not this stream, so player-triggered text never perturbs media's own selection stream (mirrors #22's `world.text` vs `world.arcs` separation). Registration pinned at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Build strictly as a **consumer** of #22's `InteractionTextGenerator` and #33 morale — no forked text or morale system. Define the intent/slots #35 passes into `Generate` and the exact morale-write direction into #33.
- **KD-2** Two RNG axes: does #35 own a selection sub-stream (`0x27`) while text draws stay on `world.text`? Confirm the cursor-separation invariant so a rendered conference does not shift the arc/text cursor.
- **KD-3** Reputation: reuse an existing #30/#40 reputation field, or introduce a minimal manager-reputation scalar here? Avoid a duplicate.
- **KD-4** Consequence scope at Stage 2 vs Stage 4 (single-player morale vs squad/board/fan spread) — one code path.
- **KD-5** Who queues a conference (the #30 day-advance loop) and how does #46 discover the resulting item without #35 referencing the inbox directly?

## 7. Primary surfaces (proposed)
- A conference-generation entry point (proposed) invoked by the #30 loop around fixtures.
- An answer-choice → consequence applier (proposed) that writes #33 morale and reputation.
- Read models for #46/#38 (proposed) exposing the generated Q/A and outcome.

## 8. Test focus
Round-trip determinism over any retained reputation/queue state; identity behaviour (minimal single-choice conference reproducible byte-for-byte); the `world.text`-vs-selection cursor separation locked by a determinism test; fail-loud on unknown intent/slots into `Generate`; morale write bounded to #33's valid range.

## 9. Open questions / risks
- Reputation ownership ambiguity (#30/#40 vs here) risks a duplicate truth.
- Coupling temptation: media should read #33 and write a bounded morale delta, not become a second morale engine.
- Consequence-spread scope creep at Stage 4.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
