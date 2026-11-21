// File:     src/discipline/AssemblyInfo.cs
// Created:  2026-08-13
// Modified: 2026-11-20 (#44 T3 review: friend access to the card summary entry point.)
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 / §4.2; Code Standards #20
// Purpose:  Lets discipline tests pin internal behavior and SeasonSave submit its quick-sim card
//           summaries through the fixture fold. DisciplineRules owns public rule application.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.Discipline.Tests")]
[assembly: InternalsVisibleTo("TacticalDirector.SeasonSave")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0): InternalsVisibleTo. |
// | 1.1     | 2026-11-20 | —      | #44 T3 review: SeasonSave friend for card summaries. |
#endregion
