# Club Infrastructure & Facilities #53 — Outline

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Purpose

Spec #53 owns the **per-club facility model**: a fixed roster of facility types each carrying an integer
**level**, the **upgrade lifecycle** that raises those levels on the world tick, and the **projection of
levels into the value-input dials four already-approved specs declare**.

It exists because those four specs — #34, #42, #28, #40 — each name a facility model and each attribute
it to **#40**, whose own approved scope contains no such model. The producer is designed-for, named, and
does not exist. #53 is that producer.

It is deliberately **not** the owner of money (#40), of the decision to spend (the command layer), of
staff quality (#34), of any consumer's response curve (#42/#29/#41/#40), or of the stadium as a rendered
place (#48). #53 supplies levels and the values derived from them; other specs decide with them.

**Promoted from:** `docs/tracking/club-infrastructure-facilities-design.md` v0.4 (AR-1 0H+2M → AR-2
0H+2M → AR-3 0H+0M+2L, CONVERGENCE).

## Section map

| § | Content |
|---|---|
| 1 | Scope, out-of-scope table, dependencies + DAG, KD-1..KD-9, determinism posture, folded-in lessons |
| 2 | FR-IN-001..032, data structures, failure modes F1..F8 |
| 3 | FM-IN-01..05 — the startability check, the latch, the day advance, the level→dial projection, capacity; worked examples |
| 4 | Assembly, file layout, the command-layer purchase sequence, the root-assembled projection seams, save composition, reference contracts |
| 5 | Test plan — identity / units / determinism / save / seams / fail-loud / structural |
| 6 | Performance — world tick only, no hot path |
| 7 | T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6 |
| 8 | Cross-references XC-053-001..018, back-prop table |
| 9 | Approval checklist + gates |
| A | Constant catalogue, save layout, facility roster + dial-mapping table |

## Key decisions (summary — full text in §1.5)

- **KD-1** — #53 owns **levels**; #40 owns **money**; the **command layer** joins them in a pinned
  check → debit → latch order. The surface is split in two (`CanStartUpgrade` pure / `StartUpgrade`
  latch) precisely so that order is expressible without roll-back-on-failure.
- **KD-2** — A **fixed, APPEND-only** roster of exactly four facility types, one per **existing**
  consumer dial. Genesis is a **uniform baseline**, which is what keeps #53 outside
  `WORLD_GENERATION_VERSION`.
- **KD-3** — An upgrade is a **dated latch**: a stored `CompletionWorldDay`, never a remaining-days
  counter.
- **KD-4** — #53 projects into the **existing** dials; the **composition root** combines #53's term with
  #34's. #53 never pre-blends staff quality.
- **KD-5** — Persistence is #53's own opaque, independently version-gated sub-blob.
- **KD-6** — **Draw-free**: no RNG stream, no domain tag, no `SubsystemOrdinal`, and none of the
  roadmap §6 reserved slack consumed.
- **KD-7** — **No idempotency cursor.** #53's day advance is idempotent *by construction*, so it needs
  neither a `LastAdvancedWorldDay` field nor a day-gap guard — the one management spec where that is
  true, and stated as a decision so it is not "fixed" into existence later.
- **KD-8** — **Two identity conventions, not one.** `AcademyQuality`/`TrainingInput` are zero-identity;
  `MedicalModifier` is 1000-per-mille identity with a fail-loud `default()`. #53 returns each
  consumer's own form.
- **KD-9** — The **training-ground term feeds #29, not #28's `TrainingInput` directly** — #29 is the
  sole writer of that type (FR-TR-005).

## Back-props (land atomically at APPROVED)

| ID | Target |
|---|---|
| ERR-034-001 | #34 — re-attribute "#40 facilities" to #53 |
| ERR-042-001 | #42 — re-attribute "#40 facility spend" to #53's `YouthFacilities` projection |
| ERR-028-002 | #28 — name #53 as the facility producer behind the academy structure |
| ERR-040-002 | #40 — record that #53 owns facility state; #40's role is funding; `Stadium` capacity is the §7.2 attendance input |
| ERR-029-003 | #29 — record the #53 facility term as a second root-assembled input to `ComputeTrainingInput` (KD-9). **`-001` is filed and RESOLVED; `-002` is soft-reserved by #34** — verified, so `-003` is next free |
| ERR-030-020 | #30 — insert `AdvanceFacilityDay` into the pinned day-advance tick order |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial outline from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes: added **KD-7** (no idempotency cursor — a decision, not an omission), **KD-8** (the two identity conventions), **KD-9** (the training term feeds #29, not #28) and the resulting **ERR-029-003** back-prop row; section map cited `XC-053-001..014`, §8 defines **001..018**. |
#endregion
