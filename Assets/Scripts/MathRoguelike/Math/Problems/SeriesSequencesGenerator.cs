using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates series and sequence problems: convergence, sums, Taylor coefficients.
    /// </summary>
    public class SeriesSequencesGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.SeriesAndSequences;
            p.difficulty = difficulty;
            p.baseGoldReward  = 15 + (int)difficulty * 10;
            p.baseScoreReward = 150 + (int)difficulty * 75;
            p.baseDamage      = 25 + (int)difficulty * 10;
            p.penaltyDamage   = 12 + (int)difficulty * 6;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    BuildGeometricSeries(p);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildTaylorCoefficient(p);
                    break;
                default:
                    BuildConvergenceTest(p);
                    break;
            }
            return p;
        }

        private void BuildGeometricSeries(MathProblem p)
        {
            // Sum of a/(1-r) for |r| < 1
            int a = Random.Range(1, 6);
            int[] rNums = { 1, 1, 2, 3 }; // numerators of r
            int[] rDens = { 2, 3, 3, 4 }; // denominators of r
            int idx = Random.Range(0, rNums.Length);
            int rn = rNums[idx], rd = rDens[idx];

            // Sum = a / (1 - rn/rd) = a*rd / (rd-rn)
            int sumNum = a * rd, sumDen = rd - rn;
            string sumStr = (sumNum % sumDen == 0)
                ? (sumNum / sumDen).ToString()
                : $"{sumNum}/{sumDen}";

            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"Find the sum of the infinite geometric series with first term {a} and ratio {rn}/{rd}.";
            p.equationLatex = $"\\sum_{{n=0}}^{{\\infty}} {a}\\left(\\frac{{{rn}}}{{{rd}}}\\right)^n";
            p.correctAnswer = sumStr;
            p.hint          = "For |r|<1: S = a/(1−r).";
        }

        private void BuildTaylorCoefficient(MathProblem p)
        {
            // Coefficient of x^n in e^x Taylor series = 1/n!
            int n = Random.Range(2, 6);
            long factorial = 1;
            for (int i = 2; i <= n; i++) factorial *= i;

            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"What is the coefficient of x^{n} in the Maclaurin series of e^x?";
            p.equationLatex = $"[x^{{{n}}}]\\, e^x";
            p.correctAnswer = $"1/{factorial}";
            p.hint          = $"The Maclaurin series of e^x = Σ xⁿ/n!. For n={n}, it is 1/{n}!";
        }

        private void BuildConvergenceTest(MathProblem p)
        {
            // p-series: Σ 1/n^p converges iff p > 1
            int[] pValues = { 2, 3, 1 }; // last one diverges
            int pv = pValues[Random.Range(0, pValues.Length)];
            bool converges = pv > 1;

            p.format        = ProblemFormat.TrueFalse;
            p.questionText  = $"True or False: The series Σ 1/n^{pv} (n=1 to ∞) converges.";
            p.equationLatex = $"\\sum_{{n=1}}^{{\\infty}} \\frac{{1}}{{n^{{{pv}}}}} \\text{{ converges?}}";
            p.correctAnswer = converges ? "True" : "False";
            p.hint          = "p-series Σ 1/nᵖ converges if and only if p > 1.";
        }
    }
}
