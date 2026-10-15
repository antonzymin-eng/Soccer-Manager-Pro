// File:     src/match-viewer/HttpGracefulClose.cs
// Created:  2026-10-14
// Modified: 2026-10-14
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md §7), Code Standards #20
// Purpose:  The one graceful-close routine shared by the two hand-rolled loopback HTTP servers,
//           LiveMatchServer and match-client-web's MatchClientServer. Both read only the request
//           line, so a connection always ends with request bytes still unread; closing it then makes
//           Windows send a TCP RST that discards the response before the client reads it. This
//           sends FIN first and drains what is left, within bounds. Networking tooling, not the 60 Hz
//           hot path: the zero-allocation / no-try-catch game-loop rules do not apply.

using System.Diagnostics;
using System.Net.Sockets;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Ends a served HTTP connection with FIN instead of RST. Call it after the response (or after
    /// deciding to drop the request) and before the socket is closed.
    /// </summary>
    internal static class HttpGracefulClose
    {
        /// <summary>[FIXED] Receive-buffer size for the drain, bytes. A chunk size only; the drain's bounds are MatchViewerConstants.MaxHttpCloseDrainBytes and HttpCloseDrainTimeoutMs.</summary>
        private const int DRAIN_BUFFER_BYTES = 4096;

        /// <summary>[FIXED] Unit conversion for <see cref="Socket.Poll(int, SelectMode)"/>, which takes microseconds.</summary>
        private const int MICROSECONDS_PER_MILLISECOND = 1000;

        /// <summary>
        /// Sends FIN, then reads and discards what the client still has in flight: browser headers,
        /// or the tail after a rejected or oversized request line. Closing a socket with unread
        /// receive data makes Windows send a TCP RST instead of a FIN, and the client's stack then
        /// discards a response it has not read yet. Linux hands the queued response over before
        /// reporting the reset, so only the Windows/Unity host showed this. The drain stops at EOF,
        /// at <see cref="MatchViewerConstants.MaxHttpCloseDrainBytes"/>, or after
        /// <see cref="MatchViewerConstants.HttpCloseDrainTimeoutMs"/> in total. A peer that hits
        /// either bound gets an abortive close, which is acceptable for a client that keeps sending
        /// after a <c>Connection: close</c> response. Never throws a <see cref="SocketException"/>:
        /// a peer that reset or vanished first leaves nothing to do but the caller's close.
        /// </summary>
        internal static void ShutdownAndDrain(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Send);

                // Wall-clock bound on a networking thread, not game logic: no determinism surface.
                var elapsed = Stopwatch.StartNew();
                var buffer = new byte[DRAIN_BUFFER_BYTES];
                int drained = 0;
                while (drained < MatchViewerConstants.MaxHttpCloseDrainBytes)
                {
                    long remainingMs = MatchViewerConstants.HttpCloseDrainTimeoutMs - elapsed.ElapsedMilliseconds;
                    if (remainingMs <= 0) { return; }
                    if (!socket.Poll((int)(remainingMs * MICROSECONDS_PER_MILLISECOND), SelectMode.SelectRead)) { return; }

                    int read = socket.Receive(buffer);
                    if (read <= 0) { return; } // the client closed its side: a clean FIN/FIN close
                    drained += read;
                }
            }
            catch (SocketException)
            {
                // The client reset or vanished first. The caller's close is still the right move.
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-14 | —      | Initial creation: Shutdown(Send) plus a drain bounded by bytes |
// |         |            |        | and total time, shared by LiveMatchServer and MatchClientServer |
// |         |            |        | so neither resets a response away on Windows.                  |
#endregion
