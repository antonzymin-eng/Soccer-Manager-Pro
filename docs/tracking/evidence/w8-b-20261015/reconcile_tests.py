#!/usr/bin/env python3
"""Created 2026-10-15. Reconcile a complete sweep with the focused restart-observer correction."""
from pathlib import Path
from collections import Counter
import json
import lzma
import xml.etree.ElementTree as ET
NS={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}

def results(path):
    path=Path(path)
    if not path.exists():path=Path(str(path)+".xz")
    tree=ET.ElementTree(ET.fromstring(lzma.open(path,"rt").read())) if path.suffix==".xz" else ET.parse(path)
    definitions={n.get('id'):n.find('t:TestMethod',NS) for n in tree.findall('.//t:UnitTest',NS)}
    for row in tree.findall('.//t:UnitTestResult',NS):
        method=definitions[row.get('testId')]
        key=(Path(method.get('codeBase')).name,method.get('className'),row.get('testName'))
        yield key,row.get('outcome')

work=Path(__file__).resolve().parent
base=work/'pr-gate' if (work/'pr-gate').is_dir() else Path('artifacts/coverage')
files=list(base.rglob('*.trx.xz')) if (work/'pr-gate').is_dir() else list(base.glob('*.trx'))
assert len(files)==37
sweep={}
for path in files:
    for key,outcome in results(path):
        assert key not in sweep,key
        sweep[key]=outcome
focused=dict(results(work/'final-focused.trx'))
assert Counter(focused.values())=={'Passed':165}
old_failed={k for k,v in sweep.items() if v=='Failed'}
assert len(old_failed)==1 and all(k in focused and focused[k]=='Passed' for k in old_failed)
extra=set(focused)-set(sweep)
assert len(extra)==2 and all('ClaimObservation_EndsOnExplicitSameKeeperRestart' in k[2] for k in extra)
current={**sweep,**focused};counts=Counter(current.values());assert counts.get('Failed',0)==0
summary={'fullSweepSource':'e54335b0','fullSweepExit':1,'fullSweepOutcomes':dict(Counter(sweep.values())),
    'correctedSource':'86a9496e','focusedOutcomes':dict(Counter(focused.values())),
    'resolvedFailures':[list(k) for k in sorted(old_failed)],'addedCases':[list(k) for k in sorted(extra)],
    'reconciledOutcomes':dict(counts),'cleanSingleInvocation':False,
    'limitation':'The raw full policy command failed on the diagnosed observer. Focused retest resolves it; this is not a claim of a fresh clean full-policy invocation or Unity validation.'}
(work/'test-reconciliation.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary,indent=2))
