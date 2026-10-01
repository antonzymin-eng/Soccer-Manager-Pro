# S0 Gate H high-fidelity reference v0.2

**Created:** September 30, 2026\
**Last Updated:** October 1, 2026\
**Status:** H v0.2 PASS — owner “images approved” October 1, 2026 at 12:35:32 America/Los_Angeles (19:35:32 UTC); reviewed `a1044d5`. I OPEN using v0.2; #470 blocked on completed I.\
**Prior approval preserved:** v0.1 at `4a6220c`, owner “images approved” September 30, 2026; recorded at `654c4f8` in [protocol §9.1.2](../ux-validation-protocol.md#912-s0-gate-h-owner-image-approval). Its evidence and approval fingerprints remain unchanged.\
**Journey authority:** [S0 packet §13](../ux-s0-pm1-journey.md#13-gate-h--high-fidelity-reference)

Open `index.html` locally. No build, server, account or network is needed. This is the touchline /
Retro Dynamo Blue reference for the approved four-screen S0 journey. It shares G's immutable model,
reference capture and synthetic statistics fixture; the H renderer/styles and verifier are separate.
The approved `s0-prototype/` sources, PDFs, hashes and owner records remain unchanged.

The owner approved all 23 v0.2 views at `a1044d525703a42f274b6643f9c96c3c46dc3592`.
[Protocol §9.1.3](../ux-validation-protocol.md#913-s0-gate-h-v02-owner-image-approval) records the
actual confirmation/time, seven executable-source hashes, all 46 image hashes and walkthrough hash.
Gate I now uses this approved v0.2 baseline. The earlier v0.1 approval remains intact.
This Markdown-only recording leaves every executable source/image/evidence file unchanged.

All commands/results are simulated. The ordinary 2–1 score/statistics fixture is synthetic;
`fixture=scoreline` uses the unmodified 19–9 capture. Choices do not recompute either match. H adds an
explicitly illustrative substitution overlay: the applied incoming shirt occupies the outgoing
player's captured position. It is design evidence, not a new runtime projection. Home/Away and
shirt numbers identify every choice; names and production roster identity still depend on S1/client work.

## Post-approval validation delta

S0-H-014 corrects only `fixture=long-names&pseudo=1`: Home/Away now sits inside the
complete bracketed team identity. The shared coverage check scans fault and long-name fixtures.
The current renderer/verifier hashes therefore differ from the approved source pins at `a1044d5`;
[protocol §9.1.4](../ux-validation-protocol.md#914-s0-h-014-long-name-pseudo-locale-validation-delta)
records this focused delta without changing the approval or any approved images/walkthrough.

To validate that correction without replacing the approved exports:

```bash
NODE_PATH=/path/to/node_modules UX_BROWSER=/path/to/chrome node docs/design/s0-high-fidelity/verify.cjs --pseudo-locale-only
```

This mode needs Playwright/Chromium, writes only
[`long-name-pseudo-delta.json`](evidence/v0.2/long-name-pseudo-delta.json), and checks the existing
46 image hashes against the approved walkthrough. Run `UX-H-S0-20261001-03` passes;
the full 99-check walkthrough also passes in a separate scratch copy. Ordinary/non-pseudo
identity text is unchanged. Run full export verification in a separate worktree as described below.

## Design and boundaries

Navy/blue structure, cream ink, gold selection and tabular data follow the accepted
[art direction](../art/art-direction-v1.md) and [shared UX system](../ux-shared-system.md).
Warnings use a separate rose border and explicit incomplete text; outcomes always have status text.
System font fallback is deliberately exercised offline. Exact PT Sans Narrow / IBM Plex Sans /
JetBrains Mono packaging, Cyrillic corpus and shipping fallback validation remain AP-03/client work.
The text wordmark is the menu art fallback; team text replaces missing crests; shirt markers replace
portraits. No fictional club, venue or player identity is invented. No network fonts or missing-image slots.

Primary progression uses blue; selected setup choice has a checked radio and gold outline.
Keyboard focus uses a separate 3px light-blue outline. Headings and the outcome list receive focus
only for keyboard/screen-reader continuity, so they show that outline only after keyboard input;
pointer users never see a field-like box on them (S0-H-005). Disabled controls have dashed borders and persistent
reasons. Full-time statistics use static retained/closed text, with no disabled Close control;
the accepted S0-G-008 no-reopening behavior is unchanged. Open partial figures retain their cutoff;
report partial figures start behind disclosure. Full-time report remains primary.

Mentality staging shows the current value, the requested choice plus its effect, and an explicit comparison
disclosure for all seven identical setup choices/effects tagged Current and Requested. Submit/Cancel remain separate; changing selection is not
application. Bench labels are 1–7; option values remain 0–6. An Applied shirt 4 → shirt 14 change
replaces H4 with a white-outlined H14 ↔ (enlarged glyph); Pending/Refused keep H4. Consumed outgoing/bench options are unavailable.

Outcomes carry an explicit label — Pending (dashed blue), Applied (green ✓), Refused (rose ✕), Not applied
(dotted grey –) — with colour and glyph only reinforcing the word. Period and minute sit directly under
the score; speed and current Mentality are plain text, never button-shaped.

Shared patterns: page heading/entry panel; scoreboard/context lines; pitch; playback controls;
team-action rail; outcome list; statistics table and incomplete banner/disclosure; staging modal.
The four screen identities and five existing navigation moves are unchanged. Exact shipping bindings,
focus/action/read mapping and localization allocation belong to Gate I after H approval.

Copy roles: headings/location; identity/score/time; action labels; selection effects; control-unavailable
reasons; Pending/Applied/Refused/Not-applied outcomes; statistics health/cutoff; disclosure labels.
Text stays semantic DOM content, never rasterized in the interactive reference. Strings and dynamic
arguments must be allocated to #49 localization roles in I; this English design reference claims no
shipped locale catalog. Future captions remain a clearly labelled stress fixture, not a product feature.

## Reproduction and verification

All reviewer parameters documented in [G's README](../s0-prototype/README.md#reviewer-fixtures) apply.
With Playwright, Chromium and Poppler (`pdfinfo` / `pdftotext`) available:

```bash
NODE_PATH=/path/to/node_modules UX_BROWSER=/path/to/chrome node docs/design/s0-high-fidelity/verify.cjs
```

Run `UX-H-S0-20261001-02` records 99 PASS checks and 23 PNG/PDF pairs in `evidence/v0.2/`.
`walkthrough.json` fingerprints the four H executable sources, three shared G sources and all 46
image files. The verifier derives the established G journey tests, replacing presentation-specific
assertions and adding H identity/choice/geometry assertions; v0.2 adds eight finding checks (S0-H-005–012) and an export-isolation/completeness check (S0-H-013).
No G evidence is superseded or regenerated, and `evidence/v0.1/` is not rewritten.

The fingerprints pin the exact reviewed artifact bytes, not identical output on every machine.
Browser, installed fonts and font rendering can change pixels, wrapping and image height on a rerun
even when the source hashes and checks agree. Run verification in a separate worktree and compare
the checks and layout; keep the committed reviewed evidence unchanged.

Mouse journeys cover 1366/1920/2560; keyboard covers 1366. Expanded/bracketed labels plus 200% root
font (16→32px) run at all three widths, with browser zoom at 100%. Horizontal overflow, critical
clipping, marker collision, modal focus, normal-text dropdown clearance, clock/fault boundaries,
whistle race and live feedback persistence are checked. Contrast samples are recorded in JSON;
these are measured samples, not a full accessibility certificate. Vertical scrolling is allowed; at 1920×1080 and 2560×1440 the whole pitch is visible without scrolling in
waiting, live, paused and full-time states, while 1366×768 keeps a 704px readable pitch floor and scrolls.
Staged-dialog images are single-viewport captures, so the modal backdrop covers the entire image.
All temporary export styles are removed in `finally`; pitch/grid geometry is frozen only during each export.
The verifier checks all 23 PDF page counts, PNG/PDF dimensions and every displayed statistics row label.
This prevents a modal clip leaking into later live/full-time statistics captures on the same page.
The shipping maximum text scale, font/runtime application and cert-host checks remain deferred.

All 23 PNGs were inspected at full size and all 23 single-page PDFs were rendered through Poppler and inspected.
The separate owner confirmation approves v0.2; scripted checks and author inspection supplied supporting
evidence. These files supply no Unity/runtime evidence.

## Owner review images

All 23 v0.2 views are owner-approved, including the expanded comparison, Applied shirt identity, both full-time
statistics states and the small-desktop expanded-text fault case. The v0.1 images stay in
`evidence/v0.1/` as the unchanged, owner-approved historical reference; v0.2 is the current approved baseline. PNG is convenient for image review;
PDF preserves the complete vector layout. `walkthrough.json` pins both formats.
The small review PNGs are stored directly using evidence-local Git attributes; the repository
large-binary guard still applies. No Unity asset/LFS rule is changed.

| View | PNG | PDF |
|---|---|---|
| Main Menu / admitted demo entry | [PNG](evidence/v0.2/mm.png) | [PDF](evidence/v0.2/mm.pdf) |
| Tactics Setup / all seven Mentalities | [PNG](evidence/v0.2/ts.png) | [PDF](evidence/v0.2/ts.pdf) |
| Waiting for first frame | [PNG](evidence/v0.2/mv-0.png) | [PDF](evidence/v0.2/mv-0.pdf) |
| Live Match View | [PNG](evidence/v0.2/mv-l.png) | [PDF](evidence/v0.2/mv-l.pdf) |
| Paused Match View | [PNG](evidence/v0.2/mv-p.png) | [PDF](evidence/v0.2/mv-p.pdf) |
| Full time / closed statistics | [PNG](evidence/v0.2/mv-ft.png) | [PDF](evidence/v0.2/mv-ft.pdf) |
| Healthy post-match report | [PNG](evidence/v0.2/pr.png) | [PDF](evidence/v0.2/pr.pdf) |
| Staged Mentality / requested choice and effect | [PNG](evidence/v0.2/mentality-dialog.png) | [PDF](evidence/v0.2/mentality-dialog.pdf) |
| Live comparison / all seven choices and effects | [PNG](evidence/v0.2/mentality-comparison.png) | [PDF](evidence/v0.2/mentality-comparison.pdf) |
| Shirt-number identity / bench labels 1–7 | [PNG](evidence/v0.2/substitution-dialog.png) | [PDF](evidence/v0.2/substitution-dialog.pdf) |
| Live Pending request / unchanged current choice | [PNG](evidence/v0.2/mv-live-pending.png) | [PDF](evidence/v0.2/mv-live-pending.pdf) |
| Paused Pending / resume instruction | [PNG](evidence/v0.2/mv-paused-pending.png) | [PDF](evidence/v0.2/mv-paused-pending.pdf) |
| Applied Mentality and substitution / H14 replaces H4 | [PNG](evidence/v0.2/mv-live-applied.png) | [PDF](evidence/v0.2/mv-live-applied.pdf) |
| Refused Mentality / unchanged current choice | [PNG](evidence/v0.2/mv-live-refused.png) | [PDF](evidence/v0.2/mv-live-refused.pdf) |
| Healthy live statistics | [PNG](evidence/v0.2/mv-live-statistics.png) | [PDF](evidence/v0.2/mv-live-statistics.pdf) |
| Whistle race / request not applied | [PNG](evidence/v0.2/mv-ft-not-applied.png) | [PDF](evidence/v0.2/mv-ft-not-applied.pdf) |
| Healthy retained statistics at full time | [PNG](evidence/v0.2/mv-ft-statistics-open.png) | [PDF](evidence/v0.2/mv-ft-statistics-open.pdf) |
| Faulted retained statistics at full time | [PNG](evidence/v0.2/mv-ft-statistics-fault.png) | [PDF](evidence/v0.2/mv-ft-statistics-fault.pdf) |
| Incomplete report / disclosure closed | [PNG](evidence/v0.2/report-incomplete.png) | [PDF](evidence/v0.2/report-incomplete.pdf) |
| Incomplete report / partial figures disclosed | [PNG](evidence/v0.2/report-partial-open.png) | [PDF](evidence/v0.2/report-partial-open.pdf) |
| 1366 / pseudo-locale / 200% / fault / future caption reservation | [PNG](evidence/v0.2/stress-fault-1366.png) | [PDF](evidence/v0.2/stress-fault-1366.pdf) |
| 1366 normal text / undistorted pitch | [PNG](evidence/v0.2/mv-live-1366.png) | [PDF](evidence/v0.2/mv-live-1366.pdf) |
| 2560 normal text / undistorted pitch | [PNG](evidence/v0.2/mv-live-2560.png) | [PDF](evidence/v0.2/mv-live-2560.pdf) |

## Version history

| Version | Date | Change |
|---|---|---|
| 0.1 | September 30, 2026 | Separate H touchline reference from PR #473 head b6c9c6a; carried presentation fixes, coherent illustrative shirt overlay, 90 checks and 23 image pairs. Owner H approval pending. |
| 0.1 approval record | September 30, 2026 | Explicit owner “images approved” at 4a6220c; H PASS, I OPEN for handoff. Markdown-only record preserves all executable sources/images/evidence. |
| 0.2 | October 1, 2026 | Implements Claude’s eight presentation fixes S0-H-005–012 from f7f44b5, then fixes capture-style leakage (S0-H-013) and regenerates all 23 PNG/PDF pairs in run UX-H-S0-20261001-02 (99 checks). Based on approval-recording head 654c4f8; the actual v0.1 approval, evidence and fingerprints are preserved. Separate v0.2 owner approval pending; I on hold for this revision. |
| 0.2 documentation correction | October 1, 2026 | Clarifies that hashes pin reviewed bytes rather than cross-machine output; restores H PASS / I OPEN for pinned v0.1 without inferring a new owner decision. v0.2 adoption still awaits separate approval; all executable sources/images/evidence unchanged. |
| 0.2 approval record | October 1, 2026 | Owner “images approved” at a1044d5, all 23 views. Protocol §9.1.3 pins confirmation/time, 46 images, seven sources and walkthrough hash. H PASS / I OPEN using v0.2; all executable sources/images/evidence unchanged. |
| 0.2 validation delta | October 1, 2026 | S0-H-014 localizes the complete long-name identity and extends coverage beyond fault. Focused run 03 and full 99-check scratch run pass; approved a1044d5 images/walkthrough/source pins unchanged. |
