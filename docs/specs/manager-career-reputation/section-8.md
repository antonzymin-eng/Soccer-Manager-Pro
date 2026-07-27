# Manager Career, Reputation & Job Market #54 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-054-001 | #45 **FR-BD-012** | *"#45 MUST NOT expose a sacking API, and MUST NOT fire any event that terminates a manager. It supplies confidence; **#30 decides**."* **The orphaned MUST** — #45's posture is correct; only the counterparty's name is wrong (ERR-045-002). |
| XC-054-002 | #45 §1.5 KD-3, `outline.md`, `appendices.md` | The same assignment repeated three more times, including *"#30 owns what a band means for the sacking decision"*. All four sites are re-pointed by ERR-045-002. |
| XC-054-003 | #45 `BoardConfidence` / `JobSecurityBand` | Consumed as a **routed integer**; #54 names neither type. The confidence value is #45's single truth and #54 is a **reader** (FR-MC-009). |
| XC-054-004 | #45 **FR-BD-005a** / **F4a** | `{BoardConfidence, OwnershipProfile}` must be inserted as a **factory-built pair, guarded at insertion**, because `default(BoardConfidence)` is field-**in-range** yet means the `Critical` band with a no-op day-0 guard. **The trap the appointment path must not walk into** (KD-4 / FR-MC-017). |
| XC-054-005 | #45 KD-2 | One subsystem-wide stream with keyed action ordinals, never one per club — the rule #54's S3 draw adopts (FR-MC-026), because `MaxRngStreams = 64` is a shared ceiling. |
| XC-054-006 | #30 `SeasonState` constructor | Throws when `managedClubId` is not in the club set — **verified in source**. The fact that makes an unemployed manager **structurally unrepresentable** (ERR-030-021). |
| XC-054-007 | #30 Appendix B row 3a / `ERR-030-011` | `managedClubId i32` is a **mandatory** field, whose omission from the §3.6 pseudocode was filed as a defect precisely because a season cannot be reconstructed without it. |
| XC-054-008 | #30 `RoundResolutionMode` | The capability that lets a season advance with **no** managed club — **it already exists and is tested**. What does not exist is a season state that can express the configuration (KD-4/KD-5). |
| XC-054-009 | #30 §3.3 tick order / §3.5 boundary roll | Where the tenure evaluation slot lands (ERR-030-021), filed **at approval** because the order is a pinned sequence — the `ERR-030-008` / `ERR-030-020` precedent. |
| XC-054-010 | `ERR-030-009` | Turned #30's independent `JobSecurity` scalar into a **derived band** over #45's confidence, because two truths for one quantity *"diverge at the first restore with nothing to detect it."* **The lesson KD-2 applies pre-emptively to reputation** — and the queued bump §7.4 R-2 recommends combining with. |
| XC-054-011 | #22 `WorldLoop` phase-5 `BackgroundTierSim` | A **documented null**, unbuilt because it *"summarises club-AI / transfer / **sacking** outcomes that do not exist yet"* (FR-LW-031). The deep-tier producer of rival managers; #54 must not build its consumer (FR-MC-020). |
| XC-054-012 | #22 §7 reputation-persistence extension | Anticipates reputation surviving beyond a single career. #54's **APPEND-only** career record is the durable substrate it needs — a #22 read, not a #54 change. |
| XC-054-013 | #26 `ManagerProfile` / `ManagerMode` | **A different "manager" entirely** — a per-team in-match tactical AI in `src/match-engine`. #54 must not reuse either name (FR-MC-007); the **foreseen** third CS0104 instance. |
| XC-054-014 | #40 / #53 / #27 | Vacancy attractiveness reads **root-supplied values** — the value-input pattern #42/#29/#53 already use. **No references, no spec changes** (FR-MC-022). |
| XC-054-015 | #36 `NATION_TEAM_ID_BASE` | An S5 national-team appointment is a tenure whose `ClubId` comes from #36's **disjoint reserved range** — one reason that range is disjoint. Deferred; no #36 change. |
| XC-054-016 | #19 §3.1.4 | Test-ID prefixes; the §5.11 closed-loop scenario registration under `SCENARIO_PATH_CROSS_SPEC_PREFIX`. |

