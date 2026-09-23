using System;
using System.Collections.Generic;

namespace LoopRoom
{
    public enum SessionPhase { Ready, Playing, Blackout, Escaped, TimedOut, Interrupted, Finished }

    [Serializable]
    public sealed class LoopRules
    {
        public double firstShot = 6.0;
        public double searchShot = 12.0;
        public double exitOpens = 6.5;
        public double exitCloses = 12.0;
        public double blackout = 0.16;
        public double playLimit = 172.0;
        public double endingLength = 8.0;
        public bool enforcePlayLimit = false;
        public void Validate()
        {
            if (double.IsNaN(firstShot) || double.IsInfinity(firstShot) ||
                double.IsNaN(searchShot) || double.IsInfinity(searchShot) ||
                double.IsNaN(exitOpens) || double.IsInfinity(exitOpens) ||
                double.IsNaN(exitCloses) || double.IsInfinity(exitCloses) ||
                double.IsNaN(blackout) || double.IsInfinity(blackout) ||
                double.IsNaN(playLimit) || double.IsInfinity(playLimit) ||
                double.IsNaN(endingLength) || double.IsInfinity(endingLength))
                throw new ArgumentException("Invalid loop timing rules");
            if (firstShot <= 0 || searchShot <= firstShot || exitOpens < firstShot ||
                exitCloses > searchShot || exitCloses <= exitOpens || blackout <= 0 ||
                playLimit <= 0 || endingLength <= 0 ||
                (enforcePlayLimit && playLimit + endingLength > 180.0))
                throw new ArgumentException("Invalid loop timing rules");
        }
    }

    [Serializable]
    public sealed class LoopRecord
    {
        public int loop;
        public double sessionTime;
        public double loopTime;
        public string kind;
    }

    // Pure deterministic state model. No coroutines, XR dependency or wall-clock resets.
    public sealed class LoopModel
    {
        public const double MaxStep = 3600.0;
        public readonly LoopRules Rules;
        public readonly List<LoopRecord> Records = new List<LoopRecord>();
        public SessionPhase Phase { get; private set; } = SessionPhase.Ready;
        public SessionPhase Outcome { get; private set; } = SessionPhase.Ready;
        public int LoopId { get; private set; }
        public double TotalTime { get; private set; }
        public double LoopTime { get; private set; }
        public bool ShieldRaised { get; private set; }
        public bool ShotResolved { get; private set; }
        public bool ExitAvailable => Phase == SessionPhase.Playing && ShotResolved &&
            LoopTime >= Rules.exitOpens && LoopTime < Rules.exitCloses;
        public double BlackoutRemaining { get; private set; }
        double endingRemaining;

        public LoopModel(LoopRules rules = null)
        {
            Rules = rules ?? new LoopRules();
            Rules.Validate();
        }

        public void Start()
        {
            if (Phase != SessionPhase.Ready && Phase != SessionPhase.Finished) return;
            Records.Clear(); TotalTime = 0; LoopId = 0;
            Outcome = SessionPhase.Ready;
            BeginLoop();
        }

        void BeginLoop()
        {
            LoopId++; LoopTime = 0; ShieldRaised = false; ShotResolved = false;
            BlackoutRemaining = 0; Phase = SessionPhase.Playing;
            Record("loop_started");
        }

        public bool RaiseShield(int expectedLoop)
        {
            if (Phase != SessionPhase.Playing || expectedLoop != LoopId || ShieldRaised) return false;
            ShieldRaised = true; Record("shield_raised"); return true;
        }

        public bool TryExit(int expectedLoop)
        {
            if (expectedLoop != LoopId || !ExitAvailable) return false;
            End(SessionPhase.Escaped); return true;
        }

        public bool Kill(int expectedLoop, string cause)
        {
            if (Phase != SessionPhase.Playing || expectedLoop != LoopId) return false;
            Record(cause); Phase = SessionPhase.Blackout; BlackoutRemaining = Rules.blackout;
            return true;
        }

        public void Interrupt()
        {
            if (Phase == SessionPhase.Playing || Phase == SessionPhase.Blackout)
                End(SessionPhase.Interrupted);
        }

        void End(SessionPhase outcome)
        {
            Phase = Outcome = outcome; endingRemaining = Rules.endingLength;
            Record(outcome.ToString());
        }

        void Record(string kind) => Records.Add(new LoopRecord {
            loop = LoopId, loopTime = LoopTime, sessionTime = TotalTime, kind = kind });

        // Split time at event boundaries so slow frames cannot skip an attack or grant free time.
        public void Advance(double delta)
        {
            if (double.IsNaN(delta) || double.IsInfinity(delta) || delta < 0 || delta > MaxStep)
                throw new ArgumentOutOfRangeException(nameof(delta));
            while (delta > 0.0000001)
            {
                if (Phase == SessionPhase.Ready || Phase == SessionPhase.Finished) return;
                if (Phase == SessionPhase.Escaped || Phase == SessionPhase.TimedOut || Phase == SessionPhase.Interrupted)
                {
                    double step = Math.Min(delta, endingRemaining);
                    endingRemaining -= step; TotalTime += step; delta -= step;
                    if (endingRemaining < 0.0000001) Phase = SessionPhase.Finished;
                    continue;
                }
                double untilLimit = Rules.enforcePlayLimit ? Rules.playLimit - TotalTime : double.PositiveInfinity;
                if (untilLimit < 0.0000001) { End(SessionPhase.TimedOut); continue; }
                if (Phase == SessionPhase.Blackout)
                {
                    double step = Math.Min(delta, Math.Min(BlackoutRemaining, untilLimit));
                    TotalTime += step; BlackoutRemaining -= step; delta -= step;
                    if (Rules.enforcePlayLimit && TotalTime >= Rules.playLimit - 0.0000001) End(SessionPhase.TimedOut);
                    else if (BlackoutRemaining < 0.0000001) BeginLoop();
                    continue;
                }
                double next = ShotResolved ? Rules.searchShot : Rules.firstShot;
                double slice = Math.Min(delta, Math.Min(next - LoopTime, untilLimit));
                LoopTime += slice; TotalTime += slice; delta -= slice;
                if (Rules.enforcePlayLimit && TotalTime >= Rules.playLimit - 0.0000001) { End(SessionPhase.TimedOut); continue; }
                if (LoopTime >= next - 0.0000001)
                {
                    if (!ShotResolved)
                    {
                        ShotResolved = true;
                        if (ShieldRaised) Record("shot_blocked");
                        else Kill(LoopId, "first_shot");
                    }
                    else Kill(LoopId, "flanked");
                }
            }
        }
    }
}
