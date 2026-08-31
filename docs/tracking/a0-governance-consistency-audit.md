# A0 — Governance Consistency Remediation Audit

> **Created:** August 31, 2026\
> **Purpose:** Durable evidence for the one systematic consistency-remediation pass directed after
> A0 adoption review round 5. This is a non-normative audit record. The governing source remains
> [`project-architecture-governance.md`](../planning/project-architecture-governance.md).
> **Subject:** Governance v0.8 at audit start; Governance v0.9 after the systematic remediation; Governance v0.10 after hostile-review closure.\
> **Related records:** [`a0-governance-adoption-review.md`](a0-governance-adoption-review.md) and
> [`project-architecture-governance-integration-plan.md`](../planning/project-architecture-governance-integration-plan.md).

---

## 1. Scope and method

This was one remediation pass, not another selective point-fix round. It checked the live
Governance body, appendices, approval checklists, current integration-plan representations, and the
A0 review record's current assertions. Historical version-history prose was retained as history and
was not treated as a current enum or schema source.

The pass used four deterministic inventories:

1. all 47 `FR-AG-*` registry rows were enumerated and mapped to their elaborating section(s);
2. each requirement's governing modal verb was compared with every elaborating modal statement;
3. every current representation of a schema, enum, transition, or field set was compared with its
   normative source; and
4. every normative use of `runtime component` / `runtime-bearing component` in the Governance domain
   and integration plan was enumerated.

No alias was accepted silently. A template either reproduces the governing field/value exactly or
states its governing source and is not used as a schema source.

---

## 2. Settled finding model

The authoritative model is:

| Axis | Values | Meaning |
|---|---|---|
| Disposition | `Blocker`, `Accepted Tradeoff`, `Residual Risk`, `Candidate Property` | Exactly one handling path for each substantive finding. |
| Status | `Open`, `Resolved`, `Accepted`, `Recorded`, `In property process` | Lifecycle state. A terminal Status follows from the selected Disposition. |

`Resolved` is a Status, never a Disposition. `Dispositioned` is not a Status. A finding with
Disposition `Blocker` blocks convergence only while its Status is `Open`; its severity does not select
the Disposition.

---

## 3. Exhaustive FR-AG modality matrix

`Exact` means the elaboration now states the same controlling modality or a stricter compatible one.
`Registry only` means the cited section defines state or process detail without restating a competing
modal rule.

