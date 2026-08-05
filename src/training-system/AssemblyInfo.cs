// File:     src/training-system/AssemblyInfo.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §3.1/§3.4; Code Standards #20
// Purpose:  Exposes this assembly's internals to its own test assembly so the deterministic
//           own-attribute terms (AttributeConditioningBonus, RobustnessMitigation) and the KD-3
//           ApplyCoach seam can be pinned directly. They stay internal in production because
//           TrainingStep's four public entry points are the whole #29 surface (FR-TR-004).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.TrainingSystem.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0): InternalsVisibleTo. |
#endregion
