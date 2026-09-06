from pathlib import Path

p = Path('work/docs/design/art/art-direction-v1.md')
text = p.read_text(encoding='utf-8')
old = '''4. the proposed T-01 display-font substitution is visually accepted or explicitly replaced by another Cyrillic-capable condensed face, with the shared mockup token updated in the same landing;
5. no second palette/type/spacing source has been created, and AP-03 rights/font/import work remains separate and explicitly pending;
6. the direction can produce professional Stage-1 match presentation rather than placeholder-quality visuals.
'''
new = '''4. the proposed T-01 display-font substitution is visually accepted or explicitly replaced by another Cyrillic-capable condensed face, with the shared mockup token source updated in the same landing;
5. T-01 has been rendered at the mockups' 1920×1080 reference stage on at least **Squad Screen**, **Tactics**, and one finance/data-heavy screen, with no unacceptable clipping, wrapping, hierarchy loss, or density regression relative to the chosen `touchline` baseline;
6. no second palette/type/spacing source has been created, and AP-03 rights/font/import work remains separate and explicitly pending;
7. the direction can produce professional Stage-1 match presentation rather than placeholder-quality visuals.
'''
assert old in text
text = text.replace(old, new, 1)
old = '| 1.1 | 2026-09-06 | AP-03 cross-stream typography correction: records the inherited Barlow Condensed Ukrainian/Cyrillic gap and proposes IBM Plex Sans Condensed as a `touchline`-specific T-01 display replacement; the root/stadium Barlow reference remains intact for historical comparison. Runtime font adoption remains AP-03/G2 work. |'
new = old + '\n| 1.2 | 2026-09-06 | Second hostile-review refinement: G1 now requires representative 1920×1080 dense-screen visual regression evidence for T-01 so the font substitution cannot be accepted from script coverage/prose alone. |'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')
