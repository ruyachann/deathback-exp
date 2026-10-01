# task032 差分（Opus 作成）

Assets/LoopRoom/Scripts/DoorRig.cs 654e4592b68eee408d19506f0e219a3e0d4414a5736a05cce491ba90ef063232
Assets/LoopRoom/Scripts/RoomVisuals.cs 445777f7ca6b24e25d2509c804b660495960250f07f3243e0a99e9bc4abc34ba
Assets/LoopRoom/Scripts/LoopDemo.cs 7d4983cc7b222d1b49564b0e584f2e7287ad8f9ab7405528045f7a13ea8b991d
Assets/LoopRoom/Scripts/ProceduralAudio.cs 2c0cb798436468242e7b27c1fe33272c8a74846e4cc8e7f64e1bd3cad16da2a2

新規ファイル DoorRig.cs は Read で全文を読むこと。

```diff
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index a8efa94..1dd1e45 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -22,7 +22,8 @@ namespace LoopRoom
         bool saved, privateOverlay;
         float trackingLost;
         string sessionId, logMessage = "";
-        AudioSource enemyAudio, ambience;
+        AudioSource enemyAudio, ambience, doorAudio;
+        AudioClip doorLatch, doorCreak;
         bool desktopArg, autostart, autoescape, autostartUsed, autoShieldDone, autoExitDone;
         bool simulateDrift;
         // Calibration (task021): shown before the first Ready screen and again whenever the
@@ -133,6 +134,11 @@ namespace LoopRoom
             enemyAudio = new GameObject("Enemy audio").AddComponent<AudioSource>();
             enemyAudio.transform.SetParent(room.Root,false); enemyAudio.playOnAwake=false;
             enemyAudio.spatialBlend=1; enemyAudio.minDistance=.5f; enemyAudio.maxDistance=10; enemyAudio.volume=.22f;
+            // The door's own 3D source sits in the doorway, so lever, latch and creak come from where the door is.
+            doorAudio = new GameObject("Door audio").AddComponent<AudioSource>();
+            doorAudio.transform.SetParent(room.Root,false); doorAudio.transform.localPosition=new Vector3(.5f,1.0f,2.8f);
+            doorAudio.playOnAwake=false; doorAudio.spatialBlend=1; doorAudio.minDistance=.5f; doorAudio.maxDistance=10; doorAudio.volume=.5f;
+            doorLatch=ProceduralAudio.DoorLatch(); doorCreak=ProceduralAudio.DoorCreak();
             // Local, not world, position: room.Sound is a child of room.Root, so this keeps the
             // chime coming from the room's front after Begin()/loop-change repositions Root.
             room.Sound.transform.localPosition = new Vector3(0,1.4f,.7f);
@@ -321,7 +327,7 @@ namespace LoopRoom
                 // frame, so the move itself is never seen (see task018 design note 3).
                 if(simulateDrift) rig.SimulateDesktopDrift(Model.LoopId);
                 PlaceRoom();
-                rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop();
+                rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop(); doorAudio.Stop();
                 room.Sound.PlayOneShot(room.Chime); lastLoop=Model.LoopId; lastLoopTime=0;
                 autoShieldDone=false; autoExitDone=false;
             }
@@ -340,6 +346,11 @@ namespace LoopRoom
             // A long frame can cross t=3 and the shot together; LoopTime is frozen in Blackout, so the latch still plays.
             if((Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout) && lastLoopTime<3 && Model.LoopTime>=3)
                 enemyAudio.PlayOneShot(room.Latch);
+            // Same crossing as the t=3 cue above; the clip itself starts .1s late so the lever and latch clicks follow the thump.
+            if((Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout) && lastLoopTime<DoorRig.StartTime && Model.LoopTime>=DoorRig.StartTime)
+                doorAudio.PlayOneShot(doorLatch);
+            if(Model.Phase==SessionPhase.Playing && lastLoopTime<DoorRig.CreakTime && Model.LoopTime>=DoorRig.CreakTime)
+                doorAudio.PlayOneShot(doorCreak,.7f);
             if(Model.Phase==SessionPhase.Playing && lastLoopTime<Model.ExitOpens && Model.LoopTime>=Model.ExitOpens)
                 room.Sound.PlayOneShot(room.Open,.6f);
             if(lastRecords<Model.Records.Count)
@@ -450,7 +461,7 @@ namespace LoopRoom
             float t=(float)Model.LoopTime;
             bool playing=Model.Phase==SessionPhase.Playing;
             room.Barrier.localPosition=new Vector3(0,Model.ShieldRaised?1.65f:.35f,1.08f);
-            room.Door.localPosition=new Vector3(.5f+Mathf.Clamp01((t-3)/.65f)*.9f,1.0f,2.86f);
+            room.Door.SetLoopTime(t);
             room.Enemy.gameObject.SetActive(t>=3 && (playing || Model.Phase==SessionPhase.Blackout));
             float move=Mathf.Clamp01((t-8)/3.5f);
             room.Enemy.localPosition=Vector3.Lerp(new Vector3(.5f,0,2.5f),new Vector3(1.25f,0,.38f),move);
diff --git a/Assets/LoopRoom/Scripts/ProceduralAudio.cs b/Assets/LoopRoom/Scripts/ProceduralAudio.cs
index 55ecaf5..87b8df8 100644
--- a/Assets/LoopRoom/Scripts/ProceduralAudio.cs
+++ b/Assets/LoopRoom/Scripts/ProceduralAudio.cs
@@ -65,6 +65,54 @@ namespace LoopRoom
             return Build("Latch", samples, rate);
         }
 
+        // Entry door at t=3: the lever drops (duller click) and the latch lets go (brighter tick). The first .10s is
+        // silent on purpose so these clicks trail the t=3 Latch thump instead of stacking on it.
+        public static AudioClip DoorLatch()
+        {
+            const int rate = SampleRate; const float duration = .32f; uint seed = 33011;
+            int n = (int)(duration * rate); var samples = new float[n];
+            for (int i = 0; i < n; i++)
+            {
+                float t = (float)i / rate; float s = 0f;
+                if (t >= .10f)
+                {
+                    float lt = t - .10f;
+                    s += (NextNoise(ref seed) * .5f + Mathf.Sin(2 * Mathf.PI * 1250f * lt)) * Mathf.Exp(-150f * lt) * .7f;
+                }
+                if (t >= .155f)
+                {
+                    float lt = t - .155f;
+                    s += (NextNoise(ref seed) * .7f + Mathf.Sin(2 * Mathf.PI * 2300f * lt) * .6f) * Mathf.Exp(-260f * lt);
+                }
+                samples[i] = s;
+            }
+            Normalize(samples, .5f);
+            return Build("DoorLatch", samples, rate);
+        }
+
+        // Hinge creak while the door is pushed open: a gliding stick-slip pitch that rises with the push and
+        // dies away as the door slows. Starts at CreakTime, when the main swing begins.
+        public static AudioClip DoorCreak()
+        {
+            const int rate = SampleRate; const float duration = .8f; uint seed = 44017;
+            int n = (int)(duration * rate); var samples = new float[n];
+            float phase = 0f, lowpass = 0f, wobble = 0f;
+            for (int i = 0; i < n; i++)
+            {
+                float t = (float)i / rate; float u = t / duration;
+                float freq = 85f + 120f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.15f)) + 18f * Mathf.Sin(2 * Mathf.PI * 7f * t);
+                phase += freq / rate; phase -= Mathf.Floor(phase);
+                float saw = phase * 2f - 1f;
+                wobble += (NextNoise(ref seed) - wobble) * .0015f;
+                float raw = saw * (.55f + 1.6f * Mathf.Abs(wobble)) + .12f * NextNoise(ref seed);
+                lowpass += (raw - lowpass) * .22f;
+                float envelope = Mathf.Min(1f, t / .06f) * Mathf.Pow(1f - u, 1.4f);
+                samples[i] = lowpass * envelope;
+            }
+            Normalize(samples, .4f);
+            return Build("DoorCreak", samples, rate);
+        }
+
         // Gunshot: sharp noise attack, a low impact tone, and a short quiet noise tail for reverb.
         public static AudioClip Shot()
         {
diff --git a/Assets/LoopRoom/Scripts/RoomVisuals.cs b/Assets/LoopRoom/Scripts/RoomVisuals.cs
index 81c0a47..d6f69c8 100644
--- a/Assets/LoopRoom/Scripts/RoomVisuals.cs
+++ b/Assets/LoopRoom/Scripts/RoomVisuals.cs
@@ -8,7 +8,8 @@ namespace LoopRoom
 {
     public sealed class RoomVisuals
     {
-        public Transform Root, Barrier, Enemy, Door;
+        public Transform Root, Barrier, Enemy;
+        public DoorRig Door;
         public XRSimpleInteractable ShieldHandle, ExitHandle;
         public TextMesh Card, Clock, ExitLabel;
         public Renderer ExitLamp;
@@ -316,7 +317,10 @@ namespace LoopRoom
             // The player's standing spot (origin) and the reach-zone furniture below keep their earlier positions.
             // One texture repeat represents 2m of floor, .5m of wood, 1m of plaster, or .25m of fabric.
             Shape("Floor",PrimitiveType.Cube,new Vector3(0,-.08f,1),new Vector3(3.9f,.16f,6),wood,smoothness:.28f,texture:floorTexture,textureScale:new Vector2(1.95f,3f));
-            Shape("Back wall",PrimitiveType.Cube,new Vector3(0,1.25f,3),new Vector3(3.9f,2.5f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(3.9f,2.5f));
+            // The back wall is cut around the 0.8m x 2.0m door opening (x .1..0.9) so the corridor behind it is a real space.
+            Shape("Back wall left of door",PrimitiveType.Cube,new Vector3(-.925f,1.25f,3),new Vector3(2.05f,2.5f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(2.05f,2.5f));
+            Shape("Back wall right of door",PrimitiveType.Cube,new Vector3(1.425f,1.25f,3),new Vector3(1.05f,2.5f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(1.05f,2.5f));
+            Shape("Back wall above door",PrimitiveType.Cube,new Vector3(.5f,2.25f,3),new Vector3(.8f,.5f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(.8f,.5f));
             // The left wall is cut around the window opening so the outdoor view sits behind real depth.
             Shape("Left wall below window",PrimitiveType.Cube,new Vector3(-1.875f,.5f,1),new Vector3(.15f,1.0f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,1.0f));
             Shape("Left wall above window",PrimitiveType.Cube,new Vector3(-1.875f,2.275f,1),new Vector3(.15f,.45f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,.45f));
@@ -325,7 +329,8 @@ namespace LoopRoom
             Shape("Right wall",PrimitiveType.Cube,new Vector3(1.875f,1.25f,1),new Vector3(.15f,2.5f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,2.5f));
             Detail("Reach rug",PrimitiveType.Cube,new Vector3(0,.007f,.3f),new Vector3(1.0f,.014f,.6f),new Color(.33f,.27f,.22f),smoothness:.08f,texture:fabricTexture,textureScale:new Vector2(4,2.4f));
             Detail("Ceiling",PrimitiveType.Cube,new Vector3(0,2.51f,1),new Vector3(3.8f,.12f,4),ivory,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(3.8f,4));
-            Detail("Back baseboard",PrimitiveType.Cube,new Vector3(0,.12f,2.88f),new Vector3(3.6f,.20f,.09f),trim,smoothness:.10f);
+            Detail("Back baseboard left",PrimitiveType.Cube,new Vector3(-.9f,.12f,2.88f),new Vector3(1.8f,.20f,.09f),trim,smoothness:.10f);
+            Detail("Back baseboard right",PrimitiveType.Cube,new Vector3(1.4f,.12f,2.88f),new Vector3(.8f,.20f,.09f),trim,smoothness:.10f);
             Detail("Left baseboard",PrimitiveType.Cube,new Vector3(-1.755f,.12f,.9625f),new Vector3(.09f,.20f,3.925f),trim,smoothness:.10f);
             Detail("Right baseboard",PrimitiveType.Cube,new Vector3(1.755f,.12f,.9625f),new Vector3(.09f,.20f,3.925f),trim,smoothness:.10f);
             BuildCornerShade();
@@ -354,27 +359,40 @@ namespace LoopRoom
             Barrier = Shape("Shield",PrimitiveType.Cube,new Vector3(0,.45f,1.08f),new Vector3(1.85f,1.6f,.08f),ink,true).transform;
 
             // The moving door remains private because its state is an escape clue.
-            Door = new GameObject("Entry door").transform; Door.SetParent(Root,false);
-            // Door is 0.8m wide and 2.0m high; its origin is the slab centre (y=1.0), so the closed slab sits on the floor.
-            Door.localPosition=new Vector3(.5f,1.0f,2.86f); Door.gameObject.layer=PrivateLayer;
-            Detail("Door slab",PrimitiveType.Cube,Vector3.zero,new Vector3(.8f,2.0f,.09f),doorWood,true,Door,smoothness:.27f,texture:woodTexture,textureScale:new Vector2(1.6f,4.0f));
-            Detail("Door upper panel",PrimitiveType.Cube,new Vector3(0,.38f,-.055f),new Vector3(.58f,.66f,.025f),darkWood,true,Door,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.32f));
-            Detail("Door lower panel",PrimitiveType.Cube,new Vector3(0,-.45f,-.055f),new Vector3(.58f,.62f,.025f),darkWood,true,Door,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.24f));
-            Detail("Door knob",PrimitiveType.Sphere,new Vector3(-.29f,0,-.09f),Vector3.one*.085f,metal,true,Door,smoothness:.55f,metallic:.65f);
+            // The hinge is on the left jamb (x=.1, y=1.0 is the slab centre): the open door lies along x~0, clear of the
+            // enemy's rightward path, the switch and plant on the right, and swings 105 degrees into the room.
+            var hinge = new GameObject("Entry door hinge").transform; hinge.SetParent(Root,false);
+            hinge.localPosition=new Vector3(.1f,1.0f,2.86f); hinge.gameObject.layer=PrivateLayer;
+            Detail("Door slab",PrimitiveType.Cube,new Vector3(.4f,0,0),new Vector3(.8f,2.0f,.09f),doorWood,true,hinge,smoothness:.27f,texture:woodTexture,textureScale:new Vector2(1.6f,4.0f));
+            foreach (var side in new[]{-1f,1f})
+            {
+                Detail("Door upper panel",PrimitiveType.Cube,new Vector3(.4f,.38f,side*.055f),new Vector3(.58f,.66f,.025f),darkWood,true,hinge,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.32f));
+                Detail("Door lower panel",PrimitiveType.Cube,new Vector3(.4f,-.45f,side*.055f),new Vector3(.58f,.62f,.025f),darkWood,true,hinge,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.24f));
+                Detail("Door rose",PrimitiveType.Cylinder,new Vector3(.69f,-.08f,side*.053f),new Vector3(.06f,.008f,.06f),metal,true,hinge,smoothness:.55f,metallic:.65f).transform.localRotation=Quaternion.Euler(90,0,0);
+            }
+            // The lever pivots on the spindle at the free edge and points toward the hinge; DoorRig turns it down before the latch lets go.
+            var handle = new GameObject("Door handle").transform; handle.SetParent(hinge,false);
+            handle.localPosition=new Vector3(.69f,-.08f,0); handle.gameObject.layer=PrivateLayer;
+            foreach (var side in new[]{-1f,1f})
+                Detail("Door lever",PrimitiveType.Cube,new Vector3(-.05f,0,side*.076f),new Vector3(.11f,.02f,.026f),metal,true,handle,smoothness:.55f,metallic:.65f);
+            Door = new DoorRig(hinge,handle);
+            BuildCorridor(wallTexture);
             Detail("Door lintel",PrimitiveType.Cube,new Vector3(.5f,2.04f,2.865f),new Vector3(1.0f,.08f,.12f),trim,smoothness:.10f);
             Detail("Door frame L",PrimitiveType.Cube,new Vector3(.05f,1.04f,2.865f),new Vector3(.10f,2.08f,.12f),trim,smoothness:.10f);
             Detail("Door frame R",PrimitiveType.Cube,new Vector3(.95f,1.04f,2.865f),new Vector3(.10f,2.08f,.12f),trim,smoothness:.10f);
 
             // The spectator sees a permanently closed copy, so the real door's motion remains private.
             var publicDoor = new GameObject("Public closed door").transform; publicDoor.SetParent(Root,false);
-            publicDoor.localPosition=new Vector3(.5f,1.0f,2.86f); publicDoor.gameObject.layer=PublicLayer;
+            publicDoor.localPosition=new Vector3(.1f,1.0f,2.86f); publicDoor.gameObject.layer=PublicLayer;
             var publicDoorParts = new[]
             {
-                Detail("Public door slab",PrimitiveType.Cube,Vector3.zero,new Vector3(.8f,2.0f,.09f),doorWood,parent:publicDoor,smoothness:.27f,texture:woodTexture,textureScale:new Vector2(1.6f,4.0f)),
-                Detail("Public door upper panel",PrimitiveType.Cube,new Vector3(0,.38f,-.055f),new Vector3(.58f,.66f,.025f),darkWood,parent:publicDoor,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.32f)),
-                Detail("Public door lower panel",PrimitiveType.Cube,new Vector3(0,-.45f,-.055f),new Vector3(.58f,.62f,.025f),darkWood,parent:publicDoor,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.24f)),
-                Detail("Public door knob",PrimitiveType.Sphere,new Vector3(-.29f,0,-.09f),Vector3.one*.085f,metal,parent:publicDoor,smoothness:.55f,metallic:.65f)
+                Detail("Public door slab",PrimitiveType.Cube,new Vector3(.4f,0,0),new Vector3(.8f,2.0f,.09f),doorWood,parent:publicDoor,smoothness:.27f,texture:woodTexture,textureScale:new Vector2(1.6f,4.0f)),
+                Detail("Public door upper panel",PrimitiveType.Cube,new Vector3(.4f,.38f,-.055f),new Vector3(.58f,.66f,.025f),darkWood,parent:publicDoor,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.32f)),
+                Detail("Public door lower panel",PrimitiveType.Cube,new Vector3(.4f,-.45f,-.055f),new Vector3(.58f,.62f,.025f),darkWood,parent:publicDoor,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.24f)),
+                Detail("Public door rose",PrimitiveType.Cylinder,new Vector3(.69f,-.08f,-.053f),new Vector3(.06f,.008f,.06f),metal,parent:publicDoor,smoothness:.55f,metallic:.65f),
+                Detail("Public door lever",PrimitiveType.Cube,new Vector3(.64f,-.08f,-.076f),new Vector3(.11f,.02f,.026f),metal,parent:publicDoor,smoothness:.55f,metallic:.65f)
             };
+            publicDoorParts[3].transform.localRotation=Quaternion.Euler(90,0,0);
             foreach (var publicDoorPart in publicDoorParts)
             {
                 publicDoorPart.layer=PublicLayer;
@@ -491,6 +509,25 @@ namespace LoopRoom
             renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
         }
 
+        // Short corridor behind the entry door (interior x -.3..1.3, z 3.075..5.0, 2.4m high). It is private like the door,
+        // so the spectator never sees past the permanently closed copy. One unshadowed spot lights it, aimed away from the room.
+        void BuildCorridor(Texture wallTexture)
+        {
+            var floorColor=new Color(.40f,.38f,.35f); var corridorWall=new Color(.56f,.57f,.54f); var corridorCeiling=new Color(.62f,.62f,.58f);
+            const float z0=3.075f, z1=5.0f, centerZ=(z0+z1)/2, length=z1-z0;
+            Detail("Corridor floor",PrimitiveType.Cube,new Vector3(.5f,.005f,centerZ),new Vector3(1.6f,.01f,length),floorColor,true,smoothness:.30f);
+            Detail("Corridor left wall",PrimitiveType.Cube,new Vector3(-.35f,1.2f,centerZ),new Vector3(.1f,2.4f,length),corridorWall,true,smoothness:.05f,texture:wallTexture,textureScale:new Vector2(length,2.4f));
+            Detail("Corridor right wall",PrimitiveType.Cube,new Vector3(1.35f,1.2f,centerZ),new Vector3(.1f,2.4f,length),corridorWall,true,smoothness:.05f,texture:wallTexture,textureScale:new Vector2(length,2.4f));
+            Detail("Corridor end wall",PrimitiveType.Cube,new Vector3(.5f,1.2f,z1+.05f),new Vector3(1.8f,2.4f,.1f),corridorWall,true,smoothness:.05f,texture:wallTexture,textureScale:new Vector2(1.8f,2.4f));
+            Detail("Corridor ceiling",PrimitiveType.Cube,new Vector3(.5f,2.425f,centerZ+.05f),new Vector3(1.8f,.05f,length+.1f),corridorCeiling,true,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(1.8f,length));
+            Detail("Corridor lamp",PrimitiveType.Cylinder,new Vector3(.5f,2.392f,3.9f),new Vector3(.34f,.008f,.34f),new Color(.92f,.96f,.88f),true,smoothness:.20f,emission:.5f);
+            var lamp=new GameObject("Corridor light").AddComponent<Light>();
+            lamp.transform.SetParent(Root,false); lamp.transform.localPosition=new Vector3(.5f,2.3f,3.4f);
+            lamp.transform.localRotation=Quaternion.Euler(25,0,0); lamp.type=LightType.Spot;
+            lamp.spotAngle=120; lamp.innerSpotAngle=70; lamp.range=3.2f; lamp.intensity=1.1f;
+            lamp.color=new Color(.86f,.93f,.84f); lamp.shadows=LightShadows.None;
+        }
+
         // Cheap stand-in for ambient occlusion: dark transparent gradient strips along floor, ceiling and wall corners.
         void BuildCornerShade()
         {
```
