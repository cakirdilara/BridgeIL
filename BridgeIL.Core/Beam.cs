namespace BridgeIL.Core
{   
    /// <summary>
    /// Represents a simply supported single-span beam.
    /// MVP scope: statically determinate beam with pin support at x=0
    /// and roller support at x=Span. Continuous beams not yet supported.
    /// </summary>


    public class Beam
    {
        public double Span { get; }   // span length in metres

        public Beam(double span)
        {
            Span = span;
        }

        // Bending moment influence line value at position "x"
        // for a unit load (1 kN) placed at position "a" along the span
    
        public double MomentInfluenceLine(double x, double a)
        {
             if (a <= x)
                 return (a / Span) * (Span - x);   // load left of section
             else
                 return (x / Span) * (Span - a);   // load right of section
        }

        // Shear force influence line value at position x
        // for a unit load placed at position a
        public double ShearInfluenceLine(double x, double a)
        {
            if (a < x)
                return -(a / Span);        // load left of section
            else
                return 1.0 - (a / Span);   // load right of section
        }
    }
}