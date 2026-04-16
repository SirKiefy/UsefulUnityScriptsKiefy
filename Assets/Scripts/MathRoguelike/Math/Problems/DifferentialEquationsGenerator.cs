using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates ordinary differential equation problems.
    /// High = separable ODEs; VeryHigh = first-order linear; Extreme = second-order.
    /// </summary>
    public class DifferentialEquationsGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.DifferentialEquations;
            p.difficulty = difficulty;
            p.format     = ProblemFormat.ExactExpression;
            p.baseGoldReward  = 20 + (int)difficulty * 10;
            p.baseScoreReward = 200 + (int)difficulty * 100;
            p.baseDamage      = 30 + (int)difficulty * 15;
            p.penaltyDamage   = 15 + (int)difficulty * 8;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    BuildSeparableODE(p);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildFirstOrderLinear(p);
                    break;
                default:
                    BuildSecondOrderConstant(p);
                    break;
            }
            return p;
        }

        private void BuildSeparableODE(MathProblem p)
        {
            // dy/dx = ky  →  y = Ce^(kx)
            int k = Random.Range(1, 5);
            p.questionText  = $"Solve the ODE: dy/dx = {k}y.";
            p.equationLatex = $"\\frac{{dy}}{{dx}} = {k}y";
            p.correctAnswer = $"Ce^{{{k}x}}";
            p.hint          = "Separate variables: dy/y = k dx, then integrate both sides.";
        }

        private void BuildFirstOrderLinear(MathProblem p)
        {
            // dy/dx + ay = b  →  y = b/a + Ce^(-ax)
            int a = Random.Range(1, 4), b = Random.Range(1, 6);
            p.questionText  = $"Find the general solution of: dy/dx + {a}y = {b}.";
            p.equationLatex = $"\\frac{{dy}}{{dx}} + {a}y = {b}";
            p.correctAnswer = $"{b}/{a}+Ce^{{-{a}x}}";
            p.hint          = "Use the integrating factor μ = e^(∫a dx) = e^(ax).";
        }

        private void BuildSecondOrderConstant(MathProblem p)
        {
            // y'' - (a+b)y' + ab·y = 0  →  y = C1·e^(ax) + C2·e^(bx)
            int a = Random.Range(1, 4), b = Random.Range(1, 4);
            int sum = a + b, product = a * b;
            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"Solve: y'' - {sum}y' + {product}y = 0.";
            p.equationLatex = $"y'' - {sum}y' + {product}y = 0";
            p.correctAnswer = (a == b)
                ? $"(C1+C2x)e^{{{a}x}}"
                : $"C1e^{{{a}x}}+C2e^{{{b}x}}";
            p.hint          = "Form the characteristic equation r² - " + sum + "r + " + product + " = 0.";
        }
    }
}
