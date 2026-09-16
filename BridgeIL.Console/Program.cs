using BridgeIL.Core;
using ScottPlot;

// ── Input ────────────────────────────────────────────────────────────────────
double spanFt        = 100.0;
double sectionStepFt = 5.0;
double sweepStepFt   = 0.5;

// ── Beam setup ───────────────────────────────────────────────────────────────
var beam = new Beam(Units.FeetToMetres(spanFt));
var il   = new InfluenceLine(beam, step: Units.FeetToMetres(sweepStepFt));

double laneLoadKNm = Units.KipsPerFootToKiloNewtonsPerMetre(AASHTOLoads.LaneLoadKipsPerFoot);
double stepM       = Units.FeetToMetres(sweepStepFt);

// ── Storage for envelope and critical paths ───────────────────────────────────
var sections       = new List<double>();
var truckMoments   = new List<double>();
var tandemMoments  = new List<double>();
var laneMoments    = new List<double>();
var governingMoments = new List<double>();

// Critical path storage: (x, axle1Pos, axle2Pos, axle3Pos, isTruck)
var criticalPaths = new List<(double xFt, double a1Ft, double a2Ft, double a3Ft, bool isTruck)>();

Console.WriteLine($"{"x (ft)",-10} {"Truck",-14} {"Tandem",-14} {"Lane",-12} {"Governing",-14} {"Governs",-10} {"Critical Axles (ft)"}");
Console.WriteLine(new string('-', 100));

for (double xFt = sectionStepFt; xFt < spanFt; xFt += sectionStepFt)
{
    double xM = Units.FeetToMetres(xFt);
    var momentCoeffs = il.MomentCoefficients(xM);

    // Truck sweep
    double maxTruckKipFt  = double.MinValue;
    double critTruckFrontFt = 0;
    double critTruckSpacingFt = 0;

    for (double spacingFt = 14.0; spacingFt <= 30.0; spacingFt += 0.5)
    {
        var truckAxles = new (double LoadKN, double OffsetFromFrontAxleM)[]
        {
            (Units.KipsToKiloNewtons(8.0),  Units.FeetToMetres(0.0)),
            (Units.KipsToKiloNewtons(32.0), Units.FeetToMetres(14.0)),
            (Units.KipsToKiloNewtons(32.0), Units.FeetToMetres(14.0 + spacingFt)),
        };
        var (effectKNm, critFrontM) = InfluenceLine.SweepTruck(momentCoeffs, truckAxles,
            Units.FeetToMetres(spanFt), stepM);
        double momentKipFt = Units.KiloNewtonMetresToKipFeet(effectKNm);
        if (momentKipFt > maxTruckKipFt)
        {
            maxTruckKipFt       = momentKipFt;
            critTruckFrontFt    = Units.MetresToFeet(critFrontM);
            critTruckSpacingFt  = spacingFt;
        }
    }

    // Tandem sweep
    var tandemAxles = new (double LoadKN, double OffsetFromFrontAxleM)[]
    {
        (Units.KipsToKiloNewtons(25.0), Units.FeetToMetres(0.0)),
        (Units.KipsToKiloNewtons(25.0), Units.FeetToMetres(4.0)),
    };
    var (tandemKNm, tandemFrontM) = InfluenceLine.SweepTruck(momentCoeffs, tandemAxles,
        Units.FeetToMetres(spanFt), stepM);
    double maxTandemKipFt  = Units.KiloNewtonMetresToKipFeet(tandemKNm);
    double critTandemFrontFt = Units.MetresToFeet(tandemFrontM);

    // Lane load
    double laneKNm = 0;
    foreach (var (_, coeff) in momentCoeffs)
        if (coeff > 0) laneKNm += laneLoadKNm * coeff * stepM;
    double laneKipFt = Units.KiloNewtonMetresToKipFeet(laneKNm);

    // Governing
    bool truckGoverns     = maxTruckKipFt >= maxTandemKipFt;
    double governingKipFt = Math.Max(maxTruckKipFt, maxTandemKipFt) + laneKipFt;

    // Store results
    sections.Add(xFt);
    truckMoments.Add(maxTruckKipFt);
    tandemMoments.Add(maxTandemKipFt);
    laneMoments.Add(laneKipFt);
    governingMoments.Add(governingKipFt);

    // Critical axle positions
    double a1, a2, a3;
    if (truckGoverns)
    {
        a1 = critTruckFrontFt;
        a2 = critTruckFrontFt + 14.0;
        a3 = critTruckFrontFt + 14.0 + critTruckSpacingFt;
        criticalPaths.Add((xFt, a1, a2, a3, true));
        Console.WriteLine($"{xFt,-10:F1} {maxTruckKipFt,-14:F2} {maxTandemKipFt,-14:F2} {laneKipFt,-12:F2} {governingKipFt,-14:F2} {"Truck",-10} {a1:F1}, {a2:F1}, {a3:F1}");
    }
    else
    {
        a1 = critTandemFrontFt;
        a2 = critTandemFrontFt + 4.0;
        a3 = 0;
        criticalPaths.Add((xFt, a1, a2, a3, false));
        Console.WriteLine($"{xFt,-10:F1} {maxTruckKipFt,-14:F2} {maxTandemKipFt,-14:F2} {laneKipFt,-12:F2} {governingKipFt,-14:F2} {"Tandem",-10} {a1:F1}, {a2:F1}");
    }
}
// ── Shear Envelope ───────────────────────────────────────────────────────────
var shearSections      = new List<double>();
var maxShearList       = new List<double>();
var minShearList       = new List<double>();

