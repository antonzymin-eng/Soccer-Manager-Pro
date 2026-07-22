# Spec #40 — Club Finances & Economy — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#40** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 3 financials · **Tier:** S2 min → S3 deep · **Wave:** 2 · **FR prefix (proposed):** FR-FN
> **Determinism:** domain tag `0x29` / SubsystemOrdinal 91 (proposed off-pitch block, §6 — pinned only at promotion)
> **Purpose:** Club budgets, wages, revenue/sponsorship, and FFP — the counterparty-constraint layer #31 negotiation reads.

## 1. Scope
Per-club finances: transfer + wage budgets, wage ledger, revenue (matchday/sponsorship/prize money), and (deep tier) FFP-style balance constraints. Split from #31 so the economy is a system in its own right rather than a transfer side-effect. Minimal = a single deterministic "budget from league finish" figure; deep = a per-day/per-season accounting model with revenue streams and FFP. **Out of scope:** the negotiation itself (#31 reads budgets as a constraint but owns bids/contracts); staff wages as a mechanic (#34 supplies the line items, #40 owns the ledger); board takeovers/ownership (#45, which feeds budgets).

## 2. Staging (minimal-first → deep)
Minimal identity = "budget = f(league finish)" computed deterministically at season boundary — a single number #31 reads as its spending ceiling. The S3 revenue/FFP model **modulates that same figure** through one code path (the minimal budget is the identity the revenue streams and FFP penalties adjust), never a rewrite. A club with no S3 revenue model configured yields exactly the minimal league-finish budget.

## 3. Dependencies
- **Upstream (needs):** #27 (roster → wage bill inputs), #30 (season loop: league finish, prize money, the day/season accounting cadence).
- **Downstream (consumers):** #31 (budget/wage-structure constraint on bids and contracts), #45 (board/ownership adjusts budgets), #42 (academy facility funding), #34 (staff wages against the ledger).

## 4. Persistent state & save impact
New per-club financial world-state block (balance, budgets, wage ledger, revenue/FFP accumulators). Because it advances per-day and persists across seasons it lives in the world store — bumps `WORLD_STORE_FORMAT_VERSION`, landing as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec`/`WorldStateSerializer` pattern. Every accumulator field serialized and round-trip-covered.

## 5. Determinism
World tick (`WorldClock`) drives per-day/per-season accounting (living-world KD-4 — never the match tick). Dedicated RNG sub-stream (domain tag `0x29` / `SubsystemOrdinals` 91, proposed) reserved for any stochastic revenue/sponsorship variance; the minimal league-finish budget is a pure function (no draw). Allocation pinned in #16 §3.4 at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Accounting cadence: per-day continuous accrual vs. discrete season-boundary settlement vs. both — and which fields accrue on which cadence?
- **KD-2** Is the minimal "budget from league finish" a pure deterministic function with zero draw (preferred — keeps the RNG stream dormant until S3), or does even the minimal tier vary?
- **KD-3** The #31 boundary: does #40 expose budgets as a read-only constraint #31 queries, or does #31 write commitments back into #40's ledger? (Roadmap §5 invariant: define the projection direction, avoid two-way coupling.)
- **KD-4** FFP as a hard gate vs. a soft penalty on future budgets — and how it composes with board (#45) confidence.
- **KD-5** Wage ledger ownership: does #40 own the canonical wage bill (staff #34 + players #31 both writing line items), or does each source own its own and #40 aggregate read-only?

## 7. Primary surfaces (proposed)
- Per-club `ClubFinances` world-state block (proposed).
- A read-only budget/constraint query surface (proposed) consumed by #31/#42/#34.
- A per-day/per-season `AdvanceAccounting`-style hook (proposed) driven by #30's day-advance loop.
- League-finish → budget projection (proposed) — the minimal identity.

## 8. Test focus
Behaviour-neutral identity: an S3-revenue-unconfigured club yields exactly the minimal league-finish budget. Round-trip determinism of the financial block through `WorldStore.Snapshot`/`Restore`. Two-run determinism of a full season's accounting from a fixed world seed. Fail-loud gates on negative-balance underflow / malformed ledger / budget query before the season block is initialized.

## 9. Open questions / risks
- Projection-direction (KD-3) with #31 is the coupling risk; a two-way write coupling breaks the layer taxonomy.
- #40 (Wave 2) must precede #31 (Wave 4) in authoring (roadmap §7) — #31's counterparty constraint reads it; building #31 first phantoms the economy. The wave ordering already guarantees this.
- Cadence (KD-1) interacts with save-size budget; per-day accrual multiplies serialized state.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
| v0.2 | July 22, 2026 | AR fix: §9 wave labels corrected — #40 is Wave 2, #31 is Wave 4; the precedence is guaranteed by wave order (prior text mislabelled it "Wave 4 authoring"). |
