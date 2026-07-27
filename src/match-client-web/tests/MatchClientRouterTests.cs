// File:     src/match-client-web/tests/MatchClientRouterTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6), interactive Unity client §6.4, UI / Client Framework
//           #38 §3.3, Match Analytics #37, Code Standards #20
// Purpose:  Locks the routing table and — the part that actually matters — the privilege split:
//           reading cannot change a match, playback cannot change tick CONTENT, and an intent
//           reaches the engine only through the tick-stamped queue.

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientWeb.Tests
{
    /// <summary>Router / privilege-split tests (see file header).</summary>
    [TestFixture]
    public sealed class MatchClientRouterTests
    {
        private const ulong Seed = 0x00B6C11E27FEEDUL;

        private MatchClientHost   _host;
        private MatchClientRouter _router;

        [SetUp]
        public void SetUp()
        {
            // Never started: the router must be fully exercisable against a match that has not
            // ticked, which is also the state the page first loads in.
            _host   = new MatchClientHost(MatchSetup.NeutralDemo(Seed));
            _router = new MatchClientRouter(_host);
        }

        [TearDown]
        public void TearDown() => _host?.Stop();

        // ── Routing table ───────────────────────────────────────────────────────────────────────

        [Test]
        public void Root_ServesTheSelfContainedPage()
        {
            MatchClientResponse r = _router.Route("GET", "/");

            Assert.AreEqual(200, r.Status);
            StringAssert.Contains("text/html", r.ContentType);
            StringAssert.Contains("<canvas", r.Body);
            StringAssert.DoesNotContain("http://", r.Body, "The page must carry no external asset.");
        }

        [Test]
        public void UnknownPath_Is404_AndNonGet_Is405()
        {
            Assert.AreEqual(404, _router.Route("GET", "/nope").Status);
            Assert.AreEqual(405, _router.Route("POST", "/intent?kind=SetTeamTactic&team=0").Status);
        }

        [Test]
        public void NonGet_IsRefusedBeforeAnyIntentIsParsed()
        {
            // A POST carrying a valid intent must change nothing — the refusal has to precede the
            // dispatch, not follow it.
            _router.Route("POST", "/intent?kind=SetTeamTactic&team=0&mentality=VeryAttacking");

            Assert.AreEqual(0, _host.Session.Commands.Count);
        }

        [Test]
        public void Frame_BeforeTheFirstTick_ReportsNotReady()
        {
            MatchClientResponse r = _router.Route("GET", "/frame");

            Assert.AreEqual(200, r.Status);
            StringAssert.Contains("\"ready\":false", r.Body,
                "A View must be able to tell 'nothing yet' from 'kickoff, everything at the origin'.");
        }

        [Test]
        public void Report_IsServedBeforeAnyTick_AndIsHealthy()
        {
            MatchClientResponse r = _router.Route("GET", "/report");

            Assert.AreEqual(200, r.Status);
            StringAssert.Contains("\"observedTicks\":0", r.Body);
            StringAssert.Contains("\"healthy\":true", r.Body);
            StringAssert.Contains("\"xgAvailable\":false", r.Body,
                "xG is producer-gated (#37 F4) and the flag must travel so the page shows a dash.");
        }

        // ── The privilege split ─────────────────────────────────────────────────────────────────

        [Test]
        public void Frame_DoesNotEnqueueAnything()
        {
            _router.Route("GET", "/frame");
            _router.Route("GET", "/report");

            Assert.AreEqual(0, _host.Session.Commands.Count, "Reading is not a command.");
        }

        [Test]
        public void Playback_DoesNotEnqueueAnything()
        {
            Assert.AreEqual(200, _router.Route("GET", "/playback?action=pause").Status);
            Assert.AreEqual(200, _router.Route("GET", "/playback?action=resume").Status);
            Assert.AreEqual(200, _router.Route("GET", "/playback?action=speed&value=3").Status);

            Assert.AreEqual(
                0, _host.Session.Commands.Count,
                "Pause and speed change WHEN ticks happen, never what is in them — they must never " +
                "reach the queue, or they would enter the tick-stamped replay record (§6.4).");
        }

        [Test]
        public void Intent_EnqueuesRatherThanTouchingTheEngine()
        {
            MatchClientResponse r =
                _router.Route("GET", "/intent?kind=SetTeamTactic&team=1&mentality=VeryAttacking");

            Assert.AreEqual(200, r.Status);
            Assert.AreEqual(
                1, _host.Session.Commands.Count,
                "The intent must be queued, so it applies on a tick boundary and lands in the log.");
            Assert.AreEqual(
                0, _host.Session.Driver.Log.Count,
                "…and must NOT have been applied yet — nothing has ticked.");
        }

        [Test]
        public void Intent_AppliesOnTheNextServicedTick_AndIsRecorded()
        {
            _router.Route("GET", "/intent?kind=SetTeamTactic&team=1&mentality=VeryAttacking");
            _host.Session.ServiceOnce();

            Assert.AreEqual(0, _host.Session.Commands.Count, "Drained.");
            Assert.AreEqual(1, _host.Session.Driver.Log.Count, "And recorded in the replay log.");
        }

        // ── Fail-loud parsing ───────────────────────────────────────────────────────────────────

        [Test]
        public void Intent_UnknownKind_Is400()
        {
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=Nonsense&team=0").Status);
            Assert.AreEqual(400, _router.Route("GET", "/intent?team=0").Status, "kind is required.");
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=None&team=0").Status,
                "The zero-value sentinel is not a real intent (#38 F3).");
        }

        [Test]
        public void Intent_BadTeam_Is400()
        {
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=SetTeamTactic&team=2").Status);
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=SetTeamTactic&team=-1").Status);
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=SetTeamTactic&team=x").Status);
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=SetTeamTactic").Status);
        }

        [Test]
        public void Intent_UndefinedEnumValue_IsRefusedNotAccepted()
        {
            // Enum.TryParse accepts "9" for an enum with five members. Left unguarded that value
            // reaches a [GT] lookup table indexed by ordinal and runs off the end of it.
            MatchClientResponse r =
                _router.Route("GET", "/intent?kind=SetTeamTactic&team=0&mentality=9");

            Assert.AreEqual(400, r.Status);
            Assert.AreEqual(0, _host.Session.Commands.Count);
        }

        [Test]
        public void Intent_OmittedDial_KeepsTheBalancedIdentity_RatherThanGuessing()
        {
            _router.Route("GET", "/intent?kind=SetTeamTactic&team=0&mentality=Attacking");
            _host.Session.ServiceOnce();

            Assert.AreEqual(1, _host.Session.Driver.Log.Count);
            TeamTactic applied = _host.Session.Driver.Log[0].Command.NewTeamTactic;

            Assert.AreEqual(Mentality.Attacking, applied.Mentality, "The named dial is set…");
            Assert.AreEqual(TeamTactic.Balanced.Tempo, applied.Tempo, "…and every omitted one is the identity.");
            Assert.AreEqual(TeamTactic.Balanced.Pressing, applied.Pressing);
            Assert.AreEqual(TeamTactic.Balanced.Passing, applied.Passing);
        }

        [Test]
        public void Intent_SetPlayerTactic_Is501_NotAPartialGuess()
        {
            // The seam exists; a browser surface for eleven per-agent instructions does not. Applying
            // defaults for the ten dials the manager never chose would be worse than refusing.
            MatchClientResponse r = _router.Route("GET", "/intent?kind=SetPlayerTactic&team=0");

            Assert.AreEqual(501, r.Status);
            Assert.AreEqual(0, _host.Session.Commands.Count);
        }

        [Test]
        public void Substitution_Queues_WithItsSlots()
        {
            MatchClientResponse r =
                _router.Route("GET", "/intent?kind=Substitute&team=0&outSlot=4&bench=1&reason=Injury");

            Assert.AreEqual(200, r.Status);
            Assert.AreEqual(1, _host.Session.Commands.Count);
        }

        [Test]
        public void Substitution_MissingSlots_Is400()
        {
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=Substitute&team=0&bench=1").Status);
            Assert.AreEqual(400, _router.Route("GET", "/intent?kind=Substitute&team=0&outSlot=4").Status);
        }

        [Test]
        public void Playback_BadAction_Or_BadSpeed_Is400()
        {
            Assert.AreEqual(400, _router.Route("GET", "/playback").Status);
            Assert.AreEqual(400, _router.Route("GET", "/playback?action=explode").Status);
            Assert.AreEqual(400, _router.Route("GET", "/playback?action=speed").Status);
            Assert.AreEqual(400, _router.Route("GET", "/playback?action=speed&value=fast").Status);
            Assert.AreEqual(400, _router.Route("GET", "/playback?action=speed&value=9999").Status,
                "Out-of-range speed is the streamer's own guard, surfaced as a 400 rather than a 500.");
        }

        [Test]
        public void EmptyPath_Is400()
        {
            Assert.AreEqual(400, _router.Route("GET", string.Empty).Status);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): routing table, the read / playback /    |
// |         |            |        | intent privilege split asserted against the command queue, and |
// |         |            |        | fail-loud parsing incl. the undefined-enum-ordinal guard.      |
#endregion
