// File:     src/match-client-web/tests/MatchClientServerTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6), Code Standards #20
// Purpose:  Drives the transport over real loopback sockets — the part the router tests cannot
//           reach: binding, framing, the request-line bound, and the post-Stop refusal that stops a
//           straggling /intent from changing a match after the server reports stopped.

using System;
using System.Net.Sockets;
using System.Text;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;

namespace TacticalDirector.MatchClientWeb.Tests
{
    /// <summary>Loopback transport tests (see file header).</summary>
    [TestFixture]
    public sealed class MatchClientServerTests
    {
        private const ulong Seed = 0x00577E5D1234UL;

        private MatchClientHost   _host;
        private MatchClientServer _server;

        [SetUp]
        public void SetUp()
        {
            _host   = new MatchClientHost(MatchSetup.NeutralDemo(Seed));
            _server = new MatchClientServer(new MatchClientRouter(_host), port: 0);
            _server.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Stop();
            _host?.Stop();
        }

        /// <summary>Issues one raw request line and returns the whole response.</summary>
        private string Request(string requestLine)
        {
            using (var client = new TcpClient("127.0.0.1", _server.Port))
            using (NetworkStream stream = client.GetStream())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(requestLine + "\r\n\r\n");
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                var sb = new StringBuilder();
                var buffer = new byte[4096];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                }
                return sb.ToString();
            }
        }

        [Test]
        public void BindsAnEphemeralLoopbackPort()
        {
            Assert.Greater(_server.Port, 0);
        }

        [Test]
        public void ServesThePage_WithAWellFormedFramedResponse()
        {
            string response = Request("GET / HTTP/1.1");

            StringAssert.StartsWith("HTTP/1.1 200 OK", response);
            StringAssert.Contains("Content-Type: text/html; charset=utf-8", response);
            StringAssert.Contains("Content-Length: ", response);
            StringAssert.Contains("<canvas", response);
        }

        [Test]
        public void ServesFrameAndReportAsJson()
        {
            StringAssert.Contains("application/json", Request("GET /frame HTTP/1.1"));
            StringAssert.Contains("\"observedTicks\"", Request("GET /report HTTP/1.1"));
        }

        [Test]
        public void UnknownPathIs404_AndAMalformedRequestLineIs400()
        {
            StringAssert.StartsWith("HTTP/1.1 404", Request("GET /missing HTTP/1.1"));
            StringAssert.StartsWith("HTTP/1.1 400", Request("GARBAGE"));
        }

        [Test]
        public void IntentTravelsThroughTheTransport_AndReachesTheQueue()
        {
            string response = Request("GET /intent?kind=SetTeamTactic&team=0&mentality=Attacking HTTP/1.1");

            StringAssert.StartsWith("HTTP/1.1 200", response);
            Assert.AreEqual(1, _host.Session.Commands.Count);
        }

        [Test]
        public void AfterStop_TheServerRefusesRatherThanActing()
        {
            // A straggling /intent must not be able to change a match after Stop() has returned. The
            // socket may simply be closed by then, which is also a refusal — what must NOT happen is
            // a 200 and a queued command.
            _server.Stop();

            int before = _host.Session.Commands.Count;
            try
            {
                string response = Request("GET /intent?kind=SetTeamTactic&team=0&mentality=Attacking HTTP/1.1");
                StringAssert.DoesNotStartWith("HTTP/1.1 200", response);
            }
            catch (Exception)
            {
                // Connection refused — the listener is closed. Also a refusal.
            }

            Assert.AreEqual(before, _host.Session.Commands.Count, "Nothing was queued after Stop().");
        }

        [Test]
        public void AnOversizedRequestLineIsDroppedRatherThanBuffered()
        {
            string huge = "GET /" + new string('a', MatchClientWebConstants.MAX_HTTP_REQUEST_LINE_BYTES + 64);

            // The guard drops the connection; the caller sees an empty (or truncated) response and,
            // critically, the server is still serving afterwards.
            Assert.DoesNotThrow(() => Request(huge));
            StringAssert.StartsWith("HTTP/1.1 200 OK", Request("GET /frame HTTP/1.1"));
        }

        [Test]
        public void StopIsIdempotent_AndStartAfterStopRebinds()
        {
            _server.Stop();
            Assert.DoesNotThrow(() => _server.Stop());

            _server.Start();
            StringAssert.StartsWith("HTTP/1.1 200 OK", Request("GET /frame HTTP/1.1"));
        }

        [Test]
        public void ConstructorGuardsFailLoud()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchClientServer(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MatchClientServer(new MatchClientRouter(_host), 70_000));
            Assert.Throws<ArgumentNullException>(() => new MatchClientRouter(null));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): real-loopback framing, routing, the     |
// |         |            |        | request-line bound, post-Stop refusal and lifecycle idempotence|
#endregion
