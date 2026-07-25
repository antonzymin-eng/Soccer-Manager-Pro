# UI Mockups — Soccer Manager Pro (design reference)

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (initial landing — imported design-system + screen mockups)
**Status:** DESIGN REFERENCE (non-normative)
**Related spec:** UI / Client Framework **#38** (`docs/specs/ui-client-framework/`, APPROVED July 22, 2026)

---

## 1. What this is

A self-contained set of static **HTML/CSS/JS visual mockups** for the manager-facing client:
a design-system page plus one page per management screen. They were authored outside the repo
and are landed here as the visual reference the Wave-7 screen specs and the eventual client
implementation design against.

**They are a reference, not a deliverable.** Specifically:

- **Not shipped client code.** #38 §1.3 pins the UGUI rendering binding as Unity-host-gated and
  out of the framework slice; nothing here is wired to a real view model, and none of it is on a
  build path.
- **Not determinism-relevant.** No file here is read by the sim, enters a snapshot, or feeds a
  digest. Same contract class as the `src/match-viewer/` HTML replay output and the Stage-0
  tactic-file text grammar: a presentation artifact, never a pinned wire format.
- **Not a spec.** Where a mockup and an APPROVED spec disagree, the spec wins. Screen behaviour
  is owned by the Wave-7 screen specs (#38 §7.1), each gated on its own data spec.

## 2. Inventory

### Foundations

| File | Contents |
|------|----------|
| `Soccer Manager Pro - Design System.html` | The design-system page: philosophy, color, typography, spacing/radii, buttons, inputs, tabs/chips, data tables, cards/modals, stat tiles, attribute displays, formation pitch, match-day HUD |
| `Desktop Guardrails.html` | Desktop layout/resolution guardrails (density tiers, 1920×1080 reference stage) |
| `Command Palette.html` | Global command-palette / navigation pattern |

### Screens

| File | Screen | Nearest data spec |
|------|--------|-------------------|
| `Squad Screen.html` | Squad list / player detail | #27 Squad / Player Data Layer |
| `Tactics.html` | Tactics & formation | #21 Tactical Instructions, #26 Tactical Presets |
| `Training Screen.html` | Training | #29 Training System |
| `Scouting Screen.html` | Scouting | #32 Scouting & Player Knowledge |
| `Transfers.html` | Transfers & contracts | #31 Transfers, Contracts & Negotiation |
| `Club.html` | Club overview | #40 Club Finances & Economy |
| `Club Finances.html` | Finances | #40 Club Finances & Economy |
| `Club Staff.html` | Staff & backroom | #34 Staff & Backroom |
| `Club Board Room.html` | Board / expectations | #40 (board), #33 (dynamics) |
| `Club History.html` | Club history / records | #37 Match Analytics & Statistics |
| `World.html` | World / competitions | #43 Competition Structure, #30 Season & Competition Loop |

The spec column names the data spec a screen would bind to; it is a routing aid for whoever writes
the Wave-7 screen spec, **not** a claim that any binding exists today.

### Assets (`assets/`)

`tokens.css` (design tokens) · `components.css` · `dataviz.css` · `styleguide.css` · `example.css` ·
`squad.css` · `scouting.css` · `training.css` — stylesheets;
`app.js` (direction switcher + section nav) · `squad.js` · `squad-views.js` — page scripts;
`tweaks-panel.jsx`, `squad-tweaks.jsx`, `scouting-tweaks.jsx`, `training-tweaks.jsx` — in-page
tweak panels used while iterating on the mockups.

`screenshots/squad-check.png` — a reference capture of the squad screen.

## 3. Viewing them

Open any `.html` file directly in a browser, or serve the folder:

```
python3 -m http.server 8000 --directory docs/design/ui-mockups
```

Notes:

- The pages fetch **Google Fonts over the network** (Barlow, Barlow Condensed, IBM Plex Sans,
  JetBrains Mono). Offline they fall back to system fonts and stay readable, but metrics shift.
- The design system carries **two visual directions** — `stadium` (broadcast graphics) and
  `touchline` (analyst tool) — switched by the `data-direction` attribute on `<html>` and
  persisted in `localStorage`. Both are live in `tokens.css`; **neither has been chosen yet.**
- Screen pages render onto a fixed **1920×1080 stage** scaled to the viewport, matching the
  desktop guardrails.
- All data shown is **hardcoded mock data** (players, fees, dates). It is illustrative only and
  is not sourced from `player-database` or any save.

## 4. Version History

| Version | Date | Change |
|---------|------|--------|
| 1.0 | July 25, 2026 | Initial landing: design system + 11 screen mockups + shared assets, imported as the #38 visual reference. |
