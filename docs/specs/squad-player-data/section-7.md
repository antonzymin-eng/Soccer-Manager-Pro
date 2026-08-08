# Squad / Player Data Layer Specification #27 — Section 7: Future Extensions and Wiring Status

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — AR-2 M-1, the §7.3 LANDED record; header corrected August 8, 2026 at the lint sweep: it had misstated v0.1 while the table carried v0.2 — the pass-4-M5 header-currency class, on the spec ERR-027-004 amended)
**Version:** 0.2
**Status:** APPROVED

---

Unusually for this project, the code precedes the numbered spec: the whole T-phase wiring set has
already landed. §7.1 records what is LANDED (present tense, with the governing sibling design doc);
§7.2 records the genuine deferrals.

## 7.1 T-phase wiring status — LANDED

**T1/T2 — canonical projection + `ConfigureSquads` (FR-SQ-022 / FR-SQ-023).**
`src/match-engine/PlayerAttributeProjection.cs` projects each per-spec attribute struct (#2/#5/#6/
#7/#8/#13/#14) field-by-field from the canonical `PlayerAttributes` record, and
`MatchEngine.ConfigureSquads` seeds every agent from a configured `Squad` instead of
`CreateDefault()`/`STAGE0_NEUTRAL_ATTRIBUTE`. This **closes `ERR-007` for real**: the Pass/Shot
`KickPower` proxies are now the KD-P1 derived value from real `Passing`/`Technique`/`Finishing`/
`LongShots`, and `Crossing`/`WeakFootRating` read the canonical field — the engine-side proxy tags
retire. Deliberately **not** behaviour-neutral (the point is distinct players); the no-squad path
stays byte-identical (KD-P7). Governed by `docs/tracking/player-attribute-projection-design.md`
(v0.4, AR-1..AR-3 converged).

**T3 — snapshot roster reference (FR-SQ-024).** Per-team `_rosterClubId` (= `Squad.ClubId`,
sentinel `-1` when unconfigured) is serialized in the snapshot header at `SNAPSHOT_SCHEMA_VERSION`
16 — the identity half of restore fidelity (which squad each team loaded), not per-player values
(KD-7). A configured squad is digest-distinguishable from an unconfigured one by design (KD-T3-2).
Governed by `docs/tracking/squad-roster-reference-design.md` (v0.2, converged).

**Phase-2 — distinct-squad restore re-projection (FR-SQ-025).** `MatchEngine.RestoreFromSnapshot`
takes an `ISquadProvider`; `ReprojectDistinctSquads` re-runs `LineupSelector` +
`PlayerAttributeProjection` from the resolved roster and replays the substitution bench-swap keyed
by the serialized `_activeBenchSlot`, so a distinct-squad match is now restore-deterministic.
Fail-loud on absent provider / unresolvable or mismatched `ClubId`. Landed as snapshot-deserialize
Phase 2 (`docs/tracking/snapshot-deserialize-design.md`).

**LineupSelector — proper per-line selection (FR-SQ-026).** `src/match-engine/LineupSelector.cs`
replaces the Stage-0 roster-order trust mapping with position-partitioned greedy selection by
mean-attribute rating (`PlayerId` tie-break, no RNG); GK flags flow from the selection.
A coherently-ordered squad reproduces roster order, so the default path stays neutral. Governed by
`docs/tracking/lineup-selection-design.md` (v1.0, LANDED).

## 7.2 Deferrals

- **`PlayerPosition → RoleId` mapping (KD-4).** The coarse `PlayerPosition` (4 values) is not the
  positioning-ai `RoleId` (13 granular). `LineupSelector` bridges `PlayerPosition` to a formation
  slot's `DefaultLine`; a full granular `PlayerPosition → RoleId` mapping is not invented here and
  is future work.
- **On-disk save-format squad persistence, transfer market, aging/training** — Stage-1+/Stage-2
  economy features (master development plan §4.3/§4.4), out of scope per §0. The Stage-0
  `SquadFileLoader` text import is human-authoring only, not a determinism-pinned wire format.

## 7.3 Update — GK #11 / Heading #10 projections LANDED (July 22, 2026)

The KD-P8 deferral above (recorded at T1/T2, when `MatchEngine` built neither the GK nor the
Heading orchestrator, so a `ToGoalkeeper`/`ToHeading` projection would have been a phantom consumer)
is **closed**: Goalkeeper Mechanics #11 and Heading Mechanics #10 are now wired into the engine
(opt-in Phase 1, default OFF), and `src/match-engine/PlayerAttributeProjection.cs` v1.2 adds
`ToGoalkeeper` (int→float widen of the ten canonical GK fields) + `ToHeading` (raw
`Heading`/`Strength`/`Balance` copy) — the projections are a live consumer of both orchestrators'
`Commit*Intent` seams. Governed by `docs/tracking/gk-heading-engine-integration-design.md` (AR-1..AR-3
converged). This makes the canonical record the source of truth for **every** per-spec attribute
struct (#2/#5/#6/#7/#8/#10/#11/#13/#14), completing FR-SQ-001's single-source-of-truth guarantee for
the GK/Heading structs the T1/T2 landing had left on the neutral seed.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial wiring status: T1/T2/T3/Phase-2/LineupSelector all LANDED; KD-4 mapping + Stage-1+ persistence deferred. |
| 0.2 | 2026-07-22 | — | AR-2 M-1: the GK #11 / Heading #10 projection deferral (KD-P8) is stale — `PlayerAttributeProjection.cs` v1.2 landed `ToGoalkeeper`/`ToHeading` July 22 with the opt-in #10/#11 engine integration; new §7.3 records LANDED. Status APPROVED. |
#endregion
