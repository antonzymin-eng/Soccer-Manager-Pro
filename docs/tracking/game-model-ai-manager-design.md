# Tactical Presets & AI-Manager Selection — Design Supplement

> **Created:** July 7, 2026
> **Last Updated:** July 8, 2026 (v0.5 — **PROMOTED**: candidate #26 authored as section files at
> `IN REVIEW` — `docs/specs/tactical-presets/` (FR-TP) — completing §6 steps 1–3 (`SPEC_INDEX.md`
> registry row added; RESERVED entry retired). The promoted spec resolves this note's §4 open
> questions (Q1 → compose-only, Q2 → gate-not-clock, Q3 → explicit deferral, Q4 → UI deferral) and
> supersedes this note where they deviate. Also: §6's "Specification Before Code" citation
> corrected root `CLAUDE.md` → `README.md` (the heading lives in README.md; verified by grep).)
> **Prior:** v0.4 — AR-2 fix: KD-2's event-triggered re-evaluation claim
> corrected after grepping `src/match-engine/MatchEngine.cs` — `GoalAwardedEvent`/`CardIssuedEvent`/
> `SubstitutionEvent` are all declared in Event System #17's catalogue but NONE has an actual
> producer yet (only `PossessionChangedEvent` does), not just the red-card/substitution pair as
> v0.3 claimed; all event-triggered decision points are deferred, not just two of three.)
> **Prior:** v0.3 — new §6 Implementation Plan: the spec-promotion pipeline for candidate #26
> followed by the T0–T4 code-landing sequence, plus an explicit deferral note for opponent-aware
> adaptation and the on-disk preset format.
> **Prior:** v0.2 — AR-1 fix: KD-1 and the §3 T-phase table now distinguish boot-time preset
> application (via `TeamTacticConfigApplier`/`PlayerTacticConfigApplier`, scoped to pre-kickoff
> per their own doc comments) from mid-match AI-manager adaptation, which must call
> `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly.
>
> **Status:** DESIGN SUPPLEMENT (forward-looking; **NOT** a formal approved spec, **NOT** yet
> implemented). Parallel in status to the pre-promotion `tactical-instruction-layer-design.md`
> (→ Spec #21). No code is authored from this note until it is reviewed and promoted.
> **Author:** —
> **Purpose:** Scope the "game model / AI-manager tactics" item from the July 7, 2026
> tactical-theory cross-reference: the `TeamTacticConfig`/`PlayerTacticConfig` substrate
> (Spec #21) already exists and is parser-swap-ready and default-identity-safe — what's missing
> is (a) a **preset library** layer on top of it, and (b) **AI-manager selection/adaptation
> logic** that chooses and adjusts a tactic without a human setting it. Given the existing seam,
> this is scoped as one natural next spec (candidate #26), not a re-architecture.

---

## 0. Why this is one spec, not two

Unlike the three items in the sibling note (`advanced-positional-behaviors-design.md`), the two
halves here share one substrate and one consumer path — both halves produce a `TeamTacticConfig`
/ `PlayerTacticConfig` that already flows through the existing `TeamTacticConfigApplier` /
`PlayerTacticConfigApplier` → `MatchEngine.SetTeamTactic` / `SetPlayerTactic` seam (landed
2026-06-29/06-30). The preset library is data; the AI-manager is the logic that picks from that
data. They are staged as T-phases of one spec (mirroring #21's own T0/T2/T3 staging), not split
into two numbered specs.

---

## 1. What already exists vs. what this adds

| Concern | Existing construct | This note adds |
|---|---|---|
| Team tactic value type | `TeamTactic` (readonly struct; Spec #21) | — (consumed as-is) |
| Per-agent tactic value type | `PlayerTactic` (readonly struct; Spec #21) | — (consumed as-is) |
| In-code config source | `TeamTacticConfig` / `PlayerTacticConfig` (Default = Balanced/Identity per team/agent; Spec #21 T2) | A **named preset catalogue** — `TacticPreset { Name, TeamTactic, PlayerTactic[]? }` — as a data layer above the single-config-per-match shape |
| On-disk human-authoring format | `TeamTacticFileLoader` / `PlayerTacticFileLoader` (Stage-0 `key=value` grammar; D1-deferred richer format) | A parallel **preset file format** (or an extension of the existing grammar with a `[preset NAME]` section) — Stage 0+1, same D1 deferral precedent |
| Runtime activation | `MatchEngine.SetTeamTactic` / `SetPlayerTactic` (staged → committed at AI-stride boundary, FR-TI-027) | — (the preset/manager layer calls these; no new activation seam needed) |
| Decision cadence | 10 Hz AI stride (`TacticalContext`), day-tick (`WorldClock`, Living World #22) | A **new, coarser manager-decision cadence** — see KD-2 |
| In-match tactical adaptation | None — a set tactic is static until a human calls `SetTeamTactic` again | **AI-manager adaptation logic**: score-state / time-remaining / opponent-tactic-aware re-selection |

---

## 2. Architectural decisions (candidate KDs)

**KD-1 — Preset layer is purely additive data; zero new runtime seam, with one boot-vs-mid-match
distinction to carry forward correctly.** A `TacticPreset` is a named, immutable bundle of one
`TeamTactic` + an optional `PlayerTactic[]`. `TacticPresetLibrary` is a static in-code catalogue
(Stage 0 precedent: `TeamTacticConfig.Default`, `PlayerTacticConfig.Identity`) mapping preset
names to bundles. **Applying a preset at kickoff** means calling the *existing*
`TeamTacticConfigApplier.Apply` / `PlayerTacticConfigApplier.Apply` with a config built from the
preset — no new match-engine writer needed for that path. Its own doc comment scopes it to
**"before kickoff"** (it stages via `SetTeamTactic`, which is designed to be called pre-match so
the first-stride commit lands before any tick runs); **applying/switching a preset mid-match**
(the AI-manager adaptation case, §3 T4) must instead call `MatchEngine.SetTeamTactic` /
`SetPlayerTactic` directly — the applier classes are not the right seam there, they are
boot-time-only conveniences over the same underlying call. Either way, no snapshot-schema change
beyond what Spec #21 already serializes (the applied `TeamTactic`/`PlayerTactic` values, not which
preset produced them — a preset name is authoring metadata, not simulation state, the same
distinction the project draws between the on-disk tactic-file grammar and the digested values).

**KD-2 — AI-manager decisions run on their own cadence, not every AI stride.** A manager
re-evaluating its tactic every 10 Hz tick would be both wasteful and unrealistic (real managers
don't re-tactic every 100 ms). Candidate decision points, by analogy with existing project
cadence separations (`MatchClock` 60 Hz physics / 10 Hz AI stride; `WorldClock` one tick = one
calendar day, Living World #22 KD-4): kickoff, half-time, and a configurable fixed interval (e.g.
every N match-minutes) — all three are pure `MatchClock`-tick-count derivations, available today.
Event-triggered re-evaluation (goal scored, red card, injury substitution) is **NOT** available
yet at any tier: `GoalAwardedEvent` / `CardIssuedEvent` / `SubstitutionEvent` are all declared in
Event System #17's Appendix A catalogue, but grepping `src/match-engine/MatchEngine.cs` shows only
`PossessionChangedEvent` has an actual producer — no goal-detection, disciplinary, or substitution
logic exists in the match engine yet to fire the other three. Event-triggered re-evaluation is
therefore out of scope for the first version, matching the project's phantom-interface avoidance
rule, until whichever of those producers lands first (goal-scoring detection is the most likely
near-term candidate, being a pure function of ball-in-goal geometry Ball Physics #1 already has).
A new `ManagerDecisionClock` (or a simple tick-count gate inside the match-engine composition
root, mirroring the existing `IsAiStrideTick` pattern) is the natural seam for the three
tick-count-derived triggers — not a new 10 Hz consumer.

**KD-3 — Default-neutral: no manager AI, no behaviour change.** A match with the AI-manager
layer disabled (the Stage-0 default) must be byte-identical to today — `SetTeamTactic` is called
at most once per team at boot (as it is today via `TeamTacticConfigApplier`), never again
mid-match. This mirrors the #21/#22 "default identity" precedent applied at the whole-subsystem
level, not just a single tactic dial.

**KD-4 — Manager AI reads only match-observable state, never the opponent's private
`PlayerTactic`.** The scoring function that picks/adjusts a preset may consume: current score
differential, time remaining, own team's current preset, and the opponent's team-level *tactic
category* if and only if that category is itself something the real world would expose (e.g. an
observable pattern of behaviour, not raw enum reads of the opponent's private struct). This is
the same perception-boundary principle as KD-5 in the sibling positional-behaviors note, applied
at the team-management level instead of the per-agent level. At Stage 0+1, the simplest
compliant version reads only its own team's state + the public match score/clock — no opponent
modeling at all; opponent-aware adaptation is a candidate Stage-2+ deferral (§4).

**KD-5 — On-disk preset format is Stage 0+1, not Stage 0.** Following the `TeamTacticFileLoader`
/ `PlayerTacticFileLoader` / `ScenarioIndex` D1 precedent: Stage 0 authors the preset catalogue
in code; the Stage 0+1 disk format (`[GT]` config-loader, FR-CS-019) is a pure parser swap that
produces the same `TacticPreset` catalogue and feeds the same applier path unchanged.

**KD-6 — Preset selection is not the same problem as balance-pass tuning.** The `[GT]` magnitude
values inside each preset's `TeamTactic`/`PlayerTactic` are the existing #21 §5.6/G2 balance-pass
concern (already pinned per the June 30, 2026 landing). This spec's concern is *which* preset a
manager AI picks and *when* it switches — orthogonal to the magnitudes themselves.

---

## 3. Staged scope (candidate T-phases, mirroring #21's own T0/T2/T3 pattern)

| Phase | Scope | Depends on |
|---|---|---|
| T0 | `TacticPreset` value type + `TacticPresetLibrary` in-code catalogue (a handful of named presets: e.g. Balanced, Gegenpress, Park-the-Bus, Possession, Counter-Attack) | Spec #21 `TeamTactic`/`PlayerTactic` (existing) |
| T1 | Preset → `TeamTacticConfig`/`PlayerTacticConfig` projection + boot-time wiring through the existing `TeamTacticConfigApplier`/`PlayerTacticConfigApplier` (pre-kickoff only, per KD-1) | T0 |
| T2 | `ManagerDecisionClock` / coarse-cadence gate in the match-engine composition root | Spec #16 `MatchClock` pattern (existing) |
| T3 | Kickoff preset-selection scoring function (own-state-only per KD-4), applied via T1's boot-time path | T1, T2 |
| T4 | In-match adaptation triggers (score-differential-driven re-selection at fixed intervals) — calls `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly, NOT the boot-only appliers (KD-1) | T3 |
| (deferred) | Opponent-tactic-aware adaptation; on-disk preset file format | KD-4 deferral; KD-5 |

