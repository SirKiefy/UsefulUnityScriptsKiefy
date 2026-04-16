using UnityEngine;
using UnityEngine.SceneManagement;
using UsefulScripts.Core;

namespace UsefulScripts.MathRoguelike.Core
{
    /// <summary>
    /// Singleton that owns the current <see cref="RunData"/> and drives
    /// high-level game-state transitions (main menu → dungeon → game-over).
    /// </summary>
    public class MathRoguelikeGameManager : Singleton<MathRoguelikeGameManager>
    {
        // ── Scene names (set in Inspector or match your Build Settings) ──
        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene  = "MainMenu";
        [SerializeField] private string dungeonScene   = "Dungeon";
        [SerializeField] private string gameOverScene  = "GameOver";
        [SerializeField] private string victoryScene   = "Victory";

        // ── Default run config ────────────────────────────────────────
        [Header("Default Run Config")]
        [SerializeField] private int  startingHp         = 100;
        [SerializeField] private int  startingMp         = 50;
        [SerializeField] private int  floorsPerRun        = 5;
        [SerializeField] private MathDifficulty startDifficulty = MathDifficulty.High;

        // ── Runtime ───────────────────────────────────────────────────
        public RunData CurrentRun { get; private set; }
        public int FloorsPerRun  => floorsPerRun;

        // ── Events ────────────────────────────────────────────────────
        public event System.Action<RunData> OnRunStarted;
        public event System.Action<RunData> OnRunEnded;
        public event System.Action          OnFloorAdvanced;

        protected override void OnSingletonAwake()
        {
            Application.targetFrameRate = 60;
        }

        // ─────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>Starts a fresh run and loads the dungeon scene.</summary>
        public void StartNewRun(MathDifficulty difficulty = MathDifficulty.High)
        {
            CurrentRun = new RunData(startingHp, startingMp, difficulty);
            OnRunStarted?.Invoke(CurrentRun);
            SceneManager.LoadScene(dungeonScene);
        }

        /// <summary>Advances to the next floor, scaling difficulty.</summary>
        public void AdvanceFloor()
        {
            if (CurrentRun == null) return;

            CurrentRun.currentFloor++;
            CurrentRun.currentRoomIndex = 0;

            // Scale difficulty every 2 floors
            if (CurrentRun.currentFloor % 2 == 0)
                CurrentRun.difficulty = ScaleDifficulty(CurrentRun.difficulty);

            if (CurrentRun.currentFloor > floorsPerRun)
            {
                Victory();
                return;
            }

            OnFloorAdvanced?.Invoke();
        }

        /// <summary>Called when the player's HP reaches 0.</summary>
        public void TriggerGameOver()
        {
            OnRunEnded?.Invoke(CurrentRun);
            SceneManager.LoadScene(gameOverScene);
        }

        /// <summary>Called when the player clears all floors.</summary>
        public void Victory()
        {
            OnRunEnded?.Invoke(CurrentRun);
            SceneManager.LoadScene(victoryScene);
        }

        public void ReturnToMainMenu()
        {
            CurrentRun = null;
            SceneManager.LoadScene(mainMenuScene);
        }

        // ─────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────

        private MathDifficulty ScaleDifficulty(MathDifficulty current)
        {
            return current switch
            {
                MathDifficulty.High     => MathDifficulty.VeryHigh,
                MathDifficulty.VeryHigh => MathDifficulty.Extreme,
                _                       => MathDifficulty.Extreme
            };
        }
    }
}
