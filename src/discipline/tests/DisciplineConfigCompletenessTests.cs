// File:     src/discipline/tests/DisciplineConfigCompletenessTests.cs
// Created:  2026-08-16
// Modified: 2026-08-16, latest (finding C — v1.1: widened the field filter from `public static
//           readonly int` to `public static readonly` of ANY type. The int-only filter saw only the
//           four known ints, so a future [GT] read through Config.GetBool/GetFloat/GetString (the #41
//           FR-MD-027 shape) would have been silently excluded from the reflected set and this test
//           would have kept passing at four while a fifth, differently-typed, unguarded constant
//           shipped — the single failure mode this test exists to catch. Test renamed
//           EveryConfigSettableIntConstant_IsOneOfTheGuardedFour ->
//           EveryPublicStaticReadonlyConstant_IsOneOfTheGuardedFour, since it no longer names or
//           implies an int-only scope. Behaviour-neutral on today's catalogue: DisciplineConstants
//           carries no non-int public static readonly field yet, so the reflected set is still exactly
//           the same four.)
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.3 F6 (the guarded [GT] list) + Appendix A (constant
//           catalogue); ERR-041-003 and AR pass 10's severity-split finding (the twice-filed lesson
//           that a catalogue lock running config-unbound cannot see a shipped config's breach);
//           Code Standards #20
// Purpose:  The completeness lock for #44's guarded [GT] set — the reviewer's M2 finding. Adding a
//           config-settable constant of ANY type to DisciplineConstants without extending the guard
//           chain reintroduces exactly the silent-breach class the guards exist to close, and nothing
//           in the tree noticed; this test does.

using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>
    /// Locks the SET of config-settable constants — <c>public static readonly</c>, of ANY type — in
    /// <see cref="DisciplineConstants"/> against the set the guard chain actually covers.
    /// <para>
    /// <b>Why a reflective set assertion rather than four more value checks.</b> #44's guards are
    /// deliberately at the writing sites rather than in the catalogue (ERR-041-003): the catalogue's own
    /// locks run config-unbound, so they see the design-time fallbacks forever while a shipped config
    /// violates the invariant, green all the way. That posture only holds while EVERY config-settable
    /// constant is actually guarded — and nothing enforced that. A fifth <c>[GT]</c> added to the
    /// catalogue would inherit no guard, and no existing test would say a word.
    /// </para>
    /// <para>
    /// <b>Every type, not just <c>int</c> (finding C).</b> This test was first written filtering on
    /// <c>field.FieldType == typeof(int)</c> — every <c>[GT]</c> in this catalogue happens to be an
    /// <c>int</c> today, so the filter was invisible on the catalogue it was written against, but it
    /// reproduced exactly the silent-breach class the test exists to close: a future <c>[GT]</c> read
    /// through <c>Config.GetBool</c>/<c>GetFloat</c>/<c>GetString</c> (the #41 FR-MD-027 shape) is
    /// <c>public static readonly</c> but not <c>int</c>, so it would have been silently dropped from the
    /// reflected set and this test would have kept reporting green at four while a fifth, unguarded,
    /// differently-typed constant shipped. The filter now sees every type; a non-<c>int</c> field found
    /// this way forces an explicit decision — add it to the guarded four, or exclude it here with a
    /// stated reason — rather than being skipped silently.
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
        public void EveryPublicStaticReadonlyConstant_IsOneOfTheGuardedFour()
        {
            FieldInfo[] fields = typeof(DisciplineConstants).GetFields(
                BindingFlags.Public | BindingFlags.Static);

            var settable = new System.Collections.Generic.List<string>();
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                // `public static readonly`, of ANY type. A `const` is IsLiteral and cannot be
                // config-settable at all (it is baked into every reading assembly at compile time), so
                // the [FIXED]/[CROSS] ordinals and the save magic are correctly out of scope. The type
                // filter that used to sit here (`field.FieldType != typeof(int)`) was the gap this test
                // was written to close and instead reproduced: a future [GT] read through
                // Config.GetBool/GetFloat/GetString (the #41 FR-MD-027 shape) is `public static
                // readonly` but not `int`, so it would have been silently excluded from `settable` and
                // this test would have kept passing at four while a fifth, unguarded, differently-typed
                // constant shipped. Widening to every type forces an explicit decision instead: a new
                // field of ANY type now fails this assertion until it is classified (added to the
                // guarded four, or excluded here with a stated reason).
                if (field.IsLiteral || !field.IsInitOnly)
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
                "DisciplineConstants' config-settable set (public static readonly, any type) has "
                + "changed. A new guarded [GT] must be added to DisciplineRules' site guard, BOTH "
                + "RequireCommittableConfig forms, CommitWithExplicitConfig, #44 §2.3 F6's guard list, "
                + "and this test's GuardedGameplayTunedConstants list.");
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
// | 1.1     | 2026-08-16, latest | — | Finding C. Widened the field filter from `public static       |
// |         |            |        | readonly int` to `public static readonly` of any type — the       |
// |         |            |        | int-only filter was the single failure mode this test exists to   |
// |         |            |        | catch, reproduced inside the test meant to catch it: a future     |
// |         |            |        | [GT] read through Config.GetBool/GetFloat/GetString would have    |
// |         |            |        | been silently excluded from the reflected set. Test renamed       |
// |         |            |        | EveryConfigSettableIntConstant_IsOneOfTheGuardedFour ->            |
// |         |            |        | EveryPublicStaticReadonlyConstant_IsOneOfTheGuardedFour, since it  |
// |         |            |        | no longer names or implies an int-only scope. Behaviour-neutral   |
// |         |            |        | on today's catalogue (no non-int public static readonly field     |
// |         |            |        | exists yet), so the reflected set is unchanged at four.            |
#endregion