---

## 4. Open questions

1. How many presets belong in the Stage-0+1 in-code catalogue, and do they need lead-developer
   sign-off on their `[GT]` magnitudes independent of the #21 balance pass, or do they simply
   compose already-pinned #21 values? (Current lean: compose only — a preset is a *named point*
   in the existing #21 parameter space, not a new tunable surface.)
2. Should `ManagerDecisionClock` be its own file (parallel to `WorldClock`), or is a per-team
   tick-count field on the match-engine composition root sufficient at Stage 0+1? Precedent
   (`IsAiStrideTick` as a `MatchClock` property) favors the lighter-weight option first.
3. Is opponent-aware adaptation (KD-4 deferral) in scope for the first version of this spec at
   all, or should the spec explicitly defer it to a numbered follow-up the way Living World #22
   defers `BackgroundTierSim`? Current lean: defer explicitly — building a consumer for
   opponent-tactic modeling that doesn't exist yet would be the same phantom-interface class
   FR-LW-031 / root `CLAUDE.md` "Interface Design Principle" already forbids elsewhere.
4. Does a human-managed team ever need to *see* which preset an AI-managed opponent is running
   (e.g., for in-match scouting/UI)? That's a Stage 1+ UI-layer question, out of scope for this
   spec's on-pitch/simulation-layer content.

