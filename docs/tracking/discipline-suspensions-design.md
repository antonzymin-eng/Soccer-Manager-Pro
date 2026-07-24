# Discipline & Suspensions #44 — Design Supplement

> **Created:** July 24, 2026
> **Last Updated:** July 24, 2026 (v0.4 — **PROMOTED**; prior v0.3 AR-2 CONVERGENCE, v0.2 AR-1, v0.1 initial)
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (July 24, 2026) — 11-file section set authored at
> `docs/specs/discipline-suspensions/` (FR-DC-001..022) → section-file AR PASS-1 (1M) → PASS-2 (2L) →
> CONVERGENCE → R-01..R-05 signed → **APPROVED**; `SPEC_INDEX.md` row 44 added (**41 APPROVED**). **One
> approval-time back-prop:** ERR-030-009 (the #30 FR-SN-013 availability-filter null seam;
> `spec-error-log.md` v1.40, `season-competition-loop` section-2/3 v0.8); no #16 change (read-only class).
> Section files are authoritative; this supplement is the design-history record. (Original status line
> follows for history.)
> DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #44 · **FR prefix:** FR-DC (grep-verified unclaimed across `docs/specs/**`).
> **Master-plan home:** §4.1 · **Tier:** Stage 2 (live at minimal — the #41 class) · **Wave:** 5 (second — after #43).
> **Determinism:** **read-only derivation — NO RNG stream, NO domain tag, NO `SubsystemOrdinals` entry** (the #37/#38/#49 class; roadmap §6 lists #44 read-only). No #16 §3.4 cross-cite is needed — a positive property, not a deferred allocation.
> **Source plan:** `docs/tracking/spec-plans/spec-44-discipline-suspensions.md` v0.2.

---

## 0. Scope

**Season-level discipline as a read-only derivation over already-emitted card events**: accumulate
the yellows/reds the match engine already publishes, apply threshold rules (N yellows → a ban; a
dismissal → an immediate ban), and expose a **per-player suspension-availability VIEW** the season
loop consults at squad selection. #44 **reads, never re-implements**: the in-match card mechanics
(`CardIssuedEvent`, second-yellow promotion, sent-off tracking) are `MatchEngine`-owned and stay
untouched. The availability view is the roadmap §5 masking-is-a-view invariant applied to
availability — **a suspension never mutates a `PlayerRecord` or a #27 `Squad`**.

**Out of scope:** in-match card mechanics (engine-owned); appeals/psychology; competition-scoped
accumulation nuance beyond carrying the partition key (#43-coupled, deep); quick-sim card synthesis
(a #30-owned deep extension — see §1); UI screens (#38, deferred).

## 1. What exists vs. what #44 adds (the seam reconnaissance — verified against source)

- **`CardIssuedEvent` (0x06, Tier A — verified `EventRegistry.cs:67` / `CardIssuedEvent.cs`):**
  payload `{ int Recipient; byte CardKind; ushort FoulOrdinal }` + the 12-byte header incl.
  `tick`. **The de-dup question (plan KD-5) is resolved by source, not assumption:**
  `ApplyCardAndCheckSentOff` (`MatchEngine.cs` §3293ff) publishes **exactly ONE event per card
  incident** with the ACTUAL kind — `0` = first yellow, `1` = straight red, `2` = **SecondYellow**
  (a second yellow promotes in-engine and publishes a single kind-2 event; **no separate red event
  follows**). So the fold rule is: kind 0 ⇒ yellow +1; kind 2 ⇒ yellow +1 **and** a dismissal;
  kind 1 ⇒ a dismissal (no yellow). Double-counting is structurally impossible from the emission
  side.
- **`Recipient` is a match AGENT id, not a `PlayerId`.** Cards are slot-scoped in-engine, and a
  substitution **resets the outgoing slot's yellow count** (the v1.33 M-1 fix) — so **post-match
  per-slot state cannot be the read** (a subbed-off player's cards would vanish). The correct read
  is a **tick-ordered fold over the fixture's published events**: initial lineup occupancy (the
  composition root configured the squads, so it holds slot→`PlayerId`) updated by
  `SubstitutionEvent` (0x08 — verified payload `{ int Outgoing; int Incoming; byte Team; byte
  SubstitutionReason }` + `tick`), attributing each card to the **occupying `PlayerId` at the
  card's tick**. (The exact `Incoming`-id semantics — slot-stable vs fresh id — are absorbed by
  the fold either way; verified against the live engine at T-phase.)
- **`EventBus.SerializeLedger` is write-only (no reader — the #37 KD-1 verified finding)** and #30
  retains no per-fixture ledgers (`MatchResult` is scoreline-shaped, FR-SN-016). Two consequences:
  (a) the read mechanism is the **#37-class read-only per-tick ledger tap** (FR-AN-002 — the
  APPROVED observational-read pattern: consumed every tick, lossless, unknown ordinals ignored;
  observer-neutrality digest-locked, the `match-viewer` precedent; one tap feeds both #37 and #44
  when both are built) — **not** ledger-byte parsing and **not** a new bus-subscription pattern;
  (b) the plan's KD-1 recompute-from-ledgers option is **impossible** — the tally MUST be
  persisted (persist is forced by verification, not chosen by taste).
- **Quick-sim fixtures produce no cards** (FR-SN-013a resolves a scoreline; grep-verified — no
  card surface in #30 §3). At minimal, discipline therefore accrues **only from engine-resolved
  fixtures** (the managed club's — covering both its own and its opponents' players in those
  matches). Deterministic, asymmetric by construction, and stated honestly; **quick-sim card
  synthesis** is a deferred #30-owned deep extension (a keyed draw on #30's own `0x22`
  season-events stream — never a #44 stream).
- **#30's managed-fixture flow (FR-SN-013):** `ISquadProvider.ResolveByClubId → ConfigureSquads`.
  The availability filter must act **between resolve and configure** — a #30 flow amendment #44
  pre-declares as its one approval-time back-prop (**ERR-030-009**, §8).
- **#43 (approved this wave):** fixtures/results carry `CompetitionId` (FR-CP-020); #44's tally
  carries the partition key from day one (minimal: always `0`, the league) so #43-scoped
  accumulation is a partition, not a rewrite (the plan §9 shaping concern).

**#44 adds:** a **`DisciplineState`** (per-player season tally: yellows + active bans, keyed
`(PlayerId, CompetitionId)` with `CompetitionId = 0` at minimal); a read-only
**`CardLedgerFold`** (the subscription + occupancy fold above); **threshold rules** (`[GT]`:
`YELLOW_ACCUMULATION_THRESHOLD` ⇒ `ACCUM_BAN_MATCHES`, kind-2 ⇒ `SECOND_YELLOW_BAN_MATCHES`,
kind-1 ⇒ `STRAIGHT_RED_BAN_MATCHES`); an **availability view** (`IsAvailable(playerId)` — a pure
predicate) + the squad-filter applied at the ERR-030-009 seam (a **value-copy reduced squad**,
never a #27 mutation); ban **serving** (a ban decrements per played fixture of the player's club,
engine-resolved or quick-sim alike); and a `DISCIPLINE_SAVE_FORMAT_VERSION` season-save sub-blob.
**No RNG stream, ever** (thresholds are literal; the derivation is pure).

## 2. Staging (live at minimal — the #41 class, not an identity scaffold)

#44's minimal tier is **behaviourally live** (the #41 injuries precedent, unlike the
#34/#32/#43 identity scaffolds): a player crossing the yellow threshold in engine-resolved
fixtures **is banned and the next lineup changes** — designed, deterministic behaviour. The
neutrality properties are therefore: (a) **observer-neutrality** — the fold's tap consumption
never perturbs the match digest (the `match-viewer` digest-lock); (b) **no-trigger identity** — a season
whose fixtures produce no threshold-crossing cards is byte-identical to pre-#44 (the availability
filter passes everything through); (c) **determinism** — same card events ⇒ same bans ⇒ same
filtered squads, two-run identical. Deep extensions (competition-scoped partitions via #43;
varying ban lengths by offence; quick-sim synthesis via #30) modulate the same fold/view — one
code path.

## 3. Dependencies & reference direction

- **compositionRoot → {#30, #44}** — the root wires the fold's subscription around each
  engine-resolved fixture, threads the lineup mapping in, applies the availability filter at the
  ERR-030-009 seam, and notifies #44 of each played fixture (ban serving) + the season roll.
- **#44 → #17 (EventBus)** — the read-only `Subscribe` surface for `CardIssuedEvent` /
  `SubstitutionEvent` (Tier A consumer; observer-neutral).
- **#44 → #27** — `PlayerId`/`Squad` **read-only** (the filtered squad is a value copy).
- **#44 does NOT reference #30, #43, #38, #16's RNG service, or the match engine's internals.**
  `CompetitionId` is carried as an `int` key (no #43 assembly reference).

Reference DAG: `compositionRoot → {#30, #44}`, `#44 → {#17, #27}`. **Acyclic**; no consumer
references #44; **no RNG stream** (the #37/#49 posture — no #16 §3.4 row exists or is needed).

## 4. Persistent state & save impact (KD-1 — persist; forced by verification)

An opaque, independently version-gated **discipline sub-blob** (`DISCIPLINE_SAVE_FORMAT_VERSION`
[FIXED] = 1) composed into #30's `SeasonSaveCodec` (the sibling precedent; no
`WORLD_STORE_FORMAT_VERSION` bump; outer bump a T1 coordination). Recompute-on-load is
**impossible** (no ledgers are retained — §1), so the tally persists: per entry
`(PlayerId, CompetitionId, Yellows, BanMatchesRemaining)`, strictly ascending `(PlayerId,
CompetitionId)` canonical order, plus nothing else — no RNG state (there is none). Codec
fail-loud posture (version-gate first, overflow-safe `Require`, trailing-byte guard,
canonical-order + range gates). Season boundary: **tallies reset, unserved bans carry** (the
real-football rule, pinned); genesis = empty; a load reconstructs and never resets a ban.

## 5. Determinism

**No RNG stream, no domain tag, no ordinal** — the accumulation is a pure fold over
already-deterministic Tier A events, thresholds are literal integers, serving is a per-fixture
decrement. Round-trip determinism is inherited (#30 + the engine). The one determinism obligation
#44 itself carries is **fold-order stability**: the fold consumes events in the bus's canonical
publish order (tick, then intra-phase order — already pinned by #17), so attribution is
deterministic by construction. Integer posture throughout; no float.

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// The per-player season tally. Keyed (PlayerId, CompetitionId); CompetitionId = 0 at minimal (KD-6).
public sealed class DisciplineState
{ /* map (PlayerId, CompetitionId) -> (int Yellows, int BanMatchesRemaining); canonical order; NO RNG state */ }

// KD-2 — the read-only fold. Fed by the #37-class per-tick ledger tap during an engine-resolved
// fixture; observer-neutral (unknown Tier A ordinals ignored — the FR-AN-019/F5 posture).
public sealed class CardLedgerFold   // consumes CardIssuedEvent + SubstitutionEvent off the tap
{
    /* seeded with the fixture's initial slot->PlayerId lineup (from the root's ConfigureSquads input);
       SubstitutionEvent updates occupancy; each CardIssuedEvent attributes to the occupant at its tick:
         kind 0 => Yellows+1;  kind 2 => Yellows+1 AND dismissal-ban;  kind 1 => dismissal-ban (KD-5, verified §1). */
}

// KD-4 — the availability VIEW (a pure predicate; never mutates #27 state).
public static bool IsAvailable(in DisciplineState s, int playerId /*, int competitionId = 0 */);
public static Squad FilterAvailable(in Squad resolved, in DisciplineState s);   // a reduced VALUE COPY
//   applied at #30's resolve->configure seam (ERR-030-009); pass-through when no ban is active.

// Ban serving: one decrement per played fixture of the player's club (engine-resolved or quick-sim).
public void OnClubFixturePlayed(int clubId /*, competitionId */);

// Thresholds ([GT], literal — Appendix A):
//   Yellows >= YELLOW_ACCUMULATION_THRESHOLD => ACCUM_BAN_MATCHES ban, Yellows -= threshold;
//   kind 2 => SECOND_YELLOW_BAN_MATCHES;  kind 1 => STRAIGHT_RED_BAN_MATCHES  (bans stack additively).
```

## 7. Key design decisions

- **KD-1 (persist the tally — forced, not chosen).** The plan's fork (persist vs recompute) is
  closed by verification: `SerializeLedger` has no reader and #30 retains no per-fixture ledgers,
  so recompute-on-load has no input. One small `DISCIPLINE_SAVE_FORMAT_VERSION` sub-blob (§4).
- **KD-2 (the read boundary — the #37-class per-tick ledger tap + occupancy fold; never ledger
  bytes, never post-match slot state, never a new subscription pattern).** Two verified facts
  force the shape: slot-keyed engine state loses a subbed-off player's cards (the v1.33
  substitution reset), and the ledger bytes are an engine-internal write-only format. The fold
  consumes the **read-only per-tick ledger tap #37's FR-AN-002 already pinned** (the approved
  observational-read pattern — lossless every tick, unknown Tier A ordinals ignored, one tap
  shared by #37/#44 when both land), tracks slot→player occupancy (initial lineup +
  `SubstitutionEvent`s), and attributes each card at its tick. Observer-neutrality is
  digest-locked (the `match-viewer` precedent). No #30-emitted summary is required — the root
  already holds the lineup mapping the fold seeds from, and no second read mechanism is invented.
- **KD-3 (ordering — fold at resolution; filter at selection; serve at the club's fixtures).**
  The fold completes when the fixture resolves; the availability filter runs at the **next**
  selection (the ERR-030-009 resolve→configure seam), so a card in the just-played fixture bans
  for the next one with no off-by-one (locked by a two-fixture scripted test). A ban decrements
  once per **played fixture of the player's club** — engine-resolved or quick-sim alike (the
  ban's clock is the club's calendar, not the resolution path).
- **KD-4 (availability is a VIEW).** `IsAvailable` is a pure predicate; `FilterAvailable` returns
  a **reduced value-copy `Squad`** for `ConfigureSquads` — #27's canonical `Squad`/`PlayerRecord`
  are never written (the roadmap §5 invariant, byte-identity-locked like #32's T-SC-VIEW-001).
- **KD-5 (de-dup — resolved by source).** One event per incident; kind 2 = yellow +1 **and** a
  dismissal in a single event; kind 1 adds no yellow. No de-dup table is needed — the rule is the
  emission contract, cited to `ApplyCardAndCheckSentOff` and locked by a fold test over a scripted
  kind-{0,2,1} sequence.
- **KD-6 (#43-partitionable shape from day one; re-key/retirement hygiene — bans FOLLOW the
  player).** The tally keys `(PlayerId, CompetitionId)` with `CompetitionId = 0` at minimal (an
  `int` key — no #43 assembly reference); #43-scoped accumulation and per-competition ban serving
  become a partition, not a rewrite. **Hygiene:** `PlayerId` re-keys on a #31 transfer — and
  unlike #32's knowledge (drop-on-transfer, re-scout to regain), **discipline migrates**: on a
  roster re-key the tally + unserved bans move old→new `PlayerId` (a ban follows the player in
  football — the deliberate contrast with #32's drop rule, recorded so the two hygiene rules are
  never conflated); on retirement the entry is dropped. Delivery is the same FR-TX-022
  roster-move hook / #28 lifecycle coordination the siblings consume — a T-phase wiring.
- **KD-7 (live-at-minimal staging — the #41 class).** #44's minimal changes behaviour by design
  (a ban filters a lineup); the neutrality obligations are observer-neutrality, no-trigger
  identity, and determinism (§2) — not a byte-identical always-on scaffold.
- **KD-8 (season boundary).** Tallies reset at `RollToNextSeason`; **unserved bans carry** (the
  real-football rule); both pinned and round-trip-covered.

## 8. Cross-spec back-props

**At approval: ONE** — **ERR-030-009**: #30 FR-SN-013's managed-fixture flow gains a
pre-declared **availability-filter null seam** between `ISquadProvider.ResolveByClubId` and
`ConfigureSquads` ("the resolved squad MAY be filtered through the #44 availability view — a
value-copy reduction — before `ConfigureSquads`"; a null seam until #44's T-phase wires it — the
ERR-030-002/004/006/007 pre-declaration pattern, §3.4-side rather than tick-order-side). **No #16
change** (no tag/ordinal/stream — the #37/#49 positive property). **No #43/#27/#17 change** (the
`CompetitionId` key is an `int`; the Subscribe surface is public; #27 is read-only).

**At the #44 T-phase (deferred):** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the
quick-sim card-synthesis extension (a #30-owned deep item on #30's `0x22` stream, coordinated
then); the #43 competition-scoped partition activation.

## 9. Test focus

- **Observer-neutrality (the headline):** an engine-resolved fixture with the fold subscribed is
  **digest-identical** to the same fixture unobserved (the `match-viewer` lock).
- **No-trigger identity:** a season with no threshold-crossing cards is byte-identical to pre-#44
  **except #44's own sub-blob** (sub-threshold yellows legitimately accrue there; the filter
  passes everything through — the sibling only-new-artifact phrasing).
- **Re-key/retirement hygiene:** a transfer migrates the tally + unserved bans old→new
  `PlayerId`; retirement drops the entry (KD-6).
- **Fold correctness:** scripted kind-{0,0,2,1} sequences with substitutions — attribution to the
  occupant at the card's tick (a card before a sub attributes to the outgoing player; after, to
  the incoming); kind-2 counts one yellow + one dismissal (never two events' worth); the v1.33
  slot-reset never leaks into the tally.
- **Threshold/ban lifecycle:** accumulation ban at the literal threshold (tally decremented by
  the threshold); dismissal bans; stacking; serving decrements once per club fixture (both
  resolution paths); **no off-by-one** (card in fixture N ⇒ unavailable for fixture N+1 ⇒
  available for N+1+ban).
- **View-not-mutation:** `FilterAvailable` returns a reduced value copy; #27 squads byte-identical
  through every #44 path.
- **Save round-trip:** tally + bans field-identical across save/restore and across
  `RollToNextSeason` (tallies reset, unserved bans carry); fail-loud codec gates; **no RNG-state
  field** (schema-shape).
- **Determinism:** two-run identical bans/filtered squads from the same fixture events; integer
  posture (no float).

## 10. Risks

- **The read boundary** was the plan's main risk — closed by verification (subscription + fold;
  the two dead-end reads ruled out by source facts, not preference).
- **Attribution across substitutions** — the slot-reset bug class (v1.33) is exactly what the
  occupancy fold exists to avoid; locked by the scripted-sub tests. The `Incoming`-id semantics
  are absorbed by the fold and re-verified at T-phase.
- **Asymmetric minimal coverage** (only engine-resolved fixtures produce cards) — stated, not
  hidden; evened by the deferred quick-sim synthesis (#30-owned).
- **Off-by-one ban timing** — the KD-3 ordering + the two-fixture test.
- **#43 partition rework** — pre-shaped away by the `(PlayerId, CompetitionId)` key (KD-6).

## 11. Promotion pipeline

1. 11-file section set at `IN REVIEW` (FR-DC-001..NNN) → 2. section-file AR to convergence →
3. R-01..R-05 sign-off → APPROVED; `SPEC_INDEX.md` row 44 → 4. back-prop **ERR-030-009** (the #30
availability-filter null seam) → 5. T-phase: T0 `DisciplineState` + thresholds + the availability
view (inert until wired) → T1 the sub-blob + season-save composition → T2 the tap-fed fold +
the ERR-030-009 filter wiring + ban serving + the re-key migration hook → T3 deep (#43
partitions; the #30 quick-sim synthesis coordination).

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.4 | July 24, 2026 | **PROMOTED** — 11-file section set (FR-DC-001..022), section-file AR to convergence (PASS-1 1M `(0,0)`-drop canonical-representation rule → PASS-2 2L), R-01..R-05 signed, APPROVED; `SPEC_INDEX.md` row 44; ERR-030-009 filed (`spec-error-log.md` v1.40, `season-competition-loop` section-2/3 v0.8). |
| v0.3 | July 24, 2026 | AR-2 (0H+0M+2L) → **CONVERGENCE** (L-only round): two stale "subscription" mentions (§2 observer-neutrality, §11 T2) reconciled to the tap wording; T2 gains the re-key migration hook. Full hostile re-read otherwise clean at High/Medium — ready to promote to section files. |
| v0.2 | July 24, 2026 | AR-1 (0H+2M+1L). **M-1** — KD-2/§1/§6: the read mechanism aligned to the **#37-class per-tick ledger tap** (FR-AN-002, the APPROVED observational-read pattern; one tap feeds #37+#44) — the v0.1 draft invented a parallel #17-Subscribe pattern with unanswered phase-discipline questions. **M-2** — KD-6/§9: the re-key/retirement hygiene was missing; pinned **migrate-on-re-key** (bans follow the player — the deliberate contrast with #32's drop rule, recorded so the two are never conflated) + drop-on-retirement, delivered by the FR-TX-022 hook / #28 lifecycle coordination at T-phase. **L** — the no-trigger identity phrasing gains the "except #44's own sub-blob" caveat (sub-threshold yellows accrue). |
| v0.1 | July 24, 2026 | Initial design supplement from spec-plan v0.2, grounded on verified source (the single-event kind-2 emission in `ApplyCardAndCheckSentOff`; agent-id `Recipient` + the v1.33 slot-reset ruling out post-match slot state; write-only `SerializeLedger` + scoreline-only `MatchResult` forcing persist (KD-1) and the subscription read (KD-2); card-free quick-sim). Resolves plan KD-1..KD-5 + adds KD-6 (partition key), KD-7 (live-at-minimal, the #41 class), KD-8 (boundary rules). One approval-time back-prop (ERR-030-009); no #16 change (read-only — no tag/stream). |
