// File:     src/ui-framework/AssemblyInfo.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38, Code Standards #20
// Purpose:  Grants the test assembly access to this assembly's internals, so the intent→command
//           translation (internal by design — it is a dispatcher implementation detail, not client API)
//           can be locked directly by the drift-guard test rather than only observed through its
//           effects.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.UiFramework.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation: InternalsVisibleTo for the test assembly.    |
#endregion