---

## 5. Candidate spec number (reserved, not yet promoted)

Per `SPEC_INDEX.md` "Before creating a new spec folder, add the entry here first" — reserved here
to prevent a future renumbering collision, **not** added to the registry table until promoted to
section files (the #21/#22 precedent).

| Candidate # | Working title | Folder (reserved, not yet created) |
|---|---|---|
| 26 | Tactical Presets & AI-Manager Selection | `tactical-presets/` |

See the sibling note `advanced-positional-behaviors-design.md` for candidates #23–#25
(dismarking, build-up structures, positional rotations), reserved separately — different
substrate (on-pitch per-agent AI, not the tactic-config layer).

---

## 6. Implementation plan

Per `README.md` "Specification Before Code" — no `src/` code lands from this note directly.
This section is the promotion pipeline for candidate #26, then the T0–T4 code-landing sequence
from §3 once it is `APPROVED`.

**Spec-promotion pipeline:**

1. **Outline.** Author `docs/specs/tactical-presets/outline.md` (or `outline-detailed.md`
   directly) from this note. FR prefix `FR-TP-*` — verified by grepping `docs/specs/**/*.md` for
   existing `FR-[A-Z]+-` prefixes that `TP` is unused (existing prefixes found: AT, CS, DA, DS,
   EVT, GK, HE, LW, PA, PO, PR, TI, TS); re-grep before final assignment in case it was claimed
   since this note.
2. **Promote `SPEC_INDEX.md`.** Move row 26 from "RESERVED" into the registry at `NOT STARTED` /
   `IN PROGRESS`.
3. **Section files.** Full 9-section template + appendices. §1 MUST cite Spec #21 as the hard
   dependency (this spec is additive on top of `TeamTactic`/`PlayerTactic`, not a redesign) and
   MUST cite this note's KD-4 (no reading an opponent's private `PlayerTactic`) as its own
   perception-boundary-equivalent invariant. This note's §4 open questions MUST be resolved to
   concrete decisions before `APPROVED` — in particular open question 1 (how many presets need
   independent sign-off) directly gates whether §9's approval checklist needs a numeric-value
   review step or just a shape/reference review.
