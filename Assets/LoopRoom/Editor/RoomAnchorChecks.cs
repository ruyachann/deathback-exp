using System;
using System.Collections.Generic;
using LoopRoom;

public static class RoomAnchorChecks
{
    public static List<string> Run()
    {
        var passed = new List<string>();

        Check(passed, "Center fits without correction for every sampled yaw", () =>
        {
            for (double yaw = 0; yaw < 360; yaw += RoomAnchor.SearchStepDeg)
            {
                Need(RoomAnchor.ForwardRegionFits(0, 0, yaw, 0, 0, 0), "center rejected at " + yaw);
                bool fits;
                double chosen = RoomAnchor.ChooseFrontYaw(0, 0, yaw, 0, 0, 0, out fits);
                Need(fits, "center choice failed at " + yaw);
                Near(chosen, yaw);
            }
        });

        Check(passed, "Boundary outward yaw is corrected by the minimum search step", () =>
        {
            bool fits;
            double chosen = RoomAnchor.ChooseFrontYaw(0, 0.7, 0, 0, 0, 0, out fits);
            Need(fits, "no boundary direction found");
            Near(chosen, 135);
            Need(RoomAnchor.ForwardRegionFits(0, 0.7, chosen, 0, 0, 0), "chosen direction does not fit");
            Need(!RoomAnchor.ForwardRegionFits(0, 0.7, chosen - RoomAnchor.SearchStepDeg, 0, 0, 0),
                "one closer step unexpectedly fits");
        });

        Check(passed, "All four corners fit in a center-facing direction", () =>
        {
            double[] signs = { -1, 1 };
            foreach (double xSign in signs)
            foreach (double zSign in signs)
            {
                double x = xSign * 0.7;
                double z = zSign * 0.7;
                double centerYaw = NormalizeYaw(Math.Atan2(-x, -z) * 180.0 / Math.PI);
                bool fits;
                double chosen = RoomAnchor.ChooseFrontYaw(x, z, 0, 0, 0, 0, out fits);
                Need(fits, "corner direction not found at " + x + ", " + z);
                Need(RoomAnchor.ForwardRegionFits(x, z, chosen, 0, 0, 0), "corner choice does not fit");
                Near(AngularDistance(chosen, centerYaw), 0);
            }
        });

        Check(passed, "Rotated safety area is evaluated in area coordinates", () =>
        {
            double areaYaw = 30;
            double angle = areaYaw * Math.PI / 180.0;
            double localX = 0.7;
            double localZ = 0.7;
            double x = localX * Math.Cos(angle) + localZ * Math.Sin(angle);
            double z = -localX * Math.Sin(angle) + localZ * Math.Cos(angle);
            double centerYaw = Math.Atan2(-x, -z) * 180.0 / Math.PI;
            Need(RoomAnchor.ForwardRegionFits(x, z, centerYaw, 0, 0, areaYaw),
                "rotated non-symmetric corner rejected");
        });

        Check(passed, "Position outside the safety area returns the head yaw", () =>
        {
            bool fits;
            double chosen = RoomAnchor.ChooseFrontYaw(1.0, 0, 123, 0, 0, 0, out fits);
            Need(!fits, "outside position found a fitting direction");
            Near(chosen, 123);

            chosen = RoomAnchor.ChooseFrontYaw(1.0, 0, -30, 0, 0, 0, out fits);
            Need(!fits, "outside position with negative yaw found a fitting direction");
            Near(chosen, 330);

            chosen = RoomAnchor.ChooseFrontYaw(1.0, 0, 750, 0, 0, 0, out fits);
            Need(!fits, "outside position with large yaw found a fitting direction");
            Near(chosen, 30);
        });

        Check(passed, "Chosen yaw is normalized into zero through 360", () =>
        {
            bool fits;
            Near(RoomAnchor.ChooseFrontYaw(0, 0, -30, 0, 0, 0, out fits), 330);
            Need(fits, "negative center yaw did not fit");
            Near(RoomAnchor.ChooseFrontYaw(0, 0, 750, 0, 0, 0, out fits), 30);
            Need(fits, "large center yaw did not fit");
        });

        Check(passed, "Choosing a front yaw is deterministic", () =>
        {
            bool expectedFits;
            double expected = RoomAnchor.ChooseFrontYaw(0.7, 0.7, 0, 0, 0, 0, out expectedFits);
            for (int i = 0; i < 20; i++)
            {
                bool actualFits;
                double actual = RoomAnchor.ChooseFrontYaw(0.7, 0.7, 0, 0, 0, 0, out actualFits);
                Need(actual == expected && actualFits == expectedFits, "result changed on repetition " + i);
            }
        });

        Check(passed, "Non-finite public inputs throw ArgumentException", () =>
        {
            double[] invalidValues =
            {
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity
            };

            foreach (double invalid in invalidValues)
            {
                ForwardRejects(invalid, 0, 0, 0, 0, 0, "px");
                ForwardRejects(0, invalid, 0, 0, 0, 0, "pz");
                ForwardRejects(0, 0, invalid, 0, 0, 0, "yaw");
                ForwardRejects(0, 0, 0, invalid, 0, 0, "cx");
                ForwardRejects(0, 0, 0, 0, invalid, 0, "cz");
                ForwardRejects(0, 0, 0, 0, 0, invalid, "area yaw");

                ChooseRejects(invalid, 0, 0, 0, 0, 0, "px");
                ChooseRejects(0, invalid, 0, 0, 0, 0, "pz");
                ChooseRejects(0, 0, invalid, 0, 0, 0, "head yaw");
                ChooseRejects(0, 0, 0, invalid, 0, 0, "cx");
                ChooseRejects(0, 0, 0, 0, invalid, 0, "cz");
                ChooseRejects(0, 0, 0, 0, 0, invalid, "area yaw");
            }
        });

        Check(passed, "Unsampled arc bulge outside the boundary is rejected", () =>
        {
            double halfStep = RoomAnchor.SearchStepDeg * 0.5;
            double pz = RoomAnchor.SafeHalfSize - RoomAnchor.Margin -
                RoomAnchor.Reach * Math.Cos(halfStep * Math.PI / 180.0);
            Need(!RoomAnchor.ForwardRegionFits(0, pz, -halfStep, 0, 0, 0),
                "arc between sampled points crossed the boundary without rejection");
        });

        Check(passed, "Default settings overloads match the constant API", () =>
        {
            var settings = new PlayAreaSettings();
            double[,] cases =
            {
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0.7, 0, 0, 0, 0 },
                { 0.7, 0.7, 210, 0, 0, 0 },
                { 1.0, 0, 123, 0, 0, 0 },
                { 0.2, -0.1, 725, 0.1, -0.2, 30 }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                double px = cases[i, 0];
                double pz = cases[i, 1];
                double yaw = cases[i, 2];
                double cx = cases[i, 3];
                double cz = cases[i, 4];
                double areaYaw = cases[i, 5];
                bool legacyRegion = RoomAnchor.ForwardRegionFits(px, pz, yaw, cx, cz, areaYaw);
                bool settingsRegion = RoomAnchor.ForwardRegionFits(
                    px, pz, yaw, cx, cz, areaYaw, settings);
                Need(legacyRegion == settingsRegion, "region result changed for case " + i);

                bool legacyFits;
                bool settingsFits;
                double legacyYaw = RoomAnchor.ChooseFrontYaw(
                    px, pz, yaw, cx, cz, areaYaw, out legacyFits);
                double settingsYaw = RoomAnchor.ChooseFrontYaw(
                    px, pz, yaw, cx, cz, areaYaw, settings, out settingsFits);
                Need(legacyFits == settingsFits, "fit result changed for case " + i);
                Near(settingsYaw, legacyYaw);
            }
        });

