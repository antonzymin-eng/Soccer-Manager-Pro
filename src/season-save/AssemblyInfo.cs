// File:     src/season-save/AssemblyInfo.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 KD-7 / FR-SN-032; Code Standards #20
// Purpose:  Exposes this assembly's internals to its own test assembly so the KD-7 single-writer
//           mutators on SeasonState (MarkFixturePlayed / AdvanceCursorOneRound / SetBoard /
//           BeginNextSeason) can be exercised directly. Production writers stay inside this assembly
//           (SeasonLoop, #30 T2) — the internal visibility IS the single-writer enforcement.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.SeasonSave.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): test InternalsVisibleTo. |
#endregion
