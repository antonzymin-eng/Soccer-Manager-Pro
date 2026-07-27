// File:     src/match-client-web/MatchClientResponse.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6), Code Standards #20
// Purpose:  The value the router returns and the transport writes — the seam that keeps every
//           routing decision testable without opening a socket.

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// One HTTP response as data: status, content type, body.
    ///
    /// <para>Exists so the interesting half of the server — which path does what, what is refused,
    /// what a bad parameter produces — is a pure function under test, and the socket code is left
    /// with nothing to decide. The spectator viewer's server had to be tested through real loopback
    /// sockets because routing and transport were one method; this splits them.</para>
    /// </summary>
    public readonly struct MatchClientResponse
    {
        /// <summary>HTTP status code.</summary>
        public readonly int Status;

        /// <summary>Full <c>Content-Type</c> header value.</summary>
        public readonly string ContentType;

        /// <summary>Response body.</summary>
        public readonly string Body;

        /// <summary>Constructs a response.</summary>
        public MatchClientResponse(int status, string contentType, string body)
        {
            Status      = status;
            ContentType = contentType;
            Body        = body ?? string.Empty;
        }

        /// <summary>A UTF-8 JSON 200.</summary>
        public static MatchClientResponse Json(string body) =>
            new MatchClientResponse(200, "application/json; charset=utf-8", body);

        /// <summary>A UTF-8 HTML 200.</summary>
        public static MatchClientResponse Html(string body) =>
            new MatchClientResponse(200, "text/html; charset=utf-8", body);

        /// <summary>A UTF-8 plain-text response with the given status.</summary>
        public static MatchClientResponse Text(int status, string body) =>
            new MatchClientResponse(status, "text/plain; charset=utf-8", body);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): the router/transport seam value type.   |
#endregion
