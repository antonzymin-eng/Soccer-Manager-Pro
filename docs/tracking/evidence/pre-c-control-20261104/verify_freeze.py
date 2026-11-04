"""Created 2026-11-04: verify retained as-run inputs and runtime after capture."""
from pathlib import Path
import hashlib,json,tarfile
ROOT=Path(__file__).resolve().parent
pin=json.loads((ROOT/'freeze.json').read_text())
def verify(path,expected):
    assert path.is_file(),str(path)
    assert hashlib.sha256(path.read_bytes()).hexdigest()==expected,str(path)
for path,digest in pin['inputs'].items():verify(ROOT/path,digest)
for arm in ('plain','observed'):
    for section in ('sources','binaries'):
        for path,digest in pin[section][arm].items():verify(ROOT/arm/path,digest)
runtime=Path('/workspace/.cloud-tools/dotnet')
verify(runtime/'dotnet',pin['dotnetSha256'])
for name,digest in pin['frameworkSha256'].items():verify(runtime/'shared/Microsoft.NETCore.App/8.0.32'/name,digest)
modified={'src/match-engine/MatchEngine.cs','src/match-engine/MatchEngine.KeeperHandlingLaw.cs','src/collision-system/CollisionSystem.cs','src/event-system/EventBus.cs'}
original_cs=set()
with tarfile.open(ROOT/'source.tar') as archive:
    for item in archive:
        if not item.isfile():continue
        data=archive.extractfile(item).read()
        assert (ROOT/'plain'/item.name).read_bytes()==data,item.name
        if item.name not in modified:assert (ROOT/'observed'/item.name).read_bytes()==data,item.name
        if item.name.startswith('src/') and item.name.endswith('.cs'):original_cs.add(item.name)
for arm,extra in [('plain',set()),('observed',{'src/match-engine/MatchEngine.PreCObservation.cs'})]:
    actual={str(p.relative_to(ROOT/arm)) for p in (ROOT/arm/'src').rglob('*.cs') if not {'bin','obj'}.intersection(p.relative_to(ROOT/arm).parts)}
    assert actual==original_cs|extra,(arm,actual^(original_cs|extra))
runs=json.loads((ROOT/'runs.json').read_text())
assert len(runs)==12 and all(r['exitCode']==0 for r in runs)
assert len({(r['seed'],r['arm']) for r in runs})==12
print('Plain source equals the pinned Git archive; observed source differs only by the retained overlay. Frozen source, instrument, generated compile inputs, loaded binary sets and CLR hashes unchanged; all 12 captures exited 0.')
