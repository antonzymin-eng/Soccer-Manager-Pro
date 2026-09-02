# Code Standards & Style Guide Specification #20 — Section 8: References & Citation Audit

**File:** `docs/specs/code-standards/section-8.md`
**Purpose:** Registers all sources cited by Spec #20, records verification status for
every citation, audits cross-spec references, and summarises constant provenance.
**Created:** May 8, 2026
**Modified:** September 2, 2026
**Version:** 1.3
**Status:** AMENDMENT DRAFT (A3.1b; approved v1.2 baseline remains in force)
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 8; `outline-mid.md` v1.2, §8.1–§8.4
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.35, §6; A3.1b

---

## Table of Contents

- [8.1 Source Register](#81-source-register)
- [8.2 Verification Notes](#82-verification-notes)
- [8.3 Cross-Spec Citation Audit](#83-cross-spec-citation-audit)
- [8.4 Constant Provenance Summary](#84-constant-provenance-summary)
- [8.5 Version History](#85-version-history)

---

## 8.1 Source Register

Eight sources are cited by Spec #20. Each row lists the source, its canonical path or
URL, the retrieval/verification date, and the section(s) in which it is used.

| # | Source | Path or URL | Retrieved / verified | Used by § |
|---|---|---|---|---|
| S-01 | Root `CLAUDE.md` — project invariants (constant tags, determinism rules, interface principle, tick rates, coordinate system, fatigue convention) | `CLAUDE.md` (repo root) | May 8, 2026 — verified against current file text | §1.1, §1.2, §1.3 (Authority Matrix), §2.2.1–§2.2.8, §3.2.1 (verbatim tag table), §3.3–§3.7, §3.9, §4.1, §6.1, §7.3, §7.5, Appendix D |
| S-02 | `docs/planning/development-best-practices.md` — allocation budgets and hot-path guidance | `docs/planning/development-best-practices.md` | May 8, 2026 — file present; allocation budget figures (0 bytes/frame game loop, < 1 MB/frame UI) verified against current text | §3.3, §6.1 |
| S-03 | `docs/planning/master-development-plan.md` — Stage 0–Stage 6 definitions, Fixed64 scope decision | `docs/planning/master-development-plan.md` | May 8, 2026 — Stage 5+ scope for Fixed64 confirmed in current text | §1.1, §7.3 |
| S-04 | `docs/tracking/certification-platform.md` — Unity LTS version pin, C# version pin, compiler flags | `docs/tracking/certification-platform.md` | May 8, 2026 — file present; all Stage 0 rows confirmed `_TBD_` / `⏳ Not pinned` as of this date | §2.2.1 (FR-CS-008 INACTIVE condition), §3.7, §7.1 (D1-artifact, D5-artifact), §7.2 (merge gate note), §7.5 (D1, D5) |
| S-05 | Microsoft C# Coding Conventions | https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions | May 8, 2026 | §3.1.1 (naming), §3.1.4 (Allman brace style), §7.4 |
| S-06 | Unity Scripting API — Performance Best Practices | https://docs.unity3d.com/6000.0/Documentation/Manual/analysis.html | September 2, 2026 | §3.3 (zero-alloc patterns), §6.2 (virtual dispatch), §6.3 (ProfilerMarker) |
| S-07 | RFC 2119 — Key words for use in RFCs to Indicate Requirement Levels (MUST / SHOULD / MAY) | https://www.rfc-editor.org/rfc/rfc2119.txt | September 2, 2026 | §1.1 (RFC 2119 conformance statement), §2.2 (every FR conformance level) |
| S-08 | Microsoft Roslyn Analyzer documentation — `BannedApiAnalyzers`, `.editorconfig` severity levels | https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview?view=visualstudio | September 2, 2026 | §5.2 (analyzer gate), §7.1 (D2-artifact, D3-artifact), §7.2 (PR gate command), Appendix D (Stage 1 analyzer ID column) |
| S-09 | Project Architecture Governance v0.10 — property/applicability/review/evidence authority | `docs/planning/project-architecture-governance.md` | September 2, 2026 — approved v0.10 re-verified | §1.2–§1.4, §2.2.9, §3.5.6–§3.5.7, §4.4, §5.4.8–§5.5, §7.2, Appendix F |
| S-10 | Spec #19 Testing Strategy & Framework — proof classes, bounded substitutes and gate evidence owner | `docs/specs/testing-strategy/` | September 2, 2026 — approved owner re-verified | §1.2–§1.4, §3.5.6–§3.5.7, §5.4.8–§5.5, §7.2 |

**Note on external URL retrieval:** The four external URLs (S-05 through S-08) are
well-established, stable documentation pages from Microsoft, Unity Technologies, and the
IETF. They were identified as accurate targets on May 8, 2026. At every Spec #20
amendment, the re-verifier MUST re-confirm each URL is live and that the cited content has
not materially changed. The A3.1b pass rechecked all four on September 2, 2026: S-05 remains live; S-06 is repinned to the Unity 6.0 Optimization page after the legacy 2022.3 URL redirected; S-07 is repinned to RFC Editor after the IETF URL redirected; S-08 gains the current Visual Studio view qualifier.

---

## 8.2 Verification Notes

### CLAUDE.md Citations (S-01)

All direct CLAUDE.md citations in Spec #20 were re-verified against the current
`CLAUDE.md` text on May 8, 2026. Specific checks performed:

- **Constant tag table** (§3.2.1): reproduced verbatim from CLAUDE.md "Constant Tags"
  section; five tags (GT, EST, FIXED, DERIVED, CROSS) with definitions match current
  CLAUDE.md text exactly. Attribution line present. *(Superseded August 18, 2026,
  round-6 finding H6: root `CLAUDE.md`'s table holds SIX tags — `[CROSS-PENDING]` —
  and §3.2.1 now reproduces all six; see section-3.md v1.6. The May 8, 2026 record
  above is left as the check performed on its date.)*
- **Tick rates** (§3.2.2, §3.4.1): 60 Hz physics / 10 Hz tactical loop values verified
  against CLAUDE.md "Heartbeat Tick Rate." Both loops cited with their canonical ms
  equivalents (~16.67 ms, 100 ms).
- **Determinism rules** (§3.4): SplitMix64 requirement, `unchecked{}` scope rule, Python
  `& 0xFFFFFFFFFFFFFFFF` mask, `MatchClock` time source — all verified against CLAUDE.md
  "When Writing Code" and "Things That Have Gone Wrong Before."
- **Interface principle** (§3.5.3): "Write interfaces only when both sides are specified"
  — verified against CLAUDE.md "Interface Design Principle"; ERR-001 / ERR-004 citations
  confirmed present in CLAUDE.md at time of drafting.
- **Fatigue convention** (§3.2.1): 0.0 = rested, 1.0 = fatigued — verified against
  CLAUDE.md "Fatigue Convention."
- **Coordinate system** (§3.2.1): X goal-to-goal (0–105 m), Y touchline-to-touchline
  (0–68 m), Z vertical — verified against CLAUDE.md "Coordinate System" table; origin
  = corner (not pitch center) confirmed.

### Internal Document Citations (S-02 through S-04)

- `development-best-practices.md` (S-02): file present at specified path; allocation
  budget figures confirmed in current text on May 8, 2026.
- `master-development-plan.md` (S-03): file present; Stage 5+ scope for Fixed64 confirmed.
- `certification-platform.md` (S-04): file present; all rows `_TBD_` as expected for a
  Stage 0 draft; FR-CS-008 INACTIVE justification is consistent with this state.

### Re-Verification Cadence

Every Spec #20 amendment — regardless of scope — MUST include a re-verification pass for
all ten registered sources. The version history entry for the amendment MUST record "S-01 through
S-08 re-verified" or list any source that could not be re-confirmed (with reason).

---

## 8.3 Cross-Spec Citation Audit

### Spec #20 Cited By (Downstream)

Spec #20 is the governance meta-specification for code style. It is cited *by*:

- Every Stage 1+ C# source file (via the mandatory file header, FR-CS-056, which includes
  a spec-citation list per Appendix A).
- `src/CLAUDE.md` (D5-artifact) — when authored at Stage 1, it cites Spec #20 as the
  normative source for every convention it concretises (§4.5).
- All other Stage 0 spec authors who reference the constant-tag vocabulary, FR cross-
  reference IDs, or the Authority Matrix.

Spec #20 does not depend on being cited to function. The downstream citation chain is
informational; it establishes auditability.

### Spec #20 Cites To (Substantive — Upstream)

Spec #20 imports no physics/AI domain mechanics, constants, interfaces, or data structures. The A3 amendment does add two substantive governance relationships: (a) Project Architecture Governance (S-09) is the upstream authority for property admission, applicability, review/disposition and evidence boundaries used by FR-CS-074–081; and (b) Spec #19 (S-10) owns executable proof classes, bounded substitutes and gate evidence. Spec #20 remains authoritative for the code/integration rules themselves and binds these upstream governance decisions without copying their review/proof state machines.

### Pointer-Only Citations

Two numbered specs are referenced in Spec #20 as informational pointers only — no
normative rule depends on their content:

| Spec | Pointer location | Nature of reference |
|---|---|---|
| Spec #9 — Fixed64 Library | §3.7.3, §7.3, §7.5 (D4) | Future trigger: when Spec #9 ships, §3.7 gains a cross-reference and Appendix D `det-banned` expands. No current rule depends on Spec #9. |
| Spec #19 — Testing Strategy | §1.2–§1.4, §3.5.6–§3.5.7, §3.9.4, §5.4.8–§5.5, §7.2/§7.5 | Owner of executable proof classes, bounded substitutes and gate evidence used by the architecture amendment; framework selection remains #19-owned. Spec #20 does not reproduce that machinery. |

### `[CROSS]` Constants

Spec #20 **imports no `[CROSS]` constants**. It declares no physical constants of any
kind (see §8.4). There are no mirror entries in any constant catalogue associated with
this spec.

### `TBD-NORMATIVE` Placeholders

Spec #20 contains **no `TBD-NORMATIVE` placeholders**. This tag (introduced in Spec #16
§9.5) marks cross-spec citation rows whose normative sources have not yet reached `IN
REVIEW` status. Spec #20 has no cross-spec normative dependencies that would warrant this
tag — its only upstream authority is root `CLAUDE.md` (S-01), which is always current.

### Cross-Reference ID Inventory

Spec #20 uses no `XC-`, `FM-`, `EC-`, or `ERR-` cross-reference IDs in its body text.
The spec defines the *rules* for using these IDs (§3.6.5) but does not itself contain
substantive formula references or edge-case tables that would warrant them. The single
`ERR-` appearance in Spec #20 is a citation to CLAUDE.md entries (ERR-001, ERR-004) in
the context of the interface-proliferation known-hazard note — it is an informational
historical reference, not a normative cross-reference.

---

## 8.4 Constant Provenance Summary

Spec #20 **declares no physical constants** of any kind. It is a governance
meta-specification; its subject matter is code conventions, not game physics.

The constant tag vocabulary (`[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`,
`[CROSS-PENDING]`) is
governance metadata owned by root `CLAUDE.md` (S-01, "Constant Tags" section). Spec #20
§3.2.1 reproduces the tag table verbatim with explicit attribution — this is the one
sanctioned verbatim cite in the spec (KD-6 permits verbatim reproduction of the tag
definitions because they are definitional, not a banned-API list). It does not redefine
or extend the tags; the authoritative definition remains in `CLAUDE.md`.

Because Spec #20 declares no constants, the following standard audit items are
vacuously satisfied:

- No `[GT]` constants: N/A (no gameplay-tuned values in a governance spec).
- No `[EST]` placeholder constants: N/A.
- No `[FIXED]` physical-law constants: N/A.
- No `[DERIVED]` formula-derived constants: N/A.
- No `[CROSS]` mirror entries: N/A (§8.3 confirms no imports).
- No `[CROSS-PENDING]` constants: N/A (tag added to the citation table August 18, 2026 —
  see §3.2.1; a governance spec declares none).
- No constant catalogue file: N/A (no `CodeStandardsConstants.cs` exists or will exist,
  and no `src/code-standards/` folder exists in the live tree — §4.1's tree records this
  spec as producing no source files).

---

## 8.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 8, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 8 and `outline-mid.md` v1.2 §8.1–§8.4. All eight sources verified on drafting date. | — |
| 1.1 | June 15, 2026 | — | S-06 (Unity Performance Best Practices) URL re-pinned per the §8.4 retire-or-redirect rule: the unversioned `docs.unity3d.com/Manual/…` path drifts to Unity's latest manual and began returning HTTP 503 (CI Markdown link check, PR #169). Replaced with the 2022.3 versioned path matching the certified engine (`certification-platform.md` v1.2). Same page/content; retrieved-date refreshed. No normative spec text changed. | — |
| 1.1.1 | August 18, 2026 | Claude Code | **Header correction only — no content change.** `**Status:**` read `DRAFT` against `SPEC_INDEX.md`'s record of #20 as **APPROVED (May 11, 2026)**. Corrected as part of the sweep the `ERR-020-002` adoption began: that pass fixed the three section files it touched and left six siblings at DRAFT, which turned a uniform folder-wide staleness into a misleading distinction — six of ten sections reading as not-approved. The FR-CS-056/057 class. Dated August 18, 2026 (commit `98662909`, author date 2026-08-18T03:01 UTC) — a same-session continuation of work that began August 17, 2026 UTC and crossed midnight before landing. | — |
| 1.3 | September 2, 2026 | Codex | **A3.1b supporting-surface synchronization.** Registers Project Architecture Governance and Spec #19 as the architecture amendment's upstream authorities, replaces the obsolete "no substantive cross-spec citations" claim, expands the amendment re-verification set to ten sources, and repins S-06/S-07/S-08 after live URL verification. | PENDING — A3.4 |
| 1.2 | August 18, 2026 | Claude Code | **Adversarial-review round-6 findings H6 + H2/H5-adjacent.** H6: §8.4's tag-vocabulary enumeration extended from five tags to six (`[CROSS-PENDING]` — the root `CLAUDE.md` table this section attributes ownership to has held six tags while the reproduction chain in #20 held five; see section-3.md v1.6), and the vacuously-satisfied audit list gains the matching sixth N/A row. Consistency fix (the round-6 report cited this file's line 173 under H2 as "restates the retired framing" — the line holds no three-layer wording; what it held was the same stale "§4.1 tree diagram marks this folder empty at Stage 0" claim H5 catalogues): the §4.1 citation now states what §4.1 says post-v1.1 — the spec produces no source files and no `src/code-standards/` folder exists (verified August 18, 2026: `ls -d src/code-standards` fails). | — |

---

*End of Section 8 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