## 8.2 At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-045-002** | `board-ownership-dynamics/section-2.md` FR-BD-012 + `section-1.md` KD-3 + `outline.md` + `appendices.md` | **(i)** All four sites name **#30** as the spec that decides the sacking. **Re-point to #54.** #45's own posture is **unchanged and still correct** — it exposes no sacking API and fires no terminating event; only the counterparty's identity is wrong. **(ii)** **Confirm** that `FR-BD-005a`'s factory-built pair insertion remains available **mid-career**, not only at world genesis — #54's appointment path depends on inserting a `{BoardConfidence, OwnershipProfile}` pair for a club the manager has just joined (§4.4). If #45's store is genesis-populated for every club, this reduces to a no-op and the back-prop records that instead. (`ERR-045-001` is filed; `-002` is next free — verified.) | Doc-only re-point + a confirmation |
| **ERR-030-021** | `season-competition-loop/section-2.md` §2.2 + `section-3.md` §3.3/§3.5 + Appendix B row 3a | **(i)** Record that **#54 owns tenure and termination**, and add the tick-order/boundary slot invoking `EvaluateTenure`. Filed at approval because #30's order is a **pinned sequence** (the `ERR-030-008` / `ERR-030-020` precedent) — a step whose position is decided later is a step whose ordering was never reviewed. **(ii)** Make `ManagedClubId` an **explicit optional** so an unemployed career is representable — carrying a **`SEASON_STATE_FORMAT_VERSION` bump**, and **to be combined with `ERR-030-009`'s queued bump on the same block if the tiers align**, so existing saves face **one** refusal boundary rather than two. (Proposed `ERR-030-*` ids across the pre-promotion supplements reach `-020` (#53); `-021` is #54's — verified.) | ◑ Spec-text-first: the slot and the text at approval, the representation change and its version bump at T2 |

**The (ii) half of ERR-030-021 is the one non-additive consequence of #54's approval**, and it is stated
here rather than buried: changing `ManagedClubId`'s representation makes pre-bump saves **unloadable**,
with **no migration path** (§4.6). That is the same posture #45's ERR-030-009 already carries for the same
block, which is exactly why combining them is the recommendation.

## 8.3 Deferred — land at the named tier, **not** at approval

- **`_RESERVED_0x2E_` / ordinal 96 → a named tag** in #16 §3.4, **only** when the S3 job-market draw
  exists (FR-MC-025 adds the *placeholder* at approval; the *promotion* waits for a real draw site).
- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at **T2** when the career block is composed in.
- **Rival managers** via #22's phase-5 `BackgroundTierSim` (S5) — a **producer swap behind #54's vacancy
  surface**, not a redesign, and **not a #22 change #54 files**.
- **Manager personality** via #33, and **international appointments** alongside #36 — both S5, both
  arriving as routed values.
- **#31's** reputation-influenced negotiation, if ever wanted: a **value input**, never a reference.

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#45's confidence model — untouched.** #54 is a **reader**; the drift, the bands, the store and the
  one-directional posture all stand exactly as approved. ERR-045-002 changes **a name and asks a
  question**; it changes no mechanic. This is the distinction worth preserving: the spec that was *right*
  should not be edited to accommodate the spec that was *missing*.
- **#40 / #53 / #27 — nothing.** Vacancy attractiveness reads root-supplied values; no spec changes and
  no references (FR-MC-022).
- **#22 — nothing.** Phase-5 is already a documented null seam with a stated reason. #54 does **not** file
  a change to make it produce rival managers; that is #22's own deep-tier work, and #54 is written so its
  vacancy source can be replaced when it lands.
- **#26 — nothing.** A different "manager". #54 **avoids the names** rather than amending #26 (FR-MC-007)
  — the cheaper and less invasive of the two available fixes.
- **#16 — no named tag.** Only a `_RESERVED_0x2E_` **placeholder** row at promotion (FR-MC-025), per the
  #40 `_RESERVED_0x29_` / #29 `0x21` precedent. Naming a tag for a tier that draws nothing is the phantom
  in its determinism form.
- **#36 — nothing.** An S5 national-team tenure reuses #36's existing disjoint id range; no #36 surface
  changes.

## 8.5 References

#54 introduces **no external citation**. Its content is a lifecycle model, a projection, and a set of
boundaries composed from this project's own approved specs; there is no published result it rests on, and
inventing a citation to decorate the section would be the fabrication the project's rules forbid.

Note in particular that the **reputation weights are not a citation surface**: `MC_REP_PER_SEASON`,
`MC_REP_PER_TROPHY` and the `EndReasonTerm` table are `[GT]` tuning values authored for balance, not
claims about how real managerial reputations are formed. §5.5 asserts their **shape** — monotonicity, sign
symmetry, the clamp — and never their magnitude, so the balance pass cannot invalidate a passing suite.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-054-001..016, the two approval-time back-props with ERR-030-021 marked ◑ spec-text-first and its non-additive half stated at the table rather than buried, the deferred set, the not-a-back-prop list — which leads with the point that #45 was *right* and should not be edited to accommodate the spec that was *missing* — and the no-external-citation rationale extended to the reputation weights). Status IN REVIEW. |
#endregion
