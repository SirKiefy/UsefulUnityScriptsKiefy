using System.Collections.Generic;
using UnityEngine;

namespace UsefulScripts.MathRoguelike.Core
{
    /// <summary>
    /// Holds all persistent data for a single roguelike run.
    /// Passed between scenes and reset when a new run starts.
    /// </summary>
    [System.Serializable]
    public class RunData
    {
        // ── Player state ──────────────────────────────────────────────
        public int currentHp;
        public int maxHp;
        public int currentMp;
        public int maxMp;
        public int gold;
        public int score;

        // ── Progression ───────────────────────────────────────────────
        public int currentFloor;
        public int currentRoomIndex;
        public MathDifficulty difficulty;

        // ── Relics & upgrades ─────────────────────────────────────────
        public List<string> relicIds = new List<string>();

        // ── Stats accumulated over run ────────────────────────────────
        public int problemsSolved;
        public int problemsFailed;
        public int enemiesDefeated;
        public int totalDamageDealt;

        public RunData(int maxHp = 100, int maxMp = 50, MathDifficulty difficulty = MathDifficulty.High)
        {
            this.maxHp   = maxHp;
            this.maxMp   = maxMp;
            this.currentHp = maxHp;
            this.currentMp = maxMp;
            this.difficulty = difficulty;
            currentFloor = 1;
            currentRoomIndex = 0;
            gold = 0;
            score = 0;
        }

        public bool IsAlive => currentHp > 0;

        public void TakeDamage(int amount)
        {
            currentHp = Mathf.Max(0, currentHp - amount);
        }

        public void Heal(int amount)
        {
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }

        public void SpendMp(int amount)
        {
            currentMp = Mathf.Max(0, currentMp - amount);
        }

        public void RestoreMp(int amount)
        {
            currentMp = Mathf.Min(maxMp, currentMp + amount);
        }

        public void AddRelic(string relicId)
        {
            if (!relicIds.Contains(relicId))
                relicIds.Add(relicId);
        }

        public bool HasRelic(string relicId) => relicIds.Contains(relicId);
    }
}
