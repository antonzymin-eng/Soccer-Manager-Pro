from pathlib import Path

root = Path('work')

# Art-direction document: record the AP-03 localization finding as an explicit
# proposed token change rather than silently inheriting the broken display face.
p = root / 'docs/design/art/art-direction-v1.md'
text = p.read_text(encoding='utf-8')
text = text.replace('**Created:** September 6, 2026  \n**Style version:** `art-direction-v1`  ', '**Created:** September 6, 2026  \n**Last Updated:** September 6, 2026  \n**Document version:** 1.1  \n**Style version:** `art-direction-v1`  ', 1)
old = '''### 3.4 Typography roles

Current candidates inherited from the chosen reference:

- display: **Barlow Condensed**;
- body: **IBM Plex Sans**;
- mono/data: **JetBrains Mono**.

Touchline label behavior is uppercase, weight 600, tracking `0.08em`; display weight is 700 with `-0.01em` tracking.

These are **visual candidates, not yet shipping font dependencies**. AP-03 owns redistribution/offline packaging and Ukrainian/Cyrillic/fallback evidence.
'''
new = '''### 3.4 Typography roles

The July 25 mockup baseline nominated:

- display: **Barlow Condensed**;
- body: **IBM Plex Sans**;
- mono/data: **JetBrains Mono**.

AP-03's rights/script audit found a shipping blocker in that inherited stack: current upstream Barlow/Barlow Condensed does not provide the required Ukrainian/Cyrillic coverage. AP-02 therefore proposes **IBM Plex Sans Condensed** as the display replacement while retaining IBM Plex Sans for body and JetBrains Mono for mono/data. This keeps the condensed analyst/broadcast-accent role but aligns the display/body system within the IBM Plex family and removes the known script hole.

Touchline label behavior remains uppercase, weight 600, tracking `0.08em`; display weight remains 700 with `-0.01em` tracking. The role changes font family, not hierarchy.

These remain **visual candidates, not yet shipping font dependencies**. AP-03 owns redistribution/offline packaging, exact binary/version pinning, and Ukrainian/Cyrillic/fallback evidence. G1 acceptance would approve the visual substitution; it would not by itself close G2 or authorize font binaries.
'''
assert old in text
text = text.replace(old, new, 1)
old = '''## 14. Token-change register

**v1 proposes no changes to the existing `touchline` tokens.**

Any future change to an inherited palette, type role, spacing value, radius, or `touchline` direction value must be recorded here as a proposed change and update the UI design reference in the same landing. Family-specific values in this document (for example the icon reference grid/stroke) are extensions, not silent token replacements.
'''
new = '''## 14. Token-change register

### T-01 — Display font family (PROPOSED with G1)

AP-03 identified a localization blocker in the inherited display face. AP-02 therefore proposes:

- **from:** `--font-display: "Barlow Condensed", ...`
- **to:** `--font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", ...`
- **reason:** preserve a condensed display role while providing a technically viable Ukrainian/Cyrillic path and reducing cross-family typography complexity;
- **scope:** visual reference/theme token only; no runtime font binary is adopted in AP-02;
- **evidence owner:** AP-03 for license, exact binary/version, glyph corpus, offline packaging, and Unity import;
- **rollback:** if G1 visual review rejects IBM Plex Sans Condensed, choose another Cyrillic-capable condensed display face through the same explicit token-change process rather than reverting to an uncovered shipping face.

The AP-02 branch updates `docs/design/ui-mockups/assets/tokens.css` and the mockup README in the same proposed landing so there is no second typography source of truth. No palette, spacing, radius, body-face, mono-face, or `touchline` direction token changes are proposed.

Any future change to an inherited palette, type role, spacing value, radius, or `touchline` direction value must be recorded here as a proposed change and update the UI design reference in the same landing. Family-specific values in this document (for example the icon reference grid/stroke) are extensions, not silent token replacements.
'''
assert old in text
text = text.replace(old, new, 1)
old = '4. no second palette/type/spacing source has been created;\n5. AP-03 rights/font/import work remains separate and explicitly pending;'
new = '4. the proposed T-01 display-font substitution is visually accepted or explicitly replaced by another Cyrillic-capable condensed face, with the shared mockup token updated in the same landing;\n5. no second palette/type/spacing source has been created, and AP-03 rights/font/import work remains separate and explicitly pending;'
assert old in text
text = text.replace(old, new, 1)
old = '| 1.0 | 2026-09-06 | Initial AP-02 derivation from chosen `touchline`: maps inherited tokens and extends unresolved identity, icon, data-viz, match, portrait, fictional-club, stadium, motion, and accessibility rules. No inherited token changes proposed. |'
new = old + '\n| 1.1 | 2026-09-06 | AP-03 cross-stream typography correction: records the inherited Barlow Condensed Ukrainian/Cyrillic gap and proposes IBM Plex Sans Condensed as T-01 display replacement; shared mockup token/reference is updated in the same proposed G1 landing. Runtime font adoption remains AP-03/G2 work. |'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')

