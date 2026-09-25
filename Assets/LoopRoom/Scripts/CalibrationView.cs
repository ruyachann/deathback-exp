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
        // Outcome of the last Refresh() against the reported guardian boundary. LoopDemo reads
        // this right after calling Refresh() (including its own re-check inside
        // CommitCalibration()) to decide whether a commit needs the "境界外で決定" warning
        // (task021 追修正3-1).
        public enum BoundaryFit { Unknown, Fits, Outside }

        public Transform Root;
        public BoundaryFit Fit { get; private set; } = BoundaryFit.Unknown;
        PlayAreaSettings settings;
        Renderer outerRenderer;
        LineRenderer boundaryLine;
        LineRenderer holdRing;

        static readonly Color FitColor = new Color(.25f,1f,.5f);
        static readonly Color NoFitColor = new Color(.9f,.2f,.15f);
        static readonly Color UnknownColor = new Color(.9f,.9f,.9f);
        static readonly Color MarginColor = new Color(.55f,.55f,.5f);
        static readonly Color BoundaryColor = new Color(.8f,.8f,.85f);
        static readonly Color SectorColor = new Color(.55f,.75f,.82f);
        static readonly Color FootColor = new Color(1f,.82f,.3f);
        // task027 追修正2-1: was .009f, below the sector (.010f) and thus hidden under it where the
        // two overlap. Placed above every other private-layer line here (incl. BoundaryHeight, the
        // highest of the rest) so the ring never sits under the outline/margin/sector/boundary.
        const float HoldRingHeight=.024f, HoldRingRadius=.10f, HoldRingWidth=.018f;
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
            BuildHoldRing();
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

        void BuildHoldRing()
        {
            var go=new GameObject("Hold ring"); go.transform.SetParent(Root,false); go.layer=RoomVisuals.PrivateLayer;
            holdRing=go.AddComponent<LineRenderer>();
            SetupLine(holdRing,FootColor,HoldRingWidth,false);
            // Belt-and-braces alongside HoldRingHeight: draw after the sector even if a future
            // change narrows the height gap (getter instantiates a per-renderer copy, so this
            // doesn't affect the shared FootColor material the foot marker also uses).
            holdRing.material.renderQueue+=1;
            holdRing.positionCount=0;
            go.SetActive(false);
        }

        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        // task027: progress of the A/X (or C, or --auto-calibrate) hold, 0..1. Drawn as an arc
        // around the foot marker that grows clockwise from nothing (t<=0, hidden) to a full ring
        // (t=1, about to commit), brightening toward FootColor as it fills.
        public void SetHoldProgress(float t)
        {
            t=Mathf.Clamp01(t);
            holdRing.gameObject.SetActive(t>0f);
            if(t<=0f) { holdRing.positionCount=0; return; }
            const int segments=40;
            int count=Mathf.Max(2,Mathf.RoundToInt(segments*t)+1);
            holdRing.positionCount=count;
            float sweep=360f*t;
            for(int i=0;i<count;i++)
            {
                float rad=(-90f+sweep*i/(count-1))*Mathf.Deg2Rad;
                holdRing.SetPosition(i,new Vector3(Mathf.Cos(rad)*HoldRingRadius,HoldRingHeight,Mathf.Sin(rad)*HoldRingRadius));
            }
            var dim=new Color(FootColor.r*.45f,FootColor.g*.45f,FootColor.b*.45f,1f);
            holdRing.material.color=Color.Lerp(dim,FootColor,t);
        }

        // headFloor/headYawDeg: the head's current floor projection/yaw (the candidate center and
        // orientation if the operator or player decides right now). boundaryWorldPoints/available:
        // from DemoRig.TryGetBoundaryPoints, already in world space.
        public void Refresh(Vector3 headFloor, float headYawDeg, List<Vector3> boundaryWorldPoints, bool boundaryAvailable)
        {
            Root.position=headFloor;
            Root.rotation=Quaternion.Euler(0,headYawDeg,0);
            // A polygon needs >=3 points; treat fewer (including an empty list from a caller that
            // reports success with no points) as "no boundary" rather than indexing into it below
            // (task021 追修正5-2).
            boundaryAvailable = boundaryAvailable && boundaryWorldPoints.Count>=3;
            bool fits=boundaryAvailable && FitsBoundary(headFloor,headYawDeg,boundaryWorldPoints);
            Fit = !boundaryAvailable ? BoundaryFit.Unknown : fits ? BoundaryFit.Fits : BoundaryFit.Outside;
            outerRenderer.material.color = Fit==BoundaryFit.Unknown ? UnknownColor : Fit==BoundaryFit.Fits ? FitColor : NoFitColor;
            boundaryLine.gameObject.SetActive(boundaryAvailable);
            if(boundaryAvailable)
            {
                int count=boundaryWorldPoints.Count;
                boundaryLine.positionCount=count+1;
                for(int i=0;i<count;i++) boundaryLine.SetPosition(i,boundaryWorldPoints[i]+Vector3.up*BoundaryHeight);
                boundaryLine.SetPosition(count,boundaryWorldPoints[0]+Vector3.up*BoundaryHeight);
            }
        }

        // Visual check: the area square's 4 corners must all lie inside the reported boundary
        // polygon, AND none of the square's 4 edges may cross (or touch) any boundary edge. The
        // corners-only check alone passes a concave boundary whose notch cuts across an edge/the
        // interior while all 4 corners stay inside it, which would wrongly show green (task021
        // 追修正3-2). Still independent of RoomAnchor (which checks the forward reach against the
        // aligned safe area, not the headset's guardian) and deliberately simple/conservative
        // since it is only a display aid, not a safety gate — any touching counts as Outside.
        bool FitsBoundary(Vector3 center, float yawDeg, List<Vector3> poly)
        {
            if(poly.Count<3) return false;
            float half=(float)(settings.areaSize*0.5);
            var rotation=Quaternion.Euler(0,yawDeg,0);
            var localCorners=new[]{ new Vector3(-half,0,-half), new Vector3(half,0,-half), new Vector3(half,0,half), new Vector3(-half,0,half) };
            var corners=new Vector3[4];
            for(int i=0;i<4;i++) corners[i]=center+rotation*localCorners[i];
            foreach(var corner in corners)
                if(!PointInPolygon(corner.x,corner.z,poly)) return false;
            int n=poly.Count;
            for(int i=0;i<4;i++)
            {
                var a1=corners[i]; var a2=corners[(i+1)%4];
                for(int j=0;j<n;j++)
                {
                    var b1=poly[j]; var b2=poly[(j+1)%n];
                    if(SegmentsIntersect(a1.x,a1.z,a2.x,a2.z,b1.x,b1.z,b2.x,b2.z)) return false;
                }
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

        // Standard orientation-based segment intersection for the proper-crossing case, plus an
        // explicit point-to-segment distance check (double, in meters) for the touching/near-
        // touching case (task021 追修正5-1): the previous version applied Epsilon directly to the
        // cross product, whose magnitude is in m² and scales with segment length, so "within 1e-4m"
        // was not actually what it tested (a long boundary edge could pass 0.00005m away from a
        // square edge with a cross product well outside Epsilon and wrongly read as Fits/green).
        // Checking the actual distance from each endpoint to the opposite segment is exact
        // regardless of segment length and also covers zero-length (degenerate) edges, which
        // PointSegmentDistance treats as a single point.
        const double Epsilon = 1e-4;

        static bool SegmentsIntersect(float ax1,float az1,float ax2,float az2,float bx1,float bz1,float bx2,float bz2)
        {
            double Ax1=ax1,Az1=az1,Ax2=ax2,Az2=az2,Bx1=bx1,Bz1=bz1,Bx2=bx2,Bz2=bz2;
            if(PointSegmentDistance(Ax1,Az1,Bx1,Bz1,Bx2,Bz2)<=Epsilon) return true;
            if(PointSegmentDistance(Ax2,Az2,Bx1,Bz1,Bx2,Bz2)<=Epsilon) return true;
            if(PointSegmentDistance(Bx1,Bz1,Ax1,Az1,Ax2,Az2)<=Epsilon) return true;
            if(PointSegmentDistance(Bx2,Bz2,Ax1,Az1,Ax2,Az2)<=Epsilon) return true;

            double d1=Cross(Bx1,Bz1,Bx2,Bz2,Ax1,Az1);
            double d2=Cross(Bx1,Bz1,Bx2,Bz2,Ax2,Az2);
            double d3=Cross(Ax1,Az1,Ax2,Az2,Bx1,Bz1);
            double d4=Cross(Ax1,Az1,Ax2,Az2,Bx2,Bz2);
            int s1=Math.Sign(d1), s2=Math.Sign(d2), s3=Math.Sign(d3), s4=Math.Sign(d4);
            return s1!=0 && s2!=0 && s3!=0 && s4!=0 && s1!=s2 && s3!=s4;
        }

        static double Cross(double ox,double oz,double ax,double az,double px,double pz) => (ax-ox)*(pz-oz)-(az-oz)*(px-ox);

        // Distance from point p to segment [a,b], all in double. lenSq==0 collapses to a
        // point-to-point distance, covering degenerate (zero-length) edges.
        static double PointSegmentDistance(double px,double pz,double ax,double az,double bx,double bz)
        {
            double dx=bx-ax, dz=bz-az;
            double lenSq=dx*dx+dz*dz;
            double t = lenSq<=0 ? 0 : ((px-ax)*dx+(pz-az)*dz)/lenSq;
            t = Math.Max(0,Math.Min(1,t));
            double cx=ax+t*dx, cz=az+t*dz;
            double ex=px-cx, ez=pz-cz;
            return Math.Sqrt(ex*ex+ez*ez);
        }
    }
}
