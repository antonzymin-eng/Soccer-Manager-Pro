// File:     src/match-client-web/MatchClientPage.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6, option (b)); interactive Unity client design note §5-P5
//           (the four screens) / §7 (pitch geometry, coordinate mapping, HUD); Code Standards #20
// Purpose:  Builds the self-contained PM-1 page: live pitch, HUD, tactical controls and a statistics
//           panel. Presentation only — the rendered document is NOT a determinism-pinned format.

using System.Globalization;
using System.Text;

using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// The browser client page: one self-contained HTML document with no external asset.
    ///
    /// <para><b>It renders the #38 view model, not the engine.</b> Every field it draws comes from
    /// <c>/frame</c>, which serializes <c>MatchFrameView</c> — so when the UGUI skin lands it binds
    /// the same projection and this page's layout choices carry no sim knowledge with them.</para>
    ///
    /// <para><b>Pitch geometry is read from the streamer,</b> which reports the engine's own
    /// <c>PITCH_LENGTH_M</c> / <c>PITCH_WIDTH_M</c> — a fourth independent copy of the pitch
    /// dimensions in a page template is exactly the drift this project keeps finding.</para>
    ///
    /// <para>The document is display output, never a wire format: coordinates are rounded for
    /// drawing and nothing here enters a digest.</para>
    /// </summary>
    public static class MatchClientPage
    {
        /// <summary>Builds the page for a match on <paramref name="streamer"/>'s pitch.</summary>
        public static string Build(LiveMatchStreamer streamer)
        {
            float pitchLength = streamer == null ? 105f : streamer.PitchLengthM;
            float pitchWidth  = streamer == null ? 68f  : streamer.PitchWidthM;

            float scale  = MatchClientWebConstants.PixelsPerMetre;
            int   margin = MatchClientWebConstants.CanvasMarginPx;
            int   width  = (int)(pitchLength * scale) + margin * 2;
            int   height = (int)(pitchWidth * scale) + margin * 2;

            var sb = new StringBuilder(16_384);
            sb.Append(@"<!DOCTYPE html><html lang=""en""><head><meta charset=""utf-8"">
<title>Tactical Director — Match</title>
<style>
 body{background:#14181c;color:#dfe6ec;font:13px/1.45 system-ui,sans-serif;margin:0;padding:16px}
 h1{font-size:15px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;margin:0 0 12px;color:#8fa3b0}
 #wrap{display:flex;gap:16px;flex-wrap:wrap;align-items:flex-start}
 canvas{background:#1d6b3a;border-radius:4px;display:block}
 .panel{background:#1b2126;border:1px solid #2a333a;border-radius:6px;padding:12px;min-width:260px}
 .panel h2{font-size:11px;letter-spacing:.08em;text-transform:uppercase;color:#7f929f;margin:0 0 8px}
 #hud{font-size:22px;font-variant-numeric:tabular-nums;margin-bottom:6px}
 #meta{color:#8fa3b0;margin-bottom:10px}
 #restart{color:#f0c674;min-height:18px;margin-bottom:10px}
 button,select{background:#252d34;color:#dfe6ec;border:1px solid #38444d;border-radius:4px;
  padding:4px 9px;font:inherit;cursor:pointer;margin:0 4px 6px 0}
 button:hover,select:hover{border-color:#4d5d69}
 label{display:block;margin:6px 0 2px;color:#8fa3b0;font-size:11px}
 table{border-collapse:collapse;width:100%;font-variant-numeric:tabular-nums}
 td,th{padding:2px 4px;text-align:right;border-bottom:1px solid #242c32}
 th:first-child,td:first-child{text-align:left;color:#8fa3b0;font-weight:400}
 .warn{color:#e06c75}
</style></head><body>
<h1>Tactical Director — Live Match</h1>
<div id=""wrap"">
 <div>");
            sb.Append("<canvas id=\"pitch\" width=\"").Append(width)
              .Append("\" height=\"").Append(height).Append("\"></canvas>");
            sb.Append(@"
  <div class=""panel"" style=""margin-top:12px"">
   <h2>Playback</h2>
   <button onclick=""playback('pause')"">Pause</button>
   <button onclick=""playback('resume')"">Resume</button>
   <span>Speed</span>
   <select onchange=""playback('speed', this.value)"">
    <option value=""1"">1&times;</option><option value=""3"">3&times;</option>
    <option value=""5"">5&times;</option><option value=""10"">10&times;</option>
   </select>
   <div style=""color:#6d7d89"">Playback changes when ticks happen, never what is in them &mdash;
    it is not a manager command and never reaches the match record.</div>
  </div>
 </div>
 <div class=""panel"">
  <h2>Match</h2>
  <div id=""hud"">&mdash;</div>
  <div id=""meta"">waiting for the first tick&hellip;</div>
  <div id=""restart""></div>
 </div>
 <div class=""panel"">
  <h2>Tactics</h2>
  <label>Team</label>
  <select id=""team""><option value=""0"">Home</option><option value=""1"">Away</option></select>
  <label>Mentality</label>
  <select id=""mentality"">
   <option>VeryDefensive</option><option>Defensive</option><option selected>Balanced</option>
   <option>Attacking</option><option>VeryAttacking</option>
  </select>
  <label>Pressing</label>
  <select id=""pressing"">
   <option>Low</option><option selected>Medium</option><option>High</option>
  </select>
  <label>Passing</label>
  <select id=""passing"">
   <option>Short</option><option selected>Mixed</option><option>Direct</option>
  </select>
  <button onclick=""applyTactic()"">Apply</button>
  <div id=""intentStatus"" style=""color:#6d7d89""></div>
 </div>
 <div class=""panel"">
  <h2>Statistics</h2>
  <div id=""statsWarn"" class=""warn""></div>
  <table id=""stats""><tbody></tbody></table>
 </div>
</div>
<script>
");
            sb.Append("const PITCH_L=").Append(Fmt(pitchLength)).Append(",PITCH_W=").Append(Fmt(pitchWidth))
              .Append(",SCALE=").Append(Fmt(scale)).Append(",MARGIN=").Append(margin).Append(";\n");
            sb.Append("const FRAME_MS=").Append(MatchClientWebConstants.FramePollIntervalMs)
              .Append(",REPORT_MS=").Append(MatchClientWebConstants.ReportPollIntervalMs)
              .Append(",RESTART_TICKS=").Append(MatchClientWebConstants.RestartCaptionTicks).Append(";\n");

            // Roster metadata is a boot constant, so it is baked in once rather than repeated in every
            // frame payload. Read from the streamer rather than inferred from the roster index: index
            // ordering is contiguous by construction today, but the spectator viewer's AR-1 L-3 finding
            // was precisely a page that assumed contiguous team blocks, and goalkeeper identity cannot
            // be derived from an index at all once a substitution has happened.
            AppendIntArray(sb, "TEAM", streamer, i => streamer.TeamId(i));
            AppendBoolArray(sb, "ISGK", streamer, i => streamer.IsGoalkeeper(i));
            sb.Append(@"
const cv=document.getElementById('pitch'),cx=cv.getContext('2d');
let latest=null;

const X=m=>MARGIN+m*SCALE, Y=m=>MARGIN+m*SCALE;

function pitch(){
 cx.clearRect(0,0,cv.width,cv.height);
 cx.fillStyle='#1d6b3a';cx.fillRect(0,0,cv.width,cv.height);
 cx.strokeStyle='rgba(255,255,255,.55)';cx.lineWidth=1.5;
 cx.strokeRect(X(0),Y(0),PITCH_L*SCALE,PITCH_W*SCALE);
 cx.beginPath();cx.moveTo(X(PITCH_L/2),Y(0));cx.lineTo(X(PITCH_L/2),Y(PITCH_W));cx.stroke();
 cx.beginPath();cx.arc(X(PITCH_L/2),Y(PITCH_W/2),9.15*SCALE,0,Math.PI*2);cx.stroke();
 for(const near of [true,false]){
  const x0=near?0:PITCH_L-16.5, y0=(PITCH_W-40.32)/2;
  cx.strokeRect(X(x0),Y(y0),16.5*SCALE,40.32*SCALE);
  const sx=near?0:PITCH_L-5.5, sy=(PITCH_W-18.32)/2;
  cx.strokeRect(X(sx),Y(sy),5.5*SCALE,18.32*SCALE);
 }
}

function draw(f){
 pitch();
 if(!f||!f.ready)return;
 f.agents.forEach((a,i)=>{
  const home=(TEAM[i]??(i<f.agents.length/2?0:1))===0;
  cx.beginPath();cx.arc(X(a.x),Y(a.y),ISGK[i]?6:5,0,Math.PI*2);
  cx.fillStyle=a.off?'#4a4a4a':(ISGK[i]?'#c8d64a':(home?'#5aa9e6':'#e6725a'));cx.fill();
  if(i===f.possessingAgentId){cx.strokeStyle='#ffe27a';cx.lineWidth=2;cx.stroke();}
  if(a.yellow>0&&!a.off){cx.strokeStyle='#f0c674';cx.lineWidth=2;cx.stroke();}
  if(a.sub){cx.fillStyle='#dfe6ec';cx.fillRect(X(a.x)+6,Y(a.y)-8,3,3);}
 });
 // Ball radius grows with height so a lofted ball reads as lofted on a flat canvas.
 const r=Math.max(2,3+f.ball[2]*1.1);
 cx.beginPath();cx.arc(X(f.ball[0]),Y(f.ball[1]),r,0,Math.PI*2);
 cx.fillStyle='#ffffff';cx.fill();
}

function clock(tick){
 // Round to tenths BEFORE splitting minutes, or 59.96 s renders as '0:60.0'.
 const t=Math.round(tick/60*10)/10;
 const m=Math.floor(t/60), s=(t-m*60).toFixed(1).padStart(4,'0');
 return m+':'+s;
}

async function pollFrame(){
 try{
  const f=await (await fetch('/frame')).json();
  latest=f;
  draw(f);
  if(f.ready){
   document.getElementById('hud').textContent=f.homeScore+' — '+f.awayScore;
   document.getElementById('meta').textContent=
    clock(f.tick)+'  ·  '+f.period+(f.matchEnded?'  ·  FULL TIME':'');
   const cap=document.getElementById('restart');
   if(f.lastRestart!=='None'&&f.tick-f.lastRestartTick<RESTART_TICKS){
    cap.textContent=f.lastRestart+' — '+(f.lastRestartTeam===0?'Home':f.lastRestartTeam===1?'Away':'');
   }else{cap.textContent='';}
  }
 }catch(e){/* a dropped poll is not worth a modal; the next one recovers */}
}

async function pollReport(){
 try{
  const r=await (await fetch('/report')).json();
  const warn=document.getElementById('statsWarn');
  warn.textContent=r.healthy?'':('statistics stopped: '+(r.fault||'unknown'));
  const rows=[['Possession','possession','%'],['Territory','territorial','%'],
   ['Goals','goals',''],['Fouls','fouls',''],['Yellow','yellow',''],['Red','red',''],
   ['Offside','offsides',''],['Corners','corners',''],['Throw-ins','throwIns',''],
   ['Goal kicks','goalKicks',''],['Subs','subs','']];
  let html='<tr><th>&nbsp;</th><th>Home</th><th>Away</th></tr>';
  for(const [label,key,unit] of rows){
   const h=r.home[key], a=r.away[key];
   const fmt=v=>unit==='%'?v.toFixed(1)+'%':v;
   html+='<tr><td>'+label+'</td><td>'+fmt(h)+'</td><td>'+fmt(a)+'</td></tr>';
  }
  // xG is producer-gated (#37 F4): show an em dash, never a 0.00 that reads as a measurement.
  const xg=v=>v.xgAvailable?v.xg.toFixed(2):'—';
  html+='<tr><td>xG</td><td>'+xg(r.home)+'</td><td>'+xg(r.away)+'</td></tr>';
  document.querySelector('#stats tbody').innerHTML=html;
 }catch(e){}
}

async function playback(action,value){
 const q=value===undefined?'':('&value='+encodeURIComponent(value));
 await fetch('/playback?action='+action+q);
}

async function applyTactic(){
 const g=id=>encodeURIComponent(document.getElementById(id).value);
 const url='/intent?kind=SetTeamTactic&team='+g('team')+'&mentality='+g('mentality')
  +'&pressing='+g('pressing')+'&passing='+g('passing');
 const res=await fetch(url);
 const el=document.getElementById('intentStatus');
 el.textContent=res.ok
  ? 'queued — applies at the next tick boundary'
  : ('refused: '+await res.text());
 el.className=res.ok?'':'warn';
}

pitch();
setInterval(pollFrame,FRAME_MS);
setInterval(pollReport,REPORT_MS);
pollFrame();pollReport();
</script></body></html>");
            return sb.ToString();
        }

        private static void AppendIntArray(
            StringBuilder sb, string name, LiveMatchStreamer streamer, System.Func<int, int> value)
        {
            sb.Append("const ").Append(name).Append("=[");
            int n = streamer == null ? 0 : streamer.AgentCount;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append(value(i).ToString(CultureInfo.InvariantCulture));
            }
            sb.Append("];\n");
        }

        private static void AppendBoolArray(
            StringBuilder sb, string name, LiveMatchStreamer streamer, System.Func<int, bool> value)
        {
            sb.Append("const ").Append(name).Append("=[");
            int n = streamer == null ? 0 : streamer.AgentCount;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append(value(i) ? "true" : "false");
            }
            sb.Append("];\n");
        }

        private static string Fmt(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): the self-contained PM-1 page — pitch,   |
// |         |            |        | HUD with the pre-split clock rounding, playback controls,      |
// |         |            |        | tactical input, and the #37 statistics panel with its F4 gate. |
#endregion
