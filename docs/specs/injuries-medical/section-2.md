# Injuries & Medical #41 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.9 — balance-pass AR pass 9 L4: new F8 — the sentinel itself is not a day; the refusal the code has always enforced gets its normative row)
**Last Updated (prior):** August 8, 2026 (v0.8 — balance-pass AR pass 8 (L4 + L2): FR-MD-007 no longer names the `injuries.occurrence` stream that must never exist; rows 0.6/0.7 reordered ascending)
**Last Updated (prior):** August 8, 2026 (v0.7 — balance-pass AR pass 6 M4: §2.2's signature de-phantomed — `rng` → `worldSeed, occurrenceEnabled`; the stale `MatchLoad` comment corrected. Prior header below.)
**Last Updated (prior):** August 7, 2026, later same day (v0.6 — the balance pass D3/D4: FR-MD-027 re-stated as ARMED with a required construction argument (ERR-041-011); FR-MD-005 re-anchored off the phantom registered stream onto the keyed derivation (ERR-041-012, discharging ERR-041-002's deferred half))
**Last Updated (prior):** August 7, 2026 (v0.5 — the balance pass D2 (ERR-041-010(b)): FR-MD-010 pins the `AppearanceDays` window unit — appearances in the `APPEARANCE_WINDOW_DAYS` `[GT]` window of days strictly before the draw day, never the current day (the ERR-030-027 pre-round ordering depends on the exclusion); the record itself is #30-owned)
**Last Updated (prior):** August 6, 2026 (v0.4 — ERR-041-008: §2.3 F3's exception type corrected to match the posture it cites)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.9
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**
- **FR-MD-001** — Injury advancement MUST run on the world tick only (`WorldClock` day); it MUST NOT read
  or advance the 10 Hz/60 Hz match loops.
- **FR-MD-002** — Per-player `InjuryState` (severity, recovery-remaining, cumulative injury count, last-
  advanced day) is #41-owned state, keyed by `PlayerId`, serialized under #41's sub-blob (KD-7). It is the
  single source of truth for a player's injury status.
- **FR-MD-003** — `AdvanceMedicalDay` MUST be the sole entry point that mutates `InjuryState`; a read-only
  observer (`MedicalViewModel`, FR-MD-024) and the availability view (`IsAvailable`, FR-MD-023) MUST NOT
  mutate state.
- **FR-MD-004** — Within one `AdvanceMedicalDay` call, the recovery countdown MUST be evaluated **before**
  the occurrence draw (KD-6); the occurrence draw MUST be gated on whether the player was already healthy
  at call **entry** (pre-countdown), not on the post-countdown state, so recovering-to-zero and a new
  occurrence cannot both happen from one call.

**Determinism (KD-1)**
- **FR-MD-005** — All #41 stochastic draws MUST occur on the world tick, via the single dedicated
  keyed occurrence derivation (`DrawOccurrence`, domain-separated by `DOMAIN_TAG_INJURIES_MEDICAL`);
  #41 MUST NOT draw on the match tick. *(Re-anchored at ERR-041-012, discharging ERR-041-002's deferred
  half: no registered `DeterministicRngService` stream exists or may be added — a registered stream is
  cursor-positioned, which FR-MD-006/007 forbid; `SubsystemOrdinals.InjuriesMedical = 92` stays
  deliberately unallocated per FR-LW-031.)*
- **FR-MD-006** — Each occurrence draw MUST be **position-independent / keyed** on `(playerId, worldDay,
  purpose)`; it MUST NOT depend on a free-running per-stream cursor or on the order in which other
  players/days were drawn.
- **FR-MD-007** — No `RngStreamState` / cursor is serialized for the keyed occurrence derivation — there is nothing to
  persist, because a keyed draw is reproducible from its key alone (KD-1).
- **FR-MD-008** — Draw-purpose ordinals (Appendix A) MUST be **APPEND-only**; an existing ordinal MUST NOT
  be renumbered or reused. The `DeriveActionOrdinal` bijection (§3.1.1) MUST use the **fixed**
  `DRAW_PURPOSE_RADIX` constant — never the current purpose *count* — so appending a purpose leaves every
  prior `(worldDay, purpose)` ordinal unchanged, preserving replay/save parity across a version bump; every
  `purpose` MUST be `< DRAW_PURPOSE_RADIX`.

**Risk inputs (KD-2/KD-3)**
- **FR-MD-009** — #41 MUST read #29's `InjuryRiskContribution` read-only as one occurrence input; it MUST
  NOT read or mutate #29's `TrainingFatigue` accumulator or the match engine's `AerobicPool`.
- **FR-MD-010** — `MatchLoad` is an occurrence input only, supplied by the caller (never computed or stored
  by #41). Stage-2 populates `AppearanceDays` from #30's per-player appearance record (ERR-041-010(b)):
  **the number of days the player was fielded in the `APPEARANCE_WINDOW_DAYS` `[GT]` window of days
  strictly BEFORE the draw's world day — never the current day itself** (a match on day *d* first feeds
  the draw on day *d+1*; #30 ERR-030-027 depends on this exclusion, since the draw runs pre-round). The
  window length is #41's `[GT]` because #41 owns what the input means; #30 owns the record that supplies
  it, which structurally bounds the window to `[1, 31]`. The ledger-derived `HardContacts` field is the
  deep-tier KD-3 extension. `MatchLoad.None` (all-zero) is the identity — no match-load contribution.
- **FR-MD-011** — #41 MUST NOT add a new match-engine producer or interface; any per-fixture physical-load
  derivation MUST be read-only over the already-emitted event ledger (KD-3).

**Severity & recovery (KD-4)**
- **FR-MD-012** — Stage-2 severity classification MUST use a fixed severity-tier table (Minor / Moderate /
  Serious), each mapped to a fixed recovery-days constant (Appendix A), derived from the **same** single
  occurrence draw via fixed proportional bucketing — Stage 2 MUST NOT consume a second RNG draw to
  classify severity.
- **FR-MD-013** — Stage-3 (deep tier: a distribution-driven severity draw + recurrence risk on early return)
  MUST default to the Stage-2 fixed-tier / no-recurrence behaviour via a config dial (`deepMedicalEnabled`
  off), so the minimal tier is the identity the deep tier extends (one code path, KD-4/KD-8).
- **FR-MD-014** — The Stage-2 recovery countdown MUST be **linear and integer**: `RecoveryRemaining`
  decrements by the fixed integer `RECOVERY_DAYS_PER_TICK_BASE` (= 1) per world day while `Severity !=
  None`, clamped at `[0, RECOVERY_MAX]` (F1). Staff **recovery-speed** modulation MUST be applied to the
  **assigned tier recovery-days at injury time** (`RecoveryRemaining = RecoveryDaysForTier[tier] × 1000 /
  MedicalModifier.RecoverySpeedMillMult`, integer division — a faster physio assigns fewer total days),
  **not** as a per-tick decrement multiplier (which, against a fixed integer base of 1, would truncate every
  non-integer multiplier to a no-op). All medical arithmetic MUST be integer — no float (the #28/#29
  integer-projection posture; keeps the system free of float-mode/MXCSR sensitivity).
- **FR-MD-015** — The occurrence-risk robustness/injury-proneness term MUST be a **derived** deterministic
  function of existing #27 physical attributes at Stage 2 (never RNG); a dedicated `InjuryProneness` #27
  attribute is a recorded deep-tier deferral, not built here (KD-4).

**Staff seam (KD-5)**
- **FR-MD-016** — `AdvanceMedicalDay` MUST take `in MedicalModifier` set to `MedicalModifier.Identity`
  (per-mille `1000` = ×1.0 on both occurrence-risk and recovery-speed) until #34 lands; no #34 interface is
  built (FR-LW-031). `MedicalModifier.Identity` MUST be an **explicit factory** (both per-mille fields =
  1000), and `default(MedicalModifier)` (all-zero) MUST NOT be treated as a valid runtime value — an all-zero
  modifier means ×0 occurrence-risk and a divide-by-zero recovery-days scale; a zero `RecoverySpeedMillMult`
  reaching the consuming seam MUST **fail loud** (F4, the zero-value-trap discipline the #28/#29 `Create`-vs-
  `default` precedent guards against).

**Persistence (KD-7)**
- **FR-MD-017** — `MEDICAL_SAVE_FORMAT_VERSION` [FIXED] = 1; #41's state lands as an opaque, independently
  version-gated sub-blob under #30's season save (`SeasonSaveCodec` pattern), **not** a
  `WORLD_STORE_FORMAT_VERSION` bump.
- **FR-MD-018** — Every `InjuryState` field (per club, keyed by `PlayerId`) MUST be serialized and
  round-trip field-identical; **serialize, don't regenerate** (#30 KD-5). No RNG cursor field exists to
  serialize (FR-MD-007).
- **FR-MD-019** — Restore MUST **fail loud** on version mismatch / out-of-bounds length prefix (overflow-
  safe `ReadCount`) / trailing bytes (F3/F5).

**Idempotency & tick order (KD-6)**
- **FR-MD-020** — `AdvanceMedicalDay` MUST be idempotent per world day via `LastAdvancedWorldDay` (a
  save→restore→re-run of an already-advanced day is a no-op, F6).
- **FR-MD-021** — A day gap (`worldDay > LastAdvanced + 1`, post-sentinel) MUST **fail loud**
  (`ArgumentException`) rather than silently skipping the intervening days' recovery/occurrence evaluation
  (F7, the FR-TR-026 posture) — #41 does not batch-replay a gap; #30 advances one day at a time.
- **FR-MD-022** — #41's world-tick step MUST be invoked at #30's **new** reserved slot, positioned **after**
  the #28/#29/#33 spec seams and **immediately before** `WorldStore.AdvanceDay()` (KD-6, the ERR-030-002
  back-prop) — never reordered ahead of #28/#29 (which the risk-score assembly reads the same-day output
  of) nor after the world-day clock increments.

**Availability & observers (KD-8)**
- **FR-MD-023** — A read-only `IsAvailable(in InjuryState)` MUST be exposed (`true` iff `Severity ==
  None`); #30's squad selection reads it; #41 MUST own no selection logic.
- **FR-MD-024** — A read-only `MedicalViewModel` (value copies: severity / recovery-remaining / injury-
  count / available) MUST be exposed for #38.

**Roster-membership lifecycle**
- **FR-MD-025** — The per-club `InjuryState` set MUST track roster membership in lockstep with #28's
  season-boundary roster mutation (FR-PG-011/015): on a #28 `RegenResult`, an `InjuryState.Create()`
  (healthy) MUST be inserted for each fresh `PlayerId` (never `default(InjuryState)` — that reintroduces the
  day-0 sentinel trap, F1/F6); on a `RetirementResult`, the retiree's `InjuryState` entry MUST be
  **removed**. This is the FR-PG-011 / FR-TR-025 remove/insert parallel, keyed by `PlayerId`, applied at the
  season boundary by the roster owner (#30). Without it the block leaks retired entries unboundedly across
  seasons and regens have no defined medical state (F2 — the missing-state hazard).

**Reference direction & neutrality (KD-8)**
- **FR-MD-026** — The reference direction MUST stay one-way: `#30 → #41 → {#29, #27, #16}`; #41's assembly
  MUST NOT reference `MatchEngine`, `LivingWorld`, `SeasonSave`, or #30 itself. #29's / #27's assemblies stay
  schema-untouched.
- **FR-MD-027** — The occurrence dial. **ARMED at the balance pass (ERR-041-011): a production career is
  constructed with `occurrenceEnabled` ON**, at rates measured in the football band by the season-scale
  instrument (league ≈ 780 injuries/season on the 20-club bootstrap; starters ≈ 2.1, reserves ≈ 1.1,
  squad unavailability ≈ 9%). The construction argument MUST be required, never defaulted — a default in
  either position changes behaviour at every omitting call site with no diff the day it flips. The OFF
  position stays supported and locked both ways: with `occurrenceEnabled` off, `AdvanceMedicalDay` MUST
  reduce to recovery-only (no draws, no new injuries), and `InjuryState` MUST default to `Create()` =
  Healthy. The stream-independence clause is vacuous by construction — no stream is registered
  (FR-MD-005 as re-anchored; ERR-041-012).

## 2.2 Data structures

```csharp
public enum InjurySeverity : byte { None = 0, Minor, Moderate, Serious }   // None = healthy (default)

// #41-owned per-player world-tick medical state (serialized, KD-7).
public struct InjuryState
{
    public InjurySeverity Severity;     // None = available; else injured
    public int RecoveryRemaining;       // world-days left [0, RECOVERY_MAX]; 0 iff Severity == None (F1)
    public int InjuryCount;             // cumulative career injuries (deep-tier recurrence input; history)
    public uint LastAdvancedWorldDay;   // idempotency cursor — a day is advanced at most once (F6);
                                         //   MEDICAL_NOT_ADVANCED_SENTINEL = uint.MaxValue, NOT 0 (the
                                         //   day-0 double-accrual trap the #28/#29 lifecycle precedent
                                         //   guards against)

    // Runtime states are created via Create — never default(). LastAdvancedWorldDay is seeded to the
    // sentinel so a legitimate world-day 0 cannot collide with "never advanced".
    public static InjuryState Create() =>
        new() { Severity = InjurySeverity.None, RecoveryRemaining = 0, InjuryCount = 0,
                LastAdvancedWorldDay = MEDICAL_NOT_ADVANCED_SENTINEL };
}

// The world-day step (KD-6, invoked at #30's slot 4): recovery countdown THEN occurrence draw.
// The ONLY #41 draw site. The draw is KEYED on (worldSeed, playerId, worldDay, purpose) —
// position-independent, no free-running cursor, no DeterministicRngService (KD-1/§3, ERR-041-012).
// occurrenceEnabled is the FR-MD-027 dial: REQUIRED, never defaulted; disarmed, the step is the
// recovery countdown and cursor advance alone.
public static void AdvanceMedicalDay(ref InjuryState s, int playerId, in PlayerAttributes a,
                                      in InjuryRiskContribution trainingRisk, in MatchLoad recentMatchLoad,
                                      in MedicalModifier medical, uint worldDay, ulong worldSeed,
                                      bool occurrenceEnabled);

// KD-2/KD-3 occurrence input from recent match participation, supplied by the caller (FR-MD-010) — #41
// does not track this itself. Stage-2 populates AppearanceDays from #30's per-player appearance record
// (the ERR-041-010(b) APPR block — built BECAUSE no such count existed); the ledger-derived
// HardContacts field is the deep-tier KD-3 extension. Neutral (all-zero) = training-risk-only.
public readonly struct MatchLoad { public readonly int AppearanceDays; public readonly int HardContacts;
                                    public static MatchLoad None => default; }

// KD-8 read-only availability view — #30 squad selection reads it (a player with Severity != None is out).
public static bool IsAvailable(in InjuryState s) => s.Severity == InjurySeverity.None;

// KD-5 staff routing seam — identity until #34 lands. Per-mille integer multipliers (1000 = ×1.0) so all
// medical arithmetic stays integer (FR-MD-014). Identity is an EXPLICIT factory — default() (all-zero)
// is NOT a valid runtime value (×0 risk / divide-by-zero recovery scale; fail loud per FR-MD-016 / F4).
public readonly struct MedicalModifier
{
    public readonly int OccurrenceRiskMillMult;    // 1000 = ×1.0; >1000 raises occurrence risk
    public readonly int RecoverySpeedMillMult;     // 1000 = ×1.0; >1000 = faster recovery (fewer assigned days)
    public static MedicalModifier Identity => new(1000, 1000);
    public MedicalModifier(int occ, int rec) { OccurrenceRiskMillMult = occ; RecoverySpeedMillMult = rec; }
}

// KD-8 observer surface for #38 (value copies).
public readonly struct MedicalViewModel { /* severity / recovery-remaining / injury-count / available */ }
```

The **medical block** persisted under `MEDICAL_SAVE_FORMAT_VERSION` is, per club: each player's
`InjuryState` keyed by `PlayerId`. **No RNG cursor is serialized** — occurrence draws are
position-independent keyed draws (FR-MD-007), so there is nothing beyond `InjuryState` to persist. The set
tracks roster membership per FR-MD-025 (regen inserts, retiree removes).

`AdvanceMedicalDay` is the sole mutating entry point (FR-MD-003); `IsAvailable` and `MedicalViewModel`
construction are pure reads over an `InjuryState` value. See §3.

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | `InjuryState` coherence violated — `RecoveryRemaining > 0` while `Severity == None`, or `RecoveryRemaining == 0` while `Severity != None`, reaching a consuming seam | **Fail loud** — an invalid combination is a bug, not silently repaired (the #27 `SquadFileLoader` / #28 F4 precedent, FR-MD-021 sibling). |
| **F2** | `AdvanceMedicalDay` (or any consuming seam) invoked for a `playerId` with no `InjuryState` (a regen never inserted per FR-MD-025) | **Fail loud** — a missing state is a roster-lifecycle bug (the day-0 hazard), never defaulted. |
| **F3** | `MEDICAL_SAVE_FORMAT_VERSION` mismatch on restore | **Fail loud** (`InvalidOperationException`), the `MatchSaveCodec` posture — corrected from `ArgumentException` at #41 T1 (ERR-041-008), for the reason recorded in #29 §2.3 F3. |
| **F4** | An out-of-contract `InjurySeverity` value reaches a consuming seam | **Fail loud** — an invalid enum value is a bug, not silently clamped. |
| **F5** | Corrupt length prefix (out-of-bounds) or trailing bytes in the medical block | **Fail loud** (overflow-safe bound; the `WorldStateSerializer.ReadCount` posture). |
| **F6** | `AdvanceMedicalDay` invoked twice for one world day (`worldDay <= LastAdvanced`) | Idempotent no-op guarded by `LastAdvancedWorldDay` — a mid-recovery save→restore→re-run does not double-decrement or double-draw. |
| **F7** | `AdvanceMedicalDay` called with a **day gap** (`worldDay > LastAdvanced + 1`, post-sentinel) | **Fail loud** (`ArgumentException`) — a gap silently under-advances recovery and skips an occurrence evaluation; neither is clamped, defaulted, or batch-replayed (the FR-TR-026 posture). |
| **F8** | `AdvanceMedicalDay` invoked with `worldDay == MEDICAL_NOT_ADVANCED_SENTINEL` itself | **Fail loud** (`ArgumentException`) — the sentinel is a reserved value, not a day; stored, the cursor would read back "never advanced" and re-arm the day-0 double-accrual trap F6 closes. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial FR set (FR-MD-001..027), data structures, F1..F7. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): integer-arithmetic fix — `MedicalModifier` now per-mille int multipliers with an explicit `Identity` (default() invalid → F4 fail-loud); FR-MD-014 recovery-speed applied to assigned tier-days (not a per-tick multiply); FR-MD-016 zero-modifier fail-loud. |
| 0.3 | 2026-07-23 | — | AR-2 (1M): FR-MD-008 now mandates the fixed `DRAW_PURPOSE_RADIX` in `DeriveActionOrdinal` (append-parity — the growing purpose count as radix would shift prior ordinals). |
| 0.4 | 2026-08-06 | — | **ERR-041-008** (at #41 T1): §2.3 **F3**'s exception type corrected from `ArgumentException` to `InvalidOperationException`, matching the `MatchSaveCodec` posture the same row cites (the #29 §2.3 F3 sibling correction). |
| 0.5 | 2026-08-07 | — | **Balance pass D2 (ERR-041-010(b))**: FR-MD-010's `AppearanceDays` gains its unit — the count of days fielded in the `APPEARANCE_WINDOW_DAYS` `[GT]` window strictly BEFORE the draw day, never the current day (a match on day *d* first feeds the draw on *d+1*; #30's ERR-030-027 pre-round ordering depends on the exclusion). The record is #30-owned (its own `APPR` sub-blob), reaching #41 only as the caller-supplied value; the window `[GT]` is #41's because #41 owns what the input means. Previously the FR called `AppearanceDays` "a count #30's fixture result already tracks", which was false — no such record existed (that is what ERR-041-010(b) filed). |
| 0.6 | 2026-08-07 | — | **Balance pass D3/D4 (ERR-041-011 / ERR-041-012)**: FR-MD-027 ARMED — production constructs the dial ON, the argument is required-never-defaulted, the OFF identity stays supported and locked; FR-MD-005 re-anchored onto the keyed derivation (the `injuries.occurrence` registered stream never existed and may not — cursor-positioned, forbidden by FR-MD-006/007; ordinal 92 stays unallocated). (Rows 0.5/0.6 were appended out of order and swapped at the balance-pass AR pass 4 — the third table of this class in this landing chain.) |
| 0.7 | 2026-08-08 | — | **Balance-pass AR pass 6 (M4)**: §2.2's normative `AdvanceMedicalDay` signature de-phantomed — it took the `DeterministicRngService rng` that never existed and could not express the required FR-MD-027 dial or the `worldSeed` key root declared 30 lines above it (the §3.1 v0.5 fix, which stopped one section short); the `MatchLoad` comment's "a count #30's fixture result already tracks" corrected (ERR-041-010(b) built the record because it did not). (Rows reordered ascending at AR pass 8 — the third recurrence in this table's own history.) |
| 0.8 | 2026-08-08 | — | **Balance-pass AR pass 8 (L4 + L2)**: FR-MD-007's "for `injuries.occurrence`" → "for the keyed occurrence derivation" (true statement, phantom name); rows 0.6/0.7 reordered ascending — the third recurrence in this table's own history. |
| 0.9 | 2026-08-08 | — | **Balance-pass AR pass 9 (L4)**: new **F8** — `AdvanceMedicalDay` invoked with the never-advanced sentinel as `worldDay` itself fails loud; enforced in code since T0 with no F-row (a fail-loud with no normative source). §3.1's pseudocode gains the guard in the same commit; mirrored at #29 §2.3/§3.1. |
#endregion
