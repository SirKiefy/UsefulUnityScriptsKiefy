namespace UsefulScripts.MathRoguelike
{
    /// <summary>Difficulty tiers driving which math topics and complexity are used.</summary>
    public enum MathDifficulty
    {
        High     = 0,   // Trig, basic derivatives, logarithms, vectors
        VeryHigh = 1,   // Differential equations, matrices, series, complex numbers
        Extreme  = 2    // Tensor calculus, Fourier analysis, group theory, number theory
    }

    /// <summary>Broad topic categories for math problems.</summary>
    public enum MathTopic
    {
        Trigonometry,
        Calculus,
        LinearAlgebra,
        DifferentialEquations,
        ComplexAnalysis,
        SeriesAndSequences,
        NumberTheory,
        FourierAnalysis,
        VectorCalculus,
        GroupTheory
    }

    /// <summary>How the problem is presented and answered.</summary>
    public enum ProblemFormat
    {
        MultipleChoice,     // Pick from A/B/C/D
        ExactNumeric,       // Enter a number (integer or decimal)
        ExactExpression,    // Enter a simplified expression (e.g. "2sin(x)cos(x)")
        TrueFalse           // Is the statement true or false?
    }
}
