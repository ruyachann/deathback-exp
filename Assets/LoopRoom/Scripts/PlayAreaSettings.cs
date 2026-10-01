using System;

namespace LoopRoom
{
    [Serializable]
    public sealed partial class PlayAreaSettings
    {
        public double areaSize = 1.6;
        public double margin = 0.10;
        public double reach = 0.45;
        public double halfAngleDeg = 45;
        public double searchStepDeg = 15;

        public void Validate()
        {
            RequireFinite(areaSize, nameof(areaSize));
            RequireFinite(margin, nameof(margin));
            RequireFinite(reach, nameof(reach));
            RequireFinite(halfAngleDeg, nameof(halfAngleDeg));
            RequireFinite(searchStepDeg, nameof(searchStepDeg));

            if (areaSize < 0.5 || areaSize > 4.0)
                throw new ArgumentException("areaSize must be between 0.5 and 4 meters.", nameof(areaSize));
            if (margin < 0.0 || margin >= areaSize * 0.5)
                throw new ArgumentException("margin must be non-negative and less than half of areaSize.", nameof(margin));
            if (reach < 0.1)
                throw new ArgumentException("reach must be at least 0.1 meters.", nameof(reach));
            if (halfAngleDeg < 5.0 || halfAngleDeg > 90.0)
                throw new ArgumentException("halfAngleDeg must be between 5 and 90 degrees.", nameof(halfAngleDeg));
            if (searchStepDeg < 1.0 || searchStepDeg > 45.0)
                throw new ArgumentException("searchStepDeg must be between 1 and 45 degrees.", nameof(searchStepDeg));
        }

        static void RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Value must be finite.", parameterName);
        }
    }
}
