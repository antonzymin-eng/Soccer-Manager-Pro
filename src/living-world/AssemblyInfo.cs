// File:     src/living-world/AssemblyInfo.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Living World System #22 §4.3, Code Standards #20 FR-CS-015
// Purpose:  Assembly-level attributes for TacticalDirector.LivingWorld.
//           Grants the test assembly access to internal types for unit testing.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.LivingWorld.Tests")]
