namespace BridgeIL.Core
{
    /// <summary>
    /// Unit conversion helpers between SI (kN, m) and US customary (kip, ft).
    /// Internal calculations use SI throughout.
    /// </summary>
    public static class Units
    {
        // Length
        public static double FeetToMetres(double ft)   => ft * 0.3048;
        public static double MetresToFeet(double m)    => m / 0.3048;

        // Force
        public static double KipsToKiloNewtons(double kip) => kip * 4.44822;
        public static double KiloNewtonsToKips(double kN)  => kN / 4.44822;

        // Force per unit length
        public static double KipsPerFootToKiloNewtonsPerMetre(double kipft) => kipft * 14.5939;
        public static double KiloNewtonsPerMetreToKipsPerFoot(double kNm)   => kNm / 14.5939;

        // Moment (kip·ft ↔ kN·m)
        public static double KipFeetToKiloNewtonMetres(double kipft) => kipft * 1.35582;
        public static double KiloNewtonMetresToKipFeet(double kNm)   => kNm / 1.35582;
    }
}