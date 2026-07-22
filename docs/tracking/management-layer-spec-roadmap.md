# Management-Layer Specification Roadmap — Stage 1→5 Feature Set

> **Created:** July 22, 2026
> **Status:** ROADMAP (meta-planning — one level *above* a design supplement). This document does
> **not** design any single system; it decides **which design supplements to open, in what order,
> under what candidate spec numbers**, for the manager-facing / off-pitch half of the game. Each
> candidate spec below is authored through the project's normal pipeline (design supplement →
> section files at `IN REVIEW` → adversarial review to convergence → `APPROVED`), exactly as
> #21–#26 and candidate #27 were.
> **Purpose:** Provide a high-level plan for the specification sections covering: UI/client, the
> career/season game loop, transfers/scouting/contracts, training, aging/progression, staff and
> squad dynamics, media and national teams, and the Steam packaging/release pass.
> **Governance note:** Candidate numbers here are **proposed, not reserved** — per `SPEC_INDEX.md`'s
> own rule, registry rows (and the RESERVED-section reservation) land only when a design supplement
> opens. Nothing in this file changes `SPEC_INDEX.md`.

---

## 0. Where this fits

The 26 approved specs plus candidate #27 (Squad/Player Data Layer — design supplement exists,
not yet promoted) cover the **match engine** and its immediate tactical/off-pitch substrate. The
eight feature areas in this roadmap are the **management game** that wraps that engine — the part
the master development plan schedules across Stages 2–5 (`master-development-plan.md` §4 V1, §5
Stages 3–5). Two structural facts from the current codebase shape the entire plan:

1. **The composition roots already exist.** `TacticalDirector.SeasonSave` (`src/season-save/`)
   already sits *above* both `match-engine` and `living-world` and bundles a `WorldStore.Snapshot()`
   world + an optional in-progress match into one file. The career/season loop is the natural
   **owner** of that root — it is the assembly that drives `SeasonSaveManager` day to day. The
   presentation layer already has a precedent too (`src/match-viewer/`, `TacticalDirector.MatchViewer`,
   explicitly *not* referenced by any sim assembly).

