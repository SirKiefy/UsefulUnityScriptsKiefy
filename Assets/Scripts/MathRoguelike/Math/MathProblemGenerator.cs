using System.Collections.Generic;
using UnityEngine;
using UsefulScripts.MathRoguelike.Math.Problems;

namespace UsefulScripts.MathRoguelike.Math
{
    /// <summary>
    /// Generates <see cref="MathProblem"/> instances at runtime based on
    /// difficulty and topic. Pre-authored ScriptableObject problems can
    /// also be injected via <see cref="problemPool"/>.
    /// </summary>
    public class MathProblemGenerator : MonoBehaviour
    {
        [Header("Pre-authored Problem Pool (optional)")]
        [SerializeField] private List<MathProblem> problemPool = new List<MathProblem>();

        [Header("Procedural Weights (must sum to 1)")]
        [SerializeField] private float proceduralWeight = 0.6f;   // % procedurally generated
        // remaining 40% comes from the static pool when available

        private readonly Dictionary<MathTopic, IProblemTypeGenerator> _generators
            = new Dictionary<MathTopic, IProblemTypeGenerator>();

        private void Awake()
        {
            // Register topic generators
            _generators[MathTopic.Trigonometry]          = new TrigonometryGenerator();
            _generators[MathTopic.Calculus]              = new CalculusGenerator();
            _generators[MathTopic.LinearAlgebra]         = new LinearAlgebraGenerator();
            _generators[MathTopic.DifferentialEquations] = new DifferentialEquationsGenerator();
            _generators[MathTopic.ComplexAnalysis]       = new ComplexAnalysisGenerator();
            _generators[MathTopic.SeriesAndSequences]    = new SeriesSequencesGenerator();
            _generators[MathTopic.NumberTheory]          = new NumberTheoryGenerator();
            _generators[MathTopic.FourierAnalysis]       = new FourierAnalysisGenerator();
            _generators[MathTopic.VectorCalculus]        = new VectorCalculusGenerator();
            _generators[MathTopic.GroupTheory]           = new GroupTheoryGenerator();
        }

        /// <summary>
        /// Returns a problem appropriate for the given difficulty.
        /// Randomly picks a topic relevant to that difficulty tier.
        /// </summary>
        public MathProblem GetProblem(MathDifficulty difficulty)
        {
            // Decide whether to pull from pool or generate procedurally
            bool useProcedural = (Random.value < proceduralWeight) || problemPool.Count == 0;

            if (!useProcedural)
            {
                var pooled = GetFromPool(difficulty);
                if (pooled != null) return pooled;
            }

            MathTopic topic = PickTopic(difficulty);
            return GenerateProblem(topic, difficulty);
        }

        /// <summary>Generates a problem for a specific topic and difficulty.</summary>
        public MathProblem GetProblem(MathTopic topic, MathDifficulty difficulty)
            => GenerateProblem(topic, difficulty);

        // ─────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────

        private MathProblem GenerateProblem(MathTopic topic, MathDifficulty difficulty)
        {
            if (_generators.TryGetValue(topic, out var gen))
                return gen.Generate(difficulty);

            // Fallback: generic calculus problem
            return _generators[MathTopic.Calculus].Generate(difficulty);
        }

        private MathProblem GetFromPool(MathDifficulty difficulty)
        {
            var candidates = problemPool.FindAll(p => p.difficulty == difficulty);
            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        private MathTopic PickTopic(MathDifficulty difficulty)
        {
            return difficulty switch
            {
                MathDifficulty.High => RandomFrom(
                    MathTopic.Trigonometry,
                    MathTopic.Calculus,
                    MathTopic.LinearAlgebra,
                    MathTopic.SeriesAndSequences),

                MathDifficulty.VeryHigh => RandomFrom(
                    MathTopic.DifferentialEquations,
                    MathTopic.ComplexAnalysis,
                    MathTopic.LinearAlgebra,
                    MathTopic.VectorCalculus),

                MathDifficulty.Extreme => RandomFrom(
                    MathTopic.FourierAnalysis,
                    MathTopic.NumberTheory,
                    MathTopic.ComplexAnalysis,
                    MathTopic.GroupTheory),

                _ => MathTopic.Calculus
            };
        }

        private static MathTopic RandomFrom(params MathTopic[] topics)
            => topics[Random.Range(0, topics.Length)];
    }

    /// <summary>Interface every per-topic generator must implement.</summary>
    public interface IProblemTypeGenerator
    {
        MathProblem Generate(MathDifficulty difficulty);
    }
}
