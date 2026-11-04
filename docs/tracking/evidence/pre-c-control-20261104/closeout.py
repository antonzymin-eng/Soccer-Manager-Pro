"""Created 2026-11-04: record a passing control in the owning roadmap and inventory."""
from pathlib import Path
import hashlib,json,shutil,subprocess,tarfile,lzma
ROOT=Path(__file__).resolve().parent
REPO=Path('/workspace/Soccer-Manager-Pro')
TARGET=REPO/'docs/tracking/evidence/pre-c-control-20261104'
# This managed executor has no /codex mount; retain the export bundle as scratch.
OUTPUT=ROOT/'deliverables'
assert json.loads((ROOT/'analysis.json').read_text())['passed']
shutil.copyfile(ROOT/'REPRODUCE.md',TARGET/'REPRODUCE.md')
shutil.copyfile(ROOT/'REPRODUCE.md',OUTPUT/'pre-c-control-reproduce.md')
for filename in ['package_evidence.py','render_report.py','closeout.py']:
    shutil.copyfile(ROOT/filename,TARGET/filename)
roadmap=REPO/'docs/tracking/path-to-playable-roadmap.md';text=roadmap.read_text()
assert 'pre-c-control-20261104' not in text
text=text.replace('# Path to Playable — Implementation Roadmap\n', '# Path to Playable — Implementation Roadmap\n\n> **November 4 — Pre-C control complete (v0.89):** Linux .NET control at `1d3cebf3` reproduces all six adopted post-law W8 B digests in both plain and observed runs. The source/runtime/configuration and exact-touch/team observer are frozen; all six full digest, ball and event streams match. [Evidence](evidence/pre-c-control-20261104/README.md). W8 C is the next engine item.\n',1)
text=text.replace('This first host job is complete; the pre-C control capture is next.', 'This first host job is complete; the Pre-C control also completed November 4 ([evidence](evidence/pre-c-control-20261104/README.md)). W8 C is next on the engine lane.',1)
old='''1. **Pre-C control (S).** After shared item 1: pin source, runtime, configuration and the read-only
   exact-touch/team instrument; prove neutrality; reproduce the adopted post-law B baseline. Both B→C
   arms run on the same declared Linux .NET lane (step 8's measurement obligation).'''
new='''1. **Pre-C control (S).** ✅ **Done November 4, 2026** at `1d3cebf3` on Linux .NET SDK 8.0.426 /
   CLR 8.0.32. Source, runtime, configuration and the read-only exact-touch/team instrument are frozen;
   all six adopted post-law B digests reproduce in both arms, with full per-tick digest, ball and event
   equality across 324,000 ticks per seed ([evidence](evidence/pre-c-control-20261104/README.md)). C must
   use this declared lane and instrument for its paired comparison (step 8's measurement obligation).'''
assert old in text;text=text.replace(old,new,1)
row='| v0.89 | November 4, 2026 | Close engine lane item 1: freeze Pre-C source/runtime/configuration and exact-touch/team observer at `1d3cebf3`; reproduce all six adopted post-law B digests in plain and observed Linux .NET captures; verify full digest/ball/event neutrality and retain raw attribution/order evidence. W8 C implementation and its paired/host validation remain next. |\n'
assert text.count('| v0.88 |')==1;text=text.replace('| v0.88 |',row+'| v0.88 |',1)
roadmap.write_text(text)
registry=REPO/'tools/dotnet-ci/check_evidence_manifests.py';text=registry.read_text()
assert '"pre-c-control-20261104"' not in text
text=text.replace('    "d07-six-seed-census-20261018": ("manifest", FULL_MANIFEST),','    "d07-six-seed-census-20261018": ("manifest", FULL_MANIFEST),\n    "pre-c-control-20261104": ("manifest", FULL_MANIFEST),',1)
registry.write_text(text)

def manifest(root):
    rows=[]
    for path in sorted(root.rglob('*')):
        if path.is_file() and path.name!='SHA256SUMS':
            rows.append(hashlib.sha256(path.read_bytes()).hexdigest()+'  '+str(path.relative_to(root)))
    (root/'SHA256SUMS').write_text('\n'.join(rows)+'\n')
manifest(TARGET)
inventory=REPO/'docs/tracking/file-manifest.md'
with inventory.open('a') as f:
    f.write('\n## Pre-C control evidence — November 4, 2026\n\nCreated November 4, 2026. Purpose: pin the B control and exact-touch/team instrument before W8 C.\n\n| File | Purpose |\n| --- | --- |\n')
    for path in sorted(TARGET.iterdir()):
        f.write(f'| `{path.relative_to(REPO)}` | Pre-C source/runtime/instrument, capture, analysis or integrity evidence; see the directory README |\n')
with lzma.open(OUTPUT/'pre-c-control-evidence.tar.xz','wb',preset=3) as compressed,tarfile.open(fileobj=compressed,mode='w|') as archive:
    archive.add(TARGET,arcname=TARGET.name)
manifest(OUTPUT)
print('Recorded passing Pre-C control, registered evidence integrity, and saved local deliverables.')
