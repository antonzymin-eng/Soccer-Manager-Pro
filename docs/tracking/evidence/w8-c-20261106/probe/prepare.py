from pathlib import Path
import subprocess,tarfile,io,shutil
root=Path.cwd(); work=root/'work/w8c'
changed=subprocess.check_output(['git','diff','--name-only'],text=True).splitlines()
added=['src/match-engine/MatchEngine.KeeperHandControlLaw.cs','src/match-engine/tests/W8CHandControlTests.cs']
archive=subprocess.check_output(['git','archive','HEAD'])
for arm in ['B','C']:
 target=work/('arm-'+arm);target.mkdir(exist_ok=True)
 with tarfile.open(fileobj=io.BytesIO(archive)) as f:f.extractall(target,filter='data')
 if arm=='C':
  for name in changed+added:
   dest=target/name;dest.parent.mkdir(exist_ok=True,parents=True);shutil.copy2(root/name,dest)
 # Identical observation-only patches to both arms; frozen W8 source is unchanged.
 p=target/'src/collision-system/CollisionSystem.cs';s=p.read_text()
 at='        public uint AppliedBallTouchMask { get; private set; }'
 s=s.replace(at,at+'\n        public System.Action<int, Vector3> W8ProbeAppliedTouch;')
 s=s.replace('AppliedBallTouchMask |= 1u << agentId;', 'W8ProbeAppliedTouch?.Invoke(agentId, contactPoint);\n                AppliedBallTouchMask |= 1u << agentId;')
 p.write_text(s)
 p=target/'src/match-engine/MatchEngine.KeeperHandlingLaw.cs';s=p.read_text()
 s=s.replace('            _releasedHandKeeper = agentId;', '            _releasedHandKeeper = agentId;\n            ProbeRecord("release", agentId, _ball.Position);')
 s=s.replace('            bool restricted = _releasedHandKeeper == agentId;', '            bool restricted = _releasedHandKeeper == agentId;')
 s=s.replace('            RecordPlayerBallTouch(agentId);\n            return true;', '            ProbeRecord("hand", agentId, position);\n            RecordPlayerBallTouch(agentId);\n            return true;')
 s=s.replace('            if (_releasedHandKeeper != agentId) _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;', '            if (_releasedHandKeeper != agentId && _releasedHandKeeper >= 0)\n            {\n                _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;\n                ProbeRecord("restriction-end-touch", agentId, _ball.Position);\n            }')
 s=s.replace('        private void ClearHandlingLawAtRestart()\n        {','        private void ClearHandlingLawAtRestart()\n        {\n            ProbeRecord("restart", -1, _ball.Position);')
 p.write_text(s)
 p=target/'src/match-engine/MatchEngine.cs';s=p.read_text()
 s=s.replace('            RecordPlayerBallTouch(toucher);', '            ProbeRecord("first-touch", toucher, _ball.Position);\n            RecordPlayerBallTouch(toucher);')
 s=s.replace('        private void TakeControlledPossession(int agentId)\n        {','        private void TakeControlledPossession(int agentId)\n        {\n            ProbeRecord("acquire", agentId, _ball.Position);')
 # Three adapter kick sites, in source order: pass, shot, shared heading/GK.
 needle='                _engine.RecordPlayerBallTouch(agentId);'
 for kind in ['pass','shot-touch','heading-or-gk']:
  start=s.index(needle);s=s[:start]+s[start:].replace(needle,f'                _engine.ProbeRecord("{kind}", agentId, _engine._ball.Position);\n'+needle,1)
  # Keep the next search from finding the same source site.
  s=s[:start]+s[start:].replace(needle,'                _engine.RecordPlayerBallTouch( agentId);',1)
 # Shot marker after its actual kick/touch/release; no other gameplay callback intervenes.
 a=s.index('private sealed class ShotWorldAdapter'); b=s.index('private sealed class GkHeadingWorldAdapter',a)
 part=s[a:b].replace('                _engine.ReleasePossessionOnKick(agentId);','                _engine.ReleasePossessionOnKick(agentId);\n                _engine.ProbeRecord("shot", agentId, _engine._ball.Position);')
 s=s[:a]+part+s[b:]
 s=s.replace('            payload.BytesWritten += written;', '            payload.BytesWritten += written;\n            W8ProbeLedgerObserver?.Invoke(payload.PayloadBytes, payload.BytesWritten - written, written);')
 s=s.replace('            _goals[scoringTeam]++;','            _goals[scoringTeam]++;\n            ProbeRecord("goal", _lastHolderAgentId, _ball.Position, scoringTeam);')
 p.write_text(s)
 (target/'src/match-engine/MatchEngine.W8Probe.cs').write_text((root/'work/w8c/probe/MatchEngine.W8Probe.cs').read_text())
 (target/'src/match-engine/tests/W8CComparisonDiagnosticTests.cs').write_text((root/'work/w8c/probe/W8CComparisonDiagnosticTests.cs').read_text())
print('Prepared identical observational instrumentation over B HEAD and C feature tree.')
