# Lineup Selection (Plan-3) — Design Supplement

> **Created:** July 19, 2026
> **Status:** DESIGN SUPPLEMENT (pre-implementation — no code yet; self-adversarial-reviewed to
> convergence below). Extends the Squad/Player Data Layer (candidate spec **#27**,
> `docs/tracking/squad-player-data-design.md`) — it does not open a new candidate number.
> **Purpose:** Replace the Stage-0 **roster-order** lineup mapping in `MatchEngine.ConfigureSquads`
> (player `k` → on-pitch slot `k`; caller must pre-order so player 0 is the GK) with a **proper
> lineup selection** that picks the eleven starters + bench from a full club `Squad` and assigns
> each to its formation slot by position — closing the #27 remainder item the T1 landing left open
> ("lineup selection proper stays Plan-3", projection-design v0.4).

---

## 0. Scope

**In scope (buildable now):** a deterministic, pure lineup selection over a `Squad` and a
`FormationFamily` → the ordered eleven starter records + seven bench records `ConfigureSquads`
already consumes, plus the `PlayerPosition → RoleId`/slot mapping KD-4 deferred. This is the one
#27 remainder item with no upstream/host block.

**Explicitly out of scope, and why not buildable now (the paired future pieces the task names):**

- **Stage-1 persistence / transfers / aging.** `master-development-plan.md` §4.3/§4.4 places these
  at Stage 2, and #27 §0 already declares them out of scope ("a data layer only — not squad
  management, not a UI, not a transfer market"). They need the on-disk save-format root
  (`JSON-based for V1`, §4.6) that does not exist and a season/economy layer that is a separate
  body of work. Not a lineup-selection concern; recorded here only to close the task's enumeration.
- **§4.8.2 runtime MXCSR validation + the replay re-projection path (paired future piece).**
  Genuinely blocked twice over, so building either now would be a phantom (Interface Design
  Principle / FR-LW-031): (a) MXCSR validation is native float-mode interop with **no consumer** —
  `MatchEngine.cs` has no snapshot-deserialize/replay path (verified: no `Read`/`Deserialize`; the
  #27 T3 KD-T3-3 finding), so there is nothing to *validate against* at match start; (b) the
  distinct-squad restore re-projection (re-deriving the excluded per-slot attribute records from
  the serialized `_rosterClubId` + `_activeBenchSlot`) is gated on that same absent deserialize
  path. They are **correctly paired**: both unblock together the day a snapshot-deserialize/replay
  path lands, and neither should be built before it. This supplement changes nothing about that —
  it only records the pairing so the remainder is fully accounted for.

---

## 1. What exists vs. what this changes

`ConfigureSquads(homeSquad, awaySquad)` (MatchEngine.cs v1.37+) today:
`ApplySquad` copies `squad.GetPlayer(k).Attributes` into on-pitch slot `k` for
`k ∈ [0, PLAYERS_PER_TEAM)` and `squad.GetPlayer(PLAYERS_PER_TEAM + b)` into bench slot `b` — a
**positional trust** contract: the caller must have ordered the roster so index 0 is a goalkeeper
and 1..10 line up with `GetFormationSlots(STAGE0_FORMATION)` (GK, LB, CB, CB, RB, LM, CM, CM, RM,
ST, ST for F442). The `_isGoalkeeper[i] = k == 0` boot seed hardcodes slot 0 as GK, so a squad
whose player 0 is an outfielder silently mis-seeds.

This supplement inserts a pure **selection** step between the `Squad` and `ApplySquad`: it produces
an ordered `int[PLAYERS_PER_TEAM + SUBSTITUTES_PER_TEAM]` of **local squad indices** (starters
first, in formation-slot order, then bench), and `ApplySquad` indexes the squad through it. The
attribute-projection and bounds-gate machinery is unchanged — only the index mapping changes.

---

## 2. Key decisions

- **KD-L1 (the deferred KD-4 mapping — via `LineId`, not per-role).** Each formation slot already
  carries a `RoleId` and a `DefaultLine` (`FormationSlotRecord`). The coarse
  `PlayerPosition`→slot compatibility is defined on the slot's **`DefaultLine`** (the natural coarse
  axis), not its 13-value `RoleId`: `IsGoalkeeper` slot ⇒ `Goalkeeper`; else
  `Defense→Defender`, `Midfield→Midfielder`, `Attack→Forward`. This is the minimal mapping KD-4
  said "a future T-phase may define … not invented here" — now that squads are wired (T1), that
  T-phase is here. It lives in the **match-engine** (the consumer), not `player-database` (the
  bottom-of-graph data layer must stay ignorant of positioning-ai's `LineId`, preserving KD-4's
  "no shared type, no cross-reference"). `PlayerPosition` and `LineId` never reference each other;
  the match-engine bridges them.
