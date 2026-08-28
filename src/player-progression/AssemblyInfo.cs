// File:     src/player-progression/AssemblyInfo.cs
// Created:  2026-08-23
// Modified: 2026-08-24 (round-2 finding fr-cs-015-rationale-false-and-applied-unevenly — v1.1)
// Author:   —
// Spec:     Player Progression & Lifecycle #28, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.PlayerProgression. Grants the test assembly
//           access to the internal members FR-CS-015 makes internal rather than public — the four
//           dial-taking test affordances (TestOnly_DailyBandPoints, TestOnly_AccruedBandPoints,
//           TestOnly_RetirementAgeDays, TestOnly_GameReadingOffsetDays, so the [GT] dials they bypass
//           can be EXERCISED by a test rather than only asserted in prose, the ERR-008-021/-022
//           posture) and AbilityModel.ClassifyAgeBand (no cross-assembly caller, and its own
//           production caller — ProgressionEngine.LifecycleView — lives in this assembly). The rule
//           actually applied is NOT "no cross-assembly caller ⇒ internal" (round-2 finding
//           fr-cs-015-rationale-false-and-applied-unevenly corrected the v1.0 sentence that claimed
//           it — that predicate is equally true of DailyBandPoints(long)/AccruedBandPoints(long)/
//           RetirementAgeDays(in rec)/TrySpendOnePoint/DrainOnePoint, all of which stayed public): it
//           is that a DIAL-TAKING form bypassing the catalogue is a test affordance and internal, while
//           a CATALOGUE-READING form is #28's published pure-arithmetic API and stays public whether or
//           not anything outside this assembly calls it yet.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.PlayerProgression.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-08-23 | —      | Initial creation. Without it, demoting AbilityModel's parameterised |
// |         |            |        | overloads from public to internal (v1.3, this landing) would have  |
// |         |            |        | broken AbilityModelTests' existing calls to them.                  |
// | 1.1     | 2026-08-24 | —      | Round-2 finding fr-cs-015-rationale-false-and-applied-unevenly.    |
// |         |            |        | Purpose text's "none of them has a cross-assembly production       |
// |         |            |        | caller ... so FR-CS-015 makes them internal" was true of far more  |
// |         |            |        | than the four demoted members and was not the rule actually        |
// |         |            |        | applied (round 1's own config-unbound-premise-false-28 class:      |
// |         |            |        | a rationale copied without checking, left to govern the next       |
// |         |            |        | landing). Replaced with the rule that was applied — dial-taking    |
// |         |            |        | forms are internal test affordances, catalogue-reading forms stay  |
// |         |            |        | public API regardless of caller count. Renamed the four members    |
// |         |            |        | to their new TestOnly_ names (AbilityModel.cs v1.5); added         |
// |         |            |        | ClassifyAgeBand to the covered set (also demoted internal at v1.5, |
// |         |            |        | M7 — see AbilityModel.cs and LifecycleViewModel.cs v1.1). No       |
// |         |            |        | access-modifier or IVT-target change here — the grant already      |
// |         |            |        | covers every internal member; only the doc text and the names it   |
// |         |            |        | cites change.                                                      |
#endregion
