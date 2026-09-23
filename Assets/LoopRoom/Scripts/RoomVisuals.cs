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
        Font font;
        Transform head, left, right;
        GameObject publicHead, publicLeft, publicRight;

        public static Material Material(Color color, bool unlit = false)
        {
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            var m = new Material(shader); m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .23f);
            return m;
        }

        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool hidden = false, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (hidden)
            {
                go.layer = PrivateLayer;
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        TextMesh Text(string name, string text, Vector3 p, float size, Color color, bool hidden = true, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.localPosition = p;
            if (hidden) go.layer = PrivateLayer;
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.font = font;
            mesh.fontSize = 80; mesh.characterSize = size; mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center; mesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return mesh;
        }

        public void Build(Transform owner, DemoRig rig)
        {
            Root = new GameObject("Room").transform; Root.SetParent(owner,false);
            head = rig.View.transform; left = rig.Hands[0]; right = rig.Hands[1];
            font = Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic", "Meiryo", "Arial"}, 80);
            var ink = new Color(.045f,.075f,.09f); var stone = new Color(.20f,.27f,.28f);
            var brass = new Color(.73f,.51f,.26f); var ivory = new Color(.89f,.85f,.71f);
            Shape("Floor",PrimitiveType.Cube,new Vector3(0,-.08f,1),new Vector3(5,.16f,6),ink);
            Shape("Back wall",PrimitiveType.Cube,new Vector3(0,1.6f,3),new Vector3(5,3.2f,.15f),stone);
            Shape("Left wall",PrimitiveType.Cube,new Vector3(-2.5f,1.6f,1),new Vector3(.15f,3.2f,4),stone);
            Shape("Right wall",PrimitiveType.Cube,new Vector3(2.5f,1.6f,1),new Vector3(.15f,3.2f,4),stone);
            for (int i=-2;i<=2;i++) Shape("Wall seam",PrimitiveType.Cube,new Vector3(i,1.55f,2.90f),new Vector3(.025f,3.1f,.03f),ink);
            for (int i=0;i<8;i++) Shape("Floor inlay",PrimitiveType.Cube,new Vector3(0,.006f,-.9f+i*.52f),new Vector3(4.8f,.009f,.014f),stone);
            Shape("Desk",PrimitiveType.Cube,new Vector3(0,.80f,.52f),new Vector3(1.5f,.10f,.7f),brass);
            Shape("Desk base",PrimitiveType.Cube,new Vector3(0,.39f,.68f),new Vector3(1.1f,.78f,.35f),ink);
            Shape("Instruction card",PrimitiveType.Cube,new Vector3(0,1.06f,.67f),new Vector3(.41f,.24f,.024f),ivory,true);
            Card = Text("Instructions","この部屋から\n無事に脱出しろ",new Vector3(0,1.06f,.65f),.017f,ink);
            Shape("Clock body",PrimitiveType.Cube,new Vector3(0,1.41f,.75f),new Vector3(.32f,.20f,.12f),ink,true);
            Clock = Text("Clock","00 : 00",new Vector3(0,1.41f,.682f),.026f,ivory);
            Shape("Clock feet L",PrimitiveType.Cube,new Vector3(-.1f,1.27f,.75f),new Vector3(.03f,.12f,.06f),brass,true);
            Shape("Clock feet R",PrimitiveType.Cube,new Vector3(.1f,1.27f,.75f),new Vector3(.03f,.12f,.06f),brass,true);
            var shield = Shape("Shield handle",PrimitiveType.Cube,new Vector3(-.32f,1.01f,.36f),new Vector3(.14f,.08f,.08f),new Color(.18f,.62f,.65f),true);
            ShieldHandle = shield.AddComponent<XRSimpleInteractable>();
            Text("Shield mark","遮蔽",new Vector3(-.32f,.9f,.30f),.014f,ivory);
            var exit = Shape("Exit handle",PrimitiveType.Cube,new Vector3(.32f,1.01f,.36f),new Vector3(.14f,.08f,.08f),brass,true);
            ExitHandle = exit.AddComponent<XRSimpleInteractable>();
            ExitLabel = Text("Exit mark","施錠中",new Vector3(.32f,.9f,.30f),.014f,ivory);
            ExitLamp = Shape("Exit lamp",PrimitiveType.Sphere,new Vector3(.32f,1.12f,.40f),Vector3.one*.04f,Color.red,true).GetComponent<Renderer>();
            Barrier = Shape("Shield",PrimitiveType.Cube,new Vector3(0,.45f,1.08f),new Vector3(1.85f,1.6f,.08f),ink,true).transform;
            Door = Shape("Entry door",PrimitiveType.Cube,new Vector3(.8f,1.18f,2.86f),new Vector3(1,2.36f,.09f),ink,true).transform;
            Shape("Door lintel",PrimitiveType.Cube,new Vector3(.8f,2.41f,2.82f),new Vector3(1.18f,.06f,.12f),brass,true);
            Text("Room number","第零室",new Vector3(-1.3f,2.35f,2.88f),.07f,ivory,false);
            Text("Room detail","THE ROOM BEFORE",new Vector3(-1.3f,2.08f,2.88f),.023f,ivory,false);
            Enemy = new GameObject("Enemy").transform; Enemy.SetParent(Root,false);
            Shape("Coat",PrimitiveType.Capsule,new Vector3(0,1.0f,0),new Vector3(.38f,.65f,.28f),ink,true,Enemy);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.68f,0),Vector3.one*.26f,ink,true,Enemy);
            Shape("Visor",PrimitiveType.Cube,new Vector3(0,1.70f,-.13f),new Vector3(.20f,.035f,.025f),new Color(.8f,.19f,.10f),true,Enemy);
            Shape("Weapon",PrimitiveType.Cube,new Vector3(-.13f,1.36f,-.24f),new Vector3(.09f,.10f,.35f),ink,true,Enemy);
            var light = new GameObject("Warm overhead").AddComponent<Light>();
            light.transform.SetParent(Root,false); light.transform.position = new Vector3(0,2.7f,.2f);
            light.type = LightType.Point; light.range=8; light.intensity=3; light.color=new Color(1,.82f,.60f);
            var fill = new GameObject("Cool fill").AddComponent<Light>();
            fill.transform.SetParent(Root,false); fill.transform.position=new Vector3(-1.8f,2,2);
            fill.type=LightType.Point; fill.range=6; fill.intensity=2; fill.color=new Color(.28f,.65f,.8f);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.2f,.23f,.26f);
            Blackout = Shape("Eye blackout",PrimitiveType.Quad,new Vector3(0,0,.05f),new Vector3(1,1,1),Color.black,true,head);
            Blackout.GetComponent<Renderer>().sharedMaterial=Material(Color.black,true);
            Object.Destroy(Blackout.GetComponent<Collider>()); Blackout.SetActive(false);
            Sound = new GameObject("Room audio").AddComponent<AudioSource>(); Sound.transform.SetParent(Root,false);
            Sound.spatialBlend=0; Sound.volume=.25f; Sound.playOnAwake=false;
            Chime=Tone(660,.28f,false); Shot=Tone(80,.09f,true); Latch=Tone(170,.06f,false); Open=Tone(880,.16f,false);
            BuildSpectator();
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
            Spectator.clearFlags=CameraClearFlags.SolidColor; Spectator.backgroundColor=new Color(.015f,.025f,.035f);
            publicHead=Shape("Public head",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.2f,new Color(.3f,.8f,.8f));
            publicLeft=Shape("Public left hand",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.08f,new Color(.3f,.8f,.8f));
            publicRight=Shape("Public right hand",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.08f,new Color(.3f,.8f,.8f));
            publicHead.layer=publicLeft.layer=publicRight.layer=9;
            Object.Destroy(publicHead.GetComponent<Collider>()); Object.Destroy(publicLeft.GetComponent<Collider>()); Object.Destroy(publicRight.GetComponent<Collider>());
        }

        public void UpdatePublic(bool vr, bool active)
        {
            Spectator.enabled=vr;
            publicHead.SetActive(vr); publicLeft.SetActive(vr); publicRight.SetActive(vr);
            publicHead.transform.position=head.position;
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
