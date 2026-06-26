// File:     src/first-touch/AssemblyInfo.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.FirstTouch.
//           Grants the test assembly access to internal types so the closed-loop
//           scenario corpus can drive the real PressureEvaluator producer seam, and
//           the match-engine composition root so it can run the same PressureEvaluator /
//           OrientationDetector context-assembly seams when wiring first-touch (Phase D D3).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.FirstTouch.Tests")]
[assembly: InternalsVisibleTo("TacticalDirector.MatchEngine")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-06-10 | —      | Initial creation (scenario-corpus pass): InternalsVisibleTo for the |
// |         |            |        | test assembly, parallel to pass-mechanics / agent-movement.         |
// | 1.1     | 2026-06-22 | —      | Match Engine Phase D D3: InternalsVisibleTo for the match-engine     |
// |         |            |        | composition root so RunFirstTouch can assemble the FirstTouchContext |
// |         |            |        | via the internal PressureEvaluator / OrientationDetector seams (the  |
// |         |            |        | design-note "PressureEvaluator pass + OrientationDetector" producer  |
// |         |            |        | path) rather than duplicating the §3.5 / §3.6 formulas in the host.  |
#endregion
