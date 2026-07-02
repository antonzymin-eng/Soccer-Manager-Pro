// File:     src/match-viewer/HtmlReplayExporter.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Match viewer (presentation tooling), Code Standards #20
// Purpose:  Renders a recorded MatchReplay as a single self-contained HTML file: a 2D canvas pitch
//           with ball + 22 agents, play/pause/scrub/speed controls, and the frame data embedded
//           inline. No external assets, no network — the file opens in any browser.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Exports a <see cref="MatchReplay"/> to a self-contained HTML replay viewer. Presentation
    /// tooling only — the output is NOT a determinism-pinned wire format (it embeds rounded
    /// display coordinates, never digest inputs; the same contract class as the Stage-0 tactic
    /// text grammar). All numeric formatting is InvariantCulture.
    /// </summary>
    public static class HtmlReplayExporter
    {
        /// <summary>Builds the full HTML document for <paramref name="replay"/>.</summary>
        public static string Export(MatchReplay replay)
        {
            if (replay == null) { throw new ArgumentNullException(nameof(replay)); }

            float ppm    = MatchViewerConstants.PixelsPerMetre;
            float margin = MatchViewerConstants.CanvasMarginPx;
            // The [GT] presentation values are config-resolved — a malformed config (0/negative/
            // NaN scale) would emit a silently broken document; gate per the fail-loud convention
            // (NaN-gate pattern: !(x > 0) also rejects NaN).
            if (!(ppm > 0f) || float.IsInfinity(ppm))
            {
                throw new InvalidOperationException("[GT] MatchViewerConstants.PixelsPerMetre must be finite and > 0.");
            }
            if (!(margin >= 0f) || float.IsInfinity(margin))
            {
                throw new InvalidOperationException("[GT] MatchViewerConstants.CanvasMarginPx must be finite and >= 0.");
            }
            int canvasW  = (int)Math.Ceiling(replay.PitchLengthM * ppm + 2f * margin);
            int canvasH  = (int)Math.Ceiling(replay.PitchWidthM * ppm + 2f * margin);

            var sb = new StringBuilder(1 << 20);
            sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<title>Tactical Director — Match Replay</title>\n<style>\n");
            sb.Append("body{background:#14181c;color:#e8e8e8;font:14px/1.4 system-ui,sans-serif;");
            sb.Append("display:flex;flex-direction:column;align-items:center;gap:10px;padding:16px}\n");
            sb.Append("#hud{display:flex;gap:24px;font-variant-numeric:tabular-nums}\n");
            sb.Append("#controls{display:flex;gap:12px;align-items:center;width:");
            AppendInt(sb, canvasW);
            sb.Append("px}\n#scrub{flex:1}\nbutton,select{font:inherit;padding:2px 12px}\n");
            sb.Append("canvas{border-radius:6px}\n</style>\n</head>\n<body>\n");

            sb.Append("<div id=\"hud\"><span>seed ");
            sb.Append(replay.MatchSeed.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span><span id=\"clock\">t 0</span><span id=\"poss\">ball loose</span></div>\n");
            sb.Append("<canvas id=\"pitch\" width=\"");
            AppendInt(sb, canvasW);
            sb.Append("\" height=\"");
            AppendInt(sb, canvasH);
            sb.Append("\"></canvas>\n");
            sb.Append("<div id=\"controls\"><button id=\"play\">Play</button>");
            sb.Append("<input id=\"scrub\" type=\"range\" min=\"0\" value=\"0\" step=\"1\">");
            sb.Append("<select id=\"speed\"><option value=\"0.5\">0.5&times;</option>");
            sb.Append("<option value=\"1\" selected>1&times;</option><option value=\"2\">2&times;</option>");
            sb.Append("<option value=\"4\">4&times;</option></select></div>\n");

            sb.Append("<script>\n\"use strict\";\n");
            AppendData(sb, replay);
            AppendPlayerScript(sb);
            sb.Append("</script>\n</body>\n</html>\n");
            return sb.ToString();
        }

        /// <summary>Builds the HTML document and writes it to <paramref name="path"/> (UTF-8).</summary>
        public static void ExportToFile(MatchReplay replay, string path)
        {
            if (string.IsNullOrEmpty(path)) { throw new ArgumentException("path must be non-empty.", nameof(path)); }
            File.WriteAllText(path, Export(replay), Encoding.UTF8);
        }

        // ── Embedded data ──────────────────────────────────────────────────────────────

        private static void AppendData(StringBuilder sb, MatchReplay replay)
        {
            sb.Append("const PPM=");
            AppendFloat(sb, MatchViewerConstants.PixelsPerMetre);
            sb.Append(",MARGIN=");
            AppendFloat(sb, MatchViewerConstants.CanvasMarginPx);
            sb.Append(",LEN=");
            AppendFloat(sb, replay.PitchLengthM);
            sb.Append(",WID=");
            AppendFloat(sb, replay.PitchWidthM);
            sb.Append(",TPS=");
            AppendInt(sb, replay.TicksPerSecond);
            sb.Append(",AGENT_R=");
            AppendFloat(sb, MatchViewerConstants.AgentRadiusPx);
            sb.Append(",BALL_R=");
            AppendFloat(sb, MatchViewerConstants.BallRadiusPx);
            sb.Append(",BALL_RZ=");
            AppendFloat(sb, MatchViewerConstants.BallRadiusPerMetreHeightPx);
            sb.Append(",CIRCLE_R=");
            AppendFloat(sb, MatchViewerConstants.CentreCircleRadiusM);
            sb.Append(",PEN_D=");
            AppendFloat(sb, MatchViewerConstants.PenaltyAreaDepthM);
            sb.Append(",PEN_W=");
            AppendFloat(sb, MatchViewerConstants.PenaltyAreaWidthM);
            sb.Append(",SIX_D=");
            AppendFloat(sb, MatchViewerConstants.GoalAreaDepthM);
            sb.Append(",SIX_W=");
            AppendFloat(sb, MatchViewerConstants.GoalAreaWidthM);
            sb.Append(",SPOT_D=");
            AppendFloat(sb, MatchViewerConstants.PenaltySpotDistanceM);
            sb.Append(",GOAL_W=");
            AppendFloat(sb, MatchViewerConstants.GoalWidthM);
            sb.Append(";\n");

            sb.Append("const TEAM=[");
            for (int i = 0; i < replay.AgentCount; i++)
            {
                if (i > 0) { sb.Append(','); }
                AppendInt(sb, replay.TeamId(i));
            }
            sb.Append("];\nconst GK=[");
            for (int i = 0; i < replay.AgentCount; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append(replay.IsGoalkeeper(i) ? '1' : '0');
            }
            sb.Append("];\n");

            // One flat array per frame: [tick, ballX, ballY, ballZ, possessingAgentId, x0, y0, x1, y1, ...].
            sb.Append("const FRAMES=[");
            for (int f = 0; f < replay.Frames.Count; f++)
            {
                ReplayFrame frame = replay.Frames[f];
                if (f > 0) { sb.Append(','); }
                sb.Append("\n[");
                sb.Append(frame.Tick.ToString(CultureInfo.InvariantCulture));
                sb.Append(',');
                AppendFloat(sb, frame.BallPosition.x);
                sb.Append(',');
                AppendFloat(sb, frame.BallPosition.y);
                sb.Append(',');
                AppendFloat(sb, frame.BallPosition.z);
                sb.Append(',');
                AppendInt(sb, frame.PossessingAgentId);
                for (int i = 0; i < frame.AgentPositions.Length; i++)
                {
                    sb.Append(',');
                    AppendFloat(sb, frame.AgentPositions[i].x);
                    sb.Append(',');
                    AppendFloat(sb, frame.AgentPositions[i].y);
                }
                sb.Append(']');
            }
            sb.Append("];\n");
        }

        // ── Player script (static — no per-replay content) ─────────────────────────────

        private static void AppendPlayerScript(StringBuilder sb)
        {
            sb.Append(@"
const canvas=document.getElementById('pitch'),ctx=canvas.getContext('2d');
const scrub=document.getElementById('scrub'),playBtn=document.getElementById('play');
const speedSel=document.getElementById('speed'),clockEl=document.getElementById('clock'),possEl=document.getElementById('poss');
scrub.max=FRAMES.length-1;
const px=x=>MARGIN+x*PPM,py=y=>MARGIN+(WID-y)*PPM; // corner origin, +Y drawn upward
// Per-team shirt numbers derived from the TEAM array (no contiguous-roster assumption).
const JERSEY=[];{const c={};for(let i=0;i<TEAM.length;i++){c[TEAM[i]]=(c[TEAM[i]]||0)+1;JERSEY[i]=c[TEAM[i]];}}
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
function drawFrame(f){
  drawPitch();
  const poss=f[4];
  for(let i=0;i<TEAM.length;i++){
    const x=px(f[5+2*i]),y=py(f[6+2*i]);
    if(i===poss){ctx.beginPath();ctx.arc(x,y,AGENT_R+4,0,2*Math.PI);ctx.strokeStyle='#ffd166';ctx.lineWidth=3;ctx.stroke();}
    ctx.beginPath();ctx.arc(x,y,AGENT_R,0,2*Math.PI);
    ctx.fillStyle=TEAM[i]===0?'#d1495b':'#3d7ea6';ctx.fill();
    ctx.strokeStyle=GK[i]?'#ffd166':'rgba(0,0,0,0.55)';ctx.lineWidth=GK[i]?3:1.5;ctx.stroke();
    ctx.fillStyle='#fff';ctx.font='9px system-ui';ctx.textAlign='center';ctx.textBaseline='middle';
    ctx.fillText(String(JERSEY[i]),x,y);
  }
  ctx.beginPath();ctx.arc(px(f[1]),py(f[2]),Math.max(1,BALL_R+f[3]*BALL_RZ),0,2*Math.PI);
  ctx.fillStyle='#fff';ctx.fill();ctx.strokeStyle='#111';ctx.lineWidth=1.5;ctx.stroke();
  // Round to tenths BEFORE splitting into minutes so 59.96 s shows 1:00.0, never 0:60.0.
  const tenths=Math.round(f[0]/TPS*10);
  clockEl.textContent='t '+f[0]+' ('+Math.floor(tenths/600)+':'+((tenths%600)/10).toFixed(1).padStart(4,'0')+')';
  possEl.textContent=poss<0?'ball loose':'possession: agent '+poss+' ('+(TEAM[poss]===0?'home':'away')+')';
}
let cursor=0,playing=false,last=null;
function render(){drawFrame(FRAMES[Math.floor(cursor)]);scrub.value=String(Math.floor(cursor));}
function step(now){
  if(!playing)return;
  if(last!==null){
    // Advance at the CURRENT frame's tick interval — frames are stride-spaced except the
    // forced final capture, whose interval may be shorter (recorder contract).
    const i=Math.floor(cursor);
    const dt=(i<FRAMES.length-1)?(FRAMES[i+1][0]-FRAMES[i][0]):1;
    cursor+=(now-last)/1000*(TPS/dt)*parseFloat(speedSel.value);
    if(cursor>=FRAMES.length-1){cursor=FRAMES.length-1;setPlaying(false);}
  }
  last=now;render();
  if(playing)requestAnimationFrame(step);
}
function setPlaying(p){
  playing=p;playBtn.textContent=p?'Pause':'Play';last=null;
  if(p){if(cursor>=FRAMES.length-1)cursor=0;requestAnimationFrame(step);}
}
playBtn.addEventListener('click',()=>setPlaying(!playing));
scrub.addEventListener('input',()=>{cursor=parseInt(scrub.value,10);render();});
// Space toggles playback — except when a control is focused: preventDefault there would
// hijack native activation (a focused select's dropdown; a focused button would double-toggle).
document.addEventListener('keydown',e=>{if(e.code==='Space'&&!['BUTTON','SELECT','INPUT'].includes(e.target.tagName)){e.preventDefault();setPlaying(!playing);}});
render();
");
        }

        // ── InvariantCulture number formatting ─────────────────────────────────────────

        private static void AppendInt(StringBuilder sb, int value)
        {
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendFloat(StringBuilder sb, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                // Fail loud: a non-finite coordinate reaching the exporter is upstream corruption,
                // not a display concern (the project NaN-gate pattern — never silently sanitise).
                throw new InvalidOperationException("Non-finite coordinate in replay data.");
            }
            sb.Append(value.ToString("0.##", CultureInfo.InvariantCulture));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: self-contained canvas replay (pitch         |
// |         |            |        | markings, teams/GK/possession/ball-height cues, play/pause/   |
// |         |            |        | scrub/speed, space toggles). Fail-loud non-finite gate;       |
// |         |            |        | InvariantCulture formatting throughout.                       |
// | 1.1     | 2026-07-02 | —      | AR-1 player-script fixes. L-2: playback advances at the       |
// |         |            |        | CURRENT frame's tick interval instead of assuming uniform     |
// |         |            |        | spacing (the forced final capture may be a short interval).   |
// |         |            |        | L-3: clock rounds to tenths BEFORE the minute split (59.96 s  |
// |         |            |        | showed 0:60.0); shirt numbers derived per-team from the TEAM  |
// |         |            |        | array (no contiguous-roster assumption).                      |
// | 1.2     | 2026-07-02 | —      | AR-2 L-2: ball marker radius clamped to >= 1 px — a corrupt-  |
// |         |            |        | but-finite negative ball z could drive the radius <= 0 and    |
// |         |            |        | canvas arc() throws IndexSizeError, killing every frame draw. |
// | 1.3     | 2026-07-02 | —      | AR-4 L: Export gates the config-resolved [GT] presentation    |
// |         |            |        | values (PixelsPerMetre > 0, CanvasMarginPx >= 0, NaN-gate     |
// |         |            |        | pattern) — a malformed config emitted a silently broken       |
// |         |            |        | document. Space-key toggle skips focused BUTTON/SELECT/INPUT  |
// |         |            |        | (preventDefault hijacked native activation: focused select's  |
// |         |            |        | dropdown; focused button double-toggled).                     |
#endregion
