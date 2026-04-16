using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math.Problems
{
    /// <summary>
    /// Generates number theory problems: divisibility, modular arithmetic, primes.
    /// High = GCD/LCM; VeryHigh = modular arithmetic; Extreme = Euler's theorem.
    /// </summary>
    public class NumberTheoryGenerator : IProblemTypeGenerator
    {
        public MathProblem Generate(MathDifficulty difficulty)
        {
            var p = ScriptableObject.CreateInstance<MathProblem>();
            p.topic      = MathTopic.NumberTheory;
            p.difficulty = difficulty;
            p.format     = ProblemFormat.ExactNumeric;
            p.baseGoldReward  = 20 + (int)difficulty * 10;
            p.baseScoreReward = 200 + (int)difficulty * 100;
            p.baseDamage      = 25 + (int)difficulty * 15;
            p.penaltyDamage   = 12 + (int)difficulty * 8;

            switch (difficulty)
            {
                case MathDifficulty.High:
                    BuildGCDProblem(p);
                    break;
                case MathDifficulty.VeryHigh:
                    BuildModularArithmetic(p);
                    break;
                default:
                    BuildEulerTotient(p);
                    break;
            }
            return p;
        }

        private void BuildGCDProblem(MathProblem p)
        {
            int g = Random.Range(2, 10);
            int a = g * Random.Range(2, 10);
            int b = g * Random.Range(2, 10);

            p.questionText  = $"Find gcd({a}, {b}).";
            p.equationLatex = $"\\gcd({a},\\, {b})";
            p.correctAnswer = g.ToString();
            p.hint          = "Use the Euclidean algorithm: gcd(a,b) = gcd(b, a mod b).";
        }

        private void BuildModularArithmetic(MathProblem p)
        {
            int m = Random.Range(5, 20);
            int a = Random.Range(2, 50);
            int b = Random.Range(2, 50);
            int result = ((a * b) % m + m) % m;

            p.questionText  = $"Compute ({a} × {b}) mod {m}.";
            p.equationLatex = $"({a} \\times {b}) \\mod {m}";
            p.correctAnswer = result.ToString();
            p.hint          = "Reduce each factor mod m first, then multiply and reduce again.";
        }

        private void BuildEulerTotient(MathProblem p)
        {
            // φ(p) = p−1 for prime p
            int[] primes = { 7, 11, 13, 17, 19, 23 };
            int prime = primes[Random.Range(0, primes.Length)];
            int phi   = prime - 1;

            p.questionText  = $"Calculate Euler's totient function φ({prime}).";
            p.equationLatex = $"\\varphi({prime})";
            p.correctAnswer = phi.ToString();
            p.hint          = "For a prime p, φ(p) = p − 1.";
        }
    }
}
