using System;
using LoopRoom;

internal static class Program
{
    private static int Main()
    {
        try
        {
            var existing = LoopModelChecks.Run();
            Require(existing.Count == 19, "Expected 19 existing checks.");
            foreach (var result in existing) Console.WriteLine(result);
            var model = new LoopModel(new LoopRules { enforcePlayLimit = true }); model.Start(); model.Advance(172);
            Require(model.Phase == SessionPhase.TimedOut, "Timeout at 172 seconds.");
            Require(model.Outcome == SessionPhase.TimedOut, "Retain timeout outcome.");
            Near(model.TotalTime, 172);
            model.Advance(8);
            Require(model.Phase == SessionPhase.Finished, "Finish at 180 seconds.");
            Require(model.Outcome == SessionPhase.TimedOut, "Finished retains outcome.");
            Near(model.TotalTime, 180);
            int records = model.Records.Count; model.Advance(1000);
            Near(model.TotalTime, 180);
            Require(model.Records.Count == records, "Finished stops recording.");
            Console.WriteLine("PASS: No-action deadline and retained outcome at 180 seconds");
            Console.WriteLine("PASS: 20 model checks");
            var roomAnchor = RoomAnchorChecks.Run();
            Require(roomAnchor.Count == 12, "Expected 12 room anchor checks.");
            foreach (var result in roomAnchor) Console.WriteLine(result);
            Console.WriteLine("PASS: 12 room anchor checks");
            var frameStats = FrameStatsChecks.Run();
            Require(frameStats.Count == 12, "Expected 12 frame stats checks.");
            foreach (var result in frameStats) Console.WriteLine(result);
            Console.WriteLine("PASS: 12 frame stats checks"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL: " + error); return 1; }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Near(double actual, double expected)
    { Require(Math.Abs(actual - expected) < 0.00001, "Expected " + actual + " ~ " + expected); }
}
