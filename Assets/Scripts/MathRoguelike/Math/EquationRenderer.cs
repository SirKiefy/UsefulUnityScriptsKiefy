using System.Text;
using UnityEngine;
using TMPro;

namespace UsefulScripts.MathRoguelike.Math
{
    /// <summary>
    /// Converts a simplified LaTeX-like equation string into a rich-text
    /// string compatible with TextMeshPro's Rich Text tags, then applies
    /// it to a <see cref="TMP_Text"/> component.
    ///
    /// Supported tokens:
    ///   \frac{a}{b}   → rendered as  a/b  with vertical-bar separator
    ///   \sqrt{x}      → √(x)
    ///   ^{n}  / ^n    → superscript
    ///   _{n}  / _n    → subscript
    ///   \int          → ∫
    ///   \sum          → Σ
    ///   \prod         → Π
    ///   \infty        → ∞
    ///   \pi           → π
    ///   \theta        → θ
    ///   \alpha/beta/gamma/delta/lambda/mu/sigma/omega → Greek letters
    ///   \partial      → ∂
    ///   \nabla        → ∇
    ///   \times        → ×
    ///   \cdot         → ·
    ///   \leq/\geq/\neq/\approx → ≤ ≥ ≠ ≈
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class EquationRenderer : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float    superscriptScale = 0.65f;
        [SerializeField] private float    subscriptScale   = 0.65f;

        private void Reset()
        {
            label = GetComponent<TMP_Text>();
        }

        /// <summary>Renders the equation LaTeX string onto the attached TMP_Text.</summary>
        public void Render(string latex)
        {
            if (label == null) label = GetComponent<TMP_Text>();
            label.text = ToRichText(latex);
        }

        // ─────────────────────────────────────────────────────────────
        //  Core conversion
        // ─────────────────────────────────────────────────────────────

        public static string ToRichText(string latex)
        {
            if (string.IsNullOrEmpty(latex)) return string.Empty;

            var sb = new StringBuilder(latex);

            // ── Greek letters & symbols ──────────────────────────────
            Replace(sb, @"\alpha",   "α");
            Replace(sb, @"\beta",    "β");
            Replace(sb, @"\gamma",   "γ");
            Replace(sb, @"\Gamma",   "Γ");
            Replace(sb, @"\delta",   "δ");
            Replace(sb, @"\Delta",   "Δ");
            Replace(sb, @"\epsilon", "ε");
            Replace(sb, @"\zeta",    "ζ");
            Replace(sb, @"\eta",     "η");
            Replace(sb, @"\theta",   "θ");
            Replace(sb, @"\Theta",   "Θ");
            Replace(sb, @"\iota",    "ι");
            Replace(sb, @"\kappa",   "κ");
            Replace(sb, @"\lambda",  "λ");
            Replace(sb, @"\Lambda",  "Λ");
            Replace(sb, @"\mu",      "μ");
            Replace(sb, @"\nu",      "ν");
            Replace(sb, @"\xi",      "ξ");
            Replace(sb, @"\pi",      "π");
            Replace(sb, @"\Pi",      "Π");
            Replace(sb, @"\rho",     "ρ");
            Replace(sb, @"\sigma",   "σ");
            Replace(sb, @"\Sigma",   "Σ");
            Replace(sb, @"\tau",     "τ");
            Replace(sb, @"\phi",     "φ");
            Replace(sb, @"\Phi",     "Φ");
            Replace(sb, @"\chi",     "χ");
            Replace(sb, @"\psi",     "ψ");
            Replace(sb, @"\omega",   "ω");
            Replace(sb, @"\Omega",   "Ω");
            Replace(sb, @"\partial", "∂");
            Replace(sb, @"\nabla",   "∇");
            Replace(sb, @"\infty",   "∞");
            Replace(sb, @"\int",     "∫");
            Replace(sb, @"\oint",    "∮");
            Replace(sb, @"\sum",     "Σ");
            Replace(sb, @"\prod",    "Π");
            Replace(sb, @"\sqrt",    "√");
            Replace(sb, @"\times",   "×");
            Replace(sb, @"\cdot",    "·");
            Replace(sb, @"\leq",     "≤");
            Replace(sb, @"\geq",     "≥");
            Replace(sb, @"\neq",     "≠");
            Replace(sb, @"\approx",  "≈");
            Replace(sb, @"\pm",      "±");
            Replace(sb, @"\mp",      "∓");
            Replace(sb, @"\forall",  "∀");
            Replace(sb, @"\exists",  "∃");
            Replace(sb, @"\in",      "∈");
            Replace(sb, @"\notin",   "∉");
            Replace(sb, @"\subset",  "⊂");
            Replace(sb, @"\cup",     "∪");
            Replace(sb, @"\cap",     "∩");
            Replace(sb, @"\to",      "→");
            Replace(sb, @"\Rightarrow", "⇒");
            Replace(sb, @"\Leftrightarrow", "⟺");
            Replace(sb, @"\ldots",   "…");
            Replace(sb, @"\cdots",   "⋯");
            Replace(sb, @"\,",       " ");  // thin space
            Replace(sb, @"\!",       "");   // negative space

            // ── Structural transforms (order matters) ─────────────────
            string result = sb.ToString();
            result = TransformFrac(result);
            result = TransformSqrt(result);
            result = TransformSuperscript(result);
            result = TransformSubscript(result);

            return result;
        }

