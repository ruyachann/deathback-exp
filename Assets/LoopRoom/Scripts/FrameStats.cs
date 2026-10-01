using System;

namespace LoopRoom
{
    // Pure C# (no UnityEngine dependency) so it runs unchanged in Tests/LoopModel.Tests.
    // Keeps a fixed-size histogram instead of every frame's dt, so memory stays constant
    // no matter how long a session runs.
    public sealed class FrameStats
    {
        const double FineWidthMs = 0.25;
        const int FineBucketCount = 400; // covers [0,100)ms in 0.25ms steps
        const double CoarseWidthMs = 10.0;
        const int CoarseBucketCount = 90; // covers [100,1000)ms in 10ms steps
        // Total buckets: fine + coarse. The last bucket's upper edge is 1000ms; any dt at or
        // above 1000ms (including huge/near-overflow finite values) saturates into it instead
        // of computing an index from the raw value (task026 追修正1/4).
        const int BucketCount = FineBucketCount + CoarseBucketCount;
        // Seconds are saturated to this cap before the ms conversion, so even
        // double.MaxValue keeps sum/max/mean finite (task026 追修正2-1).
        const double MaxSeconds = 3600.0;
        readonly long[] buckets = new long[BucketCount];
        readonly double targetFrameMs;
        readonly double droppedThresholdMs;
        double sumMs, maxMs;

        public double TargetHz { get; }
        public long Frames { get; private set; }
        public long Dropped { get; private set; }
        public double MeanMs => Frames == 0 ? 0 : sumMs / Frames;
        public double MaxMs => Frames == 0 ? 0 : maxMs;
        public double P95Ms => ComputeP95();

        public FrameStats(double targetHz)
        {
            // Range restricted to 1-1000Hz (actual displays are ~60-165Hz) so the
            // dropped-frame threshold (up to 1500ms) always stays well below the
            // 3600s/3600000ms saturation cap used for mean/max/histogram (task026 追修正3-1).
            if (double.IsNaN(targetHz) || double.IsInfinity(targetHz) || targetHz < 1.0 || targetHz > 1000.0)
                throw new ArgumentException("targetHz must be finite and within [1, 1000]", nameof(targetHz));
            TargetHz = targetHz;
            targetFrameMs = 1000.0 / targetHz;
            // Computed once here so Add and the tests compare against the exact same
            // threshold value (task026 追修正2-2).
            droppedThresholdMs = targetFrameMs * 1.5;
        }

        public void Add(double dtSeconds)
        {
            if (double.IsNaN(dtSeconds) || double.IsInfinity(dtSeconds) || dtSeconds < 0) return;
            double clampedSeconds = dtSeconds > MaxSeconds ? MaxSeconds : dtSeconds;
            double ms = clampedSeconds * 1000.0;
            Frames++;
            sumMs += ms;
            if (ms > maxMs) maxMs = ms;
            // Branch on the time value itself before any index arithmetic, so a huge (or
            // overflowed-to-infinity) ms never gets cast/divided into an out-of-range int
            // (task026 追修正4: the old single division could yield a negative index).
            int index;
            if (ms < 100.0) index = (int)(ms / FineWidthMs);
            else if (ms < 1000.0) index = FineBucketCount + (int)((ms - 100.0) / CoarseWidthMs);
            else index = BucketCount - 1;
            if (index >= BucketCount) index = BucketCount - 1;
            buckets[index]++;
            if (ms > droppedThresholdMs) Dropped++;
        }

        public void Reset()
        {
            Frames = 0; Dropped = 0; sumMs = 0; maxMs = 0;
            Array.Clear(buckets, 0, buckets.Length);
        }

        // P95 is approximated by the upper edge of the bucket containing the 95th-percentile
        // frame, so the error stays within one bucket width. Every bucket (including the last)
        // has a fixed upper edge, so P95 never falls back to the observed max (task026 追修正1).
        double ComputeP95()
        {
            if (Frames == 0) return 0;
            long target = (long)Math.Ceiling(Frames * 0.95);
            if (target < 1) target = 1;
            long cumulative = 0;
            for (int i = 0; i < BucketCount; i++)
            {
                cumulative += buckets[i];
                if (cumulative >= target) return BucketUpperEdgeMs(i);
            }
            return BucketUpperEdgeMs(BucketCount - 1);
        }

        static double BucketUpperEdgeMs(int index) => index < FineBucketCount
            ? (index + 1) * FineWidthMs
            : 100.0 + (index - FineBucketCount + 1) * CoarseWidthMs;
    }
}
