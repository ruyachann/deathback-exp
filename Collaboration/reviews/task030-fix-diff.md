# task030 追修正の差分（該当ハンクのみ、Opus 作成）

RoomVisuals.cs 3e5767bae1ab5dca12b58903bc9007879049f44d643edb39b612f626dee37d34

```diff
@@ -192,4 +192,60 @@ namespace LoopRoom
         }
 
+        static Texture2D SkyTexture()
+        {
+            const int height=128;
+            var top=new Color(.50f,.72f,.92f); var haze=new Color(.84f,.92f,.97f); var ground=new Color(.60f,.66f,.60f);
+            var pixels=new Color32[4*height];
+            for(int y=0;y<height;y++)
+            {
+                float v=y/(height-1f);
+                Color c=v<.34f ? ground : v<.44f ? Color.Lerp(ground,haze,(v-.34f)/.10f) : Color.Lerp(haze,top,Mathf.Clamp01((v-.44f)/.56f));
+                for(int x=0;x<4;x++) pixels[y*4+x]=c;
+            }
+            var texture=new Texture2D(4,height,TextureFormat.RGB24,false,false);
+            texture.name="Window sky"; texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Bilinear;
+            texture.SetPixels32(pixels); texture.Apply(false,true);
+            return texture;
+        }
+
+        // Alpha fades from .28 at v=0 (the corner) to 0 at v=1 (away from it).
+        static Texture2D CornerShadeTexture()
+        {
+            const int height=32;
+            var pixels=new Color32[4*height];
+            for(int y=0;y<height;y++)
+            {
+                float fade=1f-y/(height-1f);
+                var c=new Color32(0,0,0,(byte)Mathf.RoundToInt(fade*fade*.28f*255f));
+                for(int x=0;x<4;x++) pixels[y*4+x]=c;
+            }
+            var texture=new Texture2D(4,height,TextureFormat.RGBA32,false,false);
+            texture.name="Corner shade"; texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Bilinear;
+            texture.SetPixels32(pixels); texture.Apply(false,true);
+            return texture;
+        }
+
+        static Material CornerShadeMaterial(Texture texture)
+        {
+            var shader=Shader.Find("Universal Render Pipeline/Unlit");
+            if(shader==null) shader=Shader.Find("Sprites/Default");
+            var m=new Material(shader){name="Corner shade"};
+            m.color=Color.black;
+            if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",Color.black);
+            if(m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",texture);
+            if(m.HasProperty("_MainTex")) m.SetTexture("_MainTex",texture);
+            // Blend state is set through properties rather than keywords so no shader variant is needed in a build.
+            if(m.HasProperty("_Surface")) m.SetFloat("_Surface",1);
+            if(m.HasProperty("_Blend")) m.SetFloat("_Blend",0);
+            if(m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
+            if(m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
+            if(m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
+            if(m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
+            if(m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite",0);
+            m.SetOverrideTag("RenderType","Transparent");
+            m.renderQueue=(int)RenderQueue.Transparent;
+            return m;
+        }
+
         public static Material TextMaterial(Font font)
         {
@@ -225,4 +281,9 @@ namespace LoopRoom
         }
 
+        static void NoShadow(GameObject go)
+        {
+            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
+        }
+
         TextMesh Text(string name, string text, Vector3 p, float size, Color color, bool hidden = true, Transform parent = null)
         {
@@ -248,18 +309,25 @@ namespace LoopRoom
             var wood = new Color(.49f,.32f,.18f); var darkWood = new Color(.29f,.18f,.10f);
             var fabric = new Color(.67f,.70f,.72f); var whiteFabric = new Color(.88f,.87f,.82f);
-            var curtain = new Color(.84f,.82f,.76f); var sky = new Color(.64f,.80f,.91f);
+            var curtain = new Color(.84f,.82f,.76f);
             var metal = new Color(.45f,.47f,.48f); var doorWood = new Color(.55f,.38f,.23f);
             var floorTexture=FloorTexture(); var woodTexture=WoodTexture();
             var wallTexture=WallTexture(); var fabricTexture=FabricTexture();
+            // Interior is 3.6m wide (x=±1.8), z=-1..2.925, 2.45m high: a six-to-eight-tatami one-room flat.
+            // The player's standing spot (origin) and the reach-zone furniture below keep their earlier positions.
             // One texture repeat represents 2m of floor, .5m of wood, 1m of plaster, or .25m of fabric.
-            Shape("Floor",PrimitiveType.Cube,new Vector3(0,-.08f,1),new Vector3(5,.16f,6),wood,smoothness:.28f,texture:floorTexture,textureScale:new Vector2(2.5f,3f));
-            Shape("Back wall",PrimitiveType.Cube,new Vector3(0,1.6f,3),new Vector3(5,3.2f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(5,3.2f));
-            Shape("Left wall",PrimitiveType.Cube,new Vector3(-2.5f,1.6f,1),new Vector3(.15f,3.2f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,3.2f));
-            Shape("Right wall",PrimitiveType.Cube,new Vector3(2.5f,1.6f,1),new Vector3(.15f,3.2f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,3.2f));
+            Shape("Floor",PrimitiveType.Cube,new Vector3(0,-.08f,1),new Vector3(3.9f,.16f,6),wood,smoothness:.28f,texture:floorTexture,textureScale:new Vector2(1.95f,3f));
+            Shape("Back wall",PrimitiveType.Cube,new Vector3(0,1.25f,3),new Vector3(3.9f,2.5f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(3.9f,2.5f));
+            // The left wall is cut around the window opening so the outdoor view sits behind real depth.
+            Shape("Left wall below window",PrimitiveType.Cube,new Vector3(-1.875f,.5f,1),new Vector3(.15f,1.0f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,1.0f));
+            Shape("Left wall above window",PrimitiveType.Cube,new Vector3(-1.875f,2.275f,1),new Vector3(.15f,.45f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,.45f));
+            Shape("Left wall front of window",PrimitiveType.Cube,new Vector3(-1.875f,1.525f,.025f),new Vector3(.15f,1.05f,2.05f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(2.05f,1.05f));
+            Shape("Left wall behind window",PrimitiveType.Cube,new Vector3(-1.875f,1.525f,2.575f),new Vector3(.15f,1.05f,.85f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(.85f,1.05f));
+            Shape("Right wall",PrimitiveType.Cube,new Vector3(1.875f,1.25f,1),new Vector3(.15f,2.5f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,2.5f));
             Detail("Reach rug",PrimitiveType.Cube,new Vector3(0,.007f,.3f),new Vector3(1.0f,.014f,.6f),new Color(.33f,.27f,.22f),smoothness:.08f,texture:fabricTexture,textureScale:new Vector2(4,2.4f));
-            Detail("Ceiling",PrimitiveType.Cube,new Vector3(0,3.22f,1),new Vector3(5,.12f,4),ivory,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(5,4));
-            Detail("Back baseboard",PrimitiveType.Cube,new Vector3(0,.12f,2.84f),new Vector3(4.86f,.20f,.09f),trim,smoothness:.10f);
-            Detail("Left baseboard",PrimitiveType.Cube,new Vector3(-2.34f,.12f,1),new Vector3(.09f,.20f,3.86f),trim,smoothness:.10f);
-            Detail("Right baseboard",PrimitiveType.Cube,new Vector3(2.34f,.12f,1),new Vector3(.09f,.20f,3.86f),trim,smoothness:.10f);
+            Detail("Ceiling",PrimitiveType.Cube,new Vector3(0,2.51f,1),new Vector3(3.8f,.12f,4),ivory,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(3.8f,4));
+            Detail("Back baseboard",PrimitiveType.Cube,new Vector3(0,.12f,2.88f),new Vector3(3.6f,.20f,.09f),trim,smoothness:.10f);
+            Detail("Left baseboard",PrimitiveType.Cube,new Vector3(-1.755f,.12f,.9625f),new Vector3(.09f,.20f,3.925f),trim,smoothness:.10f);
+            Detail("Right baseboard",PrimitiveType.Cube,new Vector3(1.755f,.12f,.9625f),new Vector3(.09f,.20f,3.925f),trim,smoothness:.10f);
+            BuildCornerShade();
 
             // Small desk at the established interaction position. All ordinary furniture is colliderless.
@@ -315,17 +384,28 @@ namespace LoopRoom
             }
 
-            // Open curtains and a plain bright exterior keep the room familiar rather than ominous.
-            Detail("Window daylight",PrimitiveType.Cube,new Vector3(-2.40f,1.88f,1.42f),new Vector3(.025f,1.30f,1.42f),sky,smoothness:.02f,emission:.20f,unlit:true);
-            Detail("Window top",PrimitiveType.Cube,new Vector3(-2.36f,2.57f,1.42f),new Vector3(.08f,.10f,1.58f),ivory,smoothness:.10f);
-            Detail("Window bottom",PrimitiveType.Cube,new Vector3(-2.36f,1.19f,1.42f),new Vector3(.08f,.10f,1.58f),ivory,smoothness:.10f);
-            Detail("Window front frame",PrimitiveType.Cube,new Vector3(-2.36f,1.88f,.59f),new Vector3(.08f,1.48f,.10f),ivory,smoothness:.10f);
-            Detail("Window back frame",PrimitiveType.Cube,new Vector3(-2.36f,1.88f,2.25f),new Vector3(.08f,1.48f,.10f),ivory,smoothness:.10f);
-            Detail("Window mullion",PrimitiveType.Cube,new Vector3(-2.35f,1.88f,1.42f),new Vector3(.09f,1.28f,.055f),ivory,smoothness:.10f);
-            Detail("Curtain rod",PrimitiveType.Cube,new Vector3(-2.29f,2.67f,1.42f),new Vector3(.07f,.06f,2.02f),metal,smoothness:.45f,metallic:.45f);
-            Detail("Open curtain front",PrimitiveType.Cube,new Vector3(-2.28f,1.86f,.40f),new Vector3(.09f,1.62f,.28f),curtain,smoothness:.05f,texture:fabricTexture,textureScale:new Vector2(1.12f,6.48f));
-            Detail("Open curtain back",PrimitiveType.Cube,new Vector3(-2.28f,1.86f,2.44f),new Vector3(.09f,1.62f,.28f),curtain,smoothness:.05f,texture:fabricTexture,textureScale:new Vector2(1.12f,6.48f));
+            // The window is a real opening in the left wall; a sky gradient and a few distant slate buildings sit outside it.
+            var skyTexture=SkyTexture();
+            Outdoor("Window sky",PrimitiveType.Quad,new Vector3(-8f,1.5f,3f),new Vector3(16,8,1),Color.white,Quaternion.Euler(0,-90,0),skyTexture);
+            var buildingColor=new Color(.64f,.71f,.79f);
+            Outdoor("Distant building 1",PrimitiveType.Cube,new Vector3(-6.4f,-.45f,1.2f),new Vector3(1f,3.1f,1.8f),buildingColor);
+            Outdoor("Distant building 2",PrimitiveType.Cube,new Vector3(-6.0f,.2f,3.6f),new Vector3(1f,4.4f,2.0f),buildingColor);
+            Outdoor("Distant building 3",PrimitiveType.Cube,new Vector3(-6.6f,-.3f,5.8f),new Vector3(1f,3.4f,1.5f),buildingColor);
+            Outdoor("Distant building 4",PrimitiveType.Cube,new Vector3(-6.2f,.45f,7.8f),new Vector3(1f,4.9f,2.2f),buildingColor);
+            Detail("Window top",PrimitiveType.Cube,new Vector3(-1.76f,2.07f,1.6f),new Vector3(.08f,.10f,1.30f),ivory,smoothness:.10f);
+            Detail("Window bottom",PrimitiveType.Cube,new Vector3(-1.74f,1.0f,1.6f),new Vector3(.12f,.10f,1.30f),ivory,smoothness:.10f);
+            Detail("Window front frame",PrimitiveType.Cube,new Vector3(-1.76f,1.535f,1.05f),new Vector3(.08f,1.14f,.10f),ivory,smoothness:.10f);
+            Detail("Window back frame",PrimitiveType.Cube,new Vector3(-1.76f,1.535f,2.15f),new Vector3(.08f,1.14f,.10f),ivory,smoothness:.10f);
+            Detail("Window mullion",PrimitiveType.Cube,new Vector3(-1.77f,1.525f,1.6f),new Vector3(.05f,1.0f,.045f),ivory,smoothness:.10f);
+            Detail("Curtain rod",PrimitiveType.Cube,new Vector3(-1.70f,2.25f,1.6f),new Vector3(.05f,.05f,2.0f),metal,smoothness:.45f,metallic:.45f);
+            // Each curtain is three slightly offset slabs so its folds catch the light unevenly.
+            for(int i=0;i<3;i++)
+            {
+                float x=i==1 ? -1.665f : -1.70f;
+                Detail("Curtain front fold",PrimitiveType.Cube,new Vector3(x,1.55f,.683f+i*.127f),new Vector3(.06f,1.3f,.12f),curtain,smoothness:.05f,texture:fabricTexture,textureScale:new Vector2(.48f,5.2f));
+                Detail("Curtain back fold",PrimitiveType.Cube,new Vector3(x,1.55f,2.263f+i*.127f),new Vector3(.06f,1.3f,.12f),curtain,smoothness:.05f,texture:fabricTexture,textureScale:new Vector2(.48f,5.2f));
+            }
 
             var chair = new GameObject("Desk chair").transform; chair.SetParent(Root,false);
-            chair.localPosition=new Vector3(-1.15f,0,.64f); chair.localRotation=Quaternion.Euler(0,8,0);
+            chair.localPosition=new Vector3(-1.3f,0,.45f); chair.localRotation=Quaternion.Euler(0,8,0);
             Detail("Chair seat",PrimitiveType.Cube,new Vector3(0,.48f,0),new Vector3(.58f,.10f,.56f),wood,parent:chair,smoothness:.25f,texture:woodTexture,textureScale:new Vector2(1.16f,1.12f));
             Detail("Chair back",PrimitiveType.Cube,new Vector3(0,.86f,.23f),new Vector3(.58f,.68f,.09f),wood,parent:chair,smoothness:.25f,texture:woodTexture,textureScale:new Vector2(1.16f,1.36f));
@@ -333,14 +413,34 @@ namespace LoopRoom
                 Detail("Chair leg",PrimitiveType.Cube,new Vector3(x,.23f,z),new Vector3(.07f,.46f,.07f),darkWood,parent:chair,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(.14f,.92f));
 
-            Detail("Bed base",PrimitiveType.Cube,new Vector3(-1.72f,.25f,1.82f),new Vector3(1.12f,.38f,1.78f),wood,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(2.24f,3.56f));
-            Detail("Bed mattress",PrimitiveType.Cube,new Vector3(-1.72f,.49f,1.82f),new Vector3(1.06f,.22f,1.66f),whiteFabric,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(4.24f,6.64f));
-            Detail("Bed blanket",PrimitiveType.Cube,new Vector3(-1.72f,.62f,1.56f),new Vector3(1.08f,.055f,1.02f),fabric,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(4.32f,4.08f));
-            Detail("Bed pillow",PrimitiveType.Cube,new Vector3(-1.72f,.66f,2.42f),new Vector3(.72f,.12f,.34f),ivory,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(2.88f,1.36f));
-            Detail("Bed headboard",PrimitiveType.Cube,new Vector3(-1.72f,.72f,2.72f),new Vector3(1.16f,1.00f,.10f),darkWood,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(2.32f,2));
-
-            Detail("Light switch plate",PrimitiveType.Cube,new Vector3(1.61f,1.30f,2.88f),new Vector3(.18f,.26f,.025f),ivory,smoothness:.12f);
-            Detail("Light switch",PrimitiveType.Cube,new Vector3(1.61f,1.31f,2.85f),new Vector3(.07f,.12f,.025f),trim,smoothness:.12f);
-            Detail("Picture frame",PrimitiveType.Cube,new Vector3(-.78f,2.06f,2.88f),new Vector3(.82f,.68f,.035f),darkWood,smoothness:.22f,texture:woodTexture,textureScale:new Vector2(1.64f,1.36f));
-            Detail("Picture",PrimitiveType.Cube,new Vector3(-.78f,2.06f,2.84f),new Vector3(.68f,.54f,.025f),new Color(.53f,.67f,.61f),smoothness:.06f);
+            Detail("Bed base",PrimitiveType.Cube,new Vector3(-1.24f,.25f,1.88f),new Vector3(1.0f,.38f,1.90f),wood,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(2.0f,3.8f));
+            Detail("Bed mattress",PrimitiveType.Cube,new Vector3(-1.24f,.49f,1.88f),new Vector3(.94f,.22f,1.78f),whiteFabric,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(3.76f,7.12f));
+            Detail("Bed blanket",PrimitiveType.Cube,new Vector3(-1.24f,.62f,1.62f),new Vector3(.96f,.055f,1.10f),fabric,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(3.84f,4.4f));
+            Detail("Bed pillow",PrimitiveType.Cube,new Vector3(-1.24f,.66f,2.52f),new Vector3(.60f,.12f,.34f),ivory,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(2.4f,1.36f));
+            Detail("Bed headboard",PrimitiveType.Cube,new Vector3(-1.24f,.72f,2.87f),new Vector3(1.04f,1.00f,.10f),darkWood,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(2.08f,2));
+
+            Detail("Light switch plate",PrimitiveType.Cube,new Vector3(1.38f,1.20f,2.912f),new Vector3(.18f,.26f,.025f),ivory,smoothness:.12f);
+            Detail("Light switch",PrimitiveType.Cube,new Vector3(1.38f,1.21f,2.89f),new Vector3(.07f,.12f,.025f),trim,smoothness:.12f);
+            NoShadow(Detail("Outlet plate",PrimitiveType.Cube,new Vector3(-.55f,.32f,2.914f),new Vector3(.09f,.14f,.02f),ivory,smoothness:.12f));
+            foreach (var y in new[]{.345f,.295f})
+                NoShadow(Detail("Outlet slot",PrimitiveType.Cube,new Vector3(-.55f,y,2.902f),new Vector3(.05f,.012f,.01f),ink,smoothness:.05f));
+            Detail("Picture frame",PrimitiveType.Cube,new Vector3(-1.15f,1.78f,2.9075f),new Vector3(.70f,.56f,.035f),darkWood,smoothness:.22f,texture:woodTexture,textureScale:new Vector2(1.4f,1.12f));
+            Detail("Picture",PrimitiveType.Cube,new Vector3(-1.15f,1.78f,2.89f),new Vector3(.58f,.44f,.025f),new Color(.53f,.67f,.61f),smoothness:.06f);
+
+            // Colour box against the right wall, plant in the back corner: both outside the reach zone.
+            var shelfScale=new Vector2(.6f,1.2f);
+            Detail("Shelf left board",PrimitiveType.Cube,new Vector3(1.64f,.525f,1.345f),new Vector3(.32f,1.05f,.03f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
+            Detail("Shelf right board",PrimitiveType.Cube,new Vector3(1.64f,.525f,2.155f),new Vector3(.32f,1.05f,.03f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
+            Detail("Shelf top",PrimitiveType.Cube,new Vector3(1.64f,1.035f,1.75f),new Vector3(.32f,.03f,.84f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
+            Detail("Shelf middle",PrimitiveType.Cube,new Vector3(1.64f,.55f,1.75f),new Vector3(.32f,.03f,.78f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
+            Detail("Shelf bottom",PrimitiveType.Cube,new Vector3(1.64f,.045f,1.75f),new Vector3(.32f,.03f,.78f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
+            Detail("Shelf back",PrimitiveType.Cube,new Vector3(1.788f,.525f,1.75f),new Vector3(.012f,1.05f,.84f),darkWood,smoothness:.15f);
+            NoShadow(Detail("Book 1",PrimitiveType.Cube,new Vector3(1.62f,.695f,1.56f),new Vector3(.22f,.26f,.045f),new Color(.62f,.26f,.22f),smoothness:.18f));
+            NoShadow(Detail("Book 2",PrimitiveType.Cube,new Vector3(1.62f,.67f,1.61f),new Vector3(.22f,.21f,.04f),new Color(.30f,.40f,.52f),smoothness:.18f));
+            NoShadow(Detail("Book 3",PrimitiveType.Cube,new Vector3(1.62f,.69f,1.66f),new Vector3(.22f,.25f,.05f),new Color(.78f,.64f,.34f),smoothness:.18f));
+            NoShadow(Detail("Book 4",PrimitiveType.Cube,new Vector3(1.62f,.665f,1.71f),new Vector3(.22f,.20f,.035f),new Color(.34f,.48f,.38f),smoothness:.18f));
+            Detail("Plant pot",PrimitiveType.Cylinder,new Vector3(1.5f,.14f,2.6f),new Vector3(.30f,.14f,.30f),new Color(.62f,.36f,.26f),smoothness:.15f);
+            Detail("Plant leaves 1",PrimitiveType.Sphere,new Vector3(1.5f,.62f,2.6f),new Vector3(.34f,.56f,.34f),new Color(.30f,.48f,.28f),smoothness:.12f);
+            Detail("Plant leaves 2",PrimitiveType.Sphere,new Vector3(1.43f,.84f,2.62f),new Vector3(.26f,.46f,.26f),new Color(.36f,.54f,.30f),smoothness:.12f);
+            Detail("Plant leaves 3",PrimitiveType.Sphere,new Vector3(1.58f,.78f,2.55f),new Vector3(.24f,.40f,.24f),new Color(.26f,.43f,.26f),smoothness:.12f);
 
             Enemy = new GameObject("Enemy").transform; Enemy.SetParent(Root,false);
@@ -352,20 +452,21 @@ namespace LoopRoom
             Detail("Right shoulder",PrimitiveType.Sphere,new Vector3(.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
             Detail("Hat brim",PrimitiveType.Cube,new Vector3(0,1.83f,-.015f),new Vector3(.43f,.035f,.34f),ink,true,Enemy,smoothness:.08f);
-            Detail("Ceiling light base",PrimitiveType.Cylinder,new Vector3(0,3.12f,.2f),new Vector3(.34f,.055f,.34f),ivory,smoothness:.22f);
-            Detail("Ceiling light diffuser",PrimitiveType.Cylinder,new Vector3(0,3.04f,.2f),new Vector3(.29f,.045f,.29f),new Color(1,.93f,.82f),smoothness:.18f,emission:.55f);
+            // Flat canopy disc plus a shallow dome shade, mounted flush on the 2.45m ceiling.
+            Detail("Ceiling light canopy",PrimitiveType.Cylinder,new Vector3(0,2.435f,.2f),new Vector3(.46f,.012f,.46f),ivory,smoothness:.22f);
+            Detail("Ceiling light shade",PrimitiveType.Sphere,new Vector3(0,2.44f,.2f),new Vector3(.40f,.28f,.40f),new Color(1,.93f,.82f),smoothness:.18f,emission:.55f);
             var light = new GameObject("Ceiling light").AddComponent<Light>();
-            light.transform.SetParent(Root,false); light.transform.localPosition = new Vector3(0,2.86f,.2f);
+            light.transform.SetParent(Root,false); light.transform.localPosition = new Vector3(0,2.25f,.2f);
             light.transform.localRotation=Quaternion.Euler(90,0,0); light.type=LightType.Spot;
             light.spotAngle=140; light.innerSpotAngle=110;
-            light.range=6; light.intensity=2.6f; light.color=new Color(1,.93f,.82f);
+            light.range=6; light.intensity=1.6f; light.color=new Color(1,.93f,.82f);
             light.shadows=LightShadows.Soft; light.shadowResolution=LightShadowResolution.Medium;
             var ceilingFill = new GameObject("Ceiling light fill").AddComponent<Light>();
-            ceilingFill.transform.SetParent(Root,false); ceilingFill.transform.localPosition=new Vector3(0,2.86f,.2f);
-            ceilingFill.type=LightType.Point; ceilingFill.range=6; ceilingFill.intensity=.6f;
+            ceilingFill.transform.SetParent(Root,false); ceilingFill.transform.localPosition=new Vector3(0,2.25f,.2f);
+            ceilingFill.type=LightType.Point; ceilingFill.range=6; ceilingFill.intensity=.4f;
             ceilingFill.color=new Color(1,.93f,.82f); ceilingFill.shadows=LightShadows.None;
             var fill = new GameObject("Window daylight").AddComponent<Light>();
-            fill.transform.SetParent(Root,false); fill.transform.localPosition=new Vector3(-2.20f,1.95f,1.42f);
+            fill.transform.SetParent(Root,false); fill.transform.localPosition=new Vector3(-1.65f,1.55f,1.6f);
             fill.transform.localRotation=Quaternion.Euler(0,90,0); fill.type=LightType.Spot; fill.spotAngle=105;
-            fill.range=6; fill.intensity=1.25f; fill.color=new Color(.78f,.88f,1f); fill.shadows=LightShadows.None;
+            fill.range=6; fill.intensity=1.0f; fill.color=new Color(.78f,.88f,1f); fill.shadows=LightShadows.None;
             RenderSettings.ambientMode=AmbientMode.Trilight;
             RenderSettings.ambientSkyColor=new Color(.55f,.57f,.60f);
@@ -383,4 +484,45 @@ namespace LoopRoom
         }
 
+        void Outdoor(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null, Texture texture = null)
+        {
+            var go=Detail(name,type,position,scale,color,smoothness:.02f,unlit:true,texture:texture);
+            if(rotation.HasValue) go.transform.localRotation=rotation.Value;
+            var renderer=go.GetComponent<Renderer>();
+            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
+        }
+
+        // Cheap stand-in for ambient occlusion: dark transparent gradient strips along floor, ceiling and wall corners.
+        void BuildCornerShade()
+        {
+            var material=CornerShadeMaterial(CornerShadeTexture());
+            const float width=.5f, half=1.8f, backZ=2.925f, frontZ=-1f, ceilingY=2.45f, wallHeight=2.45f, lift=.004f;
+            float sideLength=backZ-frontZ, sideCenter=(backZ+frontZ)/2, midHeight=wallHeight/2;
+            var up=Vector3.up; var down=Vector3.down;
+            Band(material,"Floor shade left",new Vector3(-half,lift,sideCenter),up,Vector3.right,sideLength,width);
+            Band(material,"Floor shade right",new Vector3(half,lift,sideCenter),up,Vector3.left,sideLength,width);
+            Band(material,"Floor shade back",new Vector3(0,lift,backZ),up,Vector3.back,2*half,width);
+            Band(material,"Ceiling shade left",new Vector3(-half,ceilingY-lift,sideCenter),down,Vector3.right,sideLength,width);
+            Band(material,"Ceiling shade right",new Vector3(half,ceilingY-lift,sideCenter),down,Vector3.left,sideLength,width);
+            Band(material,"Ceiling shade back",new Vector3(0,ceilingY-lift,backZ),down,Vector3.back,2*half,width);
+            Band(material,"Corner shade back left",new Vector3(-half,midHeight,backZ-lift),Vector3.back,Vector3.right,wallHeight,width);
+            Band(material,"Corner shade back right",new Vector3(half,midHeight,backZ-lift),Vector3.back,Vector3.left,wallHeight,width);
+            Band(material,"Corner shade left wall",new Vector3(-half+lift,midHeight,backZ),Vector3.right,Vector3.back,wallHeight,width);
+            Band(material,"Corner shade right wall",new Vector3(half-lift,midHeight,backZ),Vector3.left,Vector3.back,wallHeight,width);
+        }
+
+        // edge is the strip's centre on the corner line; the gradient runs from there along fade, facing normal.
+        void Band(Material material, string name, Vector3 edge, Vector3 normal, Vector3 fade, float length, float width)
+        {
+            var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name=name;
+            Object.Destroy(go.GetComponent<Collider>());
+            go.transform.SetParent(Root,false);
+            go.transform.localPosition=edge+fade*(width/2);
+            go.transform.localRotation=Quaternion.LookRotation(-normal,fade);
+            go.transform.localScale=new Vector3(length,width,1);
+            var renderer=go.GetComponent<Renderer>();
+            renderer.sharedMaterial=material;
+            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
+        }
+
         void BuildSpectator()
         {
```