Console.WriteLine();
Console.WriteLine($"{"x (ft)",-10} {"Truck Max V",-16} {"Truck Min V",-16} {"Tandem Max V",-16} {"Tandem Min V",-16} {"Lane V",-12} {"Max Governing",-16} {"Min Governing"}");
Console.WriteLine(new string('-', 120));

for (double xFt = sectionStepFt; xFt < spanFt; xFt += sectionStepFt)
{
    double xM = Units.FeetToMetres(xFt);
    var shearCoeffs = il.ShearCoefficients(xM);

    // Truck max shear sweep
    double maxTruckV = double.MinValue;
    double minTruckV = double.MaxValue;

    for (double spacingFt = 14.0; spacingFt <= 30.0; spacingFt += 0.5)
    {
        var truckAxles = new (double LoadKN, double OffsetFromFrontAxleM)[]
        {
            (Units.KipsToKiloNewtons(8.0),  Units.FeetToMetres(0.0)),
            (Units.KipsToKiloNewtons(32.0), Units.FeetToMetres(14.0)),
            (Units.KipsToKiloNewtons(32.0), Units.FeetToMetres(14.0 + spacingFt)),
        };

        // Max shear — skip negative coefficients
        var (maxEffectKNm, _) = InfluenceLine.SweepTruck(shearCoeffs, truckAxles,
            Units.FeetToMetres(spanFt), stepM);
        double maxV = Units.KiloNewtonsToKips(maxEffectKNm);
        if (maxV > maxTruckV) maxTruckV = maxV;

        // Min shear — skip positive coefficients
        var (minEffectKNm, _) = InfluenceLine.SweepTruck(shearCoeffs, truckAxles,
            Units.FeetToMetres(spanFt), stepM, skipPositive: true);
        double minV = Units.KiloNewtonsToKips(minEffectKNm);
        if (minV < minTruckV) minTruckV = minV;
    }

    // Tandem max and min shear
    var tandemAxles = new (double LoadKN, double OffsetFromFrontAxleM)[]
    {
        (Units.KipsToKiloNewtons(25.0), Units.FeetToMetres(0.0)),
        (Units.KipsToKiloNewtons(25.0), Units.FeetToMetres(4.0)),
    };

    var (tandemMaxKN, _) = InfluenceLine.SweepTruck(shearCoeffs, tandemAxles,
        Units.FeetToMetres(spanFt), stepM);
    var (tandemMinKN, _) = InfluenceLine.SweepTruck(shearCoeffs, tandemAxles,
        Units.FeetToMetres(spanFt), stepM, skipPositive: true);

    double maxTandemV = Units.KiloNewtonsToKips(tandemMaxKN);
    double minTandemV = Units.KiloNewtonsToKips(tandemMinKN);

    // Lane shear — positive and negative regions
    double lanePosKN = 0;
    double laneNegKN = 0;
    foreach (var (_, coeff) in shearCoeffs)
    {
        if (coeff > 0) lanePosKN += laneLoadKNm * coeff * stepM;
        if (coeff < 0) laneNegKN += laneLoadKNm * coeff * stepM;
    }
    double lanePosKips = Units.KiloNewtonsToKips(lanePosKN);
    double laneNegKips = Units.KiloNewtonsToKips(laneNegKN);

    // Governing
    double maxGoverning = Math.Max(maxTruckV, maxTandemV) + lanePosKips;
    double minGoverning = Math.Min(minTruckV, minTandemV) + laneNegKips;

    shearSections.Add(xFt);
    maxShearList.Add(maxGoverning);
    minShearList.Add(minGoverning);

    Console.WriteLine($"{xFt,-10:F1} {maxTruckV,-16:F3} {minTruckV,-16:F3} {maxTandemV,-16:F3} {minTandemV,-16:F3} {lanePosKips:F3}/{laneNegKips:F3,-8} {maxGoverning,-16:F3} {minGoverning:F3}");
}
// ── Plot ─────────────────────────────────────────────────────────────────────
var plt = new Plot();