| FR | Elaborating section(s) compared | Result |
|---|---|---|
| FR-AG-001 | §3.1, §3.4 | Registry only; lifecycle and admission decision add no competing modality. |
| FR-AG-002 | §3.2 | Exact Candidate-admission gate. |
| FR-AG-003 | §3.4 | Exact: existing authority is checked and cited instead of duplicated. |
| FR-AG-004 | §3.3, §7.1, Appendix A | Exact: stable ID is mandatory; `AP-###` remains recommended. |
| FR-AG-005 | §3.2 AC-6, §3.3 | Exact single-owner requirement. |
| FR-AG-006 | §3.2 AC-4, §3.3 | Exact defined-scope requirement. |
| FR-AG-007 | §3.2 AC-5, §3.3 | Exact evidence-model requirement. |
| FR-AG-008 | §3.2 AC-7, §3.3, Appendix E | Exact `Machine` / `Hybrid` / `Judgment` classification. |
| FR-AG-009 | §4.1–§4.2, Appendix B, Appendix F | Exact four-value Disposition enum. |
| FR-AG-010 | §4.3 | Exact prohibition on preference-only Blockers. |
| FR-AG-011 | §1.6, §4.2–§4.3 | Exact mandatory Blocker authority/failure citation; review-gate use is limited to a durably recorded, owner/existing-authority-authorized gate that cannot be self-authorized or retroactively invented by the reviewer. |
| FR-AG-012 | §4.4 | Exact MUST NOT on waiving an admitted MUST-level property. |
| FR-AG-013 | §4.5 | Exact MUST NOT on concealing missing required evidence. |
| FR-AG-014 | §4.6 | Exact MUST NOT on independent Candidate-Property blocking. |
| FR-AG-015 | §4.7, §9.6, Appendix F | Exact property-based convergence condition. |
| FR-AG-016 | §4.7, §9.6, Appendix F | Exact no-open-Blocker condition. |
| FR-AG-017 | §4.1–§4.2, §4.7, §9.6 | Exact complete-Disposition plus mapped-terminal-Status condition; every `Open` finding prevents convergence. |
| FR-AG-018 | §4.7, §9.6 | Exact fresh-current-artifact condition. |
| FR-AG-019 | §4.7 | Exact round budget cannot grant approval. |
| FR-AG-020 | §4.7, §9.6, Appendix F | Exact open-Blocker result is `NON-CONVERGED`. |
| FR-AG-021 | §1.6, Appendix C | Exact canonical runtime-bearing integration-owner requirement. |
| FR-AG-022 | Appendix C | Exact owner/integration-point requirement; placeholders are insufficient. |
| FR-AG-023 | §1.6, Appendix C | Exact construction, activation, update/use, and teardown ownership. |
| FR-AG-024 | Appendix C | Exact testhost/tool/alternate-path coverage. |
| FR-AG-025 | Appendix C | Exact prohibited-or-classified bypass coverage. |
| FR-AG-026 | registry definition, §3.3, §5.3, §7.1 | Exact complete finite-surface inventory unless the surface is explicitly within recorded Non-scope or covered by a §7.1 exception. |
| FR-AG-027 | §1.6, §5.3 | Exact structural-reachability requirement. |
| FR-AG-028 | §5.4 | Exact lifecycle/order evidence requirement. |
| FR-AG-029 | §5.5 | Exact meaningful-failure-path exercise requirement. |
| FR-AG-030 | §5.2, §5.6 | Exact triggered targeted-mutation requirement. |
| FR-AG-031 | §5.7 | Exact current-material-state requirement. |
| FR-AG-032 | §5.7, §6.5 | Exact independent reproducibility requirement. |
| FR-AG-032A | §5.7 | Exact material-dependency regeneration/revalidation trigger. |
| FR-AG-033 | §6.4 | Exact prohibition on human-effort-only sampling. |
| FR-AG-034 | §6.5 | Exact inventory/check/equivalent-evidence requirement. |
| FR-AG-035 | §6.1 | Exact SHOULD promotion to reliable automation. |
| FR-AG-036 | §6.2 | Exact retained-judgment requirement. |
| FR-AG-036A | §6.6, Appendix E | Exact governance-tool verification requirement. |
| FR-AG-036B | §3.2 AC-8, §5.2 | Exact computational-proportionality prohibition and bounded-proof disclosure. |
| FR-AG-037 | §3.5 | Exact repeated-Candidate review SHOULD. |
| FR-AG-038 | §3.4 | Exact existing-authority check before promotion. |
| FR-AG-039 | §7.1 | Exact recorded-exception mechanism. |
| FR-AG-040 | §7.5 | Exact no-silent-retirement requirement. |
| FR-AG-040A | §7.6 | Exact material-premise/scope/proof/enforcement reconsideration trigger. |
| FR-AG-040B | §5.6 | Exact process capability to retire obsolete/equivalently protected mutation obligations. |
| FR-AG-040C | §6.6 | Exact non-recursive ordinary-verification terminal boundary. |
| FR-AG-040D | §5.7 | Exact precise dependency-surface requirement. |

---

## 4. Schema, enum, transition, and template audit

| Normative set | Canonical source | Representations checked | Result |
|---|---|---|---|
| Property states and six legal transitions | §3.1 | §3.3 State, Appendix A, Appendix F | Aligned; Appendix F preserves all six transitions. |
| Property-record field set | §3.3 | Appendix A | Aligned; Appendix A now uses the exact 15 field labels. |
| Enforcement-class enum | FR-AG-008, §3.2 AC-7, §3.3 | Appendix A, Appendix E | Aligned: `Machine` / `Hybrid` / `Judgment`. |
| Finding Disposition enum | FR-AG-009, §4.1–§4.2 | Appendix B, Appendix F, integration plan §§3.8/8/A0 | Aligned: exactly four values; `Resolved` removed from all live enum sites. |
| Finding Status enum and terminal mapping | §4.1–§4.2 | Appendix B, Appendix F, integration plan §8 | Aligned: five Status values; no `Dispositioned` pseudo-state; for each Disposition only `Open` or its mapped terminal Status is legal, and convergence requires the terminal Status. |
| Finding-record fields | §4.2 | Appendix B, integration plan §3.8 | Aligned; the plan preserves all 11 required machine-field equivalents and may add ledger metadata. |
| Integration ownership/lifecycle contract | FR-AG-021–025 | Appendix C, integration plan §§3.4 and 6.1 | Aligned; `runtime-bearing component` and `teardown` are canonical. |
| Proof dependency surface | FR-AG-031/032/032A/040D, §5.7 | Appendix D, integration plan §3.7 | Aligned; static, alternate, and bypass paths are separate entries. |
| Review convergence state | FR-AG-015–020, §4.7 | §9.6, Appendix F, integration plan §8 and A0 | Aligned; an open Blocker is `Disposition: Blocker` plus `Status: Open`. |
| Exception record fields | §7.1 | No duplicate template | One normative representation only. |