        Check(passed, "Larger configured area avoids an unnecessary correction", () =>
        {
            var larger = new PlayAreaSettings { areaSize = 2.4 };
            Need(!RoomAnchor.ForwardRegionFits(0, 0.6, 0, 0, 0, 0),
                "default area unexpectedly accepted the outward direction");
            Need(RoomAnchor.ForwardRegionFits(0, 0.6, 0, 0, 0, 0, larger),
                "larger area rejected the outward direction");

            bool defaultFits;
            bool largerFits;
            double defaultYaw = RoomAnchor.ChooseFrontYaw(0, 0.6, 0, 0, 0, 0, out defaultFits);
            double largerYaw = RoomAnchor.ChooseFrontYaw(0, 0.6, 0, 0, 0, 0, larger, out largerFits);
            Need(defaultFits && largerFits, "a fitting direction was not found");
            Need(AngularDistance(defaultYaw, 0) > 0, "default area did not require correction");
            Near(largerYaw, 0);
        });

        Check(passed, "Play area settings reject invalid ranges and non-finite values", () =>
        {
            new PlayAreaSettings
            {
                areaSize = 0.5,
                margin = 0.249,
                reach = 0.1,
                halfAngleDeg = 5,
                searchStepDeg = 1
            }.Validate();
            new PlayAreaSettings
            {
                areaSize = 4,
                margin = 1.999,
                reach = 1,
                halfAngleDeg = 90,
                searchStepDeg = 45
            }.Validate();

            Action<PlayAreaSettings>[] invalidRanges =
            {
                s => s.areaSize = 0.499,
                s => s.areaSize = 4.001,
                s => s.margin = -0.001,
                s => s.margin = s.areaSize * 0.5,
                s => s.reach = 0.099,
                s => s.halfAngleDeg = 4.999,
                s => s.halfAngleDeg = 90.001,
                s => s.searchStepDeg = 0.999,
                s => s.searchStepDeg = 45.001
            };
            foreach (Action<PlayAreaSettings> makeInvalid in invalidRanges)
                SettingsRejects(makeInvalid, "out-of-range settings were accepted");

            Action<PlayAreaSettings, double>[] setters =
            {
                (s, value) => s.areaSize = value,
                (s, value) => s.margin = value,
                (s, value) => s.reach = value,
                (s, value) => s.halfAngleDeg = value,
                (s, value) => s.searchStepDeg = value
            };
            double[] nonFinite =
            {
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity
            };
            foreach (Action<PlayAreaSettings, double> setter in setters)
            foreach (double value in nonFinite)
                SettingsRejects(s => setter(s, value), "non-finite settings were accepted");
        });

