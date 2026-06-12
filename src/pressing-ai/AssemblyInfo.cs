// File:     src/pressing-ai/AssemblyInfo.cs
// Created:  2026-06-12
// Modified: 2026-06-12
// Author:   —
// Spec:     Pressing AI #13, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.PressingAI.
//           Grants the test assembly access to internal types for unit testing
//           (TriggerEvaluator per-trigger internals, ComputeGeometricPressure).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.PressingAI.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-12 | —      | Initial creation (dotnet CI gate): PressingAITests calls the      |
// |         |            |        | internal TriggerEvaluator.EvaluateBadTouch/EvaluateBackwardPass/  |
// |         |            |        | EvaluateSidelineTrap/EvaluateWeakReceiver/ComputeGeometricPressure|
// |         |            |        | members but no InternalsVisibleTo existed — the test assembly was |
// |         |            |        | uncompilable under Unity and the Linux gate alike (CS0117).       |
// |         |            |        | Pattern per agent-movement/pass-mechanics/first-touch precedent.  |
#endregion