double[] xs      = sections.ToArray();
double[] gov     = governingMoments.ToArray();
double[] trk     = truckMoments.ToArray();
double[] tan     = tandemMoments.ToArray();
double[] lan     = laneMoments.ToArray();
double[] shearXs = shearSections.ToArray();
double[] maxVArr = maxShearList.ToArray();
double[] minVArr = minShearList.ToArray();

// Moment subplot
var moment = plt.Add.Scatter(xs, gov, color: ScottPlot.Color.FromHex("#0F2D4A"));
moment.LegendText = "Governing Moment";
var truckS = plt.Add.Scatter(xs, trk, color: ScottPlot.Color.FromHex("#1A6FAE"));
truckS.LegendText = "Truck only";
var tandemS = plt.Add.Scatter(xs, tan, color: ScottPlot.Color.FromHex("#C9860A"));
tandemS.LegendText = "Tandem only";
var laneS = plt.Add.Scatter(xs, lan, color: ScottPlot.Color.FromHex("#1A7A4A"));
laneS.LegendText = "Lane only";

plt.ShowLegend();
plt.Title("HL-93 Live Load Moment Envelope");
plt.XLabel("Section x (ft)");
plt.YLabel("Moment (kip·ft)");

plt.SavePng("moment_envelope.png", 1200, 600);
Console.WriteLine("Plot saved → moment_envelope.png");

// Shear plot
var pltV = new Plot();

var maxVS = pltV.Add.Scatter(shearXs, maxVArr, color: ScottPlot.Color.FromHex("#0F2D4A"));
maxVS.LegendText = "Max Shear (governing)";
var minVS = pltV.Add.Scatter(shearXs, minVArr, color: ScottPlot.Color.FromHex("#CC0000"));
minVS.LegendText = "Min Shear (governing)";

// Zero line
double[] zeros = new double[shearXs.Length];
var zeroLine = pltV.Add.Scatter(shearXs, zeros, color: ScottPlot.Color.FromHex("#AAAAAA"));
zeroLine.LegendText = "Zero";

pltV.ShowLegend();
pltV.Title("HL-93 Live Load Shear Envelope");
pltV.XLabel("Section x (ft)");
pltV.YLabel("Shear (kip)");

pltV.SavePng("shear_envelope.png", 1200, 600);
Console.WriteLine("Plot saved → shear_envelope.png");