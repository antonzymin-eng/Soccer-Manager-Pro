# Personalities, Morale & Squad Dynamics #33 — Section 8: References & Cross-References

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Cross-spec references

| ID | Target | Nature |
|---|---|---|
| XC-033-001 | #27 Squad/Player Data — `PlayerRecord`/`PlayerAttributes`, `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex` | player identity + attribute source (upstream, read) |
| XC-033-002 | #16 Deterministic Simulation — determinism namespace, world-tick `DeterministicRngService`, `_RESERVED_0x25_` / `SubsystemOrdinals 87` | determinism substrate (upstream; the reserved slot promotes at #33 T3) |
| XC-033-003 | #30 Season loop — `RunWorldTickInFixedOrder` slot 3 (FR-SN-034), FR-SN-017 producer-only gate, `SeasonSaveCodec` | invocation + save composition (upstream; #30 invokes #33) |
| XC-033-004 | #22 Living World — FR-LW-004 `PlayerEdge` read-only mirror, `WorldLoop` phase-2 read, FR-LW-032 activation gate, `T-LW-U-035` | **#33 is the producer** of the vol-2 §2.1 edge this consumes (this spec fills XC-022-002) |
| XC-033-005 | #28 Player Progression — season-boundary regen/retirement roster churn | roster-lifecycle lockstep (FR-HS-027) |
| XC-033-006 | #34 Staff (future) — non-identity mentoring pairing producer | deferred routing seam (KD-5) |
| XC-033-007 | #46 News/Inbox (future) — man-management morale write | the sole write-INTO-#33 consumer (deferred, KD-3) |

**Producer side of XC-022-002:** #22 §8's `XC-022-002` (`vol-2 §2.1 social graph | edge model + clique
threshold (FR-LW-004)`) names the vol-2 model this spec realizes. #33 is that producer; the `PlayerEdge`
scalar + the `> 0.6` clique threshold are supplied unchanged (§3).

## 8.2 Determinism-namespace note

`DOMAIN_TAG_HUMAN_SYSTEMS` / `SubsystemOrdinals.HumanSystems` = `0x25` / `87` (roadmap §6 off-pitch block) are
present as the `_RESERVED_0x25_` placeholder row in #16 §3.4. They **stay reserved** at this spec's approval
(minimal is draw-free, KD-6 — the #40 `_RESERVED_0x29_` precedent) and are promoted to a live tag + stream
`[CROSS: #16 §3.4]` only at #33 T3's first stochastic draw. They are **not** project constants declared in this
spec — they are #16's tag-namespace reservation.

## 8.3 Academic / design references

The personality-trait vocabulary (Professionalism / Ambition / Loyalty / Temperament / Determination) and the
morale/happiness (H-Gate confidence-vs-self-efficacy) shape derive from the project's **Master Volume 2**
human-systems design (the same source #22's relationship/arc layer was built against). No external academic
citation is load-bearing at the minimal tier — the model is a deterministic per-mille projection, not an
empirically-fitted formula; the `[GT]` coefficients are illustrative pending a Stage-2/3 balance pass (§9.2,
the #21 G2 precedent). Deep-tier empirical fitting (if pursued) records its citations at that stage.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §8 (XC-033-* cross-references, XC-022-002 producer side, determinism-namespace note, Master Vol 2 basis). Status IN REVIEW. |
#endregion
