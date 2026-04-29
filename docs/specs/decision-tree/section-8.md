# Decision Tree Specification #8 — Section 8: References and Constant Governance

**File:** `section-8.md`  
**Purpose:** Record internal/external references and define governance for Decision Tree constant provenance and tuning lifecycle.  
**Created:** April 20, 2026  
**Version:** 1.0  
**Status:** ✅ APPROVED — Lead developer signed off April 27, 2026 (draft-level quality gate; see §9 approval checklist)  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

---

## Table of Contents

- [8.1 Internal Specification References](#81-internal-specification-references)
- [8.2 External/Academic References](#82-externalacademic-references)
- [8.3 Constant Classification Policy](#83-constant-classification-policy)
- [8.4 Tuning and Audit Procedure](#84-tuning-and-audit-procedure)
- [8.5 Reference Integrity Gates](#85-reference-integrity-gates)
- [8.6 Version History](#86-version-history)

---

## 8.1 Internal Specification References

Primary cross-spec dependencies:
- Ball Physics #1
- Agent Movement #2
- Collision System #3
- First Touch #4
- Pass Mechanics #5
- Shot Mechanics #6
- Perception System #7
- Decision Tree #8 (this spec)

Forward references:
- Fixed64 Math Library #9
- Heading Mechanics #10
- Event System #17

All spec numbers must match `docs/specs/SPEC_INDEX.md` canonical registry.

---

## 8.2 External/Academic References

These references inform model shape, not exact gameplay tuning values:

1. Utility-based decision architectures in game AI.
2. Sports decision-making under pressure and attentional control.
3. Deterministic simulation/replay design for agent systems.

DOI verification state must be tracked before final approval. Unverified citations cannot be marked as verified in Section 9.

---

## 8.3 Constant Classification Policy

Every constant must carry exactly one tag:
- `[GT]` gameplay tuned,
- `[EST]` estimated pending validation,
- `[FIXED]` physical law/fixed convention,
- `[DERIVED]` computed from other constants.

No untagged constants are permitted.

---

## 8.4 Tuning and Audit Procedure

1. Baseline with Stage 0 defaults.
2. Execute Section 5 balance scenarios.
3. Tune `[GT]` constants only.
4. Re-run determinism/performance tests after each tuning batch.
5. Log changed constants and rationale in version history.

---

## 8.5 Reference Integrity Gates

Before section sign-off:
- confirm all cross-spec references resolve to existing files/sections,
- confirm no stale spec numbering (e.g., Decision Tree incorrectly cited as #7),
- confirm all external citations are either DOI-verified or clearly marked unverified.

---

## 8.6 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | April 20, 2026 | Claude (AI) / Anton | Initial draft of Section 8 references and constant governance. |

---

*End of Section 8 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
