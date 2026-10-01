# task033 差分（Opus 作成）

Assets/LoopRoom/Scripts/LoopModel.cs dcc375db920c8752ab07c401f45331cc9f46020fa6f8928478cc02f01120471d
Assets/LoopRoom/Editor/LoopModelChecks.cs 2b74356322497d5a37ed1d015253dafa148808dbd1050a07f7ac9c5119202dfc
Tests/LoopModel.Tests/Program.cs c1514fa0fe718a015e5d2249f6194446c9957fb12b068ab59926ea06b3da68ed
Assets/LoopRoom/Scripts/LoopDemo.cs 81560a15c2af8539199f28b763394d0b30098a8d1507bf98d1eaa6217543ba7e
Assets/LoopRoom/Scenes/LoopRoom.unity 2f3b7c99f86994ea085a501103888c0c7199fc672bcc65c29856af89e6accbb7

```diff
diff --git a/Assets/LoopRoom/Editor/LoopModelChecks.cs b/Assets/LoopRoom/Editor/LoopModelChecks.cs
index a46c745..bc9bda6 100644
--- a/Assets/LoopRoom/Editor/LoopModelChecks.cs
+++ b/Assets/LoopRoom/Editor/LoopModelChecks.cs
@@ -16,7 +16,7 @@ public static class LoopModelChecks
             Need(!m.ShieldRaised && !m.ShotResolved,"world reset");
         });
         Check(passed,"Shield grants a real escape window",()=>{
-            var m=new LoopModel();m.Start();m.Advance(2);m.RaiseShield(m.LoopId);m.Advance(4.5);
+            var m=new LoopModel();m.Start();m.Advance(2);m.RaiseShield(m.LoopId);m.CloseBlinds(m.LoopId);m.Advance(7.5);
             Need(m.ExitAvailable && m.TryExit(m.LoopId),"escape");
             Need(!m.Kill(m.LoopId,"late"),"no death after escape");
             m.Advance(8);Need(m.Phase==SessionPhase.Finished && m.Outcome==SessionPhase.Escaped,"ending");
@@ -24,7 +24,7 @@ public static class LoopModelChecks
         Check(passed,"No escape before unlocking or with a stale loop id",()=>{
             var m=new LoopModel();m.Start();Need(!m.TryExit(1),"locked");m.Advance(6.16);
             Need(!m.RaiseShield(1) && !m.Kill(1,"stale"),"stale ignored");
-            m.RaiseShield(2);m.Advance(6.5);Need(!m.TryExit(1) && m.TryExit(2),"correct loop only");
+            m.RaiseShield(2);m.CloseBlinds(2);m.Advance(9.5);Need(!m.TryExit(1) && m.TryExit(2),"correct loop only");
         });
         Check(passed,"Repeated death callbacks produce only one reset",()=>{
             var m=new LoopModel();m.Start();Need(m.Kill(1,"one"),"first accepted");
@@ -56,19 +56,20 @@ public static class LoopModelChecks
         });
         Check(passed,"Minimum interval boundaries are enforced",()=>{
             var boundary=new LoopRules{
-                firstShot=LoopRules.MinInterval,searchShot=LoopRules.MinInterval*2,
+                firstShot=LoopRules.MinInterval,windowShot=LoopRules.MinInterval*2,searchShot=LoopRules.MinInterval*3,
                 exitOpens=LoopRules.MinInterval,exitCloses=LoopRules.MinInterval*2,
                 blackout=LoopRules.MinInterval,playLimit=LoopRules.MinInterval,
                 endingLength=LoopRules.MinInterval
             };
             boundary.Validate();
             new LoopRules{
-                firstShot=6.0,searchShot=6.05,exitOpens=6.0,exitCloses=6.05
+                firstShot=6.0,windowShot=6.05,searchShot=6.1,exitOpens=6.0,exitCloses=6.1
             }.Validate();
             new LoopRules{exitOpens=6.5,exitCloses=6.55}.Validate();
             Action<LoopRules>[] makeTooSmall={
                 r=>r.firstShot=LoopRules.MinInterval/2,
-                r=>{r.firstShot=LoopRules.MinInterval;r.searchShot=LoopRules.MinInterval*1.5;
+                r=>{r.firstShot=LoopRules.MinInterval;r.windowShot=LoopRules.MinInterval*2;
+                    r.searchShot=LoopRules.MinInterval*2.5;
                     r.exitOpens=r.firstShot;r.exitCloses=r.searchShot;},
                 r=>{r.exitOpens=6.5;r.exitCloses=6.5+LoopRules.MinInterval/2;},
                 r=>r.blackout=LoopRules.MinInterval/2,
@@ -84,7 +85,7 @@ public static class LoopModelChecks
         });
         Check(passed,"Extremely small loop timings are rejected",()=>{
             var rules=new LoopRules{
-                firstShot=1e-200,searchShot=2e-200,exitOpens=1e-200,exitCloses=2e-200,
+                firstShot=1e-200,windowShot=1.5e-200,searchShot=2e-200,exitOpens=1e-200,exitCloses=2e-200,
                 blackout=1e-200,playLimit=1e-200,endingLength=1e-200
             };
             bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
@@ -92,7 +93,7 @@ public static class LoopModelChecks
         });
         Check(passed,"Minimum intervals remain bounded at MaxStep",()=>{
             var rules=new LoopRules{
-                firstShot=LoopRules.MinInterval,searchShot=LoopRules.MinInterval*2,
+                firstShot=LoopRules.MinInterval,windowShot=LoopRules.MinInterval*2,searchShot=LoopRules.MinInterval*3,
                 exitOpens=LoopRules.MinInterval,exitCloses=LoopRules.MinInterval*2,
                 blackout=LoopRules.MinInterval,playLimit=LoopRules.MinInterval,
                 endingLength=LoopRules.MinInterval
@@ -119,7 +120,7 @@ public static class LoopModelChecks
             Need(bad,"enabled deadline accepted over 180 seconds");
         });
         Check(passed,"Missing escape window allows the enemy to flank",()=>{
-            var m=new LoopModel();m.Start();m.RaiseShield(1);m.Advance(12);
+            var m=new LoopModel();m.Start();m.RaiseShield(1);m.CloseBlinds(1);m.Advance(12);
             Need(m.Phase==SessionPhase.Blackout && !m.TryExit(1),"flank before late input");
             Need(m.Records[m.Records.Count-1].kind=="flanked","cause");
         });
@@ -148,10 +149,10 @@ public static class LoopModelChecks
             Need(bad,"bad rules");bad=false;try{new LoopModel().Advance(double.NaN);}catch(ArgumentException){bad=true;}
             Need(bad,"bad delta");
         });
-        Check(passed,"All seven timings reject NaN and both infinities",()=>{
-            string[] names={"firstShot","searchShot","exitOpens","exitCloses","blackout","playLimit","endingLength"};
+        Check(passed,"All eight timings reject NaN and both infinities",()=>{
+            string[] names={"firstShot","windowShot","searchShot","exitOpens","exitCloses","blackout","playLimit","endingLength"};
             Action<LoopRules,double>[] setters={
-                (r,v)=>r.firstShot=v,(r,v)=>r.searchShot=v,(r,v)=>r.exitOpens=v,
+                (r,v)=>r.firstShot=v,(r,v)=>r.windowShot=v,(r,v)=>r.searchShot=v,(r,v)=>r.exitOpens=v,
                 (r,v)=>r.exitCloses=v,(r,v)=>r.blackout=v,(r,v)=>r.playLimit=v,(r,v)=>r.endingLength=v
             };
             double[] values={double.NaN,double.PositiveInfinity,double.NegativeInfinity};
@@ -161,9 +162,120 @@ public static class LoopModelChecks
                 Need(bad,names[i]+" accepted "+value);
             }
         });
+        Check(passed,"Window shot kills at t=9 when the shield is up but the blinds are open",()=>{
+            var m=new LoopModel();m.Start();m.RaiseShield(1);
+            m.Advance(8.5);Need(m.Phase==SessionPhase.Playing && !m.TryExit(1),"alive before the window shot");
+            m.Advance(.5);
+            Need(m.Phase==SessionPhase.Blackout,"window death");
+            var last=m.Records[m.Records.Count-1];
+            Need(last.kind=="window_shot" && last.loop==1,"cause "+last.kind);Near(last.loopTime,9);
+            m.Advance(.16);Need(m.LoopId==2 && !m.BlindsClosed && !m.ShieldRaised,"world reset after the window death");
+        });
+        Check(passed,"Closed blinds block the window shot and the flank still comes at t=12",()=>{
+            var m=new LoopModel();m.Start();m.Advance(2);
+            Need(m.RaiseShield(1) && m.CloseBlinds(1) && m.BlindsClosed,"inputs accepted");
+            m.Advance(7);
+            Need(m.Phase==SessionPhase.Playing && Near2(m.LoopTime,9),"survives t=9");
+            Need(m.Records[m.Records.Count-1].kind=="window_blocked","window_blocked");
+            m.Advance(3);
+            Need(m.Phase==SessionPhase.Blackout && m.Records[m.Records.Count-1].kind=="flanked","flanked at t=12");
+            Near(m.Records[m.Records.Count-1].loopTime,12);
+        });
+        Check(passed,"Exit opens at t=9.5 after the window shot and stays open until t=12",()=>{
+            var m=new LoopModel();m.Start();m.RaiseShield(1);m.CloseBlinds(1);
+            m.Advance(9.25);
+            Need(!m.ExitAvailable && !m.TryExit(1) && m.Phase==SessionPhase.Playing,"exit before 9.5 must fail");
+            m.Advance(.25);Need(m.ExitAvailable,"exit at 9.5");
+            m.Advance(2.25);
+            Need(m.ExitAvailable && m.TryExit(1) && m.Outcome==SessionPhase.Escaped,"escape late in the window");
+            var late=new LoopModel();late.Start();late.RaiseShield(1);late.CloseBlinds(1);late.Advance(9.5);
+            Need(late.TryExit(1) && late.Outcome==SessionPhase.Escaped,"escape right at 9.5");
+        });
+        Check(passed,"Dying to the first shot skips the window shot (blinds do not stop the first shot)",()=>{
+            var m=new LoopModel();m.Start();m.Advance(10);
+            Need(Kinds(m,1)=="loop_started,first_shot","no input: "+Kinds(m,1));
+            Need(m.LoopId==2 && Kinds(m,2)=="loop_started","next loop clean");
+            var b=new LoopModel();b.Start();Need(b.CloseBlinds(1),"blinds");b.Advance(10);
+            Need(Kinds(b,1)=="loop_started,blinds_closed,first_shot","blinds only: "+Kinds(b,1));
+        });
+        Check(passed,"One long step from t=5 to t=13 resolves 6, 9, 12 in order",()=>{
+            string[] expected={
+                "loop_started,first_shot",
+                "loop_started,shield_raised,shot_blocked,window_shot",
+                "loop_started,blinds_closed,first_shot",
+                "loop_started,shield_raised,blinds_closed,shot_blocked,window_blocked,flanked"
+            };
+            for(int i=0;i<4;i++){
+                bool shield=(i&1)!=0,blinds=(i&2)!=0;
+                var m=new LoopModel();m.Start();m.Advance(5);
+                if(shield)Need(m.RaiseShield(1),"shield");
+                if(blinds)Need(m.CloseBlinds(1),"blinds");
+                m.Advance(8);
+                int index=(shield?1:0)+(blinds?2:0);
+                Need(Kinds(m,1)==expected[index],"case "+index+": "+Kinds(m,1));
+                Near(m.TotalTime,13);
+            }
+        });
+        Check(passed,"Stale or out-of-phase CloseBlinds is rejected and blinds reset each loop",()=>{
+            var idle=new LoopModel();Need(!idle.CloseBlinds(0) && !idle.CloseBlinds(1),"rejected before Start");
+            var m=new LoopModel();m.Start();
+            Need(!m.CloseBlinds(2),"future loop id rejected");
+            Need(m.CloseBlinds(1) && !m.CloseBlinds(1),"accepted once per loop");
+            m.Advance(6);Need(m.Phase==SessionPhase.Blackout,"first shot kills despite the blinds");
+            Need(!m.CloseBlinds(1),"rejected during blackout");
+            m.Advance(.16);
+            Need(m.LoopId==2 && !m.BlindsClosed,"blinds back up in the new loop");
+            Need(!m.CloseBlinds(1) && !m.BlindsClosed,"stale loop id rejected");
+            Need(m.CloseBlinds(2) && m.BlindsClosed,"current loop accepted");
+        });
+        Check(passed,"SameCauseStreak counts repeats, restarts on a new cause, and ignores escape/interrupt",()=>{
+            var m=new LoopModel();m.Start();
+            Need(m.LastDeathCause==null && m.SameCauseStreak==0,"clean start");
+            m.Advance(6);Need(m.LastDeathCause==null && m.SameCauseStreak==0,"updated only when the next loop begins");
+            m.Advance(.16);Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==1,"first death");
+            m.Advance(6.16);Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==2,"second same death");
+            m.Advance(6.16);Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==3,"third same death");
+            m.RaiseShield(m.LoopId);m.Advance(9.16);
+            Need(m.LastDeathCause=="window_shot" && m.SameCauseStreak==1,"different cause resets to 1");
+            m.RaiseShield(m.LoopId);m.Advance(9.16);
+            Need(m.LastDeathCause=="window_shot" && m.SameCauseStreak==2,"window repeat");
+            m.RaiseShield(m.LoopId);m.CloseBlinds(m.LoopId);m.Advance(9.5);
+            Need(m.TryExit(m.LoopId),"escape");
+            Need(m.LastDeathCause=="window_shot" && m.SameCauseStreak==2,"escape leaves the streak alone");
+            m.Advance(8);m.Start();
+            Need(m.LastDeathCause==null && m.SameCauseStreak==0,"fresh session clears the streak");
+            var n=new LoopModel();n.Start();n.Advance(6.16);n.Interrupt();
+            Need(n.LastDeathCause=="first_shot" && n.SameCauseStreak==1,"interrupt leaves the streak alone");
+        });
+        Check(passed,"windowShot must sit at least MinInterval inside (firstShot, searchShot)",()=>{
+            Near(new LoopRules().windowShot,9);Near(new LoopRules().exitOpens,9.5);
+            new LoopRules{windowShot=6.0+LoopRules.MinInterval}.Validate();
+            new LoopRules{windowShot=12.0-LoopRules.MinInterval}.Validate();
+            Action<LoopRules>[] bad={
+                r=>r.windowShot=r.firstShot,
+                r=>r.windowShot=5.0,
+                r=>r.windowShot=r.firstShot+LoopRules.MinInterval/2,
+                r=>r.windowShot=r.searchShot,
+                r=>r.windowShot=13.0,
+                r=>r.windowShot=r.searchShot-LoopRules.MinInterval/2
+            };
+            string[] names={"windowShot==firstShot","windowShot<firstShot","windowShot just after firstShot",
+                "windowShot==searchShot","windowShot>searchShot","windowShot just before searchShot"};
+            for(int i=0;i<bad.Length;i++){
+                var rules=new LoopRules();bad[i](rules);
+                bool threw=false;try{rules.Validate();}catch(ArgumentException){threw=true;}
+                Need(threw,names[i]+" accepted");
+            }
+        });
         return passed;
     }
 
+    static string Kinds(LoopModel m,int loop){
+        string s="";
+        foreach(var r in m.Records)if(r.loop==loop)s+=(s.Length>0?",":"")+r.kind;
+        return s;
+    }
+    static bool Near2(double a,double b)=>Math.Abs(a-b)<.00001;
     static void Check(List<string> list,string name,Action test){test();list.Add("PASS: "+name);}
     static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
     static void Near(double a,double b){Need(Math.Abs(a-b)<.00001,"Expected "+a+" ~ "+b);}
diff --git a/Assets/LoopRoom/Scenes/LoopRoom.unity b/Assets/LoopRoom/Scenes/LoopRoom.unity
index 0184166..3d1a7fa 100644
--- a/Assets/LoopRoom/Scenes/LoopRoom.unity
+++ b/Assets/LoopRoom/Scenes/LoopRoom.unity
@@ -47,7 +47,8 @@ MonoBehaviour:
   timings:
     firstShot: 6
     searchShot: 12
-    exitOpens: 6.5
+    windowShot: 9
+    exitOpens: 9.5
     exitCloses: 12
     blackout: 0.16
     playLimit: 172
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index 4cf1a0b..8c667db 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -25,7 +25,7 @@ namespace LoopRoom
         AudioSource enemyAudio, ambience, doorAudio, stepAudio;
         AudioClip doorLatch, doorCreak, footstep;
         int lastEnemySteps;
-        bool desktopArg, autostart, autoescape, autostartUsed, autoShieldDone, autoExitDone;
+        bool desktopArg, autostart, autoescape, autostartUsed, autoShieldDone, autoBlindsDone, autoExitDone;
         bool simulateDrift;
         // Calibration (task021): shown before the first Ready screen and again whenever the
         // operator asks to redo it. LoopModel.Phase stays Ready throughout; this is LoopDemo-only.
@@ -337,18 +337,20 @@ namespace LoopRoom
                 PlaceRoom();
                 rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop(); doorAudio.Stop(); stepAudio.Stop();
                 room.Sound.PlayOneShot(room.Chime); lastLoop=Model.LoopId; lastLoopTime=0;
-                autoShieldDone=false; autoExitDone=false;
+                autoShieldDone=false; autoBlindsDone=false; autoExitDone=false;
             }
             // Damage/time boundaries are resolved before this frame's fresh input.
             rig.Operate(controls,Model.Phase==SessionPhase.Playing);
             if(!rig.IsVR && Model.Phase==SessionPhase.Playing && keyboard!=null)
             {
                 if(keyboard.spaceKey.wasPressedThisFrame) RaiseShield();
+                if(keyboard.bKey.wasPressedThisFrame) CloseBlinds();
                 if(keyboard.eKey.wasPressedThisFrame) TryExit();
             }
             if(autoescape && !rig.IsVR && Model.Phase==SessionPhase.Playing && Model.LoopId==2)
             {
                 if(!autoShieldDone && Model.LoopTime>=1) { autoShieldDone=true; RaiseShield(); }
+                if(!autoBlindsDone && Model.LoopTime>=1) { autoBlindsDone=true; CloseBlinds(); }
                 if(!autoExitDone && Model.ExitAvailable) { autoExitDone=true; TryExit(); }
             }
             // A long frame can cross t=3 and the shot together; LoopTime is frozen in Blackout, so the latch still plays.
@@ -366,7 +368,7 @@ namespace LoopRoom
                 for(int i=lastRecords;i<Model.Records.Count;i++)
                 {
                     var record=Model.Records[i];
-                    if(record.kind=="first_shot" || record.kind=="shot_blocked" || record.kind=="flanked")
+                    if(record.kind=="first_shot" || record.kind=="shot_blocked" || record.kind=="window_shot" || record.kind=="flanked")
                     { enemyAudio.PlayOneShot(room.Shot,.8f); rig.Haptic(.18f); }
                 }
                 lastRecords=Model.Records.Count;
@@ -459,6 +461,11 @@ namespace LoopRoom
             if(Model.RaiseShield(Model.LoopId)) { room.Sound.PlayOneShot(room.Latch); rig.Haptic(.08f); }
         }
 
+        void CloseBlinds()
+        {
+            if(Model.CloseBlinds(Model.LoopId)) { room.Sound.PlayOneShot(room.Latch,.5f); rig.Haptic(.06f); }
+        }
+
         void TryExit()
         {
             if(Model.TryExit(Model.LoopId)) room.Sound.PlayOneShot(room.Open);
@@ -559,14 +566,15 @@ namespace LoopRoom
             // task028: never shown to the HMD wearer (immersion), only the operator overlay.
             bool showNoFitWarning=(!rig.IsVR || privateOverlay) && noFitActive;
             bool showFrameStats=(!rig.IsVR || privateOverlay) && frameStats!=null;
-            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0)+(showNoFitWarning?24:0)+(showFrameStats?24:0);
+            bool showCauseStreak=!rig.IsVR || privateOverlay;
+            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0)+(showNoFitWarning?24:0)+(showFrameStats?24:0)+(showCauseStreak?24:0);
             GUI.Box(new Rect(16,16,360,boxHeight),GUIContent.none);
             GUI.Label(new Rect(32,28,340,40),"第零室 / THE ROOM BEFORE",title);
             GUI.Label(new Rect(32,72,340,28),"LOOP "+Model.LoopId.ToString("00")+"  ·  "+PublicState(),body);
             if(!rig.IsVR || privateOverlay)
             {
                 float y=107;
-                GUI.Label(new Rect(32,y,340,26),rig.CanStart ? "Enter 開始 / Space 遮蔽 / E 出口" : "開始前の接続と追跡を確認中",small);
+                GUI.Label(new Rect(32,y,340,26),rig.CanStart ? "Enter 開始 / Space 遮蔽 / B 窓 / E 出口" : "開始前の接続と追跡を確認中",small);
                 y+=24;
                 GUI.Label(new Rect(32,y,340,26),calibrating ? "C: キャリブレーションを決定" : "C: キャリブレーションをやり直す（"+(aligned?"済":"未")+"）",small);
                 y+=24;
@@ -589,6 +597,8 @@ namespace LoopRoom
                 // 落ち n（目標 xx Hz）" overflowed the panel's right edge on desktop at high refresh rates.
                 if(showFrameStats) GUI.Label(new Rect(32,y,340,26),"フレーム "+frameStats.MeanMs.ToString("F1")+" / P95 "+
                     frameStats.P95Ms.ToString("F1")+" ms・落ち "+frameStats.Dropped+"・"+frameStats.TargetHz.ToString("F0")+"Hz",small);
+                if(showFrameStats) y+=24;
+                if(showCauseStreak) GUI.Label(new Rect(32,y,340,26),"直前の死因 "+(Model.LastDeathCause??"なし")+" ×"+Model.SameCauseStreak,small);
             }
         }
 
diff --git a/Assets/LoopRoom/Scripts/LoopModel.cs b/Assets/LoopRoom/Scripts/LoopModel.cs
index 6068248..97c4332 100644
--- a/Assets/LoopRoom/Scripts/LoopModel.cs
+++ b/Assets/LoopRoom/Scripts/LoopModel.cs
@@ -11,8 +11,9 @@ namespace LoopRoom
         public const double MinInterval = 0.05;
         const double IntervalTolerance = 1e-9;
         public double firstShot = 6.0;
+        public double windowShot = 9.0;
         public double searchShot = 12.0;
-        public double exitOpens = 6.5;
+        public double exitOpens = 9.5;
         public double exitCloses = 12.0;
         public double blackout = 0.16;
         public double playLimit = 172.0;
@@ -24,6 +25,7 @@ namespace LoopRoom
         public void Validate()
         {
             if (double.IsNaN(firstShot) || double.IsInfinity(firstShot) ||
+                double.IsNaN(windowShot) || double.IsInfinity(windowShot) ||
                 double.IsNaN(searchShot) || double.IsInfinity(searchShot) ||
                 double.IsNaN(exitOpens) || double.IsInfinity(exitOpens) ||
                 double.IsNaN(exitCloses) || double.IsInfinity(exitCloses) ||
@@ -38,6 +40,8 @@ namespace LoopRoom
                 throw new ArgumentException("Invalid loop timing rules");
             if (firstShot < MinInterval - IntervalTolerance ||
                 searchShot - firstShot < MinInterval - IntervalTolerance ||
+                windowShot - firstShot < MinInterval - IntervalTolerance ||
+                searchShot - windowShot < MinInterval - IntervalTolerance ||
                 exitCloses - exitOpens < MinInterval - IntervalTolerance ||
                 blackout < MinInterval - IntervalTolerance ||
                 endingLength < MinInterval - IntervalTolerance ||
@@ -69,11 +73,16 @@ namespace LoopRoom
         public double LoopTime { get; private set; }
         public bool ShieldRaised { get; private set; }
         public bool ShotResolved { get; private set; }
+        public bool BlindsClosed { get; private set; }
+        public string LastDeathCause { get; private set; }
+        public int SameCauseStreak { get; private set; }
         public bool ExitAvailable => Phase == SessionPhase.Playing && ShotResolved &&
             LoopTime >= rules.exitOpens && LoopTime < rules.exitCloses;
         public double ExitOpens => rules.exitOpens;
         public double BlackoutRemaining { get; private set; }
         double endingRemaining;
+        bool windowResolved;
+        string pendingDeathCause;
 
         public LoopModel(LoopRules rules = null)
         {
@@ -86,16 +95,29 @@ namespace LoopRoom
             if (Phase != SessionPhase.Ready && Phase != SessionPhase.Finished) return;
             Records.Clear(); TotalTime = 0; LoopId = 0;
             Outcome = SessionPhase.Ready;
+            LastDeathCause = null; SameCauseStreak = 0; pendingDeathCause = null;
             BeginLoop();
         }
 
         void BeginLoop()
         {
+            if (pendingDeathCause != null)
+            {
+                SameCauseStreak = pendingDeathCause == LastDeathCause ? SameCauseStreak + 1 : 1;
+                LastDeathCause = pendingDeathCause; pendingDeathCause = null;
+            }
             LoopId++; LoopTime = 0; ShieldRaised = false; ShotResolved = false;
+            BlindsClosed = false; windowResolved = false;
             BlackoutRemaining = 0; Phase = SessionPhase.Playing;
             Record("loop_started");
         }
 
+        public bool CloseBlinds(int expectedLoop)
+        {
+            if (Phase != SessionPhase.Playing || expectedLoop != LoopId || BlindsClosed) return false;
+            BlindsClosed = true; Record("blinds_closed"); return true;
+        }
+
         public bool RaiseShield(int expectedLoop)
         {
             if (Phase != SessionPhase.Playing || expectedLoop != LoopId || ShieldRaised) return false;
@@ -111,7 +133,8 @@ namespace LoopRoom
         public bool Kill(int expectedLoop, string cause)
         {
             if (Phase != SessionPhase.Playing || expectedLoop != LoopId) return false;
-            Record(cause); Phase = SessionPhase.Blackout; BlackoutRemaining = rules.blackout;
+            Record(cause); pendingDeathCause = cause;
+            Phase = SessionPhase.Blackout; BlackoutRemaining = rules.blackout;
             return true;
         }
 
@@ -155,7 +178,7 @@ namespace LoopRoom
                     else if (BlackoutRemaining < 0.0000001) BeginLoop();
                     continue;
                 }
-                double next = ShotResolved ? rules.searchShot : rules.firstShot;
+                double next = !ShotResolved ? rules.firstShot : !windowResolved ? rules.windowShot : rules.searchShot;
                 double slice = Math.Min(delta, Math.Min(next - LoopTime, untilLimit));
                 LoopTime += slice; TotalTime += slice; delta -= slice;
                 if (rules.enforcePlayLimit && TotalTime >= rules.playLimit - 0.0000001) { End(SessionPhase.TimedOut); continue; }
@@ -167,6 +190,12 @@ namespace LoopRoom
                         if (ShieldRaised) Record("shot_blocked");
                         else Kill(LoopId, "first_shot");
                     }
+                    else if (!windowResolved)
+                    {
+                        windowResolved = true;
+                        if (BlindsClosed) Record("window_blocked");
+                        else Kill(LoopId, "window_shot");
+                    }
                     else Kill(LoopId, "flanked");
                 }
             }
diff --git a/Tests/LoopModel.Tests/Program.cs b/Tests/LoopModel.Tests/Program.cs
index ca211f7..9ec89e9 100644
--- a/Tests/LoopModel.Tests/Program.cs
+++ b/Tests/LoopModel.Tests/Program.cs
@@ -8,7 +8,7 @@ internal static class Program
         try
         {
             var existing = LoopModelChecks.Run();
-            Require(existing.Count == 19, "Expected 19 existing checks.");
+            Require(existing.Count == 27, "Expected 27 existing checks.");
             foreach (var result in existing) Console.WriteLine(result);
             var model = new LoopModel(new LoopRules { enforcePlayLimit = true }); model.Start(); model.Advance(172);
             Require(model.Phase == SessionPhase.TimedOut, "Timeout at 172 seconds.");
@@ -22,7 +22,7 @@ internal static class Program
             Near(model.TotalTime, 180);
             Require(model.Records.Count == records, "Finished stops recording.");
             Console.WriteLine("PASS: No-action deadline and retained outcome at 180 seconds");
-            Console.WriteLine("PASS: 20 model checks");
+            Console.WriteLine("PASS: 28 model checks");
             var roomAnchor = RoomAnchorChecks.Run();
             Require(roomAnchor.Count == 12, "Expected 12 room anchor checks.");
             foreach (var result in roomAnchor) Console.WriteLine(result);
```
