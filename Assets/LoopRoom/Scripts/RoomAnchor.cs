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

        public static bool ForwardRegionFits(
            double px, double pz, double yawDeg,
            double cx, double cz, double areaYawDeg)
        {
            if (!PointFits(px, pz, cx, cz, areaYawDeg)) return false;

            for (double offset = -HalfAngleDeg;
                offset <= HalfAngleDeg + BoundaryTolerance;
                offset += SearchStepDeg)
            {
                double angle = (yawDeg + offset) * DegreesToRadians;
                double x = px + Math.Sin(angle) * Reach;
                double z = pz + Math.Cos(angle) * Reach;
                if (!PointFits(x, z, cx, cz, areaYawDeg)) return false;
            }

            return true;
        }

        public static double ChooseFrontYaw(
            double px, double pz, double headYawDeg,
            double cx, double cz, double areaYawDeg,
            out bool fits)
        {
            if (ForwardRegionFits(px, pz, headYawDeg, cx, cz, areaYawDeg))
            {
                fits = true;
                return NormalizeYaw(headYawDeg);
            }

            for (double offset = SearchStepDeg; offset < 180.0; offset += SearchStepDeg)
            {
                double positive = headYawDeg + offset;
                if (ForwardRegionFits(px, pz, positive, cx, cz, areaYawDeg))
                {
                    fits = true;
                    return NormalizeYaw(positive);
                }

                double negative = headYawDeg - offset;
                if (ForwardRegionFits(px, pz, negative, cx, cz, areaYawDeg))
                {
                    fits = true;
                    return NormalizeYaw(negative);
                }
            }

            double opposite = headYawDeg + 180.0;
            if (ForwardRegionFits(px, pz, opposite, cx, cz, areaYawDeg))
            {
                fits = true;
                return NormalizeYaw(opposite);
            }

            fits = false;
            return NormalizeYaw(headYawDeg);
        }

        static bool PointFits(
            double x, double z, double cx, double cz, double areaYawDeg)
        {
            double angle = areaYawDeg * DegreesToRadians;
            double sine = Math.Sin(angle);
            double cosine = Math.Cos(angle);
            double dx = x - cx;
            double dz = z - cz;
            double localX = dx * cosine - dz * sine;
            double localZ = dx * sine + dz * cosine;
            double halfSize = SafeHalfSize - Margin;
            return Math.Abs(localX) <= halfSize + BoundaryTolerance &&
                Math.Abs(localZ) <= halfSize + BoundaryTolerance;
        }

        static double NormalizeYaw(double yawDeg)
        {
            double normalized = yawDeg % 360.0;
            return normalized < 0.0 ? normalized + 360.0 : normalized;
        }
    }
}
