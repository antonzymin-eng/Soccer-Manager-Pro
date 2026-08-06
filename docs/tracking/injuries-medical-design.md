# Injuries & Medical #41 — Design Supplement

> **Created:** July 23, 2026
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (July 23, 2026) — 11-file section set authored at
> `docs/specs/injuries-medical/` (FR-MD-001..027) → section-file AR-1 (1M float→integer) → AR-2 (1M
> fixed-radix append parity) → AR-3 CONVERGENCE → R-01..R-05 signed → **APPROVED**; `SPEC_INDEX.md` row 41
> added; ERR-041-001 (`0x2A`/92) + ERR-030-002 (#30 tick-order step 4) filed. Section files are
> authoritative; this supplement is the design-history record. (Original status line follows for history.)
> DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #41 · **FR prefix:** FR-MD (grep-verified unclaimed across `docs/specs/**`).
> **Master-plan home:** §4.2 (injury management) · **Wave:** 2.
> **Determinism (proposed, promoting the roadmap-reserved row):** `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A`,
> `SubsystemOrdinals.InjuriesMedical = 92`, off-pitch band — **one world-tick stream** `injuries.occurrence`.
> **Source plan:** `docs/tracking/spec-plans/spec-41-injuries-medical.md` v0.2.

---

## 0. Scope

Injury **occurrence** (draw + trigger), **severity** classification, and a **recovery** timeline advancing on
the **world tick** (`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz match loops), modulated by
future physio/medical staff (#34). Split from #29 so injury is a system in its own right rather than a
training side-effect.

**Out of scope (owned elsewhere, referenced as seams):**
- The **fatigue accumulators** themselves — #29 owns the world-tick training-fatigue accumulator; the match
  engine owns in-match fatigue (`1 − AerobicPool`). #41 reads #29's *output* as an occurrence input (KD-2)
  and never touches either accumulator.
- **Squad-selection consequences** — #30 reads a read-only availability view; #41 owns no selection logic.
- The **medical-staff entity model** — #34 supplies staff quality through the identity `MedicalModifier`
  routing seam (KD-5); #41 builds no #34 interface.
- **Attribute decline from injury** — #28 owns `GrowthProjection` (the sole attribute-mutation path); #41
  exposes a read-only injury signal #28 *may* later read, never a parallel attribute write (KD-2 direction).

## 1. What exists vs. what #41 adds

**Exists (verified against source / approved specs):**
- `src/player-database/PlayerAttributes.cs` — 31 `int[1,20]` attributes. There is **no injury-proneness /
  robustness field today** (`Strength`/`Stamina`/`Balance` are the nearest physical attributes). So an
  injury-proneness input is derived from existing physical attributes at Stage 2; a dedicated attribute is a
  #27 append deferred to the deep tier (recorded, not built — KD-4).
- `docs/specs/training-system/` (#29, APPROVED) — already exposes a read-only `InjuryRiskContribution`
  scalar (FR-TR-017: "computed from `TrainingFatigue` + low `Condition`, mitigated by the player's own
  robustness attributes"), shaped **for exactly #41 to read**, with "no #41 interface built". This is the
  KD-2 input seam, already waiting.
- `docs/specs/season-competition-loop/` (#30, APPROVED) — `WorldStore.AdvanceDay()` runs a **fixed,
  documented day-advance tick order** (FR-SN-009..012 / KD-2) whose null seams are enumerated for **#28
  (slot 1) / #29 (slot 2) / #33 (slot 3) only** (FR-SN-034); a fixture day plays/quick-sims the fixture and
  its result + event ledger exist afterward. `SeasonSaveCodec` composes opaque, independently version-gated
  sub-blobs (KD-1, `SEASON_SAVE_FORMAT_VERSION`).
- `docs/specs/player-progression-lifecycle/` (#28, APPROVED) — owns a per-`PlayerId` career-state block
  composed as an opaque `PROGRESSION_SAVE_FORMAT_VERSION` sub-blob; regens/retirements mutate the roster at
  the season boundary (FR-PG-011/015) — the roster-membership lifecycle #41's state must track (KD-7).
- `docs/specs/deterministic-sim/section-3.md` — the off-pitch band is open (`0x1E`/80 … `0x22`/84); the
  roadmap §6 reserves **`0x2A` / 92** for #41. No `_RESERVED_0x2A_` placeholder row exists yet — #41's
  promotion **adds** `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` directly (ERR-041-001), because #41 has a genuine
  draw site (unlike #29).
- The match engine already emits collision / foul / card events into its Tier-A event ledger (the #37
  analytics / #44 discipline read-only-derivation substrate). No new match-engine surface is needed (KD-3).

**#41 adds:** a per-player `InjuryState` (active injury: severity, recovery-remaining, history counter); a
world-tick `AdvanceMedicalDay` step (recovery countdown + occurrence draw) invoked at a **new #30 tick-order
slot** (back-prop, KD-6); a read-only availability view (#30 reads) and `MedicalViewModel` observer (#38); the
`MEDICAL_SAVE_FORMAT_VERSION` sub-blob; and the single `injuries.occurrence` world-tick RNG stream.

## 2. Staging (minimal-first → deep, one code path)

- **Stage-2 minimal** — one occurrence model per player per world day: a risk score assembled from
  (#29's `InjuryRiskContribution` + recent match participation as an `AppearanceDays` count that #30's
  fixture result already tracks + a robustness term derived from #27 physical attributes), a single keyed
  occurrence **draw** against that score, a **fixed severity-tier** classification (Minor / Moderate /
  Serious → a fixed recovery-days constant each), and a **linear** per-day recovery countdown. Staff
  modulation is `MedicalModifier.Identity` (×1.0 on both risk and recovery).
- **Stage-3 deep** — a distribution-driven severity draw, **recurrence risk** on early return, a
  per-match-incident physical-load input derived read-only from the event ledger (KD-3), and staff-quality
  modulation — all on the **same one code path**, each defaulting to its Stage-2 identity via a config dial
  (`deepMedicalEnabled` off ⇒ fixed tiers, no recurrence, minutes-only load, `MedicalModifier.Identity`).

**One code path (KD-8):** the deep-tier severity distribution, recurrence multiplier, ledger-load input, and
staff modulation all default to their identities, so the Stage-2 surface is the exact identity the deep tier
extends — the #21/#28/#29 default-behaviour-neutral discipline, not a rewrite.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #41** — the day-advance loop *invokes* `AdvanceMedicalDay` at a **new reserved slot** (KD-6), and
  reads the availability view for squad selection. #41 never references #30.
- **#41 → #29** — #41 reads #29's `InjuryRiskContribution` **value** (an occurrence input). #29 never
  references #41 (it exposes a value; the seam already exists, FR-TR-017).
- **#41 → #27, #16** — reads `PlayerRecord` / `PlayerAttributes`; consumes the determinism namespace + the
  world-tick `DeterministicRngService`.
- **#41 → match event ledger (read-only)** — the deep-tier per-fixture physical-load summary derives from
  the already-emitted ledger, the #37/#44 posture; **no** new match-engine producer, **no** #41 interface in
  the match engine (KD-3).
- **#34 → #41** (future) — a non-identity `MedicalModifier` when staff land (KD-5); no #34 interface today.
- **#30/#38 read #41** — the availability view + `MedicalViewModel` (value copies, KD-8).

Reference DAG: `#30 → {#28, #29, #41}`, `#41 → {#29, #27, #16}`. **Acyclic.** #29's assembly stays
schema-untouched (`InjuryRiskContribution` is #29's own already-published output).

## 4. Persistent state & save impact (KD-7)

Adds an opaque, independently version-gated **medical sub-blob** (`MEDICAL_SAVE_FORMAT_VERSION` [FIXED] = 1)
composed into #30's season save via the `SeasonSaveCodec` pattern — **not** `WORLD_STORE_FORMAT_VERSION`
(this supersedes the plan §4 guess; §7 KD-7 gives the rationale). The composing outer
`SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 exactly as #28 (`PROGRESSION_SAVE_FORMAT_VERSION`)
and #29 (`TRAINING_SAVE_FORMAT_VERSION`) do — the block is appended after the existing sub-blobs and the
codec never parses it; the exact outer integer is pinned at the T-phase (as #28/#29 defer it). Per club:
each player's `InjuryState` (active-injury severity, recovery-remaining days, cumulative injury count,
`LastAdvancedWorldDay` idempotency cursor). **No RNG cursor is serialized** — occurrence draws are
position-independent keyed draws (§5), so there is nothing to persist and restore. Fail-loud on version
mismatch / out-of-bounds length prefix (overflow-safe `ReadCount`) / trailing bytes (F3/F5, the
`MatchSaveCodec` posture). **Serialize, don't regenerate** (#30 KD-5); every field round-trip-covered,
including a **mid-recovery** and a **post-fixture-draw** save.

## 5. Determinism (KD-1 — the headline; single-clock, position-independent draws)

**All #41 stochastic draws happen on the WORLD tick**, on #41's dedicated `injuries.occurrence` stream
registered on the **world-tick `DeterministicRngService`** (the same service #22's `world.text` and #28's
`player-progression.regen` register on — seeded from the world seed, sub-streams independent). **The match
tick NEVER draws for #41.**

Each daily occurrence draw is **position-independent / keyed**, not a free-running counter draw: it is keyed
on `(stream, entityId = playerId, ActionOrdinal derived deterministically from worldDay + a draw-purpose
ordinal)` — the **off-pitch keyed-draw precedent** (#28 regen keyed by `entityId = clubId`; #30 quick-sim
keyed on `(seed, seasonNumber, roundIndex, homeClubId, awayClubId)`), **not** the match-tick free-running
card-severity cursor. Consequences: **there is no free-running cursor to persist** — the same
`(playerId, worldDay, purpose)` reproduces the same draw regardless of how many other players/days drew
first, so save→restore is **automatically byte-exact** with nothing to continue, and the plan's dual-clock
cursor hazard is dissolved twice over (one clock, and no persisted cursor even on that clock). APPEND-only
draw-purpose ordinals preserve replay parity across fail-loud paths.

Match-incident injuries do **not** draw on the match tick either. The deep-tier per-fixture physical-load
summary is derived **read-only** from the already-emitted event ledger (KD-3); the occurrence **draw** it
feeds is made at a **world-tick `AdvanceMedicalDay` following the fixture** (the fixture is played after the
day-advance loop reaches the fixture day, so its result/ledger are read on the next day-advance, before the
next fixture's squad selection reads availability). A match incident is a world-tick occurrence *input*,
never a match-tick draw site.

Promotes `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` / `SubsystemOrdinals.InjuriesMedical = 92` at section-file
approval (ERR-041-001, spec-text-first like `0x20`/`0x22`); the code const + stream registration land at #41
T2 with the first draw site (FR-LW-031 — no phantom stream registered ahead of a draw). No
`DETERMINISM_DIGEST_VERSION` bump (a namespace allocation + a new off-pitch stream).

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
public enum InjurySeverity : byte { None = 0, Minor, Moderate, Serious }   // None = healthy (default)

// #41-owned per-player world-tick medical state (serialized, KD-7).
public struct InjuryState
{
    public InjurySeverity Severity;     // None = available; else injured
    public int RecoveryRemaining;       // world-days left [0, RECOVERY_MAX]; 0 iff Severity == None (F1/F2)
    public int InjuryCount;             // cumulative career injuries (deep-tier recurrence input; history)
    public uint LastAdvancedWorldDay;   // idempotency cursor — a day is advanced at most once (F6);
                                        //   MEDICAL_NOT_ADVANCED_SENTINEL = uint.MaxValue, NOT 0

    public static InjuryState Create() =>               // healthy day-0 state (never default())
        new() { Severity = InjurySeverity.None, RecoveryRemaining = 0, InjuryCount = 0,
                LastAdvancedWorldDay = MEDICAL_NOT_ADVANCED_SENTINEL };
}

// The world-day step (KD-6, invoked at #30's new slot): recovery countdown THEN occurrence draw.
// The ONLY #41 draw site. The draw is KEYED on (playerId, worldDay, purpose) — position-independent,
// no free-running cursor (KD-1/§5); `rng` is the world-tick DeterministicRngService the key resolves against.
public static void AdvanceMedicalDay(ref InjuryState s, int playerId, in PlayerAttributes a,
                                     in InjuryRiskContribution trainingRisk, in MatchLoad recentMatchLoad,
                                     in MedicalModifier medical, uint worldDay, DeterministicRngService rng);

// KD-2/KD-3 occurrence input from recent match participation. Stage-2 minimal populates AppearanceDays
// (a count #30's fixture result already tracks); the ledger-derived HardContacts field is the deep-tier
// KD-3 extension (read-only from the event ledger). Neutral (all-zero) at Stage 2 = training-risk only.
public readonly struct MatchLoad { public readonly int AppearanceDays; public readonly int HardContacts;
                                   public static MatchLoad None => default; }

// KD-8 read-only availability view — #30 squad selection reads it (a player with Severity != None is out).
public static bool IsAvailable(in InjuryState s) => s.Severity == InjurySeverity.None;

// KD-5 staff routing seam — identity until #34 lands (no phantom #34 interface).
public readonly struct MedicalModifier
{
    public static MedicalModifier Identity => default;   // ×1.0 risk, ×1.0 recovery-speed
}

// KD-8 observer surface for #38 (value copies).
public readonly struct MedicalViewModel { /* severity / recovery-remaining / injury-count / available */ }
```

## 7. Key design decisions

- **KD-1 (single-clock, position-independent occurrence — the headline risk, dissolved).** All #41 draws
  happen on the world tick, on one `injuries.occurrence` stream, and each is a **keyed / position-independent
  draw** on `(playerId, worldDay, purpose)` — the #28/#30 off-pitch keyed-draw precedent, not the match-tick
  free-running card-severity cursor (§5). The match tick never draws for #41; match-incident injuries are
  read-only ledger derivations fed as an occurrence *input* to the world-tick draw. The plan's "dual-clock
  cursor divergence" cannot occur — #41 owns exactly one clock, **and** keyed draws persist no cursor at all,
  so save/restore is byte-exact with nothing to continue.

- **KD-2 (fatigue reconciliation — read-only input, no double count).** #41 reads #29's already-published
  `InjuryRiskContribution` (FR-TR-017) read-only as one occurrence input, plus recent match participation
  (`MatchLoad`) and a robustness term. #41 never reads/mutates either fatigue accumulator (#29's training-fatigue, the match
  engine's `AerobicPool`) — #29 owns the accumulator and exposes a scalar; #41 consumes the scalar. No
  counter is shared, so no double count is representable.

- **KD-3 (match-incident coupling — read-only ledger derivation, no new producer).** The deep-tier
  per-fixture physical-load summary derives **read-only** from the already-emitted event ledger
  (collisions / hard fouls), the #37 analytics / #44 discipline posture — **no** new match-engine surface,
  **no** #41 interface in the match engine (phantom-free). **Stage-2 minimal uses `MatchLoad.AppearanceDays`
  only** (a participation count from the #30 fixture result); the ledger-derived `HardContacts` summary is
  the deep-tier extension, one code path via `deepMedicalEnabled`. This keeps the match-tick layer untouched
  and the occurrence layer single-clock.

- **KD-4 (severity/recovery model shape — one code path).** Stage-2: a **fixed severity-tier** table
  (Minor/Moderate/Serious → a fixed recovery-days constant each) + a **linear** per-day recovery countdown.
  Stage-3: a distribution-driven severity draw + recurrence risk on early return, defaulting to the fixed
  tier via the config dial. Injury-proneness is a **derived** term from #27 physical attributes at Stage 2;
  a dedicated `InjuryProneness` #27 attribute is a deep-tier #27 append **recorded, not built** (avoids a
  #27 schema ripple in the minimal tier).

- **KD-5 (staff modulation — identity routing seam).** `AdvanceMedicalDay` takes `in MedicalModifier`,
  default `Identity` (×1.0 on **both** occurrence-risk and recovery-speed). **No #34 interface is built**
  (FR-LW-031); #34 becomes the producer of a non-identity modifier when it lands — the #29 `CoachingModifier`
  pattern.

- **KD-6 (#30 tick-order integration — a back-prop, not a #30 rewrite).** #41 needs a per-day world-tick
  step (recovery countdown + occurrence draw), but #30's KD-2 tick order (§3.3) is a **pinned four-step
  sequence** — spec seams `1. progression (#28)` / `2. training (#29)` / `3. human-systems (#33)`, then the
  live terminal step `4. WorldStore.AdvanceDay()`; only #28/#29/#33 are enumerated as null seams
  (FR-SN-034, authored before #41). **Slot 4 is already the live world-day tick — it is not free.** #41's
  promotion files a **#30 back-prop (ERR-030-002)** inserting an **injuries null seam** as a new step
  **positioned after the #28/#29/#33 spec seams and immediately before `WorldStore.AdvanceDay()`** — i.e.
  the sequence becomes `1 #28 · 2 #29 · 3 #33 · 4 injuries (#41, NEW) · 5 WorldStore.AdvanceDay()`, shifting
  **only** the ordinal of the terminal live tick, never re-pinning a reserved seam's position (the
  ERR-021-005 `TeamTactic` append precedent for extending an APPROVED spec's reserved enumeration).
  **Ordering rationale (pinned):** the injuries step runs **after** progression (1) and training (2) so the
  occurrence draw reads the **day's updated** training-fatigue / condition (avoids a one-day-stale risk
  input), and **before** `WorldStore.AdvanceDay()` so it operates on the current `worldDay` before the clock
  increments (the same pre-increment position #28/#29 hold). Recovery countdown precedes the occurrence draw
  **within** the step so a player cannot both recover-to-zero and be re-injured on the same tick from one
  call.

- **KD-7 (persistence — season-save sub-blob; supersedes the plan's `WORLD_STORE_FORMAT_VERSION`).**
  `MEDICAL_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, **not** a
  `WORLD_STORE_FORMAT_VERSION` bump. Rationale: injury state is a per-`PlayerId` **career-state overlay**
  exactly like #28's lifecycle block (which chose the season-save sub-blob), and #41's RNG cursor must
  serialize **beside** the injury state so a mid-recovery / post-draw save resumes byte-identically — a
  self-contained #41 unit, not scattered across the world-store RNG block. The plan §4 wrote
  `WORLD_STORE_FORMAT_VERSION` before #28/#29 established the season-save-sub-blob convention; this
  supplement reconciles to that convention. Fail-loud gates; serialize-don't-regenerate. **Roster-membership
  lifecycle** in lockstep with #28's season-boundary churn (KD, FR): a #28 regen inserts an
  `InjuryState.Create()` (healthy) for the fresh `PlayerId`; a retirement removes the retiree's entry — the
  FR-PG-011 / FR-TR-025 remove/insert parallel, keyed by `PlayerId`, applied by the roster owner (#30).

- **KD-8 (behaviour-neutral identity + stream independence).** #41's addition is neutral in three senses:
  (a) **stream independence** — registering the `injuries.occurrence` sub-stream leaves every existing
  stream's cursor byte-identical (the #22/#26 sub-stream-independence precedent), so a world without #41
  active is unperturbed; (b) an `occurrenceEnabled` dial off reduces #41 to a recovery-only no-op (no
  draws); (c) `InjuryState` defaults to `Create()` = Healthy. The deep tier extends the fixed-tier / minutes
  / identity-staff surface, never rewrites it.

## 8. Test focus

- **Save→restore round-trip** across a **mid-recovery** boundary AND a **post-fixture-draw** boundary — the
  `InjuryState` fields restore field-identical; two-run determinism of a full season's injuries from one
  world seed.
- **Position-independent draw lock (the KD-1 lock)** — the same `(playerId, worldDay, purpose)` reproduces
  the same occurrence outcome regardless of draw order across players/days (no persisted cursor), so a save
  taken immediately after a world-tick occurrence draw resumes byte-identically with nothing to continue;
  there is **no** match-tick draw path to diverge (asserted structurally — #41 registers exactly one
  world-tick stream and the match assembly references nothing in #41).
- **Behaviour-neutral identity proof** — `occurrenceEnabled` off ⇒ no draw, recovery-only; adding the #41
  stream leaves existing streams' cursors byte-identical (stream independence).
- **Fatigue-input read-only lock** — #41 reads #29's `InjuryRiskContribution` and never writes any fatigue
  accumulator (no write-back path exists — distinct fields in distinct assemblies).
- **Recovery/occurrence ordering (KD-6)** — recovery-to-zero and re-injury cannot both fire in one
  `AdvanceMedicalDay` call; the injuries slot reads the day's updated training-fatigue (post slot-2).
- **Idempotency** — advancing the same world day twice is a no-op (`LastAdvancedWorldDay`); a day gap fails
  loud (the FR-TR-026 posture — #30 advances one day at a time).
- **Roster lifecycle** — a regen inserts a healthy `InjuryState.Create()`; a retiree's entry is removed (no
  unbounded leak across seasons).
- **Fail-loud** — bad `MEDICAL_SAVE_FORMAT_VERSION`, out-of-bounds length prefix, trailing bytes; an
  out-of-contract `InjurySeverity` / negative `RecoveryRemaining` / `RecoveryRemaining > 0` with
  `Severity == None` fails loud at the consuming seam (the #27 `SquadFileLoader` / #28 F4 precedent).

## 9. Risks

- **Dual-clock occurrence (headline).** Dissolved structurally by KD-1: single world-tick clock, single
  stream, **keyed position-independent draws (no persisted cursor)**; match incidents are read-only inputs,
  not draw sites.
- **#29 fatigue double-count.** Mitigated by KD-2: #41 reads #29's scalar output, never an accumulator.
- **#30 tick-order coupling.** Resolved by the KD-6 back-prop (ERR-030-002) — an appended null seam in the
  documented order, the established APPROVED-spec extension mechanism, not a #30 rewrite.
- **Save-home inconsistency.** Resolved by KD-7 reconciling the plan's `WORLD_STORE_FORMAT_VERSION` guess to
  the #28/#29 season-save-sub-blob convention (cross-checked at the section-file stage).
- **#34 staff lands later.** Mitigated by KD-5's identity routing seam — #41 ships neutral before staff
  exist. **#27 injury-proneness attribute** is a recorded deep-tier append (KD-4), not a minimal-tier #27
  ripple.
- **Deferred extensions (recorded, not built):** the ledger-derived per-fixture load input (KD-3 deep tier),
  recurrence risk, distribution-driven severity, a dedicated #27 `InjuryProneness` attribute, and an
  injury→#28-decline input — all default to their Stage-2 identities.

## 10. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-MD-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; flip `SPEC_INDEX.md` row.
4. #16 §3.4: **ERR-041-001** promotes `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` / `SubsystemOrdinals` 92
   (spec-text-first; code const + stream registration at #41 T2). #30: **ERR-030-002** inserts the injuries
   null seam as a new step after #28/#29/#33 and before `WorldStore.AdvanceDay()` in the KD-2 tick order.
5. T-phase implementation (post-APPROVED): T0 value types + deterministic Stage-2 occurrence/recovery step →
   T1 `MEDICAL_SAVE_FORMAT_VERSION` sub-blob + season-save composition + stream-cursor serialization → T2
   `AdvanceMedicalDay` + availability view wired at #30's new slot; register `injuries.occurrence` at the
   first draw → T3 deep severity distribution / recurrence / ledger-load input / staff modulation.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.5 | August 6, 2026 | **T2 AR pass — 3H + 4M + 4L, all fixed; pass 2 clean.** The #41-side headline is the one that would have shipped: **a mid-match save restored the wrong starting eleven.** FR-MD-023's filter reduces the squad before `ConfigureSquads`, but the match snapshot records only each team's `ClubId` — it cannot record *which eighteen of the twenty-five* — so a restore re-ran `LineupSelector` over the unfiltered roster and put a different eleven's canonical attribute records on the pitch, with the ClubId matching, the size gate passing and the match diverging from the pre-save run in silence. Fixed at `SeasonSaveManager.Load`, the only layer holding both the medical block and the match blob: it rebuilds the career from the same file and re-applies the filter through an `ISquadProvider` decorator, so restore re-selects from the squad the match was configured with. Latent today (it needs the occurrence dial armed AND a mid-match save) and armed for the interactive client. Locked by a 60-tick digest continuation across the save — the attribute records are re-derived rather than serialized, so nothing shorter can see *which* eleven came back. Also: `CanSelect` — the depleted-squad rule's viability probe added by T2 — had shipped as a hand-copied second copy of `Select`'s walk with no equivalence test, which is the parallel-surface trap in the very mechanism introduced to avoid inventing a second selection rule; collapsed to one `TrySelect`. |
| v0.4 | August 6, 2026 | **T2 LANDED** (the wiring; post-promotion record). `SeasonLoop` drives `AdvanceMedicalDay` at the new slot, after #29's, so the risk assembly reads the same-day conditioning (KD-6 / ERR-030-002); `IsAvailable` reaches squad selection via `PlayerCareerStates.SelectAvailable` at the pre-declared ERR-030-009 position on **both** resolution paths — the quick-sim rates a club by the XI it would field, so a club missing four first-choice players must be rated as such whether or not a human is watching. **No stream was registered and `SubsystemOrdinals` 92 stays unallocated**: T0 had already resolved the draw to a local keyed derivation (ERR-041-002), so §7.1's "register `injuries.occurrence` at the first draw" is satisfied by there being no stream to register. **The occurrence dial ships OFF** (FR-MD-027) — not caution: the fifth AR pass measured ~23% first-day, ~43% half-fatigued and exactly 0 on the default focus, two to three orders out in both directions, and KD-W1 forbids a re-tune ahead of the balance pass. Both dial positions are locked, because "off injures nobody" is satisfiable by a step that is never called. **The design decision this pass produced** is the depleted-squad rule: back-filling to a player COUNT is wrong, since selection refuses a position-incomplete squad outright (KD-L3) and eighteen fit outfielders with no goalkeeper would stop the season; the rule is press-the-least-injured-back-in until the club can field the formation, asked of the engine's own selector through a new `SquadRating.CanFieldStartingEleven` rather than answered by a second selection rule in `season-save`. In the limit that is the whole squad — the unfiltered behaviour — so the filter can never leave a club worse off than having none. **ERR-041-010 filed:** (a) FR-MD-025's `RegenResult`/`RetirementResult` do not exist, resolved as roster reconciliation; (b) §3.5's `MatchLoad` source — "#30's fixture result" — does not exist either, and `AppearanceLoadWeight` is a non-zero `[GT]` (150), so the term is unsupplied rather than vacuous. `MatchLoad.None` is passed; it is unreachable while the dial is off, and a recompute from the fixture list is **not** an equivalent substitute because the availability filter changes who actually played. Due with the balance pass. |
| v0.1 | July 23, 2026 | Initial design supplement from spec-plan v0.2. |
| v0.3 | July 23, 2026 | PROMOTED — 11-file section set authored + APPROVED (section-file AR-1 1M float→integer → AR-2 1M fixed-radix → AR-3 CONVERGENCE). |
| v0.2 | July 23, 2026 | AR-1 (2M+2L): **M1 (contract)** — KD-6 said append injuries "as slot 4", but #30 §3.3 slot 4 is the live `WorldStore.AdvanceDay()`; corrected to a NEW step inserted after #28/#29/#33 and before `AdvanceDay` (`1·2·3·4-injuries·5-AdvanceDay`), shifting only the terminal live tick's ordinal. **M2 (architecture/consistency)** — KD-1/KD-7/§5 persisted an `injuries.occurrence` `RngStreamState` (the match-tick card-severity precedent); switched to **position-independent keyed draws** on `(playerId, worldDay, purpose)` (the #28 regen / #30 quick-sim off-pitch precedent) → no persisted cursor, simpler sub-blob, dual-clock hazard dissolved twice over. L1 KD-1/§5 fixture-timing wording (injuries surface at a world-tick *following* the fixture). L2 §2/§6 match-load dependency made explicit (`MatchLoad.AppearanceDays` from #30's fixture result at Stage 2; ledger `HardContacts` deep-tier). |
