# Steam Packaging & Release Engineering #39 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-039-001 | `.github/workflows/ci.yml` — the eleven jobs | The pipeline that exists. **No `BuildPlayer` anywhere in the tree**, so #39 introduces the first build pipeline (§1.4(a)). |
| XC-039-002 | `unity-tests` — `if: needs.unity-license-check.outputs.configured == 'true'` | **The skip-open gate.** *"cleanly SKIPPED (not failed) until then"*, in the job's own comment. |
| XC-039-003 | `unity-license-check` — emits `configured=false` + a **notice**, not a failure | The other half of §1.4(b): a green pipeline is compatible with **nothing having been built or tested**. |
| XC-039-004 | `certification-platform.md` **v1.4 — ✅ PINNED** | Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / 1 worker / DAZ·FTZ·fp-contract·FMA off. The tuple the certifying evidence must come from (FR-PK-017). |
| XC-039-005 | `cert-runs/determinism-cert-2026-07-19.md` | The KAT run — 44 passed / 0 failed / 4 deferred skips. **Evidence row 1.** |
| XC-039-006 | `kickoff-multi-second.cert.md` + `CertifiedPerfBaseline` | FR-PO-052 promoted **PENDING → CERTIFIED** (p50 = 0.4768 ms, p99 = 2.5669 ms). **Evidence row 2** — and the gate requires `Certified`, not `Pending`. |
| XC-039-007 | `cert-run-runbook.md` | The operator checklist #39's release runbook descends from (FR-PK-018) — **and the proof the pattern works**. |
| XC-039-008 | `cert-run-runbook.md`'s own wording on `tools/dotnet-ci` | *"explicitly NON-certifying … a number sourced from it would be a fabricated certification."* The reason the gate cannot run in CI (R-2). |
| XC-039-009 | `SeasonSaveCodec` — version-first frame, `matchPresent` flag, three length-prefixed sub-blobs it *"never parses"* | Why sync is **whole-file only** and why a mid-match save is **not separable** (FR-PK-033/034). |
| XC-039-010 | `SaveManager` / `MatchSaveManager` — `temp → fsync → rename` | The atomicity guarantee that keeps the cloud from ever observing a partial file (§5.3). |
| XC-039-011 | #50 `Classify(in header) → SaveClass` | **The only source of version judgement** (FR-PK-001). Reads version fields **inside** the file, which is why FR-PK-002 requires the fetch. |
| XC-039-012 | #50 `CompareForConflict(a, b)` — direction **`#50 → #39`** | **#50 already names #39 as its consumer**, which is why §8.2 files nothing. |
| XC-039-013 | #50 KD-5 | *"a save that is `TooNew` … **cannot be resolved by this build at all**"* and *"a migrated save is **written back only on the player's next explicit save**"* — both **inherited verbatim** (FR-PK-004/008). |
| XC-039-014 | #50 KD-2 `WORLD_GENERATION_VERSION` | Rosters are regenerated, not stored — so a save is reproducible only against the generator that wrote it. **This bites hardest at Cloud** (R-4), which is precisely the mechanism carrying a save between two builds. |
| XC-039-015 | #48 `ICueSink` / #50's generator registry | The **shell-inversion precedent** #39's shell-evaluated achievements follow — the third instance in this wave (§4.1). |
| XC-039-016 | #49 FR-LC-002 / 004 / 012 / 013 / 008a | The producer contract for conflict and refusal notices: no baked strings, a sibling boundary adapter, base-locale coverage. |

## 8.2 At approval — **none**

**#39 files no back-propagations.** This is a **positive finding rather than an omission**, and it is
worth stating as one:

- **#50 already specifies `CompareForConflict` with direction `#50 → #39`**, and already pins the `TooNew`
  and write-back-on-explicit-save rules (XC-039-012/013). #39 **fits an existing contract instead of
  amending one** — the same relationship #45 had to #40's pre-specified `BoardModifier`.
- **#49 is consumed through its documented producer extension point**, which is an extension point, not a
  change.
