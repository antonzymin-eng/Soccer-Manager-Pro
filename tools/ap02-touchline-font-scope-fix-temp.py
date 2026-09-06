from pathlib import Path

root = Path('work')

p = root / 'docs/design/ui-mockups/assets/tokens.css'
text = p.read_text(encoding='utf-8')
old = '  --font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", "Arial Narrow", sans-serif;'
new = '  --font-display: "Barlow Condensed", "Oswald", "Arial Narrow", sans-serif;'
assert old in text
text = text.replace(old, new, 1)
anchor = '[data-direction="touchline"] {\n  --font-body: "IBM Plex Sans", "Geist", system-ui, sans-serif;\n'
replacement = '[data-direction="touchline"] {\n  --font-body: "IBM Plex Sans", "Geist", system-ui, sans-serif;\n  --font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", "Arial Narrow", sans-serif;\n'
assert anchor in text
text = text.replace(anchor, replacement, 1)
p.write_text(text, encoding='utf-8')

p = root / 'docs/design/art/art-direction-v1.md'
text = p.read_text(encoding='utf-8')
old = '''- **from:** `--font-display: "Barlow Condensed", ...`
- **to:** `--font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", ...`
- **reason:** preserve a condensed display role while providing a technically viable Ukrainian/Cyrillic path and reducing cross-family typography complexity;
'''
new = '''- **from:** shared/root `--font-display: "Barlow Condensed", ...` for both historical directions;
- **to:** keep the root/stadium historical value unchanged and add a `touchline`-specific `--font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", ...` override;
- **reason:** preserve a condensed display role and the historical `stadium` comparison while providing the chosen `touchline` direction a technically viable Ukrainian/Cyrillic path and reducing cross-family typography complexity;
'''
assert old in text
text = text.replace(old, new, 1)
old = 'The AP-02 branch updates `docs/design/ui-mockups/assets/tokens.css` and the mockup README in the same proposed landing so there is no second typography source of truth. No palette, spacing, radius, body-face, mono-face, or `touchline` direction token changes are proposed.'
new = 'The AP-02 branch updates `docs/design/ui-mockups/assets/tokens.css` and the mockup README in the same proposed landing so there is no second typography source of truth. The rejected `stadium` direction keeps Barlow Condensed for historical comparison; only the chosen `touchline` display role is overridden. No palette, spacing, radius, body-face, mono-face, or direction-selection changes are proposed.'
assert old in text
text = text.replace(old, new, 1)
old = '| 1.1 | 2026-09-06 | AP-03 cross-stream typography correction: records the inherited Barlow Condensed Ukrainian/Cyrillic gap and proposes IBM Plex Sans Condensed as T-01 display replacement; shared mockup token/reference is updated in the same proposed G1 landing. Runtime font adoption remains AP-03/G2 work. |'
new = '| 1.1 | 2026-09-06 | AP-03 cross-stream typography correction: records the inherited Barlow Condensed Ukrainian/Cyrillic gap and proposes IBM Plex Sans Condensed as a `touchline`-specific T-01 display replacement; the root/stadium Barlow reference remains intact for historical comparison. Runtime font adoption remains AP-03/G2 work. |'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')

p = root / 'docs/design/ui-mockups/README.md'
text = p.read_text(encoding='utf-8')
old = 'The AP-02 branch therefore changes the shared `--font-display` reference to **IBM Plex Sans Condensed**, preserving the condensed display role while giving the localization workstream a viable Cyrillic path.'
new = 'The AP-02 branch therefore adds a `touchline`-specific `--font-display` override to **IBM Plex Sans Condensed**, preserving the root/stadium Barlow Condensed value for the historical comparison while giving the chosen direction a viable Cyrillic path.'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')
