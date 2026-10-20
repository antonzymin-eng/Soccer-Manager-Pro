// File:     src/client-app/tests/CareerTestSupport.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7, Code Standards #20
// Purpose:  Shared fixtures for the career suites: a disposable temporary slot directory and the
//           quick-sim career every suite plays (QuickSimAll keeps a whole season in test budget).

using System;
using System.IO;

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp.Tests
{
    /// <summary>A slot store in a fresh temporary directory, deleted on dispose.</summary>
    internal sealed class TempSlots : IDisposable
    {
        public TempSlots()
        {
            Directory = Path.Combine(Path.GetTempPath(), "td-career-" + Guid.NewGuid().ToString("N"));
            Slots = new CareerSaveSlots(Directory);
        }

        public string Directory { get; }

        public CareerSaveSlots Slots { get; }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }

    internal static class CareerTestSupport
    {
        /// <summary>The world seed every career suite uses unless a test needs a second world.</summary>
        public const ulong WORLD_SEED = 0xC3C4UL;

        /// <summary>The managed club every career suite uses.</summary>
        public const int MANAGED_CLUB = 3;

        public static CareerSession StartQuick(CareerSaveSlots slots) =>
            CareerSession.StartNew(WORLD_SEED, MANAGED_CLUB, slots, RoundResolutionMode.QuickSimAll);

        /// <summary>Progresses until <paramref name="action"/> is next (bounded), returning the number of steps taken.</summary>
        public static int ProgressUntil(CareerSession session, CareerProgressAction action, int maxSteps = 2000)
        {
            int steps = 0;
            while (session.NextAction != action)
            {
                if (++steps > maxSteps)
                {
                    throw new InvalidOperationException("Never reached " + action + " within " + maxSteps + " steps.");
                }

                session.Progress();
            }

            return steps;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
