// File:     src/client-app/AssemblyInfo.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Grants deterministic lifecycle tests the narrow headless transaction seam.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.ClientApp.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
