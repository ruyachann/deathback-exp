using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LoopRoom
{
    // Floor overlay shown only during calibration (task021), never to the spectator (PrivateLayer,
    // no colliders): the experience-area square, its inner margin, the forward reach sector, a foot
    // marker at the candidate center, and (when the headset reports one) the real guardian boundary
    // compared against that square. Root follows the player's head floor position/yaw every frame
    // while visible; the geometry itself is built once from PlayAreaSettings at Build().
    public sealed class CalibrationView
    {
        public Transform Root;
        PlayAreaSettings settings;
        Renderer outerRenderer;
        LineRenderer boundaryLine;

        static readonly Color FitColor = new Color(.25f,1f,.5f);
        static readonly Color NoFitColor = new Color(.9f,.2f,.15f);
        static readonly Color UnknownColor = new Color(.9f,.9f,.9f);
        static readonly Color MarginColor = new Color(.55f,.55f,.5f);
        static readonly Color BoundaryColor = new Color(.8f,.8f,.85f);
        static readonly Color SectorColor = new Color(.55f,.75f,.82f);
        static readonly Color FootColor = new Color(1f,.82f,.3f);
        // Neutral stand-in for the (hidden) room floor, dark enough that the white/green/red
        // outline stays readable against it (追修正: the desk was hiding the outline).
        static readonly Color FloorColor = new Color(.16f,.17f,.19f);
        const float OuterHeight=.016f, MarginHeight=.013f, SectorHeight=.010f, FootHeight=.008f, BoundaryHeight=.020f;
        // Default primitive Plane is a 10x10m mesh at scale 1, comfortably larger than the
        // largest allowed area/reach (PlayAreaSettings caps areaSize at 4m).
        const float FloorSize=10f;

        public void Build(Transform owner, PlayAreaSettings settings)
        {
            this.settings=settings;
            Root=new GameObject("Calibration view").transform; Root.SetParent(owner,false);
            Root.gameObject.SetActive(false);
            BuildFloor();
            var outer=Square("Area outline",settings.areaSize,OuterHeight,UnknownColor,.03f);
            outerRenderer=outer.GetComponent<Renderer>();
            Square("Area margin",settings.areaSize-2*settings.margin,MarginHeight,MarginColor,.02f);
            BuildSector();
            BuildFootMarker();
            var boundaryGo=new GameObject("Guardian boundary"); boundaryGo.transform.SetParent(Root,false);
            boundaryGo.layer=RoomVisuals.PrivateLayer;
            boundaryLine=boundaryGo.AddComponent<LineRenderer>();
            SetupLine(boundaryLine,BoundaryColor,.03f,true);
            boundaryGo.SetActive(false);
        }

        void BuildFloor()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name="Neutral floor"; go.transform.SetParent(Root,false); go.layer=RoomVisuals.PrivateLayer;
            go.transform.localScale=new Vector3(FloorSize/10f,1,FloorSize/10f);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            var renderer=go.GetComponent<Renderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            renderer.sharedMaterial=RoomVisuals.Material(FloorColor,true);
        }

        LineRenderer Square(string name, double side, float height, Color color, float width)
        {
            var go=new GameObject(name); go.transform.SetParent(Root,false); go.layer=RoomVisuals.PrivateLayer;
            var line=go.AddComponent<LineRenderer>();
            SetupLine(line,color,width,false);
            float half=(float)(side*0.5);
            line.positionCount=5;
            line.SetPosition(0,new Vector3(-half,height,-half));
            line.SetPosition(1,new Vector3(half,height,-half));
            line.SetPosition(2,new Vector3(half,height,half));
            line.SetPosition(3,new Vector3(-half,height,half));
            line.SetPosition(4,new Vector3(-half,height,-half));
            return line;
        }

        // View alignment (追修正2): TransformZ stood the ribbon vertical, invisible from above
        // once the room's furniture stopped occluding it; View always faces the camera instead.
        static void SetupLine(LineRenderer line, Color color, float width, bool worldSpace)
        {
            line.useWorldSpace=worldSpace;
            line.widthMultiplier=width;
            line.alignment=LineAlignment.View;
            line.material=RoomVisuals.Material(color,true);
            line.shadowCastingMode=ShadowCastingMode.Off;
            line.receiveShadows=false;
        }

        void BuildSector()
        {
            var go=new GameObject("Forward reach sector"); go.transform.SetParent(Root,false); go.layer=RoomVisuals.PrivateLayer;
            go.transform.localPosition=new Vector3(0,SectorHeight,0);
            go.AddComponent<MeshFilter>().sharedMesh=BuildSectorMesh(settings.reach,settings.halfAngleDeg);
            var renderer=go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            var material=RoomVisuals.Material(SectorColor,true);
            // The floor is normally viewed from above; guard against the fan's winding order
            // culling it from that angle since this can't be checked without the Editor running.
            if(material.HasProperty("_Cull")) material.SetFloat("_Cull",0);
            renderer.sharedMaterial=material;
        }

        static Mesh BuildSectorMesh(double reach, double halfAngleDeg, int segments=16)
        {
            var vertices=new List<Vector3>{Vector3.zero};
            var triangles=new List<int>();
            for(int i=0;i<=segments;i++)
            {
                double t=-halfAngleDeg+(2*halfAngleDeg)*i/segments;
                double rad=t*Math.PI/180.0;
                vertices.Add(new Vector3((float)(Math.Sin(rad)*reach),0,(float)(Math.Cos(rad)*reach)));
            }
            for(int i=1;i<vertices.Count-1;i++) { triangles.Add(0); triangles.Add(i); triangles.Add(i+1); }
            var mesh=new Mesh{name="Calibration sector"};
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        void BuildFootMarker()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name="Foot marker"; go.transform.SetParent(Root,false); go.layer=RoomVisuals.PrivateLayer;
            go.transform.localPosition=new Vector3(0,FootHeight,0);
            go.transform.localScale=new Vector3(.06f,.002f,.06f);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            var renderer=go.GetComponent<Renderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            renderer.sharedMaterial=RoomVisuals.Material(FootColor,true);
        }

        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        // headFloor/headYawDeg: the head's current floor projection/yaw (the candidate center and
        // orientation if the operator or player decides right now). boundaryWorldPoints/available:
        // from DemoRig.TryGetBoundaryPoints, already in world space.
        public void Refresh(Vector3 headFloor, float headYawDeg, List<Vector3> boundaryWorldPoints, bool boundaryAvailable)
        {
            Root.position=headFloor;
            Root.rotation=Quaternion.Euler(0,headYawDeg,0);
            bool fits=boundaryAvailable && FitsBoundary(headFloor,headYawDeg,boundaryWorldPoints);
            outerRenderer.material.color = !boundaryAvailable ? UnknownColor : fits ? FitColor : NoFitColor;
            boundaryLine.gameObject.SetActive(boundaryAvailable);
            if(boundaryAvailable)
            {
                int count=boundaryWorldPoints.Count;
                boundaryLine.positionCount=count+1;
                for(int i=0;i<count;i++) boundaryLine.SetPosition(i,boundaryWorldPoints[i]+Vector3.up*BoundaryHeight);
                boundaryLine.SetPosition(count,boundaryWorldPoints[0]+Vector3.up*BoundaryHeight);
            }
        }

        // Conservative visual check: the area square's 4 corners must all lie inside the reported
        // boundary polygon. This is independent of RoomAnchor (which checks the forward reach
        // against the aligned safe area, not the headset's guardian) and is deliberately simple
        // since it is only a display aid, not a safety gate.
        bool FitsBoundary(Vector3 center, float yawDeg, List<Vector3> poly)
        {
            if(poly.Count<3) return false;
            float half=(float)(settings.areaSize*0.5);
            var rotation=Quaternion.Euler(0,yawDeg,0);
            var corners=new[]{ new Vector3(-half,0,-half), new Vector3(half,0,-half), new Vector3(half,0,half), new Vector3(-half,0,half) };
            foreach(var corner in corners)
            {
                var world=center+rotation*corner;
                if(!PointInPolygon(world.x,world.z,poly)) return false;
            }
            return true;
        }

        static bool PointInPolygon(float x, float z, List<Vector3> poly)
        {
            bool inside=false;
            int n=poly.Count;
            for(int i=0,j=n-1;i<n;j=i++)
            {
                float xi=poly[i].x, zi=poly[i].z, xj=poly[j].x, zj=poly[j].z;
                bool intersect = ((zi>z)!=(zj>z)) && (x < (xj-xi)*(z-zi)/(zj-zi)+xi);
                if(intersect) inside=!inside;
            }
            return inside;
        }
    }
}
