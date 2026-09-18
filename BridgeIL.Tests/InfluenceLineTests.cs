using BridgeIL.Core;
using Xunit;

namespace BridgeIL.Tests;

public class InfluenceLineTests
{
    private const double L = 30.48;   // 100 ft in metres
    private const double Tol = 0.01;  // tolerance

    private readonly Beam _beam;
    private readonly InfluenceLine _il;

    public InfluenceLineTests()
    {
        _beam = new Beam(L);
        _il   = new InfluenceLine(_beam, step: Units.FeetToMetres(0.5));
    }

    // ── Influence line coefficients ──────────────────────────────────────────

    [Fact]
    public void MomentCoefficient_AtMidspan_PeakIs25()
    {
        double x = Units.FeetToMetres(50.0);
        var coeffs = _il.MomentCoefficients(x);
        var (max, _, _, _) = _il.MaxMomentCoefficient(x);
        double maxFt = Units.MetresToFeet(max);
        Assert.Equal(25.0, maxFt, precision: 1);
    }

    [Fact]
    public void MomentCoefficient_AtSupports_IsZero()
    {
        double x = Units.FeetToMetres(50.0);
        double coeffAtZero  = _beam.MomentInfluenceLine(x, 0);
        double coeffAtL     = _beam.MomentInfluenceLine(x, L);
        Assert.Equal(0.0, coeffAtZero, precision: 5);
        Assert.Equal(0.0, coeffAtL,    precision: 5);
    }

    [Fact]
    public void MomentCoefficient_QuarterPoint_PeakIs18p75()
    {
        double x = Units.FeetToMetres(25.0);
        var (max, _, _, _) = _il.MaxMomentCoefficient(x);
        double maxFt = Units.MetresToFeet(max);
        Assert.InRange(maxFt, 18.70, 18.80);
    }

    // ── Lane load moment ─────────────────────────────────────────────────────

    [Fact]
    public void LaneMoment_AtMidspan_Is800()
    {
        double xM          = Units.FeetToMetres(50.0);
        double stepM       = Units.FeetToMetres(0.5);
        double laneLoadKNm = Units.KipsPerFootToKiloNewtonsPerMetre(0.64);
        var coeffs         = _il.MomentCoefficients(xM);

        double laneKNm = 0;
        foreach (var (_, coeff) in coeffs)
            if (coeff > 0) laneKNm += laneLoadKNm * coeff * stepM;

        double laneKipFt = Units.KiloNewtonMetresToKipFeet(laneKNm);
        Assert.Equal(800.0, laneKipFt, precision: 0);
    }

    [Fact]
    public void LaneMoment_AtQuarterPoint_Is600()
    {
        double xM          = Units.FeetToMetres(25.0);
        double stepM       = Units.FeetToMetres(0.5);
        double laneLoadKNm = Units.KipsPerFootToKiloNewtonsPerMetre(0.64);
        var coeffs         = _il.MomentCoefficients(xM);

        double laneKNm = 0;
        foreach (var (_, coeff) in coeffs)
            if (coeff > 0) laneKNm += laneLoadKNm * coeff * stepM;

        double laneKipFt = Units.KiloNewtonMetresToKipFeet(laneKNm);
        Assert.Equal(600.0, laneKipFt, precision: 0);
    }

    // ── Design truck moment ──────────────────────────────────────────────────

    [Fact]
    public void TruckMoment_AtMidspan_Is1520()
    {
        double xM    = Units.FeetToMetres(50.0);
        double stepM = Units.FeetToMetres(0.5);
        var coeffs   = _il.MomentCoefficients(xM);

        double maxKipFt = double.MinValue;
        for (double spacingFt = 14.0; spacingFt <= 30.0; spacingFt += 0.5)
        {
            var axles = new (double LoadKN, double OffsetFromFrontAxleM)[]
            {
                (Units.KipsToKiloNewtons(8.0),  Units.FeetToMetres(0.0)),
                (Units.KipsToKiloNewtons(32.0), Units.FeetToMetres(14.0)),
                (Units.KipsToKiloNewtons(32.0), Units.FeetToMetres(14.0 + spacingFt)),
            };
            var (kNm, _) = InfluenceLine.SweepTruck(coeffs, axles,
                Units.FeetToMetres(100.0), stepM);
            double kipFt = Units.KiloNewtonMetresToKipFeet(kNm);
            if (kipFt > maxKipFt) maxKipFt = kipFt;
        }

        Assert.Equal(1520.0, maxKipFt, precision: 0);
    }

    // ── Design tandem moment ─────────────────────────────────────────────────

    [Fact]
    public void TandemMoment_AtMidspan_Is1200()
    {
        double xM    = Units.FeetToMetres(50.0);
        double stepM = Units.FeetToMetres(0.5);
        var coeffs   = _il.MomentCoefficients(xM);

        var axles = new (double LoadKN, double OffsetFromFrontAxleM)[]
        {
            (Units.KipsToKiloNewtons(25.0), Units.FeetToMetres(0.0)),
            (Units.KipsToKiloNewtons(25.0), Units.FeetToMetres(4.0)),
        };
        var (kNm, _) = InfluenceLine.SweepTruck(coeffs, axles,
            Units.FeetToMetres(100.0), stepM);
        double kipFt = Units.KiloNewtonMetresToKipFeet(kNm);

        Assert.Equal(1200.0, kipFt, precision: 0);
    }

    // ── Shear influence line ─────────────────────────────────────────────────

    [Fact]
    public void ShearCoefficient_AtMidspan_MaxIsHalf()
    {
        double x = Units.FeetToMetres(50.0);
        var (maxV, _, _, _) = _il.MaxShearCoefficient(x);
        Assert.Equal(0.5, maxV, precision: 2);
    }
}