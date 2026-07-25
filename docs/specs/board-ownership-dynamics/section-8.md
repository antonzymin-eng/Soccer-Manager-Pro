# Board & Ownership Dynamics #45 — Section 8: Cross-References & Back-Propagations

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — ERR-045-001 scope widened)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-045-001 | #30 FR-SN-015 | #45 consumes #30's committed "on track?" projection as its daily input. **#45 does not define that projection's semantics** — including a pre-first-fixture table (FR-BD-011). |
| XC-045-002 | #30 FR-SN-014 / §2.2 `BoardState` | #30 keeps `BoardObjective` and its evaluation; `JobSecurity` becomes a **derived band** over #45's confidence at T2 (ERR-030-009). |
| XC-045-003 | #30 §3.3 `RunWorldTickInFixedOrder` | #45's daily advance occupies **slot 8**; `AdvanceDay()` → 9 (ERR-030-008). |
| XC-045-004 | #30 §3.5 `RollToNextSeason` step (b') | #45's `BoardModifier` projection is read where #30 already invokes #40's `SettleFinances`. **No new insertion point** — #45 rides the one ERR-030-003 created. |
| XC-045-005 | #40 FR-FN-018 | `BoardModifier` is #40's type with `Identity` = `1000` per-mille and a fail-loud `default()`. #45 **consumes**, never re-declares (FR-BD-017). |
| XC-045-006 | #40 FR-FN-019 | #45 is the producer #40 named. **Satisfying an existing contract — not a #40 change.** |
| XC-045-007 | #40 FR-FN-025 | #40 fails loud for a `ClubId` with no finance entry. #45's `Try` projection (FR-BD-018) keeps the two lifecycles independent: an unmodelled board is not a missing club. |
| XC-045-008 | #40 §7 | #45 adds **no second budget-multiplier path**. |
| XC-045-009 | #33 §3.1 | `DriftPermille` shape borrowed; #45 declares its own and pins equivalence by test (KD-1, T-BD-U-010). **No assembly reference.** |
| XC-045-010 | #33 FR-HS-008 | The unadvanced sentinel is `uint.MaxValue`, **not** `0` — day `0` is a legal world day. |
| XC-045-011 | #33 FR-HS-024 | #33 already lists #45 among its deferred read-only morale consumers. #45's read lands at T3 as a **routed value** (FR-BD-016). |
| XC-045-012 | #33 §2.2 `HumanSystemsDayInput` | `BoardObjectiveDeltaPermille` exists today **with no producer**; #45 becomes it at T3, under the KD-7 one-day-stale contract. |
| XC-045-013 | #16 §3.4 | `_RESERVED_0x2D_` / `SubsystemOrdinals.BoardOwnership = 95` — **reserved, not promoted** (ERR-045-001). |
| XC-045-014 | #42 §7.4 R-1 | The shared `MaxRngStreams = 64` bound. #45's single-stream + keyed-ordinal model (FR-BD-022) **does not contribute to it at any tier** — a deliberate contrast, recorded so it is not "simplified" into a per-club model later. |
| XC-045-015 | #27 | `ClubId` only; #27's assembly stays schema-untouched. |
| XC-045-016 | #19 §3.1.4 | Test-ID prefixes; the §5.9 closed-loop scenario registration. |

## 8.2 Back-propagations

