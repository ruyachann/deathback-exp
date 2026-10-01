using UnityEngine;

namespace LoopRoom
{
    // Hinged entry door. The pose is a pure function of loop time, so LoopTime=0 always shows a closed door.
    public sealed class DoorRig
    {
        public const float StartTime = 3f;
        public const float TurnEnd = .15f;
        public const float GapEnd = .25f;
        public const float SwingStart = .40f;
        public const float SwingEnd = 1.0f;
        public const float CreakTime = StartTime + SwingStart;
        public const float GapAngle = 4f;
        public const float OpenAngle = 105f;
        public const float HandleDownAngle = 38f;
        const float HandleReleaseStart = .55f, HandleReleaseEnd = .90f;

        readonly Transform hinge, handle;
        float swing = float.NaN, lever = float.NaN;

        public DoorRig(Transform hinge, Transform handle)
        {
            this.hinge = hinge; this.handle = handle;
        }

        static float EaseOut(float s)
        {
            s = Mathf.Clamp01(s);
            float r = 1f - s;
            return 1f - r * r * r;
        }

        // seconds since the handle starts to turn; positive angle swings the door into the room.
        public static float SwingAngle(float seconds)
        {
            if (seconds <= TurnEnd) return 0f;
            if (seconds < GapEnd) return GapAngle * EaseOut((seconds - TurnEnd) / (GapEnd - TurnEnd));
            if (seconds < SwingStart) return GapAngle;
            return Mathf.Lerp(GapAngle, OpenAngle, EaseOut((seconds - SwingStart) / (SwingEnd - SwingStart)));
        }

        public static float HandleAngle(float seconds)
        {
            if (seconds <= 0f) return 0f;
            if (seconds < TurnEnd) return HandleDownAngle * EaseOut(seconds / TurnEnd);
            if (seconds < HandleReleaseStart) return HandleDownAngle;
            return HandleDownAngle * (1f - Mathf.SmoothStep(0f, 1f, (seconds - HandleReleaseStart) / (HandleReleaseEnd - HandleReleaseStart)));
        }

        public void SetLoopTime(float loopTime)
        {
            float seconds = loopTime - StartTime;
            float newSwing = SwingAngle(seconds), newLever = HandleAngle(seconds);
            if (newSwing != swing) { swing = newSwing; hinge.localRotation = Quaternion.Euler(0, swing, 0); }
            if (newLever != lever) { lever = newLever; handle.localRotation = Quaternion.Euler(0, 0, lever); }
        }
    }
}