4. **PASS-1 adversarial review** + fix pass; **PASS-2** if PASS-1 found High-severity issues,
   repeating until a clean or Low-only pass, per the #17/#18/#19/#21 precedent.
5. **Lead-developer R-01..R-05 sign-off → `APPROVED`.** Update `SPEC_INDEX.md`, `PROGRESS.md`,
   `README.md`.
6. **T0.** `TacticPreset` (readonly struct: `Name`, `TeamTactic`, `PlayerTactic[]?`) +
   `TacticPresetLibrary` (static catalogue) — placement decision to make explicit in §4: fold into
   `src/tactical-instructions/` (it is a pure data layer over that assembly's own types, and
   creating a whole new assembly for a handful of named bundles would be disproportionate — the
   same "does this need its own assembly" judgment call flagged for #23/#24/#25) unless the
   eventual spec's §4 finds a concrete reason to split it out.
7. **T1.** Wire `TacticPreset` → `TeamTacticConfig`/`PlayerTacticConfig` projection; a boot-time
   caller builds a config from a chosen preset and passes it to the *existing*
   `TeamTacticConfigApplier.Apply`/`PlayerTacticConfigApplier.Apply` — no new applier code, per
   KD-1 (as corrected in AR-1 above, this path is pre-kickoff only).
8. **T2.** Resolve open question 2 concretely (a `ManagerDecisionClock` file vs. a lighter
   per-team tick-count field) and land whichever the spec's §4 settles on in the match-engine
   composition root.
9. **T3.** Kickoff-time scoring function consuming only own-team state (KD-4's Stage-0+1-
   compliant floor) — this is genuinely new decision logic, so it gets its own §3/§5 test-plan
   treatment in the eventual spec, unlike the mostly-routing work in #23–#25.
10. **T4.** In-match adaptation — calls `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly per
    KD-1's AR-1-corrected boundary (§3 T4), on the cadence T2 establishes.
11. **Implementation-level AR cycle** on each landed T-phase, per the project's universal
    post-landing convention (mirrors the #21 T0/T2/T3 AR history).

**Explicitly out of scope for the first `APPROVED` version** (per open question 3): opponent-aware
adaptation and the on-disk preset file format. Both get their own follow-up entry when their
prerequisites exist (real opponent-modeling canon; the Stage-1 `[GT]` disk loader), exactly as
Living World #22 defers `BackgroundTierSim` rather than building a phantom consumer today.

**Definition of done for this plan's own scope:** satisfied once candidate #26 reaches step 2
(promoted out of `SPEC_INDEX.md` "RESERVED") — steps 3–11 are the substance of the eventual spec
and its implementation, not further design-supplement content.

---

## VERSION HISTORY

| Version | Date | Notes |
|---|---|---|
| 0.5 | 2026-07-08 | PROMOTED — #26 section files authored at `IN REVIEW` (§6 steps 1–3 complete); `SPEC_INDEX.md` registry row added, RESERVED entry retired; §4 open questions resolved in the spec (Q1 compose-only / Q2 gate / Q3 deferral / Q4 UI deferral). §6 citation fix: "Specification Before Code" is a `README.md` heading, not root `CLAUDE.md`. |
| 0.4 | 2026-07-07 | AR-2 fix (0H+0M+1L): KD-2 claimed goal-scored re-evaluation was available today with only red-card/substitution deferred; grepping `src/match-engine/MatchEngine.cs` shows zero producers for `GoalAwardedEvent`, `CardIssuedEvent`, or `SubstitutionEvent` (only `PossessionChangedEvent` is wired) — corrected to defer all three event-triggered points uniformly, with goal-scoring flagged as the likely first candidate once a producer lands. |
| 0.3 | 2026-07-07 | Added §6 Implementation Plan — spec-promotion pipeline for #26 + T0–T4 code-landing sequence + explicit deferral note. |
| 0.2 | 2026-07-07 | AR-1 fix (0H+0M+1L): KD-1 originally implied `TeamTacticConfigApplier.Apply`/`PlayerTacticConfigApplier.Apply` cover both boot-time and mid-match preset application; their own doc comments scope `Apply` to pre-kickoff only. Corrected KD-1 and the §3 T-phase table (T1 vs T4) to route mid-match adaptation through `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly instead. |
| 0.1 | 2026-07-07 | Initial creation — scoping note for a preset library over `TeamTacticConfig`/`PlayerTacticConfig` plus AI-manager selection/adaptation logic, per the July 7, 2026 tactical-theory cross-reference's "game model / AI-manager tactics" item. |