2. **Living World (#22) was built to consume a human-systems model that does not exist yet.**
   #22 reads a "vol-2/vol-3 human-systems model" **read-only** and has documented, dormant seams
   (`WorldLoop` phases 1/2/5, `BackgroundTierSim`) that FR-LW-031 forbids wiring until their
   producers exist. **Candidate #33 (Personalities/Morale/Dynamics) is that producer.** This is the
   single most important sequencing constraint in the plan — see §4.

Everything below is a **Stage-forward pull** in the same sense #21/#22/#27 were: the master plan
places most of it at Stage 2–5, but the engine already has concrete present-day gaps (no squad
data variety, no season to play, no reason for the living world's arcs to fire), so the data and
loop layers are pulled forward minimal-first and deepened later.

---

## 1. Proposed candidate spec set (#27–#50)

| # | Working title | Feature bullet(s) covered | Master-plan home | Tier¹ |
|---|---------------|---------------------------|------------------|-------|
| 27 | **Squad / Player Data Layer** *(design supplement exists)* | (foundation for aging/progression) | §4.2 | Stage-1 pull |
| 28 | **Player Progression & Lifecycle** | Aging, retirement, regens/newgens, attribute CA/PA growth-decline | §4.3 aging, §5 youth | S2 min → S3 deep |
| 29 | **Training System** | Team + individual training, coaching effect, fitness/injury interplay | §4.4 | S2 min → S3 deep |
| 30 | **Season & Competition Loop** | Career/season game loop, league/fixtures/table/calendar, board objectives, career continuity | §4.1, §4.5 | S2 |
| 31 | **Transfers, Contracts & Negotiation** | Transfer windows, bids, contracts/clauses, wages/budgets, negotiation | §4.3, §5 (complex clauses) | S2 min → S3 deep |
| 32 | **Scouting & Player Knowledge** | Scout assignments, attribute masking/fog-of-war, reports, recommendations | §5 recruitment | S3 |
| 33 | **Personalities, Morale & Squad Dynamics** | Personality model, morale, cliques/chemistry, mentoring — *the vol-2 human-systems model #22 consumes* | §5 Stage 4 (Master Vol 2) | S4 (pulled) |
| 34 | **Staff & Backroom** | Coaches, scouts, physios, staff roles, hiring | §5 Stage 3 | S3 |
| 35 | **Media & Press Interactions** | Press conferences, questions, morale/reputation effects | §5 Stage 4 | S4 |
| 36 | **National Teams & International Management** | Call-ups, international windows, tournaments | §5 Stage 5 | S5 |
| 37 | **Match Analytics & Statistics** | (prerequisite for UI + post-match reports) possession/shots/xG/PPDA/heatmaps | §3.3 | S1 |
| 38 | **UI / Client Framework & Screens** *(cluster — likely splits)* | Menus, tactics screen, match view, squad/transfer/training/scouting screens | §3.1/§3.4, §4 | S1 min → S2 full |
| 39 | **Steam Packaging & Release Engineering** | Build pipeline, store page, achievements, Steam Cloud save, QA/cert pass | §4.6, §4.7 release | S2 |
| 40 | **Club Finances & Economy** ² | Budgets, wages, FFP, revenue/sponsorship (split from #31) | §5 Stage 3 financials | S2 min → S3 deep |
| 41 | **Injuries & Medical** ² | Injury model, treatment, physio/medical-staff effects | §4.2 injury mgmt | S2 min → S3 deep |
| 42 | **Youth Academy & Intake** ² | Academy pipeline, bio-banding intake, youth contracts | §5 Stage 3 youth | S3 |
| 43 | **Competition Structure** ² | Cups, continental competitions, promotion/relegation | §4.1, §5 | S2 min → S5 deep |
| 44 | **Discipline & Suspensions** ² | Season-level card accumulation, bans | §4.1 | S2 |
| 45 | **Board & Ownership Dynamics** ² | Ownership types, takeovers, board confidence | §5 Stage 3 board | S3 |
| 46 | **News, Inbox & Man-Management** ² | Manager comms hub; talk-to-player interactions | §4.5, §5 Stage 4 | S2 min → S4 deep |
| 47 | **New-Game Setup & Database Editor** ² | League/start selection, custom DB, data-authoring surface | (tooling) | S2 |
| 48 | **Match Presentation Depth** ² | Commentary, animation/3D, audio | §3.1 | S1 min → S2+ deep |
| 49 | **Localization & Accessibility** ² | i18n, a11y | (cross-cutting) | S2 |
| 50 | **Save Migration & Versioning** ² | Live-save migration across game updates | §4.6 | S2 |

¹ **Tier** = master-plan staging. "S2 min → S3 deep" means the spec is authored with an explicit
minimal-first Stage-2 surface and a deeper Stage-3+ extension, mirroring how #27 is a Stage-2
player-database pulled forward as a Stage-1 data layer. Numbers are **proposed** and may compress
(e.g. #37 could fold into #38) or expand: **#38 and #49 each split into an early framework/seam tier
and a late screens/content tier** across waves (one file, two wave rows — see §7).

² **Gap-fill (added v0.2).** #40–#50 were surfaced by a follow-up "what else lacks a spec" review
of the master plan against the original feature list. Numbers are stable IDs, not authoring order —
the table is roughly numeric; dependency/authoring order is §2/§7. The load-bearing gap-fills are
**#40 Finances**, **#41 Injuries/Medical**, and **#43 Competition Structure** — the season loop
(#30) is thin without them.

---

## 2. Dependency graph (author/land order, not stage order)

```
                 ┌─────────────────────────────┐
                 │ existing: match-engine,      │
                 │ season-save, living-world#22,│
                 │ tactical-instructions#21     │
                 └──────────────┬──────────────┘
                                │
        #27 Squad/Player Data ──┤ (foundation: canonical attributes, rosters)
                                │
     ┌──────────────┬──────────┼───────────────┬───────────────┐
     ▼              ▼          ▼                ▼               ▼
 #28 Progression  #29 Training  #37 Analytics  #33 Human-      #34 Staff
 (needs #27 CA/PA)(needs #27)   (needs engine) systems model   (needs #33
                                               (unblocks #22    for personality)
                                               dormant seams)
     │              │                              │
     └──────┬───────┴──────────────┐               │
            ▼                       ▼               ▼
   #30 Season & Competition Loop ◄──┴── #31 Transfers/Contracts ── #32 Scouting
   (career spine; owns SeasonSave)       (needs #27,#30,#34 econ    (needs #27,#30,
            │                              + #33 negotiation psych)   #34 staff,#33)
            ├───────────────► #35 Media (needs #30 events + #33 morale)
            └───────────────► #36 National Teams (needs #30 calendar + #27 pool)
            │
            ▼
   #38 UI/Client cluster (renders all of the above; presentation layer,
   referenced by nothing in sim — starts against #37/#21/match-engine early)
            │
            ▼
   #39 Steam Packaging (last; needs a shippable build + save/cloud + QA)
```

**Critical-path spine:** #27 → #30 → #33 → #31 → #38 → #39. Everything else attaches to that spine.
(#33 is on the spine because the §4 sequencing constraint puts it before #31's psychology-driven
negotiation and #22's dormant-seam activation — see §7 and §4.)
**The one non-obvious ordering constraint:** #33 (human-systems model) must land before #22's
dormant `WorldLoop` phase-1/2/5 seams and before #31/#35's psychology-driven behaviour can be
anything but a stub — do not build those consumers first (FR-LW-031 phantom-interface rule).

---

## 3. Per-spec scope sketches

Each is a one-paragraph scope + the load-bearing design decisions a supplement will have to make.
This is deliberately not spec content — it is the "what a design supplement for this must resolve"
list.

### #28 Player Progression & Lifecycle *(Stage 2 min → Stage 3 deep)*
Aging, decline, retirement, and regens/newgens on the **world tick** (`WorldClock`, one day =
one `worldTick` — never the match tick). Attribute progression via a CA/PA (current/potential
ability) model over #27's canonical record.
- **KD:** Stage-2 minimal is the master plan's literal `§4.3` model (>30 −1/yr, <24 +1/yr, retire
  at 36) as a **deterministic per-day projection**, deepened to per-attribute CA/PA growth curves
  at Stage 3. Both tiers must be one code path with a config dial, not a rewrite.
- **Determinism:** new RNG sub-stream + domain tag + `SubsystemOrdinals` entry in the off-pitch
  band (alongside living-world `0x1E`/80, player-database `0x1F`/81 — reserve the next block, see §6).
- **Regens** must reference clubs/nations from #27's roster world, produced day-deterministically.

### #29 Training System *(Stage 2 min → Stage 3 deep)*
Weekly team + individual training that feeds fitness, form, injury risk, and (Stage 3) attribute
growth into #28.
- **KD:** Stage-2 is the `§4.4` "pick one of N focuses → affects form/fitness only" model; Stage 3
  adds granular per-attribute training that becomes an **input to #28's growth curve** (so #28 and
  #29 share the progression seam, not duplicate it).
- **Interaction:** must not double-count fatigue with the match engine's in-match fatigue — training
  fatigue is a world-tick accumulator, match fatigue is a match-tick one; define the reconciliation.

### #30 Season & Competition Loop *(Stage 2)* — the career spine
The playable spine: league table, fixture generation, calendar, match-day flow, board objectives,
multi-season career continuity. **Owns the `SeasonSave` composition root** and drives it day to day.
- **KD:** extends `SeasonSaveManager` from "world + optional in-progress match" to "world + season
  state (table, fixtures, calendar cursor, board state) + optional match" — a new
  `SEASON_SAVE_FORMAT_VERSION` bump, with the season block as another opaque, independently
  version-gated sub-blob (the codec-never-parses-sub-blobs pattern already in `SeasonSaveCodec`).
- **KD:** fixture generation must be deterministic from the world seed (round-robin schedule).
- **Owns the day-advance loop** that ticks #28/#29/#22/#33 forward between fixtures.

### #31 Transfers, Contracts & Negotiation *(Stage 2 min → Stage 3 deep)*
Transfer windows, player search, bids, contracts (wage/length/clauses), club budgets, and
negotiation. Stage-2 minimal = `§4.3` accept/reject + summer window; Stage-3 = agents, clauses,
loans, wage structures.
- **KD:** negotiation counterparty behaviour is driven by #33 (club/agent personality); at Stage 2
  it is a deterministic valuation function, at Stage 3+ it reads the personality model. Author the
  Stage-2 valuation as the identity the personality layer later modulates.
- **Economy:** budgets/wages are a new world-state block; define whether a lightweight financial
  model lives here or is split into its own spec at Stage 3 (flag: **possible #31 split**).

### #32 Scouting & Player Knowledge *(Stage 3)*
Scout assignments, **attribute masking / fog-of-war** (the manager sees ranges, not truths, until
scouted), scout reports, and recommendations.
- **KD:** knowledge is a **per-manager view layer** over #27's true attributes — never a mutation
  of them. Determinism: scouting accuracy draws from a dedicated sub-stream.
- **Depends on #34** (scouts are staff) and #33 (scout judgement quality as a personality/skill).

### #33 Personalities, Morale & Squad Dynamics *(Stage 4, pulled forward as needed)*
The **canonical human-systems model** (Master Vol 2): personality traits, morale/happiness
(the H-Gate confidence-vs-self-efficacy model), cliques/chemistry, mentoring. **This is the
producer #22 Living World was written to consume read-only.**
- **KD:** #22 already defines the *interaction/memory/arc* layer over this model; #33 must expose
  exactly the read-only surface #22's FR-LW-004 `PlayerEdge`/relationship-layer contract expects,
  so #22's dormant seams light up without a #22 rewrite. Cross-check #22 §2.1/§3.1 before scoping.
- **KD:** morale must feed match-engine inputs (already an attribute-projection seam via #27) and
  transfer/contract willingness (#31) — define the projection direction, not a two-way coupling.

### #34 Staff & Backroom *(Stage 3)*
Coaches, scouts, physios, and their roles/skills/hiring. Staff are **entities with attributes**
(reuse #27's record shape where it fits) that modulate #29 (coaching), #32 (scouting), and injury
(#29/#28).
- **KD:** staff hiring is a transfer-market analogue — decide reuse-vs-parallel with #31's
  negotiation machinery rather than duplicating it.

### #35 Media & Press Interactions *(Stage 4)*
Press conferences, question generation, answer choices, and their morale/reputation consequences.
- **KD:** this is a **natural consumer of #22's `InteractionTextGenerator`** (deterministic
  procedural text off the `world.text` sub-stream) and #33's morale model — build it as a
  consumer of those two, not a fresh text/morale system.

### #36 National Teams & International Management *(Stage 5)*
Call-ups, international windows in the #30 calendar, and tournaments.
- **KD:** international windows are a calendar-overlay on #30; the national-team squad is a
  selection view over #27's global player pool. Depends on the global-sim scope maturing (Stage 5).

### #37 Match Analytics & Statistics *(Stage 1)* — UI prerequisite
Possession, shots/on-target, pass completion, tackles, plus advanced (xG location model, PPDA,
territorial %, heatmaps). Consumes the match engine's already-emitted events (Event System #17).
- **KD:** stats are derived **read-only** from the event ledger the match engine already produces —
  no new match-engine surface, mirroring how `match-viewer` reads world state observ­ationally.
  This is why it can be authored early (Stage 1) and unblocks post-match report UI.

### #38 UI / Client Framework & Screens *(Stage 1 min → Stage 2 full)* — will split
Menus, tactics screen, the interactive match view (upgrading the current bare live web viewer),
and the Stage-2 management screens (squad, transfer, training, scouting). Unity UGUI per the
master plan §3.4.
- **KD (layer taxonomy):** UI is the **presentation layer** — no sim assembly may reference it
  (the `match-viewer` precedent). It reads observation surfaces + view models; it never mutates
  sim state except through the same public command seams the engine already exposes
  (`SetTeamTactic`/`SetPlayerTactic`, the loop's day-advance/transfer-action APIs).
- **Almost certainly splits** into: (a) UI framework + screen-navigation + view-model contract,
  (b) tactics/match-view screens (Stage 1), (c) management screens (Stage 2). Recommend authoring
  the framework spec first and the screen specs as it stabilises.

### #39 Steam Packaging & Release Engineering *(Stage 2)* — last
Build/packaging pipeline, store-page assets checklist, achievements, **Steam Cloud save**
(binds to the #30/#39 save format), and the release QA/certification pass.
- **KD:** Steam Cloud save is a distribution concern over #30's save format — versioning +
  conflict resolution must be specified against `SEASON_SAVE_FORMAT_VERSION`. Achievements are a
  read-only consumer of career/season events.
- **KD:** this spec is mostly process/checklist (build determinism, cert QA gate) rather than
  sim code, so it is authored last against a genuinely shippable build — do not front-load it.

---

### #40 Club Finances & Economy *(Stage 2 min → Stage 3 deep)* — split from #31
Budgets, wages, revenue/sponsorship, FFP. **KD:** author the Stage-2 minimal "budget from league
finish" as the identity the Stage-3 revenue/FFP model modulates; it is the counterparty-constraint
#31 negotiation reads. New world-state block; deterministic per-day/per-season accounting.

### #41 Injuries & Medical *(Stage 2 min → Stage 3 deep)* — split from #29
Injury occurrence/severity/recovery + physio/medical-staff modulation. **KD:** injury draws are a
world-tick **and** match-tick concern — define which layer owns occurrence (match incident vs.
training/fatigue accumulation) and reconcile with #29's fatigue accumulator. Dedicated RNG stream.

### #42 Youth Academy & Intake *(Stage 3)* — distinct from #28 regens
The academy pipeline: annual intake generation (bio-banding, Master Vol 1), youth contracts,
promotion to senior squad. **KD:** intake generation reuses #28's regen/generation machinery keyed
to club/nation, but the academy *structure* (facilities, coaching → intake quality) is #34/#40-coupled.

### #43 Competition Structure *(Stage 2 min → Stage 5 deep)*
Cups, continental competitions, promotion/relegation. **KD:** #30 ships single-league (master plan
§4.1); this generalises #30's fixture/table machinery to multiple concurrent competitions and adds
knockout draws (deterministic from the world seed). Promotion/relegation is a season-boundary
transform on #30's league state. RNG stream for draws.

### #44 Discipline & Suspensions *(Stage 2)*
Season-level card accumulation, thresholds, bans. **KD:** read-only derivation over the match
engine's already-emitted card events (like #37 analytics) → a suspension-availability view #30's
squad selection consumes. No new RNG.

### #45 Board & Ownership Dynamics *(Stage 3)*
Ownership types, takeovers, board confidence beyond #30's season objectives. **KD:** board confidence
is a morale-model analogue (reuse #33's shape); takeover events draw from a dedicated stream. Feeds
#40 budgets and #30's sacking/job-security state.

### #46 News, Inbox & Man-Management *(Stage 2 min → Stage 4 deep)*
The manager's inbox/comms hub + talk-to-player interactions (distinct from media #35). **KD:** the
inbox is a **read-only aggregator** of season/transfer/board/media events; man-management writes to
#33 morale. A natural consumer of #22's `InteractionTextGenerator` — build on it, don't fork it.

### #47 New-Game Setup & Database Editor *(Stage 2)*
Start/league selection, custom database, the data-authoring surface. **KD:** the editor is the
authoring front-end over #27's roster/text-import format (the Stage-0 text loaders are its parser
seam). Tooling layer — no RNG, no sim reference.

### #48 Match Presentation Depth *(Stage 1 min → Stage 2+ deep)*
Commentary, animation/3D, audio — upgrading the bare live viewer. **KD:** presentation layer,
observation-only (the `match-viewer` contract). Commentary text can consume #22's deterministic
generator. No sim mutation, no RNG in the determinism-relevant sense.

### #49 Localization & Accessibility *(Stage 2)*
i18n string catalogue + a11y. **KD:** cross-cutting presentation concern; all user-facing text
(including #22/#35/#46 generated text) routes through one localization seam. No sim reference.

### #50 Save Migration & Versioning *(Stage 2)*
Migrating live player saves across shipped game updates (distinct from the determinism format
versions, which gate corruption, not forward-migration). **KD:** defines the migration contract over
`SEASON_SAVE_FORMAT_VERSION`/`WORLD_STORE_FORMAT_VERSION` bumps — how a v(N) save opens in a v(N+1)
build. Infra/process spec; pairs with #39 Steam Cloud.

---

## 4. The #22 / #33 sequencing constraint (call-out)

Living World #22 is APPROVED and its T0 services are landed, but its `WorldLoop` phase-1 (structured
match-outcome ingest), phase-2 (vol-2 read), and phase-5 (background-tier sim) are **deliberately
null seams** because their producers do not exist. Two of the three producers are in this roadmap:

- **phase-1 producer** = the #30 season loop emitting structured match-outcome events per day.
- **phase-2 producer** = the #33 human-systems model #22 reads read-only.

**Do not activate #22's dormant seams before #30 and #33 land.** When they do, the activation is a
*wiring* change in #22 (no #22 redesign), exactly as its section files anticipate. This is the
payoff of #22 having been built phantom-free.

---

## 5. Cross-cutting concerns every one of these specs inherits

1. **Determinism on the world tick.** All management systems advance on `WorldClock`
   (one day = one `worldTick`), never the 10 Hz/60 Hz match loops (living-world KD-4). Every
   stochastic system needs a dedicated RNG **sub-stream** with its own **domain tag** +
   `SubsystemOrdinals` entry (reserve a contiguous off-pitch block now — see §6).
2. **Save format is load-bearing.** Each system that adds persistent state bumps a format version
   (`SEASON_SAVE_FORMAT_VERSION` for season state; `WORLD_STORE_FORMAT_VERSION` if it adds to the
   world store) and lands as an **opaque, independently version-gated sub-blob** so the season
   codec never has to parse it (the pattern already in `SeasonSaveCodec`).
3. **Snapshot round-trip determinism.** The correctness contract that governs the match engine
   ("save@N → restore → advance == uninterrupted run", byte-identical) extends to the world/season
   loop. Every new world-state field must be serialized and covered by a round-trip determinism test.
4. **Layer taxonomy / no phantom interfaces.** Sim → presentation is one-directional (UI references
   sim, never the reverse). Consumers are not built ahead of producers (FR-LW-031). The composition
   roots (season loop above match+world; UI above everything) are the only assemblies that see both
   sides.
5. **Minimal-first, deepened-later, one code path.** Master plan Stage 2 wants *simplified*
   transfers/training/aging; Stage 3–5 wants depth. Author each spec so the Stage-2 surface is the
   **identity** the deeper stage modulates (the #21 default-behaviour-neutral discipline), not a
   throwaway the deeper stage rewrites.
6. **Attribute masking is a view, not a mutation** (#32) — a recurring trap: player knowledge,
   morale-as-seen, and scout opinion are all per-manager views over true state.

---

## 6. Reservations to make when the first supplement opens

When the first of these promotes past design-supplement stage, allocate a **contiguous off-pitch
determinism block** so numbering does not thrash later (precedent: living-world `DOMAIN_TAG = 0x1E` /
ordinal 80; player-database `0x1F` / 81). Proposed next block, to be pinned in Deterministic
Simulation #16 §3.4 + `SubsystemOrdinals` at each spec's promotion (not now):

| Candidate | Domain tag (proposed) | SubsystemOrdinal (proposed) |
|---|---|---|
| #28 Progression | `0x20` | 82 |
| #29 Training | `0x21` | 83 |
| #30 Season loop | `0x22` | 84 |
| #31 Transfers | `0x23` | 85 |
| #32 Scouting | `0x24` | 86 |
| #33 Human-systems | `0x25` | 87 |
| #34 Staff | `0x26` | 88 |
| #35 Media | `0x27` | 89 |
| #36 National teams | `0x28` | 90 |
| #40 Finances | `0x29` | 91 |
| #41 Injuries/Medical | `0x2A` | 92 |
| #42 Youth academy | `0x2B` | 93 |
| #43 Competition draws | `0x2C` | 94 |
| #45 Board/ownership | `0x2D` | 95 |

(#37 analytics, #44 discipline, #46 news/inbox, #38/#48 presentation, #47 editor, #39 packaging,
#49 localization, #50 migration are **read-only, presentation, or infra** — no RNG stream, no
domain tag, consistent with `match-viewer`/analytics being observational.)

**Headroom:** the 14 rows above consume `0x20`–`0x2D` / 82–95 exactly — zero slack. The next free
slot is **`0x2E` / 96**; reserve **`0x2E`–`0x2F` / 96–97** as slack so that if a candidate currently
classified read-only/presentation/infra later discovers it needs a draw, it extends from `0x2E`/96
onward and never has to fragment or renumber the contiguous 82–95 block.

---

## 7. Authoring sequence (waves)

Each candidate is a full pipeline run (design supplement → adversarial review to convergence →
section files → sign-off), and each should record its own OPEN ISSUES entry in root `CLAUDE.md`
the way #21–#27 did. Items **within a wave** can be authored in parallel; a wave's dependencies are
satisfied by the waves above it. **Per-spec high-level plans live in `spec-plans/`** (one file per
candidate).

**Wave 0 — Foundation**
- **#27 Squad/Player Data** — promote the existing supplement first; every data spec keys off its
  canonical attributes.

**Wave 1 — Spine + cheap read-only (parallel)**
- **#30 Season & Competition Loop** — the career spine; owns the `SeasonSave` root.
- **#37 Match Analytics** — read-only over the event ledger; unblocks post-match UI.
- **#38 UI *framework* only** — the framework / view-model contract can start now against tactics +
  #37; screen specs wait for their data specs (Wave 7).
- **#49 Localization *seam + template contract* only** — the localization lookup seam + procedural-text
  template contract publishes now, so every text producer authored afterward (#38 framework here,
  #35/#46 in Wave 6, #38 screens in Wave 7) emits through it from the start; only #22's already-built
  `InteractionTextGenerator` needs a retrofit. Locales + a11y *content* is Wave 8. (This is the
  framework/content split that #38 also uses; the file is shared.)

**Wave 2 — Attach to #27 / #30**
- **#28 Progression** · **#29 Training** · **#41 Injuries/Medical** · **#40 Finances** — all need
  #27 data + #30's day-advance loop; #40/#41 must precede #31.

**Wave 3 — Human-systems unblocker (gating)**
- **#33 Personalities, Morale & Dynamics** — author before #22's dormant seams, #31, and #35 need
  real psychology (phantom-interface rule, §4).

**Wave 4 — Recruitment / economy cluster**
- **#31 Transfers/Contracts → #34 Staff → #32 Scouting** — #31 first because it owns the reusable
  negotiation seam that #34 (hiring) and #32 (bids) may consume (authoring #34 first would reference
  a seam #31 has not defined; #31's own #34-influence is a deferred ×1.0 routing seam, the established
  identity-until-producer pattern). #34 before #32 (scouts are staff). #31 needs #40 economy (Wave 2) +
  #33 negotiation psychology (Wave 3).

**Wave 5 — Season extensions**
- **#43 Competition Structure** · **#44 Discipline** (extend #30) · **#42 Youth Academy**
  (needs #34/#40) · **#45 Board/Ownership** (needs #33 shape; feeds #40/#30).

**Wave 6 — World / comms consumers**
- **#35 Media → #46 News/Inbox & Man-Management → #36 National Teams** — #35 before #46 because #46's
  inbox aggregates #35's media events (producer before consumer, even though #46's aggregator is
  producer-generic). All three consume #30 events + #33 morale + #22's text generator.

**Wave 7 — Presentation / UI**
- **#38 UI screens** (deepen as data specs land) · **#48 Match Presentation Depth** ·
  **#47 New-Game Setup / DB Editor**.

**Wave 8 — Release / cross-cutting (last)**
- **#49 Localization *locales + a11y content*** (the seam contract landed in Wave 1) · **#50 Save
  Migration** · **#39 Steam Packaging** — against a genuinely shippable build. (#50 stays whole in
  Wave 8, not split like #49: its per-bump migration steps are a *post-ship* concern, so pre-ship
  format bumps need no step and there is no continuous-emission retrofit to front-load.)

**Critical path:** #27 → #30 → #33 → #31 → #38 → #39.

---

## Version History

| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial roadmap: candidate spec set #27–#39 for the management/off-pitch feature areas; dependency graph; per-spec scope sketches; cross-cutting concerns; #22/#33 sequencing call-out; proposed off-pitch determinism block; recommended authoring order. |
| v0.2 | July 22, 2026 | Folded in gap-fill candidates #40–#50 (Finances, Injuries/Medical, Youth Academy, Competition Structure, Discipline/Suspensions, Board/Ownership, News/Inbox & Man-Management, New-Game Setup/DB Editor, Match Presentation Depth, Localization/Accessibility, Save Migration) surfaced by a follow-up master-plan gap review; extended the determinism block (5 new tags), §3 scope sketches, and §7 authoring placement. |
| v0.3 | July 22, 2026 | Adversarial-review consistency pass over the roadmap + `spec-plans/`: §2 critical-path spine corrected to include #33 (matched §7/README); §7 intra-wave order set producer-before-consumer (Wave 4 → #31, #34, #32; Wave 6 → #35, #46, #36); **#49 localization split into a Wave-1 seam+template contract tier + Wave-8 content tier** (mirrors #38) so text producers bind to the seam as they land; §6 gained a determinism-block-headroom note (next free `0x2E`/96; reserve `0x2E`–`0x2F`/96–97 slack); §1 footnote updated (#38 + #49 both split; stale §5 pointer fixed). |
