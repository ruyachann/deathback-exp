# task031 差分（Opus 作成）

Assets/LoopRoom/Scripts/EnemyFigure.cs 5394ad1efb83f0177c07c2e49798e360cf08163963d313e0e1365e5ec6b69003
Assets/LoopRoom/Scripts/RoomVisuals.cs 61e3b7a853fff73fc8f193c0f3c72c96914cdf8733bad4c209ed8706fece729b
Assets/LoopRoom/Scripts/LoopDemo.cs b7a74821381c787314022aa4ba13cda4cb8ae23339029446a42fb98c90a34a20
Assets/LoopRoom/Scripts/ProceduralAudio.cs 8e179ec9bb2fc17aa0f12f83a65f7999b9c27ffd290f490994ac6fa4b2166401

新規ファイル EnemyFigure.cs は Read で全文を読むこと。

```diff
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index 1dd1e45..4cf1a0b 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -22,8 +22,9 @@ namespace LoopRoom
         bool saved, privateOverlay;
         float trackingLost;
         string sessionId, logMessage = "";
-        AudioSource enemyAudio, ambience, doorAudio;
-        AudioClip doorLatch, doorCreak;
+        AudioSource enemyAudio, ambience, doorAudio, stepAudio;
+        AudioClip doorLatch, doorCreak, footstep;
+        int lastEnemySteps;
         bool desktopArg, autostart, autoescape, autostartUsed, autoShieldDone, autoExitDone;
         bool simulateDrift;
         // Calibration (task021): shown before the first Ready screen and again whenever the
@@ -139,6 +140,13 @@ namespace LoopRoom
             doorAudio.transform.SetParent(room.Root,false); doorAudio.transform.localPosition=new Vector3(.5f,1.0f,2.8f);
             doorAudio.playOnAwake=false; doorAudio.spatialBlend=1; doorAudio.minDistance=.5f; doorAudio.maxDistance=10; doorAudio.volume=.5f;
             doorLatch=ProceduralAudio.DoorLatch(); doorCreak=ProceduralAudio.DoorCreak();
+            // Footfalls come from the enemy's feet, so the source follows the figure's ground position.
+            stepAudio = new GameObject("Enemy footsteps").AddComponent<AudioSource>();
+            stepAudio.transform.SetParent(room.Root,false); stepAudio.playOnAwake=false;
+            stepAudio.spatialBlend=1; stepAudio.minDistance=.5f; stepAudio.maxDistance=10; stepAudio.volume=.3f;
+            footstep=ProceduralAudio.Footstep();
+            var shotRules=Model.Rules;
+            room.Figure.FirstShot=(float)shotRules.firstShot; room.Figure.SearchShot=(float)shotRules.searchShot;
             // Local, not world, position: room.Sound is a child of room.Root, so this keeps the
             // chime coming from the room's front after Begin()/loop-change repositions Root.
             room.Sound.transform.localPosition = new Vector3(0,1.4f,.7f);
@@ -327,7 +335,7 @@ namespace LoopRoom
                 // frame, so the move itself is never seen (see task018 design note 3).
                 if(simulateDrift) rig.SimulateDesktopDrift(Model.LoopId);
                 PlaceRoom();
-                rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop(); doorAudio.Stop();
+                rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop(); doorAudio.Stop(); stepAudio.Stop();
                 room.Sound.PlayOneShot(room.Chime); lastLoop=Model.LoopId; lastLoopTime=0;
                 autoShieldDone=false; autoExitDone=false;
             }
@@ -462,11 +470,17 @@ namespace LoopRoom
             bool playing=Model.Phase==SessionPhase.Playing;
             room.Barrier.localPosition=new Vector3(0,Model.ShieldRaised?1.65f:.35f,1.08f);
             room.Door.SetLoopTime(t);
-            room.Enemy.gameObject.SetActive(t>=3 && (playing || Model.Phase==SessionPhase.Blackout));
-            float move=Mathf.Clamp01((t-8)/3.5f);
-            room.Enemy.localPosition=Vector3.Lerp(new Vector3(.5f,0,2.5f),new Vector3(1.25f,0,.38f),move);
-            room.Enemy.localRotation=Quaternion.Euler(0,move*75,0);
-            enemyAudio.transform.localPosition=room.Enemy.localPosition+new Vector3(0,1.4f,0);
+            bool enemyOn=t>=3 && (playing || Model.Phase==SessionPhase.Blackout);
+            room.Enemy.gameObject.SetActive(enemyOn);
+            if(enemyOn)
+            {
+                room.Figure.SetState(t,room.Root.InverseTransformPoint(rig.View.transform.position));
+                if(playing && room.Figure.StepCount>lastEnemySteps)
+                { stepAudio.transform.localPosition=room.Figure.GroundPosition; stepAudio.PlayOneShot(footstep); }
+                lastEnemySteps=room.Figure.StepCount;
+                enemyAudio.transform.localPosition=room.Figure.GroundPosition+new Vector3(0,1.4f,0);
+            }
+            else lastEnemySteps=0;
             room.Clock.text="00 : "+Mathf.FloorToInt(t).ToString("00");
             Color lamp=Model.ExitAvailable?new Color(.25f,1,.66f):new Color(.7f,.13f,.09f);
             room.ExitLamp.material.color=lamp;
diff --git a/Assets/LoopRoom/Scripts/ProceduralAudio.cs b/Assets/LoopRoom/Scripts/ProceduralAudio.cs
index 87b8df8..82edc17 100644
--- a/Assets/LoopRoom/Scripts/ProceduralAudio.cs
+++ b/Assets/LoopRoom/Scripts/ProceduralAudio.cs
@@ -65,6 +65,23 @@ namespace LoopRoom
             return Build("Latch", samples, rate);
         }
 
+        // Enemy footfall: a short dull heel thud with a soft low-passed scuff, quieter and shorter than Latch.
+        public static AudioClip Footstep()
+        {
+            const int rate = SampleRate; const float duration = .11f; uint seed = 61003;
+            int n = (int)(duration * rate); var samples = new float[n];
+            float scuff = 0f;
+            for (int i = 0; i < n; i++)
+            {
+                float t = (float)i / rate;
+                float thud = Mathf.Sin(2 * Mathf.PI * (70f + 40f * Mathf.Exp(-60f * t)) * t) * Mathf.Exp(-42f * t);
+                scuff += (NextNoise(ref seed) - scuff) * .25f;
+                samples[i] = thud * .8f + scuff * Mathf.Exp(-55f * t) * .5f;
+            }
+            Normalize(samples, .45f);
+            return Build("Footstep", samples, rate);
+        }
+
         // Entry door at t=3: the lever drops (duller click) and the latch lets go (brighter tick). The first .10s is
         // silent on purpose so these clicks trail the t=3 Latch thump instead of stacking on it.
         public static AudioClip DoorLatch()
diff --git a/Assets/LoopRoom/Scripts/RoomVisuals.cs b/Assets/LoopRoom/Scripts/RoomVisuals.cs
index d6f69c8..df339e8 100644
--- a/Assets/LoopRoom/Scripts/RoomVisuals.cs
+++ b/Assets/LoopRoom/Scripts/RoomVisuals.cs
@@ -9,6 +9,7 @@ namespace LoopRoom
     public sealed class RoomVisuals
     {
         public Transform Root, Barrier, Enemy;
+        public EnemyFigure Figure;
         public DoorRig Door;
         public XRSimpleInteractable ShieldHandle, ExitHandle;
         public TextMesh Card, Clock, ExitLabel;
@@ -461,14 +462,7 @@ namespace LoopRoom
             Detail("Plant leaves 2",PrimitiveType.Sphere,new Vector3(1.43f,.84f,2.62f),new Vector3(.26f,.46f,.26f),new Color(.36f,.54f,.30f),smoothness:.12f);
             Detail("Plant leaves 3",PrimitiveType.Sphere,new Vector3(1.58f,.78f,2.55f),new Vector3(.24f,.40f,.24f),new Color(.26f,.43f,.26f),smoothness:.12f);
 
-            Enemy = new GameObject("Enemy").transform; Enemy.SetParent(Root,false);
-            Shape("Coat",PrimitiveType.Capsule,new Vector3(0,1.0f,0),new Vector3(.38f,.65f,.28f),ink,true,Enemy);
-            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.68f,0),Vector3.one*.26f,ink,true,Enemy);
-            Shape("Visor",PrimitiveType.Cube,new Vector3(0,1.70f,-.13f),new Vector3(.20f,.035f,.025f),new Color(.8f,.19f,.10f),true,Enemy,emission:2f);
-            Shape("Weapon",PrimitiveType.Cube,new Vector3(-.13f,1.36f,-.24f),new Vector3(.09f,.10f,.35f),ink,true,Enemy);
-            Detail("Left shoulder",PrimitiveType.Sphere,new Vector3(-.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
-            Detail("Right shoulder",PrimitiveType.Sphere,new Vector3(.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
-            Detail("Hat brim",PrimitiveType.Cube,new Vector3(0,1.83f,-.015f),new Vector3(.43f,.035f,.34f),ink,true,Enemy,smoothness:.08f);
+            Figure = new EnemyFigure(Root); Enemy = Figure.Root;
             // Flat canopy disc plus a shallow dome shade, mounted flush on the 2.45m ceiling.
             Detail("Ceiling light canopy",PrimitiveType.Cylinder,new Vector3(0,2.435f,.2f),new Vector3(.46f,.012f,.46f),ivory,smoothness:.22f);
             Detail("Ceiling light shade",PrimitiveType.Sphere,new Vector3(0,2.44f,.2f),new Vector3(.40f,.28f,.40f),new Color(1,.93f,.82f),smoothness:.18f,emission:.55f);
```
