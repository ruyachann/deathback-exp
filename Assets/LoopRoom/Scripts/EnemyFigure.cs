using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LoopRoom
{
    // Humanoid enemy built from primitives. Everything is a pure function of loop time (SetState), so a new loop
    // always starts from the same pose. Coordinates are room-local (x right, z deep); Root has an identity transform
    // under the room root, so parts are placed directly in room space.
    //
    //   t 3.0 -> 5.05  walks out of the corridor, through the open doorway, to (0.5, 2.5)
    //   t 5.0 -> 5.7   turns to face the player; arms rise from 5.2, aimed by 5.85
    //   t FirstShot    fires (recoil), holds the aim, lowers the gun from FirstShot+.55
    //   t 7.0 -> 7.75  turns toward the flank route; 7.75 -> 8.0 lifts the trailing foot
    //   t 8.0 -> 11.27 walks around the barrier to (1.25, 0.38)
    //   t 11.2 -> 11.85 raises the gun at the player; SearchShot fires again
    public sealed class EnemyFigure
    {
        public const float Walk1Start = 3f, Walk1Time = 2.05f, Walk1Decel = .7f;
        public const float Walk2Start = 8f, Walk2Time = 3.27f, Walk2Accel = .5f, Walk2Decel = .7f;
        public const float Settle = .25f;
        const float Walk1End = Walk1Start + Walk1Time, Walk2End = Walk2Start + Walk2Time;

        const float HipWalkY = .92f, HipStandY = .965f, HipHalf = .095f, ThighLen = .45f, ShinLen = .45f, AnkleY = .08f;
        const float FootSide = .09f, Lift = .07f, BobAmp = .03f;
        const float UpperArm = .29f, Forearm = .27f, ShoulderHalf = .19f;
        const float AimReach = .44f;

        static readonly Color Coat = new Color(.045f, .075f, .09f);
        static readonly Color Trousers = new Color(.06f, .07f, .08f);
        static readonly Color Boots = new Color(.03f, .03f, .035f);
        static readonly Color Hat = new Color(.035f, .05f, .06f);
        static readonly Color Metal = new Color(.10f, .11f, .12f);
        static readonly Color VisorColor = new Color(.8f, .19f, .10f);

        static readonly Vector2 Spawn = new Vector2(.5f, 4.46f);
        static readonly Vector2 Stand1 = new Vector2(.5f, 2.5f);
        static readonly Vector2 Stand2 = new Vector2(1.25f, .38f);
        static readonly WalkPath Path1 = new WalkPath(new[] { Spawn, Stand1 }, 0);
        static readonly WalkPath Path2 = new WalkPath(new[] { Stand1, new Vector2(.95f, 2.2f), new Vector2(1.18f, 1.75f), new Vector2(1.18f, 1.2f), Stand2 }, 2);

        public readonly Transform Root;
        public float FirstShot = 6f, SearchShot = 12f;
        public Vector3 GroundPosition { get; private set; }
        public int StepCount { get; private set; }

        readonly Transform torso, skirt, neck, head, crown, brim, visor, padR, padL;
        readonly Transform upperR, upperL, foreR, foreL, handR, handL;
        readonly Transform thighR, thighL, shinR, shinL, footR, footL, slide, grip;

        public EnemyFigure(Transform parent)
        {
            Root = new GameObject("Enemy").transform;
            Root.SetParent(parent, false);
            torso = Part("Coat torso", PrimitiveType.Capsule, Coat, .10f);
            skirt = Part("Coat skirt", PrimitiveType.Cylinder, Coat, .08f);
            neck = Part("Neck", PrimitiveType.Cylinder, Coat, .08f);
            head = Part("Head", PrimitiveType.Sphere, Coat, .10f);
            crown = Part("Hat crown", PrimitiveType.Cylinder, Hat, .08f);
            brim = Part("Hat brim", PrimitiveType.Cylinder, Hat, .08f);
            visor = Part("Visor", PrimitiveType.Cube, VisorColor, .2f, 1.2f);
            padR = Part("Right shoulder", PrimitiveType.Sphere, Coat, .08f);
            padL = Part("Left shoulder", PrimitiveType.Sphere, Coat, .08f);
            upperR = Part("Right upper arm", PrimitiveType.Capsule, Coat, .08f);
            upperL = Part("Left upper arm", PrimitiveType.Capsule, Coat, .08f);
            foreR = Part("Right forearm", PrimitiveType.Capsule, Coat, .08f);
            foreL = Part("Left forearm", PrimitiveType.Capsule, Coat, .08f);
            handR = Part("Right hand", PrimitiveType.Sphere, Boots, .12f);
            handL = Part("Left hand", PrimitiveType.Sphere, Boots, .12f);
            thighR = Part("Right thigh", PrimitiveType.Capsule, Trousers, .08f);
            thighL = Part("Left thigh", PrimitiveType.Capsule, Trousers, .08f);
            shinR = Part("Right shin", PrimitiveType.Capsule, Trousers, .08f);
            shinL = Part("Left shin", PrimitiveType.Capsule, Trousers, .08f);
            footR = Part("Right boot", PrimitiveType.Cube, Boots, .15f);
            footL = Part("Left boot", PrimitiveType.Cube, Boots, .15f);
            slide = Part("Pistol slide", PrimitiveType.Cube, Metal, .35f, 0f, .4f);
            grip = Part("Pistol grip", PrimitiveType.Cube, Boots, .15f);
        }

        Transform Part(string name, PrimitiveType type, Color color, float smoothness, float emission = 0f, float metallic = 0f)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            var collider = go.GetComponent<Collider>(); if (collider != null) Object.Destroy(collider);
            go.transform.SetParent(Root, false);
            go.layer = RoomVisuals.PrivateLayer;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = RoomVisuals.Material(color, false, smoothness, metallic, emission);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        // Pose at loop time t. head is the player's head in room-local coordinates (what the gun points at).
        public void SetState(float t, Vector3 headTarget)
        {
            bool second = t >= Walk2Start - Settle;
            float v1, v2 = 0f, s2 = 0f;
            float s1 = Travelled(t - Walk1Start, Path1.Length, Walk1Time, 0f, Walk1Decel, out v1);
            if (second) s2 = Travelled(t - Walk2Start, Path2.Length, Walk2Time, Walk2Accel, Walk2Decel, out v2);
            WalkPath path = second ? Path2 : Path1;
            float s = second ? s2 : s1;
            float speed = second ? v2 : v1;
            float mf = Mathf.Clamp01(speed / .7f);

            // The left foot is the one that hangs in mid-swing when the walk stops, so it is lowered after a stop
            // and lifted before the next start; both feet end up side by side.
            float liftLeft = 1f;
            if (!second) { if (t > Walk1End) liftLeft = 1f - Ramp(Walk1End, Walk1End + Settle, t); }
            else if (t < Walk2Start) liftLeft = Ramp(Walk2Start - Settle, Walk2Start, t);
            else if (t > Walk2End) liftLeft = 1f - Ramp(Walk2End, Walk2End + Settle, t);

            float turnToSecond = Ramp(Walk2Start - Settle - .75f, Walk2Start - Settle, t);
            float bodyYaw, footYaw = float.NaN;
            if (!second)
            {
                float toPlayer = YawTo(Stand1, headTarget);
                float yaw = Mathf.LerpAngle(Path1.Yaw(s1), toPlayer, Ramp(Walk1End - .05f, Walk1End + .65f, t));
                bodyYaw = Mathf.LerpAngle(yaw, Path2.Yaw(0f), turnToSecond);
                if (t >= Walk1End) footYaw = Mathf.LerpAngle(Path1.Yaw(Path1.Length), Path2.Yaw(0f), turnToSecond);
            }
            else
            {
                float toPlayer2 = YawTo(Stand2, headTarget);
                bodyYaw = Mathf.LerpAngle(Path2.Yaw(s2), toPlayer2, Ramp(Walk2End - .05f, Walk2End + .65f, t));
                if (t >= Walk2End) footYaw = Mathf.LerpAngle(Path2.Yaw(Path2.Length), toPlayer2, Ramp(Walk2End + Settle - .05f, Walk2End + .65f, t));
            }

            Vector2 p = path.At(s);
            float u = .3f + s / path.Cycle;
            float cyclePhase = 2f * Mathf.PI * Frac(u);
            float bob = BobAmp * (Mathf.Cos(4f * Mathf.PI * (u - .3f)) - 1f) * .5f * mf;
            Vector3 root = new Vector3(p.x, bob + FloorLift(p.y), p.y);
            GroundPosition = new Vector3(p.x, 0f, p.y);
            StepCount = CountSteps(t, second, s1, s2);

            float aim1 = Ramp(FirstShot - .8f, FirstShot - .15f, t) * (1f - Ramp(FirstShot + .55f, FirstShot + 1.35f, t));
            float aim2 = Ramp(SearchShot - .8f, SearchShot - .15f, t);
            float aimW = Mathf.Max(aim1, aim2);
            float kick = Mathf.Max(Kick(t, FirstShot), Kick(t, SearchShot));
            float breath = Mathf.Sin(2f * Mathf.PI * .28f * t) * (1f - mf);

            float twist = 4f * Mathf.Cos(cyclePhase) * mf;
            Quaternion pelvisRot = Quaternion.Euler(0f, bodyYaw - twist, 0f);
            Quaternion torsoYaw = Quaternion.Euler(0f, bodyYaw + twist, 0f);
            Quaternion torsoRot = Quaternion.Euler(4f * mf + 3f * aimW, bodyYaw + twist, 0f);
            Vector3 fwd = torsoYaw * Vector3.forward, rgt = torsoYaw * Vector3.right;

            Vector3 hipC = root + new Vector3(0f, Mathf.Lerp(HipStandY, HipWalkY, mf), 0f);
            Vector3 hipR = hipC + pelvisRot * new Vector3(HipHalf, 0f, 0f);
            Vector3 hipL = hipC + pelvisRot * new Vector3(-HipHalf, 0f, 0f);
            Vector3 torsoBase = hipC + new Vector3(0f, .06f, 0f);
            Vector3 shoulderLift = new Vector3(0f, .004f * breath, 0f);
            Vector3 sR = torsoBase + torsoRot * new Vector3(ShoulderHalf, .44f, 0f) + shoulderLift;
            Vector3 sL = torsoBase + torsoRot * new Vector3(-ShoulderHalf, .44f, 0f) + shoulderLift;

            // Legs: feet are pinned to the path by walking distance, so they never slide.
            float yawR, yawL;
            Vector3 ankleR = FootPoint(path, s, true, 1f, footYaw, out yawR);
            Vector3 ankleL = FootPoint(path, s, false, liftLeft, footYaw, out yawL);
            Vector3 legPole = pelvisRot * Vector3.forward;
            Vector3 endR, endL;
            Vector3 kneeR = Solve(hipR, ankleR, ThighLen, ShinLen, legPole, out endR);
            Vector3 kneeL = Solve(hipL, ankleL, ThighLen, ShinLen, legPole, out endL);
            Limb(thighR, hipR, kneeR, .075f); Limb(shinR, kneeR, endR, .06f);
            Limb(thighL, hipL, kneeL, .075f); Limb(shinL, kneeL, endL, .06f);
            PlaceFoot(footR, endR, yawR); PlaceFoot(footL, endL, yawL);

            // Arms: the left swings opposite the right foot, the right carries the pistol low until it is raised.
            float swing = Mathf.Cos(cyclePhase) * mf;
            float swingAngle = (18f * swing + 2f) * Mathf.Deg2Rad;
            Vector3 restL = sL + fwd * (.5f * Mathf.Sin(swingAngle)) + Vector3.down * (.5f * Mathf.Cos(swingAngle)) - rgt * .03f;
            Vector3 lowR = sR + fwd * (.30f - .02f * swing) + Vector3.down * .33f;

            Vector3 shoulderMid = (sR + sL) * .5f;
            Vector3 toHead = headTarget - shoulderMid;
            Vector3 aimDir = toHead.sqrMagnitude > 1e-4f ? toHead.normalized : fwd;
            Vector3 aimR = sR + aimDir * AimReach - aimDir * (.07f * kick) + Vector3.up * (.012f * kick);
            Vector3 toHeadFromGun = headTarget - aimR;
            Vector3 gunDir = toHeadFromGun.sqrMagnitude > 1e-4f ? toHeadFromGun.normalized : aimDir;
            gunDir = Quaternion.AngleAxis(-8f * kick, rgt) * gunDir;
            Vector3 aimL = aimR - rgt * .075f + Vector3.down * .015f;

            Vector3 targetR = Vector3.Lerp(lowR, aimR, aimW);
            Vector3 targetL = Vector3.Lerp(restL, aimL, aimW);
            Vector3 poleR = Vector3.Lerp(Vector3.down * .6f - fwd * .3f + rgt * .15f, Vector3.down * .8f + rgt * .5f, aimW);
            Vector3 poleL = Vector3.Lerp(Vector3.down * .6f - fwd * .4f - rgt * .15f, Vector3.down * .8f - rgt * .5f, aimW);
            Vector3 wristR, wristL;
            Vector3 elbowR = Solve(sR, targetR, UpperArm, Forearm, poleR, out wristR);
            Vector3 elbowL = Solve(sL, targetL, UpperArm, Forearm, poleL, out wristL);
            Limb(upperR, sR, elbowR, .055f); Limb(foreR, elbowR, wristR, .045f);
            Limb(upperL, sL, elbowL, .055f); Limb(foreL, elbowL, wristL, .045f);
            PlaceHand(handR, elbowR, wristR); PlaceHand(handL, elbowL, wristL);

            Vector3 forearmDir = (wristR - elbowR).normalized;
            Vector3 gunLine = Vector3.Slerp(forearmDir, gunDir, aimW);
            Quaternion gunRot = Quaternion.LookRotation(gunLine, Vector3.up);
            slide.localPosition = wristR + gunLine * .07f; slide.localRotation = gunRot; slide.localScale = new Vector3(.035f, .05f, .22f);
            grip.localPosition = wristR + gunRot * new Vector3(0f, -.05f, -.005f); grip.localRotation = gunRot; grip.localScale = new Vector3(.032f, .10f, .04f);

            // Torso, head and coat.
            torso.localPosition = torsoBase + torsoRot * new Vector3(0f, .22f, 0f); torso.localRotation = torsoRot;
            torso.localScale = new Vector3(.36f, .27f, .25f * (1f + .03f * breath));
            skirt.localPosition = hipC + pelvisRot * new Vector3(0f, -.22f, 0f); skirt.localRotation = pelvisRot;
            skirt.localScale = new Vector3(.40f, .26f, .30f);
            padR.localPosition = sR; padR.localRotation = torsoRot; padR.localScale = new Vector3(.13f, .10f, .12f);
            padL.localPosition = sL; padL.localRotation = torsoRot; padL.localScale = new Vector3(.13f, .10f, .12f);
            neck.localPosition = torsoBase + torsoRot * new Vector3(0f, .53f, 0f); neck.localRotation = torsoRot; neck.localScale = new Vector3(.09f, .06f, .09f);

            Vector3 headC = torsoBase + torsoRot * new Vector3(0f, .66f, 0f) + shoulderLift;
            Vector3 toTarget = headTarget - headC;
            Quaternion headRot = torsoRot;
            if (toTarget.sqrMagnitude > 1e-4f) headRot = Quaternion.Slerp(torsoRot, Quaternion.LookRotation(toTarget, Vector3.up), aimW * .85f);
            head.localPosition = headC; head.localRotation = headRot; head.localScale = Vector3.one * .21f;
            crown.localPosition = headC + headRot * new Vector3(0f, .095f, 0f); crown.localRotation = headRot; crown.localScale = new Vector3(.235f, .05f, .235f);
            brim.localPosition = headC + headRot * new Vector3(0f, .05f, .01f); brim.localRotation = headRot * Quaternion.Euler(8f, 0f, 0f); brim.localScale = new Vector3(.40f, .008f, .40f);
            visor.localPosition = headC + headRot * new Vector3(0f, -.01f, .10f); visor.localRotation = headRot; visor.localScale = new Vector3(.15f, .02f, .015f);
        }

        static void Limb(Transform tf, Vector3 a, Vector3 b, float radius)
        {
            Vector3 d = b - a; float len = d.magnitude;
            if (len < 1e-4f) { tf.localScale = Vector3.zero; return; }
            tf.localPosition = (a + b) * .5f;
            tf.localRotation = Quaternion.FromToRotation(Vector3.up, d / len);
            tf.localScale = new Vector3(radius * 2f, len * .5f, radius * 2f);
        }

        static void PlaceFoot(Transform tf, Vector3 ankle, float yaw)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            tf.localPosition = ankle + rot * new Vector3(0f, -.045f, .05f);
            tf.localRotation = rot;
            tf.localScale = new Vector3(.10f, .07f, .26f);
        }

        static void PlaceHand(Transform tf, Vector3 elbow, Vector3 wrist)
        {
            Vector3 d = wrist - elbow;
            tf.localPosition = d.sqrMagnitude > 1e-6f ? wrist + d.normalized * .03f : wrist;
            tf.localScale = Vector3.one * .08f;
        }

        // Two-bone IK: returns the middle joint; end is the reached position (clamped to the limb's reach).
        static Vector3 Solve(Vector3 origin, Vector3 target, float l1, float l2, Vector3 pole, out Vector3 end)
        {
            Vector3 d = target - origin; float dist = d.magnitude;
            Vector3 dir = dist > 1e-4f ? d / dist : Vector3.down;
            dist = Mathf.Clamp(dist, Mathf.Abs(l1 - l2) + .01f, (l1 + l2) * .999f);
            end = origin + dir * dist;
            float a = (l1 * l1 - l2 * l2 + dist * dist) / (2f * dist);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 perp = pole - dir * Vector3.Dot(pole, dir);
            perp = perp.sqrMagnitude > 1e-6f ? perp.normalized : Vector3.Cross(dir, Vector3.right).normalized;
            return origin + dir * a + perp * h;
        }

        // Position of one ankle for the distance-driven gait: stance (60% of the cycle) holds the foot still on
        // the ground while the body passes over it; swing (40%) carries it to the next contact point.
        // rightFoot=true is the foot whose swing is centred at u=.8; the left foot is half a cycle later.
        static Vector3 FootPoint(WalkPath path, float s, bool rightFoot, float liftScale, float yawOverride, out float yaw)
        {
            float cycle = path.Cycle, halfStride = .3f * cycle;
            float phi = Frac(.3f + s / cycle + (rightFoot ? 0f : .5f));
            float rel, height = 0f;
            if (phi < .6f) rel = halfStride - phi / .6f * 2f * halfStride;
            else
            {
                float sw = (phi - .6f) / .4f;
                rel = -halfStride + 2f * halfStride * Mathf.SmoothStep(0f, 1f, sw);
                height = Lift * Mathf.Sin(Mathf.PI * sw) * liftScale;
            }
            float arc = Mathf.Clamp(s + rel, 0f, path.Length);
            Vector2 point = path.At(arc);
            yaw = float.IsNaN(yawOverride) ? path.Yaw(arc) : yawOverride;
            float rad = yaw * Mathf.Deg2Rad;
            Vector2 side = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * (rightFoot ? FootSide : -FootSide);
            return new Vector3(point.x + side.x, AnkleY + height + FloorLift(point.y), point.y + side.y);
        }

        // The corridor floor top is 1 cm above the room floor; blend over the wall thickness so soles rest on it.
        static float FloorLift(float z)
        {
            return .01f * Mathf.Clamp01((z - 2.925f) / .15f);
        }

        // Footfalls so far: heel strikes at u = .5, 1, 1.5, ... plus the trailing foot settling after each stop.
        static int CountSteps(float t, bool second, float s1, float s2)
        {
            int n = Contacts(.3f + s1 / Path1.Cycle);
            if (t >= Walk1End + Settle) n++;
            if (second)
            {
                n += Contacts(.3f + s2 / Path2.Cycle);
                if (t >= Walk2End + Settle) n++;
            }
            return n;
        }

        static int Contacts(float u)
        {
            return u < .5f ? 0 : Mathf.FloorToInt((u - .5f) / .5f + 1e-3f) + 1;
        }

        // Trapezoid speed profile: accelerate, cruise, decelerate to a stop exactly at length.
        static float Travelled(float tau, float length, float time, float accel, float decel, out float speed)
        {
            speed = 0f;
            if (tau <= 0f) return 0f;
            if (tau >= time) return length;
            float v = length / (time - accel * .5f - decel * .5f);
            if (accel > 0f && tau < accel) { speed = v * tau / accel; return v * tau * tau / (2f * accel); }
            if (tau <= time - decel) { speed = v; return v * accel * .5f + v * (tau - accel); }
            float x = time - tau; speed = v * x / decel;
            return length - v * x * x / (2f * decel);
        }

        static float Kick(float t, float shotTime)
        {
            return t < shotTime ? 0f : Mathf.Exp(-(t - shotTime) * 9f);
        }

        static float Ramp(float from, float to, float t)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - from) / (to - from)));
        }

        static float Frac(float x)
        {
            return x - Mathf.Floor(x);
        }

        static float YawTo(Vector2 from, Vector3 target)
        {
            return Mathf.Atan2(target.x - from.x, target.z - from.y) * Mathf.Rad2Deg;
        }

        // Corner-cut polyline (Chaikin) with an arc-length table; positions are (x, z).
        sealed class WalkPath
        {
            readonly Vector2[] points; readonly float[] cumulative;
            public readonly float Length, Cycle;

            public WalkPath(Vector2[] corners, int smoothing)
            {
                var list = new List<Vector2>(corners);
                for (int pass = 0; pass < smoothing; pass++)
                {
                    var next = new List<Vector2> { list[0] };
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        next.Add(Vector2.Lerp(list[i], list[i + 1], .25f));
                        next.Add(Vector2.Lerp(list[i], list[i + 1], .75f));
                    }
                    next.Add(list[list.Count - 1]);
                    list = next;
                }
                points = list.ToArray();
                cumulative = new float[points.Length];
                for (int i = 1; i < points.Length; i++) cumulative[i] = cumulative[i - 1] + Vector2.Distance(points[i - 1], points[i]);
                Length = cumulative[cumulative.Length - 1];
                Cycle = Length / Mathf.Max(1, Mathf.CeilToInt(Length / 1.2f - .001f));
            }

            public Vector2 At(float s)
            {
                s = Mathf.Clamp(s, 0f, Length);
                int i = 1;
                while (i < cumulative.Length - 1 && cumulative[i] < s) i++;
                float segment = cumulative[i] - cumulative[i - 1];
                return segment < 1e-5f ? points[i] : Vector2.Lerp(points[i - 1], points[i], (s - cumulative[i - 1]) / segment);
            }

            public float Yaw(float s)
            {
                Vector2 d = At(s + .2f) - At(s - .2f);
                return d.sqrMagnitude < 1e-8f ? 0f : Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            }
        }
    }
}
