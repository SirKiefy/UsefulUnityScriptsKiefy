using UnityEngine;
using UsefulScripts.MathRoguelike.Core;

namespace UsefulScripts.MathRoguelike.Entities
{
    /// <summary>
    /// MonoBehaviour wrapper exposing the player's <see cref="RunData"/> stats
    /// and applying relic-based modifiers. Acts as the single source-of-truth
    /// for player combat values during a battle.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        // ── Base values (overridden by RunData) ───────────────────────
        [Header("Fallback Base Stats (used outside a Run)")]
        [SerializeField] private int fallbackMaxHp = 100;
        [SerializeField] private int fallbackMaxMp = 50;

        // ── Relic modifiers (set by RelicManager) ─────────────────────
        public int BonusDamage      { get; set; } = 0;
        public int DamageReduction  { get; set; } = 0;
        public float AnswerTimeBonus{ get; set; } = 0f;  // extra seconds added to timers

        // ── Derived stats ─────────────────────────────────────────────
        public int MaxHp  => Run != null ? Run.maxHp  : fallbackMaxHp;
        public int MaxMp  => Run != null ? Run.maxMp  : fallbackMaxMp;
        public int Hp     => Run != null ? Run.currentHp : fallbackMaxHp;
        public int Mp     => Run != null ? Run.currentMp : fallbackMaxMp;
        public bool Alive => Hp > 0;

        private RunData Run => MathRoguelikeGameManager.Instance?.CurrentRun;

        // ── Combat helpers ────────────────────────────────────────────

        /// <summary>Calculates final damage dealt to an enemy given a base value.</summary>
        public int CalculateAttackDamage(int baseDamage)
            => Mathf.Max(1, baseDamage + BonusDamage);

        /// <summary>Applies incoming damage after reduction.</summary>
        public void TakeDamage(int rawDamage)
        {
            int effective = Mathf.Max(0, rawDamage - DamageReduction);
            Run?.TakeDamage(effective);
        }

        public void Heal(int amount)     => Run?.Heal(amount);
        public void SpendMp(int amount)  => Run?.SpendMp(amount);
        public void RestoreMp(int amount)=> Run?.RestoreMp(amount);
    }
}
