using System;

namespace LoopRoom
{
    public static class RoomAnchor
    {
        public const double SafeHalfSize = 0.8;
        public const double Margin = 0.10;
        public const double Reach = 0.45;
        public const double HalfAngleDeg = 45;
        public const double SearchStepDeg = 15;

        const double BoundaryTolerance = 1e-9;
        const double DegreesToRadians = Math.PI / 180.0;
        static readonly double ArcBoundaryInset = Reach *
            (1.0 - Math.Cos(SearchStepDeg * DegreesToRadians * 0.5));

        public static bool ForwardRegionFits(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg)
        {
            ValidateInputs(px, pz, yawDeg, cx, cz, areaYawDeg);
            return ForwardRegionFitsCore(px, pz, yawDeg, cx, cz, areaYawDeg);
        }

        static bool ForwardRegionFitsCore(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg)
        {
            double areaAngle = NormalizeYaw(areaYawDeg) * DegreesToRadians;
            double sine = Math.Sin(areaAngle);
            double cosine = Math.Cos(areaAngle);
            double dx = px - cx;
            double dz = pz - cz;
            double localX = dx * cosine - dz * sine;
            double localZ = dx * sine + dz * cosine;
            double halfSize = SafeHalfSize - Margin;
            if (!PointFits(localX, localZ, halfSize)) return false;

            double localYaw = NormalizeYaw(yawDeg) - NormalizeYaw(areaYawDeg);
            double insetHalfSize = halfSize - ArcBoundaryInset;
            bool insetSamplesFit = true;

            for (double offset = -HalfAngleDeg;
                offset <= HalfAngleDeg + BoundaryTolerance;
                offset += SearchStepDeg)
            {
                double angle = (localYaw + offset) * DegreesToRadians;
                double x = localX + Math.Sin(angle) * Reach;
                double z = localZ + Math.Cos(angle) * Reach;
                if (!PointFits(x, z, insetHalfSize))
                {
                    insetSamplesFit = false;
                    break;
                }
            }

            if (insetSamplesFit) return true;

            // The inset samples conservatively cover the arcs between them. A valid
            // sector can still touch the real boundary at an endpoint (notably at
            // the four corners), so inspect the arc's exact axis extrema before
            // rejecting that case.
            return ContinuousArcFits(localX, localZ, localYaw, halfSize);
        }

        public static double ChooseFrontYaw(
            double px, double pz, double headYawDeg,
            double cx, double cz, double areaYawDeg,
            out bool fits)
        {
            ValidateInputs(px, pz, headYawDeg, cx, cz, areaYawDeg);

            if (ForwardRegionFitsCore(px, pz, headYawDeg, cx, cz, areaYawDeg))
            {
                fits = true;
                return NormalizeYaw(headYawDeg);
            }

            for (double offset = SearchStepDeg; offset < 180.0; offset += SearchStepDeg)
            {
                double positive = headYawDeg + offset;
                if (ForwardRegionFitsCore(px, pz, positive, cx, cz, areaYawDeg))
                {
                    fits = true;
                    return NormalizeYaw(positive);
                }

                double negative = headYawDeg - offset;
                if (ForwardRegionFitsCore(px, pz, negative, cx, cz, areaYawDeg))
                {
                    fits = true;
                    return NormalizeYaw(negative);
                }
            }

            double opposite = headYawDeg + 180.0;
            if (ForwardRegionFitsCore(px, pz, opposite, cx, cz, areaYawDeg))
            {
                fits = true;
                return NormalizeYaw(opposite);
            }

            fits = false;
            return NormalizeYaw(headYawDeg);
        }

        static bool ContinuousArcFits(
            double localX, double localZ, double localYaw, double halfSize)
        {
            double start = localYaw - HalfAngleDeg;
            double end = localYaw + HalfAngleDeg;
            if (!ArcPointFits(localX, localZ, start, halfSize) ||
                !ArcPointFits(localX, localZ, end, halfSize)) return false;

            int firstQuarterTurn = (int)Math.Ceiling(start / 90.0);
            int lastQuarterTurn = (int)Math.Floor(end / 90.0);
            for (int quarterTurn = firstQuarterTurn;
                quarterTurn <= lastQuarterTurn;
                quarterTurn++)
            {
                if (!ArcPointFits(localX, localZ, quarterTurn * 90.0, halfSize))
                    return false;
            }

            return true;
        }

        static bool ArcPointFits(
            double localX, double localZ, double angleDeg, double halfSize)
        {
            double angle = angleDeg * DegreesToRadians;
            double x = localX + Math.Sin(angle) * Reach;
            double z = localZ + Math.Cos(angle) * Reach;
            return PointFits(x, z, halfSize);
        }

        static bool PointFits(double localX, double localZ, double halfSize)
        {
            return Math.Abs(localX) <= halfSize + BoundaryTolerance &&
                Math.Abs(localZ) <= halfSize + BoundaryTolerance;
        }

        static void ValidateInputs(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg)
        {
            RequireFinite(px, nameof(px));
            RequireFinite(pz, nameof(pz));
            RequireFinite(yawDeg, nameof(yawDeg));
            RequireFinite(cx, nameof(cx));
            RequireFinite(cz, nameof(cz));
            RequireFinite(areaYawDeg, nameof(areaYawDeg));
        }

        static void RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Value must be finite.", parameterName);
        }

        static double NormalizeYaw(double yawDeg)
        {
            double normalized = yawDeg % 360.0;
            return normalized < 0.0 ? normalized + 360.0 : normalized;
        }
    }
}
