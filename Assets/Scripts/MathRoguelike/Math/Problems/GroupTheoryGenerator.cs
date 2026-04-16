using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Stub generator for Group Theory problems (abstract algebra).
    /// High = group order; VeryHigh = subgroup check; Extreme = coset/quotient.
    /// </summary>
    public class GroupTheoryGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.GroupTheory;
            p.difficulty = difficulty;
            p.format     = ProblemFormat.TrueFalse;
            p.baseGoldReward  = 25 + (int)difficulty * 15;
            p.baseScoreReward = 250 + (int)difficulty * 150;
            p.baseDamage      = 35 + (int)difficulty * 20;
            p.penaltyDamage   = 18 + (int)difficulty * 12;

            int variant = Random.Range(0, 3);
            switch (variant)
            {
                case 0: BuildGroupOrderProblem(p);    break;
                case 1: BuildLagrangeTheorem(p);      break;
                default: BuildAbelianQuestion(p);     break;
            }
            return p;
        }

        private void BuildGroupOrderProblem(MathProblem p)
        {
            // |Z_n| = n
            int n = Random.Range(3, 13);
            p.format        = ProblemFormat.ExactNumeric;
            p.questionText  = $"What is the order of the cyclic group ℤ_{n}?";
            p.equationLatex = $"|\\mathbb{{Z}}_{{{n}}}|";
            p.correctAnswer = n.ToString();
            p.hint          = "The cyclic group ℤₙ has exactly n elements.";
        }

        private void BuildLagrangeTheorem(MathProblem p)
        {
            // Lagrange: |H| divides |G|
            int g = Random.Range(2, 4) * Random.Range(2, 4); // composite
            int[] divisors = System.Array.FindAll(
                new[] { 1, 2, 3, 4, 6, 8, 9, 12 },
                d => d > 1 && d < g && g % d == 0);
            int h = (divisors.Length > 0) ? divisors[Random.Range(0, divisors.Length)] : 1;
            bool valid = (g % h == 0);

            p.format        = ProblemFormat.TrueFalse;
            p.questionText  = $"True or False: A subgroup of order {h} can exist inside a group of order {g}.";
            p.equationLatex = $"H \\leq G,\\; |H|={h},\\; |G|={g}";
            p.correctAnswer = valid ? "True" : "False";
            p.hint          = "By Lagrange's theorem, |H| must divide |G|.";
        }

        private void BuildAbelianQuestion(MathProblem p)
        {
            p.format        = ProblemFormat.TrueFalse;
            p.questionText  = "True or False: Every cyclic group is abelian.";
            p.equationLatex = "\\text{Every cyclic group is abelian?}";
            p.correctAnswer = "True";
            p.hint          = "In a cyclic group G=⟨g⟩, any two elements gᵃ and gᵇ commute since gᵃgᵇ = gᵃ⁺ᵇ = gᵇgᵃ.";
        }
    }
}
