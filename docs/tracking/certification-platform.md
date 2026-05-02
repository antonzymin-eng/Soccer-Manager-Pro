# Certification Platform Pin

**Created:** May 2, 2026  
**Purpose:** Records the exact Stage 0 host platform tuple for deterministic simulation certification runs, as required by Spec #16 §5.5.

---

## Status

**PLACEHOLDER — values MUST be pinned before the first certification run.**

This file must be updated with exact version pins before Spec #16 §5.5's `FR-DS-009-GATE` is activated for Stage 0. Until then, `section-5.md` §5.5 reads this as "to be pinned before first certification run" per its own note.

---

## Stage 0 Host Platform (to be pinned)

| Field | Required value | Pinned value | Status |
|-------|---------------|--------------|--------|
| OS | Windows 10 or 11 | _TBD_ | ⏳ Not pinned |
| Unity version | Unity 2022 LTS | _TBD_ (e.g. 2022.3.X) | ⏳ Not pinned |
| Backend | Mono or IL2CPP per project default | _TBD_ | ⏳ Not pinned |
| IL2CPP version | — | _TBD_ | ⏳ Not pinned |
| Compiler flag set | Deterministic flags (denormals-are-zero off, fp-contract off, fma off unless platform-pinned) | _TBD_ | ⏳ Not pinned |
| CPU architecture | x64 | x64 | ✅ Fixed |
| Worker thread count | Pinned (see §4.8 EnvironmentFingerprint) | _TBD_ | ⏳ Not pinned |
| SIMD feature level | Pinned (see §4.8) | _TBD_ | ⏳ Not pinned |

---

## Maintenance Rule

Update this file and check off all rows before the first certification run. A PR updating this file requires sign-off from the Platform Certification owner (see Spec #16 §1.7 Governance Artifacts).
