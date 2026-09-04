// File:     src/match-viewer/LiveMatchServer.cs
// Created:  2026-07-15
// Modified: 2026-07-16 (AR-7 fix pass: viewer clock rounds seconds before the minute split (L-3); post-Stop connection threads refused via a volatile shutdown flag + 503 (L-4))
// Modified: 2026-07-27 (P1 AR-1 M-6: score reads follow LiveMatchFrame onto the Scoreline carrier; /frame JSON keys unchanged)
// Modified: 2026-08-03 (P4a: the roster "gk" flag comes from the live frame cue, not the boot-time cache)
// Modified: 2026-08-04 (P4a AR pass: the roster payload carries "shirt"; computeJersey deleted)
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md), Code Standards #20
// Purpose:  A minimal loopback-only HTTP server (hand-rolled over TcpListener — no package
//           dependency) that serves a live-updating HTML/canvas viewer page, a polled JSON frame
//           endpoint, and a playback-control endpoint (pause/resume/speed) backed by a
//           LiveMatchStreamer. Never references MatchEngine directly — see LiveMatchStreamer's
//           class doc for why that separation matters. Presentation/networking tooling, not the
//           60 Hz simulation hot path; the zero-allocation / no-try-catch game-loop rules do not
//           apply here (same carve-out MatchReplayRecorder / HtmlReplayExporter already use).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Loopback-only HTTP server over a <see cref="LiveMatchStreamer"/>. Routes: <c>GET /</c> (the
    /// viewer page), <c>GET /frame</c> (polled JSON snapshot), <c>GET /control?action=...</c>
    /// (playback control only — pause / resume / speed; never a gameplay-mutation channel).
    /// </summary>
    public sealed class LiveMatchServer
    {
        private readonly LiveMatchStreamer _streamer;
        private readonly int _requestedPort;

        private TcpListener _listener;
        private Thread _acceptThread;
        private readonly object _lifecycleLock = new object();
        private bool _running;

        // AR-7 L-4: Stop() joins only the ACCEPT thread — per-connection threads spawned before the
        // listener closed may still be mid-request. This volatile flag makes Route() refuse them
        // (503) so a straggling /control request cannot pause or re-speed the streamer after Stop()
        // has returned. Volatile (not the lifecycle lock) because Route() runs on connection
        // threads and must never block on, or race, the lifecycle transition.
        private volatile bool _shuttingDown;

        /// <summary>
        /// Binds the well-known <see cref="MatchViewerConstants.LiveServerDefaultPort"/>. Explicit
        /// overload rather than a default parameter — the [GT] port is config-resolved, not a
        /// compile-time constant, so it can no longer be a default parameter value (same
        /// constraint <c>MatchReplayRecorder</c>'s AR-2 M-3 fix already applied to this assembly).
        /// </summary>
        public LiveMatchServer(LiveMatchStreamer streamer) : this(streamer, MatchViewerConstants.LiveServerDefaultPort)
        {
        }

        /// <summary>
        /// <paramref name="port"/> = 0 binds an OS-chosen ephemeral port instead (read back via
        /// <see cref="Port"/> after <see cref="Start"/> — used by tests). <paramref name="streamer"/> must not be null.
        /// </summary>
        public LiveMatchServer(LiveMatchStreamer streamer, int port)
        {
            if (streamer == null) { throw new ArgumentNullException(nameof(streamer)); }
            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), port, "port must be in [0, 65535].");
            }

            _streamer      = streamer;
            _requestedPort = port;
        }

        /// <summary>The bound TCP port. Valid only after <see cref="Start"/> has returned.</summary>
        public int Port { get; private set; }

        /// <summary>
        /// Binds <c>127.0.0.1</c> (loopback only — never <see cref="IPAddress.Any"/>: this is a
        /// local spectator tool, not a service, and binding wide would expose match state and the
        /// control endpoint to the LAN) and starts the accept loop. Fail-loud on bind failure (a
        /// caller passing an in-use port needs to know, not have it silently swallowed).
        /// </summary>
        public void Start()
        {
            lock (_lifecycleLock)
            {
                if (_running) { return; }

                _listener = new TcpListener(IPAddress.Loopback, _requestedPort);
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

                // _running and _acceptThread must become visible together, atomically, under this
                // lock — otherwise a Stop() racing in right after this method releases the lock but
                // before _acceptThread is assigned would observe _running == true with a still-null
                // _acceptThread, stop the listener, and then this method would go on to start a
                // fresh accept thread against an already-stopped listener (Start() would silently
                // "succeed" while the server never actually served a request).
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "LiveMatchServer.Accept" };
                _shuttingDown = false; // a restarted server serves again (Start after Stop binds a fresh listener)
                _running = true;
                _acceptThread.Start();
            }
        }

        /// <summary>Stops the accept loop and releases the listener. Idempotent.</summary>
        public void Stop()
        {
            Thread threadToJoin;
            lock (_lifecycleLock)
            {
                if (!_running) { return; }
                _shuttingDown = true; // AR-7 L-4 — in-flight connection threads refuse from here on
                _running = false;
                threadToJoin = _acceptThread;
                _listener.Stop();
            }

            threadToJoin?.Join();
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
                    // Stop() calling listener.Stop() unblocks (or precedes) a pending
                    // AcceptTcpClient with SocketException/ObjectDisposedException/
                    // InvalidOperationException depending on exact timing — all of them mean
                    // "the listener is shutting down," never a per-connection condition (those are
                    // isolated inside HandleConnection). An unhandled exception on a background
                    // thread terminates the process, so this catch is deliberately broad: exiting
                    // the loop is always the right response here, whatever the exact exception type.
                    break;
                }

                var connectionThread = new Thread(() => HandleConnection(client))
                {
                    IsBackground = true,
                    Name = "LiveMatchServer.Connection",
                };
                connectionThread.Start();
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
                        WriteHttp(stream, 400, "text/plain; charset=utf-8", "Bad Request");
                        return;
                    }

                    Route(stream, method, pathAndQuery);
                }
            }
            catch (Exception)
            {
                // A single malformed/aborted request must not affect the accept loop or any other
                // connection — this is networking/presentation code, not the 60 Hz simulation inner
                // loop the project's try/catch ban targets (FR-CS-069 scope).
            }
        }

        /// <summary>
        /// Reads one HTTP request line, byte at a time, up to
        /// <see cref="MatchViewerConstants.MaxHttpRequestLineBytes"/> — an abuse/hang guard against
        /// a client that never sends a line terminator. Returns null on a closed connection or an
        /// oversized line (the caller drops the connection either way).
        /// </summary>
        private static string ReadRequestLine(Stream stream)
        {
            var bytes = new List<byte>(256);
            while (bytes.Count < MatchViewerConstants.MaxHttpRequestLineBytes)
            {
                int b = stream.ReadByte();
                if (b < 0) { return null; }
                if (b == '\n')
                {
                    int end = bytes.Count;
                    if (end > 0 && bytes[end - 1] == '\r') { end--; }
                    var lineBytes = new byte[end];
                    bytes.CopyTo(0, lineBytes, 0, end);
                    return Encoding.ASCII.GetString(lineBytes);
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

        private void Route(Stream stream, string method, string pathAndQuery)
        {
            // AR-7 L-4: a connection thread that outlived Stop() must not act — in particular a
            // straggling /control request must not mutate the streamer after the server reports stopped.
            if (_shuttingDown)
            {
                WriteHttp(stream, 503, "text/plain; charset=utf-8", "Service Unavailable: server is stopping");
                return;
            }

            if (method != "GET")
            {
                WriteHttp(stream, 405, "text/plain; charset=utf-8", "Method Not Allowed");
                return;
            }

            string path = pathAndQuery;
            string query = string.Empty;
            int queryIndex = pathAndQuery.IndexOf('?');
            if (queryIndex >= 0)
            {
                path  = pathAndQuery.Substring(0, queryIndex);
                query = pathAndQuery.Substring(queryIndex + 1);
            }

            switch (path)
            {
                case "/":
                case "/index.html":
                    WriteHttp(stream, 200, "text/html; charset=utf-8", BuildHtmlPage());
                    break;
                case "/frame":
                    WriteHttp(stream, 200, "application/json; charset=utf-8", BuildFrameJson());
                    break;
                case "/control":
                    HandleControl(stream, query);
                    break;
                default:
                    WriteHttp(stream, 404, "text/plain; charset=utf-8", "Not Found");
                    break;
            }
        }

        private void HandleControl(Stream stream, string query)
        {
            string action = null;
            string valueStr = null;
            foreach (string pair in query.Split('&'))
            {
                if (pair.Length == 0) { continue; }
                int eq = pair.IndexOf('=');
                string key = eq >= 0 ? pair.Substring(0, eq) : pair;
                string val = eq >= 0 ? pair.Substring(eq + 1) : string.Empty;
                if (key == "action") { action = val; }
                else if (key == "value") { valueStr = val; }
            }

            try
            {
                switch (action)
                {
                    case "pause":
                        _streamer.Pause();
                        break;
                    case "resume":
                        _streamer.Resume();
                        break;
                    case "speed":
                        if (valueStr == null ||
                            !float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed))
                        {
                            WriteHttp(stream, 400, "text/plain; charset=utf-8", "Bad Request: missing or unparsable value");
                            return;
                        }
                        _streamer.SetSpeedMultiplier(speed);
                        break;
                    default:
                        WriteHttp(stream, 400, "text/plain; charset=utf-8", "Bad Request: unknown action");
                        return;
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                WriteHttp(stream, 400, "text/plain; charset=utf-8", "Bad Request: " + ex.Message);
                return;
            }

            WriteHttp(stream, 200, "application/json; charset=utf-8", BuildFrameJson());
        }

        // ── JSON (hand-rolled — presentation-only wire format, NOT determinism-pinned) ─────

        private string BuildFrameJson()
        {
            var sb = new StringBuilder(1024);
            sb.Append('{');

            bool hasFrame = _streamer.TryGetLatestFrame(out LiveMatchFrame frame);
            AppendJsonBool(sb, "hasFrame", hasFrame);
            sb.Append(',');
            AppendJsonBool(sb, "paused", _streamer.IsPaused);
            sb.Append(',');
            AppendJsonFloat(sb, "speed", _streamer.SpeedMultiplier);
            sb.Append(',');

            sb.Append("\"roster\":[");
            for (int i = 0; i < _streamer.AgentCount; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append('{');
                AppendJsonInt(sb, "team", _streamer.TeamId(i));
                sb.Append(',');
                // Served rather than recomputed in the viewer script: RosterShirtNumbers is the one
                // implementation of the numbering rule, and it is explicitly a Stage-0 placeholder
                // that must change in exactly one place when players gain real numbers.
                AppendJsonInt(sb, "shirt", _streamer.ShirtNumber(i));
                sb.Append(',');
                // The streamer's cached flag is boot-time and goes stale when a keeper is
                // substituted, so prefer the frame's per-tick cue whenever there is a frame. Before
                // kickoff there is none, and the boot flag is then exactly right.
                bool isGoalkeeper = hasFrame && i < frame.AgentCues.Length
                    ? frame.AgentCues[i].IsGoalkeeper
                    : _streamer.IsGoalkeeper(i);
                AppendJsonBool(sb, "gk", isGoalkeeper);
                sb.Append('}');
            }
            sb.Append(']');

            if (hasFrame)
            {
                sb.Append(',');
                AppendFrameFields(sb, in frame);
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendFrameFields(StringBuilder sb, in LiveMatchFrame frame)
        {
            AppendJsonULong(sb, "tick", frame.Tick);
            sb.Append(',');
            AppendJsonInt(sb, "possession", frame.PossessingAgentId);
            sb.Append(',');
            AppendJsonInt(sb, "homeScore", frame.Score.Home);
            sb.Append(',');
            AppendJsonInt(sb, "awayScore", frame.Score.Away);
            sb.Append(',');
            AppendJsonBool(sb, "matchEnded", frame.MatchEnded);
            sb.Append(',');

            sb.Append("\"ball\":[");
            AppendJsonNumberLiteral(sb, frame.BallPosition.x);
            sb.Append(',');
            AppendJsonNumberLiteral(sb, frame.BallPosition.y);
            sb.Append(',');
            AppendJsonNumberLiteral(sb, frame.BallPosition.z);
            sb.Append(']');
            sb.Append(',');

            sb.Append("\"agents\":[");
            for (int i = 0; i < frame.AgentPositions.Length; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append('[');
                AppendJsonNumberLiteral(sb, frame.AgentPositions[i].x);
                sb.Append(',');
                AppendJsonNumberLiteral(sb, frame.AgentPositions[i].y);
                sb.Append(']');
            }
            sb.Append(']');
        }

        private static void AppendJsonBool(StringBuilder sb, string key, bool value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendJsonInt(StringBuilder sb, string key, int value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendJsonULong(StringBuilder sb, string key, ulong value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendJsonFloat(StringBuilder sb, string key, float value)
        {
            sb.Append('"').Append(key).Append("\":");
            AppendJsonNumberLiteral(sb, value);
        }

        private static void AppendJsonNumberLiteral(StringBuilder sb, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                // NaN/Infinity are not valid JSON tokens — a document containing one would be
                // handed to the browser un-parseable. On-pitch invariants elsewhere in the engine
                // mean this should never fire; fail loud rather than silently sanitise (the
                // project's NaN-gate convention — HtmlReplayExporter applies the same rule).
                throw new InvalidOperationException("Refusing to serialize a non-finite coordinate into the live-frame JSON payload.");
            }
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        // ── HTTP framing ────────────────────────────────────────────────────────────────

        private static void WriteHttp(Stream stream, int statusCode, string contentType, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            string header = string.Format(
                CultureInfo.InvariantCulture,
                "HTTP/1.1 {0} {1}\r\nContent-Type: {2}\r\nContent-Length: {3}\r\nConnection: close\r\n\r\n",
                statusCode, StatusText(statusCode), contentType, bodyBytes.Length);
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            stream.Flush();
        }

        private static string StatusText(int statusCode)
        {
            switch (statusCode)
            {
                case 200: return "OK";
                case 400: return "Bad Request";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 503: return "Service Unavailable";
                default:  return "Error";
            }
        }

        // ── Viewer page (static HTML/JS shell — content is fetched live, not embedded) ─────

        private string BuildHtmlPage()
        {
            var sb = new StringBuilder(1 << 14);
            sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<title>System XI — Live Match</title>\n<style>\n");
            sb.Append("body{background:#14181c;color:#e8e8e8;font:14px/1.4 system-ui,sans-serif;");
            sb.Append("display:flex;flex-direction:column;align-items:center;gap:10px;padding:16px}\n");
            sb.Append("#hud{display:flex;gap:24px;font-variant-numeric:tabular-nums}\n");
            sb.Append("#controls{display:flex;gap:12px;align-items:center}\n");
            sb.Append("button,select{font:inherit;padding:2px 12px}\ncanvas{border-radius:6px}\n</style>\n</head>\n<body>\n");
            sb.Append("<div id=\"hud\"><span id=\"clock\">t —</span><span id=\"score\">0 – 0</span>");
            sb.Append("<span id=\"poss\">ball loose</span><span id=\"status\">connecting…</span></div>\n");
            sb.Append("<canvas id=\"pitch\"></canvas>\n");
            sb.Append("<div id=\"controls\"><button id=\"pause\">Pause</button>");
            sb.Append("<select id=\"speed\"><option value=\"0.5\">0.5&times;</option>");
            sb.Append("<option value=\"1\" selected>1&times;</option><option value=\"2\">2&times;</option>");
            sb.Append("<option value=\"4\">4&times;</option></select></div>\n");
            sb.Append("<script>\n\"use strict\";\n");
            AppendConstants(sb);
            AppendViewerScript(sb);
            sb.Append("</script>\n</body>\n</html>\n");
            return sb.ToString();
        }

        private void AppendConstants(StringBuilder sb)
        {
            sb.Append("const PPM=");
            AppendGeometryFloat(sb, MatchViewerConstants.PixelsPerMetre);
            sb.Append(",MARGIN=");
            AppendGeometryFloat(sb, MatchViewerConstants.CanvasMarginPx);
            sb.Append(",LEN=");
            AppendGeometryFloat(sb, _streamer.PitchLengthM);
            sb.Append(",WID=");
            AppendGeometryFloat(sb, _streamer.PitchWidthM);
            sb.Append(",AGENT_R=");
            AppendGeometryFloat(sb, MatchViewerConstants.AgentRadiusPx);
            sb.Append(",BALL_R=");
            AppendGeometryFloat(sb, MatchViewerConstants.BallRadiusPx);
            sb.Append(",BALL_RZ=");
            AppendGeometryFloat(sb, MatchViewerConstants.BallRadiusPerMetreHeightPx);
            sb.Append(",CIRCLE_R=");
            AppendGeometryFloat(sb, MatchViewerConstants.CentreCircleRadiusM);
            sb.Append(",PEN_D=");
            AppendGeometryFloat(sb, MatchViewerConstants.PenaltyAreaDepthM);
            sb.Append(",PEN_W=");
            AppendGeometryFloat(sb, MatchViewerConstants.PenaltyAreaWidthM);
            sb.Append(",SIX_D=");
            AppendGeometryFloat(sb, MatchViewerConstants.GoalAreaDepthM);
            sb.Append(",SIX_W=");
            AppendGeometryFloat(sb, MatchViewerConstants.GoalAreaWidthM);
            sb.Append(",SPOT_D=");
            AppendGeometryFloat(sb, MatchViewerConstants.PenaltySpotDistanceM);
            sb.Append(",GOAL_W=");
            AppendGeometryFloat(sb, MatchViewerConstants.GoalWidthM);
            sb.Append(",POLL_MS=");
            sb.Append(MatchViewerConstants.LivePollIntervalMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(";\n");
        }

        private static void AppendGeometryFloat(StringBuilder sb, float value)
        {
            // These are all fixed/[GT] presentation constants read once at server construction,
            // never live-frame data — a non-finite catalogue value is a config-authoring error, not
            // a per-frame condition, but the same fail-loud gate applies for consistency.
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidOperationException("Non-finite MatchViewerConstants/LiveMatchStreamer pitch-geometry value.");
            }
            sb.Append(value.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private static void AppendViewerScript(StringBuilder sb)
        {
            sb.Append(@"
const canvas=document.getElementById('pitch');
canvas.width=Math.ceil(LEN*PPM+2*MARGIN);canvas.height=Math.ceil(WID*PPM+2*MARGIN);
const ctx=canvas.getContext('2d');
const pauseBtn=document.getElementById('pause'),speedSel=document.getElementById('speed');
const clockEl=document.getElementById('clock'),scoreEl=document.getElementById('score');
const possEl=document.getElementById('poss'),statusEl=document.getElementById('status');
const px=x=>MARGIN+x*PPM,py=y=>MARGIN+(WID-y)*PPM; // corner origin, +Y drawn upward
function drawPitch(){
  ctx.fillStyle='#1e7a3c';ctx.fillRect(0,0,canvas.width,canvas.height);
  ctx.strokeStyle='rgba(255,255,255,0.85)';ctx.lineWidth=2;ctx.fillStyle='rgba(255,255,255,0.85)';
  ctx.strokeRect(px(0),py(WID),LEN*PPM,WID*PPM);
  ctx.beginPath();ctx.moveTo(px(LEN/2),py(0));ctx.lineTo(px(LEN/2),py(WID));ctx.stroke();
  ctx.beginPath();ctx.arc(px(LEN/2),py(WID/2),CIRCLE_R*PPM,0,2*Math.PI);ctx.stroke();
  ctx.beginPath();ctx.arc(px(LEN/2),py(WID/2),3,0,2*Math.PI);ctx.fill();
  for(const gx of [0,LEN]){
    const s=gx===0?1:-1;
    ctx.strokeRect(px(gx),py((WID+PEN_W)/2),s*PEN_D*PPM,PEN_W*PPM);
    ctx.strokeRect(px(gx),py((WID+SIX_W)/2),s*SIX_D*PPM,SIX_W*PPM);
    ctx.beginPath();ctx.arc(px(gx+s*SPOT_D),py(WID/2),3,0,2*Math.PI);ctx.fill();
    ctx.save();ctx.strokeStyle='#fff';ctx.lineWidth=5;
    ctx.beginPath();ctx.moveTo(px(gx),py((WID-GOAL_W)/2));ctx.lineTo(px(gx),py((WID+GOAL_W)/2));ctx.stroke();
    ctx.restore();
  }
}
function drawFrame(data){
  drawPitch();
  if(!data.hasFrame){statusEl.textContent='waiting for kickoff…';return;}
  const poss=data.possession;
  for(let i=0;i<data.roster.length;i++){
    const a=data.agents[i],x=px(a[0]),y=py(a[1]),r=data.roster[i];
    if(i===poss){ctx.beginPath();ctx.arc(x,y,AGENT_R+4,0,2*Math.PI);ctx.strokeStyle='#ffd166';ctx.lineWidth=3;ctx.stroke();}
    ctx.beginPath();ctx.arc(x,y,AGENT_R,0,2*Math.PI);
    ctx.fillStyle=r.team===0?'#d1495b':'#3d7ea6';ctx.fill();
    ctx.strokeStyle=r.gk?'#ffd166':'rgba(0,0,0,0.55)';ctx.lineWidth=r.gk?3:1.5;ctx.stroke();
    ctx.fillStyle='#fff';ctx.font='9px system-ui';ctx.textAlign='center';ctx.textBaseline='middle';
    ctx.fillText(String(r.shirt),x,y);
  }
  const b=data.ball;
  ctx.beginPath();ctx.arc(px(b[0]),py(b[1]),Math.max(1,BALL_R+b[2]*BALL_RZ),0,2*Math.PI);
  ctx.fillStyle='#fff';ctx.fill();ctx.strokeStyle='#111';ctx.lineWidth=1.5;ctx.stroke();
  // Round to whole seconds BEFORE the minute split (AR-7 L-3 — the same class of clock bug
  // HtmlReplayExporter's AR-1 fixed: rounding the remainder after splitting shows 'm:60').
  const totalSecs=Math.round(data.tick/60),mins=Math.floor(totalSecs/60);
  clockEl.textContent='t '+data.tick+' ('+mins+':'+String(totalSecs%60).padStart(2,'0')+')';
  scoreEl.textContent=data.homeScore+' – '+data.awayScore;
  possEl.textContent=poss<0?'ball loose':'possession: agent '+poss+' ('+(data.roster[poss].team===0?'home':'away')+')';
  statusEl.textContent=data.matchEnded?'full time':(data.paused?'paused':'live');
  pauseBtn.textContent=data.paused?'Resume':'Pause';
  speedSel.value=String(data.speed);
}
async function poll(){
  try{
    const res=await fetch('/frame');
    const data=await res.json();
    drawFrame(data);
  }catch(e){statusEl.textContent='connection lost';}
  finally{setTimeout(poll,POLL_MS);}
}
pauseBtn.addEventListener('click',async()=>{
  const action=pauseBtn.textContent==='Pause'?'pause':'resume';
  const res=await fetch('/control?action='+action);
  drawFrame(await res.json());
});
speedSel.addEventListener('change',async()=>{
  const res=await fetch('/control?action=speed&value='+speedSel.value);
  if(res.ok)drawFrame(await res.json());
});
drawPitch();
poll();
");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: loopback-only hand-rolled HTTP server (no    |
// |         |            |        | package dependency) over TcpListener — GET / (viewer page),    |
// |         |            |        | GET /frame (polled JSON), GET /control (pause/resume/speed).   |
// |         |            |        | Bounded request-line read (abuse/hang guard); one short-lived  |
// |         |            |        | thread per connection; fail-loud on bind, silent-and-continue  |
// |         |            |        | on a single malformed request. Never holds a MatchEngine        |
// |         |            |        | reference — routes only through LiveMatchStreamer (pitch         |
// |         |            |        | dimensions come from LiveMatchStreamer.PitchLengthM/WidthM,      |
// |         |            |        | not MatchEngineConstants directly, to keep that invariant        |
// |         |            |        | literally true, not just true of the concurrency-sensitive       |
// |         |            |        | surface). Self-review before first landing caught and fixed two  |
// |         |            |        | real defects: (1) Start() set _running=true and only assigned    |
// |         |            |        | _acceptThread afterward, outside the lock — a Stop() racing in   |
// |         |            |        | that narrow window would join a null thread and stop the         |
// |         |            |        | listener while Start() was still about to spawn a fresh accept   |
// |         |            |        | thread against it, so the server could silently end up not       |
// |         |            |        | running despite Start() reporting success; _acceptThread is now  |
// |         |            |        | created, started, and _running flipped, all inside one lock.     |
// |         |            |        | (2) the default-port constructor tried to default-parameter off  |
// |         |            |        | MatchViewerConstants.LiveServerDefaultPort, a config-resolved     |
// |         |            |        | static readonly value — not a compile-time constant, so it        |
// |         |            |        | cannot be a C# default parameter (the same constraint            |
// |         |            |        | MatchReplayRecorder's AR-2 M-3 fix already hit in this            |
// |         |            |        | assembly); split into an explicit no-port overload.               |
// | 1.1     | 2026-07-16 | —      | AR-7 fix pass. L-3: the viewer clock reintroduced the exact      |
// |         |            |        | rounding bug HtmlReplayExporter's AR-1 fixed — (secs%60)         |
// |         |            |        | .toFixed(0) rendered "m:60" when the remainder rounded up; now   |
// |         |            |        | rounds to whole seconds BEFORE the minute split. L-4: Stop()     |
// |         |            |        | joins only the accept thread, so per-connection threads could    |
// |         |            |        | outlive it and a straggling /control request could still pause/  |
// |         |            |        | re-speed the streamer after the server reported stopped; a       |
// |         |            |        | volatile _shuttingDown flag (set in Stop() before the listener   |
// |         |            |        | closes, cleared on a fresh Start()) now makes Route() answer     |
// |         |            |        | 503 instead of acting.                                           |
// | 1.2     | 2026-07-27 | —      | P1 AR-1 M-6: the two score reads follow LiveMatchFrame's        |
// |         |            |        | HomeScore / AwayScore into frame.Score.Home / .Away. The /frame |
// |         |            |        | JSON keys are unchanged, so the browser viewer is untouched —   |
// |         |            |        | the P1 fields it does not yet render are B6's job.              |
// | 1.3     | 2026-08-03 | —      | P4a: the roster payload's "gk" flag reads the current frame's   |
// |         |            |        | LiveAgentCue.IsGoalkeeper when a frame exists, falling back to  |
// |         |            |        | the streamer's boot-time flag before kickoff. Fixes a keeper    |
// |         |            |        | ring drawn on the wrong player after a goalkeeper substitution. |
// |         |            |        | JSON keys and the viewer script are unchanged.                  |
// | 1.4     | 2026-08-04 | —      | P4a AR pass (M-6): the roster payload gains a "shirt" key and  |
// |         |            |        | the viewer script's computeJersey is DELETED — it was a second |
// |         |            |        | implementation of the shirt-numbering rule, living alongside   |
// |         |            |        | MatchRoster's C# copy while three documents claimed the rule   |
// |         |            |        | had MOVED into the latter. One rule (RosterShirtNumbers), one  |
// |         |            |        | implementation, served to both Views. Unlike the v1.3 gk fix   |
// |         |            |        | this IS a JSON key addition and a viewer-script change.        |
#endregion
