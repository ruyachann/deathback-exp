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
        static readonly PlayAreaSettings DefaultSettings = new PlayAreaSettings();

        public static bool ForwardRegionFits(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg)
        {
            return ForwardRegionFits(px, pz, yawDeg, cx, cz, areaYawDeg, DefaultSettings);
        }

        public static bool ForwardRegionFits(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg,
            PlayAreaSettings s)
        {
            ValidateInputs(px, pz, yawDeg, cx, cz, areaYawDeg, s);
            return ForwardRegionFitsCore(px, pz, yawDeg, cx, cz, areaYawDeg, s);
        }

        static bool ForwardRegionFitsCore(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg,
            PlayAreaSettings s)
        {
            double areaAngle = NormalizeYaw(areaYawDeg) * DegreesToRadians;
            double sine = Math.Sin(areaAngle);
            double cosine = Math.Cos(areaAngle);
            double dx = px - cx;
            double dz = pz - cz;
            double localX = dx * cosine - dz * sine;
            double localZ = dx * sine + dz * cosine;
            double halfSize = s.areaSize * 0.5 - s.margin;
            if (!PointFits(localX, localZ, halfSize)) return false;

            double localYaw = NormalizeYaw(yawDeg) - NormalizeYaw(areaYawDeg);
            double arcBoundaryInset = s.reach *
                (1.0 - Math.Cos(s.searchStepDeg * DegreesToRadians * 0.5));
            double insetHalfSize = halfSize - arcBoundaryInset;
            bool insetSamplesFit = true;

            double offset;
            for (offset = -s.halfAngleDeg;
                offset <= s.halfAngleDeg + BoundaryTolerance;
                offset += s.searchStepDeg)
            {
                double angle = (localYaw + offset) * DegreesToRadians;
                double x = localX + Math.Sin(angle) * s.reach;
                double z = localZ + Math.Cos(angle) * s.reach;
                if (!PointFits(x, z, insetHalfSize))
                {
                    insetSamplesFit = false;
                    break;
                }
            }

            if (insetSamplesFit && offset - s.searchStepDeg < s.halfAngleDeg - BoundaryTolerance)
            {
                double endAngle = (localYaw + s.halfAngleDeg) * DegreesToRadians;
                double endX = localX + Math.Sin(endAngle) * s.reach;
                double endZ = localZ + Math.Cos(endAngle) * s.reach;
                insetSamplesFit = PointFits(endX, endZ, insetHalfSize);
            }

            if (insetSamplesFit) return true;

            // The inset samples conservatively cover the arcs between them. A valid
            // sector can still touch the real boundary at an endpoint (notably at
            // the four corners), so inspect the arc's exact axis extrema before
            // rejecting that case.
            return ContinuousArcFits(localX, localZ, localYaw, halfSize, s);
        }

        public static double ChooseFrontYaw(
            double px, double pz, double headYawDeg,
            double cx, double cz, double areaYawDeg,
            out bool fits)
        {
            return ChooseFrontYaw(
                px, pz, headYawDeg, cx, cz, areaYawDeg, DefaultSettings, out fits);
        }

        public static double ChooseFrontYaw(
            double px, double pz, double headYawDeg,
            double cx, double cz, double areaYawDeg,
            PlayAreaSettings s,
            out bool fits)
        {
            ValidateInputs(px, pz, headYawDeg, cx, cz, areaYawDeg, s);

            if (ForwardRegionFitsCore(px, pz, headYawDeg, cx, cz, areaYawDeg, s))
            {
                fits = true;
                return NormalizeYaw(headYawDeg);
            }

            for (double offset = s.searchStepDeg; offset < 180.0; offset += s.searchStepDeg)
            {
                double positive = headYawDeg + offset;
                if (ForwardRegionFitsCore(px, pz, positive, cx, cz, areaYawDeg, s))
                {
                    fits = true;
                    return NormalizeYaw(positive);
                }

                double negative = headYawDeg - offset;
                if (ForwardRegionFitsCore(px, pz, negative, cx, cz, areaYawDeg, s))
                {
                    fits = true;
                    return NormalizeYaw(negative);
                }
            }

            double opposite = headYawDeg + 180.0;
            if (ForwardRegionFitsCore(px, pz, opposite, cx, cz, areaYawDeg, s))
            {
                fits = true;
                return NormalizeYaw(opposite);
            }

            fits = false;
            return NormalizeYaw(headYawDeg);
        }

        static bool ContinuousArcFits(
            double localX, double localZ, double localYaw, double halfSize,
            PlayAreaSettings s)
        {
            double start = localYaw - s.halfAngleDeg;
            double end = localYaw + s.halfAngleDeg;
            if (!ArcPointFits(localX, localZ, start, halfSize, s.reach) ||
                !ArcPointFits(localX, localZ, end, halfSize, s.reach)) return false;

            int firstQuarterTurn = (int)Math.Ceiling(start / 90.0);
            int lastQuarterTurn = (int)Math.Floor(end / 90.0);
            for (int quarterTurn = firstQuarterTurn;
                quarterTurn <= lastQuarterTurn;
                quarterTurn++)
            {
                if (!ArcPointFits(localX, localZ, quarterTurn * 90.0, halfSize, s.reach))
                    return false;
            }

            return true;
        }

        static bool ArcPointFits(
            double localX, double localZ, double angleDeg, double halfSize,
            double reach)
        {
            double angle = angleDeg * DegreesToRadians;
            double x = localX + Math.Sin(angle) * reach;
            double z = localZ + Math.Cos(angle) * reach;
            return PointFits(x, z, halfSize);
        }

        static bool PointFits(double localX, double localZ, double halfSize)
        {
            return Math.Abs(localX) <= halfSize + BoundaryTolerance &&
                Math.Abs(localZ) <= halfSize + BoundaryTolerance;
        }

        static void ValidateInputs(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg,
            PlayAreaSettings s)
        {
            RequireFinite(px, nameof(px));
            RequireFinite(pz, nameof(pz));
            RequireFinite(yawDeg, nameof(yawDeg));
            RequireFinite(cx, nameof(cx));
            RequireFinite(cz, nameof(cz));
            RequireFinite(areaYawDeg, nameof(areaYawDeg));
            if (s == null) throw new ArgumentNullException(nameof(s));
            s.Validate();
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
