using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates vector calculus problems: dot/cross products, divergence, curl, line integrals.
    /// High = dot/cross product; VeryHigh = div/curl; Extreme = line/surface integrals.
    /// </summary>
    public class VectorCalculusGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.VectorCalculus;
            p.difficulty = difficulty;
            p.format     = ProblemFormat.ExactNumeric;
            p.baseGoldReward  = 15 + (int)difficulty * 10;
            p.baseScoreReward = 150 + (int)difficulty * 75;
            p.baseDamage      = 25 + (int)difficulty * 12;
            p.penaltyDamage   = 12 + (int)difficulty * 6;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    BuildDotProduct(p);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildDivergence(p);
                    break;
                default:
                    BuildCurlMagnitude(p);
                    break;
            }
            return p;
        }

        private void BuildDotProduct(MathProblem p)
        {
            int ax = Random.Range(-4, 5), ay = Random.Range(-4, 5), az = Random.Range(-4, 5);
            int bx = Random.Range(-4, 5), by = Random.Range(-4, 5), bz = Random.Range(-4, 5);
            int dot = ax * bx + ay * by + az * bz;

            p.questionText  = $"Find A·B for A=({ax},{ay},{az}) and B=({bx},{by},{bz}).";
            p.equationLatex = $"\\mathbf{{A}}\\cdot\\mathbf{{B}},\\quad \\mathbf{{A}}=({ax},{ay},{az}),\\; \\mathbf{{B}}=({bx},{by},{bz})";
            p.correctAnswer = dot.ToString();
            p.hint          = "A·B = AxBx + AyBy + AzBz";
        }

        private void BuildDivergence(MathProblem p)
        {
            // div(F) for F = ax·i + by·j + cz·k = a+b+c (constants at a point — simplified)
            int a = Random.Range(1, 5), b = Random.Range(1, 5), c = Random.Range(1, 5);
            int div = a + b + c;

            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = $"Find div(F) for F = {a}x·i + {b}y·j + {c}z·k.";
            p.equationLatex = $"\\nabla\\cdot\\mathbf{{F}},\\quad \\mathbf{{F}}={a}x\\,\\hat{{i}}+{b}y\\,\\hat{{j}}+{c}z\\,\\hat{{k}}";
            p.correctAnswer = div.ToString();
            p.hint          = "div(F) = ∂Fx/∂x + ∂Fy/∂y + ∂Fz/∂z";
        }

        private void BuildCurlMagnitude(MathProblem p)
        {
            // curl(F) for F = y·i − x·j + 0·k = (0,0,-2)  |curl| = 2
            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = "Find |curl(F)| for F = y·î − x·ĵ.";
            p.equationLatex = "|\\nabla\\times\\mathbf{F}|,\\quad \\mathbf{F}=y\\,\\hat{i}-x\\,\\hat{j}";
            p.correctAnswer = "2";
            p.hint          = "curl(F)_z = ∂Fy/∂x − ∂Fx/∂y = −1 − 1 = −2, so |curl| = 2.";
        }
    }
}