# Shared token source. Add a single shared design-reference import instead of
# editing every mockup page's <head>. Barlow links may remain as historical
# comparison load; the active display token no longer selects it.
p = root / 'docs/design/ui-mockups/assets/tokens.css'
text = p.read_text(encoding='utf-8')
import_line = '@import url("https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Condensed:wght@500;600;700;800&display=swap");\n\n'
assert 'IBM+Plex+Sans+Condensed' not in text
text = import_line + text
old = '  --font-display: "Barlow Condensed", "Oswald", "Arial Narrow", sans-serif;'
new = '  --font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", "Arial Narrow", sans-serif;'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')

# Reference README: explain why the shared display reference changed and keep
# Barlow as historical context rather than pretending the original choice never existed.
p = root / 'docs/design/ui-mockups/README.md'
text = p.read_text(encoding='utf-8')
text = text.replace('**Last Updated:** July 25, 2026 (visual direction CHOSEN: `touchline`)', '**Last Updated:** September 6, 2026 (`touchline` retained; AP-02 display-font localization correction proposed)', 1)
old = '''- The pages fetch **Google Fonts over the network** (Barlow, Barlow Condensed, IBM Plex Sans,
  JetBrains Mono). Offline they fall back to system fonts and stay readable, but metrics shift.
'''
new = '''- The pages fetch **Google Fonts over the network**. The historical comparison pages still request Barlow / Barlow Condensed, while the shared `tokens.css` now requests **IBM Plex Sans Condensed** for the proposed AP-02 display role alongside IBM Plex Sans and JetBrains Mono. Offline the mockups still fall back to system fonts and remain reference-only; shipping font packaging is AP-03/G2 work.
'''
assert old in text
text = text.replace(old, new, 1)
anchor = '''What this does **not** pin: any of these values as final. The tokens are a starting point for the
rendering work, not an approved constant catalogue — nothing here is `[GT]`-tagged or loaded by the
sim (see §1).
'''
addition = anchor + '''
### 4.1 AP-02 typography correction — proposed with G1

AP-03's rights/script audit found that the inherited Barlow Condensed display candidate does not provide the required Ukrainian/Cyrillic coverage. The AP-02 branch therefore changes the shared `--font-display` reference to **IBM Plex Sans Condensed**, preserving the condensed display role while giving the localization workstream a viable Cyrillic path. This does not change the `touchline` direction and does not adopt a runtime font binary; AP-03 still owns exact version/license/glyph/offline-package proof before G2 can close.

Barlow Condensed remains part of the historical July 25 comparison record rather than being rewritten out of history.
'''
assert anchor in text
text = text.replace(anchor, addition, 1)
old = '| 1.1 | July 25, 2026 | Visual direction chosen: `touchline` (§5). `app.js` default corrected from `stadium` — the pages had been rendering a direction their own markup did not declare. |'
new = old + '\n| 1.2 | September 6, 2026 | AP-02 proposed typography correction: `touchline` retained; shared display token changes from Barlow Condensed to IBM Plex Sans Condensed because AP-03 identified the inherited face as unsuitable for required Ukrainian/Cyrillic coverage. Runtime font adoption remains G2-gated. |'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')

# The design-system page contains visible descriptive prose that would otherwise
# contradict the active token. Change prose only; behavior/layout remains untouched.
p = root / 'docs/design/ui-mockups/System XI - Design System.html'
text = p.read_text(encoding='utf-8')
text = text.replace('Type</span><span class="v">Barlow Condensed + Body', 'Type</span><span class="v">IBM Plex Sans Condensed + Body', 1)
text = text.replace('Barlow Condensed for display — tall, broadcast, jersey-numbery. A workhorse body face for menus and prose.', 'IBM Plex Sans Condensed for display — compact, analytical, and Cyrillic-capable. IBM Plex Sans remains the touchline body face.', 1)
p.write_text(text, encoding='utf-8')
