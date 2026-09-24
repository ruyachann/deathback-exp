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
        Font font;
        Transform head, left, right;
        GameObject publicHead, publicLeft, publicRight;

        public static Material Material(Color color, bool unlit = false, float smoothness = .23f, float metallic = 0, float emission = 0)
        {
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            var m = new Material(shader); m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (emission>0 && m.HasProperty("_EmissionColor"))
            {
                var glow=color*emission; glow.a=1;
                m.SetColor("_EmissionColor",glow); m.EnableKeyword("_EMISSION");
            }
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

        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool hidden = false, Transform parent = null, float smoothness = .23f, float metallic = 0, float emission = 0, bool unlit = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color,unlit,smoothness,metallic,emission);
            if (hidden)
            {
                go.layer = PrivateLayer;
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        GameObject Detail(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool hidden = false, Transform parent = null, float smoothness = .23f, float metallic = 0, float emission = 0, bool unlit = false)
        {
            var go=Shape(name,type,position,scale,color,hidden,parent,smoothness,metallic,emission,unlit);
            Object.Destroy(go.GetComponent<Collider>()); return go;
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
            Root = new GameObject("Room").transform; Root.SetParent(owner,false);
            head = rig.View.transform; left = rig.Hands[0]; right = rig.Hands[1];
            font = Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic", "Meiryo", "Arial"}, 80);
            var ink = new Color(.045f,.075f,.09f); var ivory = new Color(.91f,.90f,.85f);
            var wall = new Color(.82f,.81f,.77f); var trim = new Color(.68f,.66f,.61f);
            var wood = new Color(.49f,.32f,.18f); var darkWood = new Color(.29f,.18f,.10f);
            var fabric = new Color(.67f,.70f,.72f); var whiteFabric = new Color(.88f,.87f,.82f);
            var curtain = new Color(.84f,.82f,.76f); var sky = new Color(.64f,.80f,.91f);
            var metal = new Color(.45f,.47f,.48f); var doorWood = new Color(.55f,.38f,.23f);
            Shape("Floor",PrimitiveType.Cube,new Vector3(0,-.08f,1),new Vector3(5,.16f,6),wood,smoothness:.28f);
            Shape("Back wall",PrimitiveType.Cube,new Vector3(0,1.6f,3),new Vector3(5,3.2f,.15f),wall,smoothness:.04f);
            Shape("Left wall",PrimitiveType.Cube,new Vector3(-2.5f,1.6f,1),new Vector3(.15f,3.2f,4),wall,smoothness:.04f);
            Shape("Right wall",PrimitiveType.Cube,new Vector3(2.5f,1.6f,1),new Vector3(.15f,3.2f,4),wall,smoothness:.04f);
            for (int i=0;i<8;i++) Detail("Floor board seam",PrimitiveType.Cube,new Vector3(0,.006f,-.9f+i*.52f),new Vector3(4.8f,.009f,.012f),darkWood,smoothness:.06f);
            Detail("Reach rug",PrimitiveType.Cube,new Vector3(0,.007f,.3f),new Vector3(1.0f,.014f,.6f),new Color(.33f,.27f,.22f),smoothness:.08f);
            Detail("Ceiling",PrimitiveType.Cube,new Vector3(0,3.22f,1),new Vector3(5,.12f,4),ivory,smoothness:.04f);
            Detail("Back baseboard",PrimitiveType.Cube,new Vector3(0,.12f,2.84f),new Vector3(4.86f,.20f,.09f),trim,smoothness:.10f);
            Detail("Left baseboard",PrimitiveType.Cube,new Vector3(-2.34f,.12f,1),new Vector3(.09f,.20f,3.86f),trim,smoothness:.10f);
            Detail("Right baseboard",PrimitiveType.Cube,new Vector3(2.34f,.12f,1),new Vector3(.09f,.20f,3.86f),trim,smoothness:.10f);

            // Small desk at the established interaction position. All ordinary furniture is colliderless.
            Detail("Desk",PrimitiveType.Cube,new Vector3(0,.80f,.52f),new Vector3(1.5f,.10f,.7f),wood,smoothness:.32f);
            foreach (var x in new[]{-.63f,.63f}) foreach (var z in new[]{.24f,.80f})
                Detail("Desk leg",PrimitiveType.Cube,new Vector3(x,.40f,z),new Vector3(.09f,.80f,.09f),darkWood,smoothness:.20f);
            Detail("Clock shelf",PrimitiveType.Cube,new Vector3(0,1.18f,.75f),new Vector3(.52f,.04f,.22f),wood,smoothness:.30f);
            Detail("Clock shelf L",PrimitiveType.Cube,new Vector3(-.20f,1.01f,.78f),new Vector3(.05f,.34f,.08f),darkWood,smoothness:.18f);
            Detail("Clock shelf R",PrimitiveType.Cube,new Vector3(.20f,1.01f,.78f),new Vector3(.05f,.34f,.08f),darkWood,smoothness:.18f);
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
            Door.localPosition=new Vector3(.8f,1.18f,2.86f); Door.gameObject.layer=PrivateLayer;
            Detail("Door slab",PrimitiveType.Cube,Vector3.zero,new Vector3(1,2.36f,.09f),doorWood,true,Door,smoothness:.27f);
            Detail("Door upper panel",PrimitiveType.Cube,new Vector3(0,.34f,-.055f),new Vector3(.72f,.72f,.025f),darkWood,true,Door,smoothness:.20f);
            Detail("Door lower panel",PrimitiveType.Cube,new Vector3(0,-.55f,-.055f),new Vector3(.72f,.62f,.025f),darkWood,true,Door,smoothness:.20f);
            Detail("Door knob",PrimitiveType.Sphere,new Vector3(-.34f,0,-.09f),Vector3.one*.095f,metal,true,Door,smoothness:.55f,metallic:.65f);
            Detail("Door lintel",PrimitiveType.Cube,new Vector3(.8f,2.41f,2.82f),new Vector3(1.18f,.08f,.12f),trim,smoothness:.10f);
            Detail("Door frame L",PrimitiveType.Cube,new Vector3(.24f,1.22f,2.78f),new Vector3(.10f,2.50f,.12f),trim,smoothness:.10f);
            Detail("Door frame R",PrimitiveType.Cube,new Vector3(1.36f,1.22f,2.78f),new Vector3(.10f,2.50f,.12f),trim,smoothness:.10f);

            // The spectator sees a permanently closed copy, so the real door's motion remains private.
            var publicDoor = new GameObject("Public closed door").transform; publicDoor.SetParent(Root,false);
            publicDoor.localPosition=new Vector3(.8f,1.18f,2.86f); publicDoor.gameObject.layer=PublicLayer;
            var publicDoorParts = new[]
            {
                Detail("Public door slab",PrimitiveType.Cube,Vector3.zero,new Vector3(1,2.36f,.09f),doorWood,parent:publicDoor,smoothness:.27f),
                Detail("Public door upper panel",PrimitiveType.Cube,new Vector3(0,.34f,-.055f),new Vector3(.72f,.72f,.025f),darkWood,parent:publicDoor,smoothness:.20f),
                Detail("Public door lower panel",PrimitiveType.Cube,new Vector3(0,-.55f,-.055f),new Vector3(.72f,.62f,.025f),darkWood,parent:publicDoor,smoothness:.20f),
                Detail("Public door knob",PrimitiveType.Sphere,new Vector3(-.34f,0,-.09f),Vector3.one*.095f,metal,parent:publicDoor,smoothness:.55f,metallic:.65f)
            };
            foreach (var publicDoorPart in publicDoorParts)
            {
                publicDoorPart.layer=PublicLayer;
                var renderer=publicDoorPart.GetComponent<Renderer>();
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }

            // Open curtains and a plain bright exterior keep the room familiar rather than ominous.
            Detail("Window daylight",PrimitiveType.Cube,new Vector3(-2.40f,1.88f,1.42f),new Vector3(.025f,1.30f,1.42f),sky,smoothness:.02f,emission:.20f,unlit:true);
            Detail("Window top",PrimitiveType.Cube,new Vector3(-2.36f,2.57f,1.42f),new Vector3(.08f,.10f,1.58f),ivory,smoothness:.10f);
            Detail("Window bottom",PrimitiveType.Cube,new Vector3(-2.36f,1.19f,1.42f),new Vector3(.08f,.10f,1.58f),ivory,smoothness:.10f);
            Detail("Window front frame",PrimitiveType.Cube,new Vector3(-2.36f,1.88f,.59f),new Vector3(.08f,1.48f,.10f),ivory,smoothness:.10f);
            Detail("Window back frame",PrimitiveType.Cube,new Vector3(-2.36f,1.88f,2.25f),new Vector3(.08f,1.48f,.10f),ivory,smoothness:.10f);
            Detail("Window mullion",PrimitiveType.Cube,new Vector3(-2.35f,1.88f,1.42f),new Vector3(.09f,1.28f,.055f),ivory,smoothness:.10f);
            Detail("Curtain rod",PrimitiveType.Cube,new Vector3(-2.29f,2.67f,1.42f),new Vector3(.07f,.06f,2.02f),metal,smoothness:.45f,metallic:.45f);
            Detail("Open curtain front",PrimitiveType.Cube,new Vector3(-2.28f,1.86f,.40f),new Vector3(.09f,1.62f,.28f),curtain,smoothness:.05f);
            Detail("Open curtain back",PrimitiveType.Cube,new Vector3(-2.28f,1.86f,2.44f),new Vector3(.09f,1.62f,.28f),curtain,smoothness:.05f);

            var chair = new GameObject("Desk chair").transform; chair.SetParent(Root,false);
            chair.localPosition=new Vector3(-1.15f,0,.64f); chair.localRotation=Quaternion.Euler(0,8,0);
            Detail("Chair seat",PrimitiveType.Cube,new Vector3(0,.48f,0),new Vector3(.58f,.10f,.56f),wood,parent:chair,smoothness:.25f);
            Detail("Chair back",PrimitiveType.Cube,new Vector3(0,.86f,.23f),new Vector3(.58f,.68f,.09f),wood,parent:chair,smoothness:.25f);
            foreach (var x in new[]{-.23f,.23f}) foreach (var z in new[]{-.20f,.20f})
                Detail("Chair leg",PrimitiveType.Cube,new Vector3(x,.23f,z),new Vector3(.07f,.46f,.07f),darkWood,parent:chair,smoothness:.18f);

            Detail("Bed base",PrimitiveType.Cube,new Vector3(-1.72f,.25f,1.82f),new Vector3(1.12f,.38f,1.78f),wood,smoothness:.20f);
            Detail("Bed mattress",PrimitiveType.Cube,new Vector3(-1.72f,.49f,1.82f),new Vector3(1.06f,.22f,1.66f),whiteFabric,smoothness:.03f);
            Detail("Bed blanket",PrimitiveType.Cube,new Vector3(-1.72f,.62f,1.56f),new Vector3(1.08f,.055f,1.02f),fabric,smoothness:.03f);
            Detail("Bed pillow",PrimitiveType.Cube,new Vector3(-1.72f,.66f,2.42f),new Vector3(.72f,.12f,.34f),ivory,smoothness:.03f);
            Detail("Bed headboard",PrimitiveType.Cube,new Vector3(-1.72f,.72f,2.72f),new Vector3(1.16f,1.00f,.10f),darkWood,smoothness:.18f);

            Detail("Light switch plate",PrimitiveType.Cube,new Vector3(1.61f,1.30f,2.88f),new Vector3(.18f,.26f,.025f),ivory,smoothness:.12f);
            Detail("Light switch",PrimitiveType.Cube,new Vector3(1.61f,1.31f,2.85f),new Vector3(.07f,.12f,.025f),trim,smoothness:.12f);
            Detail("Picture frame",PrimitiveType.Cube,new Vector3(-.78f,2.06f,2.88f),new Vector3(.82f,.68f,.035f),darkWood,smoothness:.22f);
            Detail("Picture",PrimitiveType.Cube,new Vector3(-.78f,2.06f,2.84f),new Vector3(.68f,.54f,.025f),new Color(.53f,.67f,.61f),smoothness:.06f);

            Enemy = new GameObject("Enemy").transform; Enemy.SetParent(Root,false);
            Shape("Coat",PrimitiveType.Capsule,new Vector3(0,1.0f,0),new Vector3(.38f,.65f,.28f),ink,true,Enemy);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.68f,0),Vector3.one*.26f,ink,true,Enemy);
            Shape("Visor",PrimitiveType.Cube,new Vector3(0,1.70f,-.13f),new Vector3(.20f,.035f,.025f),new Color(.8f,.19f,.10f),true,Enemy,emission:2f);
            Shape("Weapon",PrimitiveType.Cube,new Vector3(-.13f,1.36f,-.24f),new Vector3(.09f,.10f,.35f),ink,true,Enemy);
            Detail("Left shoulder",PrimitiveType.Sphere,new Vector3(-.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
            Detail("Right shoulder",PrimitiveType.Sphere,new Vector3(.27f,1.43f,0),new Vector3(.28f,.16f,.24f),ink,true,Enemy,smoothness:.08f);
            Detail("Hat brim",PrimitiveType.Cube,new Vector3(0,1.83f,-.015f),new Vector3(.43f,.035f,.34f),ink,true,Enemy,smoothness:.08f);
            Detail("Ceiling light base",PrimitiveType.Cylinder,new Vector3(0,3.12f,.2f),new Vector3(.34f,.055f,.34f),ivory,smoothness:.22f);
            Detail("Ceiling light diffuser",PrimitiveType.Cylinder,new Vector3(0,3.04f,.2f),new Vector3(.29f,.045f,.29f),new Color(1,.93f,.82f),smoothness:.18f,emission:.55f);
            var light = new GameObject("Ceiling light").AddComponent<Light>();
            light.transform.SetParent(Root,false); light.transform.localPosition = new Vector3(0,2.86f,.2f);
            light.transform.localRotation=Quaternion.Euler(90,0,0); light.type=LightType.Spot;
            light.spotAngle=140; light.innerSpotAngle=110;
            light.range=6; light.intensity=2.6f; light.color=new Color(1,.93f,.82f);
            light.shadows=LightShadows.Soft; light.shadowResolution=LightShadowResolution.Medium;
            var ceilingFill = new GameObject("Ceiling light fill").AddComponent<Light>();
            ceilingFill.transform.SetParent(Root,false); ceilingFill.transform.localPosition=new Vector3(0,2.86f,.2f);
            ceilingFill.type=LightType.Point; ceilingFill.range=6; ceilingFill.intensity=.6f;
            ceilingFill.color=new Color(1,.93f,.82f); ceilingFill.shadows=LightShadows.None;
            var fill = new GameObject("Window daylight").AddComponent<Light>();
            fill.transform.SetParent(Root,false); fill.transform.localPosition=new Vector3(-2.20f,1.95f,1.42f);
            fill.transform.localRotation=Quaternion.Euler(0,90,0); fill.type=LightType.Spot; fill.spotAngle=105;
            fill.range=6; fill.intensity=1.25f; fill.color=new Color(.78f,.88f,1f); fill.shadows=LightShadows.None;
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

        void BuildSpectator()
        {
            var go=new GameObject("Public spectator camera"); go.transform.SetParent(Root,false);
            Spectator=go.AddComponent<Camera>(); Spectator.stereoTargetEye=StereoTargetEyeMask.None;
            Spectator.cullingMask=~(1<<PrivateLayer); Spectator.depth=10;
            // URP ignores stereoTargetEye; without this the spectator view (depth 10) is also rendered into the HMD.
            Spectator.GetUniversalAdditionalCameraData().allowXRRendering=false;
            Spectator.transform.position=new Vector3(-2.0f,2.65f,-1.8f);
            Spectator.transform.LookAt(new Vector3(0,1.1f,.8f)); Spectator.fieldOfView=63;
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
