# Youth Academy & Intake #42 — Section 8: References & Cross-References

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** IN REVIEW

---

## 8.1 Cross-spec references (XC-042-*)

| ID | Direction | Surface | Contract |
|---|---|---|---|
| XC-042-001 | #42 → #28 | `RegenGenerator.GenerateRegen(rng, streamIndex, clubId, newPlayerId, worldDay)` | #42 calls it **unmodified** from its own stream (KD-1 / FR-YA-001). #28 is schema-untouched. |
| XC-042-002 | #42 → #28 | `PlayerLifecycle` (`PotentialAbility` / `CurrentAbility` / `BirthWorldDay`) | #42 shifts `PotentialAbility` only; `CurrentAbility` is a derived cache of `AbilityModel.ComputeCA` and is never written (KD-2 / FR-YA-004). |
| XC-042-003 | #42 → #28 | `PA_MIN`, `ABILITY_MAX`, `REGEN_PA_HEADROOM`, `REGEN_AGE_MIN/MAX`, `PROGRESSION_REGEN_FIELDS`, `DAYS_PER_YEAR` | Consumed read-only as `[CROSS]`; the clamp floor reproduces `RegenGenerator`'s own expression (FR-YA-005). |
| XC-042-004 | #42 → #27 | `PlayerRecord` / `PlayerAttributes` / `CLUB_SQUAD_SIZE` | Prospects are produced in #27's shape; `CLUB_SQUAD_SIZE` bounds promotion (FR-YA-025). #42 never writes a `Squad` (FR-YA-023). |
| XC-042-005 | #42 → #16 | `DeterministicRngService` (`RegisterStream` / `Reserve` / `DrawReserved` / `CloseReservation`) + the `0x2B` / ordinal-93 namespace | One `youth.intake` stream per academy club, registered lazily at first intake (FR-YA-018). |
| XC-042-006 | #42 → #16 (idiom) | `DeriveActionOrdinal(entityId, worldDay, purpose)` — #41 §3.1.1, with #41's own AR-2 fixed-radix guard | #42 adopts the keyed-anchor **property** (position-independence) rather than a free-running cursor (KD-7 / FR-YA-019/020). |
| XC-042-007 | #30 → #42 | The academy tick-order slot in `RunWorldTickInFixedOrder` | #30 invokes `AdvanceAcademyDay`; #42 never references #30 (KD-4 / FR-YA-013). Filed as **ERR-030-007**. |
| XC-042-008 | #30 → #42 | `SeasonSaveCodec` sub-blob composition + the outer `SEASON_SAVE_FORMAT_VERSION` bump | The academy blob is opaque to the outer codec; no `WORLD_STORE_FORMAT_VERSION` bump (KD-6 / FR-YA-028). T-phase. |
| XC-042-009 | root → #42 | `AcademyQuality` | Assembled by the composition root from #34 / #40 when those producers exist; `Neutral` until then. #42 references neither (KD-3 / FR-YA-009). |
| XC-042-010 | #42 → root | `IntakeResult` / `PromotionResult` | #42 emits; the root applies to the #27 `Squad` atomically (KD-5 / FR-YA-024). |
| XC-042-011 | #34 → #42 (deferred, producer side) | #34's published staff-quality / coaching projection | #34 already publishes it and **built no #42 interface** by design (FR-ST-021 / FR-LW-031); it reaches #42 only through XC-042-009. **No #34 change at #42's approval.** |
| XC-042-012 | #42 → #38 (deferred) | `AcademyViewModel` | Read-only observer; #38 renders it. No interface built by #42 (FR-LW-031). |
| XC-042-013 | #42 → #32 (deferred) | The prospect record | #32 does not exist; #42 builds nothing for it. |
| XC-042-014 | #42 ↔ #29 | **(negative contract)** | #42 exposes **no** growth modifier. Coaching reaches growth through #29 → #28 only; the #42 dial is a one-time intake ceiling (F7 / FR-YA-012). |

## 8.2 Back-props

| ID | Target | When | Change | Status |
|---|---|---|---|---|
| **ERR-030-007** | #30 §2 FR-SN-034 + §3.3 `RunWorldTickInFixedOrder` | **At approval** | Append the **academy null seam** as step 7 (after the staff seam, before the live world-day tick; `WorldStore.AdvanceDay()` → step 8), and extend FR-SN-034's enumeration + the "documented positions" prose. A **position reservation** — empty until #42 T2 (the ERR-030-002 / -004 / -006 precedent). Doc-only; the FR-SN-026 world-floor byte-identity is unaffected by a null seam. *(`ERR-030-005` is soft-reserved by #31's deferred `RequestRosterCommit`; `-006` is #34's — `-007` is the next free number.)* | **Pending — files at approval** |
| ERR-016-xxx | #16 §3.4 + `SubsystemOrdinals` | At T2 (first draw) | Promote the roadmap-§6 reservation to `DOMAIN_TAG_YOUTH_ACADEMY = 0x2B` / `SubsystemOrdinals.YouthAcademy = 93`. Spec-text-first (the ERR-030-001 / ERR-028-001 precedent) — the code const + registration land with the first draw site, never earlier (FR-LW-031). No `DETERMINISM_DIGEST_VERSION` bump. | Deferred |
| ERR-030-xxx | #30 `SEASON_SAVE_FORMAT_VERSION` | At T2 | Bump, composing the academy sub-blob. Exact version coordinated with whichever T-phase lands first. | Deferred |
| ERR-016-yyy | #16 `DeterministicRngService` | At T2, **conditional** | Add `SeekStream(int streamIndex, ulong actionOrdinal)` so the KD-7 anchor does not re-purpose `RestoreStream` (§4.4). Not required for correctness — the fallback is documented and the invariant is identical. #41 would be a second consumer. | Deferred / conditional |
| — | **#28** | **Never** | #28 is schema-untouched by design (KD-1 / KD-2 / §7.3). | N/A |

## 8.3 External references

#42 introduces **no external academic or industry citation**. Its formulas are integer projections over
constants owned by #28 and #27; the only literature-adjacent claim in scope — **bio-banding** — is
deliberately *not* pinned here (KD-2b / §7.4 R-2): the plan cites Master Vol 1, that source model has not
been confirmed, and pinning an age band against an unverified source would be exactly the fabricated-value
failure this project's §8 discipline exists to prevent. When the band is pinned at T3 it must cite the
confirmed source at that time.

**Internal governing documents:**

| Document | Role |
|---|---|
| `docs/tracking/youth-academy-intake-design.md` v0.3 | The AR-converged design supplement these files were promoted from. |
| `docs/tracking/spec-plans/spec-42-youth-academy-intake.md` v0.1 | The one-page plan the supplement was written against. |
| `docs/tracking/management-layer-spec-roadmap.md` v0.5 | Wave 5 placement; §6 determinism-block reservation (`0x2B` / 93). |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §8 (XC-042-001..014 incl. the XC-042-014 negative contract with #29, the back-prop table with ERR-030-007 as the sole approval-time item, and the explicit no-external-citation / unpinned-bio-banding rationale). Status IN REVIEW. |
#endregion
