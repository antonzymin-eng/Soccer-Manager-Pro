"""Created 2026-11-13. Minimal source transformation of the exact #543 v1.1 capture runner."""
from pathlib import Path
import hashlib
ROOT=Path(__file__).resolve().parent
source=(ROOT/'source/OriginalProgram.cs.txt').read_text()
assert hashlib.sha256(source.encode()).hexdigest()=='e6db2df46a364aa6e08204279c4ab468addc7260c54add337729fedd16105f75'
def replace(a,b):
 global source
 assert source.count(a)==1,(a,source.count(a))
 source=source.replace(a,b)
replace('static class Program','static partial class Program')
replace('using System.Text.Json;', 'using System.Text.Json;\nusing System.Runtime.CompilerServices;\nusing TacticalDirector.ProjectConstants;')
replace('    static int Main(string[] args) {', '''    // Boot is intentionally separate from Run: beforefieldinit catalogues must not execute before config injection.
    static int Main(string[] args) {
        bool armed = args.Length > 5 && args[5] == "armed";
        GameplayConfigHolder.Bind(new GameplayConfig(new Dictionary<string,string> {
            { "match-engine.MarkMovementEnabled", armed ? "true" : "false" }
        }));
        return Run(args, armed);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Run(string[] args, bool armed) {''')
replace('            if(args[0]=="--self-test")', '            if(args[0]=="--state-proof") { ProveState(args[3], armed); return 0; }\n            if(args[0]=="--self-test")')
replace('            var e=new MatchEngine(seed);e.EnableGkHeading();', '''            var e=new MatchEngine(seed);e.EnableGkHeading();
            var markingField=typeof(MatchEngineConstants).GetField("MarkMovementEnabled");
            bool markingEnabled=markingField!=null && (bool)markingField.GetValue(null);
            Need(markingEnabled==armed,"config activation did not take effect");
            using var marking=observed ? new MarkObserver(e,output,armed) : null;''')
replace('                e.RunTick();var h=', '                marking?.BeforeTick();\n                e.RunTick(); marking?.RetainedTick(frame); var h=')
replace('                    int team=-1;', '                    marking?.Event(ordinal,eventBytes);\n                    int team=-1;')
replace('                    geometry.Write(frame);', '                    marking?.Sample(frame);\n                    geometry.Write(frame);')
replace('mode,frames,live,shots,goals,fouls,possession,teamPossession,phase,schema,buildHash=', 'mode,armed,markingEnabled,marking=marking?.Summary(),frames,live,shots,goals,fouls,possession,teamPossession,phase,schema,buildHash=')
(ROOT/'source/Program.cs').write_text(source)
print('W13 section 10 runner generated:',hashlib.sha256(source.encode()).hexdigest())
