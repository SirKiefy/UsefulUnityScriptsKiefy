using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UsefulScripts.MathRoguelike.Math;

namespace UsefulScripts.MathRoguelike.UI
{
    /// <summary>
    /// Displays a <see cref="MathProblem"/> in the battle UI.
    /// Renders the equation via <see cref="EquationRenderer"/>, shows
    /// multiple-choice buttons when applicable, and exposes a hint panel.
    /// </summary>
    public class EquationDisplayUI : MonoBehaviour
    {
        [Header("Question")]
        [SerializeField] private TMP_Text        questionText;
        [SerializeField] private EquationRenderer equationRenderer;

        [Header("Multiple Choice")]
        [SerializeField] private GameObject      choiceContainer;
        [SerializeField] private Button[]        choiceButtons;     // 4 buttons A-D
        [SerializeField] private TMP_Text[]      choiceLabels;

        [Header("Hint")]
        [SerializeField] private GameObject      hintPanel;
        [SerializeField] private TMP_Text        hintText;
        [SerializeField] private TMP_Text        hintCostLabel;

        [Header("Topic / Difficulty Badge")]
        [SerializeField] private TMP_Text        topicLabel;
        [SerializeField] private TMP_Text        difficultyLabel;

        private MathProblem _currentProblem;

        // Event fired when a multiple-choice option is selected
        public event System.Action<string> OnChoiceSelected;

        // ─────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────

        public void Display(MathProblem problem)
        {
            _currentProblem = problem;
            hintPanel?.SetActive(false);

            // Question text
            if (questionText) questionText.text = problem.questionText;

            // Equation
            if (equationRenderer)
                equationRenderer.Render(problem.equationLatex);

            // Badge
            if (topicLabel)      topicLabel.text      = problem.topic.ToString();
            if (difficultyLabel) difficultyLabel.text  = DifficultyString(problem.difficulty);

            // Multiple choice
            bool isMultiChoice = problem.format == ProblemFormat.MultipleChoice;
            choiceContainer?.SetActive(isMultiChoice);

            if (isMultiChoice) SetupChoices(problem.choices);
        }

        /// <summary>Shows the hint panel for the current problem.</summary>
        public void ShowHint()
        {
            if (_currentProblem == null) return;
            if (hintPanel)   hintPanel.SetActive(true);
            if (hintText)    hintText.text     = _currentProblem.hint;
            if (hintCostLabel) hintCostLabel.text = $"Cost: {_currentProblem.mpCostToRevealHint} MP";
        }

        // ─────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────

        private void SetupChoices(List<string> choices)
        {
            string[] labels = { "A", "B", "C", "D" };

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool active = i < choices.Count;
                choiceButtons[i].gameObject.SetActive(active);

                if (!active) continue;

                string choice = choices[i];
                string label  = $"{labels[i]}) {choice}";
                if (choiceLabels != null && i < choiceLabels.Length)
                    choiceLabels[i].text = label;

                // Capture for lambda
                int    idx  = i;
                string text = choice;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected?.Invoke(text));
            }
        }

        private static string DifficultyString(MathDifficulty d) => d switch
        {
            MathDifficulty.High     => "★★☆☆☆",
            MathDifficulty.VeryHigh => "★★★★☆",
            MathDifficulty.Extreme  => "★★★★★",
            _                       => "★☆☆☆☆"
        };
    }
}
