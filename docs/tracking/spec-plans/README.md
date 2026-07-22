# Management-Layer Spec Plans (candidate #27–#50)

> **Created:** July 22, 2026
> **Status:** PLANS (pre-design-supplement). One high-level plan per candidate spec, governed by
> `../management-layer-spec-roadmap.md`. Numbers are **proposed, not reserved** — nothing here
> changes `SPEC_INDEX.md`; registry rows land only at design-supplement promotion (the #21–#27
> precedent).
> **Purpose:** For each candidate management/off-pitch spec, a consistent one-page plan — scope,
> staging, dependencies, save impact, determinism, key decisions to resolve, proposed surfaces,
> test focus, risks — that a design supplement is then written against.

Each file follows one template (§1 Scope · §2 Staging · §3 Dependencies · §4 Persistent state &
save impact · §5 Determinism · §6 Key design decisions to resolve · §7 Primary surfaces (proposed)
· §8 Test focus · §9 Open questions/risks · Version History). "(proposed)" marks any surface that
does not exist yet; only confirmed existing seams are named bare.

## Index (by authoring wave — see roadmap §7)

| Wave | # | Plan | FR prefix | Determinism |
|------|---|------|-----------|-------------|
| 0 | 27 | [Squad / Player Data Layer](spec-27-squad-player-data-layer.md) | FR-SQ | `0x1F` / ord 81 (allocated) |
| 1 | 30 | [Season & Competition Loop](spec-30-season-competition-loop.md) *(spine)* | FR-SN | `0x22` / 84 |
| 1 | 37 | [Match Analytics & Statistics](spec-37-match-analytics-statistics.md) | FR-AN | read-only — none |
| 1 | 38 | [UI / Client Framework & Screens](spec-38-ui-client-framework-screens.md) *(framework)* | FR-UI | presentation — none |
| 1 | 49 | [Localization — seam + template contract](spec-49-localization-accessibility.md) *(seam contract)* | FR-LC | presentation — none |
| 2 | 28 | [Player Progression & Lifecycle](spec-28-player-progression-lifecycle.md) | FR-PG | `0x20` / 82 |
| 2 | 29 | [Training System](spec-29-training-system.md) | FR-TR | `0x21` / 83 |
| 2 | 40 | [Club Finances & Economy](spec-40-club-finances-economy.md) | FR-FN | `0x29` / 91 |
| 2 | 41 | [Injuries & Medical](spec-41-injuries-medical.md) | FR-MD | `0x2A` / 92 |
| 3 | 33 | [Personalities, Morale & Dynamics](spec-33-personalities-morale-dynamics.md) *(gating — #22 producer)* | FR-HS | `0x25` / 87 |
| 4 | 31 | [Transfers, Contracts & Negotiation](spec-31-transfers-contracts-negotiation.md) *(owns the reusable negotiation seam)* | FR-TX | `0x23` / 85 |
| 4 | 34 | [Staff & Backroom](spec-34-staff-backroom.md) | FR-ST | `0x26` / 88 |
| 4 | 32 | [Scouting & Player Knowledge](spec-32-scouting-player-knowledge.md) | FR-SC | `0x24` / 86 |
| 5 | 43 | [Competition Structure](spec-43-competition-structure.md) | FR-CP | `0x2C` / 94 |
| 5 | 44 | [Discipline & Suspensions](spec-44-discipline-suspensions.md) | FR-DC | read-only — none |
| 5 | 42 | [Youth Academy & Intake](spec-42-youth-academy-intake.md) | FR-YA | `0x2B` / 93 |
| 5 | 45 | [Board & Ownership Dynamics](spec-45-board-ownership-dynamics.md) | FR-BD | `0x2D` / 95 |
| 6 | 35 | [Media & Press Interactions](spec-35-media-press-interactions.md) *(event producer #46 aggregates)* | FR-ME | `0x27` / 89 |
| 6 | 46 | [News, Inbox & Man-Management](spec-46-news-inbox-man-management.md) | FR-NW | read-only — none |
| 6 | 36 | [National Teams & International](spec-36-national-teams-international.md) | FR-NT | `0x28` / 90 |
| 7 | 38 | [UI / Client — screens](spec-38-ui-client-framework-screens.md) *(same file; screens tier)* | FR-UI | presentation — none |
| 7 | 48 | [Match Presentation Depth](spec-48-match-presentation-depth.md) | FR-MP | presentation — none |
| 7 | 47 | [New-Game Setup & DB Editor](spec-47-new-game-setup-db-editor.md) | FR-ED | tooling — none |
| 8 | 49 | [Localization & Accessibility — locales + a11y](spec-49-localization-accessibility.md) *(same file; content tier)* | FR-LC | presentation — none |
| 8 | 50 | [Save Migration & Versioning](spec-50-save-migration-versioning.md) | FR-MG | infra — none |
| 8 | 39 | [Steam Packaging & Release](spec-39-steam-packaging-release.md) | FR-PK | infra — none |

**Critical path:** #27 → #30 → #33 → #31 → #38 → #39.

**Intra-wave order is significant** — within a wave, rows are listed producer-before-consumer, so
"promote in wave order" is unambiguous even where a same-wave dependency exists: Wave 4 is #31
(owns the reusable negotiation seam #34/#32 may consume) → #34 → #32; Wave 6 is #35 (media event
producer) → #46 (aggregates it) → #36. **#38 and #49 each appear twice** — a framework/seam-contract
tier early (Wave 1) and a screens/content tier late (Wave 7/8) — because both are contracts their
producers bind to as they land; the file is shared.

**Determinism block headroom:** the 14 stochastic candidates consume tags `0x20`–`0x2D` /
ordinals 82–95 exactly (the roadmap §6 block), leaving zero slack. The next free slot is
**`0x2E` / 96**; if a currently read-only/presentation candidate (#37/#44/#46/#48/#47/#39/#50, or
the #38/#49 presentation tiers) later needs a draw, it takes `0x2E`/96 onward — it does not
fragment the contiguous 82–95 block. Reserve `0x2E`–`0x2F` / 96–97 as that slack.

## Next step

Promote in wave order: open a full design supplement (`docs/tracking/<name>-design.md`) per
candidate, run it through adversarial review to convergence, author section files at `IN REVIEW`,
and register the row in `SPEC_INDEX.md` at promotion — the same pipeline #21–#26 followed. #27 is
furthest along (design supplement + partial `src/player-database/` already exist) and is the
recommended first promotion.
