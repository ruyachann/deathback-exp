# task027 差分（追修正2の後、計画担当が作成）

Assets/LoopRoom/Scripts/LoopDemo.cs cda0552efd6d0ff62174c2acf57168ab07525aaa600f08c9fddf501e81ce3ec6
Assets/LoopRoom/Scripts/CalibrationView.cs 556b7ab6d61a92011d0a3f6f99a447a36ffd778d56da8c1ca844401fb06b1618
Assets/LoopRoom/Scripts/ProceduralAudio.cs abfa232a09f14b50a85080b7399c2889a55290480ba92cdfdf7642069139be7e

```diff
diff --git a/Assets/LoopRoom/Scripts/CalibrationView.cs b/Assets/LoopRoom/Scripts/CalibrationView.cs
index 94b0c4d..0f52d62 100644
--- a/Assets/LoopRoom/Scripts/CalibrationView.cs
+++ b/Assets/LoopRoom/Scripts/CalibrationView.cs
@@ -23,6 +23,7 @@ namespace LoopRoom
         PlayAreaSettings settings;
         Renderer outerRenderer;
         LineRenderer boundaryLine;
+        LineRenderer holdRing;
 
         static readonly Color FitColor = new Color(.25f,1f,.5f);
         static readonly Color NoFitColor = new Color(.9f,.2f,.15f);
@@ -31,6 +32,10 @@ namespace LoopRoom
         static readonly Color BoundaryColor = new Color(.8f,.8f,.85f);
         static readonly Color SectorColor = new Color(.55f,.75f,.82f);
         static readonly Color FootColor = new Color(1f,.82f,.3f);
+        // task027 追修正2-1: was .009f, below the sector (.010f) and thus hidden under it where the
+        // two overlap. Placed above every other private-layer line here (incl. BoundaryHeight, the
+        // highest of the rest) so the ring never sits under the outline/margin/sector/boundary.
+        const float HoldRingHeight=.024f, HoldRingRadius=.10f, HoldRingWidth=.018f;
         // Neutral stand-in for the (hidden) room floor, dark enough that the white/green/red
         // outline stays readable against it (追修正: the desk was hiding the outline).
         static readonly Color FloorColor = new Color(.16f,.17f,.19f);
@@ -50,6 +55,7 @@ namespace LoopRoom
             Square("Area margin",settings.areaSize-2*settings.margin,MarginHeight,MarginColor,.02f);
             BuildSector();
             BuildFootMarker();
+            BuildHoldRing();
             var boundaryGo=new GameObject("Guardian boundary"); boundaryGo.transform.SetParent(Root,false);
             boundaryGo.layer=RoomVisuals.PrivateLayer;
             boundaryLine=boundaryGo.AddComponent<LineRenderer>();
@@ -138,8 +144,42 @@ namespace LoopRoom
             renderer.sharedMaterial=RoomVisuals.Material(FootColor,true);
         }
 
+        void BuildHoldRing()
+        {
+            var go=new GameObject("Hold ring"); go.transform.SetParent(Root,false); go.layer=RoomVisuals.PrivateLayer;
+            holdRing=go.AddComponent<LineRenderer>();
+            SetupLine(holdRing,FootColor,HoldRingWidth,false);
+            // Belt-and-braces alongside HoldRingHeight: draw after the sector even if a future
+            // change narrows the height gap (getter instantiates a per-renderer copy, so this
+            // doesn't affect the shared FootColor material the foot marker also uses).
+            holdRing.material.renderQueue+=1;
+            holdRing.positionCount=0;
+            go.SetActive(false);
+        }
+
         public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);
 
+        // task027: progress of the A/X (or C, or --auto-calibrate) hold, 0..1. Drawn as an arc
+        // around the foot marker that grows clockwise from nothing (t<=0, hidden) to a full ring
+        // (t=1, about to commit), brightening toward FootColor as it fills.
+        public void SetHoldProgress(float t)
+        {
+            t=Mathf.Clamp01(t);
+            holdRing.gameObject.SetActive(t>0f);
+            if(t<=0f) { holdRing.positionCount=0; return; }
+            const int segments=40;
+            int count=Mathf.Max(2,Mathf.RoundToInt(segments*t)+1);
+            holdRing.positionCount=count;
+            float sweep=360f*t;
+            for(int i=0;i<count;i++)
+            {
+                float rad=(-90f+sweep*i/(count-1))*Mathf.Deg2Rad;
+                holdRing.SetPosition(i,new Vector3(Mathf.Cos(rad)*HoldRingRadius,HoldRingHeight,Mathf.Sin(rad)*HoldRingRadius));
+            }
+            var dim=new Color(FootColor.r*.45f,FootColor.g*.45f,FootColor.b*.45f,1f);
+            holdRing.material.color=Color.Lerp(dim,FootColor,t);
+        }
+
         // headFloor/headYawDeg: the head's current floor projection/yaw (the candidate center and
         // orientation if the operator or player decides right now). boundaryWorldPoints/available:
         // from DemoRig.TryGetBoundaryPoints, already in world space.
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index 82158b3..33fe6a6 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -31,6 +31,12 @@ namespace LoopRoom
         CalibrationView calibration;
         bool calibrating = true;
         float calibrationHold;
+        // task027: 0..1 progress shown as the growing ring/guidance text, tracking whichever of
+        // the real hold (calibrationHold) or the --auto-calibrate simulated hold is further along.
+        // Kept separate from calibrationHold itself so its commit-timing logic stays untouched.
+        float calibrationHoldProgress;
+        AudioSource calibrationAudio;
+        AudioClip calibrationConfirm;
         bool calibrationVisible, wasCalibrationVisible, wasRoomHidden, lastBoundaryAvailable;
         // Set by CommitCalibration() so the operator overlay can keep showing "境界外で決定"
         // until the *next* commit, even after the calibration screen itself closes (task021 追修正3-1).
@@ -125,6 +131,12 @@ namespace LoopRoom
             ambience.transform.SetParent(room.Root,false); ambience.playOnAwake=false;
             ambience.loop=true; ambience.spatialBlend=0; ambience.volume=.12f;
             ambience.clip=ProceduralAudio.RoomTone(); ambience.Play();
+            // task027: calibration commit confirmation, played at the head (non-spatial, like a
+            // UI cue) so it is heard the same regardless of the room's/enemy's position.
+            calibrationAudio = new GameObject("Calibration audio").AddComponent<AudioSource>();
+            calibrationAudio.transform.SetParent(rig.View.transform,false);
+            calibrationAudio.playOnAwake=false; calibrationAudio.spatialBlend=0; calibrationAudio.volume=.5f;
+            calibrationConfirm = ProceduralAudio.CalibrationConfirm();
             BuildMessage();
             RefreshWorld();
         }
@@ -238,16 +250,28 @@ namespace LoopRoom
             }
             if (calibrationVisible)
             {
+                // task027 追修正2-2: an unusually long frame (e.g. the first frame after the splash
+                // screen ends) must not jump the hold/auto-calibrate clocks past their 1s feel; cap
+                // the dt fed to both at .1s/frame regardless of the real frame length.
+                float holdDt = Mathf.Min(Time.unscaledDeltaTime, .1f);
                 // Same input as DemoRig's start button (A/X); a short press must not confirm.
-                calibrationHold = rig.StartHeld ? calibrationHold + Time.unscaledDeltaTime : 0f;
-                if (calibrationHold >= 1f) CommitCalibration();
-                else if (autoCalibrate && !autoCalibrateDone)
+                calibrationHold = rig.StartHeld ? calibrationHold + holdDt : 0f;
+                calibrationHoldProgress = Mathf.Clamp01(calibrationHold);
+                if (calibrationHold >= 1f) { calibrationHoldProgress = 1f; CommitCalibration(); }
+                // 追修正 2026-09-25: don't start the auto-calibrate clock until the Unity splash
+                // screen is done, otherwise the timer runs out while the splash still covers
+                // the screen and the calibration view is never actually seen.
+                else if (autoCalibrate && !autoCalibrateDone && UnityEngine.Rendering.SplashScreen.isFinished)
                 {
-                    autoCalibrateTimer += Time.unscaledDeltaTime;
-                    if (autoCalibrateTimer >= 2f) { autoCalibrateDone = true; CommitCalibration(); }
+                    autoCalibrateTimer += holdDt;
+                    // task027 追修正: 3s wait (progress stays 0, so the screen capture has time to
+                    // catch up after the splash) then a 1s simulated hold (progress 0->1), ~4s total.
+                    float simulated = Mathf.Clamp01(autoCalibrateTimer - 3f);
+                    if (simulated > calibrationHoldProgress) calibrationHoldProgress = simulated;
+                    if (autoCalibrateTimer >= 4f) { calibrationHoldProgress = 1f; autoCalibrateDone = true; CommitCalibration(); }
                 }
             }
-            else calibrationHold = 0f;
+            else { calibrationHold = 0f; calibrationHoldProgress = 0f; }
             bool enter=keyboard!=null && keyboard.enterKey.wasPressedThisFrame;
             bool autoTrigger = autostart && !autostartUsed && !rig.IsVR;
             // operatorCommandConsumed (set by C or R above, or inside CommitCalibration for the 1s
@@ -337,7 +361,7 @@ namespace LoopRoom
         // untouched here (task021 追修正4-3): the "境界外で決定" warning must persist until the
         // next CommitCalibration() actually determines a fresh Fit, not disappear just because a
         // retry/reset happened before the operator has re-decided.
-        void ResetAlignment() { aligned=false; alignCx=0; alignCz=0; alignAreaYaw=0; calibrating=true; calibrationHold=0; }
+        void ResetAlignment() { aligned=false; alignCx=0; alignCz=0; alignAreaYaw=0; calibrating=true; calibrationHold=0; calibrationHoldProgress=0; }
 
         // Commits the calibration: the current head floor position/yaw become the alignment
         // center/orientation (same 2 values PlaceRoom/RoomAnchor use), and the calibration screen closes.
@@ -353,8 +377,12 @@ namespace LoopRoom
             calibrationOutside = calibration.Fit == CalibrationView.BoundaryFit.Outside;
             if(calibrationOutside) Debug.LogWarning("LoopRoom: calibration committed outside the reported guardian boundary.");
             alignCx=head.position.x; alignCz=head.position.z; alignAreaYaw=head.eulerAngles.y;
-            aligned=true; fitsWarned=false; calibrating=false; calibrationHold=0;
+            aligned=true; fitsWarned=false; calibrating=false; calibrationHold=0; calibrationHoldProgress=0;
             operatorCommandConsumed = true;
+            // task027: confirmation cue at the moment of commit (not the Chime, which is reserved
+            // for the loop-start "death trace").
+            calibrationAudio.PlayOneShot(calibrationConfirm);
+            rig.Haptic(.12f);
         }
 
         void Begin()
@@ -420,6 +448,7 @@ namespace LoopRoom
                 var head=rig.View.transform;
                 lastBoundaryAvailable=rig.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count>=3;
                 calibration.Refresh(new Vector3(head.position.x,0,head.position.z),head.eulerAngles.y,boundaryPoints,lastBoundaryAvailable);
+                calibration.SetHoldProgress(calibrationHoldProgress);
             }
             bool show=Model.Phase!=SessionPhase.Playing && Model.Phase!=SessionPhase.Blackout;
             messagePanel.SetActive(show && messageSized); message.gameObject.SetActive(show);
@@ -431,7 +460,9 @@ namespace LoopRoom
             {
                 message.color=messageColor;
                 if(calibrationVisible)
-                    message.text="足元の枠が体験の空間です\nA か X を長押しで決定（運営: C）";
+                    message.text=calibrationHoldProgress>0f
+                        ? "足元の枠が体験の空間です\nそのまま押し続けて…"
+                        : "足元の枠が体験の空間です\nA か X を長押しで決定（運営: C）";
                 else if(Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished)
                     message.text=!rig.CanStart ? rig.PreparationMessage : rig.IsVR ? "第零室\n手元の取っ手に手を近づけ、グリップで操作\nA または X ボタンで開始" : "第零室  /  操作確認\nEnter：開始　Space：遮蔽　E：出口\n右ドラッグ：見回す";
                 else if(Model.Phase==SessionPhase.Escaped) message.text="脱出した。\n今度は、時間が進んでいる。";
diff --git a/Assets/LoopRoom/Scripts/ProceduralAudio.cs b/Assets/LoopRoom/Scripts/ProceduralAudio.cs
index f4fc386..55ecaf5 100644
--- a/Assets/LoopRoom/Scripts/ProceduralAudio.cs
+++ b/Assets/LoopRoom/Scripts/ProceduralAudio.cs
@@ -82,6 +82,23 @@ namespace LoopRoom
             return Build("Shot", samples, rate);
         }
 
+        // task027: calibration commit confirmation, a short rising two-tone tick. Deliberately
+        // distinct from Chime (the loop-start "death trace" cue, never reused here).
+        public static AudioClip CalibrationConfirm()
+        {
+            const int rate = SampleRate; const float duration = .16f;
+            const float f0 = 520f, f1 = 780f; const float decay = 22f;
+            int n = (int)(duration * rate); var samples = new float[n];
+            for (int i = 0; i < n; i++)
+            {
+                float t = (float)i / rate;
+                float freq = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / .08f));
+                samples[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-decay * t);
+            }
+            Normalize(samples, .5f);
+            return Build("CalibrationConfirm", samples, rate);
+        }
+
         // Exit unlock: two short metallic tones in sequence (click, then a brighter clink).
         public static AudioClip Open()
         {
```