- **KD-L2 (selection = per-line greedy by rating, deterministic).** For each formation slot in
  order, choose the highest-rated **not-yet-selected** squad player whose `PlayerPosition` matches
  the slot's required coarse position; tie-break by ascending `PlayerId` (stable, deterministic —
  no RNG, this is not a generation site). "Rating" = the arithmetic mean of the player's 31
  `[1,20]` attributes (`PlayerAttributes.ToArray()` average; `WeakFootRating`'s `[1,5]` scale is
  **excluded**, per KD-2). A coarse position-average, not a role-weighted overall — a role-weighted
  rating is a Stage-1 tuning concern, deliberately not invented here.
- **KD-L3 (fallback when a line is short — fail-loud vs. fill).** A valid Stage-0 club
  (`RosterGenerator`, uniform over 4 positions across ≥ 18 players) may still, by draw, lack e.g. a
  second goalkeeper for the bench or enough forwards for two ST slots. **Starters: fail loud** — if
  no unselected player matches a *starter* slot's required position, `ConfigureSquads` throws
  (the FR-TP-014-class gate at the consuming seam; a match cannot kick off with an empty slot).
  **Bench: fill by best-remaining** — the seven bench slots are position-agnostic (a bench is a
  pool, not a shape), filled by highest rating among all unselected players, tie-break `PlayerId`.
  This keeps the *pitch* shape-correct and fail-loud while never refusing a legal 18+ squad for
  bench composition.
- **KD-L4 (GK identity flows from selection, not `k == 0`).** The boot `_isGoalkeeper[i] = k == 0`
  seed is replaced: after selection, `_isGoalkeeper[teamId*11 + slot]` is set from the selected
  slot's `FormationSlotRecord.IsGoalkeeper`, and each bench slot's `_benchIsGoalkeeper` from the
  selected bench player's `PlayerPosition == Goalkeeper`. This removes the silent mis-seed and is
  the reason selection must run **inside** `ConfigureSquads` (it owns both the attribute copy and
  the GK flags). A match with **no** `ConfigureSquads` call keeps the existing `k == 0` boot seed
  unchanged — behaviour-neutral for the unconfigured path (KD-P7 default path stays byte-identical).
