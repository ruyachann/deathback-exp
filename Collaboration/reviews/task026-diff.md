# task026 LoopDemo.cs・DemoRig.cs の差分（追修正3の後、計画担当が作成）

LoopDemo.cs 9b056c077d44d7356f58dc6e31bdf5b7c241d821c5036d851975d861f4006b67
DemoRig.cs 425ae38fe0e5f09bbed74072d4d5f42e3406aab9898848b1683993a6ed9eb618

```diff
diff --git a/Assets/LoopRoom/Scripts/DemoRig.cs b/Assets/LoopRoom/Scripts/DemoRig.cs
index a074df5..4f0eeac 100644
--- a/Assets/LoopRoom/Scripts/DemoRig.cs
+++ b/Assets/LoopRoom/Scripts/DemoRig.cs
@@ -199,6 +199,24 @@ namespace LoopRoom
             return displays.Exists(d => d.running);
         }
 
+        // Frame-time target for FrameStats (task026): the running XR display's reported
+        // refresh rate in VR, the desktop monitor's refresh rate otherwise. Falls back to a
+        // sane default (matches the design's 72/60 Hz) when the rate cannot be read.
+        public double GetTargetHz()
+        {
+            if (IsVR)
+            {
+                SubsystemManager.GetSubsystems(displays);
+                foreach (var display in displays)
+                    if (display.running && display.TryGetDisplayRefreshRate(out float hz) &&
+                        !float.IsNaN(hz) && !float.IsInfinity(hz) && hz >= 1 && hz <= 1000)
+                        return hz;
+                return 72;
+            }
+            var rate = Screen.currentResolution.refreshRateRatio.value;
+            return !double.IsNaN(rate) && !double.IsInfinity(rate) && rate >= 1 && rate <= 1000 ? rate : 60;
+        }
+
         // Guardian/play-area boundary, if the running Floor-mode input subsystem can report one
         // (task021 calibration screen). Points come back in world space: XRInputSubsystem returns
         // them relative to the tracking origin, which for this rig is the "XR Origin" GameObject's
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index 009f64d..82158b3 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -55,6 +55,22 @@ namespace LoopRoom
         bool aligned;
         // Limits the "reach doesn't fit" warning to once per alignment (task018 追修正2-3).
         bool fitsWarned;
+        // task026: frame-time stats for the Air Link device check. Created on the first Begin()
+        // and recreated only if the target Hz changes (VR<->desktop), otherwise just Reset().
+        FrameStats frameStats;
+        // Set in Begin() so the very first Update() after a session start skips its dt (that dt
+        // spans pre-Begin time, e.g. startup/menu, not gameplay; task026 追修正2).
+        bool frameStatsSkipFirst;
+
+        [Serializable] sealed class FrameStatsSummary
+        {
+            public double targetHz;
+            public long frames;
+            public double meanMs;
+            public double p95Ms;
+            public double maxMs;
+            public long dropped;
+        }
 
         [Serializable] sealed class SessionLog
         {
@@ -66,6 +82,7 @@ namespace LoopRoom
             public double elapsed;
             public LoopRules timings;
             public LoopRecord[] events;
+            public FrameStatsSummary frames;
         }
 
         void Start()
@@ -253,7 +270,16 @@ namespace LoopRoom
                     RefreshWorld(); return;
                 }
             }
+            // Classified by the state *before* Advance (task026 追修正3): this Update's dt is the
+            // time that elapsed while that state was active, regardless of what Advance() moves
+            // Phase to afterward.
+            bool wasStatsPhase = Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout;
             Model.Advance(Time.unscaledDeltaTime);
+            if(frameStats!=null && wasStatsPhase)
+            {
+                if(frameStatsSkipFirst) frameStatsSkipFirst=false;
+                else frameStats.Add(Time.unscaledDeltaTime);
+            }
             if(Model.LoopId!=lastLoop)
             {
                 // Reposition happens before RefreshWorld() below turns the blackout off this same
@@ -338,6 +364,10 @@ namespace LoopRoom
             rig.ClearSelection(); trackingLost=0;
             PlaceRoom();
             sessionId=Guid.NewGuid().ToString("N"); saved=false; lastLoop=0; lastRecords=0;
+            double targetHz=rig.GetTargetHz();
+            if(frameStats==null || frameStats.TargetHz!=targetHz) frameStats=new FrameStats(targetHz);
+            else frameStats.Reset();
+            frameStatsSkipFirst=true;
             Model.Start();
         }
 
@@ -418,9 +448,14 @@ namespace LoopRoom
                 string directory=Path.Combine(Application.persistentDataPath,"Sessions");
                 Directory.CreateDirectory(directory);
                 var record=new SessionLog { sessionId=sessionId, mode=rig.IsVR?"PCVR":"Desktop",
-                    outcome=Model.Outcome.ToString(),elapsed=Model.TotalTime,timings=Model.Rules,events=Model.Records.ToArray() };
+                    outcome=Model.Outcome.ToString(),elapsed=Model.TotalTime,timings=Model.Rules,events=Model.Records.ToArray(),
+                    frames=frameStats==null?null:new FrameStatsSummary{ targetHz=frameStats.TargetHz,frames=frameStats.Frames,
+                        meanMs=frameStats.MeanMs,p95Ms=frameStats.P95Ms,maxMs=frameStats.MaxMs,dropped=frameStats.Dropped } };
                 File.WriteAllText(Path.Combine(directory,sessionId+".json"),JsonUtility.ToJson(record,true));
                 logMessage="周回記録を保存しました";
+                if(frameStats!=null) Debug.Log("LoopRoom: frame stats target="+frameStats.TargetHz.ToString("F1")+"Hz frames="+frameStats.Frames+
+                    " mean="+frameStats.MeanMs.ToString("F2")+"ms p95="+frameStats.P95Ms.ToString("F2")+"ms max="+frameStats.MaxMs.ToString("F2")+
+                    "ms dropped="+frameStats.Dropped);
             }
             catch(Exception e) { logMessage="記録保存に失敗: "+e.GetType().Name; Debug.LogWarning(logMessage); }
         }
@@ -444,7 +479,8 @@ namespace LoopRoom
             // Stays visible from the moment of commit until the *next* commit, even once the
             // calibration screen itself has closed (task021 追修正3-1).
             bool showOutsideWarning=(!rig.IsVR || privateOverlay) && calibrationOutside;
-            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0);
+            bool showFrameStats=(!rig.IsVR || privateOverlay) && frameStats!=null;
+            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0)+(showFrameStats?24:0);
             GUI.Box(new Rect(16,16,360,boxHeight),GUIContent.none);
             GUI.Label(new Rect(32,28,340,40),"第零室 / THE ROOM BEFORE",title);
             GUI.Label(new Rect(32,72,340,28),"LOOP "+Model.LoopId.ToString("00")+"  ·  "+PublicState(),body);
@@ -461,6 +497,11 @@ namespace LoopRoom
                 GUI.Label(new Rect(32,y,340,26),"右ドラッグ 視点 / Esc 中断 / F2 運営表示",small);
                 y+=24;
                 GUI.Label(new Rect(32,y,340,26),"S01  "+Model.TotalTime.ToString("F1")+"s  "+logMessage,small);
+                y+=24;
+                // Shortened form (task026 追修正5): the original "フレーム 平均 xx.x ms / P95 xx.x ms /
+                // 落ち n（目標 xx Hz）" overflowed the panel's right edge on desktop at high refresh rates.
+                if(showFrameStats) GUI.Label(new Rect(32,y,340,26),"フレーム "+frameStats.MeanMs.ToString("F1")+" / P95 "+
+                    frameStats.P95Ms.ToString("F1")+" ms・落ち "+frameStats.Dropped+"・"+frameStats.TargetHz.ToString("F0")+"Hz",small);
             }
         }
 
```
