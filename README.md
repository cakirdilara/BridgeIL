# BridgeIL

**Influence Line & Live Load Envelope Calculator for Simply Supported Beam Bridges**

A C# / .NET library and console application that computes AASHTO LRFD HL-93 live load moment and shear envelopes using influence line theory. Built as a transparent, engineer-readable implementation — every design decision is documented and every result is verifiable by hand.

---

## What it does

Given a simply supported beam bridge with a user-defined span, BridgeIL:

1. Computes influence line coefficients for moment and shear at every section along the span
2. Positions the HL-93 design truck and design tandem at their critical locations using an efficient sweep algorithm
3. Adds the lane load contribution by integrating the influence line over the loaded length
4. Reports the governing live load (truck + lane vs. tandem + lane) and plots the full moment and shear envelopes

---

## Why influence lines

Most structural analysis software treats live load positioning as a black box. This project makes the process explicit: for each section of interest, a unit load is moved across the entire span, the response is recorded at every position, and the resulting influence line directly tells you where to place each vehicle axle to maximise (or minimise) the effect at that section.

This is the classical method — and it is still the most transparent one.

---

## Project structure

```
BridgeIL/
├── BridgeIL.Core/
│   ├── Beam.cs              — simply supported beam, influence line formulas
│   ├── InfluenceLine.cs     — coefficient computation, truck sweep, load application
│   ├── AASHTOLoads.cs       — HL-93 truck and tandem definitions
│   └── Units.cs             — SI ↔ US customary conversion
└── BridgeIL.Console/
    └── Program.cs           — moment + shear envelope, console output, PNG plots
```

---

## Engineering decisions

### Unit system
All internal calculations use SI (kN, m). Input and output use US customary (kip, ft). Conversion is isolated in `Units.cs` — the calculation engine has no unit awareness. This makes it straightforward to add Eurocode load models later without touching the core.

### Influence line coefficients
The user provides a step size in feet — the physical distance the unit load moves between positions. The code computes the number of points internally. Coefficients are computed once per section and stored as an array; no beam recalculation happens during the truck sweep.

### AASHTO LRFD HL-93 live load
- **Design truck:** 8 kip front axle, 32 kip middle axle, 32 kip rear axle
- **Front-to-middle spacing:** fixed at 14 ft
- **Middle-to-rear spacing:** variable, 14–30 ft per AASHTO LRFD — swept in 0.5 ft increments to find the critical configuration
- **Design tandem:** 25 + 25 kip, fixed 4 ft spacing
- **Lane load:** 0.64 kip/ft applied over the full loaded length

For simply supported beams, the minimum middle-to-rear spacing (14 ft) always governs moment. The variable spacing rule becomes relevant for negative moment regions in continuous beams.

### Truck positioning
Trucks are swept across the precomputed coefficient array. Axles that fall on a negative influence line region are excluded per AASHTO LRFD — they reduce the effect at the section of interest and should not be included in the summation. For maximum shear, only positive coefficient regions are loaded; for minimum shear, only negative.

All axles must remain within the span. No overhanging axles are permitted.

### Moment envelope
The moment envelope is computed by repeating the full analysis — influence line coefficients, truck sweep, tandem sweep, and lane load integration — at every section along the span. Each section gets its own critical truck position and its own governing moment. The result is a complete picture of the maximum live load moment demand at every point in the beam.

### Shear envelope
Shear influence lines have a discontinuity at the section of interest. The minimum shear coefficient approaches but never exactly reaches its theoretical value due to discretization — this is expected and acceptable for engineering purposes. Using a finer step size reduces the error at the cost of computation time.

Shear envelopes report both maximum (positive) and minimum (negative) values, since both are needed for design.

### Lane load integration
Lane load is applied only where it increases the effect being sought — not uniformly across the full span. The algorithm checks the sign of each influence line coefficient and applies lane load only where it contributes positively:

```
For maximum moment or max shear:  load positions where coeff > 0
For minimum shear:                load positions where coeff < 0
```

This is not a manual engineering judgement — the algorithm decides automatically based on the coefficient sign at each position. For a simply supported beam, the moment influence line is always positive, so lane load covers the full span. For shear, or for continuous beams with negative moment regions, the loaded length will be shorter and the boundary is determined by the influence line itself.

The lane load effect at a section is:

```
Effect = w_lane × Σ (coeff_i × step)   for contributing positions only
```

For a 100 ft simply supported beam at midspan:

```
M_lane = 0.64 × (0.5 × 100 × 25) = 800 kip·ft
```

which matches the classical formula `wL²/8 = 0.64 × 100² / 8 = 800 kip·ft`.

### Governing load combination
```
Governing = max(M_truck, M_tandem) + M_lane
```

No load factors are applied — results are unfactored live load effects. AASHTO LRFD load factors (η × γ × LL) are left to the design layer.

---

## Current limitations

- Simply supported single-span beam only
- No load factors (unfactored LL)
- No dynamic load allowance (impact factor not yet applied)
- No multiple presence factors
- Continuous beams: not yet supported

---

## Sample output — 100 ft span

```
x (ft)     Truck          Tandem         Lane         Governing
5.0        274.80         232.50         152.00       426.80
10.0       513.60         440.00         288.00       801.60
25.0       1154.00        912.50         600.00       1754.00
50.0       1520.00        1200.00        800.00       2320.00
75.0       1182.00        912.50         600.00       1782.00
95.0       308.40         232.50         152.00       460.40
```

Midspan check (x = 50 ft, truck governs):
- Middle axle (32 kip) positioned at 50 ft — maximum moment coefficient
- Front axle at 36 ft, rear axle at 64 ft
- Truck moment: 1520 kip·ft + Lane: 800 kip·ft = **2320 kip·ft**

---

## Getting started

```bash
git clone https://github.com/yourusername/BridgeIL.git
cd BridgeIL
dotnet build
cd BridgeIL.Console
dotnet run
```

Requires .NET 10 or later.

---

## Roadmap

- [ ] Dynamic load allowance (impact factor)
- [ ] AASHTO LRFD load factors and load combinations
- [ ] Multiple presence factors
- [ ] Continuous beam support
- [ ] Eurocode EN 1991-2 LM1 / LM2 load models
- [ ] CSV export for post-processing