- **KD-L5 (behaviour vs. a pre-ordered squad).** For a squad already ordered exactly as
  `GetFormationSlots` expects (player 0 a GK, 1..10 matching lines, best-rated first), selection
  reproduces the current roster-order mapping — so existing `MatchEngineSquadTests` fixtures that
  hand-order their squads stay green **iff** they are position-coherent; any fixture relying on
  index-0-is-GK without a GK there was already latent-buggy and this surfaces it (a correctness
  win, not a regression). Selection is **not** claimed byte-identical to roster-order in general —
  that is the point of "proper" selection (the #27 T1 divergence-by-design precedent).
- **KD-L6 (pure + testable without a match boot).** Selection is a `static` pure function
  `LineupSelector.Select(in Squad, FormationFamily) → LineupPlan` (the index arrays + per-slot
  GK flags), unit-testable directly — the `RosterGenerator`-stateless / `PlayerAttributeProjection`
  precedent. `ConfigureSquads` calls it, then validates + applies through the selected indices.

---

## 3. New / changed surface (T-phase plan — implemented after this converges)

- **New `src/match-engine/LineupSelector.cs`** (`static`): `Select(in Squad, FormationFamily)
  → LineupPlan`. `LineupPlan` = `int[11] StarterLocalIndices` + `int[7] BenchLocalIndices` +
  `bool[11] StarterIsGoalkeeper` + `bool[7] BenchIsGoalkeeper`. Pure; the KD-L1 `LineId →
  PlayerPosition` bridge is a private helper here (match-engine owns the cross-layer join).
  Rating helper `MeanAttribute(in PlayerAttributes)` (the 31-field `[1,20]` average).
- **Changed `MatchEngine.ConfigureSquads` / `ApplySquad` / `ValidateSquad`:** run `Select` first;
  `ValidateSquad` still bounds-gates every **consumed** record (now the selected 18, indexed
  through the plan, not the prefix `0..17`); `ApplySquad` copies via `StarterLocalIndices[k]` /
  `BenchLocalIndices[b]`; set `_isGoalkeeper` / `_benchIsGoalkeeper` from the plan (KD-L4). The
  starter-slot-unfilled fail-loud (KD-L3) is a new `ArgumentException` at the consuming seam.
  **No `SNAPSHOT_SCHEMA_VERSION` bump** — selection changes *which* boot-constant records seed each
  slot, not the serialized surface (the excluded-attrs proof and the T3 `_rosterClubId` reference
  are untouched; the roster reference already records identity, and restore re-projection stays the
  paired future piece §0).
- **Tests:** new `LineupSelectorTests` (per-line greedy-by-rating pick + `PlayerId` tie-break;
  KD-L1 mapping exactness for all three families; starter fail-loud when a line is short; bench
  best-remaining fill; pre-ordered-coherent squad reproduces roster order, KD-L5; two-call
  determinism). `MatchEngineSquadTests` extended: a deliberately **mis-ordered** distinct squad
  (GK at index 7) now seeds the GK slot correctly (KD-L4) and the unconfigured/default path stays
  byte-identical (KD-P7 re-lock).

---

## 4. Adversarial review (self-review)

**AR-1 (0 H · 2 M · 1 L — all folded in above):**
- **M-1 — `SUBSTITUTES_PER_TEAM` is 7, not the 18-man `PLAYERS_PER_TEAM+SUBSTITUTES` I first wrote
  as "7-man bench" without checking.** Verified: `MatchEngineConstants.PLAYERS_PER_TEAM = 11`,
  `SUBSTITUTES_PER_TEAM = 7` ⇒ 18 consumed, and `ValidateSquad` already requires `Count ≥ 18`.
  `CLUB_SQUAD_SIZE = 25`, so a full club has 7 u/nselected players beyond the 18 — selection must
  draw the 18 from up to 25, not assume exactly 18 (the earlier "consumed prefix" framing). §3
  corrected: `Select` ranges over `squad.Count`, not `[0,18)`.
- **M-2 — original KD-L1 draft keyed compatibility on `RoleId`, which would need a 13-way table and
  drag positioning-ai's role taxonomy into the coarse 4-value mapping** (re-introducing exactly the
  cross-layer coupling KD-4 forbade). Re-based on `DefaultLine` (3 lines + GK flag = the coarse axis
  that already matches `PlayerPosition`'s cardinality). Simpler, and keeps `player-database` free of
  any positioning-ai reference.
- **L-1 — determinism:** confirmed selection draws **no** RNG (it is ordering, not generation), so
  no `DeterministicRngService` stream, no domain-tag/ordinal allocation, no snapshot state — unlike
  `RosterGenerator`. Tie-break on `PlayerId` (already club-scoped-unique, KD-3) gives a total order,
  so two calls on the same `Squad` are byte-identical without any RNG plumbing.

**AR-2 (0 H · 0 M · 0 L) — CONVERGENCE.** Re-checked: the KD-L4 GK-flag path is the sole writer of
`_isGoalkeeper` on the configured path and leaves the unconfigured `k == 0` boot seed untouched
(default path byte-identical); no `SNAPSHOT_SCHEMA_VERSION` interaction (selection is boot-constant
identity, the same class as T1/T3); the §0 future-piece pairing (MXCSR + replay re-projection) is
correctly gated on the absent snapshot-deserialize path and is not touched here. No further findings.

---

## Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-19 | Initial draft + AR-1 (2 M + 1 L, folded in) + AR-2 (clean, CONVERGED). Opens lineup selection (Plan-3) under #27; scopes Stage-1 persistence/transfers/aging and the §4.8.2-MXCSR + replay-re-projection pair as future pieces gated on a snapshot-deserialize path. |
