using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UsefulScripts.MathRoguelike.Combat;
using UsefulScripts.MathRoguelike.Entities;
using UsefulScripts.MathRoguelike.Math;
using UsefulScripts.MathRoguelike.Relics;

namespace UsefulScripts.MathRoguelike.UI
{
    /// <summary>
    /// Main battle screen controller. Wires together <see cref="BattleManager"/>,
    /// <see cref="EquationDisplayUI"/>, <see cref="MathInputUI"/>, and
    /// enemy/player status panels.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Sub-UIs")]
        [SerializeField] private EquationDisplayUI equationDisplay;
        [SerializeField] private MathInputUI       mathInput;

        [Header("Enemy Panel")]
        [SerializeField] private TMP_Text enemyNameLabel;
        [SerializeField] private Slider   enemyHpSlider;
        [SerializeField] private TMP_Text enemyHpLabel;
        [SerializeField] private Image    enemyPortrait;

        [Header("Timer")]
        [SerializeField] private Slider   timerSlider;
        [SerializeField] private TMP_Text timerLabel;

        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private float    feedbackDisplayTime = 2f;

        [Header("Action Buttons")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button rerollButton;

        [Header("References")]
        [SerializeField] private BattleManager battleManager;

        private float _maxTime;
        private Coroutine _feedbackCoroutine;

        // ─────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            battleManager.OnProblemPresented += HandleProblemPresented;
            battleManager.OnAnswerResolved   += HandleAnswerResolved;
            battleManager.OnEnemyDefeated    += HandleEnemyDefeated;
            battleManager.OnPlayerDefeated   += HandlePlayerDefeated;
            battleManager.OnTimerTick        += HandleTimerTick;

            equationDisplay.OnChoiceSelected += battleManager.SubmitAnswer;
            mathInput.OnAnswerSubmitted      += battleManager.SubmitAnswer;

            hintButton?.onClick.AddListener(HandleHintClicked);
            rerollButton?.onClick.AddListener(HandleRerollClicked);
        }

        private void OnDisable()
        {
            battleManager.OnProblemPresented -= HandleProblemPresented;
            battleManager.OnAnswerResolved   -= HandleAnswerResolved;
            battleManager.OnEnemyDefeated    -= HandleEnemyDefeated;
            battleManager.OnPlayerDefeated   -= HandlePlayerDefeated;
            battleManager.OnTimerTick        -= HandleTimerTick;

            equationDisplay.OnChoiceSelected -= battleManager.SubmitAnswer;
            mathInput.OnAnswerSubmitted      -= battleManager.SubmitAnswer;

            hintButton?.onClick.RemoveAllListeners();
            rerollButton?.onClick.RemoveAllListeners();
        }

        /// <summary>Initialises the UI for a new battle.</summary>
        public void InitialiseBattle(EnemyInstance enemy)
        {
            UpdateEnemyPanel(enemy);
            enemy.OnHpChanged += (hp, max) => UpdateEnemyHp(hp, max);
            battleManager.StartBattle(enemy);
        }

        // ─────────────────────────────────────────────────────────────
        //  Event handlers
        // ─────────────────────────────────────────────────────────────

        private void HandleProblemPresented(MathProblem problem)
        {
            equationDisplay.Display(problem);
            mathInput.Clear();
            mathInput.SetActive(problem.format != ProblemFormat.MultipleChoice);
            _maxTime = battleManager.TimeRemaining;
            SetFeedback(string.Empty);
        }

        private void HandleAnswerResolved(bool correct, int value)
        {
            if (correct)
                ShowFeedback($"✓ Correct! -{value} HP to enemy!", Color.green);
            else if (value < 0)
                ShowFeedback($"✗ Wrong! You took {-value} damage!", Color.red);
            else
                ShowFeedback($"⚡ Partial credit! -{value} HP to enemy.", Color.yellow);
        }

        private void HandleEnemyDefeated(EnemyInstance enemy)
        {
            ShowFeedback($"🏆 {enemy.Data.displayName} defeated!", Color.cyan);
            mathInput.SetActive(false);
        }

        private void HandlePlayerDefeated()
        {
            ShowFeedback("💀 You have been defeated...", Color.red);
            mathInput.SetActive(false);
        }

        private void HandleTimerTick(float remaining)
        {
            if (timerSlider) { timerSlider.maxValue = _maxTime; timerSlider.value = remaining; }
            if (timerLabel)  timerLabel.text = Mathf.CeilToInt(remaining).ToString();
        }

        private void HandleHintClicked()
        {
            battleManager.RevealHint();
            equationDisplay.ShowHint();
        }

        private void HandleRerollClicked() => battleManager.RerollProblem();

        // ─────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────

        private void UpdateEnemyPanel(EnemyInstance enemy)
        {
            if (enemyNameLabel) enemyNameLabel.text = enemy.Data.displayName;
            if (enemyPortrait && enemy.Data.portrait) enemyPortrait.sprite = enemy.Data.portrait;
            UpdateEnemyHp(enemy.CurrentHp, enemy.Data.maxHp);
        }

        private void UpdateEnemyHp(int current, int max)
        {
            if (enemyHpSlider) { enemyHpSlider.minValue = 0; enemyHpSlider.maxValue = max; enemyHpSlider.value = current; }
            if (enemyHpLabel)  enemyHpLabel.text = $"{current}/{max}";
        }

        private void ShowFeedback(string message, Color colour)
        {
            SetFeedback(message);
            if (feedbackLabel) feedbackLabel.color = colour;
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(ClearFeedbackAfter(feedbackDisplayTime));
        }

        private void SetFeedback(string message)
        {
            if (feedbackLabel) feedbackLabel.text = message;
        }

        private System.Collections.IEnumerator ClearFeedbackAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetFeedback(string.Empty);
        }
    }
}
