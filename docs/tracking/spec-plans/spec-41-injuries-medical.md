# Spec #41 — Injuries & Medical — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#41** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.2 injury management · **Tier:** S2 min → S3 deep · **Wave:** 2 · **FR prefix (proposed):** FR-MD
> **Determinism:** domain tag `0x2A` / SubsystemOrdinal 92 (proposed off-pitch block, §6 — pinned only at promotion)
> **Purpose:** Injury occurrence, severity, and recovery, modulated by physio/medical staff — spanning world-tick fatigue accumulation and match-tick incidents.

## 1. Scope
Injury occurrence (draw + trigger), severity classification, and a recovery timeline advancing on the world tick, with physio/medical-staff (#34) modulating risk and recovery speed. Split from #29 so injury is a system in its own right rather than a training side-effect. **Out of scope:** the fatigue accumulator itself (#29 owns training fatigue; the match engine owns in-match fatigue — #41 reads both as occurrence inputs); squad selection consequences (#30 reads an availability view); the medical-staff entity model (#34 supplies staff quality).

## 2. Staging (minimal-first → deep)
Minimal identity = a single occurrence model (fatigue/incident → injury draw → fixed severity → linear recovery countdown) with staff modulation at ×1.0 (no #34 wired ⇒ baseline risk/recovery). The S3 deep tier adds severity distributions, recurrence, and staff-quality modulation **on that same one code path** — the minimal recovery countdown is the identity the deep model modulates, config-dialled.

## 3. Dependencies
- **Upstream (needs):** #27 (player record / injury-proneness attribute), #29 (world-tick fatigue accumulator as an occurrence input), #34 (physio/medical-staff modulation), the match engine (match-tick incident events as an occurrence trigger).
- **Downstream (consumers):** #30 (squad-selection availability view), #28 (injury as a development/decline input).

## 4. Persistent state & save impact
New per-player injury/medical state (active injury, severity, recovery-remaining, history) — persistent world-state. Bumps `WORLD_STORE_FORMAT_VERSION` (injuries persist across match and season boundaries), landing as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec`/`WorldStateSerializer` pattern. Recovery countdown and any RNG cursor serialized and round-trip-covered.

## 5. Determinism
Dedicated RNG sub-stream (domain tag `0x2A` / `SubsystemOrdinals` 92, proposed). **The load-bearing determinism decision (KD-1):** occurrence draws span BOTH the world tick (training/fatigue accumulation, on `WorldClock`) AND the match tick (match incidents, on the match loop) — the supplement must decide which layer owns occurrence and how one RNG stream is safely shared or split across the two clocks without cursor divergence across `Snapshot`/`Restore`. Allocation pinned in #16 §3.4 at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Which layer owns injury occurrence — the world-tick accumulator, the match-tick incident, or a split ownership with one reconciliation point? How does the shared RNG stream stay determinism-safe across two clocks (the match loop already serializes its own RNG cursor via the snapshot; the world tick uses the world-store stream)?
- **KD-2** Reconcile with #29's fatigue accumulator without double-counting: does #41 read #29's accumulator read-only as a risk input, or does #29 emit an injury-risk signal #41 consumes?
- **KD-3** Match-incident injuries — does the match engine emit a structured incident the world loop ingests (a new producer, mirroring the #30→#22 phase-1 seam), or does #41 derive them post-match from the event ledger read-only (like #37/#44)?
- **KD-4** Severity/recovery model shape: fixed-tier minimal vs. distribution-driven deep — and the config dial that makes them one path.
- **KD-5** Staff (#34) modulation composition — multiplier on risk, on recovery, or both, and neutral = ×1.0.

## 7. Primary surfaces (proposed)
- Per-player `InjuryState` world-state block (proposed).
- An availability/fitness view (proposed) consumed by #30 squad selection.
- An occurrence-evaluation hook (proposed) on both the world day-advance and a post-match/incident seam (KD-3).
- Staff-quality → risk/recovery modulation inputs (proposed) from #34.

## 8. Test focus
Behaviour-neutral identity: no-staff (×1.0) baseline reproduces the minimal fixed-severity/linear-recovery model. Round-trip determinism of injury state through `WorldStore.Snapshot`/`Restore`, including a mid-recovery save. **RNG-cursor continuity across the world-tick/match-tick boundary** (the KD-1 hazard — a save taken after a match-incident draw must resume the stream identically, the class of the match engine's own card-severity cursor lock). Two-run determinism of a season's injuries from a fixed world seed. Fail-loud gates on negative recovery / malformed injury record.

## 9. Open questions / risks
- The dual-clock occurrence ownership (KD-1) is the headline risk — the match loop's determinism contract (serialized RNG cursor) and the world store's stream must not clobber each other.
- #41 must precede #31 in Wave 2 (availability feeds squad value / transfer decisions) and pairs tightly with #29 — authoring order matters.
- Match-incident coupling (KD-3): a new match-engine producer vs. read-only ledger derivation is a layer-taxonomy fork (phantom-interface caution if built ahead of the producer).

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
