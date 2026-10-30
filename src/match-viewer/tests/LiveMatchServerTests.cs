// File:     src/match-viewer/tests/LiveMatchServerTests.cs
// Created:  2026-07-15
// Modified: 2026-10-14
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md), Testing Strategy #19 (unit layer), Code Standards #20
// Purpose:  Contract tests for LiveMatchServer, driven over real loopback TCP sockets (bound to an
//           OS-chosen ephemeral port, port = 0): routing (/, /frame, /control), pause/speed control
//           reflected back through /frame, 404/405/400 error paths, the oversized-request-line
//           abuse guard (one bad connection must not affect the server), delivery of the whole
//           response when request headers go unread (the Windows RST case), and clean shutdown.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

using NUnit.Framework;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchViewer.Tests
{
    [TestFixture]
    public class LiveMatchServerTests
    {
        private const ulong Seed = 777UL;

        // Long enough never to trip on a loaded host; short enough that a server which stops closing
        // connections fails the test instead of hanging the run.
        private const int ClientReceiveTimeoutMs = 10000;

        // The Windows reset was deterministic on the first request; repeats guard against a lucky pass.
        private const int BrowserShapedRepeats = 10;

        private static readonly string[] MalformedRequestLines =
        {
            string.Empty, "GET", "GET /", " GET / HTTP/1.1", "GET  / HTTP/1.1",
            "GET /  HTTP/1.1", "GET / HTTP/1.1 ", "GET / HTTP/1.1 EXTRA",
            "GET / HTTP/1.0", "GET / HTTP/2", "GET / FTP/1.0", "GE(T / HTTP/1.1",
            "GE:T / HTTP/1.1", "GE@T / HTTP/1.1", "GE[T / HTTP/1.1",
            "GE]T / HTTP/1.1", "GE{T / HTTP/1.1", "GE}T / HTTP/1.1",
            "GET\t/ HTTP/1.1", "GET frame HTTP/1.1", "GET http://localhost/frame HTTP/1.1",
            "GET /bad\\path HTTP/1.1", "GET /frame#fragment HTTP/1.1",
            "GET /bad\u0001path HTTP/1.1", "GE\u0001T / HTTP/1.1",
        };

        private static LiveMatchServer StartServer(out LiveMatchStreamer streamer)
        {
            streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));
            var server = new LiveMatchServer(streamer, 0);
            server.Start();
            return server;
        }

        [Test]
        public void Start_BindsEphemeralPort()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                Assert.Greater(server.Port, 0);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Ctor_GuardsFailLoud()
        {
            Assert.Throws<ArgumentNullException>(() => new LiveMatchServer(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LiveMatchServer(new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed)), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LiveMatchServer(new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed)), 65536));
        }

        [Test]
        public void Get_Root_Returns200Html()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                (int status, string body) = SendRequest(server.Port, "GET / HTTP/1.1");
                Assert.AreEqual(200, status);
                StringAssert.StartsWith("<!DOCTYPE html>", body);
                StringAssert.Contains("canvas", body);
                StringAssert.Contains("fetch('/frame')", body);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Get_Frame_BeforeAnyTick_ReportsNoFrame()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                (int status, string body) = SendRequest(server.Port, "GET /frame HTTP/1.1");
                Assert.AreEqual(200, status);
                StringAssert.Contains("\"hasFrame\":false", body);
                StringAssert.Contains("\"roster\":[", body);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Get_Frame_AfterTick_ReportsFrame_WithCorrectRosterLength()
        {
            LiveMatchServer server = StartServer(out LiveMatchStreamer streamer);
            try
            {
                streamer.TickOnce();
                (int status, string body) = SendRequest(server.Port, "GET /frame HTTP/1.1");

                Assert.AreEqual(200, status);
                StringAssert.Contains("\"hasFrame\":true", body);
                StringAssert.Contains("\"tick\":", body);
                Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, CountOccurrences(body, "\"team\":"));
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Control_Pause_ThenFrameReflectsPausedState()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                (int status, string body) = SendRequest(server.Port, "GET /control?action=pause HTTP/1.1");
                Assert.AreEqual(200, status);
                StringAssert.Contains("\"paused\":true", body);

                (_, string frameBody) = SendRequest(server.Port, "GET /frame HTTP/1.1");
                StringAssert.Contains("\"paused\":true", frameBody);

                (_, string resumeBody) = SendRequest(server.Port, "GET /control?action=resume HTTP/1.1");
                StringAssert.Contains("\"paused\":false", resumeBody);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Control_Speed_AcceptsValid_RejectsInvalid()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                (int okStatus, string okBody) = SendRequest(server.Port, "GET /control?action=speed&value=2 HTTP/1.1");
                Assert.AreEqual(200, okStatus);
                StringAssert.Contains("\"speed\":2", okBody);

                Assert.AreEqual(400, SendRequest(server.Port, "GET /control?action=speed&value=-1 HTTP/1.1").status);
                Assert.AreEqual(400, SendRequest(server.Port, "GET /control?action=speed&value=nan HTTP/1.1").status);
                Assert.AreEqual(400, SendRequest(server.Port, "GET /control?action=speed HTTP/1.1").status);
                Assert.AreEqual(400, SendRequest(server.Port, "GET /control?action=bogus HTTP/1.1").status);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Get_UnknownPath_Returns404()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                Assert.AreEqual(404, SendRequest(server.Port, "GET /nope HTTP/1.1").status);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Post_Returns405()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                Assert.AreEqual(405, SendRequest(server.Port, "POST /frame HTTP/1.1").status);
            }
            finally { server.Stop(); }
        }

        /// <summary>Rejects 25 malformed request-line classes rather than routing partial parses.</summary>
        [TestCaseSource(nameof(MalformedRequestLines))]
        public void MalformedRequestLines_Return400(string requestLine)
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                Assert.AreEqual(400, SendRequest(server.Port, requestLine).status);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void NonAsciiOctet_IsRejectedBeforeDecoding_AndCannotRouteControl()
        {
            LiveMatchServer server = StartServer(out LiveMatchStreamer streamer);
            try
            {
                byte[] prefix = Encoding.ASCII.GetBytes("GET /control");
                byte[] suffix = Encoding.ASCII.GetBytes("action=pause HTTP/1.1\r\n");
                var request = new byte[prefix.Length + 1 + suffix.Length];
                Buffer.BlockCopy(prefix, 0, request, 0, prefix.Length);
                request[prefix.Length] = 0xFF; // Encoding.ASCII would otherwise replace this with '?'.
                Buffer.BlockCopy(suffix, 0, request, prefix.Length + 1, suffix.Length);

                Assert.AreEqual(400, SendRequest(server.Port, request).status);
                Assert.IsFalse(streamer.IsPaused);
            }
            finally { server.Stop(); }
        }

        /// <summary>
        /// A browser sends headers after the request line and the server never reads them. The
        /// response must still arrive whole: closing with those bytes unread made Windows reset the
        /// connection and discard the response (Linux delivered it, so only Unity caught this).
        /// </summary>
        [Test]
        public void RequestWithUnreadHeaders_StillDeliversTheWholeResponse()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                byte[] request = Encoding.ASCII.GetBytes(
                    "GET /frame HTTP/1.1\r\nHost: 127.0.0.1\r\nUser-Agent: LiveMatchServerTests\r\n" +
                    "Accept: */*\r\nConnection: keep-alive\r\n\r\n");

                for (int i = 0; i < BrowserShapedRepeats; i++)
                {
                    (int status, string body, int contentLength) = SendRequestWithLength(server.Port, request);

                    Assert.AreEqual(200, status, "request {0}", i);
                    Assert.AreEqual(contentLength, Encoding.UTF8.GetByteCount(body), "request {0}", i);
                    StringAssert.Contains("\"roster\":[", body);
                }
            }
            finally { server.Stop(); }
        }

        [Test]
        public void OversizedRequestLine_DropsThatConnection_ButServerKeepsServingOtherRequests()
        {
            LiveMatchServer server = StartServer(out _);
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, server.Port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Never send a line terminator — an oversized line the server must abandon
                        // rather than block forever waiting for CRLF.
                        byte[] junk = new byte[MatchViewerConstants.MaxHttpRequestLineBytes + 100];
                        for (int i = 0; i < junk.Length; i++) { junk[i] = (byte)'A'; }
                        stream.Write(junk, 0, junk.Length);
                        stream.Flush();

                        // The server drops the connection with no response at all. Either end of
                        // stream or a reset is a drop: the graceful-close drain is bounded, so a peer
                        // that overruns it may be reset by design. A hang is not a drop and fails.
                        Assert.AreEqual(0, CountBytesUntilDropped(client, stream));
                    }
                }

                // A single bad connection must not have taken down the accept loop.
                Assert.AreEqual(200, SendRequest(server.Port, "GET / HTTP/1.1").status);
            }
            finally { server.Stop(); }
        }

        [Test]
        public void Stop_ThenFreshConnectionIsRefused()
        {
            LiveMatchServer server = StartServer(out _);
            int port = server.Port;
            server.Stop();

            Assert.Throws<SocketException>(() =>
            {
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, port);
                }
            });
        }

        [Test]
        public void Stop_IsIdempotent()
        {
            LiveMatchServer server = StartServer(out _);
            server.Stop();
            Assert.DoesNotThrow(() => server.Stop());
        }

        // ── Test-only raw HTTP client ───────────────────────────────────────────────────

        private static (int status, string body) SendRequest(int port, string requestLine)
        {
            return SendRequest(port, Encoding.ASCII.GetBytes(requestLine + "\r\n"));
        }

        // Strict on purpose: a reset here is a failure. Every routed or rejected request must get
        // its whole response, so this helper is the graceful-close regression lock.
        private static (int status, string body) SendRequest(int port, byte[] requestBytes)
        {
            (int status, string body, _) = SendRequestWithLength(port, requestBytes);
            return (status, body);
        }

        private static (int status, string body, int contentLength) SendRequestWithLength(int port, byte[] requestBytes)
        {
            using (var client = new TcpClient())
            {
                client.ReceiveTimeout = ClientReceiveTimeoutMs;
                client.Connect(IPAddress.Loopback, port);
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(requestBytes, 0, requestBytes.Length);

                    string all = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
                    int lineEnd = all.IndexOf("\r\n", StringComparison.Ordinal);
                    int headerEnd = all.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    int status = int.Parse(all.Substring(0, lineEnd).Split(' ')[1]);
                    string headers = headerEnd >= 0 ? all.Substring(0, headerEnd) : all;
                    string body = headerEnd >= 0 ? all.Substring(headerEnd + 4) : string.Empty;
                    return (status, body, ParseContentLength(headers));
                }
            }
        }

        private static int ParseContentLength(string headers)
        {
            const string name = "\r\nContent-Length: ";
            int start = headers.IndexOf(name, StringComparison.Ordinal);
            if (start < 0) { return -1; }
            start += name.Length;
            int end = headers.IndexOf("\r\n", start, StringComparison.Ordinal);
            return int.Parse(end >= 0 ? headers.Substring(start, end - start) : headers.Substring(start));
        }

        /// <summary>
        /// Reads until the server drops the connection and returns how many bytes arrived first.
        /// End of stream and a connection reset both count as dropped. A receive timeout does not:
        /// it means the server kept the connection open, so it propagates and fails the test.
        /// </summary>
        private static int CountBytesUntilDropped(TcpClient client, NetworkStream stream)
        {
            client.ReceiveTimeout = ClientReceiveTimeoutMs;
            var buffer = new byte[1024];
            int total = 0;
            while (true)
            {
                int read;
                try
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException ex) when (ex.InnerException is SocketException se
                                             && se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    return total;
                }
                if (read <= 0) { return total; }
                total += read;
            }
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: real-loopback-socket routing tests (/,        |
// |         |            |        | /frame, /control), pause/speed reflected through /frame,        |
// |         |            |        | 400/404/405 error paths, oversized-request-line abuse guard     |
// |         |            |        | (one bad connection does not affect the server), clean/         |
// |         |            |        | idempotent shutdown.                                            |
// | 1.1     | 2026-09-09 | —      | Locks malformed whitespace, version, token, target, and raw    |
// |         |            |        | non-ASCII forms to HTTP 400 before routing.                    |
// | 1.2     | 2026-10-14 | —      | Windows RST fix. New RequestWithUnreadHeaders test: a browser- |
// |         |            |        | shaped request gets its full Content-Length body. The oversized |
// |         |            |        | test counts bytes until the drop and accepts EOF or reset; a    |
// |         |            |        | receive timeout fails it. SendRequest stays strict on resets.   |
#endregion
