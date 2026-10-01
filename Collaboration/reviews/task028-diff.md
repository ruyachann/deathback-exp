# task028 LoopDemo.cs の差分（計画担当が作成）

LoopDemo.cs 92c771341d58a3c52863fa9d68dad4863c26ecb6c7ae959da7752846aeeb1a91

```diff
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index 33fe6a6..901b3e4 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -59,8 +59,17 @@ namespace LoopRoom
         // Left at (0,0,0) until the operator aligns once; RoomAnchor then just uses the head yaw as-is.
         double alignCx, alignCz, alignAreaYaw;
         bool aligned;
-        // Limits the "reach doesn't fit" warning to once per alignment (task018 追修正2-3).
+        // task028: logs the "reach doesn't fit" warning once per *transition* into fits=false
+        // (not once per alignment anymore), so a whole run of consecutive no-fit loops only logs
+        // its first occurrence.
         bool fitsWarned;
+        // task028: true while the most recent PlaceRoom() had fits=false; drives the operator-only
+        // overlay warning (never shown to the HMD wearer). Cleared as soon as a placement fits
+        // again, or the operator commits a fresh calibration.
+        bool noFitActive;
+        // task028: session-log counters for fits=false placements (reset each Begin()).
+        int noFitPlacements;
+        double maxNoFitDistance;
         // task026: frame-time stats for the Air Link device check. Created on the first Begin()
         // and recreated only if the target Hz changes (VR<->desktop), otherwise just Reset().
         FrameStats frameStats;
@@ -89,6 +98,8 @@ namespace LoopRoom
             public LoopRules timings;
             public LoopRecord[] events;
             public FrameStatsSummary frames;
+            public int noFitPlacements;
+            public double maxNoFitDistance;
         }
 
         void Start()
@@ -377,7 +388,7 @@ namespace LoopRoom
             calibrationOutside = calibration.Fit == CalibrationView.BoundaryFit.Outside;
             if(calibrationOutside) Debug.LogWarning("LoopRoom: calibration committed outside the reported guardian boundary.");
             alignCx=head.position.x; alignCz=head.position.z; alignAreaYaw=head.eulerAngles.y;
-            aligned=true; fitsWarned=false; calibrating=false; calibrationHold=0; calibrationHoldProgress=0;
+            aligned=true; fitsWarned=false; noFitActive=false; calibrating=false; calibrationHold=0; calibrationHoldProgress=0;
             operatorCommandConsumed = true;
             // task027: confirmation cue at the moment of commit (not the Chime, which is reserved
             // for the loop-start "death trace").
@@ -390,6 +401,7 @@ namespace LoopRoom
             if(!rig.CanStart) return;
             // Do not re-center tracking: the user's real position remains unchanged.
             rig.ClearSelection(); trackingLost=0;
+            noFitPlacements=0; maxNoFitDistance=0;
             PlaceRoom();
             sessionId=Guid.NewGuid().ToString("N"); saved=false; lastLoop=0; lastRecords=0;
             double targetHz=rig.GetTargetHz();
@@ -412,7 +424,15 @@ namespace LoopRoom
             bool corrected=Math.Abs(yawDiff)>0.001;
             Debug.Log("LoopRoom: PlaceRoom pos=("+px.ToString("F2")+","+pz.ToString("F2")+") headYaw="+headYaw.ToString("F1")+
                 " frontYaw="+frontYaw.ToString("F1")+" corrected="+corrected+" diff="+yawDiff.ToString("F1")+" fits="+fits);
-            if(!fits && !fitsWarned) { fitsWarned=true; Debug.LogWarning("LoopRoom: no orientation keeps the forward reach inside the safe area; using head yaw as-is."); }
+            if(!fits)
+            {
+                noFitPlacements++;
+                double distance=Math.Sqrt((px-alignCx)*(px-alignCx)+(pz-alignCz)*(pz-alignCz));
+                if(distance>maxNoFitDistance) maxNoFitDistance=distance;
+                if(!fitsWarned) { fitsWarned=true; Debug.LogWarning("LoopRoom: no orientation keeps the forward reach inside the safe area; using head yaw as-is. distance="+distance.ToString("F2")+"m"); }
+            }
+            else fitsWarned=false;
+            noFitActive=!fits;
         }
 
         void RaiseShield()
@@ -481,7 +501,8 @@ namespace LoopRoom
                 var record=new SessionLog { sessionId=sessionId, mode=rig.IsVR?"PCVR":"Desktop",
                     outcome=Model.Outcome.ToString(),elapsed=Model.TotalTime,timings=Model.Rules,events=Model.Records.ToArray(),
                     frames=frameStats==null?null:new FrameStatsSummary{ targetHz=frameStats.TargetHz,frames=frameStats.Frames,
-                        meanMs=frameStats.MeanMs,p95Ms=frameStats.P95Ms,maxMs=frameStats.MaxMs,dropped=frameStats.Dropped } };
+                        meanMs=frameStats.MeanMs,p95Ms=frameStats.P95Ms,maxMs=frameStats.MaxMs,dropped=frameStats.Dropped },
+                    noFitPlacements=noFitPlacements,maxNoFitDistance=maxNoFitDistance };
                 File.WriteAllText(Path.Combine(directory,sessionId+".json"),JsonUtility.ToJson(record,true));
                 logMessage="周回記録を保存しました";
                 if(frameStats!=null) Debug.Log("LoopRoom: frame stats target="+frameStats.TargetHz.ToString("F1")+"Hz frames="+frameStats.Frames+
@@ -510,8 +531,10 @@ namespace LoopRoom
             // Stays visible from the moment of commit until the *next* commit, even once the
             // calibration screen itself has closed (task021 追修正3-1).
             bool showOutsideWarning=(!rig.IsVR || privateOverlay) && calibrationOutside;
+            // task028: never shown to the HMD wearer (immersion), only the operator overlay.
+            bool showNoFitWarning=(!rig.IsVR || privateOverlay) && noFitActive;
             bool showFrameStats=(!rig.IsVR || privateOverlay) && frameStats!=null;
-            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0)+(showFrameStats?24:0);
+            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0)+(showNoFitWarning?24:0)+(showFrameStats?24:0);
             GUI.Box(new Rect(16,16,360,boxHeight),GUIContent.none);
             GUI.Label(new Rect(32,28,340,40),"第零室 / THE ROOM BEFORE",title);
             GUI.Label(new Rect(32,72,340,28),"LOOP "+Model.LoopId.ToString("00")+"  ·  "+PublicState(),body);
@@ -523,6 +546,14 @@ namespace LoopRoom
                 GUI.Label(new Rect(32,y,340,26),calibrating ? "C: キャリブレーションを決定" : "C: キャリブレーションをやり直す（"+(aligned?"済":"未")+"）",small);
                 y+=24;
                 if(showOutsideWarning) { GUI.Label(new Rect(32,y,340,26),"境界外で決定（要確認）",small); y+=24; }
+                if(showNoFitWarning)
+                {
+                    // task028 追修正: 暗い赤は灰色パネル上で読みにくかったため明るい色＋太字に変更。
+                    var prevColor=GUI.contentColor; GUI.contentColor=new Color(1f,.5f,.35f);
+                    var prevStyle=small.fontStyle; small.fontStyle=FontStyle.Bold;
+                    GUI.Label(new Rect(32,y,340,26),"安全な向きなし: 中央へ / C で再設定",small);
+                    small.fontStyle=prevStyle; GUI.contentColor=prevColor; y+=24;
+                }
                 if(showBoundaryHint) { GUI.Label(new Rect(32,y,340,26),"境界情報なし（目視で確認）",small); y+=24; }
                 if(rig.CanRetryPreparation) { GUI.Label(new Rect(32,y,340,26),"R: VR 再準備（運営）",small); y+=24; }
                 GUI.Label(new Rect(32,y,340,26),"右ドラッグ 視点 / Esc 中断 / F2 運営表示",small);
```
