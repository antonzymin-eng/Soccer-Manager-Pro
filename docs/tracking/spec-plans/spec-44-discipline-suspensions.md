# Spec #44 — Discipline & Suspensions — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#44** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.1 · **Tier:** Stage 2 · **Wave:** 5 · **FR prefix (proposed):** FR-DC
> **Determinism:** read-only derivation over already-emitted card events — none (no RNG stream, no domain tag; consistent with #37 analytics / `match-viewer` being observational).
> **Purpose:** Season-level card accumulation, thresholds, and bans — a suspension-availability view over the match engine's already-emitted card events that #30 squad selection consumes.

## 1. Scope
Season-level discipline: accumulate the yellow/red cards the match engine already emits, apply threshold rules (N yellows → a ban, a red → an immediate ban of M matches), and expose a per-player suspension-availability view #30 consumes when selecting a matchday squad. **Out of scope:** the in-match card mechanics themselves (`MatchEngine` already produces `CardIssuedEvent`, second-yellow promotion, and sent-off tracking — #44 reads, never re-implements), appeals/psychology, and cross-competition scoping nuance beyond what #43 defines (competition-scoped accumulation is a #43-coupled extension).

## 2. Staging (minimal-first → deep)
Stage-2 minimal = one accumulation counter per player over the single #30 league with literal thresholds; a suspended player is simply unavailable for the next fixture. This is the identity a deeper tier modulates: competition-scoped accumulation (yellows reset per competition, #43-coupled), varying ban lengths by offence, and cup-vs-league carry rules — all extensions of the same read-only derivation, not a rewrite.

## 3. Dependencies
- **Upstream (needs):** the match engine's already-emitted **card events** — `CardIssuedEvent` (ordinal 0x06 — `EventRegistry.cs`; published by the match engine, `MatchEngine.cs`; carries recipient + card kind) plus the engine's `_yellowCards`/`_isSentOff` discipline state, surfaced via the `EventBus` ledger (`EventBus.SerializeLedger`), read the same observational way #37 analytics reads the ledger; #30 (the season loop that owns the day-advance and squad selection).
- **Downstream (consumers):** #30 squad selection (a suspended player is filtered from the available set), #38 UI (a suspensions/availability screen), #43 (competition-scoped variants).

## 4. Persistent state & save impact
Adds a per-player season card-accumulation tally + active-suspension counters to #30's season state. Because it is a **pure derivation over the serialized card ledger**, the tally can be either persisted (a small season sub-blob) or recomputed from the ledger on load — a design decision (KD-1). If persisted it rides #30's season sub-blob under the `SeasonSaveCodec` opaque-sub-blob pattern; no independent format version is likely needed. No `WORLD_STORE_FORMAT_VERSION` impact.

## 5. Determinism
Read-only / derivation — **no RNG stream, no domain tag, no `SubsystemOrdinals` entry** (roadmap §6 lists #44 among the read-only/presentation/infra specs). Accumulation advances on the world tick as #30's day-advance processes each played fixture's card ledger; the derivation is a pure function of the (already-deterministic) card events, so round-trip determinism is inherited from #30 and the match engine — #44 adds no new stochastic surface.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Persist the accumulation tally, or recompute it from the serialized card ledger on load? (The recompute option keeps #44 stateless but requires the full season's ledgers be retained.)
- **KD-2** What is the read boundary — does #44 consume `CardIssuedEvent` records off the `EventBus` ledger, or a structured match-outcome summary #30 emits per fixture? (Prefer the summary so #44 doesn't reach into match-engine ledger internals.)
- **KD-3** Where in #30's day-advance does discipline update relative to squad selection, so a card issued in the just-played fixture correctly bans for the next one (ordering / off-by-one)?
- **KD-4** How is a suspension expressed to #30 squad selection — an availability predicate (view), never a mutation of the player record (the roadmap §5 masking-is-a-view invariant applied to availability)?
- **KD-5** Second-yellow → red is already promoted in-engine; does #44 double-count if it also sees both the yellow and the promoted red in the ledger? Define the de-dup rule against the engine's emitted events.

## 7. Primary surfaces (proposed)
A discipline-accumulator that folds each fixture's card events into a per-player season tally (proposed); a suspension-availability query #30 squad selection calls (proposed); a suspensions view model for #38 (proposed). Existing seams referenced: `MatchEngine` `CardIssuedEvent` producer, `EventBus.SerializeLedger` ledger, #30's season state + day-advance + structured match-outcome summary, `SeasonSaveCodec`.

## 8. Test focus
Behaviour-neutral / read-only proof that observing card events does not alter match or season determinism (the `match-viewer` observer-neutrality precedent); deterministic accumulation → threshold → ban over a scripted card sequence; correct next-fixture ban timing (no off-by-one); de-dup of second-yellow→red; save→restore round-trip if the tally is persisted; suspension expressed as a view, verified never to mutate the player record.

## 9. Open questions / risks
- The read boundary (KD-2) is the main risk: reaching into `EventBus` ledger internals couples #44 to match-engine serialization detail; a #30-emitted per-fixture summary is the cleaner seam but requires #30 to expose it.
- Competition-scoped accumulation is #43-dependent — if #43 lands after #44, the minimal single-competition tally must be shaped so #43 can partition it without a rewrite.
- Second-yellow de-dup depends on exactly which events the engine emits for a promoted red; must be verified against the live `CardIssuedEvent` emission, not assumed.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
| v0.2 | July 22, 2026 | AR fix: `CardIssuedEvent` ordinal 0x08 → 0x06 (verified `EventRegistry.cs:67`; 0x08 is `SubstitutionEvent`). Confirmed the engine publishes it (`MatchEngine.cs`), so the read-only-derivation premise holds. |
