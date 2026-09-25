using System;
using System.Collections.Generic;
using LoopRoom;

public static class FrameStatsChecks
{
    public static List<string> Run()
    {
        var passed = new List<string>();

        Check(passed, "Constant dt yields correct mean, P95 within one bucket, and max", () =>
        {
            var stats = new FrameStats(60);
            double dtSeconds = 1.0 / 60.0;
            double expectedMs = dtSeconds * 1000.0;
            for (int i = 0; i < 200; i++) stats.Add(dtSeconds);
            Need(stats.Frames == 200, "frame count");
            Near(stats.MeanMs, expectedMs);
            Near(stats.MaxMs, expectedMs);
            Need(stats.P95Ms >= expectedMs && stats.P95Ms < expectedMs + 0.25,
                "P95 not within one bucket of the constant value");
        });

        Check(passed, "Only dt exceeding 1.5x the target frame time counts as dropped", () =>
        {
            var stats = new FrameStats(60); // target frame time ~16.667ms, drop threshold ~25ms
            stats.Add(0.0249); // 24.9ms: below threshold
            stats.Add(0.0251); // 25.1ms: exceeds threshold
            stats.Add(0.5);    // 500ms: exceeds threshold
            Need(stats.Frames == 3, "frame count");
            Need(stats.Dropped == 2, "dropped count");
        });

        Check(passed, "Non-finite and negative dt are ignored", () =>
        {
            var stats = new FrameStats(60);
            stats.Add(double.NaN); stats.Add(double.PositiveInfinity);
            stats.Add(double.NegativeInfinity); stats.Add(-0.001);
            Need(stats.Frames == 0, "ignored values counted as frames");
            Near(stats.MeanMs, 0); Near(stats.MaxMs, 0); Near(stats.P95Ms, 0);
            Need(stats.Dropped == 0, "dropped counted from ignored values");
        });

        Check(passed, "Reset returns to an empty state", () =>
        {
            var stats = new FrameStats(60);
            for (int i = 0; i < 10; i++) stats.Add(0.02);
            stats.Reset();
            Need(stats.Frames == 0, "frames after reset");
            Near(stats.MeanMs, 0); Near(stats.MaxMs, 0); Near(stats.P95Ms, 0);
            Need(stats.Dropped == 0, "dropped after reset");
            double dtSeconds = 1.0 / 60.0;
            stats.Add(dtSeconds);
            Need(stats.Frames == 1, "frames after reset then add");
            Near(stats.MeanMs, dtSeconds * 1000.0);
        });

        Check(passed, "Invalid target Hz throws ArgumentException", () =>
        {
            double[] invalid = { 0, -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            foreach (double value in invalid)
            {
                bool bad = false;
                try { new FrameStats(value); } catch (ArgumentException) { bad = true; }
                Need(bad, "invalid targetHz " + value + " accepted");
            }
            var ok = new FrameStats(72);
            Near(ok.TargetHz, 72);
        });

        Check(passed, "Target Hz outside [1, 1000] throws ArgumentException", () =>
        {
            // Range restriction keeps the dropped-threshold well below the 3600s saturation
            // cap so an extremely low targetHz can no longer under-count Dropped (task026 追修正3-1).
            double[] outOfRange = { 0.999, 1000.001, 1.0 / 3600.0, 0, -1 };
            foreach (double value in outOfRange)
            {
                bool bad = false;
                try { new FrameStats(value); } catch (ArgumentException) { bad = true; }
                Need(bad, "out-of-range targetHz " + value + " accepted");
            }
            var lowEdge = new FrameStats(1);
            Near(lowEdge.TargetHz, 1);
            var highEdge = new FrameStats(1000);
            Near(highEdge.TargetHz, 1000);
        });

        Check(passed, "Zero frames report zero for every value", () =>
        {
            var stats = new FrameStats(90);
            Need(stats.Frames == 0, "frames");
            Near(stats.MeanMs, 0); Near(stats.P95Ms, 0); Near(stats.MaxMs, 0);
            Need(stats.Dropped == 0, "dropped");
        });

        Check(passed, "P95 lands in the extended coarse bucket when >100ms frames exceed 5%", () =>
        {
            var stats = new FrameStats(60);
            for (int i = 0; i < 94; i++) stats.Add(0.05); // 50ms, fine bucket
            for (int i = 0; i < 6; i++) stats.Add(0.2);   // 200ms, coarse bucket; 6% > 5%
            Need(stats.Frames == 100, "frame count");
            Need(stats.P95Ms >= 200 && stats.P95Ms <= 210,
                "P95 should land in the 200ms coarse bucket, not the overall max");
        });

        Check(passed, "A single extreme outlier does not pull P95 toward the max", () =>
        {
            var stats = new FrameStats(60);
            for (int i = 0; i < 99; i++) stats.Add(0.016); // 16ms, typical frame
            stats.Add(10.0);                               // 10000ms, one outlier
            Need(stats.Frames == 100, "frame count");
            Near(stats.MaxMs, 10000);
            Need(stats.P95Ms < 20, "P95 should stay near the common 16ms value, not the outlier");
        });

        Check(passed, "A huge finite dt does not throw and saturates into the last bucket", () =>
        {
            var stats = new FrameStats(60);
            stats.Add(1.0e10); // absurdly large but finite seconds
            stats.Add(0.016);
            Need(stats.Frames == 2, "frame count");
            Need(stats.Dropped == 1, "the huge dt should count as dropped");
            Need(!double.IsNaN(stats.P95Ms) && !double.IsInfinity(stats.P95Ms), "P95 should stay finite");
            Need(stats.P95Ms <= 1000.0001, "P95 should saturate at the last bucket's 1000ms edge");
        });

        Check(passed, "Binary-exact 1.5x boundary: at, just below, and just above are checked separately", () =>
        {
            // 128 Hz makes the frame time (1000/128 = 7.8125ms) and its 1.5x (11.71875ms)
            // both exact binary fractions, and 3.0/256.0 seconds converts to exactly
            // 11.71875ms, so this dt hits the threshold with no double-rounding (task026 追修正2-2).
            double atBoundarySeconds = 3.0 / 256.0;

            var below = new FrameStats(128);
            below.Add(NextDown(atBoundarySeconds));
            Need(below.Dropped == 0, "just below the 1.5x boundary should not be dropped");

            var at = new FrameStats(128);
            at.Add(atBoundarySeconds);
            Need(at.Dropped == 0, "exactly at the 1.5x boundary should not be dropped");

            var above = new FrameStats(128);
            above.Add(NextUp(atBoundarySeconds));
            Need(above.Dropped == 1, "just above the 1.5x boundary should be dropped");
        });

        Check(passed, "double.MaxValue keeps MeanMs and MaxMs finite", () =>
        {
            var stats = new FrameStats(60);
            stats.Add(double.MaxValue);
            stats.Add(0.016);
            Need(stats.Frames == 2, "frame count");
            Need(!double.IsNaN(stats.MeanMs) && !double.IsInfinity(stats.MeanMs), "MeanMs should stay finite");
            Need(!double.IsNaN(stats.MaxMs) && !double.IsInfinity(stats.MaxMs), "MaxMs should stay finite");
            Need(stats.Dropped == 1, "the saturated huge dt should still count as dropped");
        });

        return passed;
    }

    static void Check(List<string> list, string name, Action test)
    {
        test();
        list.Add("PASS: " + name);
    }

    static void Need(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Near(double actual, double expected)
    {
        Need(Math.Abs(actual - expected) < 0.00001, "Expected " + actual + " ~ " + expected);
    }

    // Math.BitDecrement/BitIncrement are unavailable on the .NET Framework 4 mscorlib
    // used to compile the tests, so the adjacent double is derived from its bit pattern
    // instead. Only positive, finite inputs need to be supported here (task026 追修正3).
    static double NextUp(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        return BitConverter.Int64BitsToDouble(bits + 1);
    }

    static double NextDown(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        return BitConverter.Int64BitsToDouble(bits - 1);
    }
}
