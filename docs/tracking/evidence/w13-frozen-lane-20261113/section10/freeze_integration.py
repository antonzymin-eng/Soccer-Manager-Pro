"""Created 2026-11-13. Freeze runtime merge for measurement; do not resolve/publish landing docs."""
from pathlib import Path
import re,subprocess,os,json
ROOT=Path(__file__).resolve().parent
WORK=ROOT/'integration'
def git(*args,**kwargs):return subprocess.check_output(['git','-C',str(WORK),*args],**kwargs)
def main():
    resolutions=[]
    for path,expected in [('src/match-engine/MatchEngine.cs',1),('src/match-engine/MatchEngineConstants.cs',2)]:
        p=WORK/path;source=p.read_text();pattern=re.compile(r'^<<<<<<<[^\n]*\n(.*?)^=======\n(.*?)^>>>>>>>[^\n]*\n',re.M|re.S)
        def keep(match):
            a,b=match.group(1),match.group(2)
            if not all(not line.strip() or line.startswith('//') for line in (a+b).splitlines()):raise RuntimeError('functional source conflict: '+path)
            b=b.replace('// | 2.18 |','// | 2.22 |').replace('// | 1.47 |','// | 1.48 |')
            return a+b
        source,count=pattern.subn(keep,source);assert count==expected,(path,count)
        p.write_text(source);git('add',path)
        resolutions.append({'path':path,'conflicts':count,'resolution':'both comment blocks retained; #543 history appended/renumbered'})
    env=os.environ.copy();env['GIT_INDEX_FILE']=str(ROOT/'measurement-source.index')
    git('read-tree','4171d3d1',env=env)
    prefixes=['src/','tools/dotnet-ci/','Packages/','ProjectSettings/']
    rows=[]
    for record in git('ls-files','--stage','-z').split(b'\0'):
        if not record:continue
        header,name=record.split(b'\t',1);mode,oid,stage=header.decode().split();name=name.decode()
        if name!='Directory.Build.targets' and not any(name.startswith(p) for p in prefixes):continue
        if stage!='0':raise RuntimeError('unresolved source '+name)
        rows.append(f'{mode} {oid}\t{name}\n')
    subprocess.run(['git','-C',str(WORK),'update-index','--index-info'],input=''.join(rows),text=True,env=env,check=True)
    tree=git('write-tree',env=env,text=True).strip()
    env.update(GIT_AUTHOR_NAME='Codex',GIT_AUTHOR_EMAIL='codex@openai.com',GIT_COMMITTER_NAME='Codex',GIT_COMMITTER_EMAIL='codex@openai.com')
    message='measurement-only: W13 4171d3d1 plus reviewed #543-on-main 64592405\n\nRuntime/source integration for W13 section 10 only. Source conflicts were comments; both sides retained. Non-source landing documentation deliberately remains the W13 parent snapshot. This is NOT a resolved landing merge or approval to enable.\n'
    commit=git('commit-tree',tree,'-p','4171d3d1','-p','64592405',input=message,text=True,env=env).strip()
    git('update-ref','refs/codex/w13-section10-measurement',commit)
    receipt={'reference':'645924057694fd9aa8bf2ab33e43a5810d3d3feb','w13':'4171d3d1e3f07a661e085ffa6f51770384af92c8',
        'measurementCommit':commit,'tree':tree,'srcTree':git('rev-parse',commit+':src',text=True).strip(),
        'scope':'source-only measurement integration; not a resolved landing merge','sourceConflicts':resolutions}
    (ROOT/'integration-source.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt,indent=2))
if __name__=='__main__':main()
