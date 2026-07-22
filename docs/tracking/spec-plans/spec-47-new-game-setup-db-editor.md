# Spec #47 — New-Game Setup & Database Editor — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#47** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** (tooling) · **Tier:** S2 · **Wave:** 7 · **FR prefix (proposed):** FR-ED
> **Determinism:** read-only / tooling — none (no RNG stream, no domain tag; authoring front-end, no sim reference)
> **Purpose:** Start/league selection and the data-authoring / custom-database surface — an editor front-end over #27's roster/text-import format.

## 1. Scope
The new-game setup flow (start point, league/team selection) and the database editor: a data-authoring surface for creating/editing clubs, players, and rosters that feed a new game. The editor is the authoring front-end over #27's roster/text-import format — the Stage-0 text loaders (`SquadFileLoader` and the tactic/text loaders they parallel) are its parser seam. **Out of scope:** the roster/attribute model and its parse/validation grammar (owned by #27 — the editor reads and writes that format, it does not redefine it); the runtime sim that consumes an authored database; the UI framework/navigation shell that hosts the editor (that is #38). Authoring produces a database; playing it is #30's job.

## 2. Staging (minimal-first → deep)
Minimal identity = start/league selection over the existing authored data plus round-trip load/edit/save through #27's text-import format (the `SquadFileLoader` grammar) — no new data shape, just an editing surface over the parser seam. The deep tier adds richer authoring (bulk edit, validation UX, custom leagues) on that same import-format code path; the editor never becomes a second source of truth for the data model — it stays a front-end over #27's format so the Stage-0+1 binary loader swap leaves the editor's contract intact.

## 3. Dependencies
- **Upstream (needs):** #27 Squad/Player Data Layer — its roster/text-import format and Stage-0 text loaders (`SquadFileLoader`) are the editor's parser seam; #43 competition structure (for custom-league setup, at depth).
- **Downstream (consumers):** a new game — authored data feeds #30's season start; but no sim assembly references the editor.

## 4. Persistent state & save impact
The editor reads/writes #27's authoring format (text at Stage 0, the pinned binary at Stage 0+1 via parser swap) — it adds no new save block and does not touch `SEASON_SAVE_FORMAT_VERSION` / `WORLD_STORE_FORMAT_VERSION`. Authored databases are input artifacts, not live saves (live-save migration is #50). Tooling — no persistent sim state of its own.

## 5. Determinism
Read-only / tooling — no RNG stream, no domain tag. Authoring is a human-driven edit over a data format; it neither advances the sim nor draws from a deterministic stream. Determinism enters only when #30 boots a game from the authored data (owned by #30/#27), so #47 needs no stream of its own — consistent with the roadmap §6 classification of the editor as tooling with no domain tag.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (parser seam — load-bearing):** the editor is the authoring front-end over #27's roster/text-import format; the Stage-0 text loaders are its parser seam. What exactly is the read/write contract, and how does it survive #27's Stage-0+1 text→binary loader swap unchanged (the parser-swap discipline)?
- **KD-2:** validation ownership — does the editor reuse #27's loader validation (fail-loud bounds gates in `SquadFileLoader`) as its single validation authority, or add an editor-side layer? (Reuse to avoid a second, drifting truth.)
- **KD-3:** custom-league / setup scope at Stage 2 vs. the deeper competition-structure authoring gated on #43 — where is the minimal boundary?
- **KD-4:** is the editor a mode inside #38's client shell or a separable tool, and either way how does it avoid the UI owning data-model logic (layer taxonomy)?
- **KD-5:** how is an authored database handed to #30 as a new-game seed without the editor referencing the sim loop?

## 7. Primary surfaces (proposed)
- A setup/selection flow (proposed) producing a new-game configuration.
- An authoring surface (proposed) reading/writing #27's roster/text-import format via its loader seam.
- Reuse of #27's loader validation (proposed) as the single validation authority.
- A new-game handoff artifact (proposed) consumed by #30 — data in, no sim reference out.

## 8. Test focus
Round-trip fidelity: author → save → load through #27's format is field-identical (the `SquadFileLoader` round-trip discipline). Validation reuse: malformed authored data fails loud through #27's existing loader gates, not a divergent editor-side path. Layer taxonomy: the editor references the data/import format only, never a sim loop assembly. Parser-swap resilience: the editor contract is unchanged by the text→binary loader swap.

## 9. Open questions / risks
- Validation drift (KD-2): an editor-side validation layer that diverges from #27's loader gates is the recurring "two sources of truth" trap.
- Scope creep into competition/league authoring before #43 exists (Wave 5) would create a phantom dependency.
- If hosted inside #38, care that the editor's data-model logic does not leak into the presentation layer (KD-4).
- The authoring format is human-facing (not determinism-pinned, like the Stage-0 tactic grammar) — must not be mistaken for a wire format.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