### At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-030-008** | `season-competition-loop/section-3.md` §3.3 + `section-2.md` FR-SN-034 | Board **null seam as tick-order step 8** (after #42's academy at 7); `AdvanceDay()` → step 9; FR-SN-034 enumeration extended to #45. Like #42's and unlike the #31/#34 deep-tier position reservations, this seam **goes live at #45's own T2**. | Doc-only re-pin |
| **ERR-030-009** | `season-competition-loop/section-2.md` FR-SN-014 + §2.2 `BoardState` + `section-3.md` §3.6 `WriteBoard` | `JobSecurity` becomes a **derived enum band** over #45's per-mille confidence rather than independent state (KD-5). Removes the layer's last float from a round-trip-deterministic block. **Carries a `SEASON_STATE_FORMAT_VERSION` bump** — #30-owned, effective at #45 T2. | ◑ Spec-text-first (the ERR-028-001 pattern): text at approval, effect + version bump at T2 |
| **ERR-045-001** | `deterministic-sim/section-3.md` §3.4 | **Three** placeholder rows — `_RESERVED_0x2B_` (#42, ordinal 93), `_RESERVED_0x2C_` (#43, 94), `_RESERVED_0x2D_` (#45, 95) — all **RESERVED, not promoted**. #45's minimal tier is draw-free, and a named tag over a zero-draw stream is the phantom surface FR-LW-031 forbids (the `_RESERVED_0x29_` #40 / `_RESERVED_0x21_` #29 precedent). **Why three and not one:** see below. | Namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump |

**Why ERR-045-001 covers `0x2B` and `0x2C` as well as #45's own `0x2D`.** #16 §3.4 carries an **A-04
rule — every allocation gap MUST have an explicit placeholder** — and the catalogue currently ends at
`DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` (#41). #42's approval (July 24) correctly deferred *promoting*
`0x2B`, but A-04 required a `_RESERVED_0x2B_` **placeholder** regardless, exactly as #29 and #40 have
one while staying unpromoted; that row was not filed, so `0x2B` is an unmarked gap today. Adding only
`_RESERVED_0x2D_` here would leave **two** unmarked gaps (`0x2B`, `0x2C`) and re-commit the precise
defect #16 v1.0.13 was written to fix, when the #40/#41 approvals allocated past the `0x22` block
without reserving `0x23`–`0x28`. Filing all three together closes the gap the same way v1.0.13 did —
retroactively and atomically — rather than leaving a known rule violation outstanding.

### Deferred — land at the named tier, **not** at approval

- Promotion `_RESERVED_0x2D_` → `DOMAIN_TAG_BOARD_OWNERSHIP` + the code const + stream registration, at
  the first takeover draw (**T3**).
- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at **T2** when the sub-blob is first composed in.
- The #33 morale read (**T3**) and #45 as producer of #33's board delta (**T3**) — both routed values.

### Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#40 — nothing to change.** FR-FN-018/019/027 and §7 already specify the `BoardModifier` seam, its
  identity, its fail-loud default, the `#45 → #40` direction, and the no-second-path rule. #45 fits the
  existing contract; a spec that arrives to find its downstream seam already written should not invent
  a change to prove it landed.
- **#33 — nothing to change.** FR-HS-024 already lists #45 as a deferred consumer, and
  `BoardObjectiveDeltaPermille` already exists. Both are #45-side wiring at T3.
- **#27 — nothing to change.** `ClubId` is consumed read-only.

## 8.3 References

#45 introduces **no external citation**. Its content is a state model composed from this project's own
approved specs; there is no published result it rests on, and inventing a citation to decorate the
section would be the fabrication the project's rules forbid. The §8.1 typed cross-references are the
authorities.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §8 (XC-045-001..016, the three approval-time back-props with ERR-030-009 marked ◑ spec-text-first, the deferred set, the explicit not-a-back-prop list for #40/#33/#27, and the no-external-citation rationale). Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | ERR-045-001 widened from `_RESERVED_0x2D_` alone to **three** placeholder rows (`0x2B` #42 / `0x2C` #43 / `0x2D` #45). Pre-approval verification against `deterministic-sim/section-3.md` found the catalogue ends at `0x2A`: #42's approval deferred *promoting* `0x2B` (correct, FR-LW-031) but omitted the A-04 **placeholder** (not correct — #29/#40 have one while unpromoted). Filing only `0x2D` would have left two unmarked gaps and re-committed the exact defect #16 v1.0.13 fixed. |
#endregion
