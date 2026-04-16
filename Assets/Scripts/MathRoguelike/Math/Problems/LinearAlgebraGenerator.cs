using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates linear algebra problems: determinants, eigenvalues, matrix operations.
    /// High = 2×2 determinants; VeryHigh = eigenvalues; Extreme = diagonalisation.
    /// </summary>
    public class LinearAlgebraGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.LinearAlgebra;
            p.difficulty = difficulty;
            p.format     = ProblemFormat.ExactNumeric;
            p.baseGoldReward  = 15 + (int)difficulty * 10;
            p.baseScoreReward = 150 + (int)difficulty * 75;
            p.baseDamage      = 25 + (int)difficulty * 15;
            p.penaltyDamage   = 12 + (int)difficulty * 8;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    Build2x2Determinant(p);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildEigenvalue2x2(p);
                    break;
                default:
                    Build3x3Determinant(p);
                    break;
            }
            return p;
        }

        private void Build2x2Determinant(MathProblem p)
        {
            int a = Random.Range(-5, 6), b = Random.Range(-5, 6);
            int c = Random.Range(-5, 6), d = Random.Range(-5, 6);
            int det = a * d - b * c;

            p.questionText  = $"Find the determinant of the 2×2 matrix: [[{a},{b}],[{c},{d}]]";
            p.equationLatex = $"\\det\\begin{{pmatrix}}{a} & {b} \\\\ {c} & {d}\\end{{pmatrix}}";
            p.correctAnswer = det.ToString();
            p.hint          = "det([[a,b],[c,d]]) = ad − bc";
        }

        private void BuildEigenvalue2x2(MathProblem p)
        {
            // Diagonal matrix for simple eigenvalues
            int a = Random.Range(1, 7), d = Random.Range(1, 7);
            // Eigenvalues of [[a,0],[0,d]] are a and d
            int smaller = Mathf.Min(a, d);

            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = $"Find the smaller eigenvalue of the matrix: [[{a},0],[0,{d}]]";
            p.equationLatex = $"\\lambda \\text{{ of }}\\begin{{pmatrix}}{a} & 0 \\\\ 0 & {d}\\end{{pmatrix}}";
            p.correctAnswer = smaller.ToString();
            p.hint          = "For a diagonal matrix the eigenvalues are the diagonal entries.";
        }

        private void Build3x3Determinant(MathProblem p)
        {
            // Keep small to stay solvable by hand
            int a = Random.Range(-3, 4), b = Random.Range(-3, 4), c = Random.Range(-3, 4);
            int d = Random.Range(-3, 4), e = Random.Range(-3, 4), f = Random.Range(-3, 4);
            int g = Random.Range(-3, 4), h = Random.Range(-3, 4), k = Random.Range(-3, 4);

            int det = a * (e * k - f * h)
                    - b * (d * k - f * g)
                    + c * (d * h - e * g);

            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = $"Find det of the 3×3 matrix: [[{a},{b},{c}],[{d},{e},{f}],[{g},{h},{k}]]";
            p.equationLatex = $"\\det\\begin{{pmatrix}}{a}&{b}&{c}\\\\{d}&{e}&{f}\\\\{g}&{h}&{k}\\end{{pmatrix}}";
            p.correctAnswer = det.ToString();
            p.hint          = "Expand along the first row using cofactors.";
        }
    }
}
