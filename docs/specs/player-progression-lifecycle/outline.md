# Player Progression & Lifecycle #28 — Outline

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — PASS-1 → AR-2 → AR-3 convergence; APPROVED)
`docs/tracking/player-progression-lifecycle-design.md` v0.3)
**Version:** 0.2
**Status:** APPROVED
**FR prefix:** FR-PG · **Wave:** 2 · **Master-plan home:** §4.3 (aging) / §5 (youth)

---

## Purpose

Player lifecycle on the **world tick** — aging, attribute decline, retirement, regen/newgen
production, and attribute growth via a current/potential-ability (CA/PA) model over #27's canonical
`PlayerAttributes` — as **one code path with a config dial** (the deep curve reduces to the literal
master-plan §4.3 step when off), driven by #30's day-advance loop at the seam #30 already reserved
for it (#30 KD-2 / KD-6). This is the **identity** the deep tier (#29 training, #42 youth) modulates,
authored as a Stage-1-forward pull (the #21/#22/#27/#30 precedent).

## Section map

| Section | Content |
|---|---|
| **1** | Introduction, scope, dependencies, key decisions (KD-1..KD-8) |
| **2** | Functional requirements (FR-PG-001..024), data structures, failure modes (F1..F6) |
| **3** | Core algorithms — the KD-1 integer fixed-point growth projection, the CA/PA model, regen generation, retirement, the save codec |
| **4** | Architecture — assembly placement, reference direction, file layout, determinism identifiers, the CS0104 note |
| **5** | Test plan — determinism / byte-exact-restore / behaviour-neutral-identity / regen / retirement locks |
| **6** | Performance — world-tick cadence budget (off-pitch, not the 60 Hz path) |
| **7** | Future extensions — the T-phase plan, the Stage-3 deep curve dial, the #29 training-input consumption |
| **8** | References |
| **9** | Approval checklist |
| **Appendices** | Constant catalogue, worked growth-across-a-save + regen examples |

## Key decisions (summary — full text in §1)

- **KD-1** Byte-exact fractional-daily projection via an **integer fixed-point `GrowthCursor`** (no
  float); `[1,20]` attributes are the single source of truth, CA a derived summary, PA the ceiling.
- **KD-2** The #29 training seam is a **method input defaulted to neutral** — training is an *input*
  to #28's single growth function, never a parallel mutation (no phantom interface).
- **KD-3** Regens are day-deterministic from the `progression.regen` stream, reuse #27's draw
  pattern, and get a **fresh `PlayerId`** (never a retiree's).
- **KD-4** #28 owns a career-state block keyed by `PlayerId` (complete `PlayerRecord` + lifecycle
  overlay); #27's canonical struct stays schema-untouched.
- **KD-5** Retirement is **flagged** on the world tick, **applied** (roster removal + regen) only at
  the season boundary — never mid-fixture.
- **KD-6** `RunSeasonBoundary` applies the deferred retirements/regens (growth is banked daily, not
  re-banked here); restartable.
- **KD-7** `ProgressionEngine` is the sole writer; a read-only `LifecycleViewModel` is observer-neutral.
- **KD-8** With the curve dial off, the projection reproduces the literal §4.3 step exactly
  (behaviour-neutral identity, digest-locked).

## Determinism identifiers (promoting the reserved #16 rows)

`DOMAIN_TAG_PLAYER_PROGRESSION = 0x20`, `SubsystemOrdinals.PlayerProgression = 82` — the
`_RESERVED_0x20_` / 82 rows `deterministic-sim/section-3.md` already holds for #28. Promoted at
approval; the stream registers at the first draw site (T-phase), never earlier (FR-LW-031).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline from the converged supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
