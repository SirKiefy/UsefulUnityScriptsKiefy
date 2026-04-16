using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates calculus problems: derivatives, integrals, and limits.
    /// High = basic rules; VeryHigh = chain/product/quotient; Extreme = multivariable.
    /// </summary>
    public class CalculusGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var problem = ScriptableObject.CreateInstance<MathProblem>();
            problem.topic      = MathTopic.Calculus;
            problem.difficulty = difficulty;
            problem.format     = ProblemFormat.ExactExpression;
            problem.baseGoldReward  = 15 + (int)difficulty * 5;
            problem.baseScoreReward = 150 + (int)difficulty * 50;
            problem.baseDamage      = 25 + (int)difficulty * 10;
            problem.penaltyDamage   = 12 + (int)difficulty * 5;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    BuildBasicDerivative(problem);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildChainRule(problem);
                    break;
                default:
                    BuildMultivariable(problem);
                    break;
            }
            return problem;
        }

        // ── Problem builders ──────────────────────────────────────────

        private void BuildBasicDerivative(MathProblem p)
        {
            // d/dx of x^n
            int n = Random.Range(2, 8);
            p.questionText  = $"Find the derivative of f(x) = x^{n}.";
            p.equationLatex = $"\\frac{{d}}{{dx}}\\left(x^{{{n}}}\\right)";
            p.correctAnswer = $"{n}x^{n - 1}";
            p.hint          = "Power rule: d/dx(xⁿ) = n·xⁿ⁻¹";
        }

        private void BuildChainRule(MathProblem p)
        {
            // d/dx of sin(ax²) or e^(ax)
            int a = Random.Range(2, 6);
            bool useExp = Random.value > 0.5f;
            if (useExp)
            {
                p.questionText  = $"Differentiate f(x) = e^({a}x) with respect to x.";
                p.equationLatex = $"\\frac{{d}}{{dx}}\\left(e^{{{a}x}}\\right)";
                p.correctAnswer = $"{a}e^{{{a}x}}";
                p.hint          = "Chain rule: d/dx(e^(u)) = e^(u)·u'";
            }
            else
            {
                p.questionText  = $"Differentiate f(x) = sin({a}x²) with respect to x.";
                p.equationLatex = $"\\frac{{d}}{{dx}}\\left(\\sin({a}x^2)\\right)";
                p.correctAnswer = $"{2 * a}xcos({a}x^2)";
                p.hint          = "Chain rule: d/dx(sin(u)) = cos(u)·u'";
            }
        }

        private void BuildMultivariable(MathProblem p)
        {
            // ∂/∂x of x²y³
            int a = Random.Range(2, 5);
            int b = Random.Range(2, 5);
            p.questionText  = $"Find ∂/∂x of f(x,y) = x^{a}·y^{b}.";
            p.equationLatex = $"\\frac{{\\partial}}{{\\partial x}}\\left(x^{{{a}}}y^{{{b}}}\\right)";
            p.correctAnswer = $"{a}x^{a - 1}y^{b}";
            p.hint          = "Treat y as a constant when differentiating with respect to x.";
        }
    }
}
