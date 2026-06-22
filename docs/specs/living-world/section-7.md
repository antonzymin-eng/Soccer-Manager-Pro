# Living World System Specification #22 — Section 7: Future Extensions, Stage Gating, Recorded Decisions

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.1)
**Version:** 0.2
**Status:** APPROVED (June 22, 2026)

---

## 7.1 Stage gating (KD-10)

Runtime activation is gated on prerequisites that do not exist at Stage 0 — none block any Stage-0 spec:

| Prerequisite | Owner | Gates |
|---|---|---|
| Persistent world store + season-calendar loop | this spec (new) | the whole layer |
| vol-2/vol-3 human-systems implemented | human-systems work | the consume-as-is reads (§1.3) |
| `[GT]` config-loader | `src/CLAUDE.md` "WHAT IS NOT HERE YET" | every `[GT]` value injection |
| Structured match-outcome events | match engine | the loop's input |

Data types, mappings, and the harness contract are authorable now (T0); activation lands as each
prerequisite does.

## 7.2 Recorded scope decisions (from design supplement v0.7)

- **Authoring corpus (DECIDED — AI-generated).** The §3.3 template/grammar corpus and §3.4 arc library
  are produced with **AI assistance as an offline authoring tool**, consistent with FR-LW-012 (no
  runtime inference on saved-state paths). Volume is no longer the gate; **curation + a balance/guardrail
  pass remain to be done** before the corpus is shippable.
- **Inspector tooling (DECIDED — full).** A debug/inspector view providing **time-scrub / replay-step**
  and **"why did this arc fire?" causal tracing**, powered by the FR-LW-016 `SpawnCause` provenance.
  Mandatory-from-day-one consequence: provenance is captured inline at spawn (it cannot be reconstructed
  later). The optional interaction-log (`(intent, cursor, snapshotRef)`, §3.6) is **determinism-neutral**:
  side-effect-free w.r.t. world state and **excluded from the determinism digest**, so a debug build that
  keeps it does not diverge from a release build that omits it.
- **Localisation (DECIDED — English-shaped now).** v1 commits to English-shaped templates and **accepts
  a costly rework** if/when multi-language is added; the grammar is not pre-built for
  gender/inflection agreement. A recorded, accepted future cost.

## 7.3 Deferred to implementation (depend on the not-yet-designed data model)

- **Residue A — cold-summary compression schema.** What a `ColdSummary` retains (net relationship +
  top-N salient episodes vs. a richer digest). Decided at implementation against the finalised episode
  /interaction set.
- **Residue B — `[GT]` budget split.** Default is a single shared pool + one eviction policy; split into
  per-class sub-quotas (live edges / live episodes / cold summaries) only if §6.2 soak shows starvation.

## 7.4 Stage-2+ candidate extensions (not in v1)

Segmented fan factions (vs. the v1 aggregate fan node); multi-language template grammar; richer
non-player relationship layers; player-agent autonomy (agents initiating interactions); cross-save
"reputation" persistence of cold summaries beyond a single career.
