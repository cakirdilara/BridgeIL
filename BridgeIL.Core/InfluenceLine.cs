using System.Collections.Generic;

namespace BridgeIL.Core
{
    /// <summary>
    /// Computes influence line coefficients and load envelopes
    /// for a simply supported single-span beam.
    /// </summary>
    public class InfluenceLine
    {
        private readonly Beam _beam;
        private readonly double _step;  // load position step size in metres

        /// <summary>
        /// Initialises the influence line calculator.
        /// </summary>
        /// <param name="beam">The simply supported beam to analyse.</param>
        /// <param name="step">Load position step size in metres (default 0.5 m).</param>
        public InfluenceLine(Beam beam, double step = 0.5)
        {
            _beam = beam;
            _step = step;
        }

        /// <summary>
        /// Returns influence line coefficients for bending moment at section x.
        /// Each coefficient is the moment at x caused by a unit load at that position.
        /// </summary>
        /// <param name="x">Section position in metres.</param>
        /// <returns>Array of (loadPosition, coefficientValue) pairs.</returns>
        public (double LoadPosition, double Coefficient)[] MomentCoefficients(double x)
        {
            var result = new List<(double, double)>();
            for (double a = 0; a <= _beam.Span + 1e-9; a += _step)
            {
                result.Add((a, _beam.MomentInfluenceLine(x, a)));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Returns influence line coefficients for shear force at section x.
        /// </summary>
        public (double LoadPosition, double Coefficient)[] ShearCoefficients(double x)
        {
            var result = new List<(double, double)>();
            for (double a = 0; a <= _beam.Span + 1e-9; a += _step)
            {
                result.Add((a, _beam.ShearInfluenceLine(x, a)));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Returns the maximum and minimum moment influence line coefficients along the span
        /// and the load positions that cause them.
        /// </summary>
        public (double MaxCoefficient, double CriticalLoadPosition,
                double MinCoefficient, double CriticalMinLoadPosition) MaxMomentCoefficient(double x)
        {
            var coefficients = MomentCoefficients(x);
            double max = double.MinValue;
            double min = double.MaxValue;
            double critMaxPos = 0;
            double critMinPos = 0;

            foreach (var (pos, coeff) in coefficients)
            {
                if (coeff > max) { max = coeff; critMaxPos = pos; }
                if (coeff < min) { min = coeff; critMinPos = pos; }
            }

            return (max, critMaxPos, min, critMinPos);
        }
        /// <summary>
/// Applies a set of point loads to the precomputed influence line coefficients
/// and returns the total effect.
/// Axles with a negative coefficient are skipped by default per AASHTO LRFD.
/// Set skipPositive to true to find minimum effect (skip positive coefficients instead).
/// </summary>
public static double ApplyLoads(
    (double LoadPosition, double Coefficient)[] coefficients,
    (double Load, double OffsetFromReference)[] axles,
    bool skipPositive = false)
{
    double total = 0;

    foreach (var (load, position) in axles)
    {
        // Find the coefficient closest to this axle's position
        double bestCoeff = 0;
        double bestDist  = double.MaxValue;

        foreach (var (pos, coeff) in coefficients)
        {
            double dist = Math.Abs(pos - position);
            if (dist < bestDist) { bestDist = dist; bestCoeff = coeff; }
        }

        // Skip axle if coefficient works against the effect being sought
        if (!skipPositive && bestCoeff < 0) continue;  // seeking max — skip negative
        if (skipPositive  && bestCoeff > 0) continue;  // seeking min — skip positive

        total += load * bestCoeff;
    }

    return total;
}

/// <summary>
/// Sweeps a truck load configuration over precomputed influence line coefficients
/// and returns the maximum (or minimum) effect.
/// </summary>
public static (double Effect, double CriticalFrontAxlePositionM) SweepTruck(
    (double LoadPosition, double Coefficient)[] coefficients,
    (double LoadKN, double OffsetFromFrontAxleM)[] axles,
    double spanM,
    double stepM,
    bool skipPositive = false)
{
    double bestEffect  = skipPositive ? double.MaxValue : double.MinValue;
    double critFrontM  = 0;

    double truckLengthM = axles[axles.Length - 1].OffsetFromFrontAxleM;

    for (double frontM = 0; frontM <= spanM - truckLengthM; frontM += stepM)
    {
        var absoluteAxles = new (double Load, double OffsetFromReference)[axles.Length];
        for (int i = 0; i < axles.Length; i++)
            absoluteAxles[i] = (axles[i].LoadKN, frontM + axles[i].OffsetFromFrontAxleM);

        double effect = ApplyLoads(coefficients, absoluteAxles, skipPositive);

        if (!skipPositive && effect > bestEffect) { bestEffect = effect; critFrontM = frontM; }
        if (skipPositive  && effect < bestEffect) { bestEffect = effect; critFrontM = frontM; }
    }

    return (bestEffect, critFrontM);
}
        /// <summary>
        /// Returns the maximum and minimum shear influence line coefficients along the span
        /// and the load positions that cause them.
        /// </summary>
        public (double MaxCoefficient, double CriticalLoadPosition,
                double MinCoefficient, double CriticalMinLoadPosition) MaxShearCoefficient(double x)
        {
            var coefficients = ShearCoefficients(x);
            double max = double.MinValue;
            double min = double.MaxValue;
            double critMaxPos = 0;
            double critMinPos = 0;

            foreach (var (pos, coeff) in coefficients)
            {
                if (coeff > max) { max = coeff; critMaxPos = pos; }
                if (coeff < min) { min = coeff; critMinPos = pos; }
            }

            return (max, critMaxPos, min, critMinPos);
        }
    }
}