        return passed;
    }

    static void SettingsRejects(Action<PlayAreaSettings> makeInvalid, string message)
    {
        var settings = new PlayAreaSettings();
        makeInvalid(settings);
        ThrowsArgumentException(settings.Validate, message);
    }

    static void ForwardRejects(
        double px, double pz, double yaw,
        double cx, double cz, double areaYaw,
        string label)
    {
        ThrowsArgumentException(
            () => RoomAnchor.ForwardRegionFits(px, pz, yaw, cx, cz, areaYaw),
            "ForwardRegionFits accepted non-finite " + label);
    }

    static void ChooseRejects(
        double px, double pz, double yaw,
        double cx, double cz, double areaYaw,
        string label)
    {
        ThrowsArgumentException(() =>
        {
            bool fits;
            RoomAnchor.ChooseFrontYaw(px, pz, yaw, cx, cz, areaYaw, out fits);
        }, "ChooseFrontYaw accepted non-finite " + label);
    }

    static void ThrowsArgumentException(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }

        throw new Exception(message);
    }

    static double NormalizeYaw(double yaw)
    {
        double normalized = yaw % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    static double AngularDistance(double a, double b)
    {
        double difference = Math.Abs(NormalizeYaw(a) - NormalizeYaw(b));
        return Math.Min(difference, 360.0 - difference);
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
}
