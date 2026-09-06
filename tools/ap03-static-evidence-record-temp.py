from pathlib import Path
p=Path('work/docs/design/art/art-technical-recipe-v1.md')
text=p.read_text(encoding='utf-8')
old='| PNG Git LFS routing | STATIC PASS | root `.gitattributes`: `*.png filter=lfs diff=lfs merge=lfs -text` |\n| TTF/OTF Git LFS routing | STATIC PASS | root `.gitattributes`: both font extensions use LFS |'
new='| PNG Git LFS routing | PASS | proof run `34060090061`: planned `Assets/GameArt/UI/Icons/ap03_import_probe.png` resolves `filter=lfs`, `diff=lfs`, `merge=lfs` |\n| TTF/OTF Git LFS routing | PASS | proof run `34060090061`: planned GameArt `.ttf` and `.otf` paths both resolve `filter=lfs`, `diff=lfs`, `merge=lfs` |'
assert old in text
text=text.replace(old,new,1)
old='| Actual Unity import | **PENDING** | must be performed in Unity 6000.4.9f1; no hand-authored production file meta permitted |'
new='| No premature runtime art | PASS | proof run `34060090061`: `Assets/GameArt/`, `Assets/GameArt.meta`, and tracked GameArt paths are absent |\n| Actual Unity import | **PENDING** | must be performed in Unity 6000.4.9f1; no hand-authored production file meta permitted |'
assert old in text
text=text.replace(old,new,1)
old='| 0.2 | 2026-09-06 | Hostile-review sequencing correction: removes accidental G1/final-font-binary prerequisites from G2, makes the import probe explicitly style-neutral/G1-independent, and treats the font audit as an explicit rights/script/fallback decision path. G2 still requires real Unity import/replacement evidence; final font binary validation remains a later shipping requirement unless AP-03 vendors fonts. |'
new=old+'\n| 0.3 | 2026-09-06 | Static evidence recorded from run `34060090061`: Unity pin, planned PNG/TTF/OTF LFS attributes, no-premature-GameArt assertion, AP-01 integrity/binary baseline, and documentation-only scope all passed. Remaining G2 blockers are the real source/export/Unity import/importer/replacement/reference proof. |'
assert old in text
text=text.replace(old,new,1)
p.write_text(text,encoding='utf-8')
