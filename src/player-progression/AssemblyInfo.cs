// File:     src/player-progression/AssemblyInfo.cs
// Created:  2026-08-23
// Modified: 2026-08-23
// Author:   —
// Spec:     Player Progression & Lifecycle #28, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.PlayerProgression. Grants the test
//           assembly access to the internal parameterised overloads (DailyBandPoints, AccruedBandPoints,
//           GameReadingOffsetDays, RetirementAgeDays) that exist so the [GT] dials they read can be
//           EXERCISED by a test rather than only asserted in prose (the ERR-008-021/-022 posture) —
//           none of them has a cross-assembly production caller (classifyageband-no-production-caller
//           finding, football-judgment proxy review batch 1), so FR-CS-015 makes them internal and this
//           file is what lets TacticalDirector.PlayerProgression.Tests still reach them.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.PlayerProgression.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-08-23 | —      | Initial creation. Without it, demoting AbilityModel's parameterised |
// |         |            |        | overloads from public to internal (v1.3, this landing) would have  |
// |         |            |        | broken AbilityModelTests' existing calls to them.                  |
#endregion
