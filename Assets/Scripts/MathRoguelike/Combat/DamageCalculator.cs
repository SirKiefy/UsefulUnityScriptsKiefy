using UnityEngine;
using UsefulScripts.MathRoguelike.Entities;
using UsefulScripts.MathRoguelike.Math;
using UsefulScripts.MathRoguelike.Relics;

namespace UsefulScripts.MathRoguelike.Combat
{
    /// <summary>
    /// Calculates damage for correct answers, wrong answers, and enemy attacks,
    /// applying relic bonuses and difficulty multipliers.
    /// </summary>
    public static class DamageCalculator
    {
        // ── Difficulty multipliers ────────────────────────────────────
        private static readonly float[] DifficultyMultipliers =
        {
            1.0f,   // High
            1.4f,   // VeryHigh
            2.0f,   // Extreme
        };

        // ── Player → Enemy ────────────────────────────────────────────

        /// <summary>
        /// Damage the player deals on a CORRECT answer.
        /// Scales with answer speed (0–1 normalised remaining time).
        /// </summary>
        public static int PlayerAttackDamage(
            MathProblem problem,
            PlayerStats player,
            float timeRemainingNorm = 1f)
        {
            float multiplier  = DifficultyMultiplier(problem.difficulty);
            float speedBonus  = 1f + (timeRemainingNorm * 0.5f); // up to 1.5× for fast answers
            int   rawDamage   = Mathf.RoundToInt(problem.baseDamage * multiplier * speedBonus);
            return Mathf.Max(1, rawDamage + player.BonusDamage);
        }

        // ── Enemy → Player ────────────────────────────────────────────

        /// <summary>
        /// Damage the enemy deals when the player answers INCORRECTLY.
        /// </summary>
        public static int EnemyAttackDamage(
            MathProblem problem,
            EnemyInstance enemy,
            PlayerStats player)
        {
            float multiplier = DifficultyMultiplier(problem.difficulty);
            int raw = Mathf.RoundToInt(
                Mathf.Max(problem.penaltyDamage, enemy.RollAttackDamage()) * multiplier);
            return Mathf.Max(0, raw - player.DamageReduction);
        }

        // ── Partial credit (relic) ─────────────────────────────────────

        /// <summary>
        /// Partial-credit damage (50 % of full correct damage) when the
        /// PartialCredit relic is active and the answer is close but wrong.
        /// </summary>
        public static int PartialCreditDamage(MathProblem problem, PlayerStats player)
        {
            int full = PlayerAttackDamage(problem, player, 0.5f);
            return Mathf.Max(1, full / 2);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static float DifficultyMultiplier(MathDifficulty d)
        {
            int idx = Mathf.Clamp((int)d, 0, DifficultyMultipliers.Length - 1);
            return DifficultyMultipliers[idx];
        }
    }
}
