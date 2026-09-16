namespace BridgeIL.Core
{
    /// <summary>
    /// AASHTO LRFD HL-93 live load definition.
    /// All internal values stored in SI (kN, m).
    /// Input and output in US customary (kip, ft).
    /// </summary>
    public static class AASHTOLoads
    {
        // Design truck axle loads — stored in kN internally
        private static readonly double _frontAxle  = Units.KipsToKiloNewtons(8.0);
        private static readonly double _middleAxle = Units.KipsToKiloNewtons(32.0);
        private static readonly double _rearAxle   = Units.KipsToKiloNewtons(32.0);

        // Front-to-middle axle spacing — fixed at 14 ft, stored in metres internally
        private static readonly double _frontToMiddle = Units.FeetToMetres(14.0);

        // Lane load — stored in kN/m internally
        private static readonly double _laneLoad = Units.KipsPerFootToKiloNewtonsPerMetre(0.64);

        /// <summary>
        /// Returns the three axle positions of the design truck.
        /// </summary>
        /// <param name="frontAxlePositionFt">Position of the front axle in feet.</param>
        /// <param name="middleToRearFt">Variable axle spacing between middle and rear axle in feet (14.0 to 30.0 ft). Default 14.0 ft.</param>
        /// <returns>Array of (axleLoadKips, axlePositionFt) pairs.</returns>
        public static (double LoadKips, double PositionFt)[] TruckAxlePositions(
            double frontAxlePositionFt,
            double middleToRearFt = 14.0)
        {
            if (middleToRearFt < 14.0 || middleToRearFt > 30.0)
                throw new ArgumentOutOfRangeException(nameof(middleToRearFt),
                    "Middle-to-rear axle spacing must be between 14.0 and 30.0 ft per AASHTO LRFD.");

            double frontM  = Units.FeetToMetres(frontAxlePositionFt);
            double middleM = frontM + _frontToMiddle;
            double rearM   = middleM + Units.FeetToMetres(middleToRearFt);

            return new[]
            {
                (Units.KiloNewtonsToKips(_frontAxle),  Units.MetresToFeet(frontM)),
                (Units.KiloNewtonsToKips(_middleAxle), Units.MetresToFeet(middleM)),
                (Units.KiloNewtonsToKips(_rearAxle),   Units.MetresToFeet(rearM)),
            };
        }

        /// <summary>
        /// Lane load in kips per foot.
        /// </summary>
        public static double LaneLoadKipsPerFoot => Units.KiloNewtonsPerMetreToKipsPerFoot(_laneLoad);

        /// <summary>
        /// Total truck length in feet for a given middle-to-rear axle spacing.
        /// </summary>
        public static double TruckLengthFt(double middleToRearFt = 14.0) =>
            Units.MetresToFeet(_frontToMiddle) + middleToRearFt;
    }
}