using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates Fourier analysis problems: Fourier coefficients, transforms, convolution.
    /// Extreme difficulty only.
    /// </summary>
    public class FourierAnalysisGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.FourierAnalysis;
            p.difficulty = difficulty;
            p.format     = ProblemFormat.ExactExpression;
            p.baseGoldReward  = 30 + (int)difficulty * 10;
            p.baseScoreReward = 300 + (int)difficulty * 100;
            p.baseDamage      = 40 + (int)difficulty * 15;
            p.penaltyDamage   = 20 + (int)difficulty * 10;

            int variant = Random.Range(0, 3);
            switch (variant)
            {
                case 0: BuildFourierCoefficient(p); break;
                case 1: BuildFourierTransform(p);   break;
                default: BuildParseval(p);          break;
            }
            return p;
        }

        private void BuildFourierCoefficient(MathProblem p)
        {
            // a_0 of f(x)=1 on [-π,π] is 1 (average is 1/(2π)·∫ dx = 1)
            p.questionText  = "Find a₀ (Fourier cosine coefficient) for f(x) = 1 on [−π, π]. (a₀ = (1/π)∫₋π^π f(x)dx)";
            p.equationLatex = "a_0 = \\frac{1}{\\pi}\\int_{-\\pi}^{\\pi} 1\\, dx";
            p.correctAnswer = "2";
            p.hint          = "Integrate 1 over [−π,π]: result is 2π, then divide by π.";
        }

        private void BuildFourierTransform(MathProblem p)
        {
            // FT of e^(-at)u(t) = 1/(a+jω)
            int a = Random.Range(1, 5);
            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"What is the Fourier transform of f(t) = e^(−{a}t)u(t)?";
            p.equationLatex = $"\\mathcal{{F}}\\left\\{{e^{{-{a}t}}u(t)\\right\\}}";
            p.correctAnswer = $"1/({a}+jω)";
            p.hint          = "For f(t) = e^(−at)u(t), F(ω) = 1/(a + jω).";
        }

        private void BuildParseval(MathProblem p)
        {
            p.format        = ProblemFormat.TrueFalse;
            p.questionText  = "True or False: Parseval's theorem states that ∫|f(t)|² dt = (1/2π)∫|F(ω)|² dω.";
            p.equationLatex = "\\int_{-\\infty}^{\\infty}|f(t)|^2 dt = \\frac{1}{2\\pi}\\int_{-\\infty}^{\\infty}|F(\\omega)|^2 d\\omega";
            p.correctAnswer = "True";
            p.hint          = "Parseval's theorem equates energy in time and frequency domains.";
        }
    }
}
