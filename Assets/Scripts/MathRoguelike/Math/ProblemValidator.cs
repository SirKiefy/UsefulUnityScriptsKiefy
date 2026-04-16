using System;
using UnityEngine;

namespace UsefulScripts.MathRoguelike.Math
{
    /// <summary>
    /// Validates player answers against the correct answer for a given
    /// <see cref="ProblemFormat"/>. Handles numeric tolerance and
    /// algebraic expression normalisation.
    /// </summary>
    public static class ProblemValidator
    {
        /// <summary>Tolerance for floating-point comparisons.</summary>
        private const float NumericTolerance = 0.01f;

        public static bool Validate(string playerAnswer, string correctAnswer, ProblemFormat format)
        {
            if (string.IsNullOrWhiteSpace(playerAnswer)) return false;

            return format switch
            {
                ProblemFormat.MultipleChoice   => ValidateMultipleChoice(playerAnswer, correctAnswer),
                ProblemFormat.ExactNumeric     => ValidateNumeric(playerAnswer, correctAnswer),
                ProblemFormat.ExactExpression  => ValidateExpression(playerAnswer, correctAnswer),
                ProblemFormat.TrueFalse        => ValidateTrueFalse(playerAnswer, correctAnswer),
                _                              => false
            };
        }

        // ── Format-specific validators ────────────────────────────────

        private static bool ValidateMultipleChoice(string player, string correct)
        {
            return string.Equals(player.Trim(), correct.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidateNumeric(string player, string correct)
        {
            // Try exact string match first (handles π, e, etc. spelled out)
            if (string.Equals(NormaliseExpression(player), NormaliseExpression(correct),
                    StringComparison.OrdinalIgnoreCase))
                return true;

            // Try floating-point comparison
            if (TryParseFloat(player,  out float pVal) &&
                TryParseFloat(correct, out float cVal))
            {
                return Mathf.Abs(pVal - cVal) <= NumericTolerance;
            }

            return false;
        }

        private static bool ValidateExpression(string player, string correct)
        {
            return string.Equals(NormaliseExpression(player),
                                 NormaliseExpression(correct),
                                 StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidateTrueFalse(string player, string correct)
        {
            return string.Equals(player.Trim(), correct.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // ── Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Strips whitespace, lowercases, replaces common aliases so that
        /// e.g. "2*sin(x)" and "2sin(x)" compare equal.
        /// </summary>
        private static string NormaliseExpression(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            s = s.ToLowerInvariant()
                 .Replace(" ", "")
                 .Replace("*", "")          // 2*x  → 2x
                 .Replace("×", "")          // unicode multiply
                 .Replace("pi", "π")
                 .Replace("inf", "∞")
                 .Replace("infinity", "∞")
                 .Replace("+-", "-")
                 .Replace("-+", "-");

            return s;
        }

        private static bool TryParseFloat(string s, out float value)
        {
            // Replace π/e before parsing
            s = s.Replace("π", System.Math.PI.ToString("F10"))
                 .Replace("pi", System.Math.PI.ToString("F10"))
                 .Trim();

            return float.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }
    }
}