- **The certification machinery is consumed as evidence** (FR-PK-019). #39 defines no determinism proof
  and **re-pins nothing**.

**That this spec — the one that gates the whole project's ability to ship — amends no approved text is
the strongest available evidence that its layering is right.** A packaging spec that needed changes across
the tree would be a packaging spec that had reached into it.

**It is also the fourth such spec in this wave** (#37, #44, #46, #48 and now #39), which is worth noting
because the pattern is not luck: each is read-only, each consumes contracts rather than defining them for
others, and each sits at the edge of the graph.

## 8.3 Deferred — land at the named tier

- **The CI workflow addition for a player-build job**, at the first packaged build (T2) — infrastructure,
  not a spec amendment, and deliberately not written before an artifact exists to build.
- **The achievement identity set**, at the first release (T3) — content, and subject to FR-PK-028's
  APPEND-only rule once shipped.
- **Bit-identical packaging** (KD-6), **Stage 5+**, alongside #52.
- **Gate automation** of the input-side rows, as far as the host constraint permits (R-2).

## 8.4 Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#50 — nothing.** The dependency runs `#50 → #39`, and **duplicating classification in #39 is the
  specific error KD-1 exists to prevent**. This is the absence that matters most: a second version
  authority is the two-truths defect, and #39 is the spec most tempted by it, because it is the one
  holding two files at once.
- **#30 and the codecs — nothing.** #39 transports the file and parses nothing (FR-PK-033).
- **`certification-platform.md` — nothing.** #39 **consumes** the pin; it does not re-pin it, and a
  packaging spec that adjusted the certification tuple would be inverting the relationship.
- **#16 — no row, no `_RESERVED_` placeholder, nothing at all.** No stream, no tag, no ordinal
  (FR-PK-041). As with #46, #48 and #50, #39 therefore has **nothing to promote later**.
- **#49 — nothing.** An ordinary producer through the documented extension point.
- **The sim, in any form.** #39 references no sim assembly, and the one prohibition running the other way
  — no sim read of achievement state (FR-PK-027) — is a **requirement on the sim's authors**, asserted by
  a reverse-reference scan, not a change to any sim spec.
- **CI — nothing, deliberately.** `unity-tests`'s skip-open behaviour is **correct for CI** and #39 does
  not ask for it to change. The gate reads the same job's **artifact** with the opposite default, which is
  the entire resolution.

**That last row deserves its place.** The obvious "fix" for §1.4(b) is to make CI fail when the licence is
absent — which would make every contributor without secrets see red, for no benefit. **The defect is not
that CI skips; it is that a ship decision would read CI's answer.** #39 changes the reader, not the job.

## 8.5 References

#39 introduces **no external citation**. Its content is a gate, a policy and a checklist composed from
this project's own approved specs, its CI configuration, and its certification records; there is no
published result it rests on, and inventing a citation to decorate the section would be the fabrication
the project's rules forbid.

**The platform's own requirements are the one thing a reader might expect cited here**, and their absence
is deliberate. Store requirements, age-rating regimes and platform SDK contracts are **external, versioned
by someone else, and change without notice** — pinning specific clauses into an approved spec would
guarantee the spec is wrong at some future date while looking authoritative. #39 therefore specifies the
**checklist and its gate classes** (Appendix D) and leaves the current external requirements to be
satisfied against the platform's live documentation at release time. That is the same reason KD-4 splits
compliance from marketing rather than enumerating store fields.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-039-001..016, with 002/003 quoting the CI configuration at source since the skip-open finding is the spec's reason for existing, and 011..014 recording that #50 already specifies the whole contract #39 consumes; **§8.2 files nothing**, stated as a positive finding — a packaging spec that needed changes across the tree would be one that had reached into it — and noted as the fourth such spec in this wave; §8.4's last row records that #39 deliberately asks for **no CI change**, because the defect is not that CI skips but that a ship decision would read its answer; §8.5 records why platform requirements are deliberately not cited, since pinning externally-versioned clauses would guarantee the spec is wrong later while looking authoritative). Status IN REVIEW. |
#endregion
