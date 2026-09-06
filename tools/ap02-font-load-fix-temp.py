from pathlib import Path

root=Path('work')
p=root/'docs/design/ui-mockups/assets/tokens.css'
text=p.read_text(encoding='utf-8')
old='@import url("https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Condensed:wght@500;600;700;800&display=swap");'
new='@import url("https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Condensed:wght@500;600;700&display=swap");'
assert old in text
p.write_text(text.replace(old,new,1), encoding='utf-8')

p=root/'docs/design/art/art-direction-v1.md'
text=p.read_text(encoding='utf-8')
old='| 1.2 | 2026-09-06 | Second hostile-review refinement: G1 now requires representative 1920×1080 dense-screen visual regression evidence for T-01 so the font substitution cannot be accepted from script coverage/prose alone. |'
new=old+'\n| 1.3 | 2026-09-06 | Visual-proof correction: the first Google Fonts request incorrectly asked IBM Plex Sans Condensed for weight 800, which the family does not provide; the failed font-load proof caught the fallback. Import narrowed to the actually required 500/600/700 weights before rerunning G1 visual evidence. |'
assert old in text
p.write_text(text.replace(old,new,1), encoding='utf-8')
