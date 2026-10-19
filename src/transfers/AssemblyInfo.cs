// File:     src/transfers/AssemblyInfo.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     Transfers, Contracts & Negotiation #31 §4.4 / §5.5; Code Standards #20
// Purpose:  Exposes this assembly's internals to its own test assembly so the T1 codec's restore path
//           (TransfersState.Restore) can be driven with non-default spend/window state directly. The
//           restore factory stays internal in production because SubmitBid is the only public writer
//           of committed spend (Spec #31 KD-2).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TacticalDirector.Transfers.Tests")]

#region VersionHistory
// | Version | Date       | Author | Notes                                                |
// | 1.0     | 2026-10-18 | —      | Initial implementation (#31 T1): InternalsVisibleTo. |
#endregion
