// File:     src/match-client-web/MatchClientServer.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6, option (b)), Code Standards #20
// Purpose:  Loopback-only HTTP transport for the browser match client. Owns sockets and threads and
//           nothing else — every routing decision belongs to MatchClientRouter, which is testable
//           without one. Presentation/networking tooling, not the 60 Hz hot path, so the
//           zero-allocation / no-try-catch game-loop rules do not apply (the same carve-out
//           LiveMatchServer already uses).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// Serves <see cref="MatchClientRouter"/> over loopback TCP.
    ///
    /// <para>Binds <c>127.0.0.1</c> only, never <c>IPAddress.Any</c>. That matters more here than for
    /// the spectator viewer: this client's <c>/intent</c> route can change a match in progress, and
    /// there is no authentication — binding wide would put the manager's controls on the LAN.</para>
    ///
    /// <para>Deliberately a separate server from <c>LiveMatchServer</c> rather than a mode of it:
    /// two surfaces with different privileges should not share a port, a lifecycle, or a routing
    /// table where one wrong branch grants the wrong one.</para>
    /// </summary>
    public sealed class MatchClientServer
    {
        private readonly MatchClientRouter _router;
        private readonly int _requestedPort;
        private readonly object _lifecycleLock = new object();

        private TcpListener _listener;
        private Thread _acceptThread;
        private bool _running;

        // Connection threads spawned before the listener closed may still be mid-request when Stop()
        // returns; this makes them refuse (503) so a straggling /intent cannot change a match after
        // the server reports stopped. Volatile rather than lock-guarded because it is read on
        // connection threads that must never block on the lifecycle transition.
        private volatile bool _shuttingDown;

        /// <summary>Binds <see cref="MatchClientWebConstants.DefaultPort"/>.</summary>
        public MatchClientServer(MatchClientRouter router)
            : this(router, MatchClientWebConstants.DefaultPort)
        {
        }

        /// <summary>
        /// <paramref name="port"/> = 0 binds an OS-chosen ephemeral port (read back from
        /// <see cref="Port"/> after <see cref="Start"/> — what tests use).
        /// </summary>
        public MatchClientServer(MatchClientRouter router, int port)
        {
            if (router == null) { throw new ArgumentNullException(nameof(router)); }
            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), port, "port must be in [0, 65535].");
            }

            _router        = router;
            _requestedPort = port;
        }

        /// <summary>The bound TCP port. Valid only after <see cref="Start"/> returns.</summary>
        public int Port { get; private set; }

        /// <summary>Binds loopback and starts accepting. Fail-loud on bind failure.</summary>
        public void Start()
        {
            lock (_lifecycleLock)
            {
                if (_running) { return; }

                _listener = new TcpListener(IPAddress.Loopback, _requestedPort);
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

                // _running and _acceptThread become visible together under this lock: a Stop() racing
                // in between would otherwise see _running true with a null thread, stop the listener,
                // and leave this method starting a fresh accept thread against a dead one.
                _acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "MatchClientServer.Accept",
                };
                _shuttingDown = false;
                _running = true;
                _acceptThread.Start();
            }
        }

        /// <summary>Stops accepting and releases the listener. Idempotent.</summary>
        public void Stop()
        {
            Thread toJoin;
            lock (_lifecycleLock)
            {
                if (!_running) { return; }
                _shuttingDown = true;
                _running = false;
                toJoin = _acceptThread;
                _listener.Stop();
            }

            toJoin?.Join();
        }

        private void AcceptLoop()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    // Stop() unblocks a pending accept with one of several exception types depending
                    // on timing; all of them mean the listener is closing, never a per-connection
                    // fault (those are isolated below). An unhandled exception on a background thread
                    // ends the process, so this catch is broad on purpose.
                    break;
                }

                var thread = new Thread(() => HandleConnection(client))
                {
                    IsBackground = true,
                    Name = "MatchClientServer.Connection",
                };
                thread.Start();
            }
        }

        private void HandleConnection(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    string requestLine = ReadRequestLine(stream);
                    if (requestLine == null) { return; }

                    if (!TryParseRequestLine(requestLine, out string method, out string pathAndQuery))
                    {
                        Write(stream, MatchClientResponse.Text(400, "Bad Request"));
                        return;
                    }

                    if (_shuttingDown)
                    {
                        Write(stream, MatchClientResponse.Text(503, "Service Unavailable: server is stopping"));
                        return;
                    }

                    Write(stream, _router.Route(method, pathAndQuery));
                }
            }
            catch (Exception)
            {
                // One malformed or aborted request must not take down the accept loop or another
                // connection. Networking code, not the simulation inner loop FR-CS-069 targets.
            }
        }

        /// <summary>
        /// Reads one HTTP request line byte-by-byte, bounded by
        /// <see cref="MatchClientWebConstants.MAX_HTTP_REQUEST_LINE_BYTES"/> — the hang guard against
        /// a client that never sends a terminator. Null on a closed connection or an oversized line.
        /// </summary>
        private static string ReadRequestLine(Stream stream)
        {
            var bytes = new List<byte>(256);
            while (bytes.Count < MatchClientWebConstants.MAX_HTTP_REQUEST_LINE_BYTES)
            {
                int b = stream.ReadByte();
                if (b < 0) { return null; }
                if (b == '\n')
                {
                    int end = bytes.Count;
                    if (end > 0 && bytes[end - 1] == '\r') { end--; }
                    var line = new byte[end];
                    bytes.CopyTo(0, line, 0, end);
                    return Encoding.ASCII.GetString(line);
                }
                bytes.Add((byte)b);
            }
            return null;
        }

        private static bool TryParseRequestLine(string requestLine, out string method, out string pathAndQuery)
        {
            method = null;
            pathAndQuery = null;
            string[] parts = requestLine.Split(' ');
            if (parts.Length < 2) { return false; }
            method = parts[0];
            pathAndQuery = parts[1];
            return true;
        }

        private static void Write(Stream stream, in MatchClientResponse response)
        {
            byte[] body = Encoding.UTF8.GetBytes(response.Body);
            var header = new StringBuilder(160);
            header.Append("HTTP/1.1 ").Append(response.Status.ToString(CultureInfo.InvariantCulture))
                  .Append(' ').Append(ReasonPhrase(response.Status)).Append("\r\n");
            header.Append("Content-Type: ").Append(response.ContentType).Append("\r\n");
            header.Append("Content-Length: ").Append(body.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            // No caching: every route is either live state or an action.
            header.Append("Cache-Control: no-store\r\n");
            header.Append("Connection: close\r\n\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private static string ReasonPhrase(int status)
        {
            switch (status)
            {
                case 200: return "OK";
                case 400: return "Bad Request";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 409: return "Conflict";
                case 501: return "Not Implemented";
                case 503: return "Service Unavailable";
                default:  return "Status";
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): loopback-only transport delegating every|
// |         |            |        | decision to MatchClientRouter, with the viewer's proven        |
// |         |            |        | lifecycle, request-line bound and post-Stop 503 refusal.       |
#endregion
