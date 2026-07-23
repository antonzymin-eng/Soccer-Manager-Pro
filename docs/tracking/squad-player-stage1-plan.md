# Squad / Player #27 — Stage-1+ Deferrals Implementation Plan

> **Created:** July 23, 2026
> **Status:** PLAN (coordination/sequencing — no section files, no `SPEC_INDEX.md` row of its own).
> This plan does **not** author a new spec; it governs *completing the three Stage-1+ items the #27
> data-layer deliberately deferred* (`squad-player-data-design.md` §4 last row + §0), by mapping each
> to the roadmap spec that now owns it and pinning the sequence + cross-spec contracts.
> **Master-plan home:** §4.2 (squad management) → §4.3 (aging/transfers) / §4.6 (save format).
> **Governed by:** `management-layer-spec-roadmap.md` + `spec-plans/` (candidate #27–#50).
> **Purpose:** A single #27-anchored view of the three deferred items — **on-disk save-format squad
> persistence, transfer market, aging** — showing what is already owned/approved elsewhere, what
> genuinely remains #27-scoped, and the wave order to implement them without a phantom or a
> save-format collision.

---

## 0. Why this plan exists (and what it deliberately is not)

The #27 Squad/Player data layer (APPROVED July 22, 2026 — `docs/specs/squad-player-data/`) closed
its Stage-0/1 scope: a canonical `PlayerAttributes` record, deterministic `RosterGenerator`, text
import, `ConfigureSquads` + `LineupSelector` wiring, and the snapshot roster-reference (#27 T3). Its
§4 explicitly deferred three Stage-1+/Stage-2 items to their own passes:

> **Stage-1+** — on-disk save-format squad persistence, transfer market, aging/training (master plan
> §4.3/§4.4, explicitly out of scope per §0).

Since then, the **management-layer roadmap** (`management-layer-spec-roadmap.md`, July 22, 2026)
decomposed all Stage-1→5 off-pitch features into candidate specs #27–#50 and **assigned owners** for
each of these three items. This plan is therefore **not** a re-plan of those specs — each has its own
high-level plan (`spec-plans/`) and, where promoted, its own design supplement + section files. It is
the **coordination layer**: the mapping, the sequence, the cross-spec contracts that keep #27's
canonical record frozen while the deferred behaviour lands on top of it, and the residual sliver that
is still genuinely #27-owned.

**This plan does not itself introduce a spec number, reserve a determinism tag, or bump a save
version.** Those actions land in the owning specs at their own promotion/implementation.

---

## 1. The mapping — where each deferral now lives

| #27 §4 deferral | Now owned by | Spec status (Jul 23, 2026) | Determinism | Save impact |
|---|---|---|---|---|
| **Aging** (age advance, decline, growth, retirement, regens) | **#28 Player Progression & Lifecycle** (`FR-PG`) | **APPROVED** — supplement + 11 section files + R-01..R-05 signed | `0x20` / ord 82 (promoted at approval, ERR-028-001) | `PROGRESSION_SAVE_FORMAT_VERSION` sub-blob composed by #30 root |
| **On-disk save-format squad persistence** (the *evolving/career-state* roster) | **#30 Season & Competition Loop** (composition root) + **#28** (career-state serialize) | **#30 APPROVED**; **#28 APPROVED** | #30 `0x22` / 84; #28 `0x20` / 82 | `SEASON_SAVE_FORMAT_VERSION` bump (#30 season block); #28 sub-blob composed into it |
| **On-disk save-format squad persistence** (the *initial/reference/shipped* roster) | **#27 residual → #47 New-Game Setup & DB Editor** | **#27 residual open; #47 PLAN** (Wave 7) | none (import, not a draw) | Not a save-version item — a load-time *source* (shipped starting DB, #47's format call), distinct from the season save format |
| **Transfer market** (windows, bids, contracts, negotiation) | **#31 Transfers, Contracts & Negotiation** (`FR-TX`) | **PLAN** (Wave 4; pre-supplement) | `0x23` / 85 (proposed) | Season + world sub-blobs (contracts durable; window/negotiation season-scoped) |
| *(adjacent, roadmap-split)* transfer **budgets/wages** constraint | **#40 Club Finances & Economy** (`FR-FN`) | PLAN (Wave 2) | `0x29` / 91 *(roadmap §6 proposed — no #16 catalogue row yet)* | world-state economy block |
| *(adjacent, roadmap-split)* **training** growth input | **#29 Training System** (`FR-TR`) | **APPROVED** | none — fully deterministic; `_RESERVED_0x21_`/83 held but **not** promoted (ERR-029-001, draws nothing) | writes #28's `TrainingInput` seam |

**Key structural fact:** the roadmap chose to keep **#27's canonical `PlayerAttributes` / `PlayerRecord`
struct frozen** (KD-4 of #28) — no CA/PA fields, no #27 record schema ripple. The *evolving* roster
lives in a **#28-owned career-state block** (serialize-don't-regenerate, the #30 KD-5 posture), and the
*initial/reference* roster stays a #27/#47 load-time source. This is the load-bearing contract this
plan exists to protect: **one canonical record shape (#27), many owners of state over it (#28
career-state, #31 contract state, #40 economy state)** — never a second attribute struct, never a
#27 version bump to add lifecycle fields.

---

## 2. What is already done vs. what remains

### 2.1 Aging — **design complete, implementation gated on #30**

#28 is APPROVED end-to-end at the spec level. Its §10 T-phase plan is:

- **T0** — lifecycle value types + `GrowthProjection` (§4.3 identity, `curveEnabled` off) +
  `RegenGenerator`, behaviour-neutral. *Buildable now* (depends only on #27 + #16, both landed).
- **T1** — the `PROGRESSION_SAVE_FORMAT_VERSION` block + season-save composition. *Gated on #30's
  season-save extension existing (the root that composes it).*
- **T2** — `AdvanceDay` / `RunSeasonBoundary` wired at #30's reserved day-advance + season-boundary
  seams. **Requires #30 implemented first** (#28 §10 L-2 / Wave-1 → Wave-2 ordering — wiring T2 before
  #30's seam code exists would bind against a phantom).
- **T3** — the deep CA/PA curve dial + #29 training-input consumption. (#29 spec is APPROVED; its
  producer half lands with #29's own T-phases.)

**Remaining work = implementation only**, in the order T0 (now) → [#30 spine] → T1/T2 → T3.

### 2.2 Persistence — **two distinct halves, don't conflate them**

The word "persistence" in #27 §4 hides two different artifacts the roadmap deliberately separates:

1. **Career-state roster persistence** (the roster as it *evolves* — aged attributes, CA/PA, growth
   cursor, retirement, transferred-in players). **Owned by #28** (serializes the complete career-state
   `PlayerRecord` set under `PROGRESSION_SAVE_FORMAT_VERSION`) and **composed by #30** into the season
   save via `SeasonSaveCodec`'s opaque-sub-blob pattern. **Design complete; lands with #28 T1 + #30.**
2. **Initial / reference / shipped-database roster** (the "new game" starting world — which clubs and
   players exist before any career state accumulates). **This is the genuine #27 residual.** #28 §4
   (KD-4) explicitly hands it off: *"A future shipped-database / on-disk-roster pass (#47 / a #27
   Stage-1+ deliverable) supplies the initial roster; #28 remains the owner of the career-state
   roster…"* Today
   the initial roster is a **boot-time `RosterGenerator` draw or a per-squad `SquadFileLoader` text
   import** — there is no shipped, editable, full-world on-disk *database* format yet; that format
   decision is #47's (master plan §4.2 squad management / the DB-editor pass), separate from the #30
   season save format.

So the only persistence work still needing a **new** owner/design is the **initial-roster on-disk
database format + editor** — and the roadmap already parks it at **#47 New-Game Setup & DB Editor**
(Wave 7). Nothing else about persistence is unassigned.

### 2.3 Transfers — **needs a design supplement (earliest genuinely-new design work here)**

#31 is still a one-page PLAN. It is the only one of the three deferrals not yet through a design
supplement. It is Wave 4 and its critical path runs `#33 → #31` (counterparty psychology) with hard
reads on **#40** (budget/wage constraint) and **#30** (window calendar + season save). Authoring it
before #40/#33 exist would phantom (its own §9 risk). So transfers is correctly **last** of the three.

---

## 3. Implementation sequence (wave order, #27-anchored)

This is a slice of the roadmap's critical path `#27 → #30 → #33 → #31 → #38`, annotated with the
#27-deferral each wave discharges:

1. **Now (no new dependency):** #28 **T0** — lifecycle types + `GrowthProjection` §4.3 identity +
   `RegenGenerator`, behaviour-neutral, draw-free except regen. Discharges the *pure-projection* half
   of **aging**. Two-run + `curveEnabled`-off digest locks (#28 §8).
2. **Wave 1 — #30 the spine:** implement the Season & Competition Loop — day-advance loop, league/
   fixtures/table/calendar, and the **`SEASON_SAVE_FORMAT_VERSION` bump** that extends the season save
   from "world + optional match" to "world + season-state + optional match." This is the composition
   root every later persistence blob attaches to.
3. **Wave 2 — #28 T1/T2 (persistence + wiring) + #29:** #28 **T1** adds its
   `PROGRESSION_SAVE_FORMAT_VERSION` sub-blob, composed into #30's season save (the **career-state
   roster persistence** half of the persistence deferral). #28 **T2** wires `AdvanceDay` /
   `RunSeasonBoundary` at #30's reserved seams (the aging *behaviour* goes live). #29 training writes
   the neutral-defaulted `TrainingInput` seam #28 already owns. **#40** economy lands here too (the
   budget/wage constraint #31 will read).
4. **Wave 3 — #33** personalities/morale (the #22 producer; gates #31's deep valuation).
5. **Wave 4 — #31 transfers:** open the design supplement → AR to convergence → section files → wire.
   Stage-2 minimal = deterministic single-counterparty valuation inside a summer window (the master
   plan §4.3 accept/reject identity); Stage-3 = agents/clauses/loans modulating the same valuation.
   Transfer/contract state lands as season + world sub-blobs. Discharges the **transfer market**
   deferral.
6. **Wave 7 — #47** new-game setup + DB editor: the on-disk **initial/reference** roster database
   format + editor. Discharges the last **persistence** sliver (§2.2 item 2). Can be pulled earlier if
   a shipped starting database is needed before the editor UI, but the format decision is #47's.

**Net:** aging is buildable immediately (T0) and behaviourally live by Wave 2; career-state
persistence is Wave 2; transfers are Wave 4; the shipped initial-DB format is Wave 7 (or pulled
forward as a format-only pass). No item is blocked on undesigned work except #31 (correctly last) and
the #47 residual.

---

## 4. Cross-spec contracts this plan protects

These are the invariants that keep the three deferrals from rippling back into #27's frozen surface.
Each is already pinned in an owning spec; listed here so a future implementer sees them in one place.

- **C-1 — #27's canonical struct is frozen.** No CA/PA fields, no lifecycle fields, no #27 record
  version bump. Aging/transfers/economy own *state over* the record, never fields *on* it (#28 KD-4).
- **C-2 — one attribute-mutation writer.** `ProgressionEngine.GrowthProjection` is the sole mutator of
  a managed player's `[1,20]` attributes. Training (#29) and any transfer-driven change feed it as
  **inputs**, never parallel mutations (#28 KD-2/KD-7). Prevents double-counting.
- **C-3 — serialize the career-state roster, don't regenerate it on load.** Generator-version drift
  makes regeneration-on-load fragile (#30 KD-5), so the evolving roster is *serialized* by #28. The
  *initial* roster (#27/#47) is the only thing generated/imported.
- **C-4 — the season-save root is the only assembly that sees both sub-blobs.** #30 owns
  `SeasonSaveManager`/`SeasonSaveCodec`; #28/#31/#40 hand it **opaque, independently version-gated**
  sub-blobs it never parses (the `WorldStore`/`MatchSaveCodec`/`SeasonSaveCodec` precedent). No spec
  reaches into another's blob internals.
- **C-5 — save-format version sequencing.** Multiple specs bump `SEASON_SAVE_FORMAT_VERSION` (#30
  1→2; #28 2→3 if it lands second; #31 next). Whoever lands second rebases on the other's frame layout
  (#30 §9 + #28 §9 ordering notes). Each **inner** block carries its own version, so only the **outer**
  frame bump needs sequencing — the collision surface is one integer, guarded by fail-loud gates.
- **C-6 — command-seam discipline for the UI.** Transfers/lifecycle expose public command APIs
  (`SetTeamTactic`-style); #38 UI drives them, never mutates state directly (#31 §7, #28 KD-7).
- **C-7 — determinism band is contiguous; know what is actually reserved vs. merely proposed.**
  Off-pitch allocations fall in three states — do not treat them as interchangeable:
  - **In #16's §3.4 catalogue:** #28 `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` / ord 82 (**promoted** at
    #28 approval, ERR-028-001 — covers regen generation *only*; the const + per-club stream register at
    #28 T2, the first regen); #29 `_RESERVED_0x21_` / ord 83 (a **placeholder deliberately NOT
    promoted** — #29 is fully deterministic and registers no stream, ERR-029-001); #30
    `DOMAIN_TAG_SEASON_LOOP = 0x22` / ord 84 (reserved at #30 approval; const + stream at #30 T2).
  - **Roadmap §6 proposed, no catalogue row yet:** #31 `0x23` / 85, #40 `0x29` / 91 — these are
    *proposed* in `management-layer-spec-roadmap.md §6` ("to be pinned in Deterministic Simulation")
    and must be allocated in #16 §3.4 **at each spec's promotion**, not assumed reserved now.
  - **Rule:** register a stream only at its first *draw site*, never earlier (the `world.arcs`
    phantom-surface rule, FR-LW-031). Aging is draw-free except regen; #29 draws nothing; the minimal
    transfer valuation is draw-free — so most of this band stays dormant until a deep tier lands.

---

## 5. Test focus (per deferral, at implementation time)

Grounded in each owning spec's §8; consolidated so the #27-deferral acceptance is legible:

- **Aging (#28):** `curveEnabled`-off == literal §4.3 step, byte-for-byte (behaviour-neutral identity);
  two-run multi-season projection byte-identical from one seed (draw-free aging half); mid-year
  save→restore byte-exact (integer fixed-point cursor, year-rollover fires exactly once, no
  double-count); regen determinism (same seed+club → same newgen, fresh `PlayerId`); retirement
  contract (flagged mid-season, `Squad` mutation only at the boundary).
- **Career-state persistence (#28 T1 + #30):** save→restore round-trip field-identity for the
  progression sub-blob through the composed season save (world + season + progression + optional
  match); fail-loud on bad `PROGRESSION_SAVE_FORMAT_VERSION` / out-of-bounds length prefix / trailing
  bytes; `|lifecycle block| == |managed roster|` (no per-season blob growth).
- **Transfers (#31):** #33-unconfigured negotiation reproduces the deterministic §4.3 valuation
  exactly; full-window two-run determinism from a fixed world seed; mid-negotiation + mid-window
  save→restore; fail-loud on over-budget bid / malformed contract / action outside a window / bid on a
  player not in #27's pool.
- **Initial-DB residual (#27/#47):** round-trip of the on-disk initial-roster format ↔ `Squad`;
  parity with `RosterGenerator`/`SquadFileLoader` semantics (omitted field ⇒ documented default); the
  format is a **load-time source**, not a determinism-pinned wire format (only the resulting
  `PlayerRecord` values matter — the `SquadFileLoader`/tactic-file-loader posture).

A `#19 ScenarioRunner` capstone (`multi-season-aging`: build a roster, advance N seasons through the
real #30 loop, assert aged state + a determinism digest) is the natural end-to-end lock once #28 T2 is
wired — the match-engine-capstone precedent.

---

## 6. Open questions / risks

- **R-1 — the initial-DB residual has no design supplement yet.** It is the one persistence sliver not
  covered by an approved spec's implementation. It is parked at #47 (Wave 7); if a shipped starting
  database is needed before the editor UI, split a **format-only** pass forward (the format is small;
  the editor is the large part). *Decision needed at the point a real starting world is required —
  flagged, not resolved here.*
- **R-2 — format-version sequencing (C-5).** The main coordination hazard: #30/#28/#31/#40 all touch
  `SEASON_SAVE_FORMAT_VERSION`. Land them in wave order and rebase each on the prior frame; the
  fail-loud gates catch a desync, but the sequencing avoids a merge-time collision. Enforced by the
  Wave 1 → 2 → 4 order in §3.
- **R-3 — #31 is the only genuinely-new design work and it is dependency-heavy.** It phantoms if
  authored before #40 (economy) and #33 (psychology). Its position as **last** of the three is a
  feature, not a delay.
- **R-4 — #27 struct-freeze pressure (C-1).** The temptation to "just add a CA field to
  `PlayerAttributes`" will recur every time a lifecycle/valuation consumer wants a summary. Resist:
  CA is a *derived* summary (#28 KD-1), never a stored #27 field. A #27 record bump would ripple
  through every projection and the roster-reference snapshot.
- **R-5 — one canonical record, many state owners.** The architecture's strength (frozen #27 record +
  parallel state blocks) is also its discipline cost: an implementer must route new over-time state to
  the correct owner (#28 lifecycle / #31 contracts / #40 economy) rather than the record. C-1..C-4 are
  the guardrails.

---

## 7. Summary

The three #27 Stage-1+ deferrals are **not undesigned work** — they were decomposed by the
management-layer roadmap and largely already carry APPROVED specs:

- **Aging → #28 (APPROVED).** Buildable now (T0); behaviourally live by Wave 2. Implementation only.
- **Career-state persistence → #28 T1 + #30 (both APPROVED).** Composed into the season save at Wave 2.
  Implementation only.
- **Initial/reference-roster on-disk format → #27 residual / #47 (PLAN).** The one persistence sliver
  still needing a design pass — parked at Wave 7, pullable forward as a format-only pass.
- **Transfer market → #31 (PLAN).** The only deferral needing a new design supplement; correctly last
  (Wave 4), gated on #40 economy + #33 psychology.

The whole plan reduces to: **build in wave order (T0 aging → #30 spine → #28/#29/#40 → #33 → #31 →
#47), keep #27's canonical record frozen, and hand each save blob to #30's composition root as an
opaque version-gated sub-blob.** No new spec number, tag, or version is introduced by this plan
itself; each lands in its owning spec.

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-23 | Initial coordination plan. Maps the three #27 §4 Stage-1+ deferrals (aging / on-disk persistence / transfer market) to their roadmap owners (#28 APPROVED / #30+#28 APPROVED + #27-#47 residual / #31 PLAN), grounded in the actual `SPEC_INDEX.md` statuses, the #28/#30/#31 plans + #28 supplement, and the management-layer roadmap. Pins the cross-spec contracts (C-1..C-7), the wave sequence (§3), and the residual initial-DB sliver (§2.2/R-1). No new spec/tag/version introduced. |
