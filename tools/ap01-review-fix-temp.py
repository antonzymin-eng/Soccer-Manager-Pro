from pathlib import Path

root = Path('work')

test = root / 'tools/unity-ci/test-meta-integrity-gameart.sh'
text = test.read_text(encoding='utf-8')
text = text.replace('grep -Fq "MISSING GAMEART FOLDER META: $probe_root"', 'grep -Fxq "MISSING GAMEART FOLDER META: $probe_root"', 1)
text = text.replace('grep -Fq "MISSING GAMEART FOLDER META: $probe_dir"', 'grep -Fxq "MISSING GAMEART FOLDER META: $probe_dir"', 1)
test.write_text(text, encoding='utf-8')

ci = root / '.github/workflows/ci.yml'
text = ci.read_text(encoding='utf-8')
old = '''      # Every tracked file/folder under src/ must have a committed .meta, no
      # orphan metas, no duplicate GUIDs. A missing .cs.meta makes Unity assign
      # a fresh random GUID on checkout, silently breaking every reference to it.
'''
new = '''      # Managed src/ + Assets/GameArt/ paths must have committed metas and no
      # orphans; duplicate GUIDs are checked project-wide across tracked Assets/
      # plus the junction-backed src/ tree because Unity GUID identity is global.
'''
assert old in text
text = text.replace(old, new, 1)
ci.write_text(text, encoding='utf-8')
