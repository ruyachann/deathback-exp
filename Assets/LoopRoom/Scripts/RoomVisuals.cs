using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LoopRoom
{
    public sealed class RoomVisuals
    {
        public Transform Root, Barrier, Enemy, Door;
        public XRSimpleInteractable ShieldHandle, ExitHandle;
        public TextMesh Card, Clock, ExitLabel;
        public Renderer ExitLamp;
        public GameObject Blackout;
        public Camera Spectator;
        public AudioSource Sound;
        public AudioClip Chime, Shot, Latch, Open;
        public const int PrivateLayer = 8;
        const int PublicLayer = 9;
        static bool textShaderWarningLogged;
        static readonly Dictionary<MaterialKey, Material> materialCache = new Dictionary<MaterialKey, Material>();
        Font font;
        Transform head, left, right;
        GameObject publicHead, publicLeft, publicRight;

        readonly struct MaterialKey : System.IEquatable<MaterialKey>
        {
            readonly int shaderId;
            readonly Color color;
            readonly float smoothness, metallic, emission;
            readonly int textureId;
            readonly Vector2 textureScale;

            public MaterialKey(Shader shader, Color color, float smoothness, float metallic, float emission, Texture texture, Vector2 textureScale)
            {
                shaderId=shader!=null ? shader.GetInstanceID() : 0;
                this.color=color; this.smoothness=smoothness; this.metallic=metallic; this.emission=emission;
                textureId=texture!=null ? texture.GetInstanceID() : 0;
                this.textureScale=textureScale;
            }

            public bool Equals(MaterialKey other)
            {
                return shaderId==other.shaderId && color.Equals(other.color) &&
                    smoothness.Equals(other.smoothness) && metallic.Equals(other.metallic) &&
                    emission.Equals(other.emission) && textureId==other.textureId &&
                    textureScale.Equals(other.textureScale);
            }

            public override bool Equals(object obj) => obj is MaterialKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash=shaderId;
                    hash=hash*397^color.GetHashCode();
                    hash=hash*397^smoothness.GetHashCode();
                    hash=hash*397^metallic.GetHashCode();
                    hash=hash*397^emission.GetHashCode();
                    hash=hash*397^textureId;
                    return hash*397^textureScale.GetHashCode();
                }
            }
        }

        public static Material Material(Color color, bool unlit = false, float smoothness = .23f, float metallic = 0, float emission = 0, Texture texture = null, Vector2? textureScale = null)
        {
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            var scale=textureScale ?? Vector2.one;
            var key=new MaterialKey(shader,color,smoothness,metallic,emission,texture,scale);
            if(materialCache.TryGetValue(key,out var cached) && cached!=null) return cached;
            var m = new Material(shader); m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (texture != null)
            {
                if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap",texture); m.SetTextureScale("_BaseMap",scale); }
                if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex",texture); m.SetTextureScale("_MainTex",scale); }
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (emission>0 && m.HasProperty("_EmissionColor"))
            {
                var glow=color*emission; glow.a=1;
                m.SetColor("_EmissionColor",glow); m.EnableKeyword("_EMISSION");
            }
            materialCache[key]=m;
            return m;
        }

        static Texture2D Texture(string name, int size, Color32[] pixels)
        {
            // linear:false makes these neutral-valued maps sRGB, matching ordinary albedo textures.
            var texture=new Texture2D(size,size,TextureFormat.RGB24,true,false);
            texture.name=name; texture.wrapMode=TextureWrapMode.Repeat;
            texture.filterMode=FilterMode.Trilinear; texture.anisoLevel=6;
            texture.SetPixels32(pixels); texture.Apply(true,true);
            return texture;
        }

        static float Noise(int x, int y, uint seed)
        {
            uint value=(uint)x*374761393u+(uint)y*668265263u+seed*2246822519u;
            value=(value^(value>>13))*1274126177u;
            return ((value^(value>>16))&65535u)/65535f;
        }

        static float PeriodicNoise(int x, int y, int size, int cellSize, uint seed)
        {
            int cells=size/cellSize;
            int x0=(x/cellSize)%cells, y0=(y/cellSize)%cells;
            int x1=(x0+1)%cells, y1=(y0+1)%cells;
            float tx=(x%cellSize)/(float)cellSize, ty=(y%cellSize)/(float)cellSize;
            tx=tx*tx*(3f-2f*tx); ty=ty*ty*(3f-2f*ty);
            float bottom=Mathf.Lerp(Noise(x0,y0,seed),Noise(x1,y0,seed),tx);
            float top=Mathf.Lerp(Noise(x0,y1,seed),Noise(x1,y1,seed),tx);
            return Mathf.Lerp(bottom,top,ty);
        }

        static Color32 Gray(float value)
        {
            byte channel=(byte)Mathf.Clamp(Mathf.RoundToInt(value),0,255);
            return new Color32(channel,channel,channel,255);
        }

        static Texture2D FloorTexture()
        {
            const int size=512, boardWidth=128, plankLength=256;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                int wrappedY=y%size;
                int board=wrappedY/boardWidth;
                int across=wrappedY%boardWidth;
                int wrappedX=(x+(board&1)*(plankLength/2))%size;
                int along=wrappedX%plankLength;
                int plank=wrappedX/plankLength;
                float variation=(Noise(board,plank,17u)-.5f)*5f;
                float px=2f*Mathf.PI*x/size, py=2f*Mathf.PI*y/size;
                float grain=Mathf.Sin(px*15f+Mathf.Sin(py*4f)*1.6f)*1.6f;
                float value=251f+variation+grain+(PeriodicNoise(x,y,size,8,29u)-.5f)*1.5f;
                if(across<3 || across>=boardWidth-3) value=216f;
                else if(along<3) value=222f;
                pixels[y*size+x]=Gray(value);
            }
            return Texture("Floor boards",size,pixels);
        }

        static Texture2D WoodTexture()
        {
            const int size=256;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float px=2f*Mathf.PI*x/size, py=2f*Mathf.PI*y/size;
                float bend=Mathf.Sin(px*2f)*5f+Mathf.Sin(px+1.3f)*9f;
                float grain=Mathf.Sin(py*9f+bend*.23f)*2.4f+Mathf.Sin(py*3f-bend*.071f)*1.4f;
                float value=250f+grain+(PeriodicNoise(x,y,size,8,43u)-.5f)*2f;
                pixels[y*size+x]=Gray(value);
            }
            return Texture("Fine wood grain",size,pixels);
        }

        static Texture2D WallTexture()
        {
            const int size=256;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float px=2f*Mathf.PI*x/size, py=2f*Mathf.PI*y/size;
                float plaster=Mathf.Sin(px)*Mathf.Sin(py)*3f+Mathf.Sin(px*3f+py*2f)*1.5f;
                float value=249f+plaster+(PeriodicNoise(x,y,size,32,71u)-.5f)*2.5f;
                pixels[y*size+x]=Gray(value);
            }
            return Texture("Subtle plaster",size,pixels);
        }

        static Texture2D FabricTexture()
        {
            const int size=256, weave=8;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                int warp=x%weave, weft=y%weave;
                float value=251f+(warp==1 ? -5f : warp==2 ? 2f : 0f)+(weft==5 ? -4f : weft==6 ? 2f : 0f);
                value+=(PeriodicNoise(x,y,size,8,97u)-.5f)*1.2f;
                pixels[y*size+x]=Gray(value);
            }
            return Texture("Fine fabric weave",size,pixels);
        }

        static Texture2D SkyTexture()
        {
            const int height=128;
            var top=new Color(.50f,.72f,.92f); var haze=new Color(.84f,.92f,.97f); var ground=new Color(.60f,.66f,.60f);
            var pixels=new Color32[4*height];
            for(int y=0;y<height;y++)
            {
                float v=y/(height-1f);
                Color c=v<.34f ? ground : v<.44f ? Color.Lerp(ground,haze,(v-.34f)/.10f) : Color.Lerp(haze,top,Mathf.Clamp01((v-.44f)/.56f));
                for(int x=0;x<4;x++) pixels[y*4+x]=c;
            }
            var texture=new Texture2D(4,height,TextureFormat.RGB24,false,false);
            texture.name="Window sky"; texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Bilinear;
            texture.SetPixels32(pixels); texture.Apply(false,true);
            return texture;
        }

        // Alpha fades from .28 at v=0 (the corner) to 0 at v=1 (away from it).
        static Texture2D CornerShadeTexture()
        {
            const int height=32;
            var pixels=new Color32[4*height];
            for(int y=0;y<height;y++)
            {
                float fade=1f-y/(height-1f);
                var c=new Color32(0,0,0,(byte)Mathf.RoundToInt(fade*fade*.28f*255f));
                for(int x=0;x<4;x++) pixels[y*4+x]=c;
            }
            var texture=new Texture2D(4,height,TextureFormat.RGBA32,false,false);
            texture.name="Corner shade"; texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Bilinear;
            texture.SetPixels32(pixels); texture.Apply(false,true);
            return texture;
        }

        static Material CornerShadeMaterial(Texture texture)
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(shader==null) shader=Shader.Find("Sprites/Default");
            var m=new Material(shader){name="Corner shade"};
            m.color=Color.black;
            if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",Color.black);
            if(m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",texture);
            if(m.HasProperty("_MainTex")) m.SetTexture("_MainTex",texture);
            // Blend state is set through properties rather than keywords so no shader variant is needed in a build.
            if(m.HasProperty("_Surface")) m.SetFloat("_Surface",1);
            if(m.HasProperty("_Blend")) m.SetFloat("_Blend",0);
            if(m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            if(m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            if(m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
            if(m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            if(m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite",0);
            m.SetOverrideTag("RenderType","Transparent");
            m.renderQueue=(int)RenderQueue.Transparent;
            return m;
        }

        public static Material TextMaterial(Font font)
        {
            var material = new Material(font.material);
            var shader = Shader.Find("LoopRoom/Text");
            if (shader != null) material.shader = shader;
            else if (!textShaderWarningLogged)
            {
                Debug.LogWarning("LoopRoom/Text shader was not found; TextMesh will use the font material shader.");
                textShaderWarningLogged = true;
            }
            return material;
        }

        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool hidden = false, Transform parent = null, float smoothness = .23f, float metallic = 0, float emission = 0, bool unlit = false, Texture texture = null, Vector2? textureScale = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color,unlit,smoothness,metallic,emission,texture,textureScale);
            if (hidden)
            {
                go.layer = PrivateLayer;
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        GameObject Detail(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool hidden = false, Transform parent = null, float smoothness = .23f, float metallic = 0, float emission = 0, bool unlit = false, Texture texture = null, Vector2? textureScale = null)
        {
            var go=Shape(name,type,position,scale,color,hidden,parent,smoothness,metallic,emission,unlit,texture,textureScale);
            Object.Destroy(go.GetComponent<Collider>()); return go;
        }

        static void NoShadow(GameObject go)
        {
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        TextMesh Text(string name, string text, Vector3 p, float size, Color color, bool hidden = true, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = p;
            if (hidden) go.layer = PrivateLayer;
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.font = font;
            mesh.fontSize = 80; mesh.characterSize = size; mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center; mesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = TextMaterial(font);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return mesh;
        }

        public void Build(Transform owner, DemoRig rig)
        {
            materialCache.Clear();
            Root = new GameObject("Room").transform; Root.SetParent(owner,false);
            head = rig.View.transform; left = rig.Hands[0]; right = rig.Hands[1];
            font = Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic", "Meiryo", "Arial"}, 80);
            var ink = new Color(.045f,.075f,.09f); var ivory = new Color(.91f,.90f,.85f);
            var wall = new Color(.82f,.81f,.77f); var trim = new Color(.68f,.66f,.61f);
            var wood = new Color(.49f,.32f,.18f); var darkWood = new Color(.29f,.18f,.10f);
            var fabric = new Color(.67f,.70f,.72f); var whiteFabric = new Color(.88f,.87f,.82f);
            var curtain = new Color(.84f,.82f,.76f);
            var metal = new Color(.45f,.47f,.48f); var doorWood = new Color(.55f,.38f,.23f);
            var floorTexture=FloorTexture(); var woodTexture=WoodTexture();
            var wallTexture=WallTexture(); var fabricTexture=FabricTexture();
            // Interior is 3.6m wide (x=±1.8), z=-1..2.925, 2.45m high: a six-to-eight-tatami one-room flat.
            // The player's standing spot (origin) and the reach-zone furniture below keep their earlier positions.
            // One texture repeat represents 2m of floor, .5m of wood, 1m of plaster, or .25m of fabric.
            Shape("Floor",PrimitiveType.Cube,new Vector3(0,-.08f,1),new Vector3(3.9f,.16f,6),wood,smoothness:.28f,texture:floorTexture,textureScale:new Vector2(1.95f,3f));
            Shape("Back wall",PrimitiveType.Cube,new Vector3(0,1.25f,3),new Vector3(3.9f,2.5f,.15f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(3.9f,2.5f));
            // The left wall is cut around the window opening so the outdoor view sits behind real depth.
            Shape("Left wall below window",PrimitiveType.Cube,new Vector3(-1.875f,.5f,1),new Vector3(.15f,1.0f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,1.0f));
            Shape("Left wall above window",PrimitiveType.Cube,new Vector3(-1.875f,2.275f,1),new Vector3(.15f,.45f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,.45f));
            Shape("Left wall front of window",PrimitiveType.Cube,new Vector3(-1.875f,1.525f,.025f),new Vector3(.15f,1.05f,2.05f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(2.05f,1.05f));
            Shape("Left wall behind window",PrimitiveType.Cube,new Vector3(-1.875f,1.525f,2.575f),new Vector3(.15f,1.05f,.85f),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(.85f,1.05f));
            Shape("Right wall",PrimitiveType.Cube,new Vector3(1.875f,1.25f,1),new Vector3(.15f,2.5f,4),wall,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(4,2.5f));
            Detail("Reach rug",PrimitiveType.Cube,new Vector3(0,.007f,.3f),new Vector3(1.0f,.014f,.6f),new Color(.33f,.27f,.22f),smoothness:.08f,texture:fabricTexture,textureScale:new Vector2(4,2.4f));
            Detail("Ceiling",PrimitiveType.Cube,new Vector3(0,2.51f,1),new Vector3(3.8f,.12f,4),ivory,smoothness:.04f,texture:wallTexture,textureScale:new Vector2(3.8f,4));
            Detail("Back baseboard",PrimitiveType.Cube,new Vector3(0,.12f,2.88f),new Vector3(3.6f,.20f,.09f),trim,smoothness:.10f);
            Detail("Left baseboard",PrimitiveType.Cube,new Vector3(-1.755f,.12f,.9625f),new Vector3(.09f,.20f,3.925f),trim,smoothness:.10f);
            Detail("Right baseboard",PrimitiveType.Cube,new Vector3(1.755f,.12f,.9625f),new Vector3(.09f,.20f,3.925f),trim,smoothness:.10f);
            BuildCornerShade();

            // Small desk at the established interaction position. All ordinary furniture is colliderless.
            Detail("Desk",PrimitiveType.Cube,new Vector3(0,.80f,.52f),new Vector3(1.5f,.10f,.7f),wood,smoothness:.32f,texture:woodTexture,textureScale:new Vector2(3,1.4f));
            foreach (var x in new[]{-.63f,.63f}) foreach (var z in new[]{.24f,.80f})
                Detail("Desk leg",PrimitiveType.Cube,new Vector3(x,.40f,z),new Vector3(.09f,.80f,.09f),darkWood,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(.18f,1.6f));
            Detail("Clock shelf",PrimitiveType.Cube,new Vector3(0,1.18f,.75f),new Vector3(.52f,.04f,.22f),wood,smoothness:.30f,texture:woodTexture,textureScale:new Vector2(1.04f,.44f));
            Detail("Clock shelf L",PrimitiveType.Cube,new Vector3(-.20f,1.01f,.78f),new Vector3(.05f,.34f,.08f),darkWood,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(.1f,.68f));
            Detail("Clock shelf R",PrimitiveType.Cube,new Vector3(.20f,1.01f,.78f),new Vector3(.05f,.34f,.08f),darkWood,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(.1f,.68f));
            Shape("Instruction card",PrimitiveType.Cube,new Vector3(0,1.06f,.67f),new Vector3(.41f,.24f,.024f),ivory,true);
            Card = Text("Instructions","この部屋から\n無事に脱出しろ",new Vector3(0,1.06f,.65f),.017f,ink);
            Detail("Clock body",PrimitiveType.Cube,new Vector3(0,1.41f,.75f),new Vector3(.32f,.20f,.12f),ink,smoothness:.26f,emission:.35f);
            Clock = Text("Clock","00 : 00",new Vector3(0,1.41f,.682f),.026f,ivory);
            Detail("Clock feet L",PrimitiveType.Cube,new Vector3(-.1f,1.27f,.75f),new Vector3(.03f,.12f,.06f),metal,smoothness:.42f,metallic:.55f);
            Detail("Clock feet R",PrimitiveType.Cube,new Vector3(.1f,1.27f,.75f),new Vector3(.03f,.12f,.06f),metal,smoothness:.42f,metallic:.55f);
            Detail("Clock top button",PrimitiveType.Cylinder,new Vector3(0,1.53f,.75f),new Vector3(.035f,.025f,.035f),metal,smoothness:.42f,metallic:.55f);
            var shield = Shape("Shield handle",PrimitiveType.Cube,new Vector3(-.32f,1.01f,.36f),new Vector3(.14f,.08f,.08f),new Color(.18f,.62f,.65f),true);
            ShieldHandle = shield.AddComponent<XRSimpleInteractable>();
            Text("Shield mark","遮蔽",new Vector3(-.32f,.9f,.30f),.014f,ivory);
            var exit = Shape("Exit handle",PrimitiveType.Cube,new Vector3(.32f,1.01f,.36f),new Vector3(.14f,.08f,.08f),metal,true,smoothness:.50f,metallic:.75f);
            ExitHandle = exit.AddComponent<XRSimpleInteractable>();
            ExitLabel = Text("Exit mark","施錠中",new Vector3(.32f,.9f,.30f),.014f,ivory);
            ExitLamp = Shape("Exit lamp",PrimitiveType.Sphere,new Vector3(.32f,1.12f,.40f),Vector3.one*.04f,Color.red,true,emission:3f).GetComponent<Renderer>();
            Barrier = Shape("Shield",PrimitiveType.Cube,new Vector3(0,.45f,1.08f),new Vector3(1.85f,1.6f,.08f),ink,true).transform;

            // The moving door remains private because its state is an escape clue.
            Door = new GameObject("Entry door").transform; Door.SetParent(Root,false);
            // Door is 0.8m wide and 2.0m high; its origin is the slab centre (y=1.0), so the closed slab sits on the floor.
            Door.localPosition=new Vector3(.5f,1.0f,2.86f); Door.gameObject.layer=PrivateLayer;
            Detail("Door slab",PrimitiveType.Cube,Vector3.zero,new Vector3(.8f,2.0f,.09f),doorWood,true,Door,smoothness:.27f,texture:woodTexture,textureScale:new Vector2(1.6f,4.0f));
            Detail("Door upper panel",PrimitiveType.Cube,new Vector3(0,.38f,-.055f),new Vector3(.58f,.66f,.025f),darkWood,true,Door,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.32f));
            Detail("Door lower panel",PrimitiveType.Cube,new Vector3(0,-.45f,-.055f),new Vector3(.58f,.62f,.025f),darkWood,true,Door,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.24f));
            Detail("Door knob",PrimitiveType.Sphere,new Vector3(-.29f,0,-.09f),Vector3.one*.085f,metal,true,Door,smoothness:.55f,metallic:.65f);
            Detail("Door lintel",PrimitiveType.Cube,new Vector3(.5f,2.04f,2.865f),new Vector3(1.0f,.08f,.12f),trim,smoothness:.10f);
            Detail("Door frame L",PrimitiveType.Cube,new Vector3(.05f,1.04f,2.865f),new Vector3(.10f,2.08f,.12f),trim,smoothness:.10f);
            Detail("Door frame R",PrimitiveType.Cube,new Vector3(.95f,1.04f,2.865f),new Vector3(.10f,2.08f,.12f),trim,smoothness:.10f);

            // The spectator sees a permanently closed copy, so the real door's motion remains private.
            var publicDoor = new GameObject("Public closed door").transform; publicDoor.SetParent(Root,false);
            publicDoor.localPosition=new Vector3(.5f,1.0f,2.86f); publicDoor.gameObject.layer=PublicLayer;
            var publicDoorParts = new[]
            {
                Detail("Public door slab",PrimitiveType.Cube,Vector3.zero,new Vector3(.8f,2.0f,.09f),doorWood,parent:publicDoor,smoothness:.27f,texture:woodTexture,textureScale:new Vector2(1.6f,4.0f)),
                Detail("Public door upper panel",PrimitiveType.Cube,new Vector3(0,.38f,-.055f),new Vector3(.58f,.66f,.025f),darkWood,parent:publicDoor,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.32f)),
                Detail("Public door lower panel",PrimitiveType.Cube,new Vector3(0,-.45f,-.055f),new Vector3(.58f,.62f,.025f),darkWood,parent:publicDoor,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(1.16f,1.24f)),
                Detail("Public door knob",PrimitiveType.Sphere,new Vector3(-.29f,0,-.09f),Vector3.one*.085f,metal,parent:publicDoor,smoothness:.55f,metallic:.65f)
            };
            foreach (var publicDoorPart in publicDoorParts)
            {
                publicDoorPart.layer=PublicLayer;
                var renderer=publicDoorPart.GetComponent<Renderer>();
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }

            // The window is a real opening in the left wall; a sky gradient and a few distant slate buildings sit outside it.
            var skyTexture=SkyTexture();
            Outdoor("Window sky",PrimitiveType.Quad,new Vector3(-8f,1.5f,3f),new Vector3(16,8,1),Color.white,Quaternion.Euler(0,-90,0),skyTexture);
            var buildingColor=new Color(.64f,.71f,.79f);
            Outdoor("Distant building 1",PrimitiveType.Cube,new Vector3(-6.4f,-.45f,1.2f),new Vector3(1f,3.1f,1.8f),buildingColor);
            Outdoor("Distant building 2",PrimitiveType.Cube,new Vector3(-6.0f,.2f,3.6f),new Vector3(1f,4.4f,2.0f),buildingColor);
            Outdoor("Distant building 3",PrimitiveType.Cube,new Vector3(-6.6f,-.3f,5.8f),new Vector3(1f,3.4f,1.5f),buildingColor);
            Outdoor("Distant building 4",PrimitiveType.Cube,new Vector3(-6.2f,.45f,7.8f),new Vector3(1f,4.9f,2.2f),buildingColor);
            Detail("Window top",PrimitiveType.Cube,new Vector3(-1.76f,2.07f,1.6f),new Vector3(.08f,.10f,1.30f),ivory,smoothness:.10f);
            Detail("Window bottom",PrimitiveType.Cube,new Vector3(-1.74f,1.0f,1.6f),new Vector3(.12f,.10f,1.30f),ivory,smoothness:.10f);
            Detail("Window front frame",PrimitiveType.Cube,new Vector3(-1.76f,1.535f,1.05f),new Vector3(.08f,1.14f,.10f),ivory,smoothness:.10f);
            Detail("Window back frame",PrimitiveType.Cube,new Vector3(-1.76f,1.535f,2.15f),new Vector3(.08f,1.14f,.10f),ivory,smoothness:.10f);
            Detail("Window mullion",PrimitiveType.Cube,new Vector3(-1.77f,1.525f,1.6f),new Vector3(.05f,1.0f,.045f),ivory,smoothness:.10f);
            Detail("Curtain rod",PrimitiveType.Cube,new Vector3(-1.70f,2.25f,1.6f),new Vector3(.05f,.05f,2.0f),metal,smoothness:.45f,metallic:.45f);
            // Each curtain is three slightly offset slabs so its folds catch the light unevenly.
            for(int i=0;i<3;i++)
            {
                float x=i==1 ? -1.665f : -1.70f;
                Detail("Curtain front fold",PrimitiveType.Cube,new Vector3(x,1.55f,.683f+i*.127f),new Vector3(.06f,1.3f,.12f),curtain,smoothness:.05f,texture:fabricTexture,textureScale:new Vector2(.48f,5.2f));
                Detail("Curtain back fold",PrimitiveType.Cube,new Vector3(x,1.55f,2.263f+i*.127f),new Vector3(.06f,1.3f,.12f),curtain,smoothness:.05f,texture:fabricTexture,textureScale:new Vector2(.48f,5.2f));
            }

            var chair = new GameObject("Desk chair").transform; chair.SetParent(Root,false);
            chair.localPosition=new Vector3(-1.3f,0,.45f); chair.localRotation=Quaternion.Euler(0,8,0);
            Detail("Chair seat",PrimitiveType.Cube,new Vector3(0,.48f,0),new Vector3(.58f,.10f,.56f),wood,parent:chair,smoothness:.25f,texture:woodTexture,textureScale:new Vector2(1.16f,1.12f));
            Detail("Chair back",PrimitiveType.Cube,new Vector3(0,.86f,.23f),new Vector3(.58f,.68f,.09f),wood,parent:chair,smoothness:.25f,texture:woodTexture,textureScale:new Vector2(1.16f,1.36f));
            foreach (var x in new[]{-.23f,.23f}) foreach (var z in new[]{-.20f,.20f})
                Detail("Chair leg",PrimitiveType.Cube,new Vector3(x,.23f,z),new Vector3(.07f,.46f,.07f),darkWood,parent:chair,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(.14f,.92f));

            Detail("Bed base",PrimitiveType.Cube,new Vector3(-1.24f,.25f,1.88f),new Vector3(1.0f,.38f,1.90f),wood,smoothness:.20f,texture:woodTexture,textureScale:new Vector2(2.0f,3.8f));
            Detail("Bed mattress",PrimitiveType.Cube,new Vector3(-1.24f,.49f,1.88f),new Vector3(.94f,.22f,1.78f),whiteFabric,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(3.76f,7.12f));
            Detail("Bed blanket",PrimitiveType.Cube,new Vector3(-1.24f,.62f,1.62f),new Vector3(.96f,.055f,1.10f),fabric,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(3.84f,4.4f));
            Detail("Bed pillow",PrimitiveType.Cube,new Vector3(-1.24f,.66f,2.52f),new Vector3(.60f,.12f,.34f),ivory,smoothness:.03f,texture:fabricTexture,textureScale:new Vector2(2.4f,1.36f));
            Detail("Bed headboard",PrimitiveType.Cube,new Vector3(-1.24f,.72f,2.87f),new Vector3(1.04f,1.00f,.10f),darkWood,smoothness:.18f,texture:woodTexture,textureScale:new Vector2(2.08f,2));

            Detail("Light switch plate",PrimitiveType.Cube,new Vector3(1.38f,1.20f,2.912f),new Vector3(.18f,.26f,.025f),ivory,smoothness:.12f);
            Detail("Light switch",PrimitiveType.Cube,new Vector3(1.38f,1.21f,2.89f),new Vector3(.07f,.12f,.025f),trim,smoothness:.12f);
            NoShadow(Detail("Outlet plate",PrimitiveType.Cube,new Vector3(-.55f,.32f,2.914f),new Vector3(.09f,.14f,.02f),ivory,smoothness:.12f));
            foreach (var y in new[]{.345f,.295f})
                NoShadow(Detail("Outlet slot",PrimitiveType.Cube,new Vector3(-.55f,y,2.902f),new Vector3(.05f,.012f,.01f),ink,smoothness:.05f));
            Detail("Picture frame",PrimitiveType.Cube,new Vector3(-1.15f,1.78f,2.9075f),new Vector3(.70f,.56f,.035f),darkWood,smoothness:.22f,texture:woodTexture,textureScale:new Vector2(1.4f,1.12f));
            Detail("Picture",PrimitiveType.Cube,new Vector3(-1.15f,1.78f,2.89f),new Vector3(.58f,.44f,.025f),new Color(.53f,.67f,.61f),smoothness:.06f);

            // Colour box against the right wall, plant in the back corner: both outside the reach zone.
            var shelfScale=new Vector2(.6f,1.2f);
            Detail("Shelf left board",PrimitiveType.Cube,new Vector3(1.64f,.525f,1.345f),new Vector3(.32f,1.05f,.03f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
            Detail("Shelf right board",PrimitiveType.Cube,new Vector3(1.64f,.525f,2.155f),new Vector3(.32f,1.05f,.03f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
            Detail("Shelf top",PrimitiveType.Cube,new Vector3(1.64f,1.035f,1.75f),new Vector3(.32f,.03f,.84f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
            Detail("Shelf middle",PrimitiveType.Cube,new Vector3(1.64f,.55f,1.75f),new Vector3(.32f,.03f,.78f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
            Detail("Shelf bottom",PrimitiveType.Cube,new Vector3(1.64f,.045f,1.75f),new Vector3(.32f,.03f,.78f),wood,smoothness:.25f,texture:woodTexture,textureScale:shelfScale);
            Detail("Shelf back",PrimitiveType.Cube,new Vector3(1.788f,.525f,1.75f),new Vector3(.012f,1.05f,.84f),darkWood,smoothness:.15f);
            NoShadow(Detail("Book 1",PrimitiveType.Cube,new Vector3(1.62f,.695f,1.56f),new Vector3(.22f,.26f,.045f),new Color(.62f,.26f,.22f),smoothness:.18f));
            NoShadow(Detail("Book 2",PrimitiveType.Cube,new Vector3(1.62f,.67f,1.61f),new Vector3(.22f,.21f,.04f),new Color(.30f,.40f,.52f),smoothness:.18f));
            NoShadow(Detail("Book 3",PrimitiveType.Cube,new Vector3(1.62f,.69f,1.66f),new Vector3(.22f,.25f,.05f),new Color(.78f,.64f,.34f),smoothness:.18f));
            NoShadow(Detail("Book 4",PrimitiveType.Cube,new Vector3(1.62f,.665f,1.71f),new Vector3(.22f,.20f,.035f),new Color(.34f,.48f,.38f),smoothness:.18f));
            Detail("Plant pot",PrimitiveType.Cylinder,new Vector3(1.5f,.14f,2.6f),new Vector3(.30f,.14f,.30f),new Color(.62f,.36f,.26f),smoothness:.15f);
            Detail("Plant leaves 1",PrimitiveType.Sphere,new Vector3(1.5f,.62f,2.6f),new Vector3(.34f,.56f,.34f),new Color(.30f,.48f,.28f),smoothness:.12f);
            Detail("Plant leaves 2",PrimitiveType.Sphere,new Vector3(1.43f,.84f,2.62f),new Vector3(.26f,.46f,.26f),new Color(.36f,.54f,.30f),smoothness:.12f);
            Detail("Plant leaves 3",PrimitiveType.Sphere,new Vector3(1.58f,.78f,2.55f),new Vector3(.24f,.40f,.24f),new Color(.26f,.43f,.26f),smoothness:.12f);

            Enemy = new GameObject("Enemy").transform; Enemy.SetParent(Root,false);
            Shape("Coat",PrimitiveType.Capsule,new Vector3(0,1.0f,0),new Vector3(.38f,.65f,.28f),ink,true,Enemy);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.68f,0),Vector3.one*.26f,ink,true,Enemy);
            Shape("Visor",PrimitiveType.Cube,new Vector3(0,1.70f,-.13f),new Vector3(.20f,.035f,.025f),new Color(.8f,.19f,.10f),true,Enemy,emission:2f);
            Shape("Weapon",PrimitiveType.Cube,new Vector3(-.13f,1.36f,-.24f),new Vector3(.09f,.10f,.35f),ink,true,Enemy);
            Detail("Left shoulder",PrimitiveType.Sphere,new Vector3(-.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
            Detail("Right shoulder",PrimitiveType.Sphere,new Vector3(.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
            Detail("Hat brim",PrimitiveType.Cube,new Vector3(0,1.83f,-.015f),new Vector3(.43f,.035f,.34f),ink,true,Enemy,smoothness:.08f);
            // Flat canopy disc plus a shallow dome shade, mounted flush on the 2.45m ceiling.
            Detail("Ceiling light canopy",PrimitiveType.Cylinder,new Vector3(0,2.435f,.2f),new Vector3(.46f,.012f,.46f),ivory,smoothness:.22f);
            Detail("Ceiling light shade",PrimitiveType.Sphere,new Vector3(0,2.44f,.2f),new Vector3(.40f,.28f,.40f),new Color(1,.93f,.82f),smoothness:.18f,emission:.55f);
            var light = new GameObject("Ceiling light").AddComponent<Light>();
            light.transform.SetParent(Root,false); light.transform.localPosition = new Vector3(0,2.25f,.2f);
            light.transform.localRotation=Quaternion.Euler(90,0,0); light.type=LightType.Spot;
            light.spotAngle=140; light.innerSpotAngle=110;
            light.range=6; light.intensity=1.6f; light.color=new Color(1,.93f,.82f);
            light.shadows=LightShadows.Soft; light.shadowResolution=LightShadowResolution.Medium;
            var ceilingFill = new GameObject("Ceiling light fill").AddComponent<Light>();
            ceilingFill.transform.SetParent(Root,false); ceilingFill.transform.localPosition=new Vector3(0,2.25f,.2f);
            ceilingFill.type=LightType.Point; ceilingFill.range=6; ceilingFill.intensity=.4f;
            ceilingFill.color=new Color(1,.93f,.82f); ceilingFill.shadows=LightShadows.None;
            var fill = new GameObject("Window daylight").AddComponent<Light>();
            fill.transform.SetParent(Root,false); fill.transform.localPosition=new Vector3(-1.65f,1.55f,1.6f);
            fill.transform.localRotation=Quaternion.Euler(0,90,0); fill.type=LightType.Spot; fill.spotAngle=105;
            fill.range=6; fill.intensity=1.0f; fill.color=new Color(.78f,.88f,1f); fill.shadows=LightShadows.None;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.55f,.57f,.60f);
            RenderSettings.ambientEquatorColor=new Color(.44f,.43f,.40f);
            RenderSettings.ambientGroundColor=new Color(.30f,.27f,.24f);
            RenderSettings.fog=false;
            Blackout = Shape("Eye blackout",PrimitiveType.Quad,new Vector3(0,0,.05f),new Vector3(1,1,1),Color.black,true,head);
            Blackout.GetComponent<Renderer>().sharedMaterial=Material(Color.black,true);
            Object.Destroy(Blackout.GetComponent<Collider>()); Blackout.SetActive(false);
            Sound = new GameObject("Room audio").AddComponent<AudioSource>(); Sound.transform.SetParent(Root,false);
            Sound.spatialBlend=0; Sound.volume=.25f; Sound.playOnAwake=false;
            Chime=Tone(660,.28f,false); Shot=Tone(80,.09f,true); Latch=Tone(170,.06f,false); Open=Tone(880,.16f,false);
            BuildSpectator();
            BuildPostProcessing(rig.View);
        }

        void Outdoor(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null, Texture texture = null)
        {
            var go=Detail(name,type,position,scale,color,smoothness:.02f,unlit:true,texture:texture);
            if(rotation.HasValue) go.transform.localRotation=rotation.Value;
            var renderer=go.GetComponent<Renderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        }

        // Cheap stand-in for ambient occlusion: dark transparent gradient strips along floor, ceiling and wall corners.
        void BuildCornerShade()
        {
            var material=CornerShadeMaterial(CornerShadeTexture());
            const float width=.5f, half=1.8f, backZ=2.925f, frontZ=-1f, ceilingY=2.45f, wallHeight=2.45f, lift=.004f;
            float sideLength=backZ-frontZ, sideCenter=(backZ+frontZ)/2, midHeight=wallHeight/2;
            var up=Vector3.up; var down=Vector3.down;
            Band(material,"Floor shade left",new Vector3(-half,lift,sideCenter),up,Vector3.right,sideLength,width);
            Band(material,"Floor shade right",new Vector3(half,lift,sideCenter),up,Vector3.left,sideLength,width);
            Band(material,"Floor shade back",new Vector3(0,lift,backZ),up,Vector3.back,2*half,width);
            Band(material,"Ceiling shade left",new Vector3(-half,ceilingY-lift,sideCenter),down,Vector3.right,sideLength,width);
            Band(material,"Ceiling shade right",new Vector3(half,ceilingY-lift,sideCenter),down,Vector3.left,sideLength,width);
            Band(material,"Ceiling shade back",new Vector3(0,ceilingY-lift,backZ),down,Vector3.back,2*half,width);
            Band(material,"Corner shade back left",new Vector3(-half,midHeight,backZ-lift),Vector3.back,Vector3.right,wallHeight,width);
            Band(material,"Corner shade back right",new Vector3(half,midHeight,backZ-lift),Vector3.back,Vector3.left,wallHeight,width);
            Band(material,"Corner shade left wall",new Vector3(-half+lift,midHeight,backZ),Vector3.right,Vector3.back,wallHeight,width);
            Band(material,"Corner shade right wall",new Vector3(half-lift,midHeight,backZ),Vector3.left,Vector3.back,wallHeight,width);
        }

        // edge is the strip's centre on the corner line; the gradient runs from there along fade, facing normal.
        void Band(Material material, string name, Vector3 edge, Vector3 normal, Vector3 fade, float length, float width)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name=name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(Root,false);
            go.transform.localPosition=edge+fade*(width/2);
            go.transform.localRotation=Quaternion.LookRotation(-normal,fade);
            go.transform.localScale=new Vector3(length,width,1);
            var renderer=go.GetComponent<Renderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        }

        void BuildSpectator()
        {
            var go=new GameObject("Public spectator camera"); go.transform.SetParent(Root,false);
            Spectator=go.AddComponent<Camera>(); Spectator.stereoTargetEye=StereoTargetEyeMask.None;
            Spectator.cullingMask=~(1<<PrivateLayer); Spectator.depth=10;
            // URP ignores stereoTargetEye; without this the spectator view (depth 10) is also rendered into the HMD.
            Spectator.GetUniversalAdditionalCameraData().allowXRRendering=false;
            // Stands behind the open end of the room (z<-1) so the whole 3.6m x 4m interior, window and door wall included, is in frame.
            Spectator.transform.position=new Vector3(-1.2f,2.0f,-2.2f);
            Spectator.transform.LookAt(new Vector3(0,1.0f,1.2f)); Spectator.fieldOfView=63;
            Spectator.clearFlags=CameraClearFlags.SolidColor; Spectator.backgroundColor=new Color(.38f,.50f,.58f);
            publicHead=Shape("Public head",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.2f,new Color(.3f,.8f,.8f));
            publicLeft=Shape("Public left hand",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.08f,new Color(.3f,.8f,.8f));
            publicRight=Shape("Public right hand",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.08f,new Color(.3f,.8f,.8f));
            publicHead.layer=publicLeft.layer=publicRight.layer=9;
            foreach (var publicPart in new[] { publicHead, publicLeft, publicRight })
            {
                var renderer=publicPart.GetComponent<Renderer>();
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            Object.Destroy(publicHead.GetComponent<Collider>()); Object.Destroy(publicLeft.GetComponent<Collider>()); Object.Destroy(publicRight.GetComponent<Collider>());
        }

        void BuildPostProcessing(Camera view)
        {
            var go=new GameObject("Room atmosphere"); go.transform.SetParent(Root,false);
            var volume=go.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=1;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>(); profile.name="Room atmosphere profile"; volume.profile=profile;
            var bloom=profile.Add<Bloom>(true); bloom.threshold.value=1.25f; bloom.intensity.value=.06f;
            bloom.scatter.value=.30f; bloom.filter.value=BloomFilterMode.Dual;
            bloom.downscale.value=BloomDownscaleMode.Quarter; bloom.maxIterations.value=4;
            var tonemapping=profile.Add<Tonemapping>(true); tonemapping.mode.value=TonemappingMode.Neutral;
            var color=profile.Add<ColorAdjustments>(true); color.saturation.value=0; color.postExposure.value=.08f;
            view.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            Spectator.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        }

        public void UpdatePublic(bool vr)
        {
            Spectator.enabled=vr;
            publicHead.SetActive(vr); publicLeft.SetActive(vr); publicRight.SetActive(vr);
            publicHead.transform.position=head.position;
            var lamp=ExitLamp.material;
            if(lamp.HasProperty("_EmissionColor")) { var glow=lamp.color*3f; glow.a=1; lamp.SetColor("_EmissionColor",glow); }
            // Quantize hands to avoid exposing exact target positions.
            publicLeft.transform.position=Coarse(left.position); publicRight.transform.position=Coarse(right.position);
        }
        Vector3 Coarse(Vector3 p) => new Vector3(Mathf.Round(p.x*3)/3,Mathf.Round(p.y*3)/3,Mathf.Round(p.z*3)/3);

        public static AudioClip Tone(float frequency,float duration,bool noise)
        {
            const int rate=22050; var samples=new float[(int)(duration*rate)]; uint seed=983;
            for(int i=0;i<samples.Length;i++)
            {
                seed=1664525*seed+1013904223;
                float wave=noise ? ((seed>>8)/(float)0xFFFFFF)*2-1 : Mathf.Sin(2*Mathf.PI*frequency*i/rate);
                samples[i]=wave*Mathf.Exp(-5f*i/samples.Length)*.45f;
            }
            var clip=AudioClip.Create(noise?"Impact":"Tone",samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
        }
    }
}
