using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates complex analysis problems: modulus, argument, powers, residues.
    /// High = modulus/argument; VeryHigh = De Moivre; Extreme = residue theorem.
    /// </summary>
    public class ComplexAnalysisGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.ComplexAnalysis;
            p.difficulty = difficulty;
            p.baseGoldReward  = 20 + (int)difficulty * 15;
            p.baseScoreReward = 200 + (int)difficulty * 100;
            p.baseDamage      = 30 + (int)difficulty * 15;
            p.penaltyDamage   = 15 + (int)difficulty * 10;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    BuildModulus(p);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildDeMoivre(p);
                    break;
                default:
                    BuildResidue(p);
                    break;
            }
            return p;
        }

        private void BuildModulus(MathProblem p)
        {
            int a = Random.Range(1, 7), b = Random.Range(1, 7);
            float mod = Mathf.Sqrt(a * a + b * b);
            string answer = (Mathf.Abs(mod - Mathf.Round(mod)) < 0.001f)
                ? ((int)Mathf.Round(mod)).ToString()
                : $"√{a * a + b * b}";

            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"Find |z| for z = {a} + {b}i.";
            p.equationLatex = $"|{a} + {b}i|";
            p.correctAnswer = answer;
            p.hint          = "|a + bi| = √(a² + b²)";
        }

        private void BuildDeMoivre(MathProblem p)
        {
            // (cos θ + i sin θ)^n  →  cos(nθ) + i sin(nθ)
            int[] angles = { 30, 45, 60, 90 };
            int theta = angles[Random.Range(0, angles.Length)];
            int n     = Random.Range(2, 5);
            int nTheta = n * theta;

            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"Use De Moivre's theorem: (cos{theta}° + i·sin{theta}°)^{n} = cos(?) + i·sin(?)";
            p.equationLatex = $"\\left(\\cos {theta}^{{\\circ}} + i\\sin {theta}^{{\\circ}}\\right)^{{{n}}}";
            p.correctAnswer = $"cos{nTheta}°+isin{nTheta}°";
            p.hint          = "(cosθ + i·sinθ)ⁿ = cos(nθ) + i·sin(nθ) by De Moivre's theorem.";
        }

        private void BuildResidue(MathProblem p)
        {
            // Res of 1/(z-a) at z=a is 1
            int a = Random.Range(1, 6);
            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = $"Find the residue of f(z) = 1/(z−{a}) at z = {a}.";
            p.equationLatex = $"\\text{{Res}}\\left(\\frac{{1}}{{z-{a}}},\\, z={a}\\right)";
            p.correctAnswer = "1";
            p.hint          = "For a simple pole at z=a, Res(f,a) = lim_(z→a) (z−a)·f(z).";
        }
    }
}
