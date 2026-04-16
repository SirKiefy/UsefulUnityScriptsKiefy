using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates trigonometry problems ranging from exact values to
    /// inverse trig and complex identities.
    /// </summary>
    public class TrigonometryGenerator : IProblemTypeGenerator
    {
        // Common exact values table: angle (degrees) → (sin, cos, tan string)
        private static readonly (int deg, string sin, string cos, string tan)[] ExactValues =
        {
            (0,   "0",        "1",        "0"),
            (30,  "1/2",      "√3/2",     "1/√3"),
            (45,  "√2/2",     "√2/2",     "1"),
            (60,  "√3/2",     "1/2",      "√3"),
            (90,  "1",        "0",        "undefined"),
            (120, "√3/2",     "-1/2",     "-√3"),
            (135, "√2/2",     "-√2/2",    "-1"),
            (150, "1/2",      "-√3/2",    "-1/√3"),
            (180, "0",        "-1",       "0"),
            (270, "-1",       "0",        "undefined"),
        };

        private static readonly string[] Identities =
        {
            "sin^{2}(x)+cos^{2}(x)",
            "1+tan^{2}(x)",
            "1+cot^{2}(x)",
        };

        private static readonly string[] IdentityAnswers = { "1", "sec^{2}(x)", "csc^{2}(x)" };

        public MathProblem Generate(MathDifficulty difficulty)
        {
            var problem = ScriptableObject.CreateInstance<MathProblem>();
            problem.topic      = MathTopic.Trigonometry;
            problem.difficulty = difficulty;

            if (difficulty == MathDifficulty.High)
                BuildExactValueProblem(problem);
            else if (difficulty == MathDifficulty.VeryHigh)
                BuildIdentityProblem(problem);
            else
                BuildInverseProblem(problem);

            problem.baseGoldReward  = 10 + (int)difficulty * 5;
            problem.baseScoreReward = 100 + (int)difficulty * 50;
            problem.baseDamage      = 20 + (int)difficulty * 10;
            problem.penaltyDamage   = 10 + (int)difficulty * 5;
            return problem;
        }

        private void BuildExactValueProblem(MathProblem p)
        {
            var entry = ExactValues[Random.Range(0, ExactValues.Length)];
            string[] funcs = { "sin", "cos", "tan" };
            string func = funcs[Random.Range(0, 3)];
            string answer = func switch
            {
                "sin" => entry.sin,
                "cos" => entry.cos,
                _     => entry.tan
            };

            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"Find the exact value of {func}({entry.deg}°).";
            p.equationLatex = $"\\{func}({entry.deg}^{{\\circ}})";
            p.correctAnswer = answer;
            p.hint          = $"Recall the unit circle. {entry.deg}° is a standard angle.";
        }

        private void BuildIdentityProblem(MathProblem p)
        {
            int idx = Random.Range(0, Identities.Length);
            p.format        = ProblemFormat.ExactExpression;
            p.questionText  = $"Simplify the expression: {Identities[idx]}";
            p.equationLatex = Identities[idx];
            p.correctAnswer = IdentityAnswers[idx];
            p.hint          = "Use Pythagorean identities.";
        }

        private void BuildInverseProblem(MathProblem p)
        {
            // arcsin / arccos / arctan of exact values
            var entry = ExactValues[Random.Range(0, 4)]; // 0–60 degrees for clean arcsin
            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = $"Evaluate arcsin({entry.sin}) in degrees.";
            p.equationLatex = $"\\arcsin\\left({entry.sin}\\right)";
            p.correctAnswer = entry.deg.ToString();
            p.hint          = "arcsin returns values in [-90°, 90°].";
        }
    }
}