---

## 5. Corrections made in the single batch

| ID | Source | Correction |
|---|---|---|
| SC-01 | AG-A0-015 | Settled the four-Dispositions/five-Statuses model; removed `Resolved` from FR-AG-009, §4.2, and Appendix B. |
| SC-02 | AG-A0-016 | Defined `runtime-bearing component` and replaced every live normative `runtime component` use, including downstream proposed FR-CS-076. |
| SC-03 | AG-A0-017 | Made §5.7's invalidation trigger an explicit FR-AG-032A MUST. |
| SC-04 | AG-A0-018 | Made §6.6's ordinary-verification terminal boundary an explicit FR-AG-040C MUST. |
| SC-05 | AG-A0-019 | Restored direct FR-AG-033, FR-AG-034, and FR-AG-036 modalities in §§6.4, 6.5, and 6.2. |
| SC-06 | AG-A0-020 | Changed Appendix B `Round` to exact `Round introduced` and added its governing-schema statement. |
| SC-07 | AG-A0-021 | Split Appendix D static, alternate, and bypass path fields. |
| SC-08 | AG-A0-022 | Changed Appendix C `Shutdown/disposal owner` to canonical `Teardown owner` and documented its governing FR range. |
| SC-09 | Systematic audit | Removed the undefined `Dispositioned` pseudo-status and defined the status/disposition transition contract. |
| SC-10 | Systematic audit | Made Blocker citation mandatory in §4.2/§4.3 and restored the direct FR-AG-010–014 elaborations. |
| SC-11 | Systematic audit | Added §4.7 so FR-AG-015–020 have one coherent elaboration rather than only registry/checklist echoes. |
| SC-12 | Systematic audit | Restored direct modalities for FR-AG-026/027/030–035/037–040D where the audit found no explicit elaborating anchor. |
| SC-13 | Systematic audit | Normalized the integration plan's finding field set, state model, A0 condition, canonical term use, and current governing-version reference. |
| SC-14 | Systematic audit | Replaced Appendix A paraphrased field labels with exact §3.3 labels and corrected Appendix B `Requirement/Property` spelling and field order. |
| SC-15 | Final coherence check | Added the narrow pre-adoption/review-gate Blocker basis so an A0 corrective finding is never classified from severity alone. |
| SC-16 | Hostile follow-up | Made the Disposition/Status transition contract normative: only `Open` or the mapped terminal Status is legal; all findings must be terminal before convergence; same-review Candidate admission forces applicability recomputation. |
| SC-17 | Hostile follow-up | Corrected FR-AG-026's inverted Non-scope wording: the exclusion applies when a surface is explicitly **within** recorded Non-scope, not outside it. |
| SC-18 | Hostile follow-up | Defined review-gate authorization: durable pre-existing record, project lead/owner or existing-authority authorization, scoped closure condition, current applicability, and no reviewer self-authorization/retroactive invention. |

---

## 6. Mechanical exit criteria

The post-remediation checks must confirm all of the following before the fresh adoption review is
considered complete:

- 47 and only 47 live `FR-AG-*` registry headings;
- identical four-value Disposition sets in FR-AG-009, §4.2, Appendix B, and Appendix F;
- identical five-value Status sets in §4.2 and Appendix B, with the same terminal mapping in Appendix
  F and integration-plan §8, plus rejection of every pairing other than `Open` or the mapped terminal Status;
- exact field labels in §3.3/Appendix A and §4.2/Appendix B;
- no live normative `runtime component` use; the sole retained phrase is the §1.6 non-normative
  statement that it has no distinct meaning;
- separate static, alternate, and bypass dependency entries in Appendix D; and
- no stale current A0 or governing-version assertion in the integration plan.

The fresh adoption review records the final result of those checks against Governance v0.10. It does not
approve the document; human sign-off remains required by the A0 gate.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.1 | August 31, 2026 | — | Hostile-review closure over v0.9: adds SC-16–SC-18, corrects the FR-AG-026 audit result, makes state-pair validity and terminal convergence explicit, and records the bounded review-gate authorization contract. Final checked subject is Governance v0.10. |
| 1.0 | August 31, 2026 | — | Initial systematic remediation audit over Governance v0.8 → v0.9 and the current integration-plan representations. |
