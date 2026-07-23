# Localization & Accessibility #49 — Appendices

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue (`LocalizationConstants`)

The localization layer consumes content (catalogues) and seam types, not numeric tuning tables.

| Constant | Tag | Value | Meaning |
|---|---|---|---|
| `BaseLocale` | `[FIXED]` | `en` | the identity locale — today's English strings; every fallback resolves here (KD-5) |

No `[GT]`/`[EST]`/`[DERIVED]`/`[CROSS]` constants — the seam has no numeric knobs. `EventKind` /
`InteractionIntent` (the template/clause keys) are #22-owned enums consumed read-only at the renderer
boundary, not re-declared here.

## Appendix B — Producer-emission → renderer-input mapping

The producer emits only its own native values; the **per-producer boundary adapter** (`LivingWorldTextBoundary`,
§2.2.1 — the only sim-side reference; the generic core references nothing sim-side) assembles the generic
`LocalizedTextRequest` (KD-6 / FR-LC-013/014). For the one built producer (#22):

| Producer native value (`living-world`) | Renderer input (`LocalizedTextRequest`, assembled by the boundary adapter) |
|---|---|
| `InteractionIntent intent` | `Id = LivingWorldTextBoundary.ForInteraction(intent)` → generic `TextTemplateId (producerTag, localOrdinal)` |
| `ulong draw` (the `world.text` reservation) | `SelectionDraw = draw` (verbatim, FR-LC-020) |
| `InteractionSlots.SubjectName` / `.OpponentName` | named slots `subject` / `opponent` in the producer-agnostic `NamedSlotSet` |
| `InteractionSlots.HomeGoals` / `.AwayGoals` | numeric slots `home` / `away`; **`{score}` is DERIVED at render** as `"{home}-{away}"` via InvariantCulture (§3.5), not a pre-formatted string |
| `InteractionSlots.HasCitedEpisode` | `HasCitedEpisode` |
| `InteractionSlots.CitedEpisode.Kind` | `CitationKind` (the `EventKind` clause key; selects the clause, not the draw — FR-LC-010) |

The renderer then computes `variant = draw % variantCount(BaseLocale, Id)`, expands the localized template,
and appends the localized clause when `HasCitedEpisode` (§3.2). Future producers (#35/#46) add a **sibling
adapter** (`MediaTextBoundary`, `InboxTextBoundary`) with their own native slot mapping — the generic core
seam is unchanged (§7.3).

## Appendix C — The #22-retrofit before/after byte-identity table (FR-LC-016 / §8 anchor)

For a fixed `(intent, draw, slots)`, base locale only:

| Step | Today (`InteractionTextGenerator`) | After (`ILocalizer.Render` + retrofit) | Identical? |
|---|---|---|---|
| Pre-draw validation | intent/slots/salience gates in `Generate`, pre-draw | **same gates**, stay in `living-world` pre-draw (§3.4) | yes (no-cursor-on-refusal preserved) |
| The draw | one `world.text` reservation | **same** reservation, in `living-world` | yes (cursor advances by 1 either way) |
| Variant count | `TemplatesFor(intent).Length` | `variantCount(BaseLocale, Id)` (migrated corpus preserves counts) | yes |
| Variant index | `draw % (ulong)length` | `draw % (ulong)variantCount` | yes |
| Template | the English template row | the **migrated** English template row (base-locale catalogue) | yes |
| Slot expansion | `.Replace({subject}/{opponent}/{score})` | `Expand` over the same placeholder set (no categories at base) | yes |
| Clause | `EpisodeClause(CitedEpisode.Kind)`, appended `text + " " + clause` | `clause(CitationKind)` (migrated table), appended `text + " " + clause` | yes |
| Serialized state | `world.text` cursor + memory | **unchanged** | yes |

**Base-locale identity is a mechanical property of preserving the corpus + the clause table + the draw + the
pre-draw gates** (T-LC-IDENTITY-001). The retrofit is behaviour-neutral at the base locale despite changing
#22's public return type.

## Appendix D — Worked render transition (§3.6)

`InteractionIntent.PlayerQuestionsMinutes` (ordinal 2; base-locale = 2 templates), draw `= 5`, slots
`{ "Rooney", "Everton", 2, 1, HasCitedEpisode=false }`:

| Step | Value |
|---|---|
| `Id` | `LivingWorldTextBoundary.ForInteraction(PlayerQuestionsMinutes)` → generic `TextTemplateId` |
| `n = variantCount(BaseLocale, Id)` | 2 |
| `variant = 5 % 2` | 1 |
| `template(Id, 1)` | `"{subject} wants a word about playing time after the {score} against {opponent}."` |
| `Expand` | `"Rooney wants a word about playing time after the 2-1 against Everton."` |
| clause (`HasCitedEpisode=false`) | none appended |
| **result** | `"Rooney wants a word about playing time after the 2-1 against Everton."` — byte-identical to today |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial appendices: constant catalogue, producer-emission→renderer-input mapping, the #22-retrofit byte-identity table, a worked render transition. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
