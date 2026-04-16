using System.Collections.Generic;
using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math
{
    /// <summary>
    /// ScriptableObject that defines a single math problem template.
    /// Problems can be static (hardcoded) or dynamic (generated at runtime).
    /// </summary>
    [CreateAssetMenu(fileName = "MathProblem", menuName = "MathRoguelike/Math Problem")]
    public class MathProblem : ScriptableObject
    {
        [Header("Identity")]
        public string problemId;
        public MathTopic topic;
        public MathDifficulty difficulty;
        public ProblemFormat format;

        [Header("Content")]
        [TextArea(2, 6)]
        public string questionText;

        /// <summary>
        /// LaTeX-style equation string rendered by <see cref="EquationRenderer"/>.
        /// e.g. "\\int_0^\\pi \\sin(x)\\,dx"
        /// </summary>
        [TextArea(1, 4)]
        public string equationLatex;

        [Header("Answer")]
        public string correctAnswer;

        [Header("Multiple Choice (if format == MultipleChoice)")]
        public List<string> choices = new List<string>();

        [Header("Hints")]
        [TextArea(1, 4)]
        public string hint;
        public int    mpCostToRevealHint = 10;

        [Header("Rewards")]
        public int baseGoldReward  = 10;
        public int baseScoreReward = 100;

        [Header("Combat")]
        [Tooltip("Damage dealt to the enemy on a correct answer.")]
        public int baseDamage      = 20;
        [Tooltip("Damage taken from the enemy on a wrong answer.")]
        public int penaltyDamage   = 15;

        // ── Validation ────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="playerAnswer"/> matches the correct answer.
        /// Delegates to <see cref="ProblemValidator"/>.
        /// </summary>
        public bool CheckAnswer(string playerAnswer)
            => ProblemValidator.Validate(playerAnswer, correctAnswer, format);
    }
}
