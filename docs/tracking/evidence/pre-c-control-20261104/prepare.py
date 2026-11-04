"""Created 2026-11-04: isolated, reproducible Pre-C source/build preparation."""
from pathlib import Path
import hashlib, json, subprocess, tarfile, xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
REPO = Path('/workspace/Soccer-Manager-Pro')
FROZEN = REPO / 'docs/tracking/evidence/pre-b-anchor-3974b78-20261013/W8StageADiagnosticTests-520ba31c.cs.txt'

def replace(path, old, new, count=1):
    text = path.read_text()
    assert text.count(old) == count, (str(path), old, text.count(old), count)
    path.write_text(text.replace(old, new))

for arm in ('plain', 'observed'):
    root = HERE / arm
    root.mkdir(exist_ok=True)
    with tarfile.open(HERE / 'source.tar') as archive:
        archive.extractall(root, filter='data')
    subprocess.run(['python3', str(root/'tools/dotnet-ci/generate_projects.py')], cwd=root, check=True)
    harness = root / 'capture'
    harness.mkdir(exist_ok=True)
    (harness/'FrozenW8.cs').write_bytes(FROZEN.read_bytes())
    (harness/'Program.cs').write_bytes((HERE/'Program.cs').read_bytes())
    projects = ET.parse(root/'src/match-engine/tests/match-engine-tests.gen.csproj')
    refs = [p.attrib['Include'] for p in projects.iter('ProjectReference')]
    paths = [(root/'src/match-engine/tests'/p).resolve() for p in refs]
    project = '''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>9.0</LangVersion>
<AssemblyName>TacticalDirector.MatchEngine.Tests</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo>
<Deterministic>true</Deterministic><Optimize>false</Optimize><DefineConstants>DEVELOPMENT_BUILD</DefineConstants>
</PropertyGroup><ItemGroup>'''
    project += ''.join(f'<ProjectReference Include="{p}" />' for p in paths)
    project += '<PackageReference Include="NUnit" Version="3.14.0" /></ItemGroup></Project>'
    (harness/'Capture.csproj').write_text(project)
    (root/'global.json').write_text('{"sdk":{"version":"8.0.426","rollForward":"disable"}}\n')
    if arm == 'plain':
        continue
    engine = root/'src/match-engine/MatchEngine.cs'
    replace(engine, 'RecordPlayerBallTouch(toucher);', 'PreCObserve("touch:first-touch", toucher, _ball.Position);\n            RecordPlayerBallTouch(toucher);')
    replace(engine, 'TakeControlledPossession(claimer);', 'PreCObserve("touch:loose-pickup", claimer, _ball.Position);\n                TakeControlledPossession(claimer);')
    replace(engine, 'case TackleOutcome.BallWon:\n', 'case TackleOutcome.BallWon:\n                    PreCObserve("touch:tackle-won", tackler, _ball.Position);\n')
    replace(engine, 'case TackleOutcome.BallLoose:\n', 'case TackleOutcome.BallLoose:\n                    PreCObserve("touch:tackle-loose", tackler, _ball.Position);\n')
    replace(engine, '_engine.RecordPlayerBallTouch(agentId);\n                _engine.ObserveW8(W8StageAKind.PassKick', '_engine.PreCObserve("touch:pass-contact", agentId, ball.Position);\n                _engine.RecordPlayerBallTouch(agentId);\n                _engine.ObserveW8(W8StageAKind.PassKick')
    replace(engine, '_engine.RecordPlayerBallTouch(agentId);\n                _engine.ObserveW8(W8StageAKind.ShotKick', '_engine.PreCObserve("touch:shot-contact", agentId, ball.Position);\n                _engine.RecordPlayerBallTouch(agentId);\n                _engine.ObserveW8(W8StageAKind.ShotKick')
    replace(engine, '_engine.RecordPlayerBallTouch(agentId);\n                _engine.ObserveW8(W8StageAKind.GkHeadingKick', '_engine.PreCObserve("gk-heading-contact", agentId, _engine._ball.Position);\n                _engine.RecordPlayerBallTouch(agentId);\n                _engine.ObserveW8(W8StageAKind.GkHeadingKick')
    replace(engine, '_engine.ObserveW8(W8StageAKind.HandClaim, agentId);', '_engine.PreCObserve("hand-claim-response", agentId, _engine._ball.Position);\n                _engine.ObserveW8(W8StageAKind.HandClaim, agentId);')
    replace(engine, 'public bool TryHandContact(int agentId, Vector3 contactPosition) =>\n                _engine.TryKeeperHandContact(agentId, contactPosition);', '''public bool TryHandContact(int agentId, Vector3 contactPosition)
            {
                _engine.PreCObserve("hand-attempt", agentId, contactPosition);
                bool applied = _engine.TryKeeperHandContact(agentId, contactPosition);
                if (applied) _engine.PreCObserve("touch:hand-contact", agentId, contactPosition);
                return applied;
            }''')
    law = root/'src/match-engine/MatchEngine.KeeperHandlingLaw.cs'
    replace(law, 'if (_releasedHandKeeper != agentId) _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;', '''int preCBefore = _releasedHandKeeper;
            if (_releasedHandKeeper != agentId) _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;
            if (preCBefore != _releasedHandKeeper) PreCObserve("restriction-clear:touch", agentId, _ball.Position);''')
    replace(law, '_releasedHandKeeper = agentId;', '{ _releasedHandKeeper = agentId; PreCObserve("release", agentId, _ball.Position); }')
    replace(law, 'private void ClearHandlingLawAtRestart()\n        {', 'private void ClearHandlingLawAtRestart()\n        {\n            PreCObserve("restriction-end:restart", _releasedHandKeeper, _ball.Position);')
    partial = '''// Created 2026-11-04. Scratch-only observation overlay; no canonical state.
using System;
using UnityEngine;
namespace TacticalDirector.MatchEngine {
public sealed partial class MatchEngine {
internal Action<string,int,Vector3,Vector3,int,int,int> PreCObserver;
internal void PreCAttach(Action<string,int,Vector3,Vector3,int,int,int> observer) {
 PreCObserver=observer;
 _collisionSystem.PreCAppliedTouchObserver=(id,contact,position)=>observer("touch:body-applied",id,contact,position,_releasedHandKeeper,_pendingHandlingOffender,_indirectKickTaker);
}
private void PreCObserve(string kind,int id,Vector3 contact) {
 if(PreCObserver==null)return;
 Vector3 position=id>=0 ? new Vector3(_agents[id].Position.x,_agents[id].Position.y,0f):Vector3.zero;
 PreCObserver(kind,id,contact,position,_releasedHandKeeper,_pendingHandlingOffender,_indirectKickTaker);
}
}}
'''
    (root/'src/match-engine/MatchEngine.PreCObservation.cs').write_text(partial)
    collision = root/'src/collision-system/CollisionSystem.cs'
    replace(collision, 'public uint AppliedBallTouchMask { get; private set; }', 'public uint AppliedBallTouchMask { get; private set; }\n        public System.Action<int, Vector3, Vector3> PreCAppliedTouchObserver;')
    replace(collision, 'AppliedBallTouchMask |= 1u << agentId;', 'PreCAppliedTouchObserver?.Invoke(agentId, contactPoint, snap.Position);\n                AppliedBallTouchMask |= 1u << agentId;')
    bus = root/'src/event-system/EventBus.cs'
    replace(bus, 'public static class EventBus\n    {', 'public static class EventBus\n    {\n        public static Action<byte, byte[]> PreCPublishObserver;')
    replace(bus, 'StructSize           = structSize,\n            };', '''StructSize           = structSize,
            };
            if (PreCPublishObserver != null) {
                byte[] preCCopy = new byte[structSize];
                Array.Copy(buf, slotOffset, preCCopy, 0, structSize);
                PreCPublishObserver(ordinal, preCCopy);
            }''')

print('Prepared plain and observed copies; shipping checkout untouched.')
