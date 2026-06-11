// File:     src/first-touch/AssemblyInfo.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.FirstTouch.
//           Grants the test assembly access to internal types so the closed-loop
//           scenario corpus can drive the real PressureEvaluator producer seam.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.FirstTouch.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-06-10 | —      | Initial creation (scenario-corpus pass): InternalsVisibleTo for the |
// |         |            |        | test assembly, parallel to pass-mechanics / agent-movement.         |
#endregion
