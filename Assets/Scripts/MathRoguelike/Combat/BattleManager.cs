using System.Collections;
using UnityEngine;
using UsefulScripts.MathRoguelike.Entities;
using UsefulScripts.MathRoguelike.Math;
using UsefulScripts.MathRoguelike.Relics;

namespace UsefulScripts.MathRoguelike.Combat
{
    public enum BattlePhase
    {
        Idle,
        PresentingProblem,
        AwaitingAnswer,
        Resolving,
        Victory,
        Defeat
    }

    /// <summary>
    /// Orchestrates a single combat encounter:
    ///   1. Present a math problem
    ///   2. Wait for player input (or timeout)
    ///   3. Evaluate → deal damage / take damage
    ///   4. Repeat until enemy is dead or player is dead
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MathProblemGenerator problemGenerator;
        [SerializeField] private PlayerStats           playerStats;
        [SerializeField] private RelicManager          relicManager;

        [Header("Settings")]
        [SerializeField] private float answerTimeLimit = 30f; // seconds per problem

        // ── Runtime ───────────────────────────────────────────────────
        public BattlePhase Phase        { get; private set; } = BattlePhase.Idle;
        public EnemyInstance Enemy      { get; private set; }
        public MathProblem CurrentProblem { get; private set; }
        public float TimeRemaining      { get; private set; }
        public bool  HasRerolled        { get; private set; }

        // ── Events ────────────────────────────────────────────────────
        public event System.Action<MathProblem>       OnProblemPresented;
        public event System.Action<bool, int>         OnAnswerResolved;  // (correct, damageValue)
        public event System.Action<EnemyInstance>     OnEnemyDefeated;
        public event System.Action                    OnPlayerDefeated;
        public event System.Action<float>             OnTimerTick;

        private Coroutine _timerCoroutine;

        // ─────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>Starts a new battle against the given enemy.</summary>
        public void StartBattle(EnemyInstance enemy)
        {
            Enemy      = enemy;
            HasRerolled = false;
            Phase       = BattlePhase.PresentingProblem;
            PresentNextProblem();
        }

        /// <summary>Called by the UI when the player submits an answer.</summary>
        public void SubmitAnswer(string playerAnswer)
        {
            if (Phase != BattlePhase.AwaitingAnswer) return;

            StopTimer();
            float timeRemainingNorm = TimeRemaining / EffectiveTimeLimit;
            ResolveAnswer(playerAnswer, timeRemainingNorm);
        }

        /// <summary>Re-rolls the current problem (once per battle, costs MP).</summary>
        public void RerollProblem(int mpCost = 15)
        {
            if (HasRerolled || Phase != BattlePhase.AwaitingAnswer) return;
            if (playerStats.Mp < mpCost) return;

            playerStats.SpendMp(mpCost);
            HasRerolled = true;
            StopTimer();
            PresentNextProblem();
        }

        /// <summary>Reveals the hint for the current problem (costs MP).</summary>
        public void RevealHint()
        {
            if (CurrentProblem == null) return;
            if (relicManager.HasEffect(RelicEffectType.HintFree))
                return; // free hint via relic — no MP cost

            playerStats.SpendMp(CurrentProblem.mpCostToRevealHint);
        }

        // ─────────────────────────────────────────────────────────────
        //  Private flow
        // ─────────────────────────────────────────────────────────────

        private void PresentNextProblem()
        {
            var run = Core.MathRoguelikeGameManager.Instance?.CurrentRun;
            MathDifficulty diff = run?.difficulty ?? MathDifficulty.High;

            CurrentProblem = problemGenerator.GetProblem(diff);
            Phase          = BattlePhase.AwaitingAnswer;
            TimeRemaining  = EffectiveTimeLimit;

            OnProblemPresented?.Invoke(CurrentProblem);
            _timerCoroutine = StartCoroutine(RunTimer());
        }

        private void ResolveAnswer(string answer, float timeRemainingNorm)
        {
            Phase = BattlePhase.Resolving;

            bool correct = CurrentProblem.CheckAnswer(answer);

            if (!correct && relicManager.HasEffect(RelicEffectType.PartialCredit))
            {
                // Partial credit: deal half damage, take no damage
                int partialDmg = DamageCalculator.PartialCreditDamage(CurrentProblem, playerStats);
                Enemy.TakeDamage(partialDmg);
                OnAnswerResolved?.Invoke(false, partialDmg);
            }
            else if (correct)
            {
                int dmg = DamageCalculator.PlayerAttackDamage(CurrentProblem, playerStats, timeRemainingNorm);
                Enemy.TakeDamage(dmg);

                // Relic: heal on correct
                if (relicManager.HasEffect(RelicEffectType.HealOnCorrect))
                    playerStats.Heal(5);
                if (relicManager.HasEffect(RelicEffectType.MpOnCorrect))
                    playerStats.RestoreMp(5);

                var runData = Core.MathRoguelikeGameManager.Instance?.CurrentRun;
                if (runData != null)
                {
                    runData.problemsSolved++;
                    runData.totalDamageDealt += dmg;
                    runData.score            += CurrentProblem.baseScoreReward;
                }

                OnAnswerResolved?.Invoke(true, dmg);
            }
            else
            {
                // Wrong answer: enemy attacks
                int enemyDmg = DamageCalculator.EnemyAttackDamage(CurrentProblem, Enemy, playerStats);
                playerStats.TakeDamage(enemyDmg);

                var runData = Core.MathRoguelikeGameManager.Instance?.CurrentRun;
                if (runData != null) runData.problemsFailed++;

                OnAnswerResolved?.Invoke(false, -enemyDmg);
            }

            CheckBattleOutcome();
        }

        private void CheckBattleOutcome()
        {
            if (!playerStats.Alive)
            {
                // Check ReviveOnce relic
                if (relicManager.ConsumeRelic(RelicEffectType.ReviveOnce))
                {
                    playerStats.Heal(1); // survive with 1 HP
                }
                else
                {
                    Phase = BattlePhase.Defeat;
                    OnPlayerDefeated?.Invoke();
                    return;
                }
            }

            if (!Enemy.IsAlive)
            {
                Phase = BattlePhase.Victory;
                var run = Core.MathRoguelikeGameManager.Instance?.CurrentRun;
                if (run != null)
                {
                    run.gold            += Enemy.Data.goldReward;
                    run.score           += Enemy.Data.scoreReward;
                    run.enemiesDefeated++;
                }
                OnEnemyDefeated?.Invoke(Enemy);
                return;
            }

            // Battle continues
            Phase = BattlePhase.PresentingProblem;
            PresentNextProblem();
        }

        // ── Timer ─────────────────────────────────────────────────────

        private float EffectiveTimeLimit =>
            answerTimeLimit + (playerStats?.AnswerTimeBonus ?? 0f);

        private IEnumerator RunTimer()
        {
            while (TimeRemaining > 0f)
            {
                yield return null;
                TimeRemaining -= Time.deltaTime;
                OnTimerTick?.Invoke(TimeRemaining);
            }

            // Time's up — treat as wrong answer
            if (Phase == BattlePhase.AwaitingAnswer)
                ResolveAnswer(string.Empty, 0f);
        }

        private void StopTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }
    }
}