        // ─────────────────────────────────────────────────────────────
        //  Structural token parsers
        // ─────────────────────────────────────────────────────────────

        /// <summary>\frac{num}{den}  →  (num)/(den)</summary>
        private static string TransformFrac(string s)
        {
            const string token = @"\frac";
            while (true)
            {
                int idx = s.IndexOf(token, System.StringComparison.Ordinal);
                if (idx < 0) break;

                int after = idx + token.Length;
                if (!TryExtractBraced(s, after, out string num, out int end1)) break;
                if (!TryExtractBraced(s, end1,  out string den, out int end2)) break;

                string replacement = $"({num})/({den})";
                s = s.Substring(0, idx) + replacement + s.Substring(end2);
            }
            return s;
        }

        /// <summary>\sqrt{x}  →  √(x)</summary>
        private static string TransformSqrt(string s)
        {
            const string token = "√";
            // After symbol replacement \sqrt → √; handle √{expr}
            while (true)
            {
                int idx = s.IndexOf(token, System.StringComparison.Ordinal);
                if (idx < 0) break;
                int after = idx + token.Length;
                if (after < s.Length && s[after] == '{')
                {
                    if (!TryExtractBraced(s, after, out string inner, out int end)) break;
                    s = s.Substring(0, idx) + $"√({inner})" + s.Substring(end);
                }
                else break;
            }
            return s;
        }

        /// <summary>^{n} or ^n  →  TMP superscript tags</summary>
        private static string TransformSuperscript(string s)
        {
            var result = new StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '^')
                {
                    i++;
                    string content = ExtractArgument(s, ref i);
                    result.Append($"<sup>{content}</sup>");
                }
                else
                {
                    result.Append(s[i++]);
                }
            }
            return result.ToString();
        }

        /// <summary>_{n} or _n  →  TMP subscript tags</summary>
        private static string TransformSubscript(string s)
        {
            var result = new StringBuilder();
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '_')
                {
                    i++;
                    string content = ExtractArgument(s, ref i);
                    result.Append($"<sub>{content}</sub>");
                }
                else
                {
                    result.Append(s[i++]);
                }
            }
            return result.ToString();
        }

        // ─────────────────────────────────────────────────────────────
        //  Utilities
        // ─────────────────────────────────────────────────────────────

        private static void Replace(StringBuilder sb, string from, string to)
        {
            sb.Replace(from, to);
        }

        private static string ExtractArgument(string s, ref int i)
        {
            if (i >= s.Length) return "";
            if (s[i] == '{')
            {
                TryExtractBraced(s, i, out string inner, out int end);
                i = end;
                return inner;
            }
            // Single character argument
            return s[i++].ToString();
        }

        private static bool TryExtractBraced(string s, int start, out string inner, out int end)
        {
            inner = "";
            end   = start;
            if (start >= s.Length || s[start] != '{') return false;

            int depth = 0;
            int i = start;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '{')      { depth++; if (depth > 1) sb.Append(c); }
                else if (c == '}') { depth--; if (depth == 0) { end = i + 1; inner = sb.ToString(); return true; } else sb.Append(c); }
                else               { sb.Append(c); }
                i++;
            }
            return false;
        }
    }
}
