// File:     src/discipline/tests/DisciplineConfigCompletenessTests.cs
// Created:  2026-08-16
// Modified: 2026-08-16
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.3 F6 (the guarded [GT] list) + Appendix A (constant
//           catalogue); ERR-041-003 and AR pass 10's severity-split finding (the twice-filed lesson
//           that a catalogue lock running config-unbound cannot see a shipped config's breach);
//           Code Standards #20
// Purpose:  The completeness lock for #44's guarded [GT] set — the reviewer's M2 finding, taken at its
//           minimal shape. Adding a config-settable constant to DisciplineConstants without extending
//           the guard chain reintroduces exactly the silent-breach class the guards exist to close, and
//           nothing in the tree noticed; this test does.

using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>
    /// Locks the SET of config-settable <c>[GT]</c> constants in <see cref="DisciplineConstants"/>
    /// against the set the guard chain actually covers.
    /// <para>
    /// <b>Why a reflective set assertion rather than four more value checks.</b> #44's guards are
    /// deliberately at the writing sites rather than in the catalogue (ERR-041-003): the catalogue's own
    /// locks run config-unbound, so they see the design-time fallbacks forever while a shipped config
    /// violates the invariant, green all the way. That posture only holds while EVERY config-settable
    /// constant is actually guarded — and nothing enforced that. A fifth <c>[GT]</c> added to the
    /// catalogue would inherit no guard, and no existing test would say a word.
    /// </para>
    /// <para>
    /// <b>The chain this stands in for.</b> A guarded <c>[GT]</c> is reachable from five places at once:
    /// <see cref="DisciplineRules"/>' site guard, BOTH <c>CardLedgerFold.RequireCommittableConfig</c>
    /// forms, <c>CardLedgerFold.CommitWithExplicitConfig</c>'s parameter list, and #44 §2.3 F6's guard
    /// list in the spec. Asserting the set here is one cheap check that fails the moment any of those
    /// five falls behind the catalogue, which is the only failure mode a per-constant test cannot see.
    /// </para>
    /// <para>
    /// <b>Reflection is legal here.</b> FR-CS-034 bans it on the game loop; this is a boot-cadence
    /// assertion in a test assembly, run once, touching no simulation state.
    /// </para>
    /// <para>
    /// <b>Recorded, not built (M2's own ruling): the eventual shape is a <c>DisciplineConfig</c></b> —
    /// one immutable struct validated once at construction, so the guard list cannot drift from the
    /// constant list by construction rather than by test. That restructure is gated on the
    /// <c>GameplayConfigHolder.Bind</c> composition-root pass, which no production caller runs yet
    /// (<c>src/CLAUDE.md</c>, "Boot-sequencing resolution"); until a config is actually bound, the
    /// restructure buys nothing this test does not, and costs a public surface change. Minimal now,
    /// structural then.
    /// </para>
    /// </summary>
    [TestFixture]
    internal sealed class DisciplineConfigCompletenessTests
    {
        /// <summary>
        /// The <c>[GT]</c>s the guard chain covers today, in the order #44 §2.3 F6 lists them.
        /// </summary>
        private static readonly string[] GuardedGameplayTunedConstants =
        {
            nameof(DisciplineConstants.YellowAccumulationThreshold),
            nameof(DisciplineConstants.AccumBanMatches),
            nameof(DisciplineConstants.SecondYellowBanMatches),
            nameof(DisciplineConstants.StraightRedBanMatches),
        };

        [Test]
        public void EveryConfigSettableIntConstant_IsOneOfTheGuardedFour()
        {
            FieldInfo[] fields = typeof(DisciplineConstants).GetFields(
                BindingFlags.Public | BindingFlags.Static);

            var settable = new System.Collections.Generic.List<string>();
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                // `public static readonly int` and nothing else. A `const` is IsLiteral and cannot be
                // config-settable at all (it is baked into every reading assembly at compile time), so
                // the [FIXED]/[CROSS] ordinals and the save magic are correctly out of scope; a
                // non-int field would not be readable through Config.GetInt in the first place.
                if (field.IsLiteral || !field.IsInitOnly || field.FieldType != typeof(int))
                {
                    continue;
                }

                settable.Add(field.Name);
            }

            settable.Sort();
            var expected = new System.Collections.Generic.List<string>(GuardedGameplayTunedConstants);
            expected.Sort();

            Assert.That(
                settable, Is.EqualTo(expected),
                "DisciplineConstants' config-settable [GT] set has changed. A new guarded [GT] must be "
                + "added to DisciplineRules' site guard, BOTH RequireCommittableConfig forms, "
                + "CommitWithExplicitConfig, #44 §2.3 F6's guard list, and this test.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-16 | —      | Initial (adversarial review, M2 minimal fix): the completeness   |
// |         |            |        | lock over DisciplineConstants' public static readonly int set.   |
// |         |            |        | #44 puts its [GT] guards at the writing sites (ERR-041-003)      |
// |         |            |        | because catalogue locks run config-unbound; nothing checked that  |
// |         |            |        | the guarded set still equalled the settable set, so a fifth [GT]  |
// |         |            |        | would have shipped unguarded and silent. The DisciplineConfig     |
// |         |            |        | restructure stays recorded, gated on the GameplayConfigHolder.    |
// |         |            |        | Bind composition-root pass no production caller runs yet.         |
#endregion